// Copyright 2022 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.LightEstimate
{
  internal static class _ARLightEstimateFactory
  {
    internal static _SerializableARLightEstimate _AsSerializable(this IARLightEstimate source)
    {
      if (source == null)
        return null;

      if (source is _SerializableARLightEstimate possibleResult)
        return possibleResult;
      
      return
        new _SerializableARLightEstimate
        (
          source.AmbientIntensity,
          source.AmbientColorTemperature,
          source.ColorCorrection
        );
    }
  }
}
