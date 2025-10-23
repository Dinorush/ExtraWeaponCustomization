using Agents;
using Enemies;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class AutoAim : 
        Trait,
        ITriggerCallback,
        ITriggerEvent,
        IWeaponProperty<WeaponCreatedContext>,
        IWeaponProperty<WeaponUnWieldContext>,
        IWeaponProperty<WeaponUpdateContext>,
        IWeaponProperty<WeaponClearContext>,
        IWeaponProperty<WeaponPreStartFireContext>,
        IWeaponProperty<WeaponFireCancelContext>,
        IWeaponProperty<WeaponPreRayContext>
    {
        public bool HipActive { get; private set; } = false;
        public bool AimActive { get; private set; } = true;
        public float Angle { get; private set; } = 0f;
        public float Range { get; private set; } = 0f;
        public bool RequireLOS { get; private set; } = true;
        public float LockTime { get; private set; } = 0f;
        public float LockDecayTime { get; private set; } = 0f;
        public bool StayOnTarget { get; private set; } = false;
        public bool StayRequireLOS { get; private set; } = true;
        public float StayAngle { get; private set; } = -1f;
        public float StayRange { get; private set; } = -1f;
        public bool ResetOnNewTarget { get; private set; } = false;
        public bool RequireLock { get; private set; } = false;
        public bool LockWhileEmpty { get; private set; } = true;
        public bool TagOnly { get; private set; } = false;
        public bool IgnoreInvisibility { get; private set; } = false;
        public TargetingMode TargetMode { get; private set; } = TargetingMode.Normal;
        public TargetingPriority TargetPriority { get; private set; } = TargetingPriority.Angle;
        public bool FavorLookPoint { get; private set; } = false;
        public bool HomingOnly { get; private set; } = false;
        public Color LockedColor { get; private set; } = new(1.2f, 0.3f, 0.1f, 1f);
        public Color LockingColor { get; private set; } = new(0.8f, 0.3f, 0.2f, 1f);
        public float LockScale { get; private set; } = 1f;
        public bool UseTrigger { get; private set; } = false;
        private TriggerCoordinator? _coordinator;
        public TriggerCoordinator? Trigger
        {
            get => _coordinator;
            set
            {
                _coordinator = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        public CrosshairHitIndicator? _reticle;
        private FPSCamera? _camera;
        private float _detectionTick;
        private EnemyAgent? _target;
        private bool _hasTarget = false;
        private Vector3 _lastTargetPos;
        private bool _reticleActive;
        private bool _lastAutoAim = false;
        private float _progress;
        private float _lastUpdateTime;
        private HashSet<BaseDamageableWrapper>? _triggerTargets;
        private HashSet<ObjectWrapper<EnemyAgent>>? _triggerAgents;
        private static readonly TriggerEventHelper _eventHelper = new(CallbackMap);

        private float _weakspotDetectionTick;
        private Collider? _weakspotCollider;
        private byte _weakspotLimbID;
        private Transform? _weakspotTarget;
        private readonly List<Collider> _bodyList = new();
        private readonly List<(Collider collider, byte limbID)> _weakspotList = new();

        private static Ray s_ray;
        private static RaycastHit s_raycastHit;

        private readonly Color _passiveDetection = new(0.5f, 0.5f, 0.5f, 1f);
        private readonly Vector3 _targetedAngles = new(0f, 0f, 45f);

        private static BaseDamageableWrapper TempDamWrapper => BaseDamageableWrapper.SharedInstance;
        private static ObjectWrapper<EnemyAgent> TempEnemyWrapper => ObjectWrapper<EnemyAgent>.SharedInstance;

        protected override OwnerType RequiredOwnerType => OwnerType.Local;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public override bool ShouldRegister(Type contextType)
        {
            if (!RequireLock && (contextType == typeof(WeaponPreStartFireContext) || contextType == typeof(WeaponFireCancelContext))) return false;
            if ((!UseTrigger || Trigger == null) && contextType == typeof(WeaponTriggerContext)) return false;
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponPreStartFireContext context) // Registered if RequireLock is true
        {
            context.Allow &= UseAutoAim;
        }

        public void Invoke(WeaponFireCancelContext context)
        {
            var gun = (LocalGunComp)CWC.Weapon;
            // We don't want to stop burst weapons from firing mid-burst, but we do want to stop fully automatic weapons.
            if (gun.FireMode == eWeaponFireMode.Burst && !gun.GunArchetype.BurstIsDone())
                return;

            context.Allow &= UseAutoAim;
        }

        public void Invoke(WeaponPreRayContext context)
        {
            if (_camera == null || HomingOnly) return;

            // Ignore pierced shots
            if (context.Data.maxRayDist < CGC.Gun.MaxRayDist) return;

            if (!UseAutoAim) return;

            // Prioritize aim if looking at the locked enemy
            if (FavorLookPoint)
            {
                s_ray.origin = _camera.Position;
                s_ray.direction = context.Data.fireDir;
                if (Physics.Raycast(s_ray, out s_raycastHit, 100f, LayerUtil.MaskEntityAndWorld3P))
                {
                    Agent? agent = DamageableUtil.GetDamageableFromRayHit(s_raycastHit)?.GetBaseAgent();
                    if (agent != null && agent.Alive && (!RequireLock || agent == _target))
                        return; // Cancel auto aim (just shoot where user is aiming)
                }
            }

            context.Data.fireDir = (GetTargetPos() - _camera.Position).normalized;
        }

        public void Invoke(WeaponCreatedContext context)
        {
            _reticle = AutoAimReticle.Reticle;
            _camera = CWC.Owner.Player.FPSCamera;
        }

        public void Invoke(WeaponUnWieldContext _) => OnDisable();

        public void Invoke(WeaponClearContext _) => OnDisable();

        public void Invoke(WeaponUpdateContext _) 
        {
            if (!_reticleActive)
            {
                _reticleActive = true;
                OnEnable();
            }

            var oldTarget = _target;
            UpdateDetection();
            if (UseAutoAim && !_lastAutoAim)
            {
                _eventHelper.Invoke(CWC, new WeaponReferenceContext(ID, (int)Callback.Locked));
                _eventHelper.Invoke(CWC, new WeaponReferenceContext(ID, (int)Callback.NewLock));
            }
            else if (UseAutoAim && _target != oldTarget && _target != null)
                _eventHelper.Invoke(CWC, new WeaponReferenceContext(ID, (int)Callback.NewLock));
            else if (!UseAutoAim && _lastAutoAim)
                _eventHelper.Invoke(CWC, new WeaponReferenceContext(ID, (int)Callback.Unlocked));

            _lastAutoAim = UseAutoAim;
            UpdateAnimate();
        }

        public void Invoke(WeaponTriggerContext context) => Trigger!.Invoke(context);

        public void TriggerApply(List<TriggerContext> contexts)
        {
            if (!UseTrigger) return;

            foreach (var tContext in contexts)
            {
                var hitContext = (WeaponHitDamageableContextBase)tContext.context;
                if (!_triggerTargets!.Contains(TempDamWrapper.Set(hitContext.Damageable)))
                {
                    _triggerTargets.Add(new(TempDamWrapper));
                    _triggerAgents!.Add(new(TempDamWrapper.Object!.GetBaseAgent().Cast<EnemyAgent>()));
                }
            }
        }

        public void TriggerReset()
        {
            if (!UseTrigger) return;

            _triggerTargets!.Clear();
            _triggerAgents!.Clear();
        }

        public int GetCallbackID(string callbackName) => _eventHelper.GetCallbackID(callbackName);

        public void OnDisable()
        {
            if (_progress == 1f)
                _eventHelper.Invoke(CWC, new WeaponReferenceContext(ID, (int)Callback.Unlocked));

            _lastAutoAim = false;
            _reticleActive = false;
            _progress = 0f;
            _target = null;
            if (_reticle != null)
            {
                _reticle.transform.localScale = Vector3.zero;
                _reticle.SetVisible(false);
            }
        }

        public void OnEnable()
        {
            _reticle?.SetVisible(true);
        }

        private void UpdateDetection()
        {
            // If LockTime/LockDecayTime is 0, _progress is set straight to 0 or 1 immediately after target changes to prevent div by 0.
            if (_target != null && _progress < 1f)
                _progress = Math.Min(1f, _progress + (Clock.Time - _lastUpdateTime) / LockTime);
            else if (_target == null && _progress > 0f)
                _progress = Math.Max(0f, _progress - (Clock.Time - _lastUpdateTime) / LockDecayTime);

            _lastUpdateTime = Clock.Time;

            if (!CanLock)
            {
                if (LockDecayTime <= 0)
                    _progress = 0;
                _target = null;
                _hasTarget = false;
                return;
            }

            // If the target died, immediately re-acquire another one
            if (_hasTarget && (_target == null || !_target.Alive || _target.Damage.Health <= 0))
            {
                _target = null;
                _detectionTick = 0f;
            }

            if (_detectionTick >= Time.time) return;

            if (StayOnTarget && _target != null && !CheckTargetValid())
                _target = null;

            // Acquire new target if applicable
            if (_target == null || !StayOnTarget)
            {
                EnemyAgent? _lastTarget = _target;
                _target = CheckForTarget();
                _hasTarget = _target != null;

                if (_hasTarget && _target != _lastTarget)
                {
                    if (TargetMode == TargetingMode.Weakspot)
                        ResetWeakspotLists();
                    if (ResetOnNewTarget)
                        _progress = 0;
                }
            }

            // Prevents statements at the top of the func from doing div by 0
            if (_target == null && LockDecayTime <= 0)
                _progress = 0f;
            else if (_target != null && LockTime <= 0)
                _progress = 1f;

            // If Angle is 0, performant enough to not need delay (just a raycast)
            if (Angle > 0)
                _detectionTick = Time.time + Configuration.AutoAimTickDelay;
        }

        private void UpdateAnimate()
        {
            if (_target != null)
                _lastTargetPos = GetTargetPos();
            _reticle!.transform.position = _camera!.m_camera.WorldToScreenPoint(_lastTargetPos);

            if (UseAutoAim && HasAmmo)
            {
                _reticle!.m_hitColor = LockedColor;
                _reticle!.transform.localScale = Vector3.one * LockScale;
                _reticle.transform.localEulerAngles = _targetedAngles;
            }
            else if(LockedTarget)
            {
                _reticle!.m_hitColor = LockingColor;
                _reticle!.transform.localScale = Vector3.one * LockScale;
                _reticle.transform.localEulerAngles += new Vector3(0, 0, 1f);
            }
            else
            {
                // Color and rotation speed depend on square progress, size uses linear. Makes a more noticeable end to the lock.
                float sqProgress = _progress * _progress;
                if (_target != null)
                    _reticle!.transform.localEulerAngles += new Vector3(0, 0, sqProgress.Lerp(4, 1));
                else
                    _reticle!.transform.localEulerAngles += new Vector3(0, 0, 5);
                
                _reticle!.m_hitColor = GetLockingColor(sqProgress);
                _reticle.transform.localScale = Vector3.one * _progress.Lerp(0, 1f) * LockScale;
            }

            bool active = AutoAimActive && HasAmmo;
            _reticle.transform.localScale *= active ? 1.6f : 1f;
            _reticle.UpdateColorsWithAlphaMul(active ? 1.0f : 0.5f);
        }

        private Color GetLockingColor(float frac)
        {
            return new Color(
                    frac.Lerp(_passiveDetection.r, LockingColor.r),
                    frac.Lerp(_passiveDetection.g, LockingColor.g),
                    frac.Lerp(_passiveDetection.b, LockingColor.b),
                    1f
                );
        }

        private bool HasAmmo => CGC.Gun.GetCurrentClip() > 0 && !((LocalGunComp)CGC.Gun).Value.IsReloading;
        private bool CanLock => LockWhileEmpty || HasAmmo;
        private bool LockedTarget => _target != null && _progress == 1f;
        private bool AutoAimActive => (
               AimActive == InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS)
            || HipActive != InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS)
            );
        public bool UseAutoAim => LockedTarget && AutoAimActive;

        public (EnemyAgent?, Collider?, byte) GetTargets() => (_target, _weakspotCollider, _weakspotLimbID);

        private bool CheckTargetValid()
        {
            if (_target == null || !_target.Alive || _target.Damage.Health <= 0) return false;
            if (UseTrigger && !_triggerAgents!.Contains(TempEnemyWrapper.Set(_target))) return false;
            if (!EnemyIsVisible(_target)) return false;

            float range = StayRange >= 0 ? StayRange : Range;
            float angle = StayAngle >= 0 ? StayAngle : Angle;
            // Check if any part of the target is still valid
            Vector3 targetPos = GetTargetPos();
            s_ray.origin = _camera!.Position;
            s_ray.direction = _camera!.CameraRayDir;
            if (StayRequireLOS && Physics.Linecast(s_ray.origin, targetPos, LayerUtil.MaskWorldExcProj)) return false;

            foreach (var limb in _target.Damage.DamageLimbs)
            {
                if (!limb.TryGetComp<Collider>(out var collider) || !collider.enabled) continue;

                if (angle == 0)
                    return collider.Raycast(s_ray, out _, range);

                Vector3 diff = collider.ClosestPoint(s_ray.origin) - s_ray.origin;
                float sqrDist = diff.sqrMagnitude;
                if (sqrDist < range * range && Vector3.Angle(s_ray.direction, diff) < angle)
                    return true;
            }
            return false;
        }

        private EnemyAgent? CheckForTarget()
        {
            // s_ray may get changed by other functions, so to ensure it remains the same, use a local one
            Ray ray = new(_camera!.Position, _camera!.CameraRayDir);

            List<EnemyAgent> enemies;
            if (UseTrigger)
            {
                if (_triggerAgents!.Count == 0) return null;

                if (Angle == 0)
                {
                    SearchUtil.SightBlockLayer = LayerUtil.MaskWorld;
                    EnemyAgent? enemy = null;
                    if (SearchUtil.RaycastFirst(
                        ray,
                        out _,
                        Range,
                        LayerUtil.MaskEnemy, 
                        (damageable) => EnemyIsVisible(enemy = damageable.GetBaseAgent().Cast<EnemyAgent>()) && _triggerAgents.Contains(TempEnemyWrapper.Set(enemy)),
                        RequireLOS ? SearchSetting.CheckLOS : SearchSetting.None
                        ))
                        return enemy;
                    return null;
                }

                enemies = new(_triggerAgents.Count);
                foreach (var wrapper in _triggerAgents)
                {
                    var agent = wrapper.Object!;
                    if (SearchUtil.IsAgentInCone(ray, Range, Angle, agent, out _))
                        enemies.Add(agent);
                }
            }
            else if (Angle == 0)
            {
                SearchUtil.SightBlockLayer = LayerUtil.MaskWorld;
                EnemyAgent? enemy = null;
                if (SearchUtil.RaycastFirst(
                    ray,
                    out _,
                    Range,
                    LayerUtil.MaskEnemy,
                    (damageable) => EnemyIsVisible(enemy = damageable.GetBaseAgent().Cast<EnemyAgent>()),
                    RequireLOS ? SearchSetting.CheckLOS : SearchSetting.None
                    ))
                    return enemy;
                return null;
            }
            else
                enemies = SearchUtil.GetEnemiesInRange(ray, Range, Angle, CWC.Owner.CourseNode);

            if (enemies.Count == 0) return null;

            List<(EnemyAgent, float)>? angleList = null; // Needed for later comparisons with current target
            switch (TargetPriority)
            {
                case TargetingPriority.Angle:
                    angleList = enemies.ConvertAll(enemy => (enemy, Vector3.Angle(ray.direction, GetSearchTargetPos(enemy) - ray.origin)));
                    angleList.Sort(SortUtil.FloatTuple);
                    SortUtil.CopySortedList(angleList, enemies);
                    break;
                case TargetingPriority.Distance:
                    var distList = enemies.ConvertAll(enemy => (enemy, (GetSearchTargetPos(enemy) - ray.origin).sqrMagnitude));
                    distList.Sort(SortUtil.FloatTuple);
                    SortUtil.CopySortedList(distList, enemies);
                    break;
                case TargetingPriority.Health:
                    // Since we prefer higher HealthMax, need to invert angles so the reverse gets the right order
                    var healthList = enemies.ConvertAll(enemy => (enemy, enemy.Damage.HealthMax, 180f - Vector3.Angle(ray.direction, GetSearchTargetPos(enemy) - ray.origin)));
                    healthList.Sort(SortUtil.FloatTuple);
                    healthList.Reverse();
                    SortUtil.CopySortedList(healthList, enemies);
                    break;
                default:
                    return null;
            };

            // New targets scanned don't check weakspots for performance.
            // Still, we want to consider current target's weakspots (since it's already cached and feels better).
            if (TargetPriority == TargetingPriority.Angle && CheckTargetValid())
            {
                float targetAngle = Vector3.Angle(ray.direction, GetTargetPos() - ray.origin);
                foreach ((EnemyAgent enemy, float angle) in angleList!)
                {
                    if (angle >= targetAngle) return _target;
                    if (EnemyIsVisible(enemy) && (!RequireLOS || !Physics.Linecast(ray.origin, GetSearchTargetPos(enemy), LayerUtil.MaskWorldExcProj)))
                        return enemy;
                }
            }
            else
            {
                foreach (var enemy in enemies)
                    if (EnemyIsVisible(enemy) && (!RequireLOS || !Physics.Linecast(ray.origin, GetSearchTargetPos(enemy), LayerUtil.MaskWorldExcProj)))
                        return enemy;
            }

            return null;
        }

        private bool EnemyIsVisible(EnemyAgent enemy) => IgnoreInvisibility || (!enemy.RequireTagForDetection && !TagOnly) || enemy.IsTagged;

        private Vector3 GetSearchTargetPos(EnemyAgent enemy)
        {
            return TargetMode == TargetingMode.Body ? enemy.AimTargetBody.position : enemy.AimTarget.position;
        }

        private Vector3 GetTargetPos()
        {
            if (_target == null) return Vector3.zero;

            UpdateWeakspotLimb();

            return TargetMode switch
            {
                TargetingMode.Body => _target.AimTargetBody.position,
                TargetingMode.Weakspot => _weakspotTarget!.position,
                _ => _target.AimTarget.position,
            };
        }

        private void ResetWeakspotLists()
        {
            _weakspotList.Clear();
            _bodyList.Clear();
            _weakspotCollider = null;
            _weakspotLimbID = 0;
            if (_target == null) return;

            _weakspotTarget = _target.AimTarget;
            foreach (var limb in _target.Damage.DamageLimbs)
            {
                if (limb.TryGetComp<Collider>(out var collider) && collider.enabled)
                {
                    if (limb.m_type == eLimbDamageType.Weakspot)
                        _weakspotList.Add((collider, (byte)limb.m_limbID));
                    else
                        _bodyList.Add(collider);
                }
            }
        }

        private void UpdateWeakspotLimb()
        {
            if (TargetMode != TargetingMode.Weakspot) return;

            if (_weakspotList.Count == 0)
            {
                _weakspotTarget = _target!.AimTarget;
                return;
            }

            if (_weakspotLimbID != 0 && _weakspotCollider!.enabled && Clock.Time < _weakspotDetectionTick) return;
            _weakspotDetectionTick = Clock.Time + Configuration.AutoAimTickDelay;

            _weakspotList.Sort(WeakspotCompare);
            _weakspotList.Reverse();
            _weakspotCollider = null;
            _weakspotLimbID = 0;

            for (int i = _weakspotList.Count - 1; i >= 0; i--)
            {
                (Collider collider, byte limbID) = _weakspotList[i];
                if (collider == null || !collider.enabled)
                {
                    _weakspotList.RemoveAt(i);
                    continue;
                }

                s_ray.direction = collider.transform.position - _camera!.Position;
                s_ray.origin = _camera.Position;
                collider.Raycast(s_ray, out s_raycastHit, (collider.transform.position - _camera.Position).magnitude);

                if (!_bodyList.Any(collider => collider.Raycast(s_ray, out _, s_raycastHit.distance)) && (!StayRequireLOS || !Physics.Linecast(_camera.Position, collider.transform.position, LayerUtil.MaskWorldExcProj)))
                {
                    _weakspotTarget = collider.transform;
                    _weakspotCollider = collider;
                    _weakspotLimbID = limbID;
                    return;
                }
            }

            if (_weakspotList.Count > 0)
            {
                _weakspotCollider = _weakspotList[0].collider;
                _weakspotLimbID = _weakspotList[0].limbID;
                _weakspotTarget = _weakspotCollider.transform;
            }
            else
            {
                _weakspotTarget = _target!.AimTarget;
                _weakspotCollider = null;
                _weakspotLimbID = 0;
            }
        }

        private int WeakspotCompare((Collider? collider, byte) x, (Collider? collider, byte) y)
        {
            if (x.collider == null) return 1;
            if (y.collider == null) return -1;
            float angleX = Vector3.Angle(_camera!.CameraRayDir, x.collider.transform.position - _camera!.Position);
            float angleY = Vector3.Angle(_camera!.CameraRayDir, y.collider.transform.position - _camera!.Position);
            return angleX < angleY ? -1 : 1;
        }

        public override WeaponPropertyBase Clone()
        {
            var copy = (AutoAim)base.Clone();
            copy.Trigger = Trigger?.Clone();
            if (UseTrigger)
            {
                copy._triggerTargets = new();
                copy._triggerAgents = new();
            }
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteBoolean(nameof(HipActive), HipActive);
            writer.WriteBoolean(nameof(AimActive), AimActive);
            writer.WriteNumber(nameof(Angle), Angle);
            writer.WriteNumber(nameof(Range), Range);
            writer.WriteBoolean(nameof(RequireLOS), RequireLOS);
            writer.WriteNumber(nameof(LockTime), LockTime);
            writer.WriteNumber(nameof(LockDecayTime), LockDecayTime);
            writer.WriteBoolean(nameof(StayOnTarget), StayOnTarget);
            writer.WriteBoolean(nameof(StayRequireLOS), StayRequireLOS);
            writer.WriteNumber(nameof(StayAngle), StayAngle);
            writer.WriteNumber(nameof(StayRange), StayRange);
            writer.WriteBoolean(nameof(ResetOnNewTarget), ResetOnNewTarget);
            writer.WriteBoolean(nameof(RequireLock), RequireLock);
            writer.WriteBoolean(nameof(LockWhileEmpty), LockWhileEmpty);
            writer.WriteBoolean(nameof(TagOnly), TagOnly);
            writer.WriteBoolean(nameof(IgnoreInvisibility), IgnoreInvisibility);
            writer.WriteString(nameof(TargetMode), TargetMode.ToString());
            writer.WriteString(nameof(TargetPriority), TargetPriority.ToString());
            writer.WriteBoolean(nameof(FavorLookPoint), FavorLookPoint);
            writer.WriteBoolean(nameof(HomingOnly), HomingOnly);
            EWCJson.Serialize(writer, nameof(LockedColor), LockedColor);
            EWCJson.Serialize(writer, nameof(LockingColor), LockingColor);
            writer.WriteNumber(nameof(LockScale), LockScale);
            writer.WriteBoolean(nameof(UseTrigger), UseTrigger);
            writer.WriteNull(nameof(Trigger));
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "hipactive":
                case "hip":
                    HipActive = reader.GetBoolean();
                    break;
                case "aimactive":
                case "aim":
                    AimActive = reader.GetBoolean();
                    break;
                case "angle":
                    Angle = reader.GetSingle();
                    break;
                case "range":
                    Range = reader.GetSingle();
                    break;
                case "requirelos":
                    RequireLOS = reader.GetBoolean();
                    break;
                case "locktime":
                    LockTime = reader.GetSingle();
                    break;
                case "lockdecaytime":
                    LockDecayTime = reader.GetSingle();
                    break;
                case "stayontarget":
                    StayOnTarget = reader.GetBoolean();
                    break;
                case "stayrequirelos":
                    StayRequireLOS = reader.GetBoolean();
                    break;
                case "stayangle":
                    StayAngle = reader.GetSingle();
                    break;
                case "stayrange":
                    StayRange = reader.GetSingle();
                    break;
                case "resetonnewtarget":
                    ResetOnNewTarget = reader.GetBoolean();
                    break;
                case "requirelock":
                    RequireLock = reader.GetBoolean();
                    break;
                case "lockwhileempty":
                    LockWhileEmpty = reader.GetBoolean();
                    break;
                case "tagonly":
                    TagOnly = reader.GetBoolean();
                    break;
                case "ignoreinvisibility":
                case "ignoreinvis":
                    IgnoreInvisibility = reader.GetBoolean();
                    break;
                case "targetbody":
                    if (reader.GetBoolean())
                        TargetMode = TargetingMode.Body;
                    break;
                case "targetingmode":
                case "targetmode":
                    TargetMode = reader.GetString().ToEnum(TargetingMode.Normal);
                    break;
                case "targetingpriority":
                case "targetpriority":
                    TargetPriority = reader.GetString().ToEnum(TargetingPriority.Angle);
                    break;
                case "favorlookpoint":
                case "favourlookpoint":
                    FavorLookPoint = reader.GetBoolean();
                    break;
                case "homingonly":
                    HomingOnly = reader.GetBoolean();
                    break;
                case "lockedcolor":
                    LockedColor = EWCJson.Deserialize<Color>(ref reader);
                    break;
                case "lockingcolor":
                    LockingColor = EWCJson.Deserialize<Color>(ref reader);
                    break;
                case "lockscale":
                    LockScale = reader.GetSingle();
                    break;
                case "usetrigger":
                    UseTrigger = reader.GetBoolean();
                    if (UseTrigger)
                    {
                        _triggerTargets = new();
                        _triggerAgents = new();
                    }
                    break;
                case "triggertype":
                case "trigger":
                    Trigger = TriggerCoordinator.Deserialize(ref reader);
                    VerifyTriggers();
                    break;
                default:
                    break;
            }
        }

        private void VerifyTriggers()
        {
            if (Trigger == null) return;

            for (int i = Trigger.Activate.Triggers.Count - 1; i >= 0; i--)
            {
                TriggerName name = Trigger.Activate.Triggers[i].Name;
                if (!ITrigger.HitTriggers.Contains(name))
                {
                    EWCLogger.Warning($"{GetType().Name} has an invalid trigger {name}. Only the following are allowed: {string.Join(", ", ITrigger.HitTriggers)}");
                    Trigger.Activate.Triggers.RemoveAt(i);
                    continue;
                }
            }

            if (Trigger?.Activate.Triggers.Any() != true)
                Trigger = null;
        }

        private static int CallbackMap(string callback) => callback switch
        {
            "locked" => (int)Callback.Locked,
            "unlocked" => (int)Callback.Unlocked,
            "newlock" => (int)Callback.NewLock,
            _ => 0
        };

        enum Callback
        {
            Locked,
            Unlocked,
            NewLock
        }
    }
    
    public enum TargetingMode
    {
        Normal,
        Body,
        Weakspot
    }

    public enum TargetingPriority
    {
        Angle,
        Distance,
        Health
    }
}
