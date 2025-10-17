using BepInEx.Unity.IL2CPP;
using Gear;
using MSC.CustomMeleeData;
using System.Runtime.CompilerServices;

namespace EWC.Dependencies
{
    internal static class MSCWrapper
    {
        public const string PLUGIN_GUID = "Dinorush.MeleeSwingCustomization";

        public static readonly bool hasMSC;

        static MSCWrapper()
        {
            hasMSC = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static MSCData GetAttackSpeedMods(MeleeWeaponFirstPerson melee)
        {
            if (!hasMSC)
                return new MSCData();
            else
                return GetAttackSpeedMods_Unsafe(melee);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MSCData GetAttackSpeedMods_Unsafe(MeleeWeaponFirstPerson melee)
        {
            var data = MeleeDataManager.Current.GetData(melee);
            if (data == null)
                return new MSCData();
            else
                return new MSCData(data.LightAttackSpeed, data.ChargedAttackSpeed, data.PushSpeed);
        }

        public struct MSCData
        {
            public float lightAttackSpeed = 1f;
            public float chargedAttackSpeed = 1f;
            public float pushAttackSpeed = 1f;

            public MSCData() { }
            public MSCData(float lightSpeed, float chargedSpeed, float pushSpeed)
            {
                lightAttackSpeed = lightSpeed;
                chargedAttackSpeed = chargedSpeed;
                pushAttackSpeed = pushSpeed;
            }
        }
    }
}
