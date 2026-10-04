using UnityEngine;
using SpatialPoker.Networking.Protocol;

namespace SpatialPoker.Presentation.Table
{
    public sealed class BoardPresentation : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text boardText;
        [SerializeField] private TMPro.TMP_Text potText;
        [SerializeField] private Transform dealerButton;
        [SerializeField] private Transform[] seatAnchors;

        public void Apply(PublicGameSnapshotDto snapshot)
        {
            if (snapshot == null) return;

            if (boardText != null)
                boardText.text = snapshot.board == null
                    ? string.Empty
                    : string.Join("  ", snapshot.board);

            if (potText != null)
                potText.text = $"POT {snapshot.pot}";

            if (dealerButton != null &&
                seatAnchors != null &&
                snapshot.dealerSeat >= 0 &&
                snapshot.dealerSeat < seatAnchors.Length &&
                seatAnchors[snapshot.dealerSeat] != null)
            {
                dealerButton.position = seatAnchors[snapshot.dealerSeat].position;
                dealerButton.rotation = seatAnchors[snapshot.dealerSeat].rotation;
            }
        }
    }
}
