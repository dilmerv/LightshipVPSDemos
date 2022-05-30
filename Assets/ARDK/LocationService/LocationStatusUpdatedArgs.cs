// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.LocationService
{
  public struct LocationStatusUpdatedArgs:
    IArdkEventArgs
  {
    public readonly LocationServiceStatus Status;

    public LocationStatusUpdatedArgs(LocationServiceStatus status)
    {
      Status = status;
    }
  }
}
