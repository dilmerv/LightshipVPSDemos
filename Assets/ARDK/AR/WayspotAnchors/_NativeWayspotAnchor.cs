// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal class _NativeWayspotAnchor:
    _ThreadCheckedObject,
    IWayspotAnchor,
    _IInternalTrackable
  {
    /// Gets the native handle for this wayspot anchor
    internal IntPtr _NativeHandle { get; private set; }

    /// The event for when the position and/or rotation of the native wayspot acnhor has been updated
    public ArdkEventHandler<WayspotAnchorResolvedArgs> TrackingStateUpdated { get; set; }

    /// Checks whether or not the wayspot anchor's position/rotation is being tracked
    public bool Tracking { get; private set; }

    /// Sets whether or not the native anchor should be tracked
    /// @param tracking Whether or not to track the native anchor
    /// @note This is an internal method
    void _IInternalTrackable.SetTrackingEnabled(bool tracking) //This method is internal, not private
    {
      Tracking = tracking;
    }

    /// Creates a new native wayspot anchor
    /// @param nativeHandle The pointer to the native handle
    public _NativeWayspotAnchor(IntPtr nativeHandle)
    {
      _NativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);
    }

    /// Creates a new native wayspot anchor
    /// @param dataBlob The blob of data used to create the wayspot anchor
    public _NativeWayspotAnchor(byte[] dataBlob)
    {
      var nativeHandle = _NAR_ManagedPose_InitFromBlob(dataBlob, dataBlob.Length);
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("Failed to create wayspot anchor!", nameof(nativeHandle));

      _NativeHandle = nativeHandle;
      GC.AddMemoryPressure(_MemoryPressure);
    }

    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NAR_ManagedPose_Release(nativeHandle);
    }

    /// Disposes of the native wayspot anchor
    public void Dispose()
    {
      GC.SuppressFinalize(this);

      var nativeHandle = _NativeHandle;
      if (nativeHandle == IntPtr.Zero)
        return;

      _NativeHandle = IntPtr.Zero;

      _ReleaseImmediate(nativeHandle);
      GC.RemoveMemoryPressure(_MemoryPressure);
    }

    /// Gets the ID of the native wayspot anchor
    public Guid ID
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          Guid id;
          _NAR_ManagedPose_GetIdentifier(_NativeHandle, out id);
          return id;
        }
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
    }

    /// Gets the payload for the native wayspot anchor
    public WayspotAnchorPayload Payload
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          var dataSize = _NAR_ManagedPose_GetDataSize(_NativeHandle);
          byte[] dataArray = new byte[dataSize];
          unsafe
          {
            fixed (byte* ptr = dataArray)
            {
              IntPtr identifierPtr = (IntPtr)ptr;
              _NAR_ManagedPose_GetData(_NativeHandle, identifierPtr);
            }
          }

          var payload = new WayspotAnchorPayload(dataArray);
          return payload;
        }
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
    }

    private long _MemoryPressure
    {
      get => (255L);
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NAR_ManagedPose_InitFromBlob(byte[] blob, int dataSize);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ManagedPose_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ManagedPose_GetIdentifier
      (IntPtr nativeHandle, out Guid wayspotAnchorId);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ManagedPose_GetData
      (IntPtr nativeHandle, IntPtr wayspotAnchorData);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _NAR_ManagedPose_GetDataSize(IntPtr nativeHandle);
  }
}
