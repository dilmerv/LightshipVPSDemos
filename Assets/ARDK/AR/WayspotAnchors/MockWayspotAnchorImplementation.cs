// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal sealed class MockWayspotAnchorImplementation: _IWayspotAnchorImplementation
  {
    /// Called when the localization state has changed
    public event ArdkEventHandler<LocalizationStateUpdatedArgs> LocalizationStateUpdated;

    /// Called when the status of managed poses has changed
    public event ArdkEventHandler<WayspotAnchorStatusUpdatedArgs> WayspotAnchorStatusUpdated;

    /// Called when new wayspot anchors have been created
    public event ArdkEventHandler<WayspotAnchorsCreatedArgs> WayspotAnchorsCreated;

    /// Called when wayspot anchors have updated their position/rotation
    public event ArdkEventHandler<WayspotAnchorsResolvedArgs> WayspotAnchorsResolved;

    private Dictionary<Guid, _MockWayspotAnchor> _wayspotAnchors =
      new Dictionary<Guid, _MockWayspotAnchor>();

    private List<Guid> resolvedWayspotAnchors = new List<Guid>();

    private bool _isDisposed;
    private IARSession _arSession;
    private LocalizationState _localizationState;

    /// Creates a Mock Wayspot Anchor Controller
    /// @param arSession The AR Session to create the mock wayspot anchor controller with
    internal MockWayspotAnchorImplementation(IARSession arSession)
    {
      _arSession = arSession;
      _arSession.Paused += HandleARSessionPaused;
      _arSession.Ran += HandleARSessionRan;
      _isDisposed = false;
      ResolveWayspotAnchors();
    }

    /// Disposes the Mock Wayspot Anchor Controller
    public void Dispose()
    {
      _isDisposed = true;
    }

    /// Starts VPS
    /// @param localizationConfiguration The configuration to start VPS with
    public async void StartVPS(IWayspotAnchorsConfiguration wayspotAnchorsConfiguration)
    {
      await SimulateServerWork();
      _localizationState = LocalizationState.Initializing;
      var initializingLocalizationStateUpdatedArgs = new LocalizationStateUpdatedArgs
        (LocalizationState.Initializing, LocalizationFailureReason.None);

      LocalizationStateUpdated?.Invoke(initializingLocalizationStateUpdatedArgs);

      await SimulateServerWork();
      _localizationState = LocalizationState.Localizing;
      var localizingLocalizationStateUpdatedArgs = new LocalizationStateUpdatedArgs
        (LocalizationState.Localizing, LocalizationFailureReason.None);

      LocalizationStateUpdated?.Invoke(localizingLocalizationStateUpdatedArgs);

      await SimulateServerWork();
      _localizationState = LocalizationState.Localized;
      var localizedLocalizationStateUpdatedArgs = new LocalizationStateUpdatedArgs
        (LocalizationState.Localized, LocalizationFailureReason.None);

      LocalizationStateUpdated?.Invoke(localizedLocalizationStateUpdatedArgs);
    }

    /// Stops VPS
    public async void StopVPS()
    {
      await SimulateServerWork();
      var localizedLocalizationStateUpdatedArgs = new LocalizationStateUpdatedArgs
        (LocalizationState.Failed, LocalizationFailureReason.Canceled);

      LocalizationStateUpdated?.Invoke(localizedLocalizationStateUpdatedArgs);
    }

    /// Creates new wayspot anchors with position and rotation
    /// @param localPoses The position and rotation of the new wayspot anchors to create
    /// @return The IDs of the newly created wayspot anchors
    public Guid[] CreateWayspotAnchors(params Matrix4x4[] localPoses)
    {
      var createdWayspotAnchors = new Dictionary<Guid, Matrix4x4>();
      var ids = new List<Guid>();
      var statuses = new List<WayspotAnchorStatusUpdate>();
      foreach (var localPose in localPoses)
      {
        var id = Guid.NewGuid();
        ids.Add(id);
        createdWayspotAnchors.Add(id, localPose);
        var status = new WayspotAnchorStatusUpdate(id, WayspotAnchorStatusCode.Pending);
        statuses.Add(status);
      }

      var wayspotAnchorStatusUpdatedArgs = new WayspotAnchorStatusUpdatedArgs(statuses.ToArray());
      WayspotAnchorStatusUpdated?.Invoke(wayspotAnchorStatusUpdatedArgs);
      CreateWayspotAnchors(createdWayspotAnchors);
      return ids.ToArray();
    }

    /// Starts resolving the wayspot anchors.  Anchors that are being resolved will report changes in position and rotation via the WayspotAnchorsResolved event
    /// @param wayspotAnchors The wayspot anchors to start resolving
    public async void StartResolvingWayspotAnchors(params IWayspotAnchor[] wayspotAnchors)
    {
      await SimulateServerWork();
      var ids = wayspotAnchors.Select(p => p.ID);
      var createdWayspotAnchors = wayspotAnchors.Where
          (p => !_wayspotAnchors.ContainsKey(p.ID))
        .Select(p => (_MockWayspotAnchor)p);

      CreateWayspotAnchors(createdWayspotAnchors.ToArray());
      resolvedWayspotAnchors.AddRange(ids);
    }

    /// Stops resolving the wayspot anchors
    /// param wayspotAnchors Wayspot anchors to stop resolving
    public async void StopResolvingWayspotAnchors(params IWayspotAnchor[] wayspotAnchors)
    {
      await SimulateServerWork();
      var ids = wayspotAnchors.Select(p => p.ID);
      foreach (var id in ids)
      {
        resolvedWayspotAnchors.Remove(id);
      }
    }

    private async void HandleARSessionPaused(ARSessionPausedArgs arSessionPausedArgs)
    {
      await SimulateServerWork();
      _localizationState = LocalizationState.Localizing;
      var localizedLocalizationStateUpdatedArgs = new LocalizationStateUpdatedArgs
        (LocalizationState.Localizing, LocalizationFailureReason.None);

      LocalizationStateUpdated?.Invoke(localizedLocalizationStateUpdatedArgs);
    }

    private async void HandleARSessionRan(ARSessionRanArgs args)
    {
      await SimulateServerWork();
      _localizationState = LocalizationState.Localized;
      var localizedLocalizationStateUpdatedArgs = new LocalizationStateUpdatedArgs
        (LocalizationState.Localized, LocalizationFailureReason.None);

      LocalizationStateUpdated?.Invoke(localizedLocalizationStateUpdatedArgs);
    }

    private async void CreateWayspotAnchors(Dictionary<Guid, Matrix4x4> localPoses)
    {
      await SimulateServerWork();
      var createdWayspotAnchors = new List<IWayspotAnchor>();
      foreach (var localPose in localPoses)
      {
        var wayspotAnchor = new _MockWayspotAnchor(localPose.Key, localPose.Value);
        createdWayspotAnchors.Add(wayspotAnchor);
        _wayspotAnchors.Add(wayspotAnchor.ID, wayspotAnchor);
      }

      var wayspotAnchorsCreatedArgs = new WayspotAnchorsCreatedArgs
        (createdWayspotAnchors.ToArray());

      WayspotAnchorsCreated?.Invoke(wayspotAnchorsCreatedArgs);
      var statuses = new List<WayspotAnchorStatusUpdate>();
      foreach (var createdWayspotAnchor in createdWayspotAnchors)
      {
        var id = createdWayspotAnchor.ID;
        var status = new WayspotAnchorStatusUpdate(id, WayspotAnchorStatusCode.Success);
        statuses.Add(status);
      }

      var wayspotAnchorStatusUpdatedArgs = new WayspotAnchorStatusUpdatedArgs(statuses.ToArray());
      WayspotAnchorStatusUpdated?.Invoke(wayspotAnchorStatusUpdatedArgs);
    }

    private void CreateWayspotAnchors(params _MockWayspotAnchor[] wayspotAnchors)
    {
      var statuses = new List<WayspotAnchorStatusUpdate>();
      foreach (var wayspotAnchor in wayspotAnchors)
      {
        var id = wayspotAnchor.ID;
        _wayspotAnchors.Add(id, wayspotAnchor);
        var status = new WayspotAnchorStatusUpdate(id, WayspotAnchorStatusCode.Success);
        statuses.Add(status);
      }

      var wayspotAnchorStatusUpdatedArgs = new WayspotAnchorStatusUpdatedArgs(statuses.ToArray());
      WayspotAnchorStatusUpdated?.Invoke(wayspotAnchorStatusUpdatedArgs);
    }

    private async void ResolveWayspotAnchors()
    {
      while (!_isDisposed)
      {
        if (_localizationState != LocalizationState.Localized)
        {
          await Task.Delay(1);
          continue;
        }
        await SimulateServerWork();
        var resolutions = new List<WayspotAnchorResolvedArgs>();
        foreach (var id in resolvedWayspotAnchors)
        {
          var wayspotAnchor = _wayspotAnchors[id];
          var localPose = wayspotAnchor.LocalPose;
          var resolution = new WayspotAnchorResolvedArgs(id, localPose);
          resolutions.Add(resolution);
        }

        var wayspotAnchorsResolvedArgs = new WayspotAnchorsResolvedArgs(resolutions.ToArray());
        WayspotAnchorsResolved?.Invoke(wayspotAnchorsResolvedArgs);
      }
    }

    private async Task SimulateServerWork()
    {
      await Task.Delay(100);
    }
  }
}
