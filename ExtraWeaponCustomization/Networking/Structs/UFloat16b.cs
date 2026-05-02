using UnityEngine;

namespace EWC.Networking.Structs
{
    public struct UFloat16b
    {
        public ushort internalValue;

        private const float convOut = 1.52590219E-05f;

        private const float convIn = 65535f;

        public float Value
        {
            readonly get
            {
                return internalValue * convOut;
            }
            set
            {
                internalValue = (ushort)(Mathf.Clamp01(value) * convIn);
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
