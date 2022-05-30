// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Configuration;

namespace Niantic.ARDK.AR
{
  internal interface _IARSession:
    IARSession
  {
    ARSessionChangesCollector ARSessionChangesCollector { get; }

    /// Gets how this session will transition the AR state when re-run.
    ARSessionRunOptions RunOptions { get; }

    bool IsPlayback { get; }
  }
}
