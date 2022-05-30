// Copyright 2022 Niantic, Inc. All Rights Reserved.
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public class WayspotAnchorsResolvedArgs: IArdkEventArgs
  {
    /// The resolutions for the wayspot anchors
    public WayspotAnchorResolvedArgs[] Resolutions { get; }

    /// Creates the args for resolved wayspot anchors
    /// @param resolutions The resolutions of the wayspot anchors
    internal WayspotAnchorsResolvedArgs(WayspotAnchorResolvedArgs[] resolutions)
    {
      Resolutions = resolutions;
    }
  }
}
