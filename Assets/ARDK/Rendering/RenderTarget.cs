// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.Rendering
{
  /// A render target can either be a camera or an offscreen texture.
  public readonly struct RenderTarget: 
    IEquatable<RenderTarget>
  {
    /// The actual camera as a render target, if any.
    public readonly Camera Camera;

    /// The actual GPU texture as a render target, if any.
    public readonly RenderTexture RenderTexture;

    /// The identifier of this render target.
    public readonly RenderTargetIdentifier Identifier;

    // Whether the target is a Unity camera
    public readonly bool IsTargetingCamera;
    
    // Whether the target is a RenderTexture
    public readonly bool IsTargetingTexture;

    /// Creates a render target from the specified camera.
    public RenderTarget(Camera cam)
    {
      Camera = cam;
      IsTargetingCamera = true;

      RenderTexture = null;
      IsTargetingTexture = false;

      Identifier = Camera.targetTexture == null
        ? BuiltinRenderTextureType.CurrentActive  // TODO: what if this is a secondary camera?
        : BuiltinRenderTextureType.CameraTarget;
    }

    /// Creates a render target from the specified texture.
    public RenderTarget(RenderTexture texture)
    {
      Camera = null;
      IsTargetingCamera = false;

      RenderTexture = texture;
      IsTargetingTexture = true;

      Identifier = new RenderTargetIdentifier(texture);
    }

    /// Returns a the resolution of the target, in the function of
    /// the specified screen orientation.
    public Resolution GetResolution(ScreenOrientation forOrientation)
    {
      int longer, shorter;

      if (IsTargetingCamera)
      {
        longer = Camera.pixelWidth > Camera.pixelHeight
          ? Camera.pixelWidth
          : Camera.pixelHeight;

        shorter = Camera.pixelWidth < Camera.pixelHeight
          ? Camera.pixelWidth
          : Camera.pixelHeight;
      }
      else
      {
        longer = RenderTexture.width > RenderTexture.height
          ? RenderTexture.width
          : RenderTexture.height;

        shorter = RenderTexture.width < RenderTexture.height
          ? RenderTexture.width
          : RenderTexture.height;
      }

      var needsLandscape = forOrientation == ScreenOrientation.LandscapeLeft ||
        forOrientation == ScreenOrientation.LandscapeRight;

      return needsLandscape
        // Landscape
        ? new Resolution { width = longer, height = shorter }
        // Portrait
        : new Resolution { width = shorter, height = longer };
    }
    
    public static implicit operator RenderTarget(Camera cam)
    {
      return new RenderTarget(cam);
    }

    public static implicit operator RenderTarget(RenderTexture texture)
    {
      return new RenderTarget(texture);
    }
    
    public bool Equals(RenderTarget other)
    {
      return Identifier.Equals(other.Identifier);
    }

    public override bool Equals(object obj)
    {
      return obj is RenderTarget other && Equals(other);
    }

    public override int GetHashCode()
    {
      return Identifier.GetHashCode();
    }
    
    /// Returns the current screen orientation. When called in the editor,
    /// this property infers the orientation from the screen resolution.
    public static ScreenOrientation ScreenOrientation
    {
      get
      {
#if UNITY_EDITOR
        return Screen.width > Screen.height
          ? UnityEngine.ScreenOrientation.Landscape
          : UnityEngine.ScreenOrientation.Portrait;
#else
        return Screen.orientation;
#endif
      }
    }
  }
}
