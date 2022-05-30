// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.LocationService
{
  /// An object that manages location updates. Use the LocationServiceFactory to create an
  /// implementation for the desired runtime environment.
  ///
  /// The native implementation of this interface uses Unity's Input.location service, and so
  /// is only available on mobile devices.
  ///
  /// The in-editor implementation of this interface (SpoofLocationService) will start in
  /// San Francisco, California, USA unless SpoofLocationService.SetLocation is called to specify
  /// a different location. See the SpoofLocationService API for more methods to mock location and
  /// location movement in the Unity Editor.
  ///
  /// @note
  ///   In order to use LocationServices on iOS 10+, the "Location Usage Description" box in
  ///   the Player Settings > iOS > Other Settings panel must be filled out. If location permission
  ///   has not yet been granted, the permission request popup will automatically be launched by iOS
  ///   when Start is called.
  ///
  /// @note
  ///   For Android players, Location permissions must be requested prior to calling Start in order
  ///   for the call to succeed. Use or reference the[PermissionRequester](@ref Niantic.ARDK.Utilities.Permissions.PermissionRequester)
  ///   in order to do so.
  public interface ILocationService
  {
    LocationServiceStatus Status { get; }
    LocationInfo LastData { get; }

    /// Starts location service updates.
    void Start();

    void Start(float desiredAccuracyInMeters, float updateDistanceInMeters);

    /// Stops location service updates. This could be useful for saving battery life.
    void Stop();

    /// Informs subscribers when the session status changes.
    event ArdkEventHandler<LocationStatusUpdatedArgs> StatusUpdated;

    /// Informs subscribers when there is an update to the device's location.
    event ArdkEventHandler<LocationUpdatedArgs> LocationUpdated;

    /// Informs subscribers when there is an update to the device's compass.
    event ArdkEventHandler<CompassUpdatedArgs> CompassUpdated;
  }
}
