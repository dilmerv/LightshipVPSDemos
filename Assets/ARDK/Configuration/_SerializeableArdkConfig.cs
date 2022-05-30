// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Configuration
{
  internal sealed class _SerializeableArdkConfig:
    _IArdkConfig
  {
    private string _dbowUrl;
    private string _apiKey;
    private string _contextAwarenessUrl = "";
    private string _authenticationUrl = "";
    internal static string _userId;

    public _SerializeableArdkConfig()
    {
      ARLog._Debug($"Using config: {nameof(_SerializeableArdkConfig)}");
    }

    public bool SetUserIdOnLogin(string userId)
    {
      _userId = userId;
      return true;
    }

    public bool SetDbowUrl(string url)
    {
      _dbowUrl = url;

      return true;
    }

    public string GetDbowUrl()
    {
      return _dbowUrl;
    }

    public bool SetContextAwarenessUrl(string url)
    {
      _contextAwarenessUrl = url;

      return true;
    }

    public string GetContextAwarenessUrl()
    {
      return _contextAwarenessUrl;
    }

    public bool SetApiKey(string apiKey)
    {
      _apiKey = apiKey;
      return true;
    }

    public string GetAuthenticationUrl()
    {
      return _authenticationUrl;
    }

    public bool SetAuthenticationUrl(string url)
    {
      _authenticationUrl = url;
      return true;
    }

    public NetworkingErrorCode VerifyApiKeyWithFeature(string feature, bool isAsync)
    {
      if (String.IsNullOrEmpty(_apiKey))
        return NetworkingErrorCode.ApiKeyNotSet;

      return NetworkingErrorCode.Ok;
    }
  }
}
