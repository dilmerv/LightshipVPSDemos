// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.ObjectModel;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.ARSessionEventArgs
{
  public struct AnchorsMergedArgs:
    IArdkEventArgs
  {
    public AnchorsMergedArgs(IARAnchor parent, IARAnchor[] children):
      this()
    { 
      Parent = parent;
      Children = new ReadOnlyCollection<IARAnchor>(children);
    }

    public IARAnchor Parent { get; }
    public ReadOnlyCollection<IARAnchor> Children { get; }
  }
}
