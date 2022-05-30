// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections;
using System.Collections.Generic;

using Niantic.ARDK.VirtualStudio.AR;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Camera.Input
{
  internal class _LegacyCameraController: MonoBehaviour
  {
#if ENABLE_LEGACY_INPUT_MANAGER
    private const string MouseHorizontalAxis = "Mouse X";
    private const string MouseVerticalAxis = "Mouse Y";
    
    private Transform _cameraTransform;

    private void Awake()
    {
      _cameraTransform = gameObject.transform;
    }

    private void Update()
    {
      RotateScroll();
      RotateDrag();
      Move();
    }

    private void RotateScroll()
    {
      var scrollSpeed = _MockCameraConfiguration.LookSpeed / 2;
      var mouseScrollVector = UnityEngine.Input.mouseScrollDelta * _MockCameraConfiguration.ScrollDirection;
      Rotate(mouseScrollVector, scrollSpeed);
    }

    private void RotateDrag()
    {
      if (UnityEngine.Input.GetMouseButton(1))
      {
        var dragDelta = new Vector2
          (UnityEngine.Input.GetAxis(MouseHorizontalAxis), -UnityEngine.Input.GetAxis(MouseVerticalAxis));

        Rotate(dragDelta, _MockCameraConfiguration.LookSpeed);
      }
    }

    private void Rotate(Vector2 direction, float speed)
    {
      var pitchVector = Time.deltaTime * speed * direction.y;
      _cameraTransform.RotateAround
        (_cameraTransform.position, _cameraTransform.right, pitchVector);

      var yawVector = Time.deltaTime * speed * direction.x;
      _cameraTransform.RotateAround(_cameraTransform.position, Vector3.up, yawVector);
    }

    private void Move()
    {
      _cameraTransform.position +=
        Time.deltaTime * _MockCameraConfiguration.MoveSpeed * GetMoveInput();
    }

    private Vector3 GetMoveInput()
    {
      var input = Vector3.zero;

      if (UnityEngine.Input.GetKey(KeyCode.W))
        input += _cameraTransform.forward;

      if (UnityEngine.Input.GetKey(KeyCode.S))
        input -= _cameraTransform.forward;

      if (UnityEngine.Input.GetKey(KeyCode.A))
        input -= _cameraTransform.right;

      if (UnityEngine.Input.GetKey(KeyCode.D))
        input += _cameraTransform.right;

      if (UnityEngine.Input.GetKey(KeyCode.Q))
        input -= Vector3.up;

      if (UnityEngine.Input.GetKey(KeyCode.E))
        input += Vector3.up;

      return input;
    }
#endif
  }
}
