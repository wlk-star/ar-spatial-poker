using UnityEngine;
using SpatialPoker.Networking;

namespace SpatialPoker.Presentation.Cards
{
    public sealed class LocalHoleCardsBinder : MonoBehaviour
    {
        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private TMPro.TMP_Text firstCardText;
        [SerializeField] private TMPro.TMP_Text secondCardText;

        private void OnEnable()
        {
            if (synchronizer != null)
                synchronizer.Store.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (synchronizer != null)
                synchronizer.Store.Changed -= Refresh;
        }

        public void Refresh()
        {
            var cards = synchronizer?.Store.Private?.holeCards;

            if (firstCardText != null)
                firstCardText.text = cards != null && cards.Length > 0
                    ? cards[0]
                    : string.Empty;

            if (secondCardText != null)
                secondCardText.text = cards != null && cards.Length > 1
                    ? cards[1]
                    : string.Empty;
        }
    }
}
