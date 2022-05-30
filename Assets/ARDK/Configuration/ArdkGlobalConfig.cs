// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using Niantic.ARDK.AR;
using Niantic.ARDK.Configuration.Internal;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Configuration
{
  /// Global configuration class.
  /// Allows developers to setup different configuration values during runtime.
  /// This exists such that in a live production environment, you can obtain the configuration
  /// settings remotely and set them before running the rest of the application.
  public static class ArdkGlobalConfig
  {
    internal const string _DBOW_URL = "https://bowvocab.eng.nianticlabs.com/dbow_b50_l3.bin";
    internal const string _DEFAULT_AUTH_URL = "https://us-central1-ar-dev-portal-prod.cloudfunctions.net/auth_token";
    
    static ArdkGlobalConfig()
    {
      var implementationType = _GetImplementationType();
      
      switch (implementationType)
      {
        case _ImplementationType.Native:
          _impl = new _NativeArdkConfig();
          ARLog._Debug("_NativeArdkConfigInternal");
          _Internal = new _NativeArdkConfigInternal();
          break;

        case _ImplementationType.Placeholder:
        default:
          _impl = new _PlaceholderArdkConfig();
          ARLog._Debug("_PlaceholderArdkConfigInternal");
          _Internal = new _PlaceholderArdkConfigInternal();
          break;
      }
    }

    private static _IArdkConfig _impl { get; }

    internal static _IArdkConfigInternal _Internal { get; }

    public static bool SetDbowUrl(string url)
    {
      return _impl.SetDbowUrl(url);
    }

    public static string GetDbowUrl()
    {
      return _impl.GetDbowUrl();
    }

    public static string GetContextAwarenessUrl()
    {
      return _impl.GetContextAwarenessUrl();
    }

    public static bool SetContextAwarenessUrl(string url)
    {
      return _impl.SetContextAwarenessUrl(url);
    }

    public static bool SetApiKey(string apiKey)
    {
      return _impl.SetApiKey(apiKey);
    }

    public static string GetAuthenticationUrl()
    {
      return _impl.GetAuthenticationUrl();
    }
    
    /// Returns the clientId - a unique identifier generated for the user/device
    /// in cases where a userId is not provided.
    public static string GetClientId()
    {
      return _Internal.GetClientId();
    }

    public static bool SetAuthenticationUrl(string url)
    {
      return _impl.SetAuthenticationUrl(url);
    }

    // returns the last good jwt token from API key validation
    internal static string GetJwtToken()
    {
      if (_impl is _NativeArdkConfig nativeConfig)
      {
        var token = nativeConfig.GetJwtToken();
        
        // if it is not populated, maybe no auth attempt has been made yet. make a blocking call here
        //  to attempt to get a token
        if (string.IsNullOrEmpty(token))
        {
          _VerifyApiKeyWithFeature("authentication", isAsync: false);

          token = nativeConfig.GetJwtToken();
        }

        return token;
      }

      return null;
    }
    
    /// Set the user id associated with the current user.
    /// We strongly recommend generating and using User IDs. Accurate user information allows
    ///  Niantic to support you in maintaining data privacy best practices and allows you to
    ///  understand usage patterns of features among your users. 
    /// ARDK has no strict format or length requirements for User IDs, although the User ID string
    ///  must be a UTF8 string. We recommend avoiding using an ID that maps back directly to the
    ///  user. So, for example, don’t use email addresses, or login IDs. Instead, you should
    ///  generate a unique ID for each user. We recommend generating a GUID.
    /// @param userId String containing the user id.
    /// @returns True if set properly, false if not
    public static bool SetUserIdOnLogin(string userId)
    {
      return _impl.SetUserIdOnLogin(userId);
    }

    /// Clear the user id set by |SetUserIdOnLogin|.
    /// @returns True if the user id is cleared properly, false if not
    public static bool ClearUserIdOnLogout()
    {
      return _impl.SetUserIdOnLogin("");
    }
 
    internal static NetworkingErrorCode _VerifyApiKeyWithFeature(string feature, bool isAsync = true)
    {
      return _impl.VerifyApiKeyWithFeature(feature, isAsync: isAsync);
    }
    
    private static bool IsM1Processor()
    {
      /*
       * https://developer.apple.com/documentation/apple-silicon/about-the-rosetta-translation-environment
       * From sysctl.proc_translated,
       * Intel/iPhone => -1
       * Just M1 => 0
       * M1 with Rosetta => 1
       */
      int value;
      var size = (IntPtr)4;
      var param = "sysctl.proc_translated";
      var result = sysctlbyname(param, out value, ref size, IntPtr.Zero, (IntPtr)0);

      return result >= 0;
    }

    private static _ImplementationType _GetImplementationType()
    {
      // Default to native unless you want to have more custom urls in which case, go for placeholder.
      // Use Placeholder also if you are running windows or a M1 mac.
      
      // Note: We can create a _NativeFeaturePreloader without setting the AccessMode to Native, 
      // and then the preloader will download from default URLs instead of ones set in the ArdkGlobalConfig.
      // There's currently no important use case where that's relevant though, so leaving the bug as known but unresolved.
      
      _ImplementationType typeToReturn = _ImplementationType.Native;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
      // For IOS and Android, we always use the native implementation.
      return typeToReturn;
#else  // UNITY_EDITOR
      var isNativeMode = NativeAccess.Mode == NativeAccess.ModeType.Native;
      if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        // Android/Linux
        if (!isNativeMode)
          typeToReturn = _ImplementationType.Placeholder;
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        var isIntelMac = !IsM1Processor();
        var isArchitectureX64 = RuntimeInformation.OSArchitecture == Architecture.X64;
        var isOSBigSurOrAbove = IsOperatingSystemBigSurAndAbove();
        var isTestingEnvironment = NativeAccess.Mode == NativeAccess.ModeType.Testing;

        if (!isArchitectureX64 ||
            !isIntelMac ||
            !isOSBigSurOrAbove ||
            isTestingEnvironment)
        {
          typeToReturn = _ImplementationType.Placeholder;;
        }
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        typeToReturn = _ImplementationType.Placeholder;
      }
      return typeToReturn;
#endif
    }
    
    private static bool IsOperatingSystemBigSurAndAbove()
    {
      // https://en.wikipedia.org/wiki/Darwin_%28operating_system%29#Release_history 
      // 20.0.0 Darwin is the first version of BigSur
      Version minimumDarwinBigSurVersion = new Version(20, 0, 0);
      
      return Environment.OSVersion.Version >= minimumDarwinBigSurVersion;
    }
    
    [DllImport("libSystem.dylib")]
    private static extern int sysctlbyname ([MarshalAs(UnmanagedType.LPStr)]string name, out int int_val, ref IntPtr length, IntPtr newp, IntPtr newlen);
    
    private enum _ImplementationType
    {
      Native = 0,
      Placeholder,
    }
  }
}
