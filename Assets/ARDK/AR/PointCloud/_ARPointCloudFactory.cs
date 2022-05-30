// Copyright 2022 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.PointCloud
{
  internal static class _ARPointCloudFactory
  {
    internal static _SerializableARPointCloud _AsSerializable(this IARPointCloud pointCloud)
    {
      var existsAndIsSerializable =
        pointCloud == null ||
        pointCloud is _SerializableARPointCloud;
      
      if (existsAndIsSerializable)
        return (_SerializableARPointCloud)pointCloud;

      return new _SerializableARPointCloud
      (
        pointCloud.Points,
        pointCloud.Identifiers,
        pointCloud.WorldScale
      );
    }
  }
}
