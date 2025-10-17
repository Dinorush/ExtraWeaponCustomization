using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;
using AIGraph;
using LevelGeneration;
using System.Collections.Generic;
using System;
using EWC.CustomWeapon.Enums;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class WallPierce : 
        Trait,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        private static readonly Queue<AIG_CourseNode> s_nodeQueue = new();

        public bool RequireOpenPath { get; private set; } = false;

        protected override OwnerType RequiredOwnerType => OwnerType.Managed;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public void Invoke(WeaponSetupContext context)
        {
            CGC.ShotComponent.WallPierce = this;
        }

        public void Invoke(WeaponClearContext context)
        {
            CGC.ShotComponent.WallPierce = null;
        }

        public bool IsTargetReachable(AIG_CourseNode? source, AIG_CourseNode? target)
        {
            if (source == null || target == null) return true;
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
                    iLG_Door_Core? door = portal.m_door;
                    // If we don't require an open path, ignore non-security door plugs
                    if (!RequireOpenPath && door != null && door.DoorType != eLG_DoorType.Security && door.DoorType != eLG_DoorType.Apex)
                        door = null;
                        
                    // Don't pass through closed doors
                    if (door != null && door.LastStatus != eDoorStatus.Open && door.LastStatus != eDoorStatus.Opening && door.LastStatus != eDoorStatus.Destroyed)
                            continue;
                    
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

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteBoolean(nameof(RequireOpenPath), RequireOpenPath);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "requireopenpath":
                    RequireOpenPath = reader.GetBoolean();
                    break;
            }
        }
    }
}
