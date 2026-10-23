using UnityEngine;
using UnityEngine.Audio;

namespace ZiddikJari
{
public class AudioSettingsController : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;

        public void SetMasterVolume(float value)
        {
            SetVolume("MasterVolume", value);
        }

        public void SetMusicVolume(float value)
        {
            SetVolume("MusicVolume", value);
        }

        public void SetSFXVolume(float value)
        {
            SetVolume("SFXVolume", value);
        }

        private void SetVolume(string parameter, float value)
        {
            float dB = value <= 0.0001f
                ? -80f
                : Mathf.Log10(value) * 20f;

            mixer.SetFloat(parameter, dB);
        }
    }
}
