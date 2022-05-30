// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;

using Niantic.ARDK.AR;
using Niantic.ARDK.VirtualStudio.Remote;

using UnityEngine;

namespace Niantic.ARDK.Rendering
{
  public static class ARFrameRendererFactory
  {
    // Recommended near and far clipping settings for AR rendering
    private const float DefaultNearClipping = 0.1f;
    private const float DefaultFarClipping = 100f;

    private static ARFrameRenderer _Create
    (
      RenderTarget target,
      IEnumerable<RuntimeEnvironment> envs,
      float nearClipping,
      float farClipping
    )
    {
      bool triedAtLeast1 = false;

      if (envs != null)
      {
        foreach (var env in envs)
        {
          var possibleResult = Create(target, env, nearClipping, farClipping);
          if (possibleResult != null)
            return possibleResult;

          triedAtLeast1 = true;
        }
      }

      if (!triedAtLeast1)
        return _Create(target, ARSessionFactory._defaultBestMatches, nearClipping, farClipping);

      throw new NotSupportedException("None of the provided envs are supported by this build.");
    }

    /// Create an ARFrameRenderer with the specified RuntimeEnvironment.
    /// @param env The runtime environment to create the renderer for.
    /// @returns The created renderer, or null if it was not possible to create a renderer.
    public static ARFrameRenderer Create
    (
      RenderTarget target,
      RuntimeEnvironment env,
      float nearClipping = DefaultNearClipping,
      float farClipping = DefaultFarClipping
    )
    {
      ARFrameRenderer result;
      switch (env)
      {
        case RuntimeEnvironment.Default:
          return _Create(target, null, nearClipping, farClipping);

        case RuntimeEnvironment.LiveDevice:
#pragma warning disable CS0162
          if (NativeAccess.Mode != NativeAccess.ModeType.Native && NativeAccess.Mode != NativeAccess.ModeType.Testing)
            return null;

#if UNITY_IOS
          result = new _ARKitFrameRenderer(target, nearClipping, farClipping);
#elif UNITY_ANDROID
          result = new _ARCoreFrameRenderer(target, nearClipping, farClipping);
#else
          return null;
#endif
          break;
#pragma warning restore CS0162

        case RuntimeEnvironment.Remote:
          if (!_RemoteConnection.IsEnabled)
            return null;

          result = new _RemoteFrameRenderer(target, nearClipping, farClipping);
          break;

        case RuntimeEnvironment.Mock:
          result = new _MockFrameRenderer(target, nearClipping, farClipping);
          break;

        default:
          throw new InvalidEnumArgumentException(nameof(env), (int)env, env.GetType());
      }

      return result;
    }

    internal static ARFrameRenderer _CreatePlayback
    (
      RenderTarget target,
      float nearClipping = DefaultNearClipping,
      float farClipping = DefaultFarClipping
    )
    {
      return new _ARPlaybackFrameRenderer(target,nearClipping, farClipping);
    }
  }
}
