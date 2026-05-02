using UnityEngine;

namespace EWC.Networking.Structs
{
    public struct UFloat8b
    {
        public byte internalValue;

        private const float convOut = 0.003921569f;

        private const float convIn = 255f;

        public float Value
        {
            readonly get
            {
                return internalValue * convOut;
            }
            set
            {
                internalValue = (byte)(Mathf.Clamp01(value) * convIn);
            }
        }

        public void Set(float v, float maxSize)
        {
            Value = v / maxSize;
        }

        public readonly float Get(float maxSize)
        {
            return Value * maxSize;
        }
    }
}
