// Copyright 2022 Niantic, Inc. All Rights Reserved.
using Niantic.ARDK.AR.Awareness.Depth;

namespace Niantic.ARDK.AR.Frame
{
  internal interface _IARFrame: IARFrame
  {
    new IDepthPointCloud DepthPointCloud { get; set; }
  }
}
