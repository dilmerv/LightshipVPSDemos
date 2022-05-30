// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDK.LocationService
{
  /// <summary>
  /// Latitude and Longitude, location on a globe
  /// </summary>
  [Serializable]
  public struct LatLng : IEquatable<LatLng>
  {
    [SerializeField]
    private double lat_degrees;
    public double Latitude { get { return lat_degrees; } }

    [SerializeField]
    private double lng_degrees;
    public double Longitude { get { return lng_degrees; } }

    public LatLng(double latitude, double longtitude)
    {
      if (latitude > 90.0 || latitude < -90.0)
      {
        throw new ArgumentOutOfRangeException("latitude", "Argument must be in range of -90 to 90");
      }

      if (longtitude > 180.0 || longtitude < -180.0)
      {
        throw new ArgumentOutOfRangeException("longitude", "Argument must be in range of -180 to 180");
      }

      lat_degrees = latitude;
      lng_degrees = longtitude;
    }

    public LatLng(LocationInfo locationInfo)
      : this (locationInfo.Coordinates.Latitude, locationInfo.Coordinates.Longitude)
    {
    }

    public LatLng(UnityEngine.LocationInfo locationInfo)
      : this (locationInfo.latitude, locationInfo.longitude)
    {
    }

    // LatLng objects that are equivalent have the same hash code.
    public override int GetHashCode()
    {
      unchecked {
        var hash = 31;
        hash += Latitude.GetHashCode();
        hash *= 397;
        hash += Longitude.GetHashCode();

        return hash;
      }
    }

    public override string ToString()
    {
      return $"[Latitude: {Latitude}, Longitude: {Longitude}]";
    }

    public override bool Equals(object obj)
    {
      if (!(obj is LatLng))
        return false;

      return Equals((LatLng)obj);
    }

    public bool Equals(LatLng other)
    {
      return this == other;
    }

    /// Calculates "as-the-crow-flies" distance between points using the Haversine formula.
    /// @returns Distance between points in meters.
    public double Distance(LatLng other)
    {
      if (Equals(other))
        return 0;

      if (double.IsNaN(Latitude) || double.IsNaN(Latitude) || double.IsNaN(other.Latitude) ||
        double.IsNaN(other.Longitude))
      {
        throw new ArgumentException("Latitude or longitude is NaN");
      }

      var rad1 = ToRadian();
      var rad2 = other.ToRadian();

      double latDelta = rad2.Latitude - rad1.Latitude;
      double lngDelta = rad2.Longitude - rad1.Longitude;

      double a =
        Math.Pow(Math.Sin(latDelta / 2.0), 2.0) +
        Math.Cos(rad1.Latitude) * Math.Cos(rad2.Latitude) * Math.Pow(Math.Sin(lngDelta / 2.0), 2.0);

      return 2.0 * EarthRadius * Math.Asin(Math.Sqrt(a));
    }

    /// Calculates "as-the-crow-flies" distance between points using the Haversine formula.
    /// @returns Distance between points in meters.
    public static double Distance(LatLng l1, LatLng l2)
    {
      return l1.Distance(l2);
    }

    /// Calculates the initial bearing (sometimes referred to as forward azimuth) which if
    /// followed in a straight line along a great-circle arc will take you from the l1 to l2 points.
    /// @returns Initial bearing in degrees
    public static double Bearing(LatLng l1, LatLng l2)
    {
      var rad1 = l1.ToRadian();
      var rad2 = l2.ToRadian();

      double lngDelta = rad2.Longitude - rad1.Longitude;

      double y = Math.Sin(lngDelta) * Math.Cos(rad2.Latitude);
      double x =
        Math.Cos(rad1.Latitude) * Math.Sin(rad2.Latitude) -
        Math.Sin(rad1.Latitude) * Math.Cos(rad2.Latitude) * Math.Cos(lngDelta);

      double r = Math.Atan2(y, x);

      return (r * 180.0/Math.PI + 360) % 360.0;
    }

    /// @param bearing Bearing in degrees, clockwise from north
    /// @param distance Distance travelled in meters
    public LatLng Add(double bearing, double distance)
    {
      var rad = ToRadian();

      bearing *= DegToRad;
      var angularDistance = distance / EarthRadius;

      var lat =
        Math.Asin
        (
          Math.Sin(rad.Latitude) * Math.Cos(angularDistance) +
          Math.Cos(rad.Latitude) * Math.Sin(angularDistance) * Math.Cos(bearing)
        );

      var lng =
        rad.Longitude +
        Math.Atan2
        (
          Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(rad.Latitude),
          Math.Cos(angularDistance) - Math.Sin(rad.Latitude) * Math.Sin(lat)
        );

      return (new LatLng(lat, lng)).ToDegrees();
    }

    public static bool operator ==(LatLng l1, LatLng l2)
    {
      return l1.Latitude.Equals(l2.Latitude) && l1.Longitude.Equals(l2.Longitude);
    }

    public static bool operator !=(LatLng l1, LatLng l2)
    {
      return !l1.Latitude.Equals(l2.Latitude) || !l1.Longitude.Equals(l2.Longitude);
    }

    const double EarthRadius = 6371009.0;
    const double DegToRad = Math.PI / 180.0;
    const double RadToDeg = 180.0 / Math.PI;
    public LatLng ToRadian()
    {
      return new LatLng(Latitude * DegToRad, Longitude * DegToRad);
    }

    public LatLng ToDegrees()
    {
      return new LatLng(Latitude * RadToDeg, Longitude * RadToDeg);
    }
  }
}
