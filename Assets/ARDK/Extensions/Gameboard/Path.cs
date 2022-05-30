// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

namespace Niantic.ARDK.Extensions.Gameboard
{
  public class Path
  {
    public List<Waypoint> Waypoints { get; }
    public int Length
    {
      get
      {
        if (Waypoints == null)
          return 0;
        else
          return Waypoints.Count;
      }
    }

    public enum Status {PathComplete, PathPartial, PathInvalid}
    public Status PathStatus { get; }

    public Path(List<Waypoint> waypoints, Status pathStatus)
    {
      Waypoints = waypoints;
      PathStatus = pathStatus;

      if (waypoints == null || waypoints.Count == 0)
      {
        PathStatus = Status.PathInvalid;
      }
    }
  }
}
