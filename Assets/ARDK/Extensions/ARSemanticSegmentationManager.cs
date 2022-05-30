// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR;

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.Internals.EditorUtilities;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Serialization;

namespace Niantic.ARDK.Extensions
{
  [DisallowMultipleComponent]
  public sealed class ARSemanticSegmentationManager:
    ARRenderFeatureProvider
  {
    [FormerlySerializedAs("_arCamera")]
    [SerializeField]
    [_Autofill]
    [Tooltip("The scene camera used to render AR content.")]
    private Camera _camera;

    [SerializeField]
    [Range(0, 60)]
    [Tooltip("How many times the semantic segmentation routine should target running per second.")]
    private uint _keyFrameFrequency = 20;

    [SerializeField]
    [HideInInspector]
    private string[] _depthSuppressionChannels;

    [SerializeField]
    [HideInInspector]
    [Tooltip("Whether the semantics buffer should synchronize with the camera pose.")]
    private InterpolationMode _interpolation = InterpolationMode.Smooth;

    [SerializeField]
    [HideInInspector]
    [Range(0.0f, 1.0f)]
    [Tooltip
      (
        "Sets whether to align semantics pixels with closer (0.1) or distant (1.0) pixels " +
        "in the color image (aka the back-projection distance)."
      )
    ]
    private float _interpolationPreference = AwarenessParameters.DefaultBackProjectionDistance;

    /// Returns a reference to the scene camera used to render AR content, if present.
    public Camera Camera
    {
      get => _camera;
      set
      {
        if (Initialized)
          throw new InvalidOperationException("Cannot set this property after this component is initialized.");

        _camera = value;
      }
    }

    /// The value specifying the how many times the semantic segmentation routine
    /// should target running per second.
    public uint KeyFrameFrequency
    {
      get => _keyFrameFrequency;
      set
      {
        if (value != _keyFrameFrequency)
        {
          _keyFrameFrequency = value;
          RaiseConfigurationChanged();
        }
      }
    }

    /// The value specifying whether the semantics buffer should synchronize with the camera pose.
    public InterpolationMode Interpolation
    {
      get => _interpolation;
      set
      {
        if (Initialized)
          throw new InvalidOperationException("Cannot set this property after this component is initialized.");

        _interpolation = value;
      }
    }

    /// The value specifying whether to align semantics pixels with closer (0.1)
    /// or distant (1.0) pixels in the color image (aka the back-projection distance).
    public float InterpolationPreference
    {
      get => _interpolationPreference;
      set
      {
        if (Initialized)
          throw new InvalidOperationException("Cannot set this property after this component is initialized.");

        if (value < 0f && value > 1f)
        {
          throw new ArgumentOutOfRangeException
          (
            nameof(value),
            "InterpolationPreference value must be between 0 and 1."
          );
        }

        _interpolationPreference = value;
      }
    }

    /// Sets the depth suppression channels. If there is an existing set of channels, calling
    /// this method will override them.
    public void SetDepthSuppressionChannels(params string[] channelNames)
    {
      if (GetComponent<ARDepthManager>() == null)
      {
        throw new InvalidOperationException
        (
          "An AR Depth Manager component is required to add depth suppression channels."
        );
      }

      if (_depthSuppressionChannels != null && _depthSuppressionChannels.Length > 0)
        ARLog._Debug("Overriding existing depth suppression channels.");

      _depthSuppressionChannels = channelNames;
    }

    /// Returns a reference to the depth suppression mask texture, if present.
    /// If the suppression feature is disabled, this returns null.
    public Texture DepthSuppressionTexture
    {
      get => _suppressionTexture;
    }

    public ISemanticBufferProcessor SemanticBufferProcessor
    {
      get => _GetOrCreateProcessor();
    }

    /// Event for when the first semantics buffer is received.
    public event ArdkEventHandler<ContextAwarenessArgs<ISemanticBuffer>> SemanticBufferInitialized;

    /// Event for when the contents of the semantic buffer or its affine transform was updated.
    public event
      ArdkEventHandler<ContextAwarenessStreamUpdatedArgs<ISemanticBuffer>> SemanticBufferUpdated;

    private SemanticBufferProcessor _semanticBufferProcessor;
    private SemanticBufferProcessor _GetOrCreateProcessor()
    {
      if (_semanticBufferProcessor == null)
      {
        _semanticBufferProcessor = new SemanticBufferProcessor(_camera)
        {
          InterpolationMode = _interpolation,
          InterpolationPreference = _interpolationPreference
        };
      }

      return _semanticBufferProcessor;
    }

    private Texture2D _suppressionTexture;
    private int[] _suppressionChannelIndices;

    protected override void InitializeImpl()
    {
      if (_camera == null)
      {
        var warning =
          "The Camera field was not set on the ARSemanticSegmentationManager before use. " +
          "Will default to use Unity's Camera.main";

        ARLog._Warn(warning);
        _camera = Camera.main;
      }

      _GetOrCreateProcessor();

      base.InitializeImpl();
    }

    protected override void DeinitializeImpl()
    {
      // Release the semantics processor
      _semanticBufferProcessor?.Dispose();

      if (_suppressionTexture != null)
        Destroy(_suppressionTexture);

      base.DeinitializeImpl();
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();
      _semanticBufferProcessor.AwarenessStreamBegan += OnSemanticBufferInitialized;
      _semanticBufferProcessor.AwarenessStreamUpdated += OnSemanticBufferUpdated;
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      if (_suppressionTexture != null)
        Destroy(_suppressionTexture);

      _semanticBufferProcessor.AwarenessStreamBegan -= OnSemanticBufferInitialized;
      _semanticBufferProcessor.AwarenessStreamUpdated -= OnSemanticBufferUpdated;
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      if (properties.ARConfiguration is IARWorldTrackingConfiguration worldConfig)
      {
        worldConfig.IsSemanticSegmentationEnabled = AreFeaturesEnabled;
        worldConfig.SemanticTargetFrameRate = _keyFrameFrequency;
      }
    }

    /// Invoked when this component is asked about the render features
    /// it is may be responsible for.
    /// @note: The implementation needs to include all features that is
    /// possible to manipulate with this component.
    protected override HashSet<string> OnAcquireFeatureSet()
    {
      return new HashSet<string>
      {
        FeatureBindings.DepthSuppression
      };
    }

    /// Invoked when it is time to calculate the actual features
    /// that this component currently manages.
    protected override RenderFeatureConfiguration OnEvaluateConfiguration()
    {
      // If the semantics manager is not active, all features are disabled
      if (!AreFeaturesEnabled)
        return new RenderFeatureConfiguration(null, featuresDisabled: Features);

      var enabledFeatures = new List<string>();

      // Is depth suppression enabled?
      if (_depthSuppressionChannels.Length > 0)
        enabledFeatures.Add(FeatureBindings.DepthSuppression);

      // All other features are considered disabled
      var disabledFeatures = new HashSet<string>(Features);
      disabledFeatures.ExceptWith(enabledFeatures);
      return new RenderFeatureConfiguration(enabledFeatures, disabledFeatures);
    }

    protected override void OnRenderTargetChanged(RenderTarget? target)
    {
      _GetOrCreateProcessor().AssignViewport(target ?? _camera);
    }

    /// Called when it is time to copy the current render state to the main rendering material.
    /// @param material Material used to render the frame.
    public override void UpdateRenderState(Material material)
    {
      if (_depthSuppressionChannels.Length == 0)
        return;

      material.SetTexture(PropertyBindings.DepthSuppressionMask, _suppressionTexture);
      material.SetMatrix(PropertyBindings.SemanticsTransform, _semanticBufferProcessor.SamplerTransform);
    }

    private void OnSemanticBufferInitialized(ContextAwarenessArgs<ISemanticBuffer> args)
    {
      // Currently just a pass-through
      SemanticBufferInitialized?.Invoke(args);
    }

    private void OnSemanticBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
    {
      // Avoid generating a suppression texture if suppression isn't enabled
      if (_depthSuppressionChannels.Length != 0)
      {
        // Acquire the typed buffer
        var semanticBuffer = args.Sender.AwarenessBuffer;

        // Determine whether we should update the list
        // of channels that are used to suppress depth
        var shouldUpdateSuppressionIndices = _suppressionChannelIndices == null ||
          _suppressionChannelIndices.Length != _depthSuppressionChannels.Length;

        // Update channel list
        if (shouldUpdateSuppressionIndices)
          _suppressionChannelIndices = _depthSuppressionChannels
            .Select(channel => semanticBuffer.GetChannelIndex(channel))
            .ToArray();

        // Update semantics on the GPU
        if (args.IsKeyFrame)
          semanticBuffer.CreateOrUpdateTextureARGB32
          (
            ref _suppressionTexture,
            _suppressionChannelIndices
          );
      }

      // Finally, let users know the manager has finished updating.
      SemanticBufferUpdated?.Invoke(args);
    }
  }
}
