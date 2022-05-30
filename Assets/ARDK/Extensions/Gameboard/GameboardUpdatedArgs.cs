// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Gameboard
{
    public class GameboardUpdatedArgs : IArdkEventArgs
    {
        public HashSet<Vector2Int> RemovedNodes { get; }
        public readonly bool PruneOrClear;

        public GameboardUpdatedArgs(HashSet<Vector2Int> removedNodes, bool pruneOrClear)
        {
            RemovedNodes = removedNodes;
            PruneOrClear = pruneOrClear;
        }
    }
}