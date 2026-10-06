using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpatialPoker.Networking;

namespace SpatialPoker.UI
{
    /// <summary>
    /// Lets the player point the client at a reachable server address.
    /// Phone builds resolve 127.0.0.1 to the phone itself, so testers type the
    /// PC's LAN address (e.g. ws://192.168.1.20:8080) here once; the choice
    /// persists via PlayerPrefs and the client reconnects immediately.
    /// </summary>
    public sealed class ServerAddressPanel : MonoBehaviour
    {
        [SerializeField] private ClientWebSocketTransport transport;
        [SerializeField] private PokerSessionClient session;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private Button applyButton;

        private void OnEnable()
        {
            if (applyButton != null)
                applyButton.onClick.AddListener(Apply);

            if (addressInput != null && transport != null &&
                string.IsNullOrEmpty(addressInput.text))
            {
                addressInput.text = transport.CurrentUrl;
            }
        }

        private void OnDisable()
        {
            if (applyButton != null)
                applyButton.onClick.RemoveListener(Apply);
        }

        private void Apply()
        {
            if (transport == null || addressInput == null)
                return;

            transport.SetUrl(addressInput.text);
            addressInput.text = transport.CurrentUrl;

            // Reconnect through the session so room state resets cleanly.
            transport.Disconnect();
            if (session != null)
                session.Connect();
            else
                transport.Connect();
        }
    }
}
