// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  /// The wayspot anchor payload is the container for data used to save and recover wayspot anchors, via the RecoverWayspotAnchor methods
  public class WayspotAnchorPayload
  {
    /// The blob of data sent to C++ to create a waypoint anchor.  This is retrieved from the initially created waypoint anchor
    internal byte[] _Blob { get; }

    /// Creates a new payload for a waypoint anchor
    /// @param blob The data blob to create the payload with
    internal WayspotAnchorPayload(byte[] blob)
    {
      _Blob = blob;
    }

    /// Serializes the payload into a string that can be stored in external storage for later recovery
    /// @return The string to save
    public string Serialize()
    {
      string data = Convert.ToBase64String(_Blob);

      return data;
    }

    /// Deserializes the previously serialized payload back into a payload
    /// @param data The string from a previously serialized payload
    /// The payload created from the data
    public static WayspotAnchorPayload Deserialize(string data)
    {
      var blob = Convert.FromBase64String(data);
      var payload = new WayspotAnchorPayload(blob);

      return payload;
    }
  }
}
