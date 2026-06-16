using System;
using UnityEngine;

namespace SuzerainUnbound;

public class BackgroundMuteController : MonoBehaviour
{
    public BackgroundMuteController(IntPtr ptr) : base(ptr) { }

    // Store the volume so we can restore it safely
    private float _previousVolume = 1f;
    private bool _isMutedByUs = false;

    void OnApplicationFocus(bool hasFocus)
    {
        if (Plugin.EnableBackgroundRun.Value == BackgroundRunMode.muted)
        {
            //Plugin.Log.LogInfo($"[BackgroundMute] Application focus changed to {hasFocus}");
            if (!hasFocus)
            {
                // We just lost focus. Save the current volume and set it to 0.
                if (!_isMutedByUs)
                {
                    _previousVolume = AudioListener.volume;
                    AudioListener.volume = 0f;
                    _isMutedByUs = true;
                }
            }
            else
            {
                // We regained focus. Restore the original volume.
                if (_isMutedByUs)
                {
                    AudioListener.volume = _previousVolume;
                    _isMutedByUs = false;
                }
            }
        }
    }
}