// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Runtime.InteropServices;

using AOT;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal sealed class NativeWayspotAnchorImplementation:
    _ThreadCheckedObject,
    _IWayspotAnchorImplementation
  {
    // Private handles and code to deal with native callbacks and initialization
    private IntPtr _nativeHandle;

    // Caching `this` for native device callbacks
    private IntPtr _cachedHandleIntPtr = IntPtr.Zero;
    private SafeGCHandle<NativeWayspotAnchorImplementation> _cachedHandle;

    private IntPtr _handle
    {
      get
      {
        _CheckThread();

        var cachedHandleIntPtr = _cachedHandleIntPtr;
        if (cachedHandleIntPtr != IntPtr.Zero)
          return cachedHandleIntPtr;

        _cachedHandle = SafeGCHandle.Alloc(this);
        cachedHandleIntPtr = _cachedHandle.ToIntPtr();
        _cachedHandleIntPtr = cachedHandleIntPtr;

        return cachedHandleIntPtr;
      }
    }

    private readonly IARSession _arSession;

    private ArdkEventHandler<LocalizationStateUpdatedArgs> _localizationStateUpdated;

    /// Called when the localization state has changed
    public event ArdkEventHandler<LocalizationStateUpdatedArgs> LocalizationStateUpdated
    {
      add
      {
        _CheckThread();

        _SubscribeToDidUpdateLocalizationState();

        _localizationStateUpdated += value;
      }
      remove
      {
        _localizationStateUpdated -= value;
      }
    }

    private ArdkEventHandler<WayspotAnchorStatusUpdatedArgs> _wayspotAnchorStatusesUpdated;

    /// Called when the status of wayspot anchors has changed
    public event ArdkEventHandler<WayspotAnchorStatusUpdatedArgs> WayspotAnchorStatusUpdated
    {
      add
      {
        _CheckThread();

        _SubscribeToDidUpdateWayspotAnchorStatuses();

        _wayspotAnchorStatusesUpdated += value;
      }
      remove
      {
        _wayspotAnchorStatusesUpdated -= value;
      }
    }

    private ArdkEventHandler<WayspotAnchorsCreatedArgs> _wayspotAnchorsCreated;

    /// Called when new wayspot anchors have been created
    public event ArdkEventHandler<WayspotAnchorsCreatedArgs> WayspotAnchorsCreated
    {
      add
      {
        _CheckThread();

        _SubscribeToDidCreateWayspotAnchors();

        _wayspotAnchorsCreated += value;
      }
      remove
      {
        _wayspotAnchorsCreated -= value;
      }
    }

    private ArdkEventHandler<WayspotAnchorsResolvedArgs> _wayspotAnchorsResolved;

    /// Called when wayspot anchors have been resolved
    public event ArdkEventHandler<WayspotAnchorsResolvedArgs> WayspotAnchorsResolved
    {
      add
      {
        _CheckThread();

        _SubscribeToDidResoveWayspotAnchors();

        _wayspotAnchorsResolved += value;
      }
      remove
      {
        _wayspotAnchorsResolved -= value;
      }
    }

    /// Creates a new native wayspot anchor controller
    /// @param session The session used to create the native wayspot anchor controller
    internal NativeWayspotAnchorImplementation(IARSession session)
    {
      _arSession = session;
      _nativeHandle = _NAR_ManagedPoseController_Init(session.StageIdentifier.ToByteArray());
      if (_nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(_nativeHandle));
    }

    /// Creates new wayspot anchors
    /// @param localPoses The local poses (position and rotation only) used to create the wayspot anchors
    /// @return The IDs of the newly created wayspot anchors
    public Guid[] CreateWayspotAnchors(params Matrix4x4[] localPoses)
    {
      _CheckThread();

      var numPoses = localPoses.Length;
      float[] nativeTransforms = new float[16 * numPoses];
      for (int i = 0; i < numPoses; i++)
      {
        var poseArray = _Convert.Matrix4x4ToInternalArray
          (NARConversions.FromUnityToNAR(localPoses[i]));

        for (int x = 0; x < 16; x++)
        {
          nativeTransforms[16 * i + x] = poseArray[x];
        }
      }

      int kBytesPerIdentifier = 16;
      byte[] identifierByteArrayOut = new byte[kBytesPerIdentifier * numPoses];

      unsafe
      {
        fixed (byte* ptr = identifierByteArrayOut)
        {
          IntPtr identifierPtr = (IntPtr)ptr;
          _NAR_ManagedPoseController_CreateManagedPoses
            (
              _nativeHandle, 
              (UInt64)numPoses, 
              nativeTransforms, 
              identifierPtr
            );
        }
      }

      var identifierArrayOut = new Guid[numPoses];
      for (int i = 0; i < numPoses; i++)
      {
        byte[] identifierBytes = new byte[kBytesPerIdentifier];
        for (int b = 0; b < kBytesPerIdentifier; b++)
        {
          identifierBytes[b] = identifierByteArrayOut[kBytesPerIdentifier * i + b];
        }

        identifierArrayOut[i] = new Guid(identifierBytes);
      }

      return identifierArrayOut;
    }

    /// Starts resolving wayspot anchors.  Resolving anchors will have their position and rotation updates reported via the _wayspotAnchorsResolved
    /// event
    /// @param wayspotAnchors The wayspot anchors to update
    public void StartResolvingWayspotAnchors(params IWayspotAnchor[] wayspotAnchors)
    {
      _CheckThread();

      if (!_ValidateARSessionIsAlive())
      {
        throw new Exception("AR Session validation has failed.");
      }

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var numPoses = wayspotAnchors.Length;

        IntPtr[] wayspotAnchorsHandles = new IntPtr[numPoses];
        for (int i = 0; i < numPoses; i++)
        {
          var wayspotAnchor = wayspotAnchors[i];
          if (wayspotAnchor is _NativeWayspotAnchor nativeWayspotAnchor)
          {
            wayspotAnchorsHandles[i] = nativeWayspotAnchor._NativeHandle;
          }
          else
          {
            ARLog._Error
            (
              $"Must use a {nameof(_NativeWayspotAnchor)} with {nameof(NativeWayspotAnchorImplementation)}"
            );

            return;
          }
        }

        _NAR_ManagedPoseController_StartResolvingManagedPoses(_nativeHandle, (UInt64)numPoses, wayspotAnchorsHandles);
      }
    }

    /// Stops resolving the wayspot anchors
    /// @param wayspotAnchors The wayspot anchors to stop resolving
    public void StopResolvingWayspotAnchors(params IWayspotAnchor[] wayspotAnchors)
    {
      _CheckThread();

      if (!_ValidateARSessionIsAlive())
      {
        ARLog._Error("The ARSession is deinitialized, cannot stop localization");
        return;
      }

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var numPoses = wayspotAnchors.Length;

        IntPtr[] wayspotAnchorHandles = new IntPtr[numPoses];
        for (int i = 0; i < numPoses; i++)
        {
          var wayspotAnchor = wayspotAnchors[i];
          if (wayspotAnchor is _NativeWayspotAnchor nativeWayspotAnchor)
          {
            wayspotAnchorHandles[i] = nativeWayspotAnchor._NativeHandle;
          }
          else
          {
            var error = "Must use a _NativeManagedPose with _NativeManagedPoseController";
            ARLog._Error(error);
            return;
          }
        }

        _NAR_ManagedPoseController_StopResolvingManagedPoses
          (_nativeHandle, (UInt64)numPoses, wayspotAnchorHandles);
      }
    }

    /// Whether or not the native wayspot anchor controller has been destroyed
    public bool IsDestroyed
    {
      get => _nativeHandle == IntPtr.Zero;
    }

    /// Disposees of the native wayspot anchor controller
    public void Dispose()
    {
      _CheckThread();

      GC.SuppressFinalize(this);

      var nativeHandle = _nativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _nativeHandle = IntPtr.Zero;

        _ReleaseImmediate(nativeHandle);
      }

      _cachedHandle.Free();
      _cachedHandleIntPtr = IntPtr.Zero;
    }

    /// <inheritdoc />
    public void StartVPS(IWayspotAnchorsConfiguration wayspotAnchorsConfiguration)
    {
      _CheckThread();

      if (wayspotAnchorsConfiguration == null)
      {
        throw new ArgumentNullException(nameof(wayspotAnchorsConfiguration));
      }

      if (!_ValidateARSessionIsAlive())
      {
        ARLog._Error("The ARSession is deinitialized, cannot start localization");
        return;
      }

      if (_arSession is _NativeARSession nativeSession)
      {
        if (!nativeSession._IsLocationServiceInitialized())
        {
          ARLog._Error
          (
            "SetupLocationService(locationService) must be called before attempting to localize" +
            " against any available world coordinate space."
          );

          return;
        }
      }

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        if (wayspotAnchorsConfiguration is _NativeWayspotAnchorsConfiguration nativeConfig)
        {
          _NAR_ManagedPoseController_StartVPS
          (
            _nativeHandle,
            nativeConfig._NativeHandle
          );
        }
        else
        {
          ARLog._Error
          (
            $"Must use a {nameof(_NativeWayspotAnchorsConfiguration)} with {nameof(NativeWayspotAnchorImplementation)}"
          );
        }
      }
    }

    /// <inheritdoc />
    public void StopVPS()
    {
      _CheckThread();

      if (!_ValidateARSessionIsAlive())
      {
        ARLog._Error("The ARSession is deinitialized, cannot stop localization");
        return;
      }

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NAR_ManagedPoseController_StopVPS(_nativeHandle);
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NAR_ManagedPoseController_Release(nativeHandle);
      }
    }

    private bool _ValidateARSessionIsAlive()
    {
      if (_arSession is _NativeARSession nativeSession)
      {
        return !nativeSession.IsDestroyed;
      }

      ARLog._Error
      (
        $"Must use a {nameof(_NativeWayspotAnchorsConfiguration)} with ${nameof(NativeWayspotAnchorImplementation)}"
      );

      return false;
    }

    private bool _onDidUpdateLocalizationState;

    private void _SubscribeToDidUpdateLocalizationState()
    {
      _CheckThread();

      if (_onDidUpdateLocalizationState)
        return;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NAR_ManagedPoseController_Set_didUpdateLocalizationStateCallback
        (
          _handle,
          _nativeHandle,
          _onDidUpdateLocalizationStateNative
        );

        ARLog._Debug("Subscribed to native localization updated");
      }

      _onDidUpdateLocalizationState = true;
    }

    private bool _onDidUpdateWayspotAnchorStatuses;

    private void _SubscribeToDidUpdateWayspotAnchorStatuses()
    {
      _CheckThread();

      if (_onDidUpdateWayspotAnchorStatuses)
        return;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NAR_ManagedPoseController_Set_didUpdateManagedPoseStatusCodesCallback
        (
          _handle,
          _nativeHandle,
          _onDidUpdateManagedPoseStatusesNative
        );

        ARLog._Debug("Subscribed to native localization updated");
      }

      _onDidUpdateWayspotAnchorStatuses = true;
    }

    private bool _onDidCreateWayspotAnchors;

    private void _SubscribeToDidCreateWayspotAnchors()
    {
      _CheckThread();

      if (_onDidCreateWayspotAnchors)
        return;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NAR_ManagedPoseController_Set_didCreateManagedPosesCallback
        (
          _handle,
          _nativeHandle,
          _onDidCreateManagedPosesNative
        );

        ARLog._Debug("Subscribed to native localization updated");
      }

      _onDidCreateWayspotAnchors = true;
    }

    private bool _onDidResolveWayspotAnchors;

    private void _SubscribeToDidResoveWayspotAnchors()
    {
      _CheckThread();

      if (_onDidResolveWayspotAnchors)
        return;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NAR_ManagedPoseController_Set_didResolveManagedPosesCallback
        (
          _handle,
          _nativeHandle,
          _onDidResolveManagedPosesNative
        );

        ARLog._Debug("Subscribed to native localization updated");
      }

      _onDidResolveWayspotAnchors = true;
    }


