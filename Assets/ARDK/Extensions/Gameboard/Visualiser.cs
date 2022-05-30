// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Gameboard
{
  internal sealed class Visualiser
  {
    private IGameboard _gameboard;
    private GameboardModel _model;

    private List<GameObject> _pathDebugObjects;
    private List<GameObject> _unusedPathDebugObjects;
    private LineRenderer _lineRenderer;

    private GameObject _visualRoot;
    private MeshFilter _meshFilter = new MeshFilter();

    private bool _active;

    public Visualiser(IGameboard gameboard, GameboardModel model, bool active)
    {
      _model = model;
      _gameboard = gameboard;

      if (active)
        CreateNewDebugObjects();

      _active = active;

      if (active)
        _gameboard.GameboardUpdated += OnGameboardSurfaceUpdate;
    }

    public void Destroy()
    {
      if (_pathDebugObjects == null)
        return;

      foreach (var obj in _pathDebugObjects)
        GameObject.Destroy(obj);

      _pathDebugObjects.Clear();

      foreach (var obj in _unusedPathDebugObjects)
        GameObject.Destroy(obj);

      _unusedPathDebugObjects.Clear();

      if (_meshFilter != null)
        GameObject.Destroy(_meshFilter.gameObject);
      GameObject.Destroy(_visualRoot);
    }

    public void SetActive(bool active)
    {
      _active = active;

      if (_active)
      {
        if (_meshFilter == null)
          CreateNewDebugObjects();

        _gameboard.GameboardUpdated += OnGameboardSurfaceUpdate;
        UpdateDebugMesh(_model.Surfaces, _meshFilter.mesh);
      }
      else
      {
        _gameboard.GameboardUpdated -= OnGameboardSurfaceUpdate;
        _meshFilter.mesh.Clear();
      }

      _lineRenderer.enabled = _active;

      foreach (var sphere in _pathDebugObjects)
        sphere.SetActive(_active);
    }

    private void OnGameboardSurfaceUpdate(GameboardUpdatedArgs args)
    {
      if (args.PruneOrClear)
      {
        _meshFilter.mesh.Clear();
        return;
      }

      UpdateDebugMesh(_model.Surfaces, _meshFilter.mesh);
    }

    private void CreateNewDebugObjects()
    {
      _visualRoot = new GameObject();
      _visualRoot.name = "Gameboard Visualisation";
      _visualRoot.transform.position = Vector3.zero;

      _pathDebugObjects = new List<GameObject>();
      _unusedPathDebugObjects = new List<GameObject>();

      _lineRenderer = _visualRoot.AddComponent<LineRenderer>();
      _lineRenderer.widthCurve = new AnimationCurve(new Keyframe(0, 0.1f));
      _lineRenderer.material.color = Color.black;
      _lineRenderer.positionCount = 0;

      GameObject debugMeshGameObject = new GameObject();
      debugMeshGameObject.transform.SetParent(_visualRoot.transform, false);
      debugMeshGameObject.name = "GameboardDebug";
      MeshRenderer renderer = debugMeshGameObject.AddComponent<MeshRenderer>();
      renderer.material.color = Color.green;
      renderer.material.shader = Shader.Find("Unlit/Color");
      _meshFilter = debugMeshGameObject.AddComponent<MeshFilter>();
      _meshFilter.mesh.MarkDynamic();
    }

    #region TilesDrawing

    public void UpdateDebugMesh(List<Surface> surfaces, Mesh mesh)
    {
      float offset = 0.002f;

      var vertices = new List<Vector3>();
      var triangles = new List<int>();
      var vIndex = 0;
      var halfSize = _gameboard.Settings.TileSize / 2.0f;
      foreach (var surface in surfaces)
      {
        foreach (var center in surface.Elements.Select
          (node => Utils.TileToPosition(node.Coordinates, surface.Elevation, _gameboard.Settings.TileSize)))
        {
          // Vertices
          vertices.Add
            (center + new Vector3(-halfSize + offset, 0.0f, -halfSize + offset));

          vertices.Add(center + new Vector3(halfSize - offset, 0.0f, -halfSize + offset));
          vertices.Add(center + new Vector3(halfSize - offset, 0.0f, halfSize - offset));
          vertices.Add(center + new Vector3(-halfSize + offset, 0.0f, halfSize - offset));


          // Indices
          triangles.Add(vIndex + 2);
          triangles.Add(vIndex + 1);
          triangles.Add(vIndex);

          triangles.Add(vIndex);
          triangles.Add(vIndex + 3);
          triangles.Add(vIndex + 2);

          vIndex += 4;
        }
      }

      mesh.Clear();
      if (vertices.Count >= 4)
      {
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.UploadMeshData(markNoLongerReadable: false);
      }
    }

    #endregion

    #region PathDrawing

    private void DrawLine(List<Waypoint> path)
    {
      _lineRenderer.positionCount = path.Count;
      Vector3[] positions = new Vector3[path.Count];
      for (int index = 0; index < path.Count; index++)
      {
        positions[index] = path[index].WorldPosition;
      }
      _lineRenderer.SetPositions(positions);
    }

    private GameObject CreateSphere(Vector3 position)
    {
      GameObject waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      Object.Destroy(waypoint.GetComponent<Collider>());

      waypoint.transform.localScale = _gameboard.Settings.TileSize * 0.5f * Vector3.one;
      waypoint.transform.position = position;
      waypoint.transform.SetParent(_visualRoot.transform);

      return waypoint;
    }

    private void MoveSphereToCache(GameObject unusedSphere)
    {
      unusedSphere.transform.position = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
      _unusedPathDebugObjects.Add(unusedSphere);
      _pathDebugObjects.RemoveAt(_pathDebugObjects.Count-1);
    }

    private void MoveSphereFromCache(GameObject usedSphere, Vector3 position)
    {
      usedSphere.transform.position = position;
      _unusedPathDebugObjects.RemoveAt(0);
      _pathDebugObjects.Add(usedSphere);
    }

    public void DrawPath(Path path)
    {
      if (path.PathStatus == Path.Status.PathInvalid)
      {
        while (_pathDebugObjects.Count > 0)
        {
          MoveSphereToCache(_pathDebugObjects.Last());
        }

        _lineRenderer.positionCount = 0;
        return;
      }

      DrawLine(path.Waypoints);

      for (int index = 0; index < path.Waypoints.Count; index++)
      {
        if (index < _pathDebugObjects.Count)
        {
          _pathDebugObjects[index].transform.position = path.Waypoints[index].WorldPosition;
        }
        else
        {
          if (_unusedPathDebugObjects.Count > 0)
          {
            GameObject waypoint = _unusedPathDebugObjects[0];
            MoveSphereFromCache(waypoint, path.Waypoints[index].WorldPosition);
          }
          else
          {
            GameObject waypointDebugSphere = CreateSphere(path.Waypoints[index].WorldPosition);
            _pathDebugObjects.Add(waypointDebugSphere);
          }
        }
      }

      if (path.Waypoints.Count < _pathDebugObjects.Count)
      {
        for (int index = _pathDebugObjects.Count - 1; index > path.Waypoints.Count - 1; index--)
        {
          GameObject unusedWaypoint = _pathDebugObjects[index];
          MoveSphereToCache(unusedWaypoint);
        }
      }
    }


    #endregion
  }
}
