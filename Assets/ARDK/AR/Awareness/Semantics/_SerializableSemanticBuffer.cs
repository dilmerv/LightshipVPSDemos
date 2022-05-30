// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Linq;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.Utilities.Logging;

using Unity.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Semantics
{
  // Can't use [Serializable]. Need to provide a serializer.
  internal sealed class _SerializableSemanticBuffer:
    _SerializableAwarenessBufferBase<UInt32>,
    ISemanticBuffer
  {
    private static bool _hasWarnedAboutInterpolation;
    private bool[] _hasChannelCache;

    internal _SerializableSemanticBuffer
    (
      uint width,
      uint height,
      bool isKeyframe,
      Matrix4x4 viewMatrix,
      NativeArray<UInt32> data,
      string[] channelNames,
      CameraIntrinsics intrinsics
    )
      : base(width, height, isKeyframe, viewMatrix, data, intrinsics)
    {
      ChannelCount = (uint)channelNames.Length;
      ChannelNames = channelNames;
    }

    /// <inheritdoc />
    public uint ChannelCount { get; private set; }

    /// <inheritdoc />
    public string[] ChannelNames { get; private set; }

    /// <inheritdoc />
    public int GetChannelIndex(string channelName)
    {
      var index = Array.IndexOf(ChannelNames, channelName);

      if (index < 0)
      {
        string suggestion = string.Empty;
        var lowercase = channelName.ToLower();

        if (Array.IndexOf(ChannelNames, lowercase) >= 0)
          suggestion = string.Format("Did you mean \"{0}\"?", lowercase);

        ARLog._ErrorFormat
        (
          "Invalid channelName \"{0}\". {1}",
          channelName,
          suggestion
        );
      }

      return index;
    }

    /// <inheritdoc />
    public UInt32 GetChannelTextureMask(int channelIndex)
    {
      // test for invalid index
      if (channelIndex < 0 || channelIndex >= ChannelNames.Length)
        return 0;

      return 1u << (_NativeSemanticBuffer.BitsPerPixel - 1 - channelIndex);
    }

    /// <inheritdoc />
    public UInt32 GetChannelTextureMask(int[] channelIndices)
    {
      UInt32 mask = 0;

      for (int i = 0; i < channelIndices.Length; i++)
      {
        mask |= GetChannelTextureMask(channelIndices[i]);
      }

      return mask;
    }

    /// <inheritdoc />
    public UInt32 GetChannelTextureMask(string channelName)
    {
      var index = GetChannelIndex(channelName);

      return GetChannelTextureMask(index);
    }

    /// <inheritdoc />
    public UInt32 GetChannelTextureMask(string[] channelNames)
    {
      UInt32 mask = 0;

      for (int i = 0; i < channelNames.Length; i++)
      {
        mask |= GetChannelTextureMask(channelNames[i]);
      }

      return mask;
    }

    /// <inheritdoc />
    public bool DoesChannelExistAt(int x, int y, int channelIndex)
    {
      var data = Data;
      var value = data[x + y * (int) Width];
      var flag = 1u << (_NativeSemanticBuffer.BitsPerPixel - 1) - channelIndex;
      return (value & flag) != 0;
    }

    /// <inheritdoc />
    public bool DoesChannelExistAt(int x, int y, string channelName)
    {
      var index = GetChannelIndex(channelName);
      return index != -1 && DoesChannelExistAt(x, y, index);
    }

    /// <inheritdoc />
    public bool DoesChannelExistAt(Vector2 uv, int channelIndex)
    {
      var widthMinusOne = (int)Width - 1;
      var heightMinusOne = (int)Height - 1;

      // Sample the buffer
      var x = Mathf.Clamp((int)Mathf.Floor(uv.x * widthMinusOne), 0, widthMinusOne);
      var y = Mathf.Clamp((int)Mathf.Floor(uv.y * heightMinusOne), 0, heightMinusOne);

      return DoesChannelExistAt(x, y, channelIndex);
    }

    /// <inheritdoc />
    public bool DoesChannelExistAt(Vector2 uv, string channelName)
    {
      var index = GetChannelIndex(channelName);
      return index != -1 && DoesChannelExistAt(uv, index);
    }

    /// <inheritdoc />
    public bool DoesChannelExist(int channelIndex)
    {
      _ComputeHasChannelCache();
      return _hasChannelCache != null && (channelIndex < ChannelCount) && _hasChannelCache[channelIndex];
    }

    public bool DoesChannelExist(string channelName)
    {
      var index = Array.IndexOf(ChannelNames, channelName);
      return index != -1 && DoesChannelExist(index);
    }
    
    public UInt32 Sample(Vector2 uv)
    {
      var w = (int)Width;
      var h = (int)Height;

      var x = Mathf.Clamp(Mathf.RoundToInt(uv.x * w - 0.5f), 0, w - 1);
      var y = Mathf.Clamp(Mathf.RoundToInt(uv.y * h - 0.5f), 0, h - 1);

      return Data[x + w * y];
    }

    public UInt32 Sample(Vector2 uv, Matrix4x4 transform)
    {
      var w = (int)Width;
      var h = (int)Height;

      var st = transform * new Vector4(uv.x, uv.y, 1.0f, 1.0f);
      var sx = st.x / st.z;
      var sy = st.y / st.z;

      var x = Mathf.Clamp(Mathf.RoundToInt(sx * w - 0.5f), 0, w - 1);
      var y = Mathf.Clamp(Mathf.RoundToInt(sy * h - 0.5f), 0, h - 1);

      return Data[x + w * y];
    }

    /// <inheritdoc />
    public bool CreateOrUpdateTextureARGB32
    (
      ref Texture2D texture,
      int channelIndex,
      FilterMode filterMode = FilterMode.Point
    )
    {
      uint flag = 1u << (_NativeSemanticBuffer.BitsPerPixel - 1 - channelIndex);
      return _AwarenessBufferHelper._CreateOrUpdateTextureARGB32
      (
        Data,
        (int)Width,
        (int)Height,
        ref texture,
        filterMode,
        val => (val & flag) != 0 ? 1.0f : 0.0f
      );
    }

    public bool CreateOrUpdateTextureARGB32
    (
      ref Texture2D texture,
      int[] channels,
      FilterMode filterMode = FilterMode.Point
    )
    {
      uint flag = GetChannelTextureMask(channels);
      return _AwarenessBufferHelper._CreateOrUpdateTextureARGB32
      (
        Data,
        (int)Width,
        (int)Height,
        ref texture,
        filterMode,
        val => (val & flag) != 0 ? 1.0f : 0.0f
      );
    }

    /// <inheritdoc />
    public bool CreateOrUpdateTextureRFloat
    (
      ref Texture2D texture,
      FilterMode filterMode = FilterMode.Point
    )
    {
      return _AwarenessBufferHelper._CreateOrUpdateTextureRFloat
      (
        Data,
        (int)Width,
        (int)Height,
        ref texture,
        filterMode
      );
    }

    public override IAwarenessBuffer GetCopy()
    {
      return new _SerializableSemanticBuffer
      (
        Width,
        Height,
        false,
        ViewMatrix,
        new NativeArray<UInt32>(Data, Allocator.Persistent),
        ChannelNames,
        Intrinsics
      );
    }

    /// <summary>
    /// Calculate if this image has a specific channel or not by caching all the values and see
    /// which channels are present
    /// </summary>
    private void _ComputeHasChannelCache()
    {
      if (_hasChannelCache == null && Data != null && Data.Length > 0)
      {
        var bitsPerPixel = Marshal.SizeOf(Data[0]) * 8;

        _hasChannelCache = new bool[ChannelCount];

        foreach (var pixel in Data)
        {
          for (var i = 0; i < ChannelCount; i++)
          {
            if (!_hasChannelCache[i])
            {
              var flag = 1u << (bitsPerPixel - 1) - i;
              if ((pixel & flag) != 0)
              {
                _hasChannelCache[i] = true;
              }
            }
          }
        }
      }
    }
  }
}
