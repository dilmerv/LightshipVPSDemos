// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public class WayspotAnchorStatusUpdate
  {
    /// The ID of the wayspot anchor
    public Guid ID { get; }

    /// The status code for thy wayspot anchor
    public WayspotAnchorStatusCode Code { get; }

    /// Creates the status for the wayspot anchor
    /// @param id The ID of the wayspot anchor
    /// @param code The status code for the wayspot anchor
    public WayspotAnchorStatusUpdate(Guid id, WayspotAnchorStatusCode code)
    {
      ID = id;
      Code = code;
    }
  }
}
