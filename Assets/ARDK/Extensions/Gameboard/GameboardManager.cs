// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Serialization;

namespace Niantic.ARDK.Extensions.Gameboard
{
  /// This helper can be placed in a scene to easily create and update a Gameboard. Other scripts
  ///  can subscribe to GameboardFactory.OnGameboardCreated to access the created Gameboard.
  /// It will trigger regular scans of the environment in front of the ARCamera based on the scan
  ///  settings. The Gameboard will add and remove tiles based on the Gameboard settings.
  /// Scanning can be enabled/disabled by calling EnableFeatures() / DisableFeatures().
  /// Gameboard debug visibility can be toggled on and off. This includes Gameboard tiles, paths,
  ///  and gizmos of the scanning rays
  public class GameboardManager: UnityLifecycleDriver
  {
#pragma warning disable 649
    [FormerlySerializedAs("_arCamera")]
    [SerializeField]
    [Tooltip("The scene camera used to render AR content.")]
    private Camera _camera;
#pragma warning restore 649

    public IGameboard Gameboard { get; private set; }

    [Header("Gameboard Settings")]
    [SerializeField]
    [Tooltip("Metric size of a grid tile containing one node")]
    [Min(0.0000001f)]
    private float _tileSize = 0.15f;

    [SerializeField]
    [Tooltip("Tolerance to consider floor as flat despite meshing noise")]
    [Min(0.0000001f)]
    private float _flatFloorTolerance = 0.2f;

    [SerializeField]
    [Tooltip("Maximum slope angle (degrees) an area can have and still be considered flat")]
    [Range(0, 40)]
    private float _maxSlope = 25.0f;

    [SerializeField]
    [Tooltip("The maximum amount two cells can differ in elevation and still be considered on the same plane")]
    [Min(0.0000001f)]
    private float _stepHeight = 0.1f;

    [Header("Scan Settings")]
    [SerializeField]
    private float _scanInterval = 0.1f;

    [SerializeField]
    private float _scanRange = 1.5f;

    [SerializeField]
    [Tooltip("Must be the same layer as meshes.")]
    private LayerMask _layerMask = ~0;

    [Header("Debug")]
    [SerializeField]
    public bool _visualise = true;

    private bool recreateGameboard = false;

#region GetterAndSetter
    public Camera Camera
    {
      get => _camera;
      set => _camera = value;
    }

    [Obsolete("Use Camera property instead.")]
    public Camera ArCamera
    {
      get => _camera;
      set => _camera = value;
    }

    /// The value specifying the size (meters) of a grid tile containing one node.
    public float TileSize
    {
      get => _tileSize;
      set
      {
        if (value <= 0f)
          throw new ArgumentOutOfRangeException(nameof(value), "TileSize must be greater than 0.");

        _tileSize = value;
      }
    }

    /// The value specifying the amount of tolerance for meshing noise allowed to consider the floor as flat.
    public float FlatFloorTolerance
    {
      get => _flatFloorTolerance;
      set => _flatFloorTolerance = value;
    }

    /// The value specifying the maximum slope angle (degrees) an area can have and still be considered flat
    public float MaxSlope
    {
      get => _maxSlope;
      set => _maxSlope = value;
    }

    /// The value specifying the maximum amount two cells can differ in elevation and still be considered on the same plane.
    public float StepHeight
    {
      get => _stepHeight;
      set => _stepHeight = value;
    }

    public float ScanInterval
    {
      get => _scanInterval;
      set => _scanInterval = value;
    }

    public float ScanRange
    {
      get => _scanRange;
      set => _scanRange = value;
    }

    public LayerMask LayerMask
    {
      get => _layerMask;
      set => _layerMask = value;
    }

    /// The value specifying whether to render the debug Gameboard visualization or not.
    public bool Visualise
    {
      get => _visualise;
      set => _visualise = value;
    }
#endregion

#region OnValidateVariables
    private Camera _prevArCamera;
    private float _prevTileSize;
    private float _prevFlatFloorTolerance;
    private float _prevMaxSlope;
    private float _prevStepHeight;
    private float _prevScanInterval;
    private float _prevScanRange;
    private LayerMask _prevLayerMask;
    private bool _prevVisualise;
#endregion

    private ModelSettings _modelSettings;

    private float _lastScan;

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      _prevArCamera = _camera;
      _prevTileSize = _tileSize;
      _prevFlatFloorTolerance = _flatFloorTolerance;
      _prevMaxSlope = _maxSlope;
      _prevStepHeight = _stepHeight;
      _prevScanInterval = _scanInterval;
      _prevScanRange = _scanRange;
      _prevLayerMask = _layerMask;
      _prevVisualise = _visualise;

      CreateNewGameboard();
    }

    protected override void DeinitializeImpl()
    {
      base.DeinitializeImpl();
      DestroyGameboard();
    }

    /// Activate/deactivate visualisation of scan raycasts, Gameboard tiles and paths.
    public void SetVisualisationActive(bool active)
    {
      Gameboard?.SetVisualisationActive(active);
    }

    /// This function triggers a scan of the environment to update the Gameboard model, by
    ///  adding new free tiles and removing newly occupied tiles. The scan is done in front of
    ///  the ARCamera.
    private void UpdateGameboard()
    {
      var cameraTransform = _camera.transform;
      var playerPosition = cameraTransform.position;
      var playerForward = cameraTransform.forward;

      // The origin of the scan should be in front of the player
      var origin = playerPosition + Vector3.ProjectOnPlane(playerForward, Vector3.up).normalized;

      // Scan the environment
      Gameboard.Scan(origin, range: _scanRange);
    }

    private void Update()
    {
      if (!AreFeaturesEnabled || Gameboard == null)
        return;

      if (!(Time.time - _lastScan > _scanInterval))
        return;

      _lastScan = Time.time;
      UpdateGameboard();

      if (recreateGameboard)
      {
        DestroyGameboard();
        CreateNewGameboard();
        recreateGameboard = false;
      }
    }

    /// Destroys the existing Gameboard
    public void DestroyGameboard()
    {
      if (Gameboard == null)
      {
        ARLog._Warn("No Gameboard to destroy.");
        return;
      }

      Gameboard.Destroy();
      Gameboard = null;
    }

    /// Creates a new Gameboard using the GameboardFactory
    public void CreateNewGameboard()
    {
      _modelSettings =
        new ModelSettings
        (
          _tileSize,
          _flatFloorTolerance,
          _maxSlope,
          _stepHeight,
          _layerMask
        );

      Gameboard = GameboardFactory.Create(_modelSettings, _visualise);
    }

    private void OnValidate()
    {
      // this means initializeImpl is not finished yet;
      if (_prevTileSize == 0)
        return;

      var configChanged = false;

      if (_tileSize != _prevTileSize)
      {
        _prevTileSize = _tileSize;
        configChanged = true;
      }

      if (_flatFloorTolerance != _prevFlatFloorTolerance)
      {
        _prevFlatFloorTolerance = _flatFloorTolerance;
        configChanged = true;
      }

      if (_maxSlope != _prevMaxSlope)
      {
        _prevMaxSlope = _maxSlope;
        configChanged = true;
      }

      if (_stepHeight != _prevStepHeight)
      {
        _prevStepHeight = _stepHeight;
        configChanged = true;
      }

      if (configChanged)
      {
        recreateGameboard = true;
      }
    }
  }
}
