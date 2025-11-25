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
        private readonly static HealFXSync _fxSync = new();
        private const float SingleVal = 1f / 65535f; // For fixing rounding errors
        private const float FLASH_CONVERSION = 6f;

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _sync.Setup();
            _fxSync.Setup();
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
        }

        internal static void Internal_ReceiveHeal(PlayerAgent player, float heal, float cap, bool cancelRegen)
        {
            Dam_PlayerDamageBase dam = player.Damage;
            if (heal > 0)
            {
                heal = Math.Min(heal, (cap + SingleVal) - dam.Health);
                if (heal <= 0) return;

                dam.SendSetHealth(Math.Min(dam.Health + heal, player.Damage.HealthMax));
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
                _fxSync.Send(heal, player.Owner);
            }
        }

        internal static void Internal_ReceiveHealDamage(float damage)
        {
            var player = PlayerManager.GetLocalPlayerAgent();
            player.FPSCamera.AddHitReact(damage / player.Damage.HealthMax * FLASH_CONVERSION, UnityEngine.Vector3.up, 0f);
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
