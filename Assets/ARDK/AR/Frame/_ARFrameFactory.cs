// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Linq;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Image;
using Niantic.ARDK.AR.LightEstimate;
using Niantic.ARDK.AR.PointCloud;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Utilities.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.Frame
{
  internal static class _ARFrameFactory
  {
    internal static _SerializableARFrame _AsSerializable
    (
      this IARFrame source, 
      bool includeImageBuffers = true, 
      bool includeAwarenessBuffers = true,
      int compressionLevel = 70, 
      bool includeFeaturePoints = false
    )
    {
      if (source == null)
        return null;

      if (source is _SerializableARFrame possibleResult)
        return possibleResult;

      var serializedFrame = NewSerializableFrameWithoutBuffers(source);
      
      if (includeImageBuffers)
      {
        var imageBuffer = source.CapturedImageBuffer;
        if (imageBuffer != null)
          serializedFrame.CapturedImageBuffer = imageBuffer._AsSerializable(compressionLevel);
      }

      if (includeAwarenessBuffers)
      {
        IDepthBuffer depthBuffer = source.Depth;
        if (depthBuffer != null)
          serializedFrame.DepthBuffer = depthBuffer._AsSerializable();

        ISemanticBuffer semanticBuffer = source.Semantics;
        if (semanticBuffer != null)
          serializedFrame.SemanticBuffer = semanticBuffer._AsSerializable();
      }

      if (includeFeaturePoints)
        serializedFrame.RawFeaturePoints = source.RawFeaturePoints._AsSerializable();

      return serializedFrame;
    }

    private static _SerializableARFrame NewSerializableFrameWithoutBuffers(IARFrame source)
    {
      var serializedAnchors =
      (
        from anchor in source.Anchors
        select anchor._AsSerializable()
      ).ToArray();

      var estimatedDisplayTransform =
        source.CalculateDisplayTransform
        (
          Screen.orientation,
          Screen.width,
          Screen.height
        );

      var serializableMaps =
      (
        from map in source.Maps
        select map._AsSerializable()
      ).ToArray();

      return
        new _SerializableARFrame
        (
          capturedImageBuffer: null,
          depthBuffer: null,
          semanticBuffer: null,
          source.Camera._AsSerializable(),
          source.LightEstimate._AsSerializable(),
          serializedAnchors.AsNonNullReadOnly<IARAnchor>(),
          serializableMaps,
          source.WorldScale,
          estimatedDisplayTransform
        );
    }
  }
}
