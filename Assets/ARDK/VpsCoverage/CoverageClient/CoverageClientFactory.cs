// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Niantic.ARDK.VirtualStudio.VpsCoverage;

namespace Niantic.ARDK.VPSCoverage
{
  /// Factory to create CoverageClient instances.
  public static class CoverageClientFactory
  {
    /// Create an ICoverageLoader implementation appropriate for the current device.
    ///
    /// On a mobile device, the attempted order will be LiveDevice, Remote, and finally Mock.
    /// In the Unity Editor, the attempted order will be Remote, then Mock.
    ///
    /// @returns The created loader, or throws if it was not possible to create a loader.
    public static ICoverageClient Create()
    {
      return _Create(null);
    }

    /// Create an ICoverageLoader with the specified RuntimeEnvironment.
    ///
    /// @param env
    ///   The env used to create the loader for.
    /// @param mockResponses
    ///   A ScriptableObject containing the data that a Mock implementation of the ICoverageClient
    ///   will return. This is a required argument for using the mock client on a mobile
    ///   device. It is optional in the Unity Editor; the mock client will simply use the data
    ///   provided in the ARDK/VirtualStudio/VpsCoverage/VPS Coverage Responses.asset file.
    ///
    /// @returns The created loader, or null if it was not possible to create a loader.
    public static ICoverageClient Create(RuntimeEnvironment env, VpsCoverageResponses mockResponses = null)
    {
      ICoverageClient result;
      switch (env)
      {
        case RuntimeEnvironment.Default:
          return Create();

        case RuntimeEnvironment.LiveDevice:
          result = new _NativeCoverageClient();
          break;

        case RuntimeEnvironment.Remote:
          throw new NotSupportedException();

        case RuntimeEnvironment.Mock:
          result = new _MockCoverageClient(mockResponses);
          break;

        default:
          throw new InvalidEnumArgumentException(nameof(env), (int)env, env.GetType());
      }

      return result;
    }

    private static readonly RuntimeEnvironment[] _defaultBestMatches =
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
      new RuntimeEnvironment[] { RuntimeEnvironment.LiveDevice };
#else
      new RuntimeEnvironment[] { RuntimeEnvironment.Mock };
#endif

    /// Tries to create an ICoverageLoader implementation of any of the given envs.
    ///
    /// @param envs
    ///   A collection of runtime environments used to create the session for. As not all platforms
    ///   support all environments, the code will try to create the session for the first
    ///   environment, then for the second and so on. If envs is null or empty, then the order used
    ///   is LiveDevice, Remote and finally Mock.
    ///
    /// @returns The created loader, or null if it was not possible to create a loader.
    internal static ICoverageClient _Create(IEnumerable<RuntimeEnvironment> envs = null)
    {
      bool triedAtLeast1 = false;

      if (envs != null)
      {
        foreach (var env in envs)
        {
          var possibleResult = Create(env);
          if (possibleResult != null)
            return possibleResult;

          triedAtLeast1 = true;
        }
      }

      if (!triedAtLeast1)
        return _Create(_defaultBestMatches);

      throw new NotSupportedException("None of the provided envs are supported by this build.");
    }

  }
}
