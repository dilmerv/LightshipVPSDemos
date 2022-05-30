// Copyright 2022 Niantic, Inc. All Rights Reserved.
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public class WayspotAnchorsCreatedArgs: IArdkEventArgs
  {
    /// The newly created wayspot anchors
    public IWayspotAnchor[] WayspotAnchors { get; }

    /// Creates the args for newly created wayspot anchors
    /// @param wayspotAnchors The newly created wayspot anchors
    internal WayspotAnchorsCreatedArgs(IWayspotAnchor[] wayspotAnchors)
    {
      WayspotAnchors = wayspotAnchors;
    }
  }
}
