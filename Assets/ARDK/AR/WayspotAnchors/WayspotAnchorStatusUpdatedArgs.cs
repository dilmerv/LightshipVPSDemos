// Copyright 2022 Niantic, Inc. All Rights Reserved.
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public class WayspotAnchorStatusUpdatedArgs: IArdkEventArgs
  {
    /// The statuses of waypoint anchors
    public WayspotAnchorStatusUpdate[] WayspotAnchorStatusUpdates { get; }

    /// Creates the args for waypoint anchor statuses
    /// @param wayspotAnchorStatusUpdates The statuses for the waypoint anchors
    internal WayspotAnchorStatusUpdatedArgs(WayspotAnchorStatusUpdate[] wayspotAnchorStatusUpdates)
    {
      WayspotAnchorStatusUpdates = wayspotAnchorStatusUpdates;
    }
  }
}
