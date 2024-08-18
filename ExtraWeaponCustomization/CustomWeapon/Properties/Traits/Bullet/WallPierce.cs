using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using UnityEngine;
using System.Text.Json;
using ExtraWeaponCustomization.Utils;
using AIGraph;
using LevelGeneration;
using System.Collections.Generic;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    internal class WallPierce : 
        Trait,
        IWeaponProperty<WeaponPostRayContext>
    {
        private static RaycastHit s_rayHit;

        public void Invoke(WeaponPostRayContext context)
        {
            if (!context.Result) return;
            if (context.Data.rayHit.collider.gameObject.IsInLayerMask(LayerManager.MASK_BULLETWEAPON_PIERCING_PASS)) return;

            if (!Physics.Raycast(Weapon.s_ray, out s_rayHit, context.Data.maxRayDist, LayerManager.MASK_BULLETWEAPON_PIERCING_PASS)) return;

            IDamageable? damageable = DamageableUtil.GetDamageableFromRayHit(s_rayHit);
            if (damageable == null || !IsTargetReachable(context.Weapon.Owner.CourseNode, damageable.GetBaseAgent()?.CourseNode)) return;

            context.Result = true;
            context.Data.rayHit = s_rayHit;
        }

        internal static bool IsTargetReachable(AIG_CourseNode? source, AIG_CourseNode? target)
        {
            if (source == null || target == null) return false;
            if (source.NodeID == target.NodeID) return true;

            AIG_SearchID.IncrementSearchID();
            ushort searchID = AIG_SearchID.SearchID;
            Queue<AIG_CourseNode> queue = new Queue<AIG_CourseNode>();
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                AIG_CourseNode current = queue.Dequeue();
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
                    if (nextNode.NodeID == target.NodeID) return true;
                    queue.Enqueue(nextNode);
                }
            }

            return false;
        }

        public override IWeaponProperty Clone()
        {
            return new WallPierce();
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options) { }
    }
}
