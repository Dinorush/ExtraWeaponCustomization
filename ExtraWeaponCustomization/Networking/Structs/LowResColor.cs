using UnityEngine;

namespace EWC.Networking.Structs
{
    public struct LowResColor
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        private static Color s_color = Color.black;

        public static implicit operator Color(LowResColor lowResColor)
        {
            s_color.r = lowResColor.r / 255f;
            s_color.g = lowResColor.g / 255f;
            s_color.b = lowResColor.b / 255f;
            s_color.a = lowResColor.a / 255f;
            return s_color;
        }

        public static implicit operator LowResColor(Color color)
        {
            return new()
            {
                r = (byte)(color.r * 255),
                g = (byte)(color.g * 255),
                b = (byte)(color.b * 255),
                a = (byte)(color.a * 255)
            };
        }
    }
}
