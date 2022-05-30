// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Linq;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.VPSCoverage.GeoserviceMessages;

namespace Niantic.ARDK.VPSCoverage
{
  /// Received result from server request for CoverageAreas.
  public class CoverageAreasResult
  {
    /// Response status of server request.
    public ResponseStatus Status { get; }

    /// Found CoverageAreas found for the request.
    public CoverageArea[] Areas { get; }

    internal CoverageAreasResult(_CoverageAreasResponse response)
    {
      Status = _ResponseStatusTranslator.FromString(response.status);
      Areas = response.vps_coverage_area.Select(t => new CoverageArea(t)).ToArray();
    }

    internal CoverageAreasResult(_HttpResponse<_CoverageAreasResponse> response)
    {
      Status = response.Status;

      if (Status == ResponseStatus.Success)
      {
        if (response.Data.vps_coverage_area != null)
          Areas = response.Data.vps_coverage_area.Select(t => new CoverageArea(t)).ToArray();
        else
          Areas = Array.Empty<CoverageArea>();
      }
    }
  }
}
