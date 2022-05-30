// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.External;
using Niantic.ARDK.Internals.EditorUtilities;
using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Routing;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Niantic.ARDK.Networking.HLAPI.Object.Unity
{
  /// <summary>
  /// A MonoBehaviour that provides replication details for a prefab/scene object. Handles details
  /// such as valid destructors, authority, and network groups for each networked object.
  ///
  /// An object with this component needs to stay alive for at least one frame after Awake,
  /// or else CallbackQueue will have a null reference to the object.
  /// </summary>
  [DefaultExecutionOrder(Int32.MinValue)]
  [RequireComponent(typeof(AuthBehaviour))]
  public sealed class NetworkedUnityObject:
    MonoBehaviour, 
    ISerializationCallbackReceiver
  {
    /// <summary>   
    /// An Id that represents the instance of the object, shared between all peers in the session
    /// </summary>
    [SerializeField]
    private long _rawId;

    /// <summary>
    /// An Id that represents the prefab, shared between all builds of the scene
    /// </summary>
    [SerializeField]
    private long _prefabId;

    /// <summary>
    /// The peers that have the right to network destroy the object
    /// </summary>
    [EnumFlag]
    [SerializeField]
    private DestructionAuthorizedPeerOptions _destructionAuthorizedPeers = DestructionAuthorizedPeerOptions.Anyone;

    /// <summary>
    /// If the only peer with destructor rights leaves the session without destroying the object,
    ///   clean it up locally
    /// </summary>
    [SerializeField]
    private bool _destroyIfDestroyerLeaves;

    /// <summary>
    /// The AuthBehaviour that handles authority for each instance of this object
    /// </summary>
    [_Autofill]
    [SerializeField]
    private AuthBehaviour _auth;

    /// <summary>
    /// The default NetworkedBehaviour for this object
    /// </summary>
    [SerializeField]
    private NetworkedBehaviour _defaultBehaviour = null;

    /// <summary>
    /// All NetworkedBehaviours attached to this object, will be populated automatically
    /// </summary>
    [SerializeField]
    internal NetworkedBehaviour[] _behaviours;

    /// <summary>
    /// Possible peer(s) that can be given the right to network destroy the object
    /// </summary>
    [Flags]
    private enum DestructionAuthorizedPeerOptions
    {
      /// This object should not be network destroyed
      None = 0,

      /// Peers other than the spawner/authority can network destroy this object
      UnrelatedPeers = 1,

      /// The spawner of this object can network destroy it
      Spawner = 2,

      /// The authority of this object can network destroy it
      Authority = 4,

      /// All peers can network destroy this object
      Anyone = ~None,
    }
    
    private INetworkGroup _group;
    
    private static System.Random _random = new System.Random();


    private IHlapiSession Session
    {
      get
      {
        if (Networking != null)
        {
          return Networking.GetOrCreateManagedSession();
        }

        return gameObject.scene.GetOrCreateManagedSession();
      }
    }

    /// <summary>
    /// Whether or not the object has already been destroyed
    /// </summary>
    internal bool _isDestroyed;

    /// <summary>
    /// Whether or not the object was spawned by the local peer
    /// </summary>
    public bool WasSpawnedByMe
    {
      get => SpawningPeer != null && SpawningPeer.Equals(Networking.Self);
    }

    /// <summary>
    /// The peer that network spawned this object, will be null if not network spawned
    /// </summary>
    public IPeer SpawningPeer { get; internal set; }

    /// <summary>
    /// The networking instance that this object is tied to
    /// </summary>
    public IMultipeerNetworking Networking { get; internal set; }

    /// <summary>
    /// An Id that represents the instance of the object, shared between all peers in the session
    /// </summary>
    public NetworkId Id
    {
      get => (NetworkId)(ulong)_rawId;
      internal set => _rawId = (long)(ulong)value;
    }

    /// <summary>
    /// An Id that represents the prefab, shared between all builds of the scene
    /// </summary>
    public NetworkId PrefabId
    {
      get => (NetworkId)(ulong)_prefabId;
    }

    /// <summary>
    /// The AuthBehaviour that handles this object
    /// </summary>
    public AuthBehaviour Auth
    {
      get => _auth;
    }

    /// <summary>
    /// The NetworkedBehaviour that determines the behaviour of the NetworkedUnityObject
    /// </summary>
    public NetworkedBehaviour DefaultBehaviour
    {
      get => _defaultBehaviour;
    }

    /// <summary>
    /// The networking group that corresponds to this object, handled by the UnitySceneNetworkMaster
    /// </summary>
    public INetworkGroup Group
    {
      get => _group ?? (_group = Session.CreateAndRegisterGroup(Id));
    }

    /// <summary>
    /// Initializes the NetworkedUnityObject and all NetworkedBehaviours on the object
    /// </summary>
    public void Initialize()
    {
      ARLog._DebugFormat
      (
        "Initializing NetworkedUnityObject {0}",
        false,
        Id.RawId
      );
      
      _group ??= Session.CreateAndRegisterGroup(Id);

      var initializerList = new List<KeyValuePair<int, Action>>();

      if (_behaviours.Length == 0)
      {
        var warningFormat =
          "The NetworkedUnityObject on {0} was initialized with no behaviours, maybe those need " +
          "to be set up?";

        ARLog._WarnFormat(warningFormat, false, gameObject.name);
      }

      foreach (var behaviour in _behaviours)
      {
        var order = behaviour.Initialize(out Action initializer);
        initializerList.Add(new KeyValuePair<int, Action>(order, initializer));
      }

      initializerList.Sort((kvp1, kvp2) => kvp1.Key.CompareTo(kvp2.Key));

      foreach (var initKvp in initializerList)
        initKvp.Value.Invoke();
    }

    /// <summary>
    /// Determines if a peer has the right to destroy an object
    /// </summary>
    /// <param name="peer">The peer that would like to destroy the object</param>
    /// <returns>Whether or not the peer can destroy this object</returns>
    public bool IsDestructionAuthorizedPeer(IPeer peer)
    {
      // If SpawningPeer is not set, this object was not network spawned, so don't try to network
      // destroy it
      if (SpawningPeer == null)
        return false;

      if (_destructionAuthorizedPeers == DestructionAuthorizedPeerOptions.None)
        return false;

      if (_destructionAuthorizedPeers == DestructionAuthorizedPeerOptions.Anyone)
        return true;

      var selectedUnrelatedPeers =
        _destructionAuthorizedPeers & DestructionAuthorizedPeerOptions.UnrelatedPeers;
      
      if (selectedUnrelatedPeers == DestructionAuthorizedPeerOptions.UnrelatedPeers)
        if (!SpawningPeer.Equals(peer) && _auth.RoleOfPeer(peer) != Role.Authority)
          return true;

      if ((_destructionAuthorizedPeers & DestructionAuthorizedPeerOptions.Spawner) != 0)
        if (SpawningPeer.Equals(peer))
          return true;

      if ((_destructionAuthorizedPeers & DestructionAuthorizedPeerOptions.Authority) != 0)
        if (_auth.RoleOfPeer(peer) == Role.Authority)
          return true;

      return false;
    }

    /// <summary>
    /// Checks if local destruction of this object is allowed if the sole destructor of the object
    ///   leaves the session without destroying the object, as well as if the peer in question is
    ///   in fact the sole destructor of the object
    /// </summary>
    /// <param name="peer">The peer that left the session</param>
    internal bool CanDestroyIfDestructorLeaves(IPeer peer)
    {
      if (!_destroyIfDestroyerLeaves)
        return false;

      bool shouldReturnFalse =
        (_destructionAuthorizedPeers & DestructionAuthorizedPeerOptions.Spawner) == 0 &&
        (_destructionAuthorizedPeers & DestructionAuthorizedPeerOptions.Authority) == 0;
      
      if (shouldReturnFalse)
        return false;

      return IsDestructionAuthorizedPeer(peer);
    }

    private void Start()
    {
      // If SpawningPeer is set, this object was NetworkSpawned and init already occurred
      if (SpawningPeer != null)
        return;

      if (Networking == null)
        Networking = MultipeerNetworkingFactory.Networkings.FirstOrDefault();

      if (Networking != null)
      {
        ARLog._DebugFormat
        (
          "NetworkedUnityObject {0} is attached to {1}, initializing",
          false,
          _rawId,
          Networking.StageIdentifier
        );
        Initialize();
      }
      else
        MultipeerNetworkingFactory.NetworkingInitialized += _NetworkingInitialized;
    }

    private void _NetworkingInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      MultipeerNetworkingFactory.NetworkingInitialized -= _NetworkingInitialized;
      Networking = args.Networking;
      ARLog._DebugFormat
      (
        "NetworkedUnityObject {0} is attached to {1}, initializing",
        false,
        _rawId,
        Networking.StageIdentifier
      );
      Initialize();
    }

    private void OnDestroy()
    {
      ARLog._DebugFormat
      (
        "NetworkedUnityObject {0} is destroyed, unregistering",
        false,
        _rawId
      );
      Group.Unregister();
      
      /// If this object was already destroyed, don't try to destroy it again.
      if (_isDestroyed)
        return;

      if (Networking == null)
      {
        // As the networking was never set, it means we need to unregister from the static event.
        MultipeerNetworkingFactory.NetworkingInitialized -= _NetworkingInitialized;
        _isDestroyed = true;
        return;
      }

      _isDestroyed = true;
      this.NetworkDestroy();
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
      // Check if we're serializing a regular prefab
      var prefabAssetType = PrefabUtility.GetPrefabAssetType(this);
      if
      (
        prefabAssetType == PrefabAssetType.Regular &&
        !PrefabUtility.IsPartOfPrefabInstance(this)
      )
      {
        // Prefab rawID should always be 0. Important to ensure in case user hits "Apply All"
        // rawID is a trait of prefab instances anyway
        _rawId = 0;
        
        // If we haven't generated a prefabID, do so
        if (_prefabId == 0)
          _prefabId = GenerateId();
      }
#endif
    }

    public void OnAfterDeserialize()
    {
      // Explicitly left empty 
    }
    
    /// <summary>
    /// Generates a randomized Long to be used for an Id.
    /// </summary>
    internal static long GenerateId() 
    {
      var longBytes = new byte[8];
      _random.NextBytes(longBytes);
      
      return BitConverter.ToInt64(longBytes, 0);
    }
  }
}
