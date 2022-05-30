// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Runtime.InteropServices;

using Google.Protobuf;

using Niantic.ARDK.AR.Protobuf;

using UnityEngine;

namespace Niantic.ARDK.Configuration.Internal
{
  internal sealed class _PlaceholderArdkConfigInternal :
    _IArdkConfigInternal
  {
    private string _appId;
    public void SetApplicationId(string bundleId)
    {
      _appId = bundleId;
    }
    public string GetApplicationId()
    {
      return _appId;
    }

    private string _ardkInstanceId;
    public void SetArdkInstanceId(string instanceId)
    {
      _ardkInstanceId = instanceId;
    }
    public string GetArdkAppInstanceId()
    {
      return _ardkInstanceId;
    }

    public string GetPlatform()
    {
      return Application.unityVersion;
    }

    public string GetManufacturer()
    {
      return null;
    }

    public string GetDeviceModel()
    {
      return SystemInfo.operatingSystem;
    }

    public string GetArdkVersion()
    {
      // This doesn't work without the native plugin :(
      return null;
    }

    public string GetUserId()
    {
      return _PlaceholderArdkConfig._userId;
    }

    public string GetClientId()
    {
      return null;
    }

    public string GetApiKey()
    {
      return _PlaceholderArdkConfig._apiKey;
    }
  }
}
