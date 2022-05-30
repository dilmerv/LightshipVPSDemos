// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ReferenceImage;
using Niantic.ARDK.Utilities.Logging;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  [ExecuteInEditMode]
  public sealed class MockImageAnchor:
    MockAnchorBase, ISerializationCallbackReceiver
  {
    [SerializeField]
    private string _name;

    [SerializeField]
    private Orientation _orientation;

    [SerializeField]
    private Texture2D _image;

    private Texture2D _currImage;
    private bool _imageDirty;

    private readonly Vector3[] _defaultVertices =
    {
      new Vector3(-0.5f, 0, -0.5f), // BL
      new Vector3(0.5f, 0,  -0.5f),  // BR
      new Vector3(-0.5f, 0, 0.5f),  // TL
      new Vector3(0.5f, 0, 0.5f)    // TR
    };

    // Mesh is built directly in this class to make sure aspect ratio is the same as the image's.
    private Vector3[] _vertices;

    private readonly int[] _triangles = { 0, 2, 1, 2, 3, 1 };

    private Vector2[] _uv =
    {
      new Vector2(0, 0),
      new Vector2(1, 0),
      new Vector2(0, 1),
      new Vector2(1, 1)
    };

    private _SerializableARImageAnchor _anchorData;

    protected override IARAnchor AnchorData
    {
      get => _anchorData;
    }

    private MeshFilter _meshFilter;
    private MeshFilter _MeshFilter
    {
      get
      {
        if (_meshFilter == null)
          _meshFilter = GetComponent<MeshFilter>();

        return _meshFilter;
      }
    }

    private MeshRenderer _meshRenderer;
    private MeshRenderer _MeshRenderer
    {
      get
      {
        if (_meshRenderer == null)
          _meshRenderer = GetComponent<MeshRenderer>();

        return _meshRenderer;
      }
    }

    protected override bool Initialize()
    {
      Build();
      UpdateImageDisplay();

      return base.Initialize();
    }

    protected override void Update()
    {
      if (_imageDirty)
        UpdateImageDisplay();
    }

    internal override void CreateAndAddAnchorToSession(_IMockARSession arSession)
    {
      if (_anchorData == null)
      {
        // Initialize the anchor data with initial values such as
        // a new guid, non-transform related values, etc

        var imageAsBytes = _image.GetRawTextureData();

        var vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
        var botLeft = transform.localToWorldMatrix * vertices[0];
        var topRight = transform.localToWorldMatrix * vertices[3];
        var imageWidth = Mathf.Abs(botLeft.x - topRight.x);
        var imageHeight = Mathf.Abs(botLeft.y - topRight.y);

        var referenceImage =
          (_SerializableARReferenceImage)ARReferenceImageFactory.Create
          (
            _name,
            imageAsBytes,
            imageAsBytes.Length,
            imageWidth,
            _orientation
          );

        referenceImage.PhysicalSize = new Vector2(imageWidth, imageHeight);

        _anchorData =
          new _SerializableARImageAnchor
          (
            new Matrix4x4(),
            Guid.NewGuid(),
            referenceImage
          );

        // Transform value will be set here
        UpdateAnchorData();

        // Value starts off as true, so needs to be set to false here
        transform.hasChanged = false;
      }

      if (!arSession.AddAnchor(_anchorData))
      {
        ARLog._WarnFormat
        (
          "Image anchor for {0} cannot be detected. If that is unintended, make sure that a " +
          "reference image with the same name as this anchor's was added to the active " +
          "ARWorldTrackingConfiguration's DetectionImages set.",
          false,
          gameObject.name
        );

        enabled = false;
      }
    }

    internal override void RemoveAnchorFromSession(_IMockARSession arSession)
    {
      arSession.RemoveAnchor(_anchorData);
    }

    protected override bool UpdateAnchorData()
    {
      // Note: transform.hasChanged is susceptible to other logic changing this flag's value
      // but for the time being, this is also the most straightforward approach for detecting
      // changes to this GameObject's transform component
      if (_anchorData == null || !transform.hasChanged)
        return false;

      _anchorData.Transform =
        Matrix4x4.TRS
        (
          transform.position,
          transform.rotation,
          Vector3.one
        );

      transform.hasChanged = false;
      return true;
    }

#if UNITY_EDITOR
    private bool _created;
    private void Reset()
    {
      if (!_created)
      {
        if (!CreateDialog())
          return;
      }

      _created = true;
      _vertices = null;
      Build();

      _orientation = Orientation.Up;

      var parent = transform.parent;
      transform.localRotation = Quaternion.Euler(-90, 0, 0);

      // Unparent from everything to set scale to [1, 1, 1]
      transform.SetParent(null, true);
      transform.localScale = Vector3.one;

      // Reparent while keeping the scale the same.
      if (parent != null)
        transform.SetParent(parent, true);
    }

    private bool CreateDialog()
    {
      var numComponents = GetComponents<Component>().Length;
      var hasMeshFilter = GetComponent<MeshFilter>() != null;
      var hasMeshRenderer = GetComponent<MeshRenderer>() != null;

      if (hasMeshFilter || hasMeshRenderer || numComponents > 2)
      {
        var goName = gameObject.name;

        string msg;
        if (hasMeshFilter || hasMeshRenderer)
        {
          msg =
            "A MeshFilter or MeshRenderer component already exists on this GameObject. Adding a " +
            "MockImageAnchor will alter their properties.";
        }
        else
        {
          msg = "Adding a MockImageAnchor will alter this GameObject's transform.";
        }

        var option = EditorUtility.DisplayDialogComplex
        (
          $"Create MockImageAnchor on {goName}?",
          msg,
          "Create",
          "Cancel",
          "Create on new child GameObject"
        );

        switch (option)
        {
          // Create
          case 0:
            return true;

          // Cancel
          case 1:
            DestroyImmediate(this);
            return false;

          // Create on new child
          case 2:
            var child = new GameObject();
            child.transform.SetParent(transform, false);
            DestroyImmediate(this);
            child.AddComponent<MockImageAnchor>();

            Undo.RegisterCreatedObjectUndo(child, "Create MockImageAnchor");
            Selection.activeObject = child;
            return false;
        }
      }

      return true;
    }

    private void OnValidate()
    {
      if (_image != _currImage)
        _imageDirty = true;
    }
#endif

    // OnBeforeSerialize is called often while in Edit Mode, essentially whenever it's
    // open in the Inspector view, but it's also only called after Reset when a component
    // is first added to a GameObject. That makes it perfect to use to check if Reset is being
    // invoked from a a pre-existing or new component.
    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
      _created = true;
#endif
    }

    public void OnAfterDeserialize()
    {
    }

    [ContextMenu("Update Display")]
    private void UpdateImageDisplay()
    {
      _currImage = _image;

      if (_currImage == null)
      {
        UpdateVertices(0.5f, 0.5f);
        return;
      }

      _MeshRenderer.sharedMaterial.mainTexture = _currImage;

      // Match aspect ratio, but keep longest side to 1m so importing a huge image
      // isn't annoying.
      var width = (float)_currImage.width;
      var height = (float)_currImage.height;

      var mockWidth = 1f;
      var mockHeight = 1f;

      if (width > height)
        mockHeight = height / width;
      else if (width < height)
        mockWidth = width / height;

      UpdateVertices(mockWidth, mockHeight);

      _imageDirty = false;
    }

    private void Build()
    {
      var mesh = new Mesh();

      if (_MeshFilter == null)
        gameObject.AddComponent<MeshFilter>();

      _MeshFilter.mesh = mesh;

      mesh.vertices = (_vertices == null || _vertices.Length == 0) ? _defaultVertices : _vertices;
      mesh.uv = _uv;
      mesh.triangles = _triangles;

      mesh.RecalculateNormals();
      mesh.RecalculateBounds();


      if (_MeshRenderer == null)
        gameObject.AddComponent<MeshRenderer>();

      _MeshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
    }

    private void UpdateVertices(float width, float height)
    {
      var xExtent = width / 2;
      var yExtent = height / 2;

      _vertices = new Vector3[]
      {
        new Vector3(-xExtent, 0, -yExtent), // BL
        new Vector3(xExtent, 0, -yExtent),  // BR
        new Vector3(-xExtent, 0, yExtent),  // TL
        new Vector3(xExtent, 0, yExtent)    // TR
      };

      _MeshFilter.sharedMesh.vertices = _vertices;
    }
  }
}
