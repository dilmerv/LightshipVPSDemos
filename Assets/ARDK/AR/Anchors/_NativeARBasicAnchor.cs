// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Anchors
{
  internal sealed class _NativeARBasicAnchor:
    _NativeARAnchor
  {
    public _NativeARBasicAnchor(IntPtr nativeHandle):
      base(nativeHandle)
    {
    }

    public override AnchorType AnchorType
    {
      get => AnchorType.Basic;
    }
  }
}

