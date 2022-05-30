// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VPSCoverage;

using UnityEngine;

namespace Niantic.ARDK.LocationService
{
  /// @note
  ///   Will start near front of the Ferry Building in San Francisco, California, USA
  ///   unless SetLocation is called before Start to specify a different starting location.
  public sealed class SpoofLocationService
    : ILocationService
  {
    public LocationServiceStatus Status { get; private set; } = LocationServiceStatus.Stopped;
    
    public event ArdkEventHandler<LocationStatusUpdatedArgs> StatusUpdated;
    public event ArdkEventHandler<LocationUpdatedArgs> LocationUpdated;
    public event ArdkEventHandler<CompassUpdatedArgs> CompassUpdated;
    
    // Default location, if not set, is front of San Francisco Ferry Building
    private LocationInfo _prevData = new LocationInfo(37.795215, -122.394073);

    public LocationInfo LastData
    {
      get
      {
        if (Status == LocationServiceStatus.Running)
          return _prevData;

        ARLog._WarnRelease("Location service updates are not enabled. Check LocationService.status before querying last location.");
        return new LocationInfo();
      }
    }
    
    private float _updateDistance;

    private LatLng _travelStart;
    private LatLng _travelEnd;
    
    private double _travelSeconds;
    private double _t;

    internal SpoofLocationService()
    {
    }

    public void Start()
    {
      Start
      (
        _UnityLocationService._DefaultAccuracyMeters,
        _UnityLocationService._DefaultDistanceMeters
      );
    }

    public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
    {
      _updateDistance = updateDistanceInMeters;

      SetStatus(LocationServiceStatus.Initializing);

      _UpdateLoop.Tick += OnUpdate;
    }

    public void Stop()
    {
      _UpdateLoop.Tick -= OnUpdate;

      SetStatus(LocationServiceStatus.Stopped);
    }

    private void OnUpdate()
    {
      if (Status == LocationServiceStatus.Initializing)
      {
        SetStatus(LocationServiceStatus.Running);

        var handler = LocationUpdated;
        if (handler != null)
        {
          var startInfo = new LocationInfo
          (
            _prevData.Coordinates.Latitude,
            _prevData.Coordinates.Longitude,
            _prevData.Altitude,
            _prevData.HorizontalAccuracy,
            _prevData.VerticalAccuracy,
            DateTimeOffset.Now.ToUnixTimeSeconds()
          );

          handler(new LocationUpdatedArgs(startInfo));
        }

        return;
      }

      if (Status != LocationServiceStatus.Running)
        return;

      if (_travelSeconds < 1)
        return;

      _t += Time.deltaTime;
      var alpha = _t / _travelSeconds;

      if (alpha > 1.0)
      {
        ARLog._Debug("Finished travel.");
        _travelSeconds = 0;
        _t = 0f;
      }

      var currLatLng = Lerp(_travelStart, _travelEnd, alpha);

      if (currLatLng.Distance(_prevData.Coordinates) > _updateDistance)
      {
        SetLocation
        (
          currLatLng.Latitude,
          currLatLng.Longitude,
          _prevData.Altitude,
          _prevData.HorizontalAccuracy,
          _prevData.VerticalAccuracy
        );
      }
    }

    public void SetStatus(LocationServiceStatus status)
    {
      Status = status;

      var handler = StatusUpdated;
      handler?.Invoke(new LocationStatusUpdatedArgs(status));
    }

    public void SetLocation(LatLng coordinates)
    {
      SetLocation(coordinates.Latitude, coordinates.Longitude);
    }

    public void SetLocation
    (
      double latitude,
      double longitude,
      double altitude = double.NaN,
      double horizontalAccuracy = double.NaN,
      double verticalAccuracy = double.NaN
    )
    {
      var info =
        new LocationInfo
        (
          latitude,
          longitude,
          altitude,
          horizontalAccuracy,
          verticalAccuracy,
          DateTimeOffset.Now.ToUnixTimeSeconds()
        );

      var heading = LatLng.Bearing(_prevData.Coordinates, info.Coordinates);
      SetCompass((float)heading, DateTimeOffset.Now.ToUnixTimeSeconds());

      _prevData = info;

      if (Status == LocationServiceStatus.Running)
      {
        var handler = LocationUpdated;
        handler?.Invoke(new LocationUpdatedArgs(info));
      }
    }

    public void SetCompass
    (
      float trueHeading,
      double timestamp = double.NaN
    )
    {
      float headingAccuracy = float.NaN;
      
      if (Status == LocationServiceStatus.Running)
      {
        var handler = CompassUpdated;
        if (handler != null)
        {
          var args = new CompassUpdatedArgs(headingAccuracy, trueHeading, timestamp);
          handler(args);
        }
      }
    }

    /// Uses linear interpolation to travel from the current location to the specified location at
    /// a constant speed. Noticeable errors may appear when locations are close to the poles. Travel
    /// will only commence when the location service's status is Running.
    /// @param bearing In degrees, clockwise from north
    /// @param distance In meters
    /// @param speed Meters per second
    public void StartTravel(double bearing, double distance, float speed)
    {
      _travelStart = _prevData.Coordinates;
      _travelEnd = _travelStart.Add(bearing, distance);

      _travelSeconds = distance / speed;

      if (_travelSeconds < 1)
      {
        ARLog._WarnRelease("Travel time is less than one second. This may cause no location updates to surface.");
      }

      ARLog._Debug($"Starting travel of {distance} meters");
    }

    public void StartTravel(LatLng destination, float speed)
    {
      var bearing = LatLng.Bearing(_prevData.Coordinates, destination);
      var distance = _prevData.Coordinates.Distance(destination);
      StartTravel(bearing, distance, speed);
    }

    private LatLng Lerp(LatLng a, LatLng b, double t)
    {
      var lat = a.Latitude * (1 - t) + b.Latitude * t;
      var lng = a.Longitude * (1 - t) + b.Longitude * t;

      return new LatLng(lat, lng);
    }
  }
}
