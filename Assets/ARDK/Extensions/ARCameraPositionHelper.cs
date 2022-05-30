// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.Internals.EditorUtilities;
using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.Serialization;

namespace Niantic.ARDK.Extensions
{
  /// A helper component to automatically position the scene rendering AR content and transform its output
  public class ARCameraPositionHelper: MonoBehaviour
  {
    [FormerlySerializedAs("Camera")]
    [SerializeField]
    [_Autofill]
    [Tooltip("The scene camera used to render AR content.")]
    private Camera _camera;

    /// Returns a reference to the scene camera used to render AR content, if present.
    public Camera Camera
    {
      get => _camera;
      set => _camera = value;
    }

    private IARSession _session;

    private void Start()
    {
      ARSessionFactory.SessionInitialized += _OnSessionInitialized;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= _OnSessionInitialized;

      var session = _session;
      if (session != null)
        session.FrameUpdated -= _FrameUpdated;
    }

    private void _OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      var oldSession = _session;
      if (oldSession != null)
        oldSession.FrameUpdated -= _FrameUpdated;

      var newSession = args.Session;
      _session = newSession;
      newSession.FrameUpdated += _FrameUpdated;
    }

    private void _FrameUpdated(FrameUpdatedArgs args)
    {
      var localCamera = Camera;
      if (localCamera == null)
        return;

      var session = _session;
      if (session == null)
        return;

      // Set the camera's position.
      var worldTransform = args.Frame.Camera.GetViewMatrix(Screen.orientation).inverse;
      localCamera.transform.position = worldTransform.ToPosition();
      localCamera.transform.rotation = worldTransform.ToRotation();
    }
  }
}
