using UnityEngine;

/// <summary>
/// The sound effects the game uses. Every clip is generated in code at start-up (short tones
/// and noise bursts), so the project needs no audio files and stays small for submission.
/// </summary>
public enum SoundId
{
    Shoot,
    Hit,
    Death,
    PickUp,
    Dash,
    RoundEnd,
    Click
}

/// <summary>Builds the AudioClips for each SoundId from simple waveforms.</summary>
public static class SoundBank
{
    private const int SampleRate = 44100;

    public static AudioClip Create(SoundId id)
    {
        switch (id)
        {
            case SoundId.Shoot: return Sweep("Shoot", 0.09f, 700f, 180f, 0.5f, Waveform.Square);
            case SoundId.Hit: return Noise("Hit", 0.07f, 0.6f);
            case SoundId.Death: return Sweep("Death", 0.45f, 300f, 40f, 0.8f, Waveform.Saw);
            case SoundId.PickUp: return Arpeggio("PickUp", new[] { 440f, 660f, 880f }, 0.07f, 0.5f);
            case SoundId.Dash: return Noise("Dash", 0.2f, 0.35f);
            case SoundId.RoundEnd: return Arpeggio("RoundEnd", new[] { 523f, 659f, 784f, 1047f }, 0.16f, 0.6f);
            default: return Sweep("Click", 0.04f, 900f, 900f, 0.3f, Waveform.Sine);
        }
    }

    private enum Waveform { Sine, Square, Saw }

    /// <summary>A tone that slides from one frequency to another and fades out.</summary>
    private static AudioClip Sweep(string name, float seconds, float fromHz, float toHz, float volume, Waveform wave)
    {
        int count = Mathf.CeilToInt(seconds * SampleRate);
        float[] data = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float hz = Mathf.Lerp(fromHz, toHz, t);
            phase += hz / SampleRate;
            float sample = wave == Waveform.Sine ? Mathf.Sin(phase * Mathf.PI * 2f)
                : wave == Waveform.Square ? (Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f)
                : Mathf.Repeat(phase, 1f) * 2f - 1f;
            data[i] = sample * volume * (1f - t);
        }
        return ToClip(name, data);
    }

    /// <summary>White noise with a fast fade, for impacts and whooshes.</summary>
    private static AudioClip Noise(string name, float seconds, float volume)
    {
        int count = Mathf.CeilToInt(seconds * SampleRate);
        float[] data = new float[count];
        System.Random random = new System.Random(1234);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            data[i] = ((float)random.NextDouble() * 2f - 1f) * volume * (1f - t) * (1f - t);
        }
        return ToClip(name, data);
    }

    /// <summary>Several short notes in a row, for pick-ups and the round end.</summary>
    private static AudioClip Arpeggio(string name, float[] notes, float noteSeconds, float volume)
    {
        int perNote = Mathf.CeilToInt(noteSeconds * SampleRate);
        float[] data = new float[perNote * notes.Length];
        for (int n = 0; n < notes.Length; n++)
        {
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / perNote;
                float envelope = Mathf.Min(1f, t * 10f) * (1f - t);
                data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * i / SampleRate) * volume * envelope;
            }
        }
        return ToClip(name, data);
    }

    private static AudioClip ToClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
