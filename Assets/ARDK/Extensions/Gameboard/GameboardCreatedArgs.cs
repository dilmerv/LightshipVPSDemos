// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Extensions.Gameboard
{
  public class GameboardCreatedArgs: IArdkEventArgs
  {
    public IGameboard Gameboard { get; private set; }

    public GameboardCreatedArgs(IGameboard gameboard)
    {
      Gameboard = gameboard;
    }
  }
}