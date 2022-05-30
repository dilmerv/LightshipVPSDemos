// Copyright 2022 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.Collections
{
  internal static class _ArrayFromElement
  {
    public static T[] Create<T>(T element)
    {
      return new T[] { element };
    }
  }
}
