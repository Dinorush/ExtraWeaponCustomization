using GameData;
using Gear;

namespace EWC.CustomWeapon.ComponentWrapper.DataSwapper
{
    public class SwapData
    {
        public readonly uint Priority;
        public readonly BulletWeaponArchetype? GunArchetype;
        public readonly ArchetypeDataBlock ArchetypeData;
        public readonly WeaponAudioDataBlock AudioData;
        public readonly SwapSettings Settings;

        private readonly uint AudioID;
        private readonly uint ArchetypeID;

        public bool AudioMatch(SwapData data) => AudioID == data.AudioID;
        public bool ArchMatch(SwapData data) => ArchetypeID == data.ArchetypeID;

        public SwapData(BulletWeaponArchetype? gunArchetype, ArchetypeDataBlock archBlock, WeaponAudioDataBlock audioBlock, uint priority = 0, SwapSettings settings = SwapSettings.None)
        {
            GunArchetype = gunArchetype;
            Priority = priority;
            ArchetypeData = archBlock;
            ArchetypeID = archBlock.persistentID;
            AudioData = audioBlock;
            AudioID = audioBlock.persistentID;
            Settings = settings;
        }
    }
}