#region NativeCallbacks
    [MonoPInvokeCallback(typeof(_onDidUpdateLocalizationState_Definition))]
    private static void _onDidUpdateLocalizationStateNative
    (
      IntPtr context,
      UInt32 state,
      UInt32 failureReason
    )
    {
      var controller = SafeGCHandle.TryGetInstance<NativeWayspotAnchorImplementation>(context);
      if (controller == null || controller.IsDestroyed)
      {
        // controller was deallocated
        ARLog._Debug("controller is null in _onDidUpdateLocalizationStateNative()");
        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (controller.IsDestroyed)
          {
            // controller was deallocated
            return;
          }

          if (controller._localizationStateUpdated != null)
          {
            var args =
              new LocalizationStateUpdatedArgs
              (
                (LocalizationState)state,
                (LocalizationFailureReason)failureReason
              );

            controller._localizationStateUpdated(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_onDidUpdateManagedPoseStatusesDefinition))]
    private static void _onDidUpdateManagedPoseStatusesNative
    (
      IntPtr context,
      IntPtr identifiers,
      IntPtr statusCodes,
      UInt64 numOfWayspotAnchors
    )
    {
      int kBytesPerIdentifier = 16;
      int count = (int)numOfWayspotAnchors;
      byte[] byteArray = new byte[count * kBytesPerIdentifier];
      Guid[] guids = new Guid[count];
      Marshal.Copy(identifiers, byteArray, 0, count * kBytesPerIdentifier);
      for (int i = 0; i < count; i++)
      {
        byte[] identifierBytes = new byte[kBytesPerIdentifier];
        for (int b = 0; b < kBytesPerIdentifier; b++)
        {
          identifierBytes[b] = byteArray[kBytesPerIdentifier * i + b];
        }
        guids[i] = new Guid(identifierBytes);
      }


      int numStatusCodes = (int)numOfWayspotAnchors;
      int[] statusCodesArray = new int[numStatusCodes];
      Marshal.Copy(statusCodes, statusCodesArray, 0, numStatusCodes);


      var controller = SafeGCHandle.TryGetInstance<NativeWayspotAnchorImplementation>(context);
      if (controller == null || controller.IsDestroyed)
      {
        // controller was deallocated
        ARLog._Debug("controller is null in _onDidUpdateManagedPoseStatusesNative()");
        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (controller.IsDestroyed)
          {
            // controller was deallocated
            return;
          }

          if (controller._wayspotAnchorStatusesUpdated != null)
          {
            WayspotAnchorStatusUpdate[] statusesArray = new WayspotAnchorStatusUpdate[numOfWayspotAnchors];
            for (int i = 0; i < (int)numOfWayspotAnchors; i++)
            {
              var status = new WayspotAnchorStatusUpdate
              (
                guids[i],
                (WayspotAnchorStatusCode)statusCodesArray[i]
              );
              statusesArray[i] = status;
            }

            var args = new WayspotAnchorStatusUpdatedArgs(statusesArray);
            controller._wayspotAnchorStatusesUpdated(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_onDidCreateManagedPoses_Definition))]
    private static void _onDidCreateManagedPosesNative
    (
      IntPtr context,
      IntPtr[] wayspotAnchorHandles,
      UInt64 numOfWayspotAnchors
    )
    {
      var controller = SafeGCHandle.TryGetInstance<NativeWayspotAnchorImplementation>(context);
      if (controller == null || controller.IsDestroyed)
      {
        // controller was deallocated
        ARLog._Debug("controller is null in _onDidCreateManagedPosesNative()");
        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (controller.IsDestroyed)
          {
            // controller was deallocated
            return;
          }

          if (controller._wayspotAnchorsCreated != null)
          {
            IWayspotAnchor[] wayspotAnchors = new IWayspotAnchor[numOfWayspotAnchors];
            for (int i = 0; i < (int)numOfWayspotAnchors; i++)
            {
              var wayspotAnchor = new _NativeWayspotAnchor(wayspotAnchorHandles[i]);
              wayspotAnchors[i] = wayspotAnchor;
            }

            var args = new WayspotAnchorsCreatedArgs(wayspotAnchors);

            controller._wayspotAnchorsCreated(args);
          }
        }
      );
    }

    [MonoPInvokeCallback(typeof(_onDidResolveManagedPoses_Definition))]
    private static void _onDidResolveManagedPosesNative
    (
      IntPtr context,
      IntPtr identifiers, //guid[]
      IntPtr localPoses, //float[]
      IntPtr accuracies, //float[]
      UInt64 numOfWayspotAnchors
    )
    {
      int count = (int)numOfWayspotAnchors;
      if (count <= 0)
      {
        ARLog._Error($"Invalid number of managed poses resolved! ({count})");
        return;
      }
      byte[] byteArray = new byte[count * 16];
      Guid[] guids = new Guid[count];
      Marshal.Copy(identifiers, byteArray, 0, count * 16);
      for (int i = 0; i < count; i++)
      {
        byte[] identifierBytes = new byte[16];
        for (int b = 0; b < 16; b++)
        {
          identifierBytes[b] = byteArray[16 * i + b];
        }

        guids[i] = new Guid(identifierBytes);
      }

      int lpSize = (int)numOfWayspotAnchors * 16;
      float[] lp = new float[lpSize];
      Marshal.Copy(localPoses, lp, 0, lpSize);

      int accSize = (int)numOfWayspotAnchors * 6;
      float[] acc = new float[accSize];
      Marshal.Copy(accuracies, acc, 0, accSize);

      var controller = SafeGCHandle.TryGetInstance<NativeWayspotAnchorImplementation>(context);
      if (controller == null || controller.IsDestroyed)
      {
        // controller was deallocated
        ARLog._Debug("controller is null in _onDidResolveManagedPosesNative()");
        return;
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (controller.IsDestroyed)
          {
            // controller was deallocated
            return;
          }

          if (controller._wayspotAnchorsResolved != null)
          {
            var resolutions = new WayspotAnchorResolvedArgs[numOfWayspotAnchors];
            for (int i = 0; i < (int)numOfWayspotAnchors; i++)
            {
              float[] localPoseArray = new float[16];
              for (int x = 0; x < 16; x++)
              {
                localPoseArray[x] = lp[i * 16 + x];
              }

              var localPoseTransform = NARConversions.FromNARToUnity
                (_Convert.InternalToMatrix4x4(localPoseArray));

              var resolution = new WayspotAnchorResolvedArgs(guids[i], localPoseTransform);
              resolutions[i] = resolution;
            }

            var args = new WayspotAnchorsResolvedArgs(resolutions);

            controller._wayspotAnchorsResolved(args);
          }
        }
      );
    }
