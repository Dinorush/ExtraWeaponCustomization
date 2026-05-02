using UnityEngine;

namespace EWC.Networking.Structs
{
    public struct Vector3_24b_Normalized
    {
        private UFloat8b x;

        private UFloat8b y;

        private UFloat8b z;

        private static Vector3 s_tempVec = Vector3.zero;

        public Vector3 Value
        {
            readonly get
            {
                s_tempVec.Set(x.Value - 0.5f, y.Value - 0.5f, z.Value - 0.5f);
                s_tempVec *= 2f;
                return s_tempVec;
            }
            set
            {
                value *= 0.5f;
                x.Value = value.x + 0.5f;
                y.Value = value.y + 0.5f;
                z.Value = value.z + 0.5f;
            }
        }
    }
}
