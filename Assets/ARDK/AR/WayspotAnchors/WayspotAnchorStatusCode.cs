// Copyright 2022 Niantic, Inc. All Rights Reserved.
namespace Niantic.ARDK.AR.WayspotAnchors
{
  /// The enum for the wayspot anchor status codes
  public enum WayspotAnchorStatusCode
  {
    // System is not ready yet to create/resolve anchor
    Pending = 0,
    // Anchor creation or resolution was successful using VPS
    Success = 1,
    // Anchor creation or resolution failed
    Failed = 2,
    // Anchor data is invalid
    Invalid = 3,
    // Anchor creation or resolution was successful but using GPS instead of VPS
    Limited = 4
  }
}
