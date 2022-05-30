// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR.WayspotAnchors;

using UnityEngine;

namespace Niantic.ARDKExamples.WayspotAnchors
{
  public static class WayspotAnchorDataUtility
  {
    private const string DataKey = "wayspot_anchor_payloads";
    
    public static void SaveLocalPayloads(WayspotAnchorPayload[] wayspotAnchorPayloads)
    {
      var wayspotAnchorsData = new WayspotAnchorsData();
      wayspotAnchorsData.Payloads = wayspotAnchorPayloads.Select(a => a.Serialize()).ToArray();
      string wayspotAnchorsJson = JsonUtility.ToJson(wayspotAnchorsData);
      PlayerPrefs.SetString(DataKey, wayspotAnchorsJson);
    }

    public static WayspotAnchorPayload[] LoadLocalPayloads()
    {
      if (PlayerPrefs.HasKey(DataKey))
      {
        var payloads = new List<WayspotAnchorPayload>();
        var json = PlayerPrefs.GetString(DataKey);
        var wayspotAnchorsData = JsonUtility.FromJson<WayspotAnchorsData>(json);
        foreach (var wayspotAnchorPayload in wayspotAnchorsData.Payloads)
        {
          var payload = WayspotAnchorPayload.Deserialize(wayspotAnchorPayload);
          payloads.Add(payload);
        }

        return payloads.ToArray();
      }
      else
      {
        return Array.Empty<WayspotAnchorPayload>();
      }
    }

    public static void ClearLocalPayloads()
    {
      if (PlayerPrefs.HasKey(DataKey))
      {
        PlayerPrefs.DeleteKey(DataKey); 
      }
    }
    
    [Serializable]
    private class WayspotAnchorsData
    {
      /// The payloads to save via JsonUtility
      public string[] Payloads = Array.Empty<string>();
    }
  }
}
