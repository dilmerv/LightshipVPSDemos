// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Camera.Input
{
  internal static class _CameraInputSystemProvider
  {
    public static void AttachController(GameObject cameraGameObject)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
      var cameraLegacyInputController = cameraGameObject.AddComponent<_LegacyCameraController>();
      cameraLegacyInputController.enabled = true;

#else
      var cameraNewInputController = cameraGameObject.AddComponent<_CameraController>();
      cameraNewInputController.enabled = true;
#endif
    }
  }
}