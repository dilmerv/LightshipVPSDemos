// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Internals.EditorUtilities;

using UnityEngine;

namespace Niantic.ARDK.Networking.HLAPI.Object.Unity
{
  /// Defines the behaviour of a NetworkedUnityObject.
  /// Since NetworkedUnityObjects have their own setup/destroy flows, do NOT
  /// override Awake.
  [RequireComponent(typeof(NetworkedUnityObject))]
  public abstract class NetworkedBehaviour: MonoBehaviour
  {
    /// The NetworkedUnityObject attached to the same GameObject as this component.
    [_Autofill]
    [SerializeField]
    private NetworkedUnityObject _owner = null;

    /// The NetworkedUnityObject attached to the same GameObject as this script
    public NetworkedUnityObject Owner
    {
      get { return _owner == null ? GetComponent<NetworkedUnityObject>() : _owner; }
    }

    public int Initialize(out Action initializer)
    {
      ARLog._DebugFormat
      (
        "Calling SetupSession for NetworkedBehaviour attached to group {0}",
        false,
        Owner.Id
      );

      SetupSession(out initializer, out int order);
      return order;
    }

    /// Implement this method to define the behaviour of the NetworkedUnityObject upon network
    /// initialization or startup
    /// @param onNetworkingDidInitialize
    ///   Action invoked when this GameObject is network spawned.
    /// @param order
    ///   Order that the onNetworkingDidInitialize Action is invoked, relative to that of other
    ///   NetworkedBehaviors on this GameObject.
    protected abstract void SetupSession(out Action onNetworkingDidInitialize, out int order);
  }
}
