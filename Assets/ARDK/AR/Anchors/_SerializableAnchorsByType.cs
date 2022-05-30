// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

namespace Niantic.ARDK.AR.Anchors
{
  internal class _SerializableAnchorsByType
  {
    public _SerializableAnchorsByType(List<_SerializableARAnchor> basicAnchors, List<_SerializableARPlaneAnchor> planeAnchors, List<_SerializableARImageAnchor> imageAnchors)
    {
      BasicAnchors = basicAnchors;
      PlaneAnchors = planeAnchors;
      ImageAnchors = imageAnchors;
    }
    
    public List<_SerializableARAnchor> BasicAnchors { get; }
    public List<_SerializableARPlaneAnchor> PlaneAnchors { get; }
    public List<_SerializableARImageAnchor> ImageAnchors { get; }
  }
}
