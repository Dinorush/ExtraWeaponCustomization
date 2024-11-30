using System;

namespace EWC.Utils
{
    internal static class NumExtensions
    {
        public static float Map(this float orig, float fromMin, float fromMax, float toMin, float toMax, float exponent = 1f)
        {
            if (fromMin == fromMax) return orig < fromMin ? toMin : toMax;

            orig = Math.Clamp(orig, fromMin, fromMax);
            if (exponent != 1f)
                return (float) Math.Pow((orig - fromMin) / (fromMax - fromMin), exponent) * (toMax - toMin) + toMin;
            return (orig - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        public static float MapInverted(this float orig, float fromMin, float fromMax, float toMax, float toMin, float exponent = 1f)
        {
            if (fromMin == fromMax) return orig < fromMin ? toMax : toMin;

            orig = Math.Clamp(orig, fromMin, fromMax);
            if (exponent != 1f)
                return (float)Math.Pow((fromMax - orig) / (fromMax - fromMin), exponent) * (toMax - toMin) + toMin;
            return (fromMax - orig) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        public static float Lerp(this float t, float min, float max) => (max - min) * Math.Clamp(t, 0, 1) + min;

        public static float Lerp(this double t, float min, float max) => (max - min) * (float) Math.Clamp(t, 0, 1) + min;
    }
}
