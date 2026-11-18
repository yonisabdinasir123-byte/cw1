using System.Linq;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Live scoreboard. It never counts anything itself: it reads kills and deaths from each
/// player's custom properties and redraws whenever Photon reports that those properties
/// changed or a player joined or left.
/// </summary>
public class ScoreboardUI : MonoBehaviourPunCallbacks
{
    [SerializeField, Tooltip("Multi-line label the scoreboard is written into.")]
    private Text scoreText;

    private void Start()
    {
        Refresh();
    }

    /// <summary>Photon callback: a player's custom properties changed on any client.</summary>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(NetworkKeys.Kills) || changedProps.ContainsKey(NetworkKeys.Deaths))
        {
            Refresh();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Refresh();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Refresh();
    }

    /// <summary>Rebuilds the text, most kills first.</summary>
    public void Refresh()
    {
        if (scoreText == null || !PhotonNetwork.InRoom)
        {
            return;
        }
        scoreText.text = BuildScoreboardText("SCOREBOARD");
    }

    /// <summary>Builds the scoreboard lines for every player in the room, most kills first. Shared with the end screen.</summary>
    public static string BuildScoreboardText(string title)
    {
        var ordered = PhotonNetwork.PlayerList
            .OrderByDescending(KillTracker.GetKills)
            .ThenBy(KillTracker.GetDeaths)
            .ThenBy(player => player.ActorNumber);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<b>" + title + "</b>");
        foreach (Player player in ordered)
        {
            string name = player.IsLocal ? player.NickName + " (you)" : player.NickName;
            builder.AppendLine(string.Format("{0}   {1} K   {2} D", name, KillTracker.GetKills(player), KillTracker.GetDeaths(player)));
        }
        return builder.ToString();
    }
}
