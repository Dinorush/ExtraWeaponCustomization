using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Shared.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ActionLock :
        Effect,
        IWeaponProperty<WeaponPreSwapContext>,
        IWeaponProperty<WeaponPreSprintContext>,
        IWeaponProperty<WeaponFireCancelContext>,
        IWeaponProperty<WeaponPreReloadContext>
    {
        public PlayerAction Actions { get; private set; } = PlayerAction.None;
        public float Duration { get; private set; } = 0f;

        private float _endTime = 0f;

        protected override OwnerType RequiredOwnerType => OwnerType.Local;

        public override bool ShouldRegister(Type contextType)
        {
            if (!Actions.HasFlag(PlayerAction.Swap) && contextType == typeof(WeaponPreSwapContext))
                return false;
            if (!Actions.HasFlag(PlayerAction.Sprint) && contextType == typeof(WeaponPreSprintContext))
                return false;
            if (!Actions.HasFlag(PlayerAction.Fire) && contextType == typeof(WeaponFireCancelContext))
                return false;
            if (!Actions.HasFlag(PlayerAction.Reload) && contextType == typeof(WeaponPreReloadContext))
                return false;
            return base.ShouldRegister(contextType);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (Actions.HasFlag(PlayerAction.Sprint))
            {
                var locomotion = CWC.Owner.Player!.Locomotion;
                if (locomotion.m_currentStateEnum == Player.PlayerLocomotion.PLOC_State.Run)
                    locomotion.ChangeState(Player.PlayerLocomotion.PLOC_State.Stand);
            }
            _endTime = Clock.Time + Duration;
        }

        public override void TriggerReset() => _endTime = 0f;

        public void Invoke(WeaponPreSwapContext context)
        {
            context.Allow = context.Allow && _endTime < Clock.Time;
        }

        public void Invoke(WeaponPreSprintContext context)
        {
            context.Allow = context.Allow && _endTime < Clock.Time;
        }

        public void Invoke(WeaponFireCancelContext context)
        {
            context.Allow = context.Allow && _endTime < Clock.Time;
        }

        public void Invoke(WeaponPreReloadContext context)
        {
            context.Allow = context.Allow && _endTime < Clock.Time;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteString(nameof(Actions), Actions.ToString());
            writer.WriteNumber(nameof(Duration), Duration);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "actions":
                case "action":
                    Actions = ToPlayerAction(reader.GetString()!);
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
            }
        }

        private static PlayerAction ToPlayerAction(string value)
        {
            value = value.Replace(" ", null).ToLower();
            if (value == "all")
                return PlayerAction.All;

            PlayerAction result = PlayerAction.None;
            if (value.Contains("swap"))
                result |= PlayerAction.Swap;
            if (value.Contains("sprint") || value.Contains("run"))
                result |= PlayerAction.Sprint;
            if (value.Contains("fire") || value.Contains("shoot"))
                result |= PlayerAction.Fire;
            if (value.Contains("reload"))
                result |= PlayerAction.Reload;
            return result;
        }
    }

    [Flags]
    public enum PlayerAction
    {
        None = 0,
        Swap = 1,
        Sprint = 1 << 1,
        Fire = 1 << 2,
        Reload = 1 << 3,
        All = -1
    }
}
