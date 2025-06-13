using GameData;

namespace EWC.Utils.Extensions
{
    internal static class ArchetypeExtensions
    {
        public static int PierceLimit(this ArchetypeDataBlock dataBlock) => dataBlock.PiercingBullets ? dataBlock.PiercingDamageCountLimit : 1;
    }
}
