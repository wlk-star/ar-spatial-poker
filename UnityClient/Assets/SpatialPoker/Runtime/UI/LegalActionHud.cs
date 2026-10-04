using UnityEngine;
using UnityEngine.UI;
using SpatialPoker.Interaction;

namespace SpatialPoker.UI
{
    public sealed class LegalActionHud : MonoBehaviour
    {
        [SerializeField] private LegalActionGate gate;
        [SerializeField] private Button foldButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button callButton;
        [SerializeField] private Button betRaiseButton;
        [SerializeField] private Button allInButton;
        [SerializeField] private TMPro.TMP_Text callLabel;
        [SerializeField] private TMPro.TMP_Text betRaiseLabel;

        private void OnEnable()
        {
            if (gate != null)
                gate.Changed += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (gate != null)
                gate.Changed -= Refresh;
        }

        public void Refresh()
        {
            if (gate == null)
                return;

            if (foldButton != null)
                foldButton.interactable = gate.Allows("FOLD");

            if (checkButton != null)
                checkButton.interactable = gate.Allows("CHECK");

            if (callButton != null)
                callButton.interactable = gate.Allows("CALL");

            var canBet = gate.Allows("BET");
            var canRaise = gate.Allows("RAISE");

            if (betRaiseButton != null)
                betRaiseButton.interactable = canBet || canRaise;

            if (allInButton != null)
                allInButton.interactable = gate.Allows("ALL_IN");

            if (callLabel != null)
                callLabel.text = gate.Allows("CALL")
                    ? $"Call {gate.CallAmount}"
                    : "Call";

            if (betRaiseLabel != null)
            {
                if (canRaise)
                    betRaiseLabel.text = $"Raise ≥ {gate.MinRaiseTo}";
                else if (canBet)
                    betRaiseLabel.text = $"Bet ≥ {gate.MinRaiseTo}";
                else
                    betRaiseLabel.text = "Bet / Raise";
            }
        }
    }
}
