// Copyright 2022 Niantic, Inc. All Rights Reserved.
#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;

namespace Niantic.ARDK.Utilities.Input.InputSystem
{
    public class InputInteractionHandler : MonoBehaviour
    {
        private InputInteractions _inputInteractions;

        public delegate void StartPrimaryTouch(Vector2 position);
        public event StartPrimaryTouch OnStartPrimaryTouch;

        private void Awake()
        {
            _inputInteractions = new InputInteractions();
            _inputInteractions.Player.LeftClick.performed += StartTouchOnDetection;
        }

        private void StartTouchOnDetection(InputAction.CallbackContext obj)
        {
            var position = _inputInteractions.Player.TouchPosition.ReadValue<Vector2>();
            OnStartPrimaryTouch?.Invoke(position);
        }

        private void OnEnable()
        {
            _inputInteractions.Enable();
        }

        private void OnDisable()
        {
            _inputInteractions.Disable();
        }
    }
}
#endif