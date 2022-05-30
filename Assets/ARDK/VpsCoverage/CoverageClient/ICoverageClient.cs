// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Threading.Tasks;

using Niantic.ARDK.LocationService;

namespace Niantic.ARDK.VPSCoverage
{
  /// Client to request CoverageAreas and LocalizationTargets. 
  public interface ICoverageClient
  {
    /// Request CoverageAreas at device location within a radius using the async await pattern.
    /// @param queryLocation Center of query from device location.
    /// @param queryRadius Radius for query between 0m and 500m. Negative radius will default to the maximum radius of 500m.
    /// @returns Task with the received CoverageAreasResult as result. 
    Task<CoverageAreasResult> RequestCoverageAreasAsync(LocationInfo queryLocation, int queryRadius);
    
    /// Request CoverageAreas at any location within a radius using the async await pattern.
    /// @param queryLocation Center of query.
    /// @param queryRadius Radius for query between 0m and 500m. Negative radius will default to the maximum radius of 500m.
    /// @returns Task with the received CoverageAreasResult as result. 
    Task<CoverageAreasResult> RequestCoverageAreasAsync(LatLng queryLocation, int queryRadius);

    /// Request LocalizationTargets for a set of identifiers using the async await pattern.
    /// @param targetIdentifiers Set of unique identifiers of the requested targets.
    /// @returns Task with the received LocalizationTargetsResult as result. 
    Task<LocalizationTargetsResult> RequestLocalizationTargetsAsync(string[] targetIdentifiers);
    
    /// Request CoverageAreas at device location within a radius using the callback pattern.
    /// @param queryLocation Center of query from device location.
    /// @param queryRadius Radius for query between 0m and 500m. Negative radius will default to the maximum radius of 500m.
    /// @param onAreasReceived Callback function to process the received CoverageAreasResult.
    void RequestCoverageAreas(LocationInfo queryLocation, int queryRadius, Action<CoverageAreasResult> onAreasReceived);
    
    /// Request CoverageAreas at device location within a radius using the callback pattern.
    /// @param queryLocation Center of query.
    /// @param queryRadius Radius for query between 0m and 500m. Negative radius will default to the maximum radius of 500m.
    /// @param onAreasReceived Callback function to process the received CoverageAreasResult.
    void RequestCoverageAreas(LatLng queryLocation, int queryRadius, Action<CoverageAreasResult> onAreasReceived);

    /// Request LocalizationTargets for a set of identifiers using the callback pattern.
    /// @param targetIdentifiers Set of unique identifiers of the requested targets.
    /// @param onTargetsReceived Callback function to process the received LocalizationTargetsResult.
    void RequestLocalizationTargets(string[] targetIdentifiers, Action<LocalizationTargetsResult> onTargetsReceived);
  }
}
