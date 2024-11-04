using Agents;
using Player;
using SNetwork;
using System;

namespace EWC.CustomWeapon.Properties.Effects.Heal
{
    public static class HealManager
    {
        internal static HealSync Sync { get; private set; } = new();
        private const float SingleVal = 1f / 65535f; // For fixing rounding errors

        internal static void Init()
        {
            Sync.Setup();
        }

        public static void DoHeal(PlayerAgent player, float heal, float cap, HealthMod hBase)
        {
            if (heal == 0) return;

            HealData data = new() {
                damageFX = hBase.TriggerDamageFX,
                cancelRegen = hBase.CancelRegen
            };
            data.player.Set(player);
            data.heal.Set(heal, player.Damage.HealthMax);
            data.cap.Set(cap, player.Damage.HealthMax);
            Sync.Send(data, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveHeal(PlayerAgent player, float heal, float cap, bool damageFX, bool cancelRegen)
        {
            Dam_PlayerDamageBase dam = player.Damage;
            if (heal > 0)
            {
                heal = Math.Min(heal, (cap + SingleVal) - dam.Health);
                if (heal <= 0) return;

                dam.Health = Math.Min(dam.Health + heal, player.Damage.HealthMax);
                dam.SendSetHealth(dam.Health);
                if (cancelRegen)
                    dam.m_nextRegen = Clock.Time + player.PlayerData.healthRegenStartDelayAfterDamage;
            }
            else
            {
                heal = Math.Min(-heal, dam.Health - (cap - SingleVal));
                if (heal <= 0) return;

                float origRegen = dam.m_nextRegen;

                if (damageFX)
                    player.Damage.OnIncomingDamage(heal, heal, player);
                else
                {
                    dam.Health = Math.Min(dam.Health - heal, player.Damage.HealthMax);
                    dam.SendSetHealth(dam.Health);
                }

                dam.m_nextRegen = cancelRegen ? origRegen : Clock.Time + player.PlayerData.healthRegenStartDelayAfterDamage;
            }
        }
    }

    public struct HealData
    {
        public pPlayerAgent player;
        public SFloat16 heal;
        public UFloat16 cap;
        public bool damageFX;
        public bool cancelRegen;
    }
}
