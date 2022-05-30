// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;

using Niantic.ARDK.AR;

namespace Niantic.ARDK.LocationService
{
  public class LocationServiceFactory
  {
    public static ILocationService Create()
    {
      return _Create(null);
    }

    public static ILocationService Create(RuntimeEnvironment env)
    {
      switch (env)
      {
        case RuntimeEnvironment.Default:
          return Create();

        case RuntimeEnvironment.LiveDevice:
#if !UNITY_EDITOR
          return new _UnityLocationService();
#else
          return null;
#endif

        case RuntimeEnvironment.Remote:
          return null;

        case RuntimeEnvironment.Mock:
          return new SpoofLocationService();

        default:
          throw new InvalidEnumArgumentException(nameof(env), (int)env, env.GetType());
      }
    }

    private static ILocationService _Create(IEnumerable<RuntimeEnvironment> envs = null)
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
        return _Create(ARSessionFactory._defaultBestMatches);

      throw new NotSupportedException("None of the provided envs are supported by this build.");
    }
  }
}
