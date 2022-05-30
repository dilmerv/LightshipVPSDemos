// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

using Niantic.ARDK.LocationService;

namespace Niantic.ARDK.VPSCoverage.GeoserviceMessages
{
  [Serializable]
  internal class _LocalizationTargetsResponse
  {
    public VpsLocalizationTarget[] vps_localization_target;
    public string status;
    public string error_message;

    public _LocalizationTargetsResponse
    (
      VpsLocalizationTarget[] vpsLocalizationTarget,
      string status,
      string errorMessage
    )
    {
      vps_localization_target = vpsLocalizationTarget;
      this.status = status;
      error_message = errorMessage;
    }

    [Serializable]
    public struct VpsLocalizationTarget
    {
      public Shape shape;
      public string id;
      public string name;
      public string image_url;

      public VpsLocalizationTarget(Shape shape, string id, string name, string imageURL)
      {
        this.shape = shape;
        this.id = id;
        this.name = name;
        image_url = imageURL;
      }
    }

    [Serializable]
    public struct Shape
    {
      public LatLng point;

      public Shape(LatLng point)
      {
        this.point = point;
      }
    }
  }
}