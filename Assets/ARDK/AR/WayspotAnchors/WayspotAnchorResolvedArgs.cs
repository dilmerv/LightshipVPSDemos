// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public class WayspotAnchorResolvedArgs: IArdkEventArgs
  {
    /// The ID of the wayspot anchor being resolved
    public Guid ID { get; }

    /// The new position of the wayspot anchor
    public Vector3 Position { get; }

    /// The new rotation of the wayspot anchor
    public Quaternion Rotation { get; }

    /// Creates the args for wayspot anchor resolutions
    /// param id The ID of the wayspot anchor being resolved
    /// param localPose The new local pose of the wayspot anchor being resolved
    internal WayspotAnchorResolvedArgs(Guid id, Matrix4x4 localPose)
    {
      ID = id;
      Position = localPose.ToPosition();
      Rotation = localPose.ToRotation();
    }
  }
}
