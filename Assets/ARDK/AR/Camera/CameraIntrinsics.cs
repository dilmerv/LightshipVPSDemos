// Copyright 2022 Niantic, Inc. All Rights Reserved.
using UnityEngine;

namespace Niantic.ARDK.AR.Camera
{
  /// Where (fx, fy) is the focal length and (px, py) is the principal point location,
  /// can be cast into a vector: [fx, fy, px, py]
  public struct CameraIntrinsics
  {
    public Vector2 FocalLength { get; }
    public Vector2 PrincipalPoint { get; }

    private readonly Vector4 _vector;

    internal CameraIntrinsics(float fx, float fy, float px, float py)
    {
      FocalLength = new Vector2(fx, fy);
      PrincipalPoint = new Vector2(px, py);

      _vector = new Vector4(FocalLength.x, FocalLength.y, PrincipalPoint.x, PrincipalPoint.y);
    }

    public static implicit operator Vector4(CameraIntrinsics o)
    {
      return o._vector;
    }

    public static implicit operator CameraIntrinsics(Vector4 v)
    {
      return new CameraIntrinsics(v.x, v.y, v.z, v.w);
    }
    
    /// Converts this CameraIntrinsics instance to a Matrix4x4.
    /// The resulting matrix will have the following layout:
    /// 
    /// | Fx  0  0  Cx |
    /// | 0  Fy  0  Cy |
    /// | 0  0   1  0  |
    /// | 0  0   0  1  |
    /// 
    /// As an example, this matrix can be used to project points
    /// from screen to camera space:
    /// var pointInCamera =
    ///   depth * (Matrix4x4.Inverse(intrinsics) * new Vector4(x, y, 1.0f, 1.0f));
    public static implicit operator Matrix4x4(CameraIntrinsics intrinsics)
    {
      Matrix4x4 result = Matrix4x4.identity;
      result[0, 0] = intrinsics.FocalLength.x;
      result[1, 1] = intrinsics.FocalLength.y;
      result[0, 3] = intrinsics.PrincipalPoint.x;
      result[1, 3] = intrinsics.PrincipalPoint.y;
      return result;
    }
  }
}
