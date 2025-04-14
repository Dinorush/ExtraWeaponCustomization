using BepInEx.Unity.IL2CPP;
using Gear;
using ExtraRecoilData.API;
using ExtraRecoilData.CustomRecoil;
using System.Runtime.CompilerServices;

namespace EWC.Dependencies
{
    internal static class ERDAPIWrapper
    {
        public const string PLUGIN_GUID = "Dinorush.ExtraRecoilData";

        public static readonly bool hasERD;

        static ERDAPIWrapper()
        {
            hasERD = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static void ChangeCustomRecoil(uint archetypeID, BulletWeapon weapon)
        {
            if (hasERD)
                ERD_ChangeERDComponent(archetypeID, weapon);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ERD_ChangeERDComponent(uint archetypeID, BulletWeapon weapon) => ChangeAPI.ChangeERDComponent(archetypeID, weapon);
    }
}
