using UnityEngine;
using SpatialPoker.Networking;

namespace SpatialPoker.Presentation.Cards
{
    /// <summary>
    /// Renders the public board into five fixed slots. Unused slots are
    /// cleared. Board cards are public information; no privacy handling needed.
    /// </summary>
    public sealed class CommunityBoardPresentation : MonoBehaviour
    {
        private const int MaxBoardCards = 5;

        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private TMPro.TMP_Text[] cardLabels = new TMPro.TMP_Text[MaxBoardCards];

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
                var label = cardLabels != null && i < cardLabels.Length
                    ? cardLabels[i]
                    : null;

                if (label == null)
                    continue;

                label.text = board != null && i < board.Length
                    ? board[i]
                    : string.Empty;
            }
        }
    }
}
