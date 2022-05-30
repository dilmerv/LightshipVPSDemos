// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Niantic.ARDK.Configuration.Internal;

using UnityEngine;

namespace Niantic.ARDK.VPSCoverage.GeoserviceMessages
{
  [Serializable]
  internal class _LocalizationTargetsRequest
  {
    [SerializeField]
    private string[] query_id;

    [SerializeField]
    private ARCommonMetadataStruct ar_common_metadata;

    public _LocalizationTargetsRequest(string[] queryId, ARCommonMetadataStruct arCommonMetadata)
    {
      query_id = queryId;
      ar_common_metadata = arCommonMetadata;
    }

    public static string ReadFromFile()
    {
      string path = Path.Combine(Application.dataPath, "../../ARDK/Assets/ARDK/VpsCoverage/MockData/VpsCoverageTargets/VpsCoverageTargetsRequest.json");
      string json = File.ReadAllText(path);

      return json;
    }
  }
}