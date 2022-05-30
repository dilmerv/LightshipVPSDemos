// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Image;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

using MathUtils = Niantic.ARDK.Utilities.MathUtils;

#if ARDK_HAS_URP
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;

using Niantic.ARDK.Rendering.SRP;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  using Camera = UnityEngine.Camera;
  internal sealed class _MockFrameBufferProvider:
    IDisposable
  {
    // ARSession data
    private readonly _MockARSession _arSession;
    private readonly float _timeBetweenFrames;
    private float _timeSinceLastFrame;
    private _SerializableARCamera _cachedSerializedCamera;
    private readonly Transform _camerasRoot;

    // Image buffer
    private Camera _imageCamera;
    private CameraIntrinsics _imageIntrinsics;
    private RenderTexture _imageRT;
    private RenderTexture _imageFlippedRT;
    private Shader _flipImageShader;
    private Material _flipImageMaterial;

    // Depth buffer
    private bool _generateDepth;
    private readonly float _timeBetweenDepthUpdates;
    private float _timeSinceLastDepthUpdate;
    private Camera _depthCamera;
    private CameraIntrinsics _depthIntrinsics;
    private RenderTexture _depthRT;
    private RenderTexture _depthOnlyRT;
    private Shader _depthToDisparityShader;
    private Material _depthToDisparityMaterial;

    // Semantics buffer
    private bool _generateSemantics;
    private readonly float _timeBetweenSemanticsUpdates;
    private float _timeSinceLastSemanticsUpdate;
    private Camera _semanticsCamera;
    private CameraIntrinsics _semanticsIntrinsics;
    private Shader _semanticsShader;
    private RenderTexture _semanticsRT;
    private Texture2D _semanticsTex;
    private string[] _channelNames;

    // Magic numbers to define the image resolution of mock frames
    internal const int _ARImageWidth = 1920;
    internal const int _ARImageHeight = 1440;
    internal const int _SensorFocalLength = 26;

    // Awareness Model Params (updated ARDK 0.10):
    private const int _ModelWidth = 256;
    private const int _ModelHeight = 144;
    private const float _ModelNearDistance = 0.2f;
    private const float _ModelFarDistance = 100f;

    public const string MOCK_LAYER_NAME = "ARDK_MockWorld";

    public const string MOCK_LAYER_MISSING_MSG =
      "No ARDK_MockWorld layer found in the Layers list (Edit > ProjectSettings > Tags and Layers)";

    public _MockFrameBufferProvider(_MockARSession mockARSession, Transform camerasRoot)
    {
      _arSession = mockARSession;
      _arSession.Ran += CheckRunConfiguration;
      _timeBetweenFrames = 1f / _MockCameraConfiguration.FPS;

      if (mockARSession.Configuration is _SerializableARWorldTrackingConfiguration worldTrackingConfiguration)
      {
        _timeBetweenDepthUpdates = 1f / worldTrackingConfiguration.DepthTargetFrameRate;
        _timeBetweenSemanticsUpdates = 1f / worldTrackingConfiguration.SemanticTargetFrameRate;
      }

      _camerasRoot = camerasRoot;
      InitializeImageGeneration();

      _UpdateLoop.Tick += Update;
    }

    private void CheckRunConfiguration(ARSessionRanArgs args)
    {
      if (_arSession.Configuration is IARWorldTrackingConfiguration worldTrackingConfiguration)
      {
        _generateDepth = worldTrackingConfiguration.IsDepthEnabled;
        _generateSemantics = worldTrackingConfiguration.IsSemanticSegmentationEnabled;
      }
      else
      {
        _generateDepth = false;
        _generateSemantics = false;
      }

      if (_generateDepth && _depthCamera == null)
        InitializeDepthGeneration();

      if (_generateSemantics && _semanticsCamera == null)
        InitializeSemanticsGeneration();

      if (_depthCamera != null)
      {
        _depthCamera.enabled = _generateDepth;
        _depthCamera.depthTextureMode =
          _generateDepth ? DepthTextureMode.Depth : DepthTextureMode.None;
      }

      if (_semanticsCamera != null)
      {
        _semanticsCamera.enabled = _generateSemantics;
      }
    }

    private void InitializeImageGeneration()
    {
      // Instantiate a new Unity camera
      _imageCamera = CreateCameraBase("Image");

      // Configure the camera to use physical properties
      _imageCamera.usePhysicalProperties = true;
      _imageCamera.focalLength = _SensorFocalLength;
      _imageCamera.nearClipPlane = 0.1f;
      _imageCamera.farClipPlane = 100f;

      // Infer the orientation of the editor
      var editorOrientation = Screen.width > Screen.height
        ? ScreenOrientation.Landscape
        : ScreenOrientation.Portrait;

      // Rotate the 'device' to the UI orientation
      _imageCamera.transform.localRotation = MathUtils.CalculateViewRotation
      (
        from: ScreenOrientation.Landscape,
        to: editorOrientation
      ).ToRotation();

      // Set up rendering offscreen to render texture.
      _imageRT = new RenderTexture(_ARImageWidth, _ARImageHeight, 16, RenderTextureFormat.BGRA32);
      _imageFlippedRT = new RenderTexture(_ARImageWidth, _ARImageHeight, 16, RenderTextureFormat.BGRA32);
      _imageRT.Create();
      _imageFlippedRT.Create();
      
      _flipImageShader = Resources.Load<Shader>("FlipImage");
      _flipImageMaterial = new Material(_flipImageShader);
      _imageCamera.targetTexture = _imageRT;
      
      // This needs to be called AFTER we set the target texture
      _imageIntrinsics = MathUtils.CalculateIntrinsics(_imageCamera);

      // Reading this property's value is equivalent to calling
      // the CalculateProjectionMatrix method.
      var projection = MathUtils.CalculateProjectionMatrix
      (
        _imageIntrinsics,
        _ARImageWidth,
        _ARImageHeight,
        Screen.width,
        Screen.height,
        RenderTarget.ScreenOrientation,
        _imageCamera.nearClipPlane,
        _imageCamera.farClipPlane
      );
      
      // Initialize the view matrix.
      // This will be updated in every frame.
      var initialView = GetMockViewMatrix(_imageCamera);

      var imageResolution = new Resolution
      {
        width = _ARImageWidth, height = _ARImageHeight
      };

      _cachedSerializedCamera = new _SerializableARCamera
      (
        TrackingState.Normal,
        TrackingStateReason.None,
        imageResolution,
        imageResolution,
        _imageIntrinsics,
        _imageIntrinsics,
        initialView.inverse,
        projectionMatrix: projection,
        estimatedViewMatrix: initialView,
        worldScale: 1.0f
      );
    }

    private void InitializeDepthGeneration()
    {
      _depthCamera = CreateAwarenessCamera("Depth");
      _depthCamera.depthTextureMode = DepthTextureMode.Depth;

      var editorOrientation = Screen.width > Screen.height
        ? ScreenOrientation.Landscape
        : ScreenOrientation.Portrait;

      // Rotate the 'device' to the UI orientation
      _depthCamera.transform.localRotation = MathUtils.CalculateViewRotation
      (
        from: ScreenOrientation.Landscape,
        to: editorOrientation
      ).ToRotation();

      _depthRT =
      new RenderTexture
      (
        _ModelWidth,
        _ModelHeight,
        16,
        RenderTextureFormat.Depth
      );

    _depthOnlyRT =
      new RenderTexture
      (
        _ModelWidth,
        _ModelHeight,
        0,
        RenderTextureFormat.RFloat
      );

      _depthToDisparityShader = Resources.Load<Shader>("UnityToMetricDepth");
      _depthToDisparityMaterial = new Material(_depthToDisparityShader);

      var farDividedByNear = _ModelFarDistance / _ModelNearDistance;
      _depthToDisparityMaterial.SetFloat("_ZBufferParams_Z", (-1 + farDividedByNear) / _ModelFarDistance);
      _depthToDisparityMaterial.SetFloat("_ZBufferParams_W", 1 / _ModelFarDistance);

      _depthCamera.targetTexture = _depthRT;
      _depthIntrinsics = MathUtils.CalculateIntrinsics(_depthCamera);
    }

    private void InitializeSemanticsGeneration()
    {
      _semanticsCamera = CreateAwarenessCamera("Semantics");
      _semanticsCamera.clearFlags = CameraClearFlags.SolidColor;
      _semanticsCamera.backgroundColor = new Color(0, 0, 0, 0);

      var editorOrientation = Screen.width > Screen.height
        ? ScreenOrientation.Landscape
        : ScreenOrientation.Portrait;

      // Rotate the 'device' to the UI orientation
      _semanticsCamera.transform.localRotation = MathUtils.CalculateViewRotation
        (
          from: ScreenOrientation.Landscape,
          to: editorOrientation
        ).ToRotation();

      _semanticsRT =
        new RenderTexture
        (
          _ModelWidth,
          _ModelHeight,
          16,
          RenderTextureFormat.ARGB32
        );

      _semanticsRT.Create();
      _semanticsCamera.targetTexture = _semanticsRT;

      _semanticsShader = Resources.Load<Shader>("Segmentation");
      _semanticsCamera.SetReplacementShader(_semanticsShader, String.Empty);

      _semanticsTex = new Texture2D(_ModelWidth, _ModelHeight, TextureFormat.ARGB32, false);

      _semanticsIntrinsics = MathUtils.CalculateIntrinsics(_semanticsCamera);

      SetupReplacementRenderer();

      _channelNames = Enum.GetNames(typeof(MockSemanticLabel.ChannelName));
    }

    private void SetupReplacementRenderer()
    {
#if ARDK_HAS_URP
      if (!_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
        return;

      var rendererIndex =
        _RenderPipelineInternals.GetRendererIndex
        (
          _RenderPipelineInternals.REPLACEMENT_RENDERER_NAME
        );

      if (rendererIndex < 0)
      {
        ARLog._Error
        (
          "Cannot generate mock semantic segmentation buffers unless the ArdkUrpAssetRenderer" +
          " is added to the Renderer List."
        );

        return;
      }

      _semanticsCamera.GetUniversalAdditionalCameraData().SetRenderer(rendererIndex);
#endif
    }

    private Camera CreateCameraBase(string name)
    {
      var cameraObject = new GameObject(name);
      cameraObject.transform.SetParent(_camerasRoot);

      var camera = cameraObject.AddComponent<Camera>();
      camera.depth = int.MinValue;

#if UNITY_EDITOR
      var layerIndex = LayerMask.NameToLayer(MOCK_LAYER_NAME);
      if (layerIndex < 0)
      {
        if (!CreateLayer(MOCK_LAYER_NAME, out layerIndex))
          return null;
      }
      camera.cullingMask = LayerMask.GetMask(MOCK_LAYER_NAME);
#endif

      return camera;
    }

    private Camera CreateAwarenessCamera(string name)
    {
      var camera = CreateCameraBase(name);

      camera.nearClipPlane = _ModelNearDistance;
      camera.farClipPlane = _ModelFarDistance;
      camera.usePhysicalProperties = true;
      camera.focalLength = _SensorFocalLength;

      return camera;
    }

    private bool _isDisposed;
    public void Dispose()
    {
      if (_isDisposed)
        return;

      _isDisposed = true;

      _imageRT.Release();
      _imageFlippedRT.Release();

      if (_depthCamera != null)
      {
        GameObject.Destroy(_depthCamera.gameObject);
        _depthRT.Release();
        _depthOnlyRT.Release();
      }

      if (_semanticsCamera != null)
      {
        GameObject.Destroy(_semanticsCamera.gameObject);
        _semanticsRT.Release();
      }
    }

    private static Matrix4x4 GetMockViewMatrix(Camera serializedCamera)
    {
      var rotation = MathUtils.CalculateViewRotation
      (
        from: ScreenOrientation.Landscape,
        to: RenderTarget.ScreenOrientation
      );

      var narView = serializedCamera.worldToCameraMatrix.ConvertViewMatrixBetweenNarAndUnity();

      return rotation * narView;
    }

    private void Update()
    {
      if (_arSession != null && _arSession.State == ARSessionState.Running)
      {
        _timeSinceLastFrame += Time.deltaTime;
        if (_timeSinceLastFrame >= _timeBetweenFrames)
        {
          _timeSinceLastFrame = 0;

          var mockViewMatrix = GetMockViewMatrix(_imageCamera);
          _cachedSerializedCamera._estimatedViewMatrix = mockViewMatrix;
          _cachedSerializedCamera.Transform = mockViewMatrix.inverse;

          _SerializableDepthBuffer depthBuffer = null;
          if (_generateDepth && Time.time - _timeSinceLastDepthUpdate > _timeBetweenDepthUpdates)
          {
            depthBuffer = _GetDepthBuffer();
            _timeSinceLastDepthUpdate = Time.time;
          }

          _SerializableSemanticBuffer semanticBuffer = null;
          if (_generateSemantics && Time.time - _timeSinceLastSemanticsUpdate > _timeBetweenSemanticsUpdates)
          {
            semanticBuffer = _GetSemanticBuffer();
            _timeSinceLastSemanticsUpdate = Time.time;
          }

          var serializedFrame = new _SerializableARFrame
          (
            capturedImageBuffer: _GetImageBuffer(),
            depthBuffer: depthBuffer,
            semanticBuffer: semanticBuffer,
            camera: _cachedSerializedCamera,
            lightEstimate: null,
            anchors: null,
            maps: null,
            worldScale: 1.0f,
            estimatedDisplayTransform: MathUtils.CalculateDisplayTransform
            (
              _ARImageWidth,
              _ARImageHeight,
              Screen.width,
              Screen.height,
              RenderTarget.ScreenOrientation,
              invertVertically: true
            )
          );

          _arSession.UpdateFrame(serializedFrame);
        }
      }
    }

    private _SerializableImageBuffer _GetImageBuffer()
    {
      Graphics.Blit(_imageRT, _imageFlippedRT, _flipImageMaterial);
      
      var imageData =
        new NativeArray<byte>
        (
          _ARImageWidth * _ARImageHeight * 4,
          Allocator.Persistent,
          NativeArrayOptions.UninitializedMemory
        );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref imageData, AtomicSafetyHandle.Create());
#endif

      AsyncGPUReadback.RequestIntoNativeArray(ref imageData, _imageFlippedRT).WaitForCompletion();

      var plane =
        new _SerializableImagePlane
        (
          imageData,
          _ARImageWidth,
          _ARImageHeight,
          _ARImageWidth * 4,
          4
        );

      var buffer =
        new _SerializableImageBuffer
        (
          ImageFormat.BGRA,
          new _SerializableImagePlanes(new[] { plane }),
          75
        );

      return buffer;
    }

    private _SerializableDepthBuffer _GetDepthBuffer()
    {
      Graphics.Blit(_depthRT, _depthOnlyRT, _depthToDisparityMaterial);

      var depthData = new NativeArray<float>
      (
        _ModelWidth * _ModelHeight,
        Allocator.Persistent,
        NativeArrayOptions.UninitializedMemory
      );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref depthData, AtomicSafetyHandle.Create());
#endif

      AsyncGPUReadback.RequestIntoNativeArray(ref depthData, _depthOnlyRT).WaitForCompletion();

      var buffer = new _SerializableDepthBuffer
      (
        _ModelWidth,
        _ModelHeight,
        isKeyframe:true,
        GetMockViewMatrix(_imageCamera),
        depthData,
        _ModelNearDistance,
        _ModelFarDistance,
        _depthIntrinsics
      )
      {
        IsRotatedToScreenOrientation = true
      };

      return buffer;
    }

    private _SerializableSemanticBuffer _GetSemanticBuffer()
    {
       var data = new NativeArray<uint>
       (
         _ModelWidth * _ModelHeight,
         Allocator.Persistent,
         NativeArrayOptions.UninitializedMemory
       );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref data, AtomicSafetyHandle.Create());
