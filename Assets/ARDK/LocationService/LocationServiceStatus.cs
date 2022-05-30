// Copyright 2022 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.LocationService
{
  /// Describes location session status.
  public enum LocationServiceStatus
  {
    /// Location service is not active
    Stopped,

    /// Location service has been activated, but is not yet running
    Initializing,

    /// Location service is running, and location can be queried
    Running,

    /// Location service failed to initialize, due to the user denying
    /// app permission to device's location service
    PermissionFailure,

    /// Location service failed to initialize, due to user disabling
    /// location services at a device level
    DeviceAccessError,

    /// Unknown reason for Unity's location service failure
    UnknownError
  }
}
