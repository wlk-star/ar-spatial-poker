import WebSocket from "ws";
import { PokerRoom } from "./room.js";

export class RoomRegistry {
  private rooms = new Map<string, PokerRoom>();

  createRoom(
    socket: WebSocket,
    playerId: string,
    displayName: string
  ): PokerRoom {
    let code = this.generateCode();
    while (this.rooms.has(code)) code = this.generateCode();

    const room = new PokerRoom(code);
    this.rooms.set(code, room);

    const joined = room.addPlayer(socket, playerId, displayName);
    room.sendJoinEnvelope(
      socket,
      playerId,
      joined.seat,
      joined.reconnectToken,
      true
    );

    return room;
  }

  get(code: string): PokerRoom | undefined {
    return this.rooms.get(code);
  }

  private generateCode(): string {
    return Math.floor(100000 + Math.random() * 900000).toString();
  }
}
