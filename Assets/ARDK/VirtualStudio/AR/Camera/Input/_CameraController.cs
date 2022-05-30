// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;

using Niantic.ARDK.VirtualStudio.AR;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Niantic.ARDK.VirtualStudio.AR.Camera.Input
{
  internal class _CameraController: MonoBehaviour
  {
#if ENABLE_INPUT_SYSTEM
    private Transform cameraTransform;
    private Mouse currentMouse;
    private Keyboard currentKeyboard;

    private void Awake()
    {
      cameraTransform = transform;
      currentMouse = Mouse.current;
      currentKeyboard = Keyboard.current;
    }

    private void Update()
    {
      RotateScroll();
      RotateDrag();
      Move();
    }

    private void Move()
    {
      cameraTransform.position += Time.deltaTime * _MockCameraConfiguration.MoveSpeed * GetMoveInput();
    }

    private void RotateDrag()
    {
      var isPressed = currentMouse.rightButton.isPressed ||
                      currentMouse.rightButton.wasPressedThisFrame;
      if (isPressed)
      {
          var mouseDeltaDirection = new Vector2(currentMouse.delta.x.ReadValue(), -1 * currentMouse.delta.y.ReadValue());
          Rotate(mouseDeltaDirection, _MockCameraConfiguration.LookSpeed);
      }
    }

    private void RotateScroll()
    {
      if (Mouse.current.scroll.ReadValue().magnitude > 0.01f) // not sure why but if this is 0, the camera tracks the mouse cursor.
      {
          var mouseScrollDelta = 
              new Vector2
              (
                  currentMouse.scroll.x.ReadValue(), 
                  currentMouse.scroll.y.ReadValue()
              );

          Rotate
          (
              direction:mouseScrollDelta * _MockCameraConfiguration.ScrollDirection,
              speed:_MockCameraConfiguration.LookSpeed / 10
          );
      }
    }

    private void Rotate(Vector2 direction, float speed)
    {
      var pitchVector = Time.deltaTime * speed * direction.y;
      var position = cameraTransform.position;
      cameraTransform.RotateAround(position, cameraTransform.right, pitchVector);

      var yawVector = Time.deltaTime * speed * direction.x;
      cameraTransform.RotateAround(position, Vector3.up, yawVector);
    }

    private Vector3 GetMoveInput()
    {
      var input = Vector3.zero;

      var isWPressed = currentKeyboard.wKey.isPressed ||
                       currentKeyboard.wKey.wasPressedThisFrame;
      if (isWPressed)
      {
        input += cameraTransform.forward;
      }

      var isSPressed = currentKeyboard.sKey.isPressed ||
                       currentKeyboard.sKey.wasPressedThisFrame;
      if (isSPressed)
      {
        input -= cameraTransform.forward;
      }

      var isAPressed = currentKeyboard.aKey.isPressed ||
                       currentKeyboard.aKey.wasPressedThisFrame;
      if (isAPressed)
      {
        input -= cameraTransform.right;
      }

      var isDPressed = currentKeyboard.dKey.isPressed ||
                       currentKeyboard.dKey.wasPressedThisFrame;
      if (isDPressed)
      {
        input += cameraTransform.right;
      }

      var isQPressed = currentKeyboard.qKey.isPressed ||
                       currentKeyboard.qKey.wasPressedThisFrame;
      if (isQPressed)
      {
        input -= Vector3.up;
      }

      var isEPressed = currentKeyboard.eKey.isPressed ||
                       currentKeyboard.eKey.wasPressedThisFrame;
      if (isEPressed)
      {
        input += Vector3.up;
      }

      return input;
    }
#endif
  }
}