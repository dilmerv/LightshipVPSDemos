// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

using Google.Protobuf;

namespace Niantic.ARDK.Configuration.Internal
{
  internal interface _IArdkConfigInternal
  {
    // Bundle id (iOS) or package name (Android). For example, com.nianticlabs.ardkexamples
    void SetApplicationId(string bundleId);
    
    // Guid encoded into a string.
    // From C#, this is the hyphen-separated canonical representation 
    void SetArdkInstanceId(string instanceId);

    // Common metadata getters
    
    // Get the previously set application id
    string GetApplicationId();
    
    // Get the platform (OS) of the device.
    // On iOS, this is formatted as "iOS ##.#"
    // On Android, this is formatted as "Android ##"
    string GetPlatform();
    
    // Get the reported device manufacturer
    string GetManufacturer();
    
    // Get the reported device model
    string GetDeviceModel();
    
    // Get the native ardk version
    string GetArdkVersion();
    
    // Get the previously set user id (identity)
    string GetUserId();
    
    // Get the ARDK generated client id associated with this device
    string GetClientId();
    
    // Get the previously set ardk application instance id
    string GetArdkAppInstanceId();

    // Get the currently set Api Key, or an empty string if none has been set
    string GetApiKey();
  }
}
