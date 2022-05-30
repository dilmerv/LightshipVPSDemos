// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal interface _IWayspotAnchorImplementation:
    IDisposable
  {
    /// Called when the VPS Localization State has changed
    event ArdkEventHandler<LocalizationStateUpdatedArgs> LocalizationStateUpdated;

    /// Called when the status for wayspot anchors has changed
    event ArdkEventHandler<WayspotAnchorStatusUpdatedArgs> WayspotAnchorStatusUpdated;

    /// Called when a new wayspot anchor has been created
    event ArdkEventHandler<WayspotAnchorsCreatedArgs> WayspotAnchorsCreated;

    /// Called when a wayspot anchor position/rotation has been updated
    event ArdkEventHandler<WayspotAnchorsResolvedArgs> WayspotAnchorsResolved;

    /// Starts VPS
    /// @param The configuration to start VPS with
    void StartVPS(IWayspotAnchorsConfiguration wayspotAnchorsConfiguration);

    /// Stops VPS
    void StopVPS();

    /// Creates new wayspot anchors
    /// @param localPoses The local poses used ot create the wayspot anchors
    Guid[] CreateWayspotAnchors(params Matrix4x4[] localPoses);

    /// Starts tracking the managed poses
    /// @param wayspotAnchors The wayspot anchors to track
    void StartResolvingWayspotAnchors(params IWayspotAnchor[] wayspotAnchors);

    /// Stops tracking wayspot anchors
    /// @param wayspotAnchors The wayspot anchors to stop tracking
    void StopResolvingWayspotAnchors(params IWayspotAnchor[] wayspotAnchors);
  }
}
