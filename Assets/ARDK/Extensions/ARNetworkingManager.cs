// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  /// A Unity component that manages an ARNetworking's lifetime. The session can either be started
  /// automatically through Unity lifecycle events, or can be controlled programatically.
  /// Any outstanding sessions are always cleaned up on destruction. Integrates with the
  /// ARSessionManager and NetworkSessionManager to make sure all components are set up correctly.
  ///
  /// If ManageUsingUnityLifecycle is true:
  ///   OnAwake():
  ///     An ARNetworking (and the component ARSession and MultipeerNetworking objects)
  ///     will be initialized
  ///   OnEnable():
  ///     The ARSession will be run and the MultipeerNetworking will join a session
  ///   OnDisable():
  ///     The ARSession will be paused and the MultipeerNetworking will leave the session
  ///   OnDestroy():
  ///     The ARNetworking (and the component ARSession and MultipeerNetworking objects)
  ///     will be disposed
  /// Else:
  ///   Call Initialize to:
  ///     Initialize an ARNetworking (and the component ARSession and MultipeerNetworking objects)
  ///   Call EnableFeatures to:
  ///     Run the ARSession and join the MultipeerNetworking
  ///   Call DisableFeatures to:
  ///     Pause the ARSession and leave the MultipeerNetworking session
  ///   Call Destroy to:
  ///     Dispose the ARNetworking (and the component ARSession and MultipeerNetworking objects)
  ///
  /// @note
  ///   Because the CapabilityChecker's method for checking device support is async, the above
  ///   events (i.e. initialization of ARNetworking) may not happen on the exact frame as
  ///   the method (OnAwake or Initialize) is invoked.
  [RequireComponent(typeof(ARSessionManager))]
  [RequireComponent(typeof(NetworkSessionManager))]
  [DisallowMultipleComponent]
  public class ARNetworkingManager: ARConfigChanger
  {
    private IARNetworking _arNetworking;
    private ARSessionManager _arSessionManager;
    private NetworkSessionManager _networkSessionManager;

    private bool _shouldBeRunning;
    private bool _needToRecreate;

    /// @warning
    ///   Underlying object will change if this component is enabled (connected), disabled (disconnected),
    ///   and then connected again (reconnected). If subscribing to IARNetworking events,
    ///   you should listen to the ARNetworkingFactory.ARNetworkingInitialized event
    ///   and add your IARNetworking event subscriptions to the latest initialized networking object.
    public IARNetworking ARNetworking
    {
      get { return _arNetworking; }
    }

    public ARSessionManager ARSessionManager
    {
      get { return _arSessionManager; }
    }

    public NetworkSessionManager NetworkSessionManager
    {
      get { return _networkSessionManager; }
    }

    protected override bool _CanReinitialize
    {
      get { return true; }
    }

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      ARSessionFactory.SessionInitialized += CreateOnARSessionInitialized;

      _arSessionManager = GetComponent<ARSessionManager>();
      _arSessionManager.Initialize();

      _networkSessionManager = GetComponent<NetworkSessionManager>();
    }

    private void CreateOnARSessionInitialized(AnyARSessionInitializedArgs args)
    {
      var arSession = args.Session;

      if (_arNetworking != null)
      {
        ARLog._Error("Failed to create an ARNetworking session because one already exists.");
        return;
      }

      _networkSessionManager._InitializeWithIdentifier(arSession.StageIdentifier);
      _arNetworking = ARNetworkingFactory.Create(arSession, _networkSessionManager.Networking);

      ARLog._DebugFormat
      (
        "Created {0} ARNetworking: {1}.",
        false,
        _arNetworking.ARSession.RuntimeEnvironment,
        _arNetworking.ARSession.StageIdentifier
      );

      // Networking is recreated when it disconnects and reconnects, so the ARNetworking must
      // be recreated too.
      _arNetworking.Networking.Connected += _ => _needToRecreate = true;

      // Just in case the dev disposes the ARNetworking themselves instead of through this manager
      _arNetworking.Deinitialized +=
        _ =>
        {
          _arNetworking = null;
          _needToRecreate = false;
        };

      if (_shouldBeRunning)
        EnableSessionManagers();
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();

      ARSessionFactory.SessionInitialized -= CreateOnARSessionInitialized;

      if (_arNetworking == null)
        return;

      _arNetworking.Dispose();
      _arNetworking = null;

      _arSessionManager.Deinitialize();
      _networkSessionManager.Deinitialize();
    }

    protected override void EnableFeaturesImpl()
    {
      // A networking, once left, is useless because it cannot be used to join/re-join a session.
      // So _arNetworking.Networking will be destroyed by the NetworkSessionManager.DisableFeatures
      // call, meaning if this component is enabled again, a new ARNetworking instance has to be
      // created
      if (_arNetworking != null && _needToRecreate)
      {
        _arNetworking.Dispose();
        _arNetworking = null;
      }

      // Call base here so the backup Initialize call creates the new objects
      base.EnableFeaturesImpl();

      _shouldBeRunning = true;

      if (ARSessionManager.ARSession != null)
        EnableSessionManagers();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      _shouldBeRunning = false;

      _arSessionManager.DisableFeatures();
      _networkSessionManager.DisableFeatures();
    }

    private void EnableSessionManagers()
    {
      _arSessionManager.EnableFeatures();
      _networkSessionManager.EnableFeatures();
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      if (properties.ARConfiguration is IARWorldTrackingConfiguration worldConfig)
        worldConfig.IsSharedExperienceEnabled = AreFeaturesEnabled;
    }
  }
}
