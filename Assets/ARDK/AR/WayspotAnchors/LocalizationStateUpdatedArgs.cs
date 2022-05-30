// Copyright 2022 Niantic, Inc. All Rights Reserved.
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public class LocalizationStateUpdatedArgs: IArdkEventArgs
  {
    public LocalizationState State { get; }
    public LocalizationFailureReason FailureReason { get; }

    internal LocalizationStateUpdatedArgs
      ( 
        LocalizationState state, 
        LocalizationFailureReason failureReason
      )
    {
      State = state;
      FailureReason = failureReason;
    }
  }
}
