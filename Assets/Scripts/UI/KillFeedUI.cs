using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the last few "A eliminated B" lines and drops each one after a few seconds.
/// Fed by KillTracker's kill announcement RPC, which the master sends to every client.
/// </summary>
public class KillFeedUI : MonoBehaviour
{
    [SerializeField, Tooltip("Multi-line label the feed is written into.")]
    private Text feedText;

    [SerializeField, Tooltip("Seconds a line stays visible.")]
    private float lineLifetime = 5f;

    [SerializeField, Tooltip("Maximum lines shown at once.")]
    private int maxLines = 5;

    private readonly List<(string text, float expires)> lines = new List<(string, float)>();

    private void OnEnable()
    {
        KillTracker.KillAnnounced += HandleKill;
    }

    private void OnDisable()
    {
        KillTracker.KillAnnounced -= HandleKill;
    }

    private void HandleKill(string killer, string victim)
    {
        lines.Add((killer + " eliminated " + victim, Time.time + lineLifetime));
        while (lines.Count > maxLines)
        {
            lines.RemoveAt(0);
        }
        AudioManager.Play(SoundId.Death, 0.6f);
    }

    private void Update()
    {
        lines.RemoveAll(line => Time.time > line.expires);

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            builder.AppendLine(line.text);
        }
        feedText.text = builder.ToString();
    }
}
