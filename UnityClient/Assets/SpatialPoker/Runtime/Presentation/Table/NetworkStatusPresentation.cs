using UnityEngine;
using SpatialPoker.Networking;

namespace SpatialPoker.Presentation.Table
{
    public sealed class NetworkStatusPresentation : MonoBehaviour
    {
        [SerializeField] private ClientWebSocketTransport transport;
        [SerializeField] private TMPro.TMP_Text statusText;
        [SerializeField] private GameObject disconnectedPanel;

        private void OnEnable()
        {
            if (transport == null) return;

            transport.Connected += OnConnected;
            transport.Disconnected += OnDisconnected;

            Apply(transport.IsConnected);
        }

        private void OnDisable()
        {
            if (transport == null) return;

            transport.Connected -= OnConnected;
            transport.Disconnected -= OnDisconnected;
        }

        private void OnConnected() => Apply(true);
        private void OnDisconnected() => Apply(false);

        private void Apply(bool connected)
        {
            if (statusText != null)
                statusText.text = connected ? "Connected" : "Reconnecting…";

            if (disconnectedPanel != null)
                disconnectedPanel.SetActive(!connected);
        }
    }
}
