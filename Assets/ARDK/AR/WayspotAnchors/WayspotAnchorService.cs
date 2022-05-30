// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public class WayspotAnchorService : IDisposable
  {
    /// The current localization state of the waypoint anchor service
    public LocalizationState LocalizationState { get; private set; }

    private readonly WayspotAnchorController _wayspotAnchorController;
    private readonly IARSession _arSession;
    private readonly ILocationService _locationService;
    private readonly IWayspotAnchorsConfiguration _wayspotAnchorsConfiguration;
    private readonly Dictionary<Guid, IWayspotAnchor> _wayspotAnchors;
    private readonly Dictionary<Guid, WayspotAnchorStatusCode> _wayspotAnchorStatusCodes;

    private const string _mustLocaliseBeforeCreatingAnchorErrorMessage = "Must localize before creating wayspot anchors.";

    /// Creates a new wayspot anchor service
    /// @param arSession The AR Session used to create the wayspot anchor service
    /// @param locationService The location service used to create the wayspot anchor service
    /// @param wayspotAnchorsConfiguration The configuration of the wayspot anchors
    public WayspotAnchorService
    (
      IARSession arSession,
      ILocationService locationService,
      IWayspotAnchorsConfiguration wayspotAnchorsConfiguration
    )
    {
      _wayspotAnchors = new Dictionary<Guid, IWayspotAnchor>();
      _wayspotAnchorStatusCodes = new Dictionary<Guid, WayspotAnchorStatusCode>();
      _arSession = arSession;
      _locationService = locationService;
      _wayspotAnchorsConfiguration = wayspotAnchorsConfiguration;
      _arSession.Deinitialized += HandleSessionDeinitialized;
      _wayspotAnchorController = CreateWayspotAnchorController();
      _arSession.Paused += HandleARSessionPaused;
      _arSession.Ran += HandleARSessionRan;
    }

    private void HandleARSessionPaused(ARSessionPausedArgs args)
    {
      _wayspotAnchorController.PauseTracking(_wayspotAnchors.Values.ToArray());
    }

    private void HandleARSessionRan(ARSessionRanArgs args)
    {
      _wayspotAnchorController.ResumeTracking(_wayspotAnchors.Values.ToArray());
    }

    /// Creates new wayspot anchors
    /// @param callback The callback when the wayspot anchors have been created
    /// @param localPoses The positions and rotations used the create the wayspot anchors
    public async void CreateWayspotAnchors(Action<IWayspotAnchor[]> callback, params Matrix4x4[] localPoses)
    {
      if (LocalizationState != LocalizationState.Localized)
      {
        ARLog._Error(_mustLocaliseBeforeCreatingAnchorErrorMessage);
        return;
      }

      var wayspotAnchors = await CreateWayspotAnchorsAsync(localPoses);
      callback?.Invoke(wayspotAnchors);
    }

    /// Creates new wayspot anchors.  The new wayspot anchor is tracked by default.
    /// @param localPoses The positions and rotations used the create the wayspot anchors
    /// @return The newly created wayspot anchors
    public async Task<IWayspotAnchor[]> CreateWayspotAnchorsAsync(params Matrix4x4[] localPoses)
    {
      if (LocalizationState != LocalizationState.Localized)
      {
        ARLog._Error(_mustLocaliseBeforeCreatingAnchorErrorMessage);
        return default;
      }

      var ids = _wayspotAnchorController.CreateWayspotAnchors(localPoses);
      var wayspotAnchors = await GetCreatedWayspotAnchors(ids);

      return wayspotAnchors;
    }

    /// Restores previously created wayspot anchors via the payload from them
    /// @param wayspotAnchorPayloads The payloads from the wayspot anchors used to restore them
    /// @return The restored wayspot anchors
    public IWayspotAnchor[] RestoreWayspotAnchors(params WayspotAnchorPayload[] wayspotAnchorPayloads)
    {
      if (LocalizationState != LocalizationState.Localized)
      {
        ARLog._Error(_mustLocaliseBeforeCreatingAnchorErrorMessage);
        return default;
      }

      var wayspotAnchors = _wayspotAnchorController.RestoreWayspotAnchors(wayspotAnchorPayloads);
      AddWayspotAnchors(wayspotAnchors);
      return wayspotAnchors;
    }

    /// Destroys existing wayspot anchors
    /// @param anchors The wayspot anchors to destroy
    public void DestroyWayspotAnchors(params IWayspotAnchor[] anchors)
    {
      var ids = anchors.Select(a => a.ID);
      DestroyWayspotAnchors(ids.ToArray());
    }

    /// Destroys wayspot anchors by ID
    /// @param ids The IDs of the wayspot anchors to destroy
    public void DestroyWayspotAnchors(params Guid[] ids)
    {
      var wayspotAnchors = _wayspotAnchors.Values.Where
          (a => Array.IndexOf(ids, a.ID) >= 0)
        .ToArray();

      _wayspotAnchorController.PauseTracking(wayspotAnchors);
      foreach (var id in ids)
      {
        _wayspotAnchors[id].Dispose();
        _wayspotAnchors.Remove(id);
        _wayspotAnchorStatusCodes.Remove(id);
      }
    }

    /// Gets all of the wayspot anchors
    /// @return All of the wayspot anchors
    public IWayspotAnchor[] GetAllWayspotAnchors()
    {
      return _wayspotAnchors.Values.ToArray();
    }

    /// Gets a wayspot anchor by its ID
    /// @param id The ID of the wayspot anchor to retrieve
    /// @return The wayspot anchor
    public IWayspotAnchor GetWayspotAnchor(Guid id)
    {
      if (_wayspotAnchors.ContainsKey(id))
      {
        return _wayspotAnchors[id];
      }
      else
      {
        ARLog._Error($"Wayspot Anchor {id} does not exist.");
        return default;
      }
    }

    /// Restarts VPS
    public async void Restart()
    {
      _wayspotAnchorController.StopVps();
      _locationService.Stop();
      while (LocalizationState != LocalizationState.Failed)
      {
        await Task.Delay(1);
      }
      _locationService.Start();
      _wayspotAnchorController.StartVps(_wayspotAnchorsConfiguration);
    }

    /// Disposes of the Wayspot Anchor Service
    public void Dispose()
    {
      _arSession.Paused -= HandleARSessionPaused;
      _arSession.Ran -= HandleARSessionRan;
    }

    private WayspotAnchorController CreateWayspotAnchorController()
    {
      var wayspotAnchorController = new WayspotAnchorController(_arSession, _locationService);
      wayspotAnchorController.LocalizationStateUpdated += HandleLocalizationStateUpdated;
      wayspotAnchorController.WayspotAnchorsCreated += HandleWayspotAnchorsCreated;
      wayspotAnchorController.WayspotAnchorsTrackingUpdated += HandleWayspotAnchorsResolved;
      wayspotAnchorController.WayspotAnchorStatusUpdated += HandleWayspotAnchorStatusUpdated;

      wayspotAnchorController.StartVps(_wayspotAnchorsConfiguration);

      return wayspotAnchorController;
    }

    private void HandleSessionDeinitialized(ARSessionDeinitializedArgs arSessionDeinitializedArgs)
    {
      DestroyWayspotAnchors(_wayspotAnchors.Keys.ToArray());
      _arSession.Deinitialized -= HandleSessionDeinitialized;
      _wayspotAnchorController.LocalizationStateUpdated -= HandleLocalizationStateUpdated;
      _wayspotAnchorController.WayspotAnchorsCreated -= HandleWayspotAnchorsCreated;
      _wayspotAnchorController.WayspotAnchorsTrackingUpdated -= HandleWayspotAnchorsResolved;
      _wayspotAnchorController.WayspotAnchorStatusUpdated -= HandleWayspotAnchorStatusUpdated;
    }

    private void HandleLocalizationStateUpdated(LocalizationStateUpdatedArgs localizationStateUpdatedArgs)
    {
      LocalizationState = localizationStateUpdatedArgs.State;
      if (LocalizationState == LocalizationState.Failed)
      {
        ARLog._Error($"Localization has failed: {localizationStateUpdatedArgs.FailureReason}");
      }
    }

    private void HandleWayspotAnchorsCreated(WayspotAnchorsCreatedArgs wayspotAnchorsCreatedArgs)
    {
      AddWayspotAnchors(wayspotAnchorsCreatedArgs.WayspotAnchors);
    }

    private void AddWayspotAnchors(params IWayspotAnchor[] anchors)
    {
      _wayspotAnchorController.ResumeTracking(anchors);
      foreach (var anchor in anchors)
      {
        if (!_wayspotAnchors.ContainsKey(anchor.ID))
        {
          _wayspotAnchors.Add(anchor.ID, anchor);
        }

        if (!_wayspotAnchorStatusCodes.ContainsKey(anchor.ID))
        {
          _wayspotAnchorStatusCodes.Add(anchor.ID, WayspotAnchorStatusCode.Success);
        }
      }
    }

    private void HandleWayspotAnchorsResolved(WayspotAnchorsResolvedArgs wayspotAnchorsResolvedArgs)
    {
      foreach (var resolution in wayspotAnchorsResolvedArgs.Resolutions)
      {
        if (!_wayspotAnchors.ContainsKey(resolution.ID) || !_wayspotAnchorStatusCodes.ContainsKey(resolution.ID))
        {
          continue;
        }

        var wayspotAnchor = _wayspotAnchors[resolution.ID];
        if (_wayspotAnchorStatusCodes[resolution.ID] == WayspotAnchorStatusCode.Success)
        {
          wayspotAnchor.TrackingStateUpdated?.Invoke(resolution);
        }
      }
    }

    private void HandleWayspotAnchorStatusUpdated(WayspotAnchorStatusUpdatedArgs wayspotAnchorStatusUpdatedArgs)
    {
      foreach (var wayspotStatus in wayspotAnchorStatusUpdatedArgs.WayspotAnchorStatusUpdates)
      {
        _wayspotAnchorStatusCodes[wayspotStatus.ID] = wayspotStatus.Code;
        switch (wayspotStatus.Code)
        {
          case WayspotAnchorStatusCode.Failed:
            ARLog._Error($"Wayspot Anchor has failed: {wayspotStatus.ID}");
            break;

          case WayspotAnchorStatusCode.Invalid:
            break;

          case WayspotAnchorStatusCode.Limited:
            break;

          case WayspotAnchorStatusCode.Pending:
            break;

          case WayspotAnchorStatusCode.Success:
            break;

          default:
            throw new InvalidEnumArgumentException
            (
              $"Invalid wayspot anchor status code provided for {wayspotStatus.ID}: {wayspotStatus.Code}"
            );
        }
      }
    }

    private async Task<IWayspotAnchor[]> GetCreatedWayspotAnchors(Guid[] ids)
    {
      IWayspotAnchor[] wayspotAnchors = null;
      var task = TaskUtility.WaitUntil
      (
        () =>
        {
          wayspotAnchors = _wayspotAnchors.Where
              (a => ids.Contains(a.Key))
            .Select(a => a.Value)
            .ToArray();

          return wayspotAnchors.Length == ids.Length;
        }
      );

      var timeout = Task.Delay(1000);
      await Task.WhenAny(task, timeout);

      return wayspotAnchors;
    }
  }
}
