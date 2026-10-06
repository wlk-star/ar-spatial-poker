using UnityEngine;
using SpatialPoker.Interaction;
using SpatialPoker.Networking;
using SpatialPoker.Presentation.Table;
using SpatialPoker.UI;

namespace SpatialPoker.App
{
    /// <summary>
    /// Central runtime composition root for the prototype.
    /// Keeps cross-layer wiring out of Cards/Chips/AR domain components.
    /// </summary>
    public sealed class SpatialPokerRuntimeInstaller : MonoBehaviour
    {
        [Header("Networking")]
        [SerializeField] private ClientWebSocketTransport transport;
        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private PokerActionClient actionClient;
        [SerializeField] private PokerSessionClient sessionClient;

        [Header("Intent Routing")]
        [SerializeField] private LegalActionGate legalActionGate;
        [SerializeField] private LegalActionIntentSink legalActionSink;
        [SerializeField] private PokerIntentNormalizer intentNormalizer;
        [SerializeField] private PokerActionBridge actionBridge;

        [Header("Presentation")]
        [SerializeField] private TablePresentationBinder tableBinder;
        [SerializeField] private LegalActionHud legalActionHud;

        private void Awake()
        {
            if (transport == null)
            {
                Debug.LogError("[SpatialPoker] Missing WebSocket transport.", this);
                return;
            }

            synchronizer?.Bind(transport);
            actionClient?.Bind(transport);

            if (sessionClient == null)
                Debug.LogWarning("[SpatialPoker] Session client is not assigned.", this);

            if (legalActionGate == null ||
                legalActionSink == null ||
                intentNormalizer == null ||
                actionBridge == null)
            {
                Debug.LogWarning(
                    "[SpatialPoker] Intent routing is incomplete. AR gestures may not reach the server.",
                    this);
            }

            tableBinder?.Refresh();
            legalActionHud?.Refresh();
        }
    }
}
