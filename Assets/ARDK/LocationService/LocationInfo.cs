// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

namespace Niantic.ARDK.LocationService
{
  public struct LocationInfo
  {
    /// Altitude relative to sea level, in meters.
    public readonly double Altitude;

    /// Geographical device location coordinates in degrees.
    public readonly LatLng Coordinates;

    /// Horizontal accuracy of the location.
    public readonly double HorizontalAccuracy;

    /// Vertical accuracy of the location.
    public readonly double VerticalAccuracy;

    /// POSIX Timestamp (in seconds since 1970) when location was recorded.
    public readonly double Timestamp;

    public LocationInfo(UnityEngine.LocationInfo info)
      : this
        (
          info.latitude,
          info.longitude,
          info.altitude,
          info.horizontalAccuracy,
          info.verticalAccuracy,
          info.timestamp
        )
    {
    }

    public LocationInfo(LatLng coordinates): this(coordinates.Latitude, coordinates.Longitude)
    {
    }

    public LocationInfo
    (
      double latitude,
      double longitude,
      double altitude = double.NaN,
      double horizontalAccuracy = double.NaN,
      double verticalAccuracy = double.NaN,
      double timestamp = double.NaN
    )
    {
      Coordinates = new LatLng(latitude, longitude);
      Altitude = altitude;
      HorizontalAccuracy = horizontalAccuracy;
      VerticalAccuracy = verticalAccuracy;
      Timestamp = timestamp;
    }

    // LocationInfo objects that are equivalent have the same hash code.
    public override int GetHashCode()
    {
      unchecked {
        var hash = 31;
        hash *= 97 + Coordinates.GetHashCode();
        hash *= 97 + Altitude.GetHashCode();
        hash *= 97 + HorizontalAccuracy.GetHashCode();
        hash *= 97 + VerticalAccuracy.GetHashCode();
        hash *= 97 + Timestamp.GetHashCode();

        return hash;
      }
    }

    public override bool Equals(object obj)
    {
      if (!(obj is LocationInfo))
        return false;

      return Equals((LocationInfo)obj);
    }

    public bool Equals(LocationInfo other)
    {
      return this == other;
    }

    public static bool operator ==(LocationInfo l1, LocationInfo l2)
    {
      return
        l1.Coordinates == l2.Coordinates &&
        ApproximatelyEquals(l1.Altitude, l2.Altitude) &&
        ApproximatelyEquals(l1.HorizontalAccuracy, l2.HorizontalAccuracy) &&
        ApproximatelyEquals(l1.VerticalAccuracy, l2.VerticalAccuracy) &&
        ApproximatelyEquals(l1.Timestamp, l2.Timestamp);
    }

    public static bool operator !=(LocationInfo l1, LocationInfo l2)
    {
      return !(l1 == l2);
    }

    public override string ToString()
    {
      return $"{Coordinates.Latitude}°, {Coordinates.Longitude}° (Altitude: {Altitude}, Horizontal Accuracy: {HorizontalAccuracy}, Vertical Accuracy: {VerticalAccuracy}";
    }

    private static bool ApproximatelyEquals(double x, double y)
    {
      if (double.IsNaN(x))
        return double.IsNaN(y);

      if (double.IsNaN(y))
        return false;

      return Math.Abs(x - y) < double.Epsilon;
    }
  }
}
