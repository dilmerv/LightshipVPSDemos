// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.Image;
using Niantic.ARDK.AR.LightEstimate;
using Niantic.ARDK.AR.PointCloud;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Frame
{
  internal sealed class _NativeARFrame:
    _IARFrame
  {
    // Used to inform the GC about how many unmanaged memory our type holds.
    // image + camera + anchors + raw feature points + maps
    private const long _MemoryPressure =
      (1L * 1080L * 1920L) +
      (2L * 1080L * (1920L / 2L)) +
      (1L * 8L) +
      (1L * 8L) +
      (1L * 8L);

    static _NativeARFrame()
    {
      Platform.Init();
    }

    internal static void _ReleaseImmediate(IntPtr framePtr)
    {
      switch (NativeAccess.Mode)
      {
        case NativeAccess.ModeType.Native:
          _NARFrame_ReleaseImageAndTextures(framePtr);
          _NARFrame_Release(framePtr);
          break;
        
#pragma warning disable 0162
        case NativeAccess.ModeType.Testing:
          _TestingShim.ReleasedHandles.Add(framePtr);
          break;
#pragma warning restore 0162
      }
    }
    private readonly _ThreadCheckedObject _threadChecker = new _ThreadCheckedObject();
#if DEBUG
    private readonly StackTrace _creationStack = new StackTrace();
#endif

    internal _NativeARFrame(IntPtr nativeHandle, float worldScale)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(nativeHandle));

      _NativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);

      WorldScale = worldScale;
    }

    ~_NativeARFrame()
    {
      ARLog._Error
      (
        "_NativeARFrame destructor invoked. This shouldn't happen.\n"+
        "If you are using a DisposalPolicy different from Dispose, you should manually dispose " +
        "of the frames by calling Dispose() on them."
#if DEBUG
        + "\nCreation Stack:\n" + _creationStack
#endif
      );

      _ReleaseImmediate(_NativeHandle);
      GC.RemoveMemoryPressure(_MemoryPressure);
    }

    public void Dispose()
    {
      _threadChecker._CheckThread();

      GC.SuppressFinalize(this);

      ReleaseImageAndTextures();

      var nativeHandle = _NativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _NativeHandle = IntPtr.Zero;

        _ReleaseImmediate(nativeHandle);
        GC.RemoveMemoryPressure(_MemoryPressure);
      }
    }

    internal IntPtr _NativeHandle { get; private set; }

    public ARFrameDisposalPolicy? DisposalPolicy { get; set; }

    public IDepthPointCloud DepthPointCloud { get; set; }

    public float WorldScale { get; }

    private IntPtr[] _capturedImageTextures;
    public IntPtr[] CapturedImageTextures
    {
      get
      {
        _threadChecker._CheckThread();

        #pragma warning disable 0162
        if (NativeAccess.Mode != NativeAccess.ModeType.Native)
          return EmptyArray<IntPtr>.Instance;
        #pragma warning restore 0162
        if (_capturedImageTextures != null)
          return _capturedImageTextures;

        var capturedImageTextures = EmptyArray<IntPtr>.Instance;
        while (true)
        {
          // When _NARFrame_GetGPUTextures receives a buffer smaller than the number of items, it
          // returns a negative value telling the amount of items the buffer needs to support.
          var obtained =
            _NARFrame_GetGPUTextures
            (
              _NativeHandle,
              capturedImageTextures.Length,
              capturedImageTextures
            );

          if (obtained == capturedImageTextures.Length)
          {
            _capturedImageTextures = capturedImageTextures;
            return capturedImageTextures;
          }

          capturedImageTextures = new IntPtr[Math.Abs(obtained)];
        }
      }
    }

    private _NativeImageBuffer _capturedImageBuffer;
    private bool _capturedImageBufferRead;

    public _NativeImageBuffer CapturedImageBuffer
    {
      get
      {
        _threadChecker._CheckThread();

        if (_capturedImageBufferRead)
          return _capturedImageBuffer;

        _capturedImageBufferRead = true;

        IntPtr imageBufferHandle = IntPtr.Zero;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          imageBufferHandle = _NARFrame_GetCPUImage(_NativeHandle);

        if (imageBufferHandle == IntPtr.Zero)
          return null;

        _capturedImageBuffer = new _NativeImageBuffer(imageBufferHandle);
        return _capturedImageBuffer;
      }
    }

    private _NativeDepthBuffer _depthBuffer;
    private _NativeSemanticBuffer _semanticBuffer;
    private bool _depthBufferRead;
    private bool _semanticBufferRead;

    public _NativeDepthBuffer Depth
    {
      get
      {
        _threadChecker._CheckThread();

        if (_depthBufferRead)
          return _depthBuffer;

        _depthBufferRead = true;

        IntPtr handle = IntPtr.Zero;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          handle = _NARFrame_GetDepthBuffer(_NativeHandle);

        if (handle == IntPtr.Zero)
          return null;

        _depthBuffer = new _NativeDepthBuffer(handle, WorldScale, Camera.Intrinsics);

        return _depthBuffer;
      }
    }

    public _NativeSemanticBuffer Semantics
    {
      get
      {
        _threadChecker._CheckThread();

        if (_semanticBufferRead)
          return _semanticBuffer;

        _semanticBufferRead = true;

        IntPtr handle = IntPtr.Zero;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          handle = _NARFrame_GetSemanticBuffer(_NativeHandle);

        if (handle == IntPtr.Zero)
          return null;

        _semanticBuffer = new _NativeSemanticBuffer(handle, WorldScale, Camera.Intrinsics);

        return _semanticBuffer;
      }
    }

    private _NativeARCamera _camera;

    public _NativeARCamera Camera
    {
      get
      {
        _threadChecker._CheckThread();

        var result = _camera;

        if (result == null)
        {
          IntPtr cameraHandle = IntPtr.Zero;

          if (NativeAccess.Mode == NativeAccess.ModeType.Native)
            cameraHandle = _NARFrame_GetCamera(_NativeHandle);

          // Using a constructor here instead of caching + reusing objects in _NativeARCamera._FromNativeHandle
          //  We are disposing the camera every frame to prevent a crash on exit. Reintroduce caching
          //  in a way that supports disposing.
          result = new _NativeARCamera(cameraHandle, WorldScale);
          _camera = result;
        }

        return result;
      }
    }

    private _NativeARLightEstimate _lightEstimate;
    private bool _lightEstimateRead;

    public _NativeARLightEstimate LightEstimate
    {
      get
      {
        _threadChecker._CheckThread();

        if (_lightEstimateRead)
          return _lightEstimate;

        _lightEstimateRead = true;

        var lightEstimateHandle = IntPtr.Zero;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          lightEstimateHandle = _NARFrame_GetLightEstimate(_NativeHandle);

        if (lightEstimateHandle == IntPtr.Zero)
          return null;

        var result = new _NativeARLightEstimate(lightEstimateHandle);
        _lightEstimate = result;
        return result;
      }
    }

    private ReadOnlyCollection<IARAnchor> _anchors;

    public ReadOnlyCollection<IARAnchor> Anchors
    {
      get
      {
        _threadChecker._CheckThread();

        var result = _anchors;

        if (result == null)
        {
          result = _GetAnchorArray().AsNonNullReadOnly<IARAnchor>();
          _anchors = result;
        }

        return result;
      }
    }

    private _NativeARAnchor[] _anchorArray;

    private _NativeARAnchor[] _GetAnchorArray()
    {
      if (_anchorArray != null)
        return _anchorArray;

      var rawAnchors = EmptyArray<IntPtr>.Instance;
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        while (true)
        {
          var obtained = _NARFrame_GetAnchors(_NativeHandle, rawAnchors.Length, rawAnchors);
          if (obtained == rawAnchors.Length)
            break;

          rawAnchors = new IntPtr[Math.Abs(obtained)];
        }
      }

      var anchorCount = rawAnchors.Length;
      var anchors = EmptyArray<_NativeARAnchor>.Instance;

      if (anchorCount > 0)
      {
        // Create the native types
        anchors = new _NativeARAnchor[anchorCount];

        for (var i = 0; i < anchorCount; i++)
        {
          var rawAnchor = rawAnchors[i];
          anchors[i] = _ARAnchorFactory._FromNativeHandle(rawAnchor);
        }
      }

      _anchorArray = anchors;
      return anchors;
    }

    private ReadOnlyCollection<IARMap> _maps;

    public ReadOnlyCollection<IARMap> Maps
    {
      get
      {
        _threadChecker._CheckThread();

        var result = _maps;

        if (result == null)
        {
          result = _GetMapArray().AsNonNullReadOnly<IARMap>();
          _maps = result;
        }

        return result;
      }
    }

    private _NativeARMap[] _mapArray;

    private _NativeARMap[] _GetMapArray()
    {
      if (_mapArray != null)
        return _mapArray;

      var rawMaps = EmptyArray<IntPtr>.Instance;
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        while (true)
        {
          var obtained = _NARFrame_GetMaps(_NativeHandle, rawMaps.Length, rawMaps);
          if (obtained == rawMaps.Length)
            break;

          rawMaps = new IntPtr[Math.Abs(obtained)];
        }
      }

      var mapCount = rawMaps.Length;
      var maps = EmptyArray<_NativeARMap>.Instance;

      if (mapCount > 0)
      {
        // Create the native types
        maps = new _NativeARMap[mapCount];

        for (var i = 0; i < mapCount; i++)
        {
          var rawMap = rawMaps[i];
          maps[i] = _NativeARMap._FromNativeHandle(rawMap, WorldScale);
        }
      }

      _mapArray = maps;
      return maps;
    }

    private _NativeARPointCloud _rawFeaturePoints;
    private bool _rawFeaturePointsRead;

    public _NativeARPointCloud RawFeaturePoints
    {
      get
      {
        _threadChecker._CheckThread();

        if (_rawFeaturePointsRead)
          return _rawFeaturePoints;

        _rawFeaturePointsRead = true;

        var rawFeaturePointsHandle = IntPtr.Zero;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          rawFeaturePointsHandle = _NARFrame_GetFeaturePoints(_NativeHandle);

        if (rawFeaturePointsHandle == IntPtr.Zero)
          return null;

        var result = new _NativeARPointCloud(rawFeaturePointsHandle, WorldScale);
        _rawFeaturePoints = result;
        return result;
      }
    }


    public ReadOnlyCollection<IARHitTestResult> HitTest
    (
      int viewportWidth,
      int viewportHeight,
      Vector2 screenPoint,
      ARHitTestResultType types
    )
    {
      _threadChecker._CheckThread();

      var viewportPoint =
        new Vector2
        (
          screenPoint.x / viewportWidth,
          screenPoint.y / viewportHeight
        );

      // Get the count and the handle to the hit test results
      var hitTestResultsHandle = IntPtr.Zero;
      uint hitCount = 0;
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        hitCount = _NARFrame_HitTestAgainstTypes
        (
          _NativeHandle,
          (UInt64)Screen.orientation,
          viewportWidth,
          viewportHeight,
          viewportPoint.x,
          viewportPoint.y,
          (UInt64)types,
          ref hitTestResultsHandle
        );
      }

      // Return zero if none
      if (hitTestResultsHandle == IntPtr.Zero)
        return EmptyReadOnlyCollection<IARHitTestResult>.Instance;

      if (hitCount == 0)
      {
        _Memory.Free(hitTestResultsHandle);
        return EmptyReadOnlyCollection<IARHitTestResult>.Instance;
      }

      // Copy directly into the IntPtr array, then free the handle
      var rawHitTestResults = new IntPtr[hitCount];
      Marshal.Copy(hitTestResultsHandle, rawHitTestResults, 0, (int)hitCount);
      _Memory.Free(hitTestResultsHandle);

      // Copy and create the C# types
      var hitTestResults = new IARHitTestResult[hitCount];

      for (var i = 0; i < (int)hitCount; i++)
        hitTestResults[i] = new _NativeARHitTestResult(rawHitTestResults[i], WorldScale);

      return hitTestResults.AsNonNullReadOnly();
    }

    public Matrix4x4 CalculateDisplayTransform
    (
      ScreenOrientation orientation,
      int viewportWidth,
      int viewportHeight
    )
    {
      _threadChecker._CheckThread();

      var nativeDisplayTransform = new float[6];

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARFrame_CalculateDisplayTransform
        (
          _NativeHandle,
          (UInt64)orientation,
          viewportWidth,
          viewportHeight,
          nativeDisplayTransform
        );
      }
      #pragma warning disable 0162
      else
        throw new IncorrectlyUsedNativeClassException();
      #pragma warning restore 0162

      return _Convert.DisplayAffineToMatrix4x4(nativeDisplayTransform);
    }

    public void ReleaseImageAndTextures()
    {
      _threadChecker._CheckThread();

      if (_capturedImageBufferRead)
      {
        var capturedBuffer = _capturedImageBuffer;
        if (capturedBuffer != null)
        {
          _capturedImageBuffer = null;
          capturedBuffer.Dispose();
        }

        _capturedImageBufferRead = false;
      }

      if (_depthBufferRead)
      {
        var depthBuffer = _depthBuffer;
        if (depthBuffer != null)
        {
          _depthBuffer = null;
          depthBuffer.Dispose();
        }

        _depthBufferRead = false;
      }
      if (_semanticBufferRead)
      {
        var semanticBuffer = _semanticBuffer;
        if (semanticBuffer != null)
        {
          _semanticBuffer = null;
          semanticBuffer.Dispose();
        }

        _semanticBufferRead = false;
      }

      if (_camera != null) {
          _camera.Dispose();
          _camera = null;
      }

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARFrame_ReleaseImageAndTextures(_NativeHandle);
#pragma warning disable 0162
      else
        _TestingShim.FramesWithReleasedTextures.Add(this);
#pragma warning restore 0162
    }

    IImageBuffer IARFrame.CapturedImageBuffer
    {
      get => CapturedImageBuffer;
    }

    IDepthBuffer IARFrame.Depth
    {
      get => Depth;
    }

    ISemanticBuffer IARFrame.Semantics
    {
      get => Semantics;
    }


    IARCamera IARFrame.Camera
    {
      get => Camera;
    }

    IARLightEstimate IARFrame.LightEstimate
    {
      get => LightEstimate;
    }

    IARPointCloud IARFrame.RawFeaturePoints
    {
      get => RawFeaturePoints;
    }

