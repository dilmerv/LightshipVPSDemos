// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;

using Google.Protobuf;

using Niantic.ARDK.AR.Protobuf;

using UnityEditor;

namespace Niantic.ARDK.Configuration.Internal
{
  internal static class ArdkConfigInternalExtension
  {
    public const string AuthorizationHeaderKey = "Authorization";
    public const string ClientIdHeaderKey = "x-ardk-clientid";
    public const string UserIdHeaderKey = "x-ardk-userid";
    
    /// Get the common data envelope (client metadata protobuf) serialized into a Json string
    public static string GetCommonDataEnvelopeAsJson(this _IArdkConfigInternal config)
    {
      // Cannot apply a null value to protobufs, so we need the null checks.
      var proto = new ARCommonMetadata();
      PopulateProtoFields(proto, config);
      var protoAsJson = JsonFormatter.Default.Format(proto);
      
      return protoAsJson;
    }
    
    /// Get the common data envelope (client metadata protobuf) serialized into a Json string
    /// Additionally populates the request_id field with a randomly generated UUID
    public static string GetCommonDataEnvelopeWithRequestIdAsJson(this _IArdkConfigInternal config)
    {
      var proto = new ARCommonMetadata();
      PopulateProtoFields(proto, config);
      proto.RequestId = Guid.NewGuid().ToString();
      var protoAsJson = JsonFormatter.Default.Format(proto);

      return protoAsJson;
    }
    
    public static ARCommonMetadataStruct GetCommonDataEnvelopeWithRequestIdAsStruct(this _IArdkConfigInternal config)
    {
      var metadata = new ARCommonMetadataStruct
      (
        config.GetApplicationId(),
        config.GetPlatform(),
        config.GetManufacturer(),
        config.GetDeviceModel(),
        config.GetUserId(),
        config.GetClientId(),
        config.GetArdkVersion(),
        config.GetArdkAppInstanceId(),
        Guid.NewGuid().ToString()
      );

      return metadata;
    }

    public static Dictionary<string, string> GetApiGatewayHeader(this _IArdkConfigInternal config)
    {
      Dictionary<string, string> header = new Dictionary<string, string>();

      header.Add(AuthorizationHeaderKey, config.GetApiKey());
      header.Add(ClientIdHeaderKey, config.GetClientId()); 
      header.Add(UserIdHeaderKey, config.GetUserId());
      
      return header;
    }

    private static void PopulateProtoFields(ARCommonMetadata proto, _IArdkConfigInternal config)
    {
      var manufacturer = config.GetManufacturer();
      if (!string.IsNullOrEmpty(manufacturer))
        proto.Manufacturer = manufacturer;

      var appId = config.GetApplicationId();
      if (!string.IsNullOrEmpty(appId))
        proto.ApplicationId = appId;

      var appInstanceId = config.GetArdkAppInstanceId();
      if (!string.IsNullOrEmpty(appInstanceId))
        proto.ArdkAppInstanceId = appInstanceId;

      var ardkVersion = config.GetArdkVersion();
      if (!string.IsNullOrEmpty(ardkVersion))
        proto.ArdkVersion = ardkVersion;

      var clientId = config.GetClientId();
      if (!string.IsNullOrEmpty(clientId))
        proto.ClientId = clientId;

      var deviceModel = config.GetDeviceModel();
      if (!string.IsNullOrEmpty(deviceModel))
        proto.DeviceModel = deviceModel;

      var platform = config.GetPlatform();
      if (!string.IsNullOrEmpty(platform))
        proto.Platform = platform;

      var userId = config.GetUserId();
      if (!string.IsNullOrEmpty(userId))
        proto.UserId = userId;
    }
  }
  
  [Serializable]    
  public struct ARCommonMetadataStruct 
  {
    public string application_id; 
    public string platform; 
    public string manufacturer; 
    public string device_model; 
    public string user_id; 
    public string client_id; 
    public string ardk_version;
    public string ardk_app_instance_id;
    public string request_id; 

    public ARCommonMetadataStruct
    (
      string applicationID,
      string platform,
      string manufacturer,
      string deviceModel,
      string userID,
      string clientID,
      string ardkVersion,
      string ardkAppInstanceID,
      string requestID
    )
    {
      application_id = applicationID;
      this.platform = platform;
      this.manufacturer = manufacturer;
      device_model = deviceModel;
      user_id = userID;
      client_id = clientID;
      ardk_version = ardkVersion;
      ardk_app_instance_id = ardkAppInstanceID;
      request_id = requestID;
    }
  }
}
