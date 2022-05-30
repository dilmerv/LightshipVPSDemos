// Copyright 2022 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif
#if (UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_DESKTOP) && !UNITY_EDITOR
#define AR_NATIVE_SUPPORT
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Niantic.ARDK.Configuration.Authentication;

using Niantic.ARDK.Configuration;
using Niantic.ARDK.Configuration.Internal;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace Niantic.ARDK.Internals
{
  /// Controls the startup systems for ARDK.
  public static class StartupSystems
  {
    // Add a destructor to this class to try and catch editor reloads
    private static readonly _Destructor Finalise = new _Destructor();
    
    // The pointer to the C++ NarSystemBase handling functionality at the native level
    private static IntPtr _nativeHandle = IntPtr.Zero;

    private const string FileDisablingSuffix = ".DISABLED";

#if UNITY_EDITOR_OSX
    [InitializeOnLoadMethod]
    private static void EditorStartup()
    {
      EnforceRosettaBasedCompatibility();
      
#if !REQUIRE_MANUAL_STARTUP
      ManualStartup();
#endif
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Startup()
    {
#if AR_NATIVE_SUPPORT
#if !REQUIRE_MANUAL_STARTUP
      ManualStartup();
#endif
#endif
    }

    /// <summary>
    /// Starts up the ARDK startup systems if they haven't been started yet.
    /// </summary>
    public static void ManualStartup()
    {
#if (AR_NATIVE_SUPPORT || UNITY_EDITOR_OSX)
      try
      {
        // TODO(sxian): Remove the _ROR_CREATE_STARTUP_SYSTEMS() after moving the functionalities to
        // NARSystemBase class.
        // Note, don't put any code before calling _NARSystemBase_Initialize() below, since Narwhal C++
        // _NARSystemBase_Initialize() should be the first API to be called before other components are initialized.
        _ROR_CREATE_STARTUP_SYSTEMS();
      }
      catch (DllNotFoundException e)
      {
        ARLog._DebugFormat("Failed to create ARDK startup systems: {0}", false, e);
      }

      if (_nativeHandle == IntPtr.Zero) {
        _nativeHandle = _InitialiseNarBaseSystemBasedOnOS();
        _CallbackQueue.ApplicationWillQuit += OnApplicationQuit;
      } else {
        ARLog._Error("_nativeHandle is not null, ManualStartup is called twice");
      }

      // The initialization of C# components should happen below.
      SetAuthenticationParameters();
      SetDeviceMetadata();
#endif
    }

    private static void OnApplicationQuit()
    {
#if (AR_NATIVE_SUPPORT || UNITY_EDITOR_OSX)
      if (_nativeHandle != IntPtr.Zero)
      {
        _NARSystemBase_Release(_nativeHandle);
        _nativeHandle = IntPtr.Zero;
      }
#endif
    }

    private const string AUTH_DOCS_MSG =
      "For more information, visit the niantic.dev/docs/authentication.html site.";

    private static void SetAuthenticationParameters()
    {
      // We always try to find an api key
      var apiKey = "";
      var authConfigs = Resources.LoadAll<ArdkAuthConfig>("ARDK/ArdkAuthConfig");

      if (authConfigs.Length > 1)
      {
        var errorMessage = "There are multiple ArdkAuthConfigs in Resources/ARDK/ " +
                           "directories, loading the first API key found. Remove extra" +
                           " ArdkAuthConfigs to prevent API key problems. " + AUTH_DOCS_MSG;
        ARLog._Error(errorMessage);
      }
      else if (authConfigs.Length == 0)
      {
        ARLog._Error
        (
          "Could not load an ArdkAuthConfig, please add one in a Resources/ARDK/ directory. " +
          AUTH_DOCS_MSG
        );
      }
      else
      {
        var authConfig = authConfigs[0];
        apiKey = authConfig.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
          ArdkGlobalConfig.SetApiKey(apiKey);
      }

      authConfigs = null;
      Resources.UnloadUnusedAssets();

      //Only continue if needed
      if (!ServerConfiguration.AuthRequired)
        return;

      if (string.IsNullOrEmpty(ServerConfiguration.ApiKey))
      {

        if (!string.IsNullOrEmpty(apiKey))
        {
          ServerConfiguration.ApiKey = apiKey;
        }
        else
        {
          ARLog._ErrorFormat
          (
            "No API Key was found. Add it to the {0} file. {1}",
#if UNITY_EDITOR
            AssetDatabase.GetAssetPath(authConfigs[0]),
#else
            "Resources/ARDK/ArdkAuthConfig.asset",
#endif
            AUTH_DOCS_MSG
          );
        }
      }

      var authUrl = ArdkGlobalConfig.GetAuthenticationUrl();
      if (string.IsNullOrEmpty(authUrl))
      {
        ArdkGlobalConfig.SetAuthenticationUrl(ArdkGlobalConfig._DEFAULT_AUTH_URL);
        authUrl = ArdkGlobalConfig.GetAuthenticationUrl();
      }

      ServerConfiguration.AuthenticationUrl = authUrl;

#if UNITY_EDITOR
      if (!string.IsNullOrEmpty(apiKey))
      {
        var authResult = ArdkGlobalConfig._VerifyApiKeyWithFeature("feature:unity_editor", isAsync: false);
        if(authResult == NetworkingErrorCode.Ok)
          ARLog._Debug("Successfully authenticated ARDK Api Key");
        else
        {
          ARLog._Error("Attempted to authenticate ARDK Api Key, but got error: " + authResult);
        }
      }
#endif

      var installMode = Application.installMode;
      var installModeString = string.Format("install_mode:{0}", installMode.ToString());
      if (!string.IsNullOrEmpty(apiKey))
        ArdkGlobalConfig._VerifyApiKeyWithFeature(installModeString, isAsync: true);
    }

    private static void SetDeviceMetadata()
    {
      ArdkGlobalConfig._Internal.SetApplicationId(Application.identifier);

      var guid = Guid.NewGuid();
      // Formats as a hex string without "0x" (ie: 0123456789ABCDEF0123456789ABCDEF)
      var guidAsHexString = $"{guid.ToString("N").ToUpper()}";
      ArdkGlobalConfig._Internal.SetArdkInstanceId(guidAsHexString);
    }

    // TODO(bpeake): Find a way to shutdown gracefully and add shutdown here.

    private static IntPtr _InitialiseNarBaseSystemBasedOnOS()
    {
      // prioritise android and ios to always initialise base nar system
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
      return _InitialiseNarSystem();
#else
      // Macbooks which are M1 processors or are Catalina and below need to use IntPtr.Zero right now

      bool hasM1ProcessorOrHasOSBelowBigSur = _IsM1Processor() || !_IsOperatingSystemBigSurAndAbove();
      bool isMacNotCompatibleForNative = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && hasM1ProcessorOrHasOSBelowBigSur;
      
      if (isMacNotCompatibleForNative || 
          RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        // return 0 as native handler for windows, m1 macbooks and macbooks below BigSur
        return IntPtr.Zero;
      }
      
      // for everything else initialise nar base system
      return _InitialiseNarSystem();
#endif
    }

    private static IntPtr _InitialiseNarSystem()
    {
#if (AR_NATIVE_SUPPORT || UNITY_EDITOR_OSX)
      return _NARSystemBase_Initialize();
#else
      return IntPtr.Zero;
#endif
    }
    
    private static readonly Dictionary<string, string> _rosettaFiles = new Dictionary<string, string>()
    {
      {"mcs", "/ARDK/mcs.rsp"},
      {"csc", "/ARDK/csc.rsp"},
    };
    private static bool _rosettaCompatibilityCheckPerformed = false;

    private static void EnforceRosettaBasedCompatibility()
    {
      if (_rosettaCompatibilityCheckPerformed)
        return;
      
#if UNITY_EDITOR
      if (_IsUsingRosetta())
      {
        _EnableRosettaFiles();
      }
      else
      {
        _DisableRosettaFiles();
      }
#endif 
      _rosettaCompatibilityCheckPerformed = true;
    }
    
#if UNITY_EDITOR
    private static void _EnableRosettaFiles()
    {
      ARLog._Debug("Enabling the files for rosetta compatibility");
      
      foreach (var rosettaFile in _rosettaFiles)
      {
        var disabledFileName = rosettaFile.Value + FileDisablingSuffix;
        var absolutePath = _GetPathForFile(rosettaFile.Key, disabledFileName);

        if (!string.IsNullOrWhiteSpace(absolutePath))
          _EnableFileWithRename(absolutePath);
      }
    }

    private static void _DisableRosettaFiles()
    {
      ARLog._Debug("Disabling the files for rosetta compatibility");

      foreach (var rosettaFile in _rosettaFiles)
      {
        var absolutePath = _GetPathForFile(rosettaFile.Key, rosettaFile.Value);

        if (!string.IsNullOrWhiteSpace(absolutePath))
          _DisableFileWithRename(absolutePath);
      }
    }

    private static void _EnableFileWithRename(string sourcePath)
    {
      string newPath = sourcePath.Substring(0, sourcePath.Length - FileDisablingSuffix.Length);
      
      if (File.Exists(newPath))
      {
        ARLog._Debug($"File with name {newPath} already exists. So cleaning it up");
        File.Delete(newPath);
      }

      File.Move(sourcePath, newPath);
      
      RemoveMetaFile(sourcePath);
    }
    
    private static void _DisableFileWithRename(string sourcePath)
    {
      string newPath = sourcePath + FileDisablingSuffix;

      if (File.Exists(newPath))
      {
        ARLog._Debug($"File with name {newPath} already exists. So cleaning it up");
        File.Delete(newPath);
      }

      File.Move(sourcePath, newPath);
      
      RemoveMetaFile(sourcePath);
    }

    private static void RemoveMetaFile(string sourcePath)
    {
      string metaFilePath = sourcePath + ".meta";

      if(File.Exists(metaFilePath))
        File.Delete(metaFilePath);
    }

    private static string _GetPathForFile(string searchString, string pathInArdk)
    {
      var possibleAssetsGuids = AssetDatabase.FindAssets(searchString);
      foreach (var possibleAssetGuid in possibleAssetsGuids)
      {
        var path = AssetDatabase.GUIDToAssetPath(possibleAssetGuid);

        // sanity check
        if (path.Length < pathInArdk.Length)
          continue;
        
        // Get the last characters to compare with the pathInArdk string
        var finalSubstring = path.Substring(path.Length - pathInArdk.Length);
        if (finalSubstring.Equals(pathInArdk))
        {
          return path;
        }
      }

      return null;
    }

#endif
    
    private sealed class _Destructor
    {
      ~_Destructor()
      {
        OnApplicationQuit();
      }
    }

#if (AR_NATIVE_SUPPORT || UNITY_EDITOR_OSX)
    
    // TODO AR-10581 Consolidate OS branching logic from here and from ArdkGlobalConfig
    private static bool _IsOperatingSystemBigSurAndAbove()
    {
      // https://en.wikipedia.org/wiki/Darwin_%28operating_system%29#Release_history 
      // 20.0.0 Darwin is the first version of BigSur
      return Environment.OSVersion.Version >= new Version(20, 0, 0);
    }
    
    private static bool _IsUsingRosetta()
    {
      return 
        _IsM1Processor() && 
        RuntimeInformation.ProcessArchitecture == Architecture.X64;
    }
    
    private static bool _IsM1Processor()
    {
      /*
       * https://developer.apple.com/documentation/apple-silicon/about-the-rosetta-translation-environment
       * From sysctl.proc_translated,
       * Intel/iPhone => -1
       * M1 => 0
       */
      int _;
      var size = (IntPtr)4;
      var param = "sysctl.proc_translated";
      var result = sysctlbyname(param, out _, ref size, IntPtr.Zero, (IntPtr)0);

      return result >= 0;
    }
    
    [DllImport("libSystem.dylib")]
    private static extern int sysctlbyname ([MarshalAs(UnmanagedType.LPStr)]string name, out int int_val, ref IntPtr length, IntPtr newp, IntPtr newlen);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _ROR_CREATE_STARTUP_SYSTEMS();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARSystemBase_Initialize();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARSystemBase_Release(IntPtr nativeHandle);
#else

    private static bool _IsUsingRosetta()
    {
      return false;
    }

    private static bool _IsM1Processor()
    {
      return false;
    }

    private static bool _IsOperatingSystemBigSurAndAbove()
    {
      return false;
    }
    
#endif
  }
}
