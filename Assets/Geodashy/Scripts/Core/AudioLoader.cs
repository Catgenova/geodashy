using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Geodashy.Core
{
    /// <summary>Loads imported songs (mp3 / ogg / wav) from disk asynchronously, with a cache.</summary>
    public static class AudioLoader
    {
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static AudioClip GetCached(string path)
        {
            return path != null && cache.TryGetValue(path, out var c) && c != null ? c : null;
        }

        public static AudioType TypeFor(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".mp3": return AudioType.MPEG;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".wav": return AudioType.WAV;
                default: return AudioType.UNKNOWN;
            }
        }

        /// <summary>Starts loading on the host and calls back with the clip (or null on failure).</summary>
        public static void Load(MonoBehaviour host, string path, Action<AudioClip> onDone)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                onDone?.Invoke(null);
                return;
            }
            var cached = GetCached(path);
            if (cached != null)
            {
                onDone?.Invoke(cached);
                return;
            }
            host.StartCoroutine(LoadRoutine(path, onDone));
        }

        static IEnumerator LoadRoutine(string path, Action<AudioClip> onDone)
        {
            var uri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
            using (var req = UnityWebRequestMultimedia.GetAudioClip(uri, TypeFor(path)))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Could not load song " + path + ": " + req.error);
                    onDone?.Invoke(null);
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip != null)
                {
                    clip.name = Path.GetFileName(path);
                    cache[path] = clip;
                }
                onDone?.Invoke(clip);
            }
        }
    }
}
