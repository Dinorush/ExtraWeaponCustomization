using AIGraph;
using Enemies;
using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Managers;
using EWC.Utils;
using EWC.Utils.Log;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components
{
    public sealed class EWCProjectileHoming
    {
        private readonly EWCProjectileComponentBase _base;

        private ProjectileHomingSettings? _settings;
        private PlayerAgent _owner;
        private bool _isLocal;
        private bool _wallPierce;
        private bool _enabled = false;

        private bool _homingEnabled;
        private float _homingStartTime;
        private float _initialHomingEndTime;
        private EnemyAgent? _homingAgent;
        private Dam_EnemyDamageLimb? _homingLimb;
        private Transform? _homingTarget;
        private readonly List<Dam_EnemyDamageLimb> _weakspotList = new();
        private bool _hadTarget;
        private float _nextSearchTime;

        private static Vector3 s_position;
        private static Vector3 s_dir;
        private readonly static Queue<AIG_CourseNode> s_nodeQueue = new();

#pragma warning disable CS8618
        // Hidden null warnings since other methods will initialize members prior to Update
        public EWCProjectileHoming(EWCProjectileComponentBase comp)
        {
            _base = comp;
        }
#pragma warning restore CS8618

        public void Init(Projectile projBase, bool isLocal, Vector3 position, Vector3 dir)
        {
            if (_enabled) return;

            _enabled = true;
            _isLocal = isLocal;
            _owner = projBase.CWC.Weapon.Owner;
            _settings = projBase.HomingSettings;
            _wallPierce = projBase.CWC.HasTrait(typeof(WallPierce));

            _homingStartTime = Time.time + _settings.HomingDelay;
            _initialHomingEndTime = _homingStartTime + _settings.InitialHomingDuration;
            _homingEnabled = _settings.HomingEnabled;
            _nextSearchTime = 0f;
            _homingAgent = null;
            _homingLimb = null;
            if (_isLocal && _homingEnabled)
            {
                s_position = position;
                s_dir = dir;
                if (_settings.SearchInitialMode == SearchMode.AutoAim)
                {
                    _homingEnabled = false;
                    AutoAim? autoAim = projBase.CWC.AutoAim;
                    if (autoAim == null || !autoAim.UseAutoAim) return;

                    (EnemyAgent? target, Dam_EnemyDamageLimb? limb) = autoAim.GetTargets();
                    _homingAgent = target;
                    ResetWeakspotList();
                    UpdateHomingTarget();
                    if (limb != null)
                    {
                        _homingLimb = limb;
                        _homingTarget = limb.transform;
                    }
                    _homingEnabled = true;
                }
                else
                {
                    if (_settings.SearchInitialMode == SearchMode.AimDir)
                        s_dir = _owner.FPSCamera.Forward;
                    FindHomingAgent();
                }
            }
        }

        public void Update(Vector3 position, ref Vector3 dir)
        {
            s_position = position;
            s_dir = dir;
            if (!_homingEnabled || Time.time < _homingStartTime || !UpdateHomingAgent()) return;

            float strength = _settings!.InitialHomingStrength;
            Vector3 diff = _homingTarget!.position - position;
            if (Time.time >= _initialHomingEndTime)
            {
                float distMod = diff.magnitude.Map(_settings.HomingMinDist, _settings.HomingMaxDist, 1f, 0f);
                strength = (float)(_settings.HomingStrength * Math.Pow(distMod, _settings.HomingDistExponent));
            }

            dir = Vector3.Slerp(dir, diff.normalized, Math.Min(strength * Time.deltaTime, 1f));
        }

        public void Die()
        {
            _enabled = false;
            _weakspotList.Clear();
        }

        private bool UpdateHomingAgent()
        {
            if (!_isLocal)
            {
                if (_homingAgent == null || !_homingAgent.Alive) return false;
                if (_homingLimb != null && _homingLimb.m_health > 0) return true;

                _homingTarget = GetHomingTarget(_homingAgent);
                _homingLimb = null;
                return true;
            }

            if (_homingAgent == null || !_homingAgent.Alive)
            {
                if (_settings!.SearchStopMode.HasFlag(StopSearchMode.Kill))
                {
                    _homingEnabled = false;
                    return false;
                }

                FindHomingAgent();
                return _homingAgent != null;
            }

            if (_homingAgent != null)
            {
                UpdateHomingTarget();
                return true;
            }
            return false;
        }

        public void SetHomingAgent(EnemyAgent agent, Dam_EnemyDamageLimb? limb)
        {
            _homingAgent = agent;
            _homingLimb = limb;
            _homingTarget = limb != null ? limb.transform : GetHomingTarget(agent);
        }

        public void UpdateOnPierce()
        {
            if (_settings!.SearchStopMode.HasFlag(StopSearchMode.Pierce))
            {
                _homingEnabled = false;
                return;
            }

            FindHomingAgent();
        }

        public void FindHomingAgent()
        {
            if (!_hadTarget && _nextSearchTime > Time.time)
            {
                _homingAgent = null;
                return;
            }

            _nextSearchTime = Time.time + Math.Max(Configuration.HomingTickDelay, _settings!.SearchCooldown);
            _hadTarget = false;
            _homingAgent = null;
            _homingLimb = null;
            _weakspotList.Clear();
            Ray ray = new(s_position, s_dir);
            SearchUtil.DupeCheckSet = _base.Hitbox.HitEnts;
            List<EnemyAgent> enemies = SearchUtil.GetEnemiesInRange(ray, _settings!.SearchRange, _settings.SearchAngle, SearchUtil.GetCourseNode(s_position, _owner), SearchSetting.IgnoreDupes);

            switch (_settings.TargetPriority)
            {
                case TargetingPriority.Angle:
                    var angleList = enemies.ConvertAll(enemy => (enemy, Vector3.Angle(ray.direction, GetHomingTargetPos(enemy) - ray.origin)));
                    angleList.Sort();
                    SortUtil.CopySortedList(angleList, enemies);
                    break;
                case TargetingPriority.Distance:
                    var distList = enemies.ConvertAll(enemy => (enemy, (GetHomingTargetPos(enemy) - ray.origin).sqrMagnitude));
                    distList.Sort();
                    SortUtil.CopySortedList(distList, enemies);
                    break;
                case TargetingPriority.Health:
                    // Since we prefer higher HealthMax, need to invert angles so the reverse gets the right order
                    var healthList = enemies.ConvertAll(enemy => (enemy, enemy.Damage.HealthMax, 180f - Vector3.Angle(ray.direction, GetHomingTargetPos(enemy) - ray.origin)));
                    healthList.Sort(SortUtil.FloatTriple);
                    healthList.Reverse();
                    SortUtil.CopySortedList(healthList, enemies);
                    break;
                default:
                    return;
            };

            foreach (var enemy in enemies)
            {
                if (!_settings.SearchIgnoreInvisibility && (enemy.RequireTagForDetection || _settings.SearchTagOnly) && !enemy.IsTagged) continue;
                if (!_settings.SearchIgnoreWalls && Physics.Linecast(ray.origin, GetHomingTargetPos(enemy), LayerUtil.MaskWorldExcProj)) continue;
                if (_settings.SearchIgnoreWalls && !IsTargetReachable(_owner.CourseNode, enemy.CourseNode)) continue;

                _homingAgent = enemy;
                break;
            }

            if (_homingAgent == null) return;

            _hadTarget = true;
            ResetWeakspotList();
            UpdateHomingTarget();
            byte limbID = (byte)(_homingLimb != null ? (byte)_homingLimb.m_limbID : 0);
            EWCProjectileManager.DoProjectileTarget(_base.PlayerIndex, _base.SyncID, _homingAgent, limbID);
        }

        private void ResetWeakspotList()
        {
            if (_settings!.TargetMode != TargetingMode.Weakspot) return;

            _weakspotList.Clear();
            foreach (Dam_EnemyDamageLimb limb in _homingAgent!.Damage.DamageLimbs)
                if (limb.m_health > 0 && limb.m_type == eLimbDamageType.Weakspot)
                    _weakspotList.Add(limb);
        }

        private void UpdateHomingTarget()
        {
            if (_settings!.TargetMode != TargetingMode.Weakspot || _weakspotList.Count == 0)
            {
                _homingLimb = null;
                _homingTarget = GetHomingTarget(_homingAgent!);
                return;
            }

            if (_homingLimb != null && _homingLimb.m_health > 0) return;

            _weakspotList.Sort(WeakspotCompare);
            _weakspotList.Reverse();
            for (int i = _weakspotList.Count - 1; i >= 0; i--)
            {
                Dam_EnemyDamageLimb weakspot = _weakspotList[i];
                if (weakspot == null || weakspot.m_health <= 0)
                {
                    _weakspotList.RemoveAt(i);
                    continue;
                }

                _homingLimb = _weakspotList[i];
                _homingTarget = _homingLimb.transform;
                return;
            }

            _homingTarget = _homingAgent!.AimTarget;
            _homingLimb = null;
        }

        private int WeakspotCompare(Dam_EnemyDamageLimb x, Dam_EnemyDamageLimb y)
        {
            float angleX = Vector3.Angle(s_dir, x.DamageTargetPos - s_position);
            float angleY = Vector3.Angle(s_dir, y.DamageTargetPos - s_position);
            if (angleX == angleY) return 0;
            return angleX < angleY ? -1 : 1;
        }
        private Transform GetHomingTarget(EnemyAgent enemy)
        {
            return _settings!.TargetMode == TargetingMode.Body ? enemy.AimTargetBody : enemy.AimTarget;
        }
        private Vector3 GetHomingTargetPos(EnemyAgent enemy)
        {
            return GetHomingTarget(enemy).position;
        }

        private bool IsTargetReachable(AIG_CourseNode? source, AIG_CourseNode? target)
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
                    iLG_Door_Core? door = portal.m_door;
                    if (_wallPierce && door != null && door.DoorType != eLG_DoorType.Security && door.DoorType != eLG_DoorType.Apex)
                        door = null;
                    if (door != null && door.LastStatus != eDoorStatus.Open && door.LastStatus != eDoorStatus.Opening && door.LastStatus != eDoorStatus.Destroyed)
                        continue;

                    AIG_CourseNode nextNode = portal.GetOppositeNode(current);
                    if (nextNode.NodeID == target.NodeID)
                    {
                        s_nodeQueue.Clear();
                        return true;
                    }
                    if (nextNode.m_searchID == searchID) continue;
                    s_nodeQueue.Enqueue(nextNode);
                }
            }
            s_nodeQueue.Clear();
            return false;
        }
    }
}
