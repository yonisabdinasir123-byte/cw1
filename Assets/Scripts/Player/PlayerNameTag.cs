using Photon.Pun;
using UnityEngine;

/// <summary>
/// Shows the owner's nickname above the player and keeps it facing the camera.
/// The local player's tag is green and everyone else's is red, so you can tell them apart in tests.
/// </summary>
public class PlayerNameTag : MonoBehaviourPun
{
    [SerializeField, Tooltip("3D text that displays the nickname.")]
    private TextMesh label;

    [SerializeField, Tooltip("Colour for the local player's name.")]
    private Color localColor = new Color(0.3f, 1f, 0.3f);

    [SerializeField, Tooltip("Colour for other players' names.")]
    private Color remoteColor = new Color(1f, 0.4f, 0.4f);

    private void Start()
    {
        if (label == null)
        {
            return;
        }

        label.text = photonView.Owner != null ? photonView.Owner.NickName : "Player";
        label.color = photonView.IsMine ? localColor : remoteColor;
    }

    private void LateUpdate()
    {
        if (label != null && Camera.main != null)
        {
            // Billboard: copy the camera rotation so the text is always readable.
            label.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
