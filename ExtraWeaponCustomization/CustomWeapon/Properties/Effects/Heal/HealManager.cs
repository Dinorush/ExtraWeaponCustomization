using Agents;
using Player;
using SNetwork;
using System;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Heal
{
    public static class HealManager
    {
        internal static HealSync Sync { get; private set; } = new();
        private const float SingleVal = 1f / 65535f; // For fixing rounding errors

        internal static void Init()
        {
            Sync.Setup();
        }

        public static void DoHeal(PlayerAgent player, float heal, float cap)
        {
            if (heal == 0) return;

            HealData data = default;
            data.player.Set(player);
            data.heal.Set(heal, player.Damage.HealthMax);
            data.cap.Set(cap, player.Damage.HealthMax);
            Sync.Send(data, SNet_ChannelType.GameNonCritical);
        }

        internal static void Internal_ReceiveHeal(PlayerAgent player, float heal, float cap)
        {
            Dam_PlayerDamageBase dam = player.Damage;
            if (heal > 0)
            {
                heal = Math.Min(heal, (cap + SingleVal) - dam.Health);
                if (heal <= 0) return;

                dam.Health = Math.Min(dam.Health + heal, player.Damage.HealthMax);
                dam.SendSetHealth(dam.Health);
            }
            else
            {
                heal = Math.Min(-heal, dam.Health - (cap - SingleVal));
                if (heal <= 0) return;
                
                player.Damage.OnIncomingDamage(heal, heal, player);
            }
        }
    }

    public struct HealData
    {
        public pPlayerAgent player;
        public SFloat16 heal;
        public UFloat16 cap;
    }
}
