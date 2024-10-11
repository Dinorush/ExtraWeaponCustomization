using System;

namespace EWC.CustomWeapon.ObjectWrappers
{
    internal abstract class KeyWrapper
    {
        public IntPtr Pointer { get; protected set; }

        public KeyWrapper(IntPtr ptr)
        {
            Pointer = ptr;
        }

        public override int GetHashCode()
        {
            return Pointer.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is KeyWrapper wrapper && wrapper.Pointer == Pointer;
        }
    }
}
