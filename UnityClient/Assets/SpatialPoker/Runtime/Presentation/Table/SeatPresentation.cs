using UnityEngine;
using SpatialPoker.Networking.Protocol;

namespace SpatialPoker.Presentation.Table
{
    public sealed class SeatPresentation : MonoBehaviour
    {
        [SerializeField] private int seatIndex;
        [SerializeField] private GameObject activeIndicator;
        [SerializeField] private GameObject disconnectedIndicator;
        [SerializeField] private TMPro.TMP_Text nameText;
        [SerializeField] private TMPro.TMP_Text stackText;
        [SerializeField] private TMPro.TMP_Text betText;

        public int SeatIndex => seatIndex;

        public void Apply(PublicPlayerSnapshotDto player, int currentActionSeat)
        {
            var hasPlayer = player != null;
            gameObject.SetActive(hasPlayer);
            if (!hasPlayer) return;

            if (nameText != null) nameText.text = player.displayName;
            if (stackText != null) stackText.text = player.stack.ToString();
            if (betText != null) betText.text = player.streetContribution > 0
                ? player.streetContribution.ToString()
                : string.Empty;

            if (activeIndicator != null)
                activeIndicator.SetActive(player.seat == currentActionSeat);

            if (disconnectedIndicator != null)
                disconnectedIndicator.SetActive(!player.connected);
        }
    }
}
