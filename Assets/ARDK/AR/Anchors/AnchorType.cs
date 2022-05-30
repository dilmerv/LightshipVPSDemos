// Copyright 2022 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Anchors
{
  /// Possible types for an anchor. Useful when checking if an Anchor object is actually a
  /// sub-object.
  public enum AnchorType
  {
    /// An anchor.
    Basic = 0,

    /// A plane anchor.
    Plane = 1,

    /// An image anchor.
    /// @note This is an iOS-only value.
    Image = 2,

    /// A face anchor.
    /// @note Face anchors are only available in the face tracking feature branch
    //Face = 3,
  }
}
