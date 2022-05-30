// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.LocationService
{
  public struct LocationUpdatedArgs:
    IArdkEventArgs
  {
    public LocationInfo LocationInfo;

    public LocationUpdatedArgs(LocationInfo info)
    {
      LocationInfo = info;
    }
  }
}
