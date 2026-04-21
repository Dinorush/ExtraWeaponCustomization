using EWC.Attributes;
using Player;
using SNetwork;
using System;

namespace EWC.CustomWeapon.Properties.Effects.Stamina
{
    public static class StaminaManager
    {
        private readonly static StaminaSync _sync = new();

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
        }

        public static void DoStaminaChange(PlayerAgent player, float change, float cap, StaminaMod sBase)
        {
            if (change == 0) return;

            StaminaData data = new() {
                cancelRegen = sBase.CancelRegen
            };

            data.mod.Set(change, 1f);
            data.cap.Set(cap, 1f);
            _sync.Send(data, player.Owner, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveStamina(float stamChange, float cap, bool cancelRegen)
        {
            if (!PlayerManager.HasLocalPlayerAgent()) return;

            var stam = PlayerManager.GetLocalPlayerAgent().Stamina;
            // Have to write this myself since vanilla UseStamina can't give stamina
            if (stamChange > 0)
            {
                if (DramaManager.InActionState)
                    cap = Math.Min(cap, stam.PlayerData.StaminaMaximumCapWhenInCombat);

                if (stam.Stamina >= cap) return;

                stam.Stamina = Math.Min(cap, stam.Stamina + stamChange);
            }
            else
            {
                if (!DramaManager.InActionState)
                    cap = Math.Max(cap, stam.PlayerData.StaminaMinimumCapWhenNotInCombat);

                if (stam.Stamina <= cap) return;

                stam.Stamina = Math.Max(cap, stam.Stamina + stamChange);
            }

            if (cancelRegen)
                stam.m_lastExertion = Clock.Time;
        }
    }

    public struct StaminaData
    {
        public SFloat16 mod;
        public UFloat8 cap;
        public bool cancelRegen;
    }
}
