using UnityEngine;
using SpatialPoker.Networking;

namespace SpatialPoker.Presentation.Cards
{
    /// <summary>
    /// Shows opponent card backs from the public <c>holeCardCount</c> only.
    /// Card values never reach this component: privacy is structural, not
    /// conventional.
    /// </summary>
    public sealed class OpponentCardBackPresentation : MonoBehaviour
    {
        private const int MaxHoleCards = 2;

        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private GameObject[] cardBacks = new GameObject[MaxHoleCards];

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
            var snapshot = synchronizer?.Store.Public;
            var localPlayerId = synchronizer?.Store.Private?.playerId;

            var count = 0;
            if (snapshot?.players != null)
            {
                foreach (var player in snapshot.players)
                {
                    if (player == null || player.playerId == localPlayerId)
                        continue;

                    count = Mathf.Clamp(player.holeCardCount, 0, MaxHoleCards);
                    break;
                }
            }

            for (var i = 0; i < MaxHoleCards; i++)
            {
                var back = cardBacks != null && i < cardBacks.Length
                    ? cardBacks[i]
                    : null;

                if (back != null)
                    back.SetActive(i < count);
            }
        }
    }
}
