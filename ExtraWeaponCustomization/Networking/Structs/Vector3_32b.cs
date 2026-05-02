using UnityEngine;

namespace EWC.Networking.Structs
{
    public struct Vector3_32b
    {
        private Vector3_24b_Normalized vector;

        private UFloat8b length;

        public void Set(Vector3 vec, float maxLength)
        {
            float magnitude = vec.magnitude;
            length.Set(magnitude, maxLength);
            vec.Normalize();
            vector.Value = vec;
        }

        public readonly Vector3 Get(float maxLength)
        {
            return vector.Value * length.Get(maxLength);
        }
    }
}
