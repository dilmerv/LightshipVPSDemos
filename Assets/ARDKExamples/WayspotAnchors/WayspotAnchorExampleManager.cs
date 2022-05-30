// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.HitTest;
using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities.Input.Legacy;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.WayspotAnchors
{
  public class WayspotAnchorExampleManager: MonoBehaviour
  {
    [Tooltip("The anchor that will be placed")]
    [SerializeField]
    private GameObject _anchorPrefab;

    [Tooltip("Camera used to place the anchors via raycasting")]
    [SerializeField]
    private Camera _camera;

    [Tooltip("Text used to display the current status of the demo")]
    [SerializeField]
    private Text _statusLog;

    [Tooltip("Text used to show the current localization state")]
    [SerializeField]
    private Text _localizationStatus;

    private WayspotAnchorService _wayspotAnchorService;
    private IARSession _arSession;
    private LocalizationState _localizationState;

    private readonly Dictionary<Guid, GameObject> _wayspotAnchorGameObjects =
      new Dictionary<Guid, GameObject>();

    private void Awake()
    {
      // This is necessary for setting the user id associated with the current user. 
      // We strongly recommend generating and using User IDs. Accurate user information allows
      //  Niantic to support you in maintaining data privacy best practices and allows you to
      //  understand usage patterns of features among your users.  
      // ARDK has no strict format or length requirements for User IDs, although the User ID string
      //  must be a UTF8 string. We recommend avoiding using an ID that maps back directly to the
      //  user. So, for example, don’t use email addresses, or login IDs. Instead, you should
      //  generate a unique ID for each user. We recommend generating a GUID.
      // When the user logs out, clear ARDK's user id with ArdkGlobalConfig.ClearUserIdOnLogout

      //  Sample code:
      //  // GetCurrentUserId() is your code that gets a user ID string from your login service
      //  var userId = GetCurrentUserId(); 
      //  ArdkGlobalConfig.SetUserIdOnLogin(userId);

      _statusLog.text = "Initializing Session.";
    }

    private void OnEnable()
    {
      ARSessionFactory.SessionInitialized += HandleSessionInitialized;
    }

    private void Update()
    {
      if (_wayspotAnchorService == null)
      {
        return;
      }
      //Get the pose where you tap on the screen
      var success = TryGetTouchInput(out Matrix4x4 localPose);
      if (_wayspotAnchorService.LocalizationState == LocalizationState.Localized)
      {
        if (success) //Check is screen tap was a valid tap
        {
          PlaceAnchor(localPose); //Create the Wayspot Anchor and place the GameObject
        }
      }
      else
      {
        if (success) //Check is screen tap was a valid tap
        {
          _statusLog.text = "Must localize before placing anchor.";
        }
        if(_localizationState != _wayspotAnchorService.LocalizationState)
        {
          _wayspotAnchorGameObjects.Values.ToList().ForEach(a => a.SetActive(false));
        }
      }
            _localizationStatus.text = _wayspotAnchorService.LocalizationState.ToString();
      _localizationState = _wayspotAnchorService.LocalizationState;
    }

    private void OnDisable()
    {
      ARSessionFactory.SessionInitialized -= HandleSessionInitialized;
    }

    private void OnDestroy()
    {
      _wayspotAnchorService.Dispose();
    }

    /// Saves all of the existing wayspot anchors
    public void SaveWayspotAnchors()
    {
      if (_wayspotAnchorGameObjects.Count > 0)
      {
        var wayspotAnchors = _wayspotAnchorService.GetAllWayspotAnchors();
        var payloads = wayspotAnchors.Select(a => a.Payload);
        WayspotAnchorDataUtility.SaveLocalPayloads(payloads.ToArray());
      }
      else
      {
        WayspotAnchorDataUtility.SaveLocalPayloads(Array.Empty<WayspotAnchorPayload>());
      }

      _statusLog.text = "Saved Wayspot Anchors.";
    }

    /// Loads all of the saved wayspot anchors
    public void LoadWayspotAnchors()
    {
      if (_wayspotAnchorService.LocalizationState != LocalizationState.Localized)
      {
        _statusLog.text = "Must localize before loading anchors.";
        return;
      }

      var payloads = WayspotAnchorDataUtility.LoadLocalPayloads();
      if (payloads.Length > 0)
      {
        var wayspotAnchors = _wayspotAnchorService.RestoreWayspotAnchors(payloads);
        CreateAnchorGameObjects(wayspotAnchors);
        _statusLog.text = "Loaded Wayspot Anchors.";
      }
      else
      {
        _statusLog.text = "No anchors to load.";
      }
    }

    /// Clears all of the active wayspot anchors
    public void ClearAnchorGameObjects()
    {
      if (_wayspotAnchorGameObjects.Count == 0)
      {
        _statusLog.text = "No anchors to clear.";
        return;
      }

      foreach (var anchor in _wayspotAnchorGameObjects)
      {
        Destroy(anchor.Value);
      }

      _wayspotAnchorService.DestroyWayspotAnchors(_wayspotAnchorGameObjects.Keys.ToArray());
      _wayspotAnchorGameObjects.Clear();
      _statusLog.text = "Cleared Wayspot Anchors.";
    }

    /// Pauses the AR Session
    public void PauseARSession()
    {
      if (_arSession.State == ARSessionState.Running)
      {
        _arSession.Pause();
        _statusLog.text = $"AR Session Paused.";
      }
      else
      {
        _statusLog.text = $"Cannot pause AR Session.";
      }
    }

    /// Resumes the AR Session
    public void ResumeARSession()
    {
      if (_arSession.State == ARSessionState.Paused)
      {
        _arSession.Run(_arSession.Configuration);
        _statusLog.text = $"AR Session Resumed.";
      }
      else
      {
        _statusLog.text = $"Cannot resume AR Session.";
      }
    }

    /// Restarts Wayspot Anchor Service
    public void RestartWayspotAnchorService()
    {
      _wayspotAnchorService.Restart();
    }

    private void HandleSessionInitialized(AnyARSessionInitializedArgs anyARSessionInitializedArgs)
    {
      _statusLog.text = "Running Session...";
      _arSession = anyARSessionInitializedArgs.Session;
      _arSession.Ran += HandleSessionRan;
    }

    private void HandleSessionRan(ARSessionRanArgs arSessionRanArgs)
    {
      _arSession.Ran -= HandleSessionRan;
      _wayspotAnchorService = CreateWayspotAnchorService();
      _statusLog.text = "Session Initialized.";
    }

    private void PlaceAnchor(Matrix4x4 localPose)
    {
      _wayspotAnchorService.CreateWayspotAnchors(CreateAnchorGameObjects, localPose);
      // Alternatively, you can make this method async and create wayspot anchors using await:
      // var wayspotAnchors = await _wayspotAnchorService.CreateWayspotAnchorsAsync(localPose);
      // CreateAnchorGameObjects(wayspotAnchors);

      _statusLog.text = "Anchor placed.";
    }

    private WayspotAnchorService CreateWayspotAnchorService()
    {
      var wayspotAnchorsConfiguration = WayspotAnchorsConfigurationFactory.Create();
      
      var locationService = LocationServiceFactory.Create(_arSession.RuntimeEnvironment);
      locationService.Start();

      var wayspotAnchorService = new WayspotAnchorService(_arSession, locationService, wayspotAnchorsConfiguration);
      return wayspotAnchorService;
    }

    private void CreateAnchorGameObjects(IWayspotAnchor[] wayspotAnchors)
    {
      foreach (var wayspotAnchor in wayspotAnchors)
      {
        if (_wayspotAnchorGameObjects.ContainsKey(wayspotAnchor.ID))
        {
          continue;
        }
        wayspotAnchor.TrackingStateUpdated += HandleWayspotAnchorTrackingUpdated;
        var id = wayspotAnchor.ID;
        var anchor = Instantiate(_anchorPrefab);
        anchor.SetActive(false);
        anchor.name = $"Anchor {id}";
        _wayspotAnchorGameObjects.Add(id, anchor);
      }
    }

    private void HandleWayspotAnchorTrackingUpdated(WayspotAnchorResolvedArgs wayspotAnchorResolvedArgs)
    {
      var anchor = _wayspotAnchorGameObjects[wayspotAnchorResolvedArgs.ID].transform;
      anchor.position = wayspotAnchorResolvedArgs.Position;
      anchor.rotation = wayspotAnchorResolvedArgs.Rotation;
      anchor.gameObject.SetActive(true);
    }

    private bool TryGetTouchInput(out Matrix4x4 localPose)
    {
      if (_arSession == null || PlatformAgnosticInput.touchCount <= 0)
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      var touch = PlatformAgnosticInput.GetTouch(0);
      if (touch.IsTouchOverUIObject())
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      if (touch.phase != TouchPhase.Began)
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      var currentFrame = _arSession.CurrentFrame;
      if (currentFrame == null)
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      var results = currentFrame.HitTest
      (
        _camera.pixelWidth,
        _camera.pixelHeight,
        touch.position,
        ARHitTestResultType.ExistingPlane
      );

      int count = results.Count;
      if (count <= 0)
      {
        localPose = Matrix4x4.zero;
        return false;
      }

      var result = results[0];
      localPose = result.WorldTransform;
      return true;
    }
  }
}
