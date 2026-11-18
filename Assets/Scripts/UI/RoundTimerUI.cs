using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the remaining round time as m:ss, turning red in the last ten seconds.
/// It only reads RoundManager; it never decides when the round ends.
/// </summary>
public class RoundTimerUI : MonoBehaviour
{
    [SerializeField, Tooltip("Label the time is written into.")]
    private Text timerText;

    [SerializeField, Tooltip("Seconds left at which the text turns red.")]
    private float warningSeconds = 10f;

    private void Update()
    {
        if (RoundManager.Instance == null || !RoundManager.Instance.IsRoundActive)
        {
            timerText.text = string.Empty;
            return;
        }

        float seconds = RoundManager.Instance.RemainingSeconds;
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainder = Mathf.CeilToInt(seconds - minutes * 60f);
        if (remainder == 60)
        {
            minutes++;
            remainder = 0;
        }
        timerText.text = minutes + ":" + remainder.ToString("00");
        timerText.color = seconds <= warningSeconds ? new Color(1f, 0.35f, 0.3f) : Color.white;
    }
}
