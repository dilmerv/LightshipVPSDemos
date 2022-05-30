// Copyright 2022 Niantic, Inc. All Rights Reserved.


using System;
#if UNITY_ANDROID
using Niantic.ARDK.Utilities.Permissions;
using UnityEngine.Android;

namespace Niantic.ARDK.Extensions.Permissions
{
  /// Static helper for requesting permissions at runtime.
  public static class AndroidPermissionManager
  {
    /// Request a single Android permission.
    [Obsolete("Use PermissionRequester.RequestPermission(ARDKPermission permission, Action<PermissionStatus> callback) instead.")]
    public static void RequestPermission(ARDKPermission permission)
    {
      if (!Permission.HasUserAuthorizedPermission(PermissionRequester.AndroidPermissionString[permission]))
        Permission.RequestUserPermission(PermissionRequester.AndroidPermissionString[permission]);
    }

    [Obsolete("Use PermissionRequester.HasPermission(permission) instead.")]
    public static bool HasPermission(ARDKPermission permission)
    {
      return PermissionRequester.HasPermission(permission);
    }
  }
}
#endif
