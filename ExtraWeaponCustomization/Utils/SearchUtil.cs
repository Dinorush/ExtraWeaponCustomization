using Agents;
using AIGraph;
using Enemies;
using LevelGeneration;
using System;
using System.Collections.Generic;
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
        CheckDoors = 8
    }

    internal static class SearchUtil
    {
        private static readonly List<EnemyAgent> s_enemyCache = new();
        private static readonly List<(EnemyAgent, RaycastHit)> s_combinedCache = new();
        private static readonly Queue<AIG_CourseNode> s_nodeQueue = new();

        private static readonly List<RaycastHit> s_lockCache = new();

        public static int SightBlockLayer = 0;

        private static Ray s_ray;
        private static RaycastHit s_rayHit;
        private const float Epsilon = 1e-5f;

        // Bounds.ClosestPoint doesn't actually return the closest point
        private static Vector3 ClosestPointOnBounds(Bounds bounds, Vector3 point)
        {
            return new(
                Mathf.Clamp(point.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(point.y, bounds.min.y, bounds.max.y),
                Mathf.Clamp(point.z, bounds.min.z, bounds.max.z)
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
            float reqDist = closest.magnitude * Mathf.Tan(angle * Mathf.Deg2Rad);
            if (reqDist < 0) // If angle > 90, need to account for backwards angles
            {
                // If in front or perpendicular, valid
                if (dot >= 0) return true;
                // Otherwise, it must be outside the circle instead
                reqDist = Mathf.Max(0f, reqDist + diagonal);
                return (localCenter - closest).sqrMagnitude >= reqDist * reqDist;
            }
            
            if (dot <= 0) return false; // Angle < 90, ignore angles behind us
            reqDist += diagonal;
            return (localCenter - closest).sqrMagnitude <= reqDist * reqDist;
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
                s_ray.origin = ray.origin;

                foreach (EnemyAgent enemy in node.m_enemiesInNode)
                {
                    if (enemy == null || !enemy.Alive) continue;
                    if ((ClosestPointOnBounds(enemy.MovingCuller.Culler.Bounds, ray.origin) - ray.origin).sqrMagnitude > sqrRange) continue;

                    float minDist = sqrRange;
                    Collider? minCollider = null;
                    foreach (var collider in enemy.GetComponentsInChildren<Collider>())
                    {
                        Dam_EnemyDamageLimb? limb = collider.GetComponent<Dam_EnemyDamageLimb>();
                        if (limb?.IsDestroyed == true) continue;

                        Vector3 trgtPos = collider.ClosestPoint(ray.origin);
                        float sqrDist = (trgtPos - ray.origin).sqrMagnitude;
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
                                s_ray.origin -= ray.direction * Math.Min(0.1f, range/2);
                                s_ray.direction = trgtPos - s_ray.origin;
                                if (collider.Raycast(s_ray, out s_rayHit, range))
                                {
                                    s_rayHit.point = trgtPos;
                                    s_rayHit.distance = 0;
                                }
                                else
                                    minCollider = null;

                                s_ray.origin = ray.origin;
                                break; // Can't get lower than 0 distance
                            }
                            s_ray.direction = trgtPos - ray.origin;
                        }
                    }
                    if (minCollider == null) continue;
                    if (settings.HasFlag(SearchSetting.CacheHit) && minDist >= Epsilon && !minCollider.Raycast(s_ray, out s_rayHit, range)) continue;

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

        public static List<(EnemyAgent enemy, RaycastHit hit)> GetHitsInRange(Ray ray, float range, float angle, AIG_CourseNode origin, SearchSetting settings = SearchSetting.CacheHit)
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

                if (settings.HasFlag(SearchSetting.CheckLOS)
                 && Physics.Linecast(ray.origin, damageable.DamageTargetPos, out s_rayHit, SightBlockLayer)
                 && s_rayHit.collider.gameObject.GetInstanceID() != collider.gameObject.GetInstanceID())
                    continue;

                ray.direction = damageable.DamageTargetPos - ray.origin;
                if (collider.Raycast(ray, out s_rayHit, range) && Vector3.Angle(ray.direction, origDir) < angle)
                    s_lockCache.Add(s_rayHit);
            }

            if (settings.HasFlag(SearchSetting.Alloc))
                return new(s_lockCache);

            return s_lockCache;
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
            if (AIG_GeomorphNodeVolume.TryGetGeomorphVolume(0, Dimension.GetDimensionFromPos(position).DimensionIndex, position, out var volume))
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
    }
}
