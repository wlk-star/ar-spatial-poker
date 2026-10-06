using UnityEngine;
using SpatialPoker.Networking;

namespace SpatialPoker.Presentation.Cards
{
    /// <summary>
    /// Renders the public board into five fixed slots. Unused slots are
    /// hidden. Board cards are public information; no privacy handling needed.
    /// Slots show procedural card-face textures via <see cref="CardFaceLibrary"/>,
    /// falling back to the text label when a texture is missing.
    /// </summary>
    public sealed class CommunityBoardPresentation : MonoBehaviour
    {
        private const int MaxBoardCards = 5;

        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private TMPro.TMP_Text[] cardLabels = new TMPro.TMP_Text[MaxBoardCards];
        [SerializeField] private GameObject[] cardSlots = new GameObject[MaxBoardCards];
        [SerializeField] private Renderer[] cardRenderers = new Renderer[MaxBoardCards];
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
            var board = synchronizer?.Store.Public?.board;

            for (var i = 0; i < MaxBoardCards; i++)
            {
                var code = board != null && i < board.Length ? board[i] : null;
                var slot = cardSlots != null && i < cardSlots.Length ? cardSlots[i] : null;
                var renderer = cardRenderers != null && i < cardRenderers.Length ? cardRenderers[i] : null;
                var label = cardLabels != null && i < cardLabels.Length ? cardLabels[i] : null;

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
}
