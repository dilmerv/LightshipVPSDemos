// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Permissions;

using UnityEngine;
using UnityLocationServiceStatus = UnityEngine.LocationServiceStatus;

namespace Niantic.ARDK.LocationService
{
  /// Controls and surfaces location updates from the device.
  /// @note This is currently not supported with Remote Connection.
  internal sealed class _UnityLocationService:
    ILocationService
  {
    private LocationServiceStatus _prevStatus = LocationServiceStatus.Stopped;
    public LocationServiceStatus Status { get { return _prevStatus; } }

    private LocationInfo _prevData;
    public LocationInfo LastData { get { return _prevData; } }

    internal const float _DefaultAccuracyMeters = 10f;
    internal const float _DefaultDistanceMeters = 10f;

    private float _prevCompassHeading;
    private float _prevCompassAccuracy;

    public void Start()
    {
      Start(_DefaultAccuracyMeters, _DefaultDistanceMeters);
    }

    public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
    {
      if (!Input.location.isEnabledByUser)
      {
        ARLog._WarnRelease("Device's location services are not enabled.");
        CheckAndPublishStatusChange(LocationServiceStatus.DeviceAccessError);
        return;
      }

      // Start service
      Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
      Input.compass.enabled = true;

      _UpdateLoop.Tick += OnUpdate;
    }

    public void Stop()
    {
      Input.location.Stop();

      // Stop update loop
      _UpdateLoop.Tick -= OnUpdate;
    }

    // Check for location updates every frame
    private void OnUpdate()
    {
      var currentStatus = ConvertToCompatibleStatus(Input.location.status);
      CheckAndPublishStatusChange(currentStatus);

      switch (currentStatus)
      {
        case LocationServiceStatus.Initializing:
        case LocationServiceStatus.Stopped:
          // Do nothing
          return;

        case LocationServiceStatus.PermissionFailure:
        case LocationServiceStatus.DeviceAccessError:
          Stop();
          return;

        case LocationServiceStatus.Running:
          CheckAndPublishLocationChange(Input.location.lastData);
          CheckAndPublishCompassChange(Input.compass);
          return;
      }
    }

    // Convert between Unity location status and native location status
    private LocationServiceStatus ConvertToCompatibleStatus(UnityLocationServiceStatus unityStatus)
    {
      switch (unityStatus)
      {
        case UnityLocationServiceStatus.Initializing:
          return LocationServiceStatus.Initializing;

        case UnityLocationServiceStatus.Stopped:
          return LocationServiceStatus.Stopped;

        case UnityLocationServiceStatus.Running:
          return LocationServiceStatus.Running;

        case UnityLocationServiceStatus.Failed:
        {
#if UNITY_ANDROID
          if (!PermissionRequester.HasPermission(ARDKPermission.FineLocation))
            return LocationServiceStatus.PermissionFailure;

          if (!Input.location.isEnabledByUser)
            return LocationServiceStatus.DeviceAccessError;

          return LocationServiceStatus.UnknownError;
#else
          return LocationServiceStatus.PermissionFailure;
#endif
        }

        default:
          var message =
            "No ARDK.LocationService.LocationServiceStatus compatible with " +
            "UnityEngine.LocationServiceStatus {0} could be found.";

          throw new ArgumentOutOfRangeException(nameof(unityStatus), message);
      }
    }

    // Publish change in status of location service if needed
    private void CheckAndPublishStatusChange(LocationServiceStatus newStatus)
    {
      if (_prevStatus == newStatus)
        return;

      _prevStatus = newStatus;

      var handler = StatusUpdated;
      if (handler != null)
        handler(new LocationStatusUpdatedArgs(newStatus));
    }

    // Publish update in location if needed
    private void CheckAndPublishLocationChange(UnityEngine.LocationInfo info)
    {
      if (_prevData == new LocationInfo(info))
        return;

      _prevData = new LocationInfo(info);

      var handler = LocationUpdated;
      if (handler != null)
      {
        var args = new LocationUpdatedArgs(new LocationInfo(info));
        handler(args);
      }
    }

    private void CheckAndPublishCompassChange(Compass compass)
    {
      if (Equals(compass.trueHeading, _prevCompassHeading) &&
          Equals(compass.headingAccuracy, _prevCompassAccuracy))
      {
        return;
      }

      _prevCompassHeading = compass.trueHeading;
      _prevCompassAccuracy = compass.headingAccuracy;

      var handler = CompassUpdated;
      if (handler != null)
      {
        var args = new CompassUpdatedArgs
        (
          compass.trueHeading,
          compass.headingAccuracy,
          compass.timestamp
        );

        handler(args);
      }
    }

    public event ArdkEventHandler<LocationStatusUpdatedArgs> StatusUpdated;

    public event ArdkEventHandler<LocationUpdatedArgs> LocationUpdated;

    public event ArdkEventHandler<CompassUpdatedArgs> CompassUpdated;
  }
}
