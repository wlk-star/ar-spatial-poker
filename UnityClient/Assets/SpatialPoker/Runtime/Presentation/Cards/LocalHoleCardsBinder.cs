using UnityEngine;
using SpatialPoker.Networking;

namespace SpatialPoker.Presentation.Cards
{
    /// <summary>
    /// Binds the local player's private hole cards to the two seat slots.
    /// Shows procedural card-face textures via <see cref="CardFaceLibrary"/>;
    /// values never leave this client except through the normal server sync.
    /// </summary>
    public sealed class LocalHoleCardsBinder : MonoBehaviour
    {
        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private TMPro.TMP_Text firstCardText;
        [SerializeField] private TMPro.TMP_Text secondCardText;
        [SerializeField] private GameObject[] cardSlots = new GameObject[2];
        [SerializeField] private Renderer[] cardRenderers = new Renderer[2];
        [SerializeField] private Material cardFaceTemplate;

        private void OnEnable()
        {
            if (synchronizer != null)
                synchronizer.Store.Changed += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (synchronizer != null)
                synchronizer.Store.Changed -= Refresh;
        }

        public void Refresh()
        {
            var cards = synchronizer?.Store.Private?.holeCards;

            BindSlot(0, cards != null && cards.Length > 0 ? cards[0] : null, firstCardText);
            BindSlot(1, cards != null && cards.Length > 1 ? cards[1] : null, secondCardText);
        }

        private void BindSlot(int index, string code, TMPro.TMP_Text label)
        {
            var slot = cardSlots != null && index < cardSlots.Length ? cardSlots[index] : null;
            var renderer = cardRenderers != null && index < cardRenderers.Length ? cardRenderers[index] : null;

            if (slot != null && renderer != null)
            {
                CardFaceLibrary.ApplyToSlot(slot, renderer, cardFaceTemplate, label, code);
            }
            else if (label != null)
            {
                label.text = code ?? string.Empty;
            }
        }
    }
}
