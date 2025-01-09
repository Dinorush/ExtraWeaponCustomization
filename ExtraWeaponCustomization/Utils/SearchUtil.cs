using Agents;
using AIGraph;
using Enemies;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.Utils
{
    [Flags]
    internal enum SearchSetting
    {
        None = 0,
        Alloc = 1,
        CacheHit = 2,
        CheckLOS = 4,
        CheckDoors = 8,
        CheckOwner = 16,
        CheckFriendly = 32,
        IgnoreDupes = 64,
    }

    internal static class SearchUtil
    {
        private static readonly List<EnemyAgent> s_enemyCache = new();
        private static readonly List<(EnemyAgent, RaycastHit)> s_combinedCache = new();
        private static readonly List<(PlayerAgent, RaycastHit)> s_combinedCachePlayer = new();
        private static readonly Queue<AIG_CourseNode> s_nodeQueue = new();

        private static readonly List<RaycastHit> s_lockCache = new();

        public static HashSet<IntPtr>? DupeCheckSet;
        public static int SightBlockLayer = 0;
        public const float WeakspotBufferDist = 0.1f;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private const float Epsilon = 1e-5f;

        // Bounds.ClosestPoint doesn't actually return the closest point
        private static Vector3 ClosestPointOnBounds(Bounds bounds, Vector3 point)
        {
            return new(
                Math.Clamp(point.x, bounds.min.x, bounds.max.x),
                Math.Clamp(point.y, bounds.min.y, bounds.max.y),
                Math.Clamp(point.z, bounds.min.z, bounds.max.z)
                );
        }

        private static bool PortalInRange(Ray ray, float range, float angle, AIG_CoursePortal portal)
        {
            Bounds bounds = portal.m_cullPortal.m_portalBounds;
            Vector3 closest = ClosestPointOnBounds(bounds, ray.origin);
            if ((ray.origin - closest).sqrMagnitude > range * range) return false;

            if (angle >= 180f || bounds.Contains(ray.origin)) return true;

            Vector3 localCenter = portal.m_cullPortal.m_center - ray.origin;
            float dot = Vector3.Dot(localCenter, ray.direction);
            if (angle == 90f) return dot >= 0; // Can't use Tan on 90 angle

            // Angle detection: Project to get the closest (perpendicular) point to center,
            //   then check if it is within the valid distance at that point on the viewing cone.
            closest = Vector3.Project(localCenter, ray.direction);
            float diagonal = portal.m_cullPortal.m_portalBounds.extents.magnitude;
            float reqDist = closest.magnitude * (float)Math.Tan(angle * Mathf.Deg2Rad);
            if (reqDist < 0) // If angle > 90, need to account for backwards angles
            {
                // If in front or perpendicular, valid
                if (dot >= 0) return true;
                // Otherwise, it must be outside the circle instead
                reqDist = Math.Max(0f, reqDist + diagonal);
                return (localCenter - closest).sqrMagnitude >= reqDist * reqDist;
            }
            
            if (dot <= 0) return false; // Angle < 90, ignore angles behind us
            reqDist += diagonal;
            return (localCenter - closest).sqrMagnitude <= reqDist * reqDist;
        }

        private static bool RaycastEnsured(Collider collider, Vector3 backupOrigin, float range, out RaycastHit hit)
        {
            // This should rarely ever fail
            if (collider.Raycast(s_ray, out hit, range)) return true;

            // I LOVE RAYCAST TO CLOSEST POINT FAILING!!! (it's probably tangent to the collider)
            Vector3 diff = collider.ClosestPoint(backupOrigin) - collider.bounds.center;
            Vector3 diffNormal = diff.normalized;
            s_ray.origin = collider.bounds.center + diff + diffNormal * Math.Min(0.1f, range / 2);
            s_ray.direction = -diffNormal;
            return collider.Raycast(s_ray, out hit, range);
        }

        private static bool TryGetClosestHit(Ray ray, float range, float angle, Agent agent, out RaycastHit hit, SearchSetting settings)
        {
            hit = default;
            if (agent == null || !agent.Alive) return false;

            if (settings.HasFlag(SearchSetting.IgnoreDupes))
            {
                if (agent.Type == AgentType.Enemy && DupeCheckSet?.Contains(agent.Cast<EnemyAgent>().Damage.Pointer) == true)
                    return false;
                else if (agent.Type == AgentType.Player && DupeCheckSet?.Contains(agent.Cast<PlayerAgent>().Damage.Pointer) == true)
                    return false;
            }

            s_ray.origin = ray.origin;
            float sqrRange = range * range;
            float minDist = sqrRange;
            Collider? minCollider = null;
            bool casted = false;
            foreach (var collider in agent.GetComponentsInChildren<Collider>())
            {
                Dam_EnemyDamageLimb? limb = null;
                if (agent.Type == AgentType.Enemy)
                {
                    limb = collider.GetComponent<Dam_EnemyDamageLimb>();
                    if (limb == null || limb.IsDestroyed) continue;
                }
                else if (agent.Type == AgentType.Player && collider.GetComponent<IDamageable>() == null)
                    continue;

                Vector3 trgtPos = collider.ClosestPoint(ray.origin);
                Vector3 diff = trgtPos - ray.origin;
                float sqrDist = diff.sqrMagnitude;
                float origDist = sqrDist;
                if (limb?.m_type == eLimbDamageType.Weakspot && sqrDist < sqrRange)
                {
                    float newDist = Math.Max(diff.magnitude - WeakspotBufferDist, 0);
                    sqrDist = newDist * newDist;
                }

                if (sqrDist < minDist && Vector3.Angle(ray.direction, trgtPos - ray.origin) <= angle)
                {
                    if (settings.HasFlag(SearchSetting.CheckLOS) && Physics.Linecast(ray.origin, trgtPos, SightBlockLayer)) continue;

                    minDist = sqrDist;
                    minCollider = collider;
                    // If not caching a hit to the closest collider, can just add the enemy as soon as one is valid
                    if (!settings.HasFlag(SearchSetting.CacheHit)) break;

                    // If the distance is close to 0, need different logic since raycast will break
                    if (minDist < Epsilon)
                    {
                        if (origDist > Epsilon) break; // No point in searching further but can use the actual distance

                        s_ray.origin -= ray.direction * Math.Min(0.1f, range / 2);
                        s_ray.direction = trgtPos - s_ray.origin;
                        if (RaycastEnsured(collider, ray.origin, range, out hit))
                        {
                            hit.point = trgtPos;
                            hit.distance = 0;
                            casted = true;
                        }
                        else
                            minCollider = null;

                        break; // Can't get lower than 0 distance
                    }

                    s_ray.direction = diff;
                }
            }
            if (minCollider == null) return false;
            if (settings.HasFlag(SearchSetting.CacheHit) && !casted && !RaycastEnsured(minCollider, ray.origin, range, out hit))
                return false;

            return true;
        }

        private static void CacheEnemiesInRange(Ray ray, float range, float angle, AIG_CourseNode origin, SearchSetting settings)
        {
            AIG_SearchID.IncrementSearchID();
            ushort searchID = AIG_SearchID.SearchID;
            float sqrRange = range * range;
            s_nodeQueue.Enqueue(origin);
            origin.m_searchID = searchID;
            s_combinedCache.Clear();

            while (s_nodeQueue.TryDequeue(out AIG_CourseNode? node))
            {
                foreach (AIG_CoursePortal portal in node.m_portals)
                {
                    AIG_CourseNode oppositeNode = portal.GetOppositeNode(node);
                    if (settings.HasFlag(SearchSetting.CheckDoors) && !portal.IsTraversable)
                        continue;

                    if (oppositeNode.m_searchID != searchID && PortalInRange(ray, range, angle, portal))
                    {
                        oppositeNode.m_searchID = searchID;
                        s_nodeQueue.Enqueue(oppositeNode);
                    }
                }

                foreach (EnemyAgent enemy in node.m_enemiesInNode)
                {
                    if (enemy == null || !enemy.Alive || enemy.Damage.Health <= 0) continue;
                    if ((ClosestPointOnBounds(enemy.MovingCuller.Culler.Bounds, ray.origin) - ray.origin).sqrMagnitude > sqrRange) continue;
                    if (!TryGetClosestHit(ray, range, angle, enemy, out s_rayHit, settings)) continue;
                    s_combinedCache.Add((enemy, s_rayHit));
                }
            }
        }

        public static List<EnemyAgent> GetEnemiesInRange(Ray ray, float range, float angle, AIG_CourseNode origin, SearchSetting settings = SearchSetting.None)
        {
            s_enemyCache.Clear();
            if (range == 0 || angle == 0)
                return settings.HasFlag(SearchSetting.Alloc) ? new List<EnemyAgent>() : s_enemyCache;

            CacheEnemiesInRange(ray, range, angle, origin, settings);
            if (settings.HasFlag(SearchSetting.Alloc))
                return s_combinedCache.ConvertAll(pair => pair.Item1);

            foreach ((EnemyAgent enemy, _) in s_combinedCache)
                s_enemyCache.Add(enemy);
            return s_enemyCache;
        }

        public static List<(EnemyAgent enemy, RaycastHit hit)> GetEnemyHitsInRange(Ray ray, float range, float angle, AIG_CourseNode origin, SearchSetting settings = SearchSetting.CacheHit)
        {
            if (range == 0 || angle == 0)
            {
                s_combinedCache.Clear();
                return settings.HasFlag(SearchSetting.Alloc) ? new List<(EnemyAgent, RaycastHit)>() : s_combinedCache;
            }

            settings |= SearchSetting.CacheHit;
            CacheEnemiesInRange(ray, range, angle, origin, settings);
            if (settings.HasFlag(SearchSetting.Alloc))
                return new(s_combinedCache);

            return s_combinedCache;
        }

        public static List<RaycastHit> GetLockHitsInRange(Ray ray, float range, float angle, SearchSetting settings = SearchSetting.None)
        {
            s_lockCache.Clear();
            if (range == 0 || angle == 0)
                return settings.HasFlag(SearchSetting.Alloc) ? new List<RaycastHit>() : s_lockCache;

            Collider[] colliders = Physics.OverlapSphere(ray.origin, range, LayerUtil.MaskDynamic);
            Vector3 origDir = ray.direction;
            foreach (Collider collider in colliders)
            {
                IDamageable? damageable = DamageableUtil.GetDamageableFromCollider(collider);
                if (damageable == null) continue;

                if (settings.HasFlag(SearchSetting.IgnoreDupes) && DupeCheckSet?.Contains(damageable.GetBaseDamagable().Pointer) == true) continue;

                if (settings.HasFlag(SearchSetting.CheckLOS)
                 && Physics.Linecast(ray.origin, damageable.DamageTargetPos, out s_rayHit, SightBlockLayer)
                 && s_rayHit.collider.gameObject.Pointer != collider.gameObject.Pointer)
                    continue;

                ray.direction = damageable.DamageTargetPos - ray.origin;
                if (collider.Raycast(ray, out s_rayHit, range) && Vector3.Angle(ray.direction, origDir) < angle)
                    s_lockCache.Add(s_rayHit);
            }

            if (settings.HasFlag(SearchSetting.Alloc))
                return new(s_lockCache);

            return s_lockCache;
        }

        public static List<(PlayerAgent, RaycastHit)> GetPlayerHitsInRange(Ray ray, float range, float angle, SearchSetting settings = SearchSetting.CheckFriendly | SearchSetting.CheckOwner)
        {
            s_combinedCachePlayer.Clear();
            if (range == 0 || angle == 0)
                return settings.HasFlag(SearchSetting.Alloc) ? new List<(PlayerAgent, RaycastHit)>() : s_combinedCachePlayer;

            // Players have one collider; checking LoS can easily put the floor as the closest point, so this uses a custom position
            float sqrRange = range * range;
            foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
            {
                if (player == null || !player.Alive) continue;
                if ((ClosestPointOnBounds(player.m_movingCuller.Culler.Bounds, ray.origin) - ray.origin).sqrMagnitude > sqrRange) continue;
                if (player.IsLocallyOwned)
                {
                    if (!settings.HasFlag(SearchSetting.CheckOwner)) continue;
                    s_ray.origin = ray.origin;
                    s_ray.direction = player.Damage.DamageTargetPos - ray.origin;
                    if (!player.GetComponent<Collider>().Raycast(s_ray, out s_rayHit, range)) continue;
                    if (settings.HasFlag(SearchSetting.CheckLOS) && Physics.Linecast(ray.origin, s_rayHit.point, SightBlockLayer)) continue;
                }
                else if (!settings.HasFlag(SearchSetting.CheckFriendly) || !TryGetClosestHit(ray, range, angle, player, out s_rayHit, settings))
                    continue;

                s_combinedCachePlayer.Add((player, s_rayHit));
            }

            if (settings.HasFlag(SearchSetting.Alloc))
                return new(s_combinedCachePlayer);

            return s_combinedCachePlayer;
        }


        private static bool HasCluster(AIG_VoxelNodePillar pillar)
        {
            foreach (var node in pillar.m_nodes)
                if (node.ClusterID != 0 && AIG_NodeCluster.TryGetNodeCluster(node.ClusterID, out _))
                    return true;
            return false;
        }

        public static AIG_CourseNode GetCourseNode(Vector3 position, Agent agent)
        {
            Vector3 source = agent.Position;
            var dimension = Dimension.GetDimensionFromPos(position);
            if (dimension != null && TryGetGeomorphVolumeSilent(dimension.DimensionIndex, position, out var volume))
            {
                position.y = volume.Position.y;
                source.y = position.y;
                Vector3 move = (source - position).normalized;
                AIG_VoxelNodePillar? pillar = null;

                for (int i = 0; i < 10 && (!volume.m_voxelNodeVolume.TryGetPillar(position, out pillar) || !HasCluster(pillar)); i++)
                    position += move;

                if (pillar == null)
                    return agent.CourseNode;

                foreach (var voxelNode in pillar.m_nodes)
                    if (voxelNode.ClusterID != 0 && AIG_NodeCluster.TryGetNodeCluster(voxelNode.ClusterID, out var nodeCluster) && nodeCluster.CourseNode != null)
                        return nodeCluster.CourseNode;
            }
            return agent.CourseNode;
        }

        // Copy-pasted code without logging errors
        private static bool TryGetGeomorphVolumeSilent(eDimensionIndex dimensionIndex, Vector3 pos, [MaybeNullWhen(false)] out AIG_GeomorphNodeVolume resultingGeoVolume)
        {
            resultingGeoVolume = null;
            LG_Floor currentFloor = Builder.Current.m_currentFloor;
            if (currentFloor == null) return false;
            if (!currentFloor.GetDimension(dimensionIndex, out var dimension)) return false;
            if (dimension.Grid == null || !TryGetCell(dimension.Grid, pos, out LG_Cell? cell)) return false;
            if (cell.m_grouping == null || cell.m_grouping.m_geoRoot == null) return false;

            resultingGeoVolume = cell.m_grouping.m_geoRoot.m_nodeVolume.TryCast<AIG_GeomorphNodeVolume>();
            return resultingGeoVolume != null;
        }

        private static bool TryGetCell(LG_Grid grid, Vector3 pos, [MaybeNullWhen(false)] out LG_Cell cell)
        {
            pos -= grid.m_gridPosition;
            int x = (int)Math.Round((pos.x - grid.m_cellDimHalf) / grid.m_cellDim);
            int z = (int)Math.Round((pos.z - grid.m_cellDimHalf) / grid.m_cellDim);
            if (x < 0 || z < 0 || x >= grid.m_sizeX || z >= grid.m_sizeZ)
            {
                cell = null;
                return false;
            }            
            
            cell = grid.GetCell(x, z);
            return true;
        }
    }
}
