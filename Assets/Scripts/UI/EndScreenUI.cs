using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen overlay shown when the round ends: the winner's name in large text, a personal
/// "You Won!", "You Lost" or "Draw!" line, the final scoreboard and a Leave button.
/// It appears on every client because every client receives the same room property update.
/// </summary>
public class EndScreenUI : MonoBehaviour
{
    [SerializeField, Tooltip("Root object of the overlay, hidden during play.")]
    private GameObject panel;

    [SerializeField, Tooltip("Large text for the winner's name.")]
    private Text winnerText;

    [SerializeField, Tooltip("Personal result line under the winner's name.")]
    private Text resultText;

    [SerializeField, Tooltip("Final scoreboard text.")]
    private Text finalScoreText;

    [SerializeField, Tooltip("Returns to the lobby.")]
    private Button leaveButton;

    private void Start()
    {
        panel.SetActive(false);
        leaveButton.onClick.AddListener(() => GameManager.Instance.LeaveGame());
        RoundManager.Instance.RoundEnded += Show;
        RoundManager.Instance.RoundStarted += Hide;
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.RoundEnded -= Show;
            RoundManager.Instance.RoundStarted -= Hide;
        }
    }

    /// <summary>Fills in the texts from the round result and shows the overlay.</summary>
    public void Show(RoundResult result)
    {
        winnerText.text = result.IsDraw ? "Draw: " + result.WinnerNames : result.WinnerNames + " wins!";

        if (result.IsDraw)
        {
            resultText.text = result.LocalPlayerIsWinner ? "Draw!" : "You Lost";
        }
        else
        {
            resultText.text = result.LocalPlayerIsWinner ? "You Won!" : "You Lost";
        }
        resultText.color = result.LocalPlayerIsWinner ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.45f);

        finalScoreText.text = ScoreboardUI.BuildScoreboardText("FINAL SCORES");
        panel.SetActive(true);
    }

    private void Hide()
    {
        panel.SetActive(false);
    }
}
