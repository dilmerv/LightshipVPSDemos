// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Configuration
{
  internal static class _ARConfigurationValidator
  {
    private static bool IsValidConfiguration(this IARConfiguration config, out string message)
    {
      message = string.Empty;

      if (config is IARWorldTrackingConfiguration worldConfig)
      {
        var hasHeadingAlignment = (worldConfig.WorldAlignment == WorldAlignment.GravityAndHeading);
        if (worldConfig.IsSharedExperienceEnabled && hasHeadingAlignment)
        {
          message =
            "Configuration with SharedExperienceEnabled can not use GravityAndHeading world " +
            "alignment.";

          return false;
        }

        if (worldConfig.IsMeshingEnabled && hasHeadingAlignment)
        {
          message =
            "Configuration with IsMeshingEnabled can not use GravityAndHeading world alignment.";

          return false;
        }
      }

      return true;
    }

    private static void SetMissingValues(this IARConfiguration config)
    {
      var worldConfig = config as IARWorldTrackingConfiguration;

      if (worldConfig == null)
        return;


      var isDepthEnabled = worldConfig.IsDepthEnabled;
      var isPointCloudEnabled = worldConfig.IsDepthPointCloudEnabled;

      if (isPointCloudEnabled && !isDepthEnabled)
      {
        ARLog._WarnRelease
        (
          "Enabling depth because depth point clouds were enabled. Use the ARDepthManager " +
          "component or the IARWorldTrackingConfiguration properties to further configure depth " +
          "functionality."
        );

        worldConfig.IsDepthEnabled = true;
      }

      if (worldConfig.IsMeshingEnabled && !isDepthEnabled)
      {
        ARLog._WarnRelease
        (
          "Enabling depth because meshing was enabled. Use the ARDepthManager component or " +
          "the IARWorldTrackingConfiguration properties to further configure depth functionality."
        );

        worldConfig.IsDepthEnabled = true;
      }

      var needsContextAwarenessUrl =
        isDepthEnabled ||
        worldConfig.IsMeshingEnabled ||
        worldConfig.IsSemanticSegmentationEnabled;

      var hasEmptyUrl = string.IsNullOrEmpty(ArdkGlobalConfig.GetContextAwarenessUrl());

      if (needsContextAwarenessUrl && hasEmptyUrl)
      {
        ARLog._Debug("Context Awareness URL was not set. The default URL will be used.");
        ArdkGlobalConfig.SetContextAwarenessUrl("");
      }
    }

    private static void CheckDeviceSupport(this IARConfiguration config)
    {
      var worldConfig = config as IARWorldTrackingConfiguration;

      if (worldConfig == null)
        return;

      if (worldConfig.IsDepthEnabled &&
        !ARWorldTrackingConfigurationFactory.CheckDepthEstimationSupport())
      {
        ARLog._Error
        (
          "Depth estimation is not supported on this device. " +
          "Unexpected behaviour or crashes may occur."
        );
      }

      if (worldConfig.IsMeshingEnabled &&
        !ARWorldTrackingConfigurationFactory.CheckMeshingSupport())
      {
        ARLog._Error
        (
          "Meshing is not supported on this device. " +
          "Unexpected behaviour or crashes may occur."
        );
      }

      if (worldConfig.IsSemanticSegmentationEnabled &&
        !ARWorldTrackingConfigurationFactory.CheckSemanticSegmentationSupport())
      {
        ARLog._Error
        (
          "Semantic segmentation is not supported on this device. " +
          "Unexpected behaviour or crashes may occur."
        );
      }
    }

    public static bool RunAllChecks
    (
      IARSession arSession,
      IARConfiguration newConfiguration
    )
    {
      string validConfigCheckMessage;
      if (!newConfiguration.IsValidConfiguration(out validConfigCheckMessage))
      {
        ARLog._Error(validConfigCheckMessage);
        return false;
      }

      newConfiguration.SetMissingValues();

      // ARDK's device support checks serve as recommendations, not a hard block.
      // Devices are still able to try to run unsupported features.
      newConfiguration.CheckDeviceSupport();

      return true;
    }
  }
}
