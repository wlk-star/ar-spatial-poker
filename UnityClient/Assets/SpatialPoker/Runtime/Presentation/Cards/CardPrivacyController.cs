using UnityEngine;
using SpatialPoker.Interaction;

namespace SpatialPoker.Presentation.Cards
{
    public sealed class CardPrivacyController : MonoBehaviour
    {
        [SerializeField] private GameObject cardBackVisual;
        [SerializeField] private GameObject cardFaceVisual;
        [SerializeField] private RevealPolicy revealPolicy = RevealPolicy.OwnerOnly;
        [SerializeField] private string ownerPlayerId;
        [SerializeField] private string localPlayerId = "local-player";
        [SerializeField] private bool serverRevealOverride;

        public bool CanReveal =>
            revealPolicy == RevealPolicy.Public ||
            (revealPolicy == RevealPolicy.OwnerOnly && ownerPlayerId == localPlayerId) ||
            (revealPolicy == RevealPolicy.ServerControlled && serverRevealOverride);

        public void SetServerReveal(bool revealed)
        {
            serverRevealOverride = revealed;
            Apply(false);
        }

        public void SetPeekAmount(float amount)
        {
            var reveal = amount >= 0.25f;
            Apply(reveal);
        }

        public void Hide()
        {
            Apply(false);
        }

        private void Apply(bool requestedReveal)
        {
            var showFace = requestedReveal && CanReveal;

            if (cardFaceVisual != null)
                cardFaceVisual.SetActive(showFace);

            if (cardBackVisual != null)
                cardBackVisual.SetActive(!showFace);
        }
    }
}
