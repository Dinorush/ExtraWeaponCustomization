using EWC.CustomWeapon.WeaponContext.Contexts;
using UnityEngine;
using System.Text.Json;
using EWC.Utils;
using AIGraph;
using LevelGeneration;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Traits
{
    internal class WallPierce : 
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPostRayContext>
    {
        private static RaycastHit s_rayHit;
        private static Queue<AIG_CourseNode> s_nodeQueue = new();

        public void Invoke(WeaponPostRayContext context)
        {
            if (!context.Result || CWC.HasTrait(typeof(ThickBullet)) || CWC.HasTrait(typeof(Projectile))) return;
            if (context.Data.RayHit.collider == null) return;
            if (context.Data.RayHit.collider.gameObject.IsInLayerMask(LayerUtil.MaskEntity3P)) return;

            if (!Physics.Raycast(Weapon.s_ray, out s_rayHit, context.Data.maxRayDist, LayerUtil.MaskEntity3P)) return;

            IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(s_rayHit);
            if (damageable == null || !IsTargetReachable(CWC.Weapon.Owner.CourseNode, damageable.GetBaseAgent()?.CourseNode)) return;

            context.Result = true;
            context.Data.RayHit = s_rayHit;
        }

        internal static bool IsTargetReachable(AIG_CourseNode? source, AIG_CourseNode? target)
        {
            if (source == null || target == null) return false;
            if (source.NodeID == target.NodeID) return true;

            AIG_SearchID.IncrementSearchID();
            ushort searchID = AIG_SearchID.SearchID;
            s_nodeQueue.Enqueue(source);

            while (s_nodeQueue.Count > 0)
            {
                AIG_CourseNode current = s_nodeQueue.Dequeue();
                current.m_searchID = searchID;
                foreach (AIG_CoursePortal portal in current.m_portals)
                {
                    LG_SecurityDoor? secDoor = portal.Gate?.SpawnedDoor?.TryCast<LG_SecurityDoor>();
                    if (secDoor != null)
                    {
                        if (secDoor.LastStatus != eDoorStatus.Open && secDoor.LastStatus != eDoorStatus.Opening)
                            continue;
                    }
                    AIG_CourseNode nextNode = portal.GetOppositeNode(current);
                    if (nextNode.m_searchID == searchID) continue;
                    if (nextNode.NodeID == target.NodeID)
                    {
                        s_nodeQueue.Clear();
                        return true;
                    }
                    s_nodeQueue.Enqueue(nextNode);
                }
            }
            s_nodeQueue.Clear();
            return false;
        }

        public override IWeaponProperty Clone()
        {
            return new WallPierce();
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader) { }
    }
}
