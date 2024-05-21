using Agents;
using Player;
using SNetwork;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public static class HealManager
    {
        internal static HealDamageSync Sync { get; private set; } = new();

        internal static void Init()
        {
            Sync.Setup();
        }

        public static void DoHeal(PlayerAgent player, float heal)
        {
            if (heal >= 0)
                player.Damage.AddHealth(heal, player);
            else
            {
                HealData data = default;
                data.player.Set(player);
                data.healDamage.Set(-heal, player.Damage.HealthMax);
                Sync.Send(data, SNet_ChannelType.GameNonCritical);
            }
        }

        internal static void Internal_ReceiveHealDamage(PlayerAgent player, float damage)
        {
            player.Damage.OnIncomingDamage(damage, damage, player);
        }
    }

    public struct HealData
    {
        public pPlayerAgent player;
        public UFloat16 healDamage;
    }
}
