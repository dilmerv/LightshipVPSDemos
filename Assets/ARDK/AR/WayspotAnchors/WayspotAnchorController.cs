// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public class WayspotAnchorController
  {
    /// Called when the localization status has changed
    public ArdkEventHandler<LocalizationStateUpdatedArgs> LocalizationStateUpdated;

    /// Called when new anchors have been created
    public ArdkEventHandler<WayspotAnchorsCreatedArgs> WayspotAnchorsCreated;

    /// Called when wayspot anchors report a new position/rotation
    public ArdkEventHandler<WayspotAnchorsResolvedArgs> WayspotAnchorsTrackingUpdated;

    /// Called when the status of wayspot anchors has changed
    public ArdkEventHandler<WayspotAnchorStatusUpdatedArgs> WayspotAnchorStatusUpdated;

    private IARSession _arSession;
    private _IWayspotAnchorImplementation _wayspotAnchorImplementation;
    private LocalizationState _localizationState;

    /// Creates a new wayspot anchor API to consume
    /// @param arSession The AR session required by the WayspotAnchorController to run VPS.
    /// @param locationService The location service required by the WayspotAnchorController to run VPS.
    public WayspotAnchorController(IARSession arSession, ILocationService locationService)
    {
      _arSession = arSession;
      _arSession.SetupLocationService(locationService);
      _arSession.Deinitialized += HandleSessionDeinitialized;
      _wayspotAnchorImplementation = CreateWayspotAnchorController();
    }

    /// Starts the virtual position system
    /// @param wayspotAnchorsConfiguration The configuration to start VPS with
    public void StartVps(IWayspotAnchorsConfiguration wayspotAnchorsConfiguration)
    {
      _wayspotAnchorImplementation.StartVPS(wayspotAnchorsConfiguration);
      ARLog._Debug($"Started VPS for Stage ID: {_arSession.StageIdentifier}");
    }

    /// Stops the virtual position system
    /// @note This will reset the state and stop all pending creations and trackings
    public void StopVps()
    {
      _wayspotAnchorImplementation.StopVPS();
    }

    /// Creates new wayspot anchors based on position and rotations
    /// @param localPoses The position and rotation used to create new wayspot anchors with
    /// @return The IDs of the newly created wayspot anchors
    public Guid[] CreateWayspotAnchors(params Matrix4x4[] localPoses)
    {
      if (_localizationState != LocalizationState.Localized)
      {
        ARLog._Error
        (
          $"Failed to create wayspot anchor, because the Localization State is {_localizationState}."
        );

        return Array.Empty<Guid>();
      }

      return _wayspotAnchorImplementation.CreateWayspotAnchors(localPoses);
    }

    /// Pauses the tracking of wayspot anchors.  This can be used to conserve resources for wayspot anchors which you currently do not care about,
    /// but may again in the future
    /// @param wayspotAnchors The wayspot anchors to pause tracking for
    public void PauseTracking(params IWayspotAnchor[] wayspotAnchors)
    {
      _wayspotAnchorImplementation.StopResolvingWayspotAnchors(wayspotAnchors);
      foreach (var wayspotAnchor in wayspotAnchors)
      {
        var trackable = ((_IInternalTrackable)wayspotAnchor);
        trackable.SetTrackingEnabled(false);
      }
    }

    /// Resumes the tracking of previously paused wayspot anchors
    /// @param wayspotAnchors The wayspot anchors to resume tracking for
    public void ResumeTracking(params IWayspotAnchor[] wayspotAnchors)
    {
      _wayspotAnchorImplementation.StartResolvingWayspotAnchors(wayspotAnchors);
      foreach (var wayspotAnchor in wayspotAnchors)
      {
        var trackable = ((_IInternalTrackable)wayspotAnchor);
        trackable.SetTrackingEnabled(true);
      }
    }

    /// Restores previously created wayspot anchors via their payloads.  Use this to restore wayspot anchors from the payload stored in your database
    /// from a another session.
    /// @param wayspotAnchorPayloads The payload (data) used to restore previously created wayspot anchors
    /// @return The restored wayspot anchors
    public IWayspotAnchor[] RestoreWayspotAnchors(params WayspotAnchorPayload[] wayspotAnchorPayloads)
    {
      var wayspotAnchors = new List<IWayspotAnchor>();
      foreach (var wayspotAnchorPayload in wayspotAnchorPayloads)
      {
        byte[] blob = wayspotAnchorPayload._Blob;
      #if UNITY_EDITOR
        var wayspotAnchor = new _MockWayspotAnchor(blob);
      #else
        var wayspotAnchor = new _NativeWayspotAnchor(blob);
      #endif
        wayspotAnchors.Add(wayspotAnchor);
      }

      return wayspotAnchors.ToArray();
    }

    private void HandleSessionDeinitialized(ARSessionDeinitializedArgs arSessionDeinitializedArgs)
    {
      _arSession.Deinitialized -= HandleSessionDeinitialized;
      _wayspotAnchorImplementation.LocalizationStateUpdated -= HandleLocalizationStateUpdated;
      _wayspotAnchorImplementation.WayspotAnchorsCreated -= HandleWayspotAnchorsCreated;
      _wayspotAnchorImplementation.WayspotAnchorsResolved -= HandleWayspotAnchorsResolved;
      _wayspotAnchorImplementation.WayspotAnchorStatusUpdated -= HandleWayspotAnchorStatusUpdated;

      _wayspotAnchorImplementation.Dispose();
    }

    private _IWayspotAnchorImplementation CreateWayspotAnchorController()
    {
      _IWayspotAnchorImplementation wayspotAnchorImplementation;
      switch (_arSession.RuntimeEnvironment)
      {
        case RuntimeEnvironment.Default:
        case RuntimeEnvironment.Remote:
          throw new NotImplementedException($"Remote runtime environment not yet supported.");
        case RuntimeEnvironment.LiveDevice:
          wayspotAnchorImplementation = new NativeWayspotAnchorImplementation(_arSession);
          break;

        case RuntimeEnvironment.Mock:
          wayspotAnchorImplementation = new MockWayspotAnchorImplementation(_arSession);
          break;

        default:
          throw new InvalidEnumArgumentException($"Invalid runtime environment! ({_arSession.RuntimeEnvironment}).");
      }

      wayspotAnchorImplementation.LocalizationStateUpdated += HandleLocalizationStateUpdated;
      wayspotAnchorImplementation.WayspotAnchorsCreated += HandleWayspotAnchorsCreated;
      wayspotAnchorImplementation.WayspotAnchorsResolved += HandleWayspotAnchorsResolved;
      wayspotAnchorImplementation.WayspotAnchorStatusUpdated += HandleWayspotAnchorStatusUpdated;

      return wayspotAnchorImplementation;
    }

    private void HandleLocalizationStateUpdated(LocalizationStateUpdatedArgs localizationStateUpdatedArgs)
    {
      _localizationState = localizationStateUpdatedArgs.State;
      LocalizationStateUpdated?.Invoke(localizationStateUpdatedArgs);
    }

    private void HandleWayspotAnchorsCreated(WayspotAnchorsCreatedArgs wayspotAnchorsCreatedArgs)
    {
      WayspotAnchorsCreated?.Invoke(wayspotAnchorsCreatedArgs);
    }

    private void HandleWayspotAnchorsResolved(WayspotAnchorsResolvedArgs wayspotAnchorsResolvedArgs)
    {
      WayspotAnchorsTrackingUpdated?.Invoke(wayspotAnchorsResolvedArgs);
    }

    private void HandleWayspotAnchorStatusUpdated(WayspotAnchorStatusUpdatedArgs wayspotAnchorStatusUpdatedArgs)
    {
      WayspotAnchorStatusUpdated?.Invoke(wayspotAnchorStatusUpdatedArgs);
    }
  }
}
