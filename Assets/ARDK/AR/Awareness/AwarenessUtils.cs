// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  /// A collection of utility methods for working with contextual awareness buffers.
  public static class AwarenessUtils
  {
    /// Converts pixel coordinates from the raw awareness buffer's
    /// coordinate frame to viewport pixel coordinates.
    /// @param processor Reference to the context awareness processor.
    /// @param x Awareness buffer pixel position on the x axis.
    /// @param y Awareness buffer pixel position on the y axis.
    /// @returns Pixel coordinates on the viewport.
    public static Vector2Int FromBufferToScreenPosition<TBuffer>
    (
      AwarenessBufferProcessor<TBuffer> processor,
      int x,
      int y
    ) where TBuffer : class, IDisposable, IAwarenessBuffer
    {
      // Acquire the buffer resolution
      var buffer = processor.AwarenessBuffer;
      var bufferWidth = buffer.Width;
      var bufferHeight = buffer.Height;

      // Acquire the viewport resolution
      var viewport = processor.CurrentViewportResolution;
      var viewWidth = viewport.x;
      var viewHeight = viewport.y;

      // The sampler transform takes from viewport to buffer,
      // so we need to invert it to go the other way around
      var transform = processor.SamplerTransform.inverse;

      // Get normalized buffer coordinates
      var uv = new Vector4
      (
        Mathf.Clamp((float)x / bufferWidth, 0.0f, 1.0f),
        Mathf.Clamp((float)y / bufferHeight, 0.0f, 1.0f),
        1.0f,
        1.0f
      );

      // Apply transform
      var st = transform * uv;
      var sx = st.x / st.z;
      var sy = st.y / st.z;

      // Scale result to viewport
      return new Vector2Int
      (
        x: Mathf.Clamp(Mathf.RoundToInt(sx * viewWidth - 0.5f), 0, viewWidth - 1),
        y: Mathf.Clamp(Mathf.RoundToInt(sy * viewHeight - 0.5f), 0, viewHeight - 1)
      );
    }
  }
}
