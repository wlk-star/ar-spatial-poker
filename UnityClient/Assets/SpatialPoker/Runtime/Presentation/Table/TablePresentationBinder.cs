using System.Linq;
using UnityEngine;
using SpatialPoker.Networking;
using SpatialPoker.Networking.Protocol;

namespace SpatialPoker.Presentation.Table
{
    public sealed class TablePresentationBinder : MonoBehaviour
    {
        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private BoardPresentation board;
        [SerializeField] private SeatPresentation[] seats;

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
            var snapshot = synchronizer?.Store.Public;
            if (snapshot == null) return;

            board?.Apply(snapshot);

            if (seats == null) return;

            foreach (var seat in seats)
            {
                if (seat == null) continue;

                var player = snapshot.players?
                    .FirstOrDefault(p => p != null && p.seat == seat.SeatIndex);

                seat.Apply(player, snapshot.currentActionSeat);
            }
        }
    }
}
