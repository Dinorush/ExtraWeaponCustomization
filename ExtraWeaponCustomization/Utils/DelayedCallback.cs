using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections;
using UnityEngine;

namespace EWC.Utils
{
    public sealed class DelayedCallback
    {
        private readonly Func<float> _endTime;
        private readonly Action? _onStart;
        private readonly Action? _onRefresh;
        private readonly Action? _onEnd;
        private Coroutine? _routine;

        public DelayedCallback(Func<float> endTime, Action? onStart, Action? onRefresh, Action? onEnd)
        {
            _endTime = endTime;
            _onStart = onStart;
            _onRefresh = onRefresh;
            _onEnd = onEnd;
        }

        public void Start()
        {
            _onRefresh?.Invoke();
            _routine ??= CoroutineManager.StartCoroutine(Update().WrapToIl2Cpp());
        }

        public IEnumerator Update()
        {
            _onStart?.Invoke();
            float endTime;
            while (Clock.Time < (endTime = _endTime.Invoke()))
                yield return new WaitForSeconds(endTime - Clock.Time);
            _routine = null;
            _onEnd?.Invoke();
        }

        public void Stop()
        {
            if (_routine != null)
            {
                CoroutineManager.StopCoroutine(_routine);
                _routine = null;
                _onEnd?.Invoke();
            }
        }

        public void Cancel()
        {
            if (_routine != null)
            {
                CoroutineManager.StopCoroutine(_routine);
                _routine = null;
            }
        }
    }
}