#endif

      // Doing this in the CPU is slower, but I couldn't figure out how to get
      // the correct uint value out of a shader. Performance is sufficient.
      var currRT = RenderTexture.active;
      RenderTexture.active = _semanticsRT;

      _semanticsTex.ReadPixels(new Rect(0, 0, _ModelWidth, _ModelHeight), 0, 0);
      _semanticsTex.Apply();

      RenderTexture.active = currRT;

      var byteArray = _semanticsTex.GetPixels32();
      for (var i = 0; i < byteArray.Length; i++)
      {
        data[i] = MockSemanticLabel.ToInt(byteArray[i]);
      }

      var buffer = new _SerializableSemanticBuffer
      (
        _ModelWidth,
        _ModelHeight,
        isKeyframe:true,
        GetMockViewMatrix(_imageCamera),
        data,
        _channelNames,
        _semanticsIntrinsics
      )
      {
        IsRotatedToScreenOrientation = true
      };

      return buffer;
    }

#if UNITY_EDITOR
    internal static void RemoveMockFromCullingMask(Camera cam)
    {
      // get the input layer name's index, which should range from 0 to Unity's max # of layers minus 1
      int layerIndex = LayerMask.NameToLayer(MOCK_LAYER_NAME);

      if (layerIndex >= 0 || CreateLayer(MOCK_LAYER_NAME, out layerIndex))
      {
        // perform a guardrail check to see if the mock layer
        // is included in the ar camera's culling mask
        if ((cam.cullingMask & (1 << layerIndex)) != 0)
        {
          // in the case that the mock layer is included, remove it from the culling mask
          cam.cullingMask &= ~(1 << layerIndex);
        }
      }
    }

    public static bool CreateLayer(string layerName, out int layerIndex)
    {
      layerIndex = -1;
      if (LayerMask.NameToLayer(layerName) >= 0)
      {
        ARLog._WarnFormat("Layer: {0} already exists in TagManager.", false, layerName);
        return false;
      }

      var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
      if (assets.Length == 0)
      {
        ARLog._ErrorFormat
        (
          "No ProjectSettings/TagManager.asset file found to add Layer: {0} required for use " +
          "of Virtual Studio Mock mode.",
          false,
          layerName
        );

        return false;
      }

      var tagManager = new SerializedObject(assets[0]);
      var layers = tagManager.FindProperty("layers");

      // First 7 layers are reserved for Unity's built-in layers.
      for (int i = 8, j = layers.arraySize; i < j; i++)
      {
        var layerProp = layers.GetArrayElementAtIndex(i);
        if (string.IsNullOrEmpty(layerProp.stringValue))
        {
          layerProp.stringValue = layerName;
          tagManager.ApplyModifiedProperties();
          AssetDatabase.SaveAssets();

          ARLog._ReleaseFormat
          (
            "Layer: {0} has been added to your project for use with Virtual Studio Mock mode." +
            "See the Edit > Project Settings > Tags and Layers menu to verify.",
            false,
            layerName
          );

          layerIndex = i;
          return true;
        }
      }

      ARLog._ErrorFormat
      (
        "All user layer slots are in use. Unable to add Layer: {0} required for use " +
        "of Virtual Studio Mock mode.",
        layerName
      );

      return false;
    }
#endif
  }
}