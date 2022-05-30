// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Linq;

using Niantic.ARDK.LocationService;
using Niantic.ARDK.VPSCoverage.GeoserviceMessages;

using UnityEngine;

namespace Niantic.ARDK.VPSCoverage
{
  /// An area where localization with VPS is possible.
  [Serializable]
  public struct CoverageArea
  {
    [SerializeField]
    private string[] _localizationTargetIdentifiers;
    /// Identifiers of all LocalizationTargets within the CoverageArea.
    public string[] LocalizationTargetIdentifiers { get { return _localizationTargetIdentifiers; } }

    [SerializeField]
    private LatLng[] _shape;
    /// Polygon outlining the CoverageArea.
    public LatLng[] Shape { get { return _shape; } }

    /// Centroid of the Shape polygon.
    public LatLng Centroid { get; private set; }

    [SerializeField]
    private Localizability _localizabilityQuality;
    
    /// The localizability quality gives information about the chances of a successful localization
    /// in this CoverageArea.  
    public Localizability LocalizabilityQuality { get {return _localizabilityQuality;} }

    /// The localizability quality is split into CoverageAreas above a quality threshold marked as
    /// PRODUCTION, and CoverageAreas that have a lower localizability quality marked as EXPERIMENTAL. 
    public enum Localizability 
    {
      UNSET,
      PRODUCTION,
      EXPERIMENTAL
    }
    
    internal CoverageArea(_CoverageAreasResponse.VpsCoverageArea location)
      : this(location.vps_localization_target_id, location.shape.polygon.loop[0].vertex, location.localizability)
    {
    }

    internal CoverageArea(string[] localizationTargetIdentifiers, LatLng[] shape, string localizability)
    {
      _localizationTargetIdentifiers = localizationTargetIdentifiers;
      _shape = shape;
      Centroid = CalculateCentroid(_shape);

      Enum.TryParse(localizability, out _localizabilityQuality);
    }

    // taken from https://stackoverflow.com/questions/6671183/calculate-the-center-point-of-multiple-latitude-longitude-coordinate-pairs
    private static LatLng CalculateCentroid(params LatLng[] points)
    {
      if (points.Length == 1)
      {
        return points.Single();
      }

      double x = 0;
      double y = 0;
      double z = 0;

      foreach (LatLng point in points)
      {
        var latitude = point.Latitude * Math.PI / 180;
        var longitude = point.Longitude * Math.PI / 180;

        x += Math.Cos(latitude) * Math.Cos(longitude);
        y += Math.Cos(latitude) * Math.Sin(longitude);
        z += Math.Sin(latitude);
      }

      int total = points.Length;

      x = x / total;
      y = y / total;
      z = z / total;

      var centralLongitude = Math.Atan2(y, x);
      var centralSquareRoot = Math.Sqrt(x * x + y * y);
      var centralLatitude = Math.Atan2(z, centralSquareRoot);

      return new LatLng(centralLatitude * 180 / Math.PI, centralLongitude * 180 / Math.PI);
    }

    // JSONUtility can not handle the nested array format of GeoJson and Newtonsoft will add an
    // additional dependency. Manual built string works for now.
    
    /// Convert the CoverageArea information into a GeoJson string. 
    /// @returns GeoJson as string
    public string ToGeoJson()
    {
      string geoJson =
        "{" +
          "\"type\": \"FeatureCollection\", \"features\": " +
          "[{" +
            "\"type\": \"Feature\",\"geometry\": " +
            "{" +
              "\"type\": \"Polygon\"," + "\"properties\": " +
              "{" +
                "\"location_type\": \"coverage_area\", " +
                "\"localizability_quality\": \"" + _localizabilityQuality + "\", " +
                "\"location_target_identifiers\": " +
                "[";
                    for (int i = 0; i < _localizationTargetIdentifiers.Length; i++)
                    {
                      if (i != 0)
                        geoJson += ",";
                      geoJson += "\"" + _localizationTargetIdentifiers[i] + "\"";
                    }
                    geoJson += "]}," +
              "\"coordinates\": " +
              "[[";
                for (int i = 0; i < Shape.Length; i++)
                {
                  geoJson += "[" + Shape[i].Longitude + "," + Shape[i].Latitude + "],";
                }
                geoJson +=
                "[" + Shape[0].Longitude + "," + Shape[0].Latitude + "]";
                geoJson += 
              "]]" + 
            "}" +
          "}]" +
        "}";
                
        return geoJson;
    }
  }
}
