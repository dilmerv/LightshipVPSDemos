// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Runtime.InteropServices;
using System.Text;

using Google.Protobuf;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities.VersionUtilities;

using UnityEngine;

namespace Niantic.ARDK.Configuration.Internal
{
  internal sealed class _NativeArdkConfigInternal : 
    _IArdkConfigInternal
  {
    // Keep this synchronized with ardk_global_config_helper.hpp
    private enum _ConfigDataField : uint
    {
      ApplicationId = 1,
      Platform,
      Manufacturer,
      DeviceModel,
      UserId,
      ClientId,
      DeveloperId,
      ArdkVersion,
      ArdkAppInstanceId
    } 
    
    public void SetApplicationId(string bundleId)
    {
      _NAR_ARDKGlobalConfigHelperInternal_SetDataField((uint)_ConfigDataField.ApplicationId, bundleId);
    }

    public void SetArdkInstanceId(string instanceId)
    {
      _NAR_ARDKGlobalConfigHelperInternal_SetDataField((uint)_ConfigDataField.ArdkAppInstanceId, instanceId);
    }

    public string GetApplicationId()
    {
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetDataField((uint)_ConfigDataField.ApplicationId, stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
    }

    public string GetPlatform()
    {
#if UNITY_EDITOR
      return Application.unityVersion;
#else
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetDataField((uint)_ConfigDataField.Platform, stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
#endif
    }

    public string GetManufacturer()
    {
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetDataField((uint)_ConfigDataField.Manufacturer, stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
    }

    public string GetDeviceModel()
    {
#if UNITY_EDITOR
      return SystemInfo.operatingSystem;
#else
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetDataField((uint)_ConfigDataField.DeviceModel, stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
#endif
    }

    public string GetArdkVersion()
    {
      return ARDKGlobalVersion.GetARDKVersion();
    }

    public string GetUserId()
    {
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetDataField((uint)_ConfigDataField.UserId, stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
    }

    public string GetClientId()
    {
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetDataField((uint)_ConfigDataField.ClientId, stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
    }

    public string GetArdkAppInstanceId()
    {
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetDataField((uint)_ConfigDataField.ArdkAppInstanceId, stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
    }

    public string GetApiKey()
    {
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetApiKey(stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
    }

    // Switch to using a protobuf to pass data back and forth when that is solidified.
    // This is a bit fragile for now
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKGlobalConfigHelperInternal_SetDataField(uint field, string data);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKGlobalConfigHelper_GetApiKey(StringBuilder outKey, ulong maxKeySize);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKGlobalConfigHelper_GetDataField
    (
      uint field,
      StringBuilder outData,
      ulong maxDataSize
    );
  }
}
