// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal sealed class _SerializableWayspotAnchorsConfiguration:
    IWayspotAnchorsConfiguration
  {
    public float LocalizationTimeout { get; set; }

    public float RequestTimeLimit { get; set; }

    public float RequestsPerSecond { get; set; }

    public float MaxResolutionsPerSecond { get; set; }

    public float GoodTrackingWait { get; set; }

    public bool ContinuousLocalizationEnabled { get; set; }

    public bool CloudProcessingForced { get; set; }

    public bool ClientProcessingForced { get; set; }

    public string ConfigURL { get; set; }

    public string HealthURL { get; set; }

    public string LocalizationURL { get; set; }

    public string GraphSyncURL { get; set; }

    public string WayspotAnchorCreateURL { get; set; }

    public string WayspotAnchorResolveURL { get; set; }

    public string RegisterNodeURL { get; set; }

    public string LookUpNodeURL { get; set; }

    void IDisposable.Dispose()
    {
      // Do nothing. This implementation of ILocalizationConfiguration is fully managed.
    }
  }
}
