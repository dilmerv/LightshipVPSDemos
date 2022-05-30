// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Gameboard
{
  /// Holds information about unoccupied areas in the environment. Requires meshing to be enabled.
  /// Provides pathfinding and functions for environmental queries. 
  public interface IGameboard
  {
    /// The configuration this Gameboard was created with.
    ModelSettings Settings { get; }
    
    /// The discovered free area in square meters.
    float Area { get; }
    
    /// Alerts subscribers that the Gameboard has been updated.
    event ArdkEventHandler<GameboardUpdatedArgs> GameboardUpdated;
    
    /// Alerts subscribers that the Gameboard has been destroyed.
    event ArdkEventHandler<IArdkEventArgs> GameboardDestroyed;

    /// Destroys the Gameboard and triggers the GameboardDestroyed event.
    void Destroy();
    
    /// Searches for occupied and unoccupied areas in the environment and updates the Gameboard
    /// accordingly by adding and removing nodes. For this, rays are cast against the mesh from the
    /// scan origin downwards. Raycasts will not find free areas under obstacles like tables, etc.
    /// @param origin Origin of the scan in world position.
    /// @param range Area covered by the scan is size range*range.
    void Scan(Vector3 origin, float range);
    
    /// Removes all surfaces from the board.
    void Clear();

    /// Removes nodes outside the specified squared area of size range*range.
    /// Use this to prune Gameboard for performance.
    /// @param keepNodesOrigin Defines an origin in world position from which nodes will be kept.
    /// @param range Range of the box area where nodes will be kept.
    void Prune(Vector3 keepNodesOrigin, float range);

    /// Checks whether an area is free to occupy by a box with footprint size*size. Does not take
    ///  the height into account.
    /// @param center Origin of the area in world position.
    /// @param size Width/Length of the object's estimated footprint in meter.
    bool CheckFit(Vector3 center, float size);

    /// Checks whether the specified (projected) world position is on the Gameboard surface.
    /// @param position World coordinate for the query.
    /// @param delta Tolerance in y position still considered on the Gameboard surface
    /// @returns True, if the specified position is on the Gameboard.
    bool IsOnGameboard(Vector3 position, float delta);

    /// Finds the nearest world position on the Gameboard to the specified source position.
    /// @param sourcePosition The origin of the search.
    /// @param nearestPosition The resulting nearest position, if any.
    /// @returns True, if a nearest point could be found.
    bool FindNearestFreePosition(Vector3 sourcePosition, out Vector3 nearestPosition);

    /// Finds the nearest world position on the Gameboard to the specified source position within
    ///  a specified range.
    /// @param sourcePosition The origin of the search.
    /// @param range Defines the search window (size = 2 * range).
    /// @param nearestPosition The resulting nearest position, if any.
    /// @returns True, if a nearest point could be found.
    bool FindNearestFreePosition(Vector3 sourcePosition, float range, out Vector3 nearestPosition);

    /// Finds a random world position on the Gameboard.
    /// @param randomPosition The resulting random position, if any.
    /// @returns True, if a point could be found.
    bool FindRandomPosition(out Vector3 randomPosition);
    
    /// Finds a random world position on the Gameboard within a specified range.
    /// @param sourcePosition The origin of the search.
    /// @param range Defines the search window (size = 2 * range).
    /// @param randomPosition The resulting random position, if any.
    /// @returns True, if a point could be found.
    bool FindRandomPosition(Vector3 sourcePosition, float range, out Vector3 randomPosition);
    
    /// Calculates a walkable path between the two specified positions.
    /// @param fromPosition Start position.
    /// @param toPosition Destination position
    /// @param agent The configuration of the agent is path is calculated for.
    /// @param path The calculated path
    /// @returns True if either a complete or partial path is found. False otherwise.
    bool CalculatePath
    (
      Vector3 fromPosition,
      Vector3 toPosition,
      AgentConfiguration agent,
      out Path path
    );
    
    /// Raycasts against the Gameboard.
    /// @param ray Ray to perform this function with.
    /// @param hitPoint Hit point in world coordinates, if any.
    /// @returns True if the ray hit a point on any plane within the Gameboard.
    bool RayCast(Ray ray, out Vector3 hitPoint);

    /// Activates/Deactivates visualisation of Gameboard areas and agent paths
    /// @param active Activates visualisation if true, deactivates if false
    void SetVisualisationActive(bool active);
  }
}
