// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDK.AR.Anchors
{
  [Serializable]
  internal sealed class _SerializableARBasicAnchor:
    _SerializableARAnchor
  {
    public _SerializableARBasicAnchor
    (
      Matrix4x4 transform,
      Guid identifier
    ):
      base(transform, identifier)
    {
    }

    public override AnchorType AnchorType
    {
      get => AnchorType.Basic;
    }

    public override _SerializableARAnchor Copy()
    {
      return new _SerializableARBasicAnchor(Transform, Identifier);
    }
  }
}
