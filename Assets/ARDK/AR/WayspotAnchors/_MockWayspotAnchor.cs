// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Text;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal class _MockWayspotAnchor: 
    IWayspotAnchor,
    _IInternalTrackable
  {
    /// Called when the position oof rotation of the mock wayspot anchor has been updated
    public ArdkEventHandler<WayspotAnchorResolvedArgs> TrackingStateUpdated { get; set; }

    /// Whether or not the mock anchor is currently being tracked
    public bool Tracking { get; private set; }

    /// Sets whether or not the mock anchor should be tracked
    /// @param tracking Whether or not to track the mock anchor
    /// @note This is an internal method
    void _IInternalTrackable.SetTrackingEnabled (bool tracking) //This method is internal, not private
    {
      Tracking = tracking;
    }

    /// Gets the mock anchor's local pose
    internal Matrix4x4 LocalPose { get; }

    /// Creates a mock anchor
    /// @param id The ID of the mock anchor to create
    /// @param localPose The local pose of the mock anchor
    public _MockWayspotAnchor(Guid id, Matrix4x4 localPose)
    {
      ID = id;
      LocalPose = localPose;
    }

    /// Creates a mock anchor
    /// @param blob The blob of data used to create the mock anchor
    public _MockWayspotAnchor(byte[] blob)
    {
      string json = Encoding.UTF8.GetString(blob);
      var mockWayspotAnchorData = JsonUtility.FromJson<_MockWayspotAnchorData>(json);
      string id = mockWayspotAnchorData._ID;
      var position = new Vector3
      (
        mockWayspotAnchorData._XPosition,
        mockWayspotAnchorData._YPosition,
        mockWayspotAnchorData._ZPosition
      );

      var rotation = new Vector3
      (
        mockWayspotAnchorData._XRotation,
        mockWayspotAnchorData._YRotation,
        mockWayspotAnchorData._ZRotation
      );

      var localPose = Matrix4x4.TRS(position, Quaternion.Euler(rotation), Vector3.one);
      ID = Guid.Parse(id);
      LocalPose = localPose;
    }

    /// Gets the ID of the mock anchor
    public Guid ID { get; }

    /// Gets the payload of the mock anchor
    /// @note This is a wrapper around the blob of data
    public WayspotAnchorPayload Payload
    {
      get
      {
        string id = ID.ToString();
        var position = LocalPose.ToPosition();
        var rotation = LocalPose.ToRotation().eulerAngles;
        var mockWayspotAnchorData = new _MockWayspotAnchorData()
        {
          _ID = id,
          _XPosition = position.x,
          _YPosition = position.y,
          _ZPosition = position.z,
          _XRotation = rotation.x,
          _YRotation = rotation.y,
          _ZRotation = rotation.z
        };

        string json = JsonUtility.ToJson(mockWayspotAnchorData);
        byte[] blob = Encoding.UTF8.GetBytes(json);
        var payload = new WayspotAnchorPayload(blob);

        return payload;
      }
    }

    /// The data class used to serialize/deserialize the payload
    [Serializable]
    public class _MockWayspotAnchorData
    {
      public string _ID;
      public float _XPosition;
      public float _YPosition;
      public float _ZPosition;
      public float _XRotation;
      public float _YRotation;
      public float _ZRotation;
    }

    /// Disposes the mock wayspot anchor
    public void Dispose()
    {
    }
  }
}
