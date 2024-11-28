using System;

namespace EWC.Utils
{
    internal static class NumExtensions
    {
        public static float Map(this float orig, float fromMin, float fromMax, float toMin, float toMax)
        {
            if (fromMin == fromMax) return orig < fromMin ? toMin : toMax;

            orig = Math.Clamp(orig, fromMin, fromMax);
            return (orig - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        public static float Lerp(this float t, float min, float max) => (max - min) * Math.Clamp(t, 0, 1) + min;

        public static float Lerp(this double t, float min, float max) => (max - min) * (float) Math.Clamp(t, 0, 1) + min;
    }
}
