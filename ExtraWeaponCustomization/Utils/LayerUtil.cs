using UnityEngine;

namespace EWC.Utils
{
    public static class LayerUtil
    {
        public static int MaskDynamic { get; private set; }
        public static int MaskEntityAndWorld { get; private set; }
        public static int MaskEntityAndWorld3P { get; private set; }
        public static int MaskWorld { get; private set; }
        public static int MaskWorldExcProj { get; private set; }
        public static int MaskDecalValid { get; private set; }
        public static int MaskEntityDynamic3P { get; private set; }
        public static int MaskEntity { get; private set; }
        public static int MaskEntity3P { get; private set; }
        public static int MaskOwner { get; private set; }
        public static int MaskFriendly { get; private set; }
        public static int MaskEnemy { get; private set; }
        public static int MaskEnemyDynamic { get; private set; }

        // Has duplicates with LayerManager, want to avoid Il2Cpp overhead
        internal static void Init()
        {
            MaskOwner = LayerMask.GetMask("PlayerMover");
            MaskFriendly = LayerMask.GetMask("PlayerSynced");
            MaskEnemy = LayerMask.GetMask("EnemyDamagable");
            MaskDynamic = LayerMask.GetMask("Dynamic");

            MaskEntity3P = MaskFriendly | MaskEnemy;
            MaskEntity = MaskOwner | MaskEntity3P;

            MaskDecalValid = LayerMask.GetMask("Default", "Default_NoGraph", "Default_BlockGraph");
            MaskWorldExcProj = MaskDecalValid | MaskDynamic;
            MaskWorld = MaskWorldExcProj | LayerMask.GetMask("ProjectileBlocker");
            MaskEntityAndWorld = MaskEntity | MaskWorld;
            MaskEntityDynamic3P = MaskEntity3P | MaskDynamic;
            MaskEntityAndWorld3P = MaskEntity3P | MaskWorld;
        }
    }
}
