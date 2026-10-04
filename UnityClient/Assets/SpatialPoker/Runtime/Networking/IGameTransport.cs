using System;

namespace SpatialPoker.Networking
{
    public interface IGameTransport
    {
        bool IsConnected { get; }

        event Action Connected;
        event Action Disconnected;
        event Action<string> MessageReceived;

        void Connect();
        void Disconnect();
        void Send(string json);
    }
}
