using System;

namespace EWC.CustomWeapon.ComponentWrapper.DataSwapper
{
    [Flags]
    public enum SwapSettings
    {
        None = 0,
        KeepMagCost = 1,
        EndFiring = 1 << 1,
        EndBurst = 1 << 2,
        EndCharge = 1 << 3
    }
}
