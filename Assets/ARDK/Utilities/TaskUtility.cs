// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;
using System.Threading.Tasks;

using UnityEngine;

namespace Niantic.ARDK.Utilities
{
  public static class TaskUtility
  {
    /// Waits while the function is true
    /// @param condition The function to check
    /// @param delay The number of milliseconds between each condition check
    public static async Task WaitWhile(Func<bool> condition, int delay = 1)
    {
      delay = Mathf.Max(1, delay);
      while (condition())
      {
        await Task.Delay(delay);
      }
    }

    /// Waits until the function is true
    /// @param condition The function to wait on
    /// @param delay The number of milliseconds between each condition check
    public static async Task WaitUntil(Func<bool> condition, int delay = 1)
    {
      delay = Mathf.Max(1, delay);
      while (!condition())
      {
        await Task.Delay(delay);
      }
    }
  }
}
