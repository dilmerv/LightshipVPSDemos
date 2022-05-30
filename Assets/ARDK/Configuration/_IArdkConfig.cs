// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Networking;

namespace Niantic.ARDK.Configuration
{
  internal interface _IArdkConfig
  {
    /// Set the user id associated with the current user.
    bool SetUserIdOnLogin(string userId);
    
    bool SetDbowUrl(string url);

    string GetDbowUrl();

    string GetContextAwarenessUrl();

    // This field needs to be able to take in string.Empty since it is required for a lower level to 
    // setup the correct url
    bool SetContextAwarenessUrl(string url);

    bool SetApiKey(string key);

    string GetAuthenticationUrl();

    bool SetAuthenticationUrl(string url);

    NetworkingErrorCode VerifyApiKeyWithFeature(string feature, bool isAsync = true);
  }
}
