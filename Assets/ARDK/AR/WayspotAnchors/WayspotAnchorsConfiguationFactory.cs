// Copyright 2022 Niantic, Inc. All Rights Reserved.
namespace Niantic.ARDK.AR.WayspotAnchors
{
  /// Class factory for [WayspotAnchorsConfiguration]
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  public static class WayspotAnchorsConfigurationFactory
  {
    /// Initializes a new instance of the WayspotAnchorsConfiguration class.
    public static IWayspotAnchorsConfiguration Create()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return new _NativeWayspotAnchorsConfiguration();

#pragma warning disable 0162
      return new _SerializableWayspotAnchorsConfiguration();
#pragma warning restore 0162
    }
  }
}
