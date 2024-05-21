﻿using GTFO.API;
using SNetwork;

namespace ExtraWeaponCustomization.Networking
{
    public abstract class SyncedEvent<T> where T : struct
    {
        public delegate void ReceiveHandler(T packet);

        public abstract string GUID { get; }
        public bool IsSetup { get => _isSetup; }
        public string EventName { get; private set; } = string.Empty;

        private bool _isSetup = false;

        public void Setup()
        {
            if (_isSetup)
                return;

            EventName = $"EWC{GUID}";
            NetworkAPI.RegisterEvent<T>(EventName, ReceiveClient_Callback);
            _isSetup = true;
        }

        public void Send(T packetData, SNet_Player? target = null, SNet_ChannelType priority = SNet_ChannelType.GameNonCritical)
        {
            if (target != null)
            {
                NetworkAPI.InvokeEvent(EventName, packetData, target, priority);
            }
            else
            {
                NetworkAPI.InvokeEvent(EventName, packetData, priority);
            }

            ReceiveLocal_Callback(packetData);
        }

        private void ReceiveLocal_Callback(T packet)
        {
            ReceiveLocal(packet);
            OnReceiveLocal?.Invoke(packet);
        }

        private void ReceiveClient_Callback(ulong sender, T packet)
        {
            Receive(packet);
            OnReceive?.Invoke(packet);
        }

        protected virtual void ReceiveLocal(T packet)
        {
        }

        protected virtual void Receive(T packet)
        {
        }

        public event ReceiveHandler? OnReceive;

        public event ReceiveHandler? OnReceiveLocal;
    }
}