// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Niantic.ARDK.Extensions
{
  /// Initializes, connects, runs, and disposes a MultipeerNetworking object either according to
  /// the Unity lifecycle of this component, or when scripted to do so.
  ///
  /// If ManageUsingUnityLifecycle is true:
  ///   OnAwake(): a MultipeerNetworking instance will be initialized
  ///   OnEnable(): the MultipeerNetworking will attempt to join a session
  ///   OnDisable(): the MultipeerNetworking will leave the session
  ///   OnDestroy(): the MultipeerNetworking will be disposed
  /// Else:
  ///   Call Initialize() to initialize a MultipeerNetworking instance
  ///   Call EnableFeatures() to join a networking session
  ///   Call DisableFeatures() to leave the networking session
  ///   Call Deinitialize() to dispose the MultipeerNetworking instance
  /// @note
  ///   A MultipeerNetworking instance cannot leave a session and be re-used to join a session
  ///   again, so OnEnable/EnableFeatures will initialize a new MultipeerNetworking instance
  ///   if the existing one has left a session.
  [DisallowMultipleComponent]
  public sealed class NetworkSessionManager
    : UnityLifecycleDriver
  {
    /// If unspecified, will default to trying to create a live device session, then remote, then mock.
    /// If specified, will throw an exception if the source is not supported on the current platform.
    /// @note
    ///   Live device networking is supported in the Unity Editor, but it must be explicitly
    ///   specified here.
    [FormerlySerializedAs("RuntimeEnvironment")]
    [SerializeField]
    private RuntimeEnvironment _runtimeEnvironment = RuntimeEnvironment.Default;

    /// Should be true if this ARSessionManager is being used in conjunction with an ARNetworkingManager.
    [SerializeField]
    private bool _useWithARNetworkingSession;

    /// The session identifier used when `Connect` is called.
    /// @note If the `InputField` is not-null, its text value will override this field's current value.
    [SerializeField]
    [Tooltip("The session identifier used when `Connect` is called.")]
    private string _sessionIdentifier = null;

    /// If not empty, the text value of this InputField will be used as the session
    /// identifier when `Connect` is called. Leave empty to get the default behaviour.
    [SerializeField]
    [Tooltip("(Optional) InputField source for the session identifier.")]
    private InputField _inputField = null;

    private IMultipeerNetworking _networking;
    private bool _needToRecreate;
    private Guid _stageIdentifier = default;

    /// @warning
    ///   Underlying object will change if this component is enabled (connected), disabled (disconnected),
    ///   and then connected again (reconnected). If subscribing to IMultipeerNetworking events,
    ///   you should listen to the MultipeerNetworkingFactory.NetworkingInitialized event
    ///   and add your IMultipeerNetworking event subscriptions to the latest initialized networking object.
    public IMultipeerNetworking Networking
    {
      get { return _networking; }
    }

    public RuntimeEnvironment RuntimeEnvironment
    {
      get => _runtimeEnvironment;
      set
      {
        if (_networking != null)
          throw new InvalidOperationException("Cannot set this property after this component has been initialized.");

        _runtimeEnvironment = value;
      }
    }

    public string SessionIdentifier
    {
      get => _sessionIdentifier;
      set
      {
        if (_networking != null && _networking.IsConnected)
        {
          ARLog._WarnRelease
          (
            "SessionIdentifier value changed but the networking instance is already in a session. " +
            "The new value will be used the next time this manager is used to connect to a session."
          );
        }

        _sessionIdentifier = value;
      }
    }

    protected override bool _CanReinitialize
    {
      get { return true; }
    }

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      Create();

      if (_inputField != null)
      {
        _sessionIdentifier = _inputField.text;
        _inputField.onEndEdit.AddListener(SetSessionIdentifier);
      }
    }

    internal void _InitializeWithIdentifier(Guid identifier)
    {
      _stageIdentifier = identifier;
      Initialize();
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();

      _stageIdentifier = Guid.Empty;

      if (_inputField != null)
        _inputField.onEndEdit.RemoveListener(SetSessionIdentifier);

      if (_networking != null)
      {
        _networking.Dispose();
        _networking = null;
      }
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      if (_networking != null && _needToRecreate)
      {
        // A networking, once left, is useless because it cannot be used to join/re-join a session.
        // So it has to be disposed and a new one created.
        _networking.Dispose();
        _networking = null;
        Create();
      }

      Connect();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      if (_networking != null)
      {
        if (_networking.IsConnected)
          _networking.Leave();
      }
    }

    public void SetSessionIdentifier(string sessionIdentifier)
    {
      _sessionIdentifier = sessionIdentifier;

      if (_inputField != null)
        _inputField.text = sessionIdentifier;
    }

    /// Initializes a new MultipeerNetworking object with the set RuntimeEnvironment(s), if one does
    /// not yet exist.
    private void Create()
    {
      if (_networking != null)
      {
        ARLog._Error
        (
          "Failed to create a MultipeerNetworking session because one already exists. " +
          "To create multiple sessions, use the MultipeerNetworkingFactory API instead."
        );

        return;
      }

      if (_useWithARNetworkingSession)
      {
        if (_stageIdentifier == Guid.Empty)
        {
          ARLog._Error
          (
            "Failed to create a MultipeerNetworking session because _useWithARNetworkingSession " +
            "is true but no stage identifier was specified."
          );

          return;
        }

        _networking = MultipeerNetworkingFactory.Create(_runtimeEnvironment, _stageIdentifier);
      }
      else
      {
        _networking = MultipeerNetworkingFactory.Create(_runtimeEnvironment);
      }

      ARLog._DebugFormat("Created {0} MultipeerNetworking: {1}.", false, _networking.RuntimeEnvironment, _networking.StageIdentifier);

      // Just in case the dev disposes the networking themselves instead of through this manager
      _networking.Deinitialized += _ =>
      {
        _networking = null;
        _needToRecreate = false;
      };
    }

    /// Connects the existing MultipeerNetworking object to a session with the set SessionIdentifier.
    private void Connect()
    {
      if (_networking == null)
      {
        ARLog._Error("Failed to connect MultipeerNetworking session because one was not initialized.");
        return;
      }

      if (string.IsNullOrEmpty(_sessionIdentifier) && _inputField != null)
        _sessionIdentifier = _inputField.text;

      var sessionMetadata = Encoding.UTF8.GetBytes(_sessionIdentifier);

      _needToRecreate = true;
      _networking.Join(sessionMetadata);
    }
  }
}
