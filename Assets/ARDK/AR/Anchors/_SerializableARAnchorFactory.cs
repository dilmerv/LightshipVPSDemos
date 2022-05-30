// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.ReferenceImage;

namespace Niantic.ARDK.AR.Anchors
{
  internal static class _SerializableARAnchorFactory
  {
    internal static _SerializableARAnchor _AsSerializable(this IARAnchor source)
    {
      if (source == null)
        return null;

      var anchorType = source.AnchorType;
      switch (anchorType)
      {
        case AnchorType.Basic:
          return _AsSerializableBasic(source);

        case AnchorType.Image:
          return _AsSerializableImage((IARImageAnchor)source);

        case AnchorType.Plane:
          return _AsSerializablePlane((IARPlaneAnchor)source);
      }

      throw new ArgumentException("Unknown anchorType: " + anchorType);
    }

    internal static _SerializableARBasicAnchor _AsSerializableBasic(this IARAnchor source)
    {
      if (source is _SerializableARBasicAnchor possibleResult)
        return possibleResult;

      var result =
        new _SerializableARBasicAnchor
        (
          source.Transform,
          source.Identifier
        );

      return result;
    }

    internal static _SerializableARImageAnchor _AsSerializableImage(this IARImageAnchor source)
    {
      if (source is _SerializableARImageAnchor possibleResult)
        return possibleResult;

      var result =
        new _SerializableARImageAnchor
        (
          source.Transform,
          source.Identifier,
          source.ReferenceImage._AsSerializable()
        );

      return result;
    }

    internal static _SerializableARPlaneAnchor _AsSerializablePlane(this IARPlaneAnchor source)
    {
      if (source is _SerializableARPlaneAnchor possibleResult)
        return possibleResult;

      var result =
        new _SerializableARPlaneAnchor
        (
          source.Transform,
          source.Identifier,
          source.Alignment,
          source.Classification,
          source.ClassificationStatus,
          source.Center,
          source.Extent
        );

      return result;
    }
  }
}
