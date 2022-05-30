// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Collections.Generic;

using Niantic.ARDK.Extensions.Gameboard;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Input.Legacy;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
    public class GameboardExampleManager: MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        [Tooltip("The scenes ARCamera")]
        private Camera _arCamera;

        [SerializeField]
        [Tooltip("GameObject to instantiate as the agent")]
        private GameObject _agentPrefab;

        [SerializeField]
        [Tooltip("GameObject used to mark the agents set destination")]
        private GameObject _destinationMarker;

        [Header("UI")]
        [SerializeField]
        [Tooltip("Button to trigger placement or replacement of agent")]
        private Button _replaceButton;

        [SerializeField]
        [Tooltip("Text shown in replace button")]
        private Text _replaceButtonText;

        [SerializeField]
        [Tooltip("Button to call the agent")]
        private Button _callButton;

#pragma warning restore 0649

        private IGameboard _gameboard;
        private GameObject _agentGameObject;
        private GameboardAgent _agent;
        private bool _isReplacing;
        private bool _arIsRunning;
        private bool _gameboardIsRunning;

        /// Inform about started ARSession.
        public void ARSessionStarted()
        {
            _arIsRunning = true;
        }

        /// Inform about stopped ARSession, update UI and clear Gameboard.
        public void ARSessionStopped()
        {
            Destroy(_agentGameObject);
            _agentGameObject = null;

            _replaceButtonText.text = "Place";

            _replaceButton.interactable = false;
            _callButton.interactable = false;

            _isReplacing = false;
            _arIsRunning = false;

            _gameboard.Clear();
        }

        private void Awake()
        {
            GameboardFactory.GameboardInitialized += OnGameboardCreated;

            _callButton.interactable = false;
            _replaceButton.interactable = false;
            _replaceButtonText.text = "Place";
        }

        private void OnGameboardCreated(GameboardCreatedArgs args)
        {
            _gameboard = args.Gameboard;
            _gameboardIsRunning = true;
            _gameboard.GameboardDestroyed += OnGameboardDestroyed;
        }

        private void OnGameboardDestroyed(IArdkEventArgs args)
        {
            _gameboard = null;
            _gameboardIsRunning = false;
        }

        private void OnEnable()
        {
            _replaceButton.onClick.AddListener(ReplaceButton_OnClick);
            _callButton.onClick.AddListener(CallButton_OnClick);
        }

        private void OnDisable()
        {
            _replaceButton.onClick.RemoveListener(ReplaceButton_OnClick);
            _callButton.onClick.RemoveListener(CallButton_OnClick);
        }

        private void Update()
        {
            if (!_gameboardIsRunning)
                return;

            if (_isReplacing)
            {
                HandlePlacement();
            }
            else
            {
                // Only allow placing the actor if at least one surface is discovered
                _replaceButton.interactable = _gameboard.Area > 0;
                HandleTouch();
            }
        }

        private void HandleTouch()
        {
            //if there is a touch call our function
            if (PlatformAgnosticInput.touchCount <= 0)
                return;

            var touch = PlatformAgnosticInput.GetTouch(0);

            //if there is no touch or touch selects UI element
            if (PlatformAgnosticInput.touchCount <= 0 || EventSystem.current.currentSelectedGameObject != null)
                return;

            if (touch.phase == TouchPhase.Began)
            {
                TouchBegan(touch);
            }
        }

        private void TouchBegan(Touch touch)
        {
            if (!_arIsRunning || _agent == null || _arCamera == null)
                return;

            //as we are using meshing we can use a standard ray cast
            Ray ray = _arCamera.ScreenPointToRay(touch.position);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                _destinationMarker.SetActive(true);
                _destinationMarker.transform.position = hit.point;
                _agent.SetDestination(hit.point);
            }
        }

        private void HandlePlacement()
        {
            // Use this technique to place an object to a user-defined position.
          // Otherwise, use FindRandomPosition() to try to place the object automatically.

          // Get a ray pointing in the user's look direction
          var cameraTransform = _arCamera.transform;
          var ray = new Ray(cameraTransform.position, cameraTransform.forward);

          // Intersect the Gameboard with the ray
          if (_gameboard.RayCast(ray, out Vector3 hitPoint))
          {
            // Check whether the object can be fit in the resulting position
            if (_gameboard.CheckFit(center: hitPoint, 0.4f))
            {
                _agentGameObject.SetActive(true);
              _agentGameObject.transform.position = hitPoint;
              var rotation = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
              _agentGameObject.transform.rotation = Quaternion.LookRotation(-rotation);
              _replaceButton.interactable = true;
            }
          }
        }

        private void ReplaceButton_OnClick()
        {
            _destinationMarker.SetActive(false);

            if (_agentGameObject == null)
            {
                _agentGameObject = Instantiate(_agentPrefab);
                _agent = _agentGameObject.GetComponent<GameboardAgent>();
                _agent.State = GameboardAgent.AgentNavigationState.Paused;
                _agentGameObject.SetActive(false);
            }

            _isReplacing = !_isReplacing;
            _replaceButtonText.text = _isReplacing ? "Done" : "Replace";
            _replaceButton.interactable = !_isReplacing;
            _callButton.interactable = !_isReplacing;

            if (_isReplacing)
            {
                // invalidates path by path to itself for path debug reset
                _agent.SetDestination(_agentGameObject.transform.position);
                _agentGameObject.SetActive(false);
            }
            else
                _agent.State = GameboardAgent.AgentNavigationState.Idle;
        }

        private void CallButton_OnClick()
        {
            _destinationMarker.SetActive(false);
            _agent.SetDestination(_arCamera.transform.position);
        }
    }
}