import { createServer } from "node:http";
import { WebSocketServer, WebSocket } from "ws";
import { RoomRegistry } from "./roomRegistry.js";
import type { ClientMessage, ServerMessage } from "./protocol.js";

const port = Number(process.env.PORT ?? 8080);
const registry = new RoomRegistry();
const socketPlayer = new WeakMap<WebSocket, { roomCode: string; playerId: string }>();

const httpServer = createServer((req, res) => {
  if (req.url === "/health") {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({ ok: true }));
    return;
  }

  res.writeHead(404);
  res.end();
});

const wss = new WebSocketServer({ server: httpServer });

wss.on("connection", socket => {
  socket.on("message", raw => {
    let message: ClientMessage;

    try {
      message = JSON.parse(raw.toString()) as ClientMessage;
    } catch {
      send(socket, {
        type: "ERROR",
        code: "BAD_JSON",
        message: "Message must be valid JSON."
      });
      return;
    }

    try {
      if (message.type === "CREATE_ROOM") {
        const room = registry.createRoom(
          socket,
          message.playerId,
          message.displayName
        );

        socketPlayer.set(socket, {
          roomCode: room.code,
          playerId: message.playerId
        });
        return;
      }

      const room = registry.get(message.roomCode);
      if (!room) {
        send(socket, {
          type: "ERROR",
          code: "ROOM_NOT_FOUND",
          message: "Room not found."
        });
        return;
      }

      if (message.type === "JOIN_ROOM") {
        const joined = room.addPlayer(
          socket,
          message.playerId,
          message.displayName,
          message.reconnectToken
        );

        socketPlayer.set(socket, {
          roomCode: room.code,
          playerId: message.playerId
        });

        room.sendJoinEnvelope(
          socket,
          message.playerId,
          joined.seat,
          joined.reconnectToken,
          false
        );
        room.broadcastSnapshots();
        return;
      }

      if (message.type === "START_HAND") {
        room.startHand();
        return;
      }

      if (message.type === "SYNC_REQUEST") {
        room.sendSnapshot(message.playerId);
        return;
      }

      if (message.type === "PLAYER_ACTION") {
        const result = room.applyAction(message);
        send(socket, result);

        if (result.type === "ACTION_ACCEPTED") {
          room.broadcastSnapshots();
        }
      }
    } catch (error) {
      send(socket, {
        type: "ERROR",
        code: "SERVER_ERROR",
        message: error instanceof Error ? error.message : "Unknown server error."
      });
    }
  });

  socket.on("close", () => {
    const identity = socketPlayer.get(socket);
    if (!identity) return;

    registry.get(identity.roomCode)?.disconnect(identity.playerId);
  });
});

httpServer.listen(port, () => {
  console.log(`AR Spatial Poker server listening on :${port}`);
});

function send(socket: WebSocket, message: ServerMessage): void {
  if (socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify(message));
  }
}
