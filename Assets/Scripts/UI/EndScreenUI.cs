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

    [SerializeField, Tooltip("Votes for another round.")]
    private Button playAgainButton;

    [SerializeField, Tooltip("Explains the Play Again state: votes so far, or why it is disabled.")]
    private Text playAgainHintText;

    private void Start()
    {
        panel.SetActive(false);
        leaveButton.onClick.AddListener(() => GameManager.Instance.LeaveGame());
        playAgainButton.onClick.AddListener(() =>
        {
            PlayAgainController.Instance.VoteReady();
            RefreshPlayAgain();
        });
        RoundManager.Instance.RoundEnded += Show;
        RoundManager.Instance.RoundStarted += Hide;
        PlayAgainController.Instance.VotesChanged += RefreshPlayAgain;
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.RoundEnded -= Show;
            RoundManager.Instance.RoundStarted -= Hide;
        }
        if (PlayAgainController.Instance != null)
        {
            PlayAgainController.Instance.VotesChanged -= RefreshPlayAgain;
        }
    }

    /// <summary>Updates the Play Again button and its hint from the live player and vote counts.</summary>
    private void RefreshPlayAgain()
    {
        if (!panel.activeSelf)
        {
            return;
        }

        PlayAgainController votes = PlayAgainController.Instance;
        if (!votes.EnoughPlayers)
        {
            playAgainButton.interactable = false;
            playAgainHintText.text = "Not enough players to play again. Waiting for another player, or Leave to return to the lobby.";
            return;
        }

        string tally = " (" + votes.ReadyCount + " / " + votes.PlayerCount + " ready)";
        if (votes.LocalIsReady)
        {
            playAgainButton.interactable = false;
            playAgainHintText.text = "Waiting for the other players" + tally;
        }
        else
        {
            playAgainButton.interactable = true;
            playAgainHintText.text = "Vote to play another round" + tally;
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
        RefreshPlayAgain();
    }

    private void Hide()
    {
        panel.SetActive(false);
    }
}
