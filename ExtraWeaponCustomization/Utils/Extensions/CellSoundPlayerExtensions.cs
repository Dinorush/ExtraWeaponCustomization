using System;
using UnityEngine;
using AkEventCallback = AkCallbackManager.EventCallback;

namespace EWC.Utils.Extensions
{
    public static class CellSoundPlayerExtensions
    {
        public static uint PostWithDoneCallback(this CellSoundPlayer soundPlayer, uint eventID, Vector3 pos, Action onDone)
        {
            return soundPlayer.Post(eventID, pos, 1u, CreateOnDoneCallback(onDone), soundPlayer);
        }

        public static AkEventCallback CreateOnDoneCallback(Action onDone)
        {
            return (AkEventCallback) ((in_cookie, in_type, callbackInfo) => onDone());
        }
    }
}
