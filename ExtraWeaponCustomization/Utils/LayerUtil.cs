using UnityEngine;

namespace EWC.Utils
{
    public static class LayerUtil
    {
        public static int MaskDynamic { get; private set; }
        public static int MaskEntityAndWorld { get; private set; }
        public static int MaskWorld { get; private set; }
        public static int MaskEntity { get; private set; }
        public static int MaskOwner { get; private set; }
        public static int MaskFriendly { get; private set; }

        internal static void Init()
        {
            MaskOwner = LayerMask.GetMask("PlayerMover");
            MaskFriendly = LayerMask.GetMask("PlayerSynced");
            MaskEntity = LayerMask.GetMask("PlayerMover", "PlayerSynced", "EnemyDamagable");
            MaskWorld = LayerMask.GetMask("Default", "Default_NoGraph", "Default_BlockGraph", "ProjectileBlocker", "Dynamic");
            MaskEntityAndWorld = MaskEntity | MaskWorld;
            MaskDynamic = LayerMask.GetMask("Dynamic");
        }
    }
}
