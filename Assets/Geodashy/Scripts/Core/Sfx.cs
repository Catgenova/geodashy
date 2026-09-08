using System.Collections.Generic;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// Sound effects player. Clips are loaded by id from Resources/SFX (e.g. "jump_horse", falling back to "jump").
    /// Replace any file in that folder to change a sound; ids are the file names without extension.
    /// </summary>
    public static class Sfx
    {
        const string SfxVolumeKey = "geodashy.sfxVolume";
        const string MusicVolumeKey = "geodashy.musicVolume";
        const int Voices = 12;

        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static AudioSource[] voices;
        static int next;
        static GameObject host;

        public static float Volume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f));
            set
            {
                PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static float MusicVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f));
            set
            {
                PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                MusicVolumeChanged?.Invoke();
            }
        }

        public static event System.Action MusicVolumeChanged;

        static void EnsureHost()
        {
            if (host != null) return;
            host = new GameObject("SFX");
            Object.DontDestroyOnLoad(host);
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = host.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
        }

        public static AudioClip Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cache.TryGetValue(id, out var c)) return c;
            c = Resources.Load<AudioClip>("SFX/" + id);
            cache[id] = c;
            return c;
        }

        /// <summary>Plays the first id that exists (so "jump_horse" can fall back to "jump").</summary>
        public static void Play(string id, float volume = 1f, float pitch = 1f, float pitchJitter = 0.04f, string fallback = null)
        {
            var clip = Get(id) ?? (fallback != null ? Get(fallback) : null);
            if (clip == null) return;
            EnsureHost();
            var v = voices[next];
            next = (next + 1) % Voices;
            v.pitch = pitch + Random.Range(-pitchJitter, pitchJitter);
            v.volume = Mathf.Clamp01(volume * Volume);
            v.PlayOneShot(clip);
        }

        public static void StopAll()
        {
            if (voices == null) return;
            foreach (var v in voices) v.Stop();
        }
    }
}
