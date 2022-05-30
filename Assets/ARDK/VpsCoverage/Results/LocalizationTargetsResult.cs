// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.VPSCoverage.GeoserviceMessages;

namespace Niantic.ARDK.VPSCoverage
{ 
  /// Received result from server request for LocalizationTargets.
  public class LocalizationTargetsResult
  {
    /// Response status of server request.
    public ResponseStatus Status { get; }
    
    /// Found LocalizationTargets found for the request as a dictionary with their identifier as keys.
    public IReadOnlyDictionary<string, LocalizationTarget> ActivationTargets { get; }

    internal LocalizationTargetsResult(_LocalizationTargetsResponse response)
    {
      Status = _ResponseStatusTranslator.FromString(response.status);
      var activationTargets = new Dictionary<string, LocalizationTarget>();
      foreach (var target in response.vps_localization_target)
      {
        activationTargets.Add(target.id, new LocalizationTarget(target));
      }

      ActivationTargets = activationTargets;
    }

    internal LocalizationTargetsResult(_HttpResponse<_LocalizationTargetsResponse> response)
    {
      Status = response.Status;

      if (Status == ResponseStatus.Success)
      {
        var activationTargets = new Dictionary<string, LocalizationTarget>();
        if (response.Data.vps_localization_target != null)
        {
          activationTargets = new Dictionary<string, LocalizationTarget>();
          foreach (var target in response.Data.vps_localization_target)
          {
            activationTargets.Add(target.id, new LocalizationTarget(target));
          }
        }
        
        ActivationTargets = activationTargets;
      }
    }
  }
}
