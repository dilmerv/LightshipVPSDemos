// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace Niantic.ARDK.AR.Anchors
{
  public static class AnchorExtensions
  {
    public static bool IsDisposed(this IARAnchor anchor)
    {
      return anchor.Identifier.Equals(Guid.Empty);
    }

    internal static _SerializableAnchorsByType ClassifyAsSerializableAnchors(IEnumerable<IARAnchor> anchors)
    {
      var basicAnchors = new List<_SerializableARAnchor>();
      var planeAnchors = new List<_SerializableARPlaneAnchor>();
      var imageAnchors = new List<_SerializableARImageAnchor>();
      foreach (var anchor in anchors)
      {
        switch (anchor.AnchorType)
        {
          case AnchorType.Basic:
            basicAnchors.Add(anchor._AsSerializableBasic());
            break;

          case AnchorType.Plane:
            planeAnchors.Add(((IARPlaneAnchor)anchor)._AsSerializablePlane());
            break;

          case AnchorType.Image:
            imageAnchors.Add(((IARImageAnchor)anchor)._AsSerializableImage());
            break;

          default:
            break;
        }
      }

      return new _SerializableAnchorsByType
      (
        basicAnchors, 
        planeAnchors, 
        imageAnchors
      );
    }
  }
}