#endregion

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NAR_ManagedPoseController_Init(byte[] stageIdentifier);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ManagedPoseController_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ManagedPoseController_StartVPS
    (
      IntPtr nativeHandle,
      IntPtr nativeConfigHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ManagedPoseController_StopVPS(IntPtr nativeHandle);


    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ManagedPoseController_CreateManagedPoses
    (
      IntPtr nativeHandle,
      UInt64 numOfWayspotAnchors,
      float[] localPosesIn,
      IntPtr wayspotAnchorIdsOut
    );


    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ManagedPoseController_StartResolvingManagedPoses
    (
      IntPtr nativeHandle,
      UInt64 numOfWayspotAnchors,
      IntPtr[] wayspotAnchorHandlesArray
    );


    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ManagedPoseController_StopResolvingManagedPoses
    (
      IntPtr nativeHandle,
      UInt64 numOfWayspotAnchors,
      IntPtr[] wayspotAnchorHandlesArray
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ManagedPoseController_Set_didUpdateLocalizationStateCallback
    (
      IntPtr applicationSession,
      IntPtr platformSession,
      _onDidUpdateLocalizationState_Definition callback
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void
      _NAR_ManagedPoseController_Set_didUpdateManagedPoseStatusCodesCallback
      (
        IntPtr applicationSession,
        IntPtr platformSession,
        _onDidUpdateManagedPoseStatusesDefinition callback
      );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ManagedPoseController_Set_didCreateManagedPosesCallback
    (
      IntPtr applicationSession,
      IntPtr platformSession,
      _onDidCreateManagedPoses_Definition callback
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ManagedPoseController_Set_didResolveManagedPosesCallback
    (
      IntPtr applicationSession,
      IntPtr platformSession,
      _onDidResolveManagedPoses_Definition callback
    );

    private delegate void _onDidUpdateLocalizationState_Definition
    (
      IntPtr handle,
      UInt32 state,
      UInt32 error
    );


    private delegate void _onDidUpdateManagedPoseStatusesDefinition
    (
      IntPtr handle,
      IntPtr identifierArrays,
      IntPtr statusCodes,
      UInt64 numOfWayspotAnchors
    );

    private delegate void _onDidCreateManagedPoses_Definition
    (
      IntPtr handle,
      IntPtr[] wayspotAnchorHandles,
      UInt64 numOfWayspotAnchors
    );

    private delegate void _onDidResolveManagedPoses_Definition
    (
      IntPtr handle,
      IntPtr identifierArrays,
      IntPtr localPoses,
      IntPtr accuracies,
      UInt64 numOfWayspotAnchors
    );
  }
}
