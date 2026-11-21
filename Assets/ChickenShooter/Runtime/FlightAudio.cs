using UnityEngine;

namespace OrbitFlock
{
    internal enum FlightSound { Laser, Missile, Egg, DefeatEnemy, Pickup, Hit }

    [RequireComponent(typeof(AudioListener))]
    internal sealed class FlightAudio : MonoBehaviour
    {
        private const string MusicKey = "OrbitFlock.MusicVolume";
        private const string EffectsKey = "OrbitFlock.EffectsVolume";
        private AudioSource music;
        private AudioSource effects;
        private AudioClip menuMusic;
        private AudioClip[] sounds;
        private bool settingsChanged;

        internal float MusicVolume => music.volume;
        internal float EffectsVolume => effects.volume;

        private void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            music.volume = PlayerPrefs.GetFloat(MusicKey, 0.55f);
            effects.volume = PlayerPrefs.GetFloat(EffectsKey, 0.70f);
            menuMusic = Resources.Load<AudioClip>("FlightAudio/Menu");
            sounds = new[]
            {
                Resources.Load<AudioClip>("FlightAudio/Laser"),
                Resources.Load<AudioClip>("FlightAudio/Missile"),
                Resources.Load<AudioClip>("FlightAudio/Egg"),
                Resources.Load<AudioClip>("FlightAudio/DefeatEnemy"),
                Resources.Load<AudioClip>("FlightAudio/Pickup"),
                Resources.Load<AudioClip>("FlightAudio/Hit")
            };
        }

        internal void PlayMenuMusic() => PlayMusic(menuMusic);

        internal void PlayMusic(AudioClip clip)
        {
            if (music.clip == clip)
                return;

            music.clip = clip;
            if (clip != null)
                music.Play();
        }

        internal void Play(FlightSound sound)
        {
            AudioClip clip = sounds[(int)sound];
            if (clip != null)
                effects.PlayOneShot(clip);
        }

        internal void SetMusicVolume(float volume)
        {
            music.volume = volume;
            PlayerPrefs.SetFloat(MusicKey, volume);
            settingsChanged = true;
        }

        internal void SetEffectsVolume(float volume)
        {
            effects.volume = volume;
            PlayerPrefs.SetFloat(EffectsKey, volume);
            settingsChanged = true;
        }

        internal void SaveSettings()
        {
            if (!settingsChanged)
                return;

            PlayerPrefs.Save();
            settingsChanged = false;
        }

        private void OnApplicationQuit() => SaveSettings();
    }
}
