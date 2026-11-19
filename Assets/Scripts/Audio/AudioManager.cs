using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays sound effects. Creates itself on first use, survives scene loads, and caches one
/// generated clip per SoundId. Sounds are purely local: each client plays its own when it sees
/// the event (a shot, a hit, a pick-up), so nothing extra crosses the network.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    private AudioSource source;
    private readonly Dictionary<SoundId, AudioClip> clips = new Dictionary<SoundId, AudioClip>();

    /// <summary>Plays a sound once at the given volume.</summary>
    public static void Play(SoundId id, float volume = 1f)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioManager>();
            instance.source = go.AddComponent<AudioSource>();
            instance.source.playOnAwake = false;
        }

        if (!instance.clips.TryGetValue(id, out AudioClip clip))
        {
            clip = SoundBank.Create(id);
            instance.clips[id] = clip;
        }
        instance.source.PlayOneShot(clip, volume);
    }
}
