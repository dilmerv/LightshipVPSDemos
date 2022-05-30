// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  /// Configuration information for a VPS localization attempt.
  public interface IWayspotAnchorsConfiguration:
    IDisposable
  {
    /// The timeout in seconds for the entire localization attempt. An attempt will send
    /// localization requests until the localization succeeds, times out, or is canceled.
    /// A value of -1 indicates no timeout.
    /// The default is 30 seconds.
    float LocalizationTimeout { get; set; }

    /// The timeout in seconds for an individual request made during the overall localization attempt.
    /// A timeout of -1 indicates no timeout
    /// The default is 10 seconds.
    float RequestTimeLimit { get; set; }

    /// The max number of network requests per second 
    /// The default and maximum value is 1.0
    float RequestsPerSecond { get; set; }

    /// The max number of wayspot anchor resolutions  per second 
    /// A value of -1 indicates no maximun (Resolve as frequent as possible).
    /// The default is 1.
    float MaxResolutionsPerSecond { get; set; }

    /// The number of seconds that the system is required to wait after entering a good tracking state before running 
    /// The default value is 3.0
    float GoodTrackingWait { get; set; }

    /// The max number of wayspot anchor resolutions  per second 
    /// The default value is false
    bool ContinuousLocalizationEnabled { get; set; }

    // The following configurations are for internal testing and debugging. 
    // Do not change the default values.

    /// Override option internal testing. You probably shouldn't touch this
    bool CloudProcessingForced { get; set; }

    /// Override option internal testing. You probably shouldn't touch this
    bool ClientProcessingForced { get; set; }

    /// The endpoint for VPS config API requests. You probably shouldn't touch this
    string ConfigURL { get; set; }

    /// The endpoint for VPS health API requests. You probably shouldn't touch this
    string HealthURL { get; set; }

    /// The endpoint for VPS localization API requests. You probably shouldn't touch this
    string LocalizationURL { get; set; }

    /// The endpoint for VPS graph sync API requests. You probably shouldn't touch this
    string GraphSyncURL { get; set; }

    /// The endpoint for VPS anchor creation API requests. You probably shouldn't touch this
    string WayspotAnchorCreateURL { get; set; }

    /// The endpoint for VPS anchor resolution API requests. You probably shouldn't touch this
    string WayspotAnchorResolveURL { get; set; }

    /// The endpoint for VPS node registration API requests. You probably shouldn't touch this
    string RegisterNodeURL { get; set; }

    /// The endpoint for VPS node lookup API requests. You probably shouldn't touch this
    string LookUpNodeURL { get; set; }
  }
}
