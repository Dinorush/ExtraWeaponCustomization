using Agents;
using EWC.Attributes;
using EWC.Dependencies;
using Player;
using SNetwork;
using System;

namespace EWC.CustomWeapon.Properties.Effects.Heal
{
    public static class HealManager
    {
        private readonly static HealSync _sync = new();
        private const float SingleVal = 1f / 65535f; // For fixing rounding errors
        private const float FLASH_CONVERSION = 6f;

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
        }

        public static void DoHeal(PlayerAgent player, float heal, float cap, HealthMod hBase)
        {
            if (heal == 0) return;

            HealData data = new() {
                cancelRegen = hBase.CancelRegen
            };
            data.player.Set(player);
            data.heal.Set(heal, player.Damage.HealthMax);
            data.cap.Set(cap, player.Damage.HealthMax);
            _sync.Send(data, SNet_ChannelType.GameNonCritical);
            ReceiveHealLocal(player, heal, cap);
        }

        internal static void Internal_ReceiveHeal(PlayerAgent player, float heal, float cap, bool cancelRegen)
        {
            Dam_PlayerDamageBase dam = player.Damage;
            if (heal > 0)
            {
                heal = Math.Min(heal, (cap + SingleVal) - dam.Health);
                if (heal <= 0) return;

                dam.Health = Math.Min(dam.Health + heal, player.Damage.HealthMax);
                dam.SendSetHealth(dam.Health);
                if (cancelRegen)
                    dam.m_nextRegen = Clock.Time + player.PlayerData.healthRegenStartDelayAfterDamage * EXPAPIWrapper.GetHealthRegenMod(player);
            }
            else
            {
                heal = Math.Min(-heal, dam.Health - (cap - SingleVal));
                if (heal <= 0) return;

                float origRegen = dam.m_nextRegen;
                player.Damage.OnIncomingDamage(heal, heal, player);
                dam.m_nextRegen = cancelRegen ? Clock.Time + player.PlayerData.healthRegenStartDelayAfterDamage * EXPAPIWrapper.GetHealthRegenMod(player) : origRegen;
            }
        }

        private static void ReceiveHealLocal(PlayerAgent player, float heal, float cap)
        {
            if (heal >= 0f) return;

            var damBase = player.Damage;
            heal = Math.Min(-heal, damBase.Health - (cap - SingleVal));
            if (heal <= 0) return;

            player.FPSCamera.AddHitReact(heal / damBase.HealthMax * FLASH_CONVERSION, UnityEngine.Vector3.up, 0f);
        }
    }

    public struct HealData
    {
        public pPlayerAgent player;
        public SFloat16 heal;
        public UFloat16 cap;
        public bool cancelRegen;
    }
}
