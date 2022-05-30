// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Extensions.Gameboard
{
    /// Class used to create Gameboard instances and passes instance to subscribers of
    /// GameboardInitialized. If a Gameboard was created and still alive, a second one can not be
    /// created.
    public static class GameboardFactory
    {
        private static ArdkEventHandler<GameboardCreatedArgs> _gameboardInitialized;

        private static object _activeGameboardLock = new object();
        private static IGameboard _activeGameboard;

        /// Create a Gameboard and notify subscribers of GameboardInitialized about it.
        /// @param settings Settings for the created Gameboard instance.
        /// @params visualise If the Gameboard visualisation is activated at start time.
        /// @returns The created Gameboard, or throws a Gameboard instance is still active.
        public static IGameboard Create(ModelSettings settings, bool visualise)
        {
            IGameboard result = new Gameboard(settings, visualise);

            _InvokeGameboardInitialized(result);
            return result;
        }

        /// Event invoked when a new Gameboard is created and initialized.
        public static event ArdkEventHandler<GameboardCreatedArgs> GameboardInitialized
        {
            add
            {
                _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => _gameboardInitialized);

                _gameboardInitialized += value;

                IGameboard activeGameboard;
                lock (_activeGameboardLock)
                    activeGameboard = _activeGameboard;

                if (activeGameboard != null)
                {
                    var args = new GameboardCreatedArgs(activeGameboard);
                    value(args);
                }
            }
            remove
            {
                _gameboardInitialized -= value;
            }
        }

        private static void _InvokeGameboardInitialized(IGameboard gameboard)
        {
            lock (_activeGameboardLock)
            {
                if (_activeGameboard != null)
                    throw new InvalidOperationException("There's already an active Gameboard.");

                _activeGameboard = gameboard;
            }

            var handler = _gameboardInitialized;
            if (handler != null)
            {
                var args = new GameboardCreatedArgs(gameboard);
                handler(args);
            }

            gameboard.GameboardDestroyed +=
                (_) =>
                {
                    lock (_activeGameboardLock)
                        if (_activeGameboard == gameboard)
                            _activeGameboard = null;
                };
        }
    }
}