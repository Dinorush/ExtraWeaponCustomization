using System;
using UnityEngine;

namespace EWC.Utils.Extensions
{
    internal static class VectorExtensions
    {
        private static Vector3 _cacheDir = Vector3.zero;
        private static Vector3 _cacheUp = Vector3.zero;
        private static Vector3 _cacheRight = Vector3.zero;
        public static Vector3 RotateBy(this Vector3 dir, float x, float y)
        {
            if (_cacheDir != dir)
            {
                Vector3 cross = Math.Abs(dir.y) < 0.99f ? Vector3.up : Vector3.forward;
                _cacheRight = Vector3.Cross(cross, dir).normalized;
                _cacheUp = Vector3.Cross(_cacheRight, dir).normalized;
                _cacheDir = dir;
            }

            if (x != 0)
                dir = Quaternion.AngleAxis(-x, _cacheUp) * dir;
            if (y != 0)
                dir = Quaternion.AngleAxis(-y, _cacheRight) * dir;
            
            return dir;
        }
    }
}
