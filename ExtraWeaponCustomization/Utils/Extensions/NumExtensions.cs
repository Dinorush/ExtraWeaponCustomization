using System;

namespace EWC.Utils.Extensions
{
    internal static class NumExtensions
    {
        public static float Map(this float orig, float fromMin, float fromMax, float toMin, float toMax, float exponent = 1f)
        {
            if (fromMin == fromMax) return orig < fromMin ? toMin : toMax;

            fromMax -= fromMin;
            orig = Math.Clamp(orig - fromMin, 0, fromMax);
            if (exponent != 1f)
                return (float)Math.Pow(orig / fromMax, exponent) * (toMax - toMin) + toMin;
            return orig / fromMax * (toMax - toMin) + toMin;
        }

        public static float MapInverted(this float orig, float fromMin, float fromMax, float toMax, float toMin, float exponent = 1f)
        {
            if (fromMin == fromMax) return orig < fromMin ? toMax : toMin;

            fromMax -= fromMin;
            orig = Math.Clamp(orig - fromMin, 0, fromMax);
            if (exponent != 1f)
                return (float)Math.Pow((fromMax - orig) / fromMax, exponent) * (toMax - toMin) + toMin;
            return (fromMax - orig) / fromMax * (toMax - toMin) + toMin;
        }

        public static float Lerp(this float t, float min, float max) => (max - min) * Math.Clamp(t, 0, 1) + min;

        public static float Lerp(this double t, float min, float max) => (max - min) * (float)Math.Clamp(t, 0, 1) + min;
    }
}
