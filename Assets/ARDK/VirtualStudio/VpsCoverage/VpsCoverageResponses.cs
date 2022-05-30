// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.VPSCoverage;
using Niantic.ARDK.VPSCoverage.GeoserviceMessages;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.VpsCoverage
{
  // Ensure object is a singleton by hiding UI to create another instance. Not foolproof because an
  // instance could be created by script, but good enough?
  //[CreateAssetMenu(fileName = "VPS Coverage Responses", menuName = "ARDK/VPS Coverage Responses", order = 0)]
  public class VpsCoverageResponses: ScriptableObject
  {
    public CoverageData Coverage;
    public LocalizationTargetsData LocalizationTargets;

    [Serializable]
    public struct CoverageData
    {
      public CoverageArea[] Areas;
      public ResponseStatus ResponseStatus;
      public string ErrorMessage;

      internal _CoverageAreasResponse ToResponse()
      {
        var areas = Areas.Select
        (
          a => new _CoverageAreasResponse.VpsCoverageArea
          (
            new _CoverageAreasResponse.Shape(a.Shape),
            a.LocalizationTargetIdentifiers,
            a.LocalizabilityQuality.ToString()
          )
        );

        return new _CoverageAreasResponse
        (
          areas.ToArray(),
          ResponseStatus.ToString(),
          ErrorMessage
        );
      }
    }

    [Serializable]
    public struct LocalizationTargetsData
    {
      public LocalizationTarget[] Targets;
      public ResponseStatus ResponseStatus;
      public string ErrorMessage;


      internal _LocalizationTargetsResponse ToResponse(string[] targetIdentifiers)
      {
        var filteredTargets = Targets.Where(t => targetIdentifiers.Contains(t.Identifier));

        var details = filteredTargets.Select
        (
          t => new _LocalizationTargetsResponse.VpsLocalizationTarget
          (
            new _LocalizationTargetsResponse.Shape(t.Center),
            t.Identifier,
            t.Name,
            t.ImageURL
          )
        );

        return new _LocalizationTargetsResponse
        (
          details.ToArray(),
          ResponseStatus.ToString(),
          ErrorMessage
        );
      }
    }
  }
}