#region TestingShim
    internal static class _TestingShim
    {
      // This stores all native handles that would normally be freed by passing them to
      // _NARFrame_Release.
      public static List<IntPtr> ReleasedHandles = new List<IntPtr>();

      public static List<_NativeARFrame> FramesWithReleasedTextures = new List<_NativeARFrame>();

      // There is no guarantee that static objects will be cleaned up between tests, or multiple
      //   runs of tests. Explicitly clean up cached data that may affect other tests.
      public static void Reset()
      {
        ReleasedHandles.Clear();
        FramesWithReleasedTextures.Clear();
      }
    }
#endregion


    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARFrame_Release(IntPtr nativeHandle);

    // Returns a negative value if the provided buffer size is wrong. Such a negative value, if converted to positive,
    // represents the number of items the gpuTextures array should have. When a negative result is returned, nothing
    // was modified.
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _NARFrame_GetGPUTextures
    (
      IntPtr nativeHandle,
      int gpuTexturesLength,
      IntPtr[] gpuTextures
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARFrame_GetCPUImage(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARFrame_GetDepthBuffer(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARFrame_GetSemanticBuffer(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARFrame_ReleaseImageAndTextures(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern double _NARFrame_GetTimestamp(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARFrame_GetCamera(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARFrame_GetLightEstimate(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARFrame_CalculateDisplayTransform
    (
      IntPtr nativeHandle,
      UInt64 interfaceOrientation,
      Int32 viewportWidth,
      Int32 viewportHeight,
      float[] outTransform
    );

    // Returns a negative value if the provided buffer size is wrong. Such a negative value, if converted to positive,
    // represents the number of items the outAnchors array should have. When a negative result is returned, nothing
    // was modified.
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _NARFrame_GetAnchors
    (
      IntPtr nativeHandle,
      int outAnchorsLength,
      IntPtr[] outAnchors
    );

    // Returns a negative value if the provided buffer size is wrong. Such a negative value, if converted to positive,
    // represents the number of items the outMaps array should have. When a negative result is returned, nothing
    // was modified.
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _NARFrame_GetMaps
    (
      IntPtr nativeHandle,
      int outMapsLength,
      IntPtr[] outMaps
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARFrame_HitTestAgainstTypes
    (
      IntPtr nativeHandle,
      UInt64 interfaceOrientation,
      Int32 viewportWidth,
      Int32 viewportHeight,
      float pointX,
      float pointY,
      UInt64 types,
      ref IntPtr hitTestResultsHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARFrame_GetFeaturePoints(IntPtr nativeHandle);
  }
}
