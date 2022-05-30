// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Threading.Tasks;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.VirtualStudio.VpsCoverage;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.VPSCoverage
{
  internal class _MockCoverageClient: ICoverageClient
  {
    private VpsCoverageResponses _responses;

    internal _MockCoverageClient(VpsCoverageResponses responses)
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(CoverageClientFactory));

      if (responses != null)
        _responses = responses;
      else
      {
#if UNITY_EDITOR
        var foundResponses = _AssetDatabaseExtension.FindAssets<VpsCoverageResponses>();
        if (foundResponses.Length == 0)
        {
          ARLog._Error("No instance of VpsCoverageResponses found in project to use for mock ICoverageClient.");
        }
        else
        {
          _responses = foundResponses[0];
        };
#else
        throw new ArgumentNullException(nameof(responses), "On a mobile device, a VpsCoverageResponses object must be provided when using a mock ICoverageClient.");
#endif
      }
    }

    public async Task<CoverageAreasResult> RequestCoverageAreasAsync(LocationInfo queryLocation, int queryRadius)
    {
      return await RequestCoverageAreasAsync(new LatLng(queryLocation), queryRadius);
    }

#pragma warning disable 1998
    public async Task<CoverageAreasResult> RequestCoverageAreasAsync(LatLng queryLocation, int queryRadius)
#pragma warning restore 1998
    {
      var mockResponse = _responses.Coverage.ToResponse();
      return new CoverageAreasResult(mockResponse);
    }

#pragma warning disable 1998
    public async Task<LocalizationTargetsResult> RequestLocalizationTargetsAsync(string[] targetIdentifiers)
#pragma warning restore 1998
    {
      var mockResponse = _responses.LocalizationTargets.ToResponse(targetIdentifiers);
      return new LocalizationTargetsResult(mockResponse);
    }

    public async void RequestCoverageAreas(LocationInfo queryLocation, int queryRadius, Action<CoverageAreasResult> onAreasReceived)
    {
      CoverageAreasResult result = await RequestCoverageAreasAsync(queryLocation, queryRadius);
      onAreasReceived?.Invoke(result);
    }

    public async void RequestCoverageAreas(LatLng queryLocation, int queryRadius, Action<CoverageAreasResult> onAreasReceived)
    {
      CoverageAreasResult result = await RequestCoverageAreasAsync(queryLocation, queryRadius);
      onAreasReceived?.Invoke(result);
    }

    public async void RequestLocalizationTargets(string[] targetIdentifiers, Action<LocalizationTargetsResult> onTargetsReceived)
    {
      LocalizationTargetsResult result = await RequestLocalizationTargetsAsync(targetIdentifiers);
      onTargetsReceived?.Invoke(result);
    }
  }
}