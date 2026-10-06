using UnityEngine;
using UnityEngine.UI;
using SpatialPoker.Interaction;
using SpatialPoker.Networking;

namespace SpatialPoker.UI
{
    /// <summary>
    /// Lab HUD: status / street / current-actor / room-code labels, a
    /// bet-amount slider bounded by the server's legal raise range, and action
    /// buttons routed through <see cref="PokerActionBridge"/>. Button
    /// enablement stays with <see cref="LegalActionHud"/>; this controller
    /// owns click wiring and the bet control.
    /// </summary>
    public sealed class PokerLabHudController : MonoBehaviour
    {
        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private ClientWebSocketTransport transport;
        [SerializeField] private LegalActionGate gate;
        [SerializeField] private PokerActionBridge bridge;

        [SerializeField] private TMPro.TMP_Text statusText;
        [SerializeField] private TMPro.TMP_Text streetText;
        [SerializeField] private TMPro.TMP_Text actorText;
        [SerializeField] private TMPro.TMP_Text roomCodeText;

        [SerializeField] private Slider betSlider;
        [SerializeField] private TMPro.TMP_Text betAmountText;
        [SerializeField] private Button betSubmitButton;
        [SerializeField] private Button foldButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button callButton;
        [SerializeField] private Button allInButton;

        private void OnEnable()
        {
            if (synchronizer != null)
                synchronizer.Store.Changed += Refresh;

            if (gate != null)
                gate.Changed += Refresh;

            if (transport != null)
            {
                transport.Connected += Refresh;
                transport.Disconnected += Refresh;
            }

            if (betSubmitButton != null)
                betSubmitButton.onClick.AddListener(SubmitBet);
            if (foldButton != null)
                foldButton.onClick.AddListener(() => bridge?.TryFold());
            if (checkButton != null)
                checkButton.onClick.AddListener(() => bridge?.TryCheck());
            if (callButton != null)
                callButton.onClick.AddListener(() => bridge?.TryCall());
            if (allInButton != null)
                allInButton.onClick.AddListener(() => bridge?.TryAllIn());

            if (betSlider != null)
                betSlider.onValueChanged.AddListener(_ => RefreshBetLabel());

            Refresh();
        }

        private void OnDisable()
        {
            if (synchronizer != null)
                synchronizer.Store.Changed -= Refresh;

            if (gate != null)
                gate.Changed -= Refresh;

            if (transport != null)
            {
                transport.Connected -= Refresh;
                transport.Disconnected -= Refresh;
            }

            if (betSubmitButton != null)
                betSubmitButton.onClick.RemoveListener(SubmitBet);
            if (foldButton != null)
                foldButton.onClick.RemoveAllListeners();
            if (checkButton != null)
                checkButton.onClick.RemoveAllListeners();
            if (callButton != null)
                callButton.onClick.RemoveAllListeners();
            if (allInButton != null)
                allInButton.onClick.RemoveAllListeners();

            if (betSlider != null)
                betSlider.onValueChanged.RemoveAllListeners();
        }

        public void Refresh()
        {
            var snapshot = synchronizer?.Store.Public;
            var connected = transport != null && transport.IsConnected;

            if (statusText != null)
                statusText.text = connected ? "Connected" : "Disconnected";

            if (roomCodeText != null)
                roomCodeText.text = string.IsNullOrEmpty(snapshot?.roomCode)
                    ? "Room: —"
                    : $"Room: {snapshot.roomCode}";

            if (streetText != null)
                streetText.text = string.IsNullOrEmpty(snapshot?.street)
                    ? "Street: —"
                    : $"Street: {snapshot.street}";

            if (actorText != null)
            {
                var actorName = "—";
                if (snapshot?.players != null)
                {
                    foreach (var player in snapshot.players)
                    {
                        if (player != null && player.seat == snapshot.currentActionSeat)
                        {
                            actorName = player.displayName;
                            break;
                        }
                    }
                }

                actorText.text = snapshot?.handId == null
                    ? "Actor: —"
                    : $"Actor: {actorName}";
            }

            RefreshBetControl();
        }

        private void RefreshBetControl()
        {
            if (betSlider == null || gate == null)
                return;

            var min = gate.MinRaiseTo;
            var max = gate.MaxRaiseTo;
            var usable = max > min &&
                         (gate.Allows("BET") || gate.Allows("RAISE"));

            betSlider.interactable = usable;
            if (betSubmitButton != null)
                betSubmitButton.interactable = usable;

            if (!usable)
                return;

            betSlider.minValue = min;
            betSlider.maxValue = max;
            betSlider.value = Mathf.Clamp(betSlider.value, min, max);
            RefreshBetLabel();
        }

        private void RefreshBetLabel()
        {
            if (betAmountText != null && betSlider != null)
                betAmountText.text = $"Bet {(int)betSlider.value}";
        }

        private void SubmitBet()
        {
            if (betSlider == null)
                return;

            bridge?.TryBetOrRaise((int)betSlider.value);
        }
    }
}
