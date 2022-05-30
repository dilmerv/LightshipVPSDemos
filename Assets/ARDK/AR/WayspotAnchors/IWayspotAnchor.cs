// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public interface IWayspotAnchor:
    IDisposable
  {
    /// Called when the position/rotation of the wayspot anchor has been updated
    /// @note This is only surfaced automatically when using the WayspotAnchorService.
    /// It needs to be invoked in your application code when using the WayspotAnchorController.
    ArdkEventHandler<WayspotAnchorResolvedArgs> TrackingStateUpdated { get; set; }

    /// Gets the ID of the wayspot anchor
    Guid ID { get; }

    /// Gets the payload for the wayspot anchor
    WayspotAnchorPayload Payload { get; }

    /// Whether or not the wayspot anchor is currently being tracked
    bool Tracking { get; }
  }
}
