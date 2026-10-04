import { randomBytes } from "node:crypto";
import WebSocket from "ws";
import { PokerEngine } from "./pokerEngine.js";
import type {
  PlayerActionMessage,
  PrivateGameState,
  PublicGameSnapshot,
  ServerMessage
} from "./protocol.js";

interface ConnectionState {
  socket: WebSocket;
  playerId: string;
}

export class PokerRoom {
  readonly code: string;
  readonly ownerPlayerId: string;
  readonly engine: PokerEngine;

  private version = 0;
  private connections = new Map<string, ConnectionState>();
  private reconnectTokens = new Map<string, string>();
  private processedActions = new Map<string, ServerMessage>();

  constructor(code: string, ownerPlayerId: string) {
    this.code = code;
    this.ownerPlayerId = ownerPlayerId;
    this.engine = new PokerEngine();
  }

  isOwner(playerId: string): boolean {
    return playerId === this.ownerPlayerId;
  }

  addPlayer(
    socket: WebSocket,
    playerId: string,
    displayName: string,
    reconnectToken?: string
  ): { seat: number; reconnectToken: string } {
    let player = this.engine.state.players.find(p => p.playerId === playerId);

    if (player) {
      const expected = this.reconnectTokens.get(playerId);
      if (!expected || expected !== reconnectToken) {
        throw new Error("Invalid reconnect token");
      }
    } else {
      const seat = this.nextFreeSeat();
      const token = this.createReconnectToken();
      player = {
        playerId,
        displayName,
        seat,
        stack: 2000,
        streetContribution: 0,
        totalContribution: 0,
        hasActedThisRound: false,
        state: "ACTIVE",
        holeCards: []
      };

      this.engine.state.players.push(player);
      this.reconnectTokens.set(playerId, token);
      this.bumpVersion("PLAYER_JOINED", { playerId, seat });
    }

    this.connections.set(playerId, { socket, playerId });

    return {
      seat: player.seat,
      reconnectToken: this.reconnectTokens.get(playerId)!
    };
  }

  disconnect(playerId: string): void {
    if (!this.connections.delete(playerId)) return;
    this.bumpVersion("PLAYER_DISCONNECTED", { playerId });
  }

  startHand(): void {
    this.engine.startHand();
    this.processedActions.clear();
    this.bumpVersion("HAND_STARTED", {
      handId: this.engine.state.handId
    });
  }

  applyAction(message: PlayerActionMessage): ServerMessage {
    const previous = this.processedActions.get(message.clientActionId);
    if (previous) return previous;

    if (message.handId !== this.engine.state.handId) {
      return {
        type: "ACTION_REJECTED",
        clientActionId: message.clientActionId,
        reason: "HAND_MISMATCH",
        currentVersion: this.version
      };
    }

    if (message.expectedVersion !== this.version) {
      return {
        type: "ACTION_REJECTED",
        clientActionId: message.clientActionId,
        reason: "STALE_VERSION",
        currentVersion: this.version
      };
    }

    const error = this.engine.apply(
      message.playerId,
      message.action,
      message.amount
    );

    if (error) {
      return {
        type: "ACTION_REJECTED",
        clientActionId: message.clientActionId,
        reason:
          error === "NOT_YOUR_TURN"
            ? "NOT_YOUR_TURN"
            : error === "INVALID_AMOUNT"
              ? "INVALID_AMOUNT"
              : "ILLEGAL_ACTION",
        currentVersion: this.version
      };
    }

    this.bumpVersion("PLAYER_ACTION_ACCEPTED", {
      playerId: message.playerId,
      action: message.action,
      amount: message.amount ?? 0
    });

    const accepted: ServerMessage = {
      type: "ACTION_ACCEPTED",
      clientActionId: message.clientActionId,
      version: this.version
    };

    this.processedActions.set(message.clientActionId, accepted);
    return accepted;
  }

  sendSnapshot(playerId: string): void {
    const connection = this.connections.get(playerId);
    if (!connection) return;

    this.send(connection.socket, {
      type: "GAME_SNAPSHOT",
      snapshot: this.publicSnapshot(),
      privateState: this.privateState(playerId)
    });
  }

  sendJoinEnvelope(
    socket: WebSocket,
    playerId: string,
    seat: number,
    reconnectToken: string,
    created: boolean
  ): void {
    if (created) {
      this.send(socket, {
        type: "ROOM_CREATED",
        roomCode: this.code,
        seat,
        reconnectToken,
        snapshot: this.publicSnapshot(),
        privateState: this.privateState(playerId)
      });
      return;
    }

    this.send(socket, {
      type: "ROOM_JOINED",
      roomCode: this.code,
      seat,
      reconnectToken,
      snapshot: this.publicSnapshot(),
      privateState: this.privateState(playerId)
    });
  }

  broadcastSnapshots(): void {
    for (const playerId of this.connections.keys()) {
      this.sendSnapshot(playerId);
    }
  }

  private bumpVersion(event: string, payload: Record<string, unknown>): void {
    this.version += 1;

    for (const connection of this.connections.values()) {
      this.send(connection.socket, {
        type: "GAME_EVENT",
        version: this.version,
        event,
        payload
      });
    }

    this.broadcastSnapshots();
  }

  private publicSnapshot(): PublicGameSnapshot {
    const state = this.engine.state;

    return {
      roomCode: this.code,
      version: this.version,
      handId: state.handId,
      street: state.street,
      dealerSeat: state.dealerSeat,
      smallBlindSeat: state.smallBlindSeat,
      bigBlindSeat: state.bigBlindSeat,
      currentActionSeat: state.currentActionSeat,
      currentBet: state.currentBet,
      minimumRaiseIncrement: state.minimumRaiseIncrement,
      pot: state.pot,
      board: [...state.board],
      players: state.players.map(p => ({
        playerId: p.playerId,
        displayName: p.displayName,
        seat: p.seat,
        stack: p.stack,
        streetContribution: p.streetContribution,
        totalContribution: p.totalContribution,
        state: p.state,
        connected: this.connections.has(p.playerId),
        holeCardCount: p.holeCards.length
      }))
    };
  }

  private privateState(playerId: string): PrivateGameState {
    const player = this.engine.state.players.find(p => p.playerId === playerId);

    return {
      playerId,
      holeCards: player ? [...player.holeCards] : [],
      reconnectToken: this.reconnectTokens.get(playerId) ?? "",
      legalActions: this.engine.legalActions(playerId)
    };
  }

  private nextFreeSeat(): number {
    for (let seat = 0; seat < 4; seat++) {
      if (!this.engine.state.players.some(p => p.seat === seat)) return seat;
    }

    throw new Error("Room is full");
  }

  private createReconnectToken(): string {
    return randomBytes(24).toString("hex");
  }

  private send(socket: WebSocket, message: ServerMessage): void {
    if (socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify(message));
    }
  }
}
