import { randomBytes } from "node:crypto";
import WebSocket from "ws";
import { PokerEngine } from "./pokerEngine.js";
import type {
  PlayerActionMessage,
  PlayerActionType,
  PrivateGameState,
  PublicGameSnapshot,
  ServerMessage
} from "./protocol.js";

interface ConnectionState {
  socket: WebSocket;
  playerId: string;
}

export const LOCAL_BOT_ID = "local-bot";
export const LOCAL_BOT_NAME = "Dealer Bot";
const BOT_ACTION_DELAY_MS = 600;
const RESULT_PAUSE_MS = 5000;

/** Conservative Bot policy: never initiates aggression. */
function pickConservativeAction(actions: PlayerActionType[]): PlayerActionType | null {
  if (actions.includes("CHECK")) return "CHECK";
  if (actions.includes("CALL")) return "CALL";
  if (actions.includes("FOLD")) return "FOLD";
  return null;
}

export class PokerRoom {
  readonly code: string;
  readonly ownerPlayerId: string;
  readonly opponentMode: "LOCAL_BOT" | undefined;
  readonly engine: PokerEngine;

  private version = 0;
  private connections = new Map<string, ConnectionState>();
  private reconnectTokens = new Map<string, string>();
  private processedActions = new Map<string, ServerMessage>();
  private botPlayerIds = new Set<string>();
  private botTimer: NodeJS.Timeout | null = null;
  private botTimerToken = 0;
  private resultTimer: NodeJS.Timeout | null = null;

  constructor(code: string, ownerPlayerId: string, opponentMode?: "LOCAL_BOT") {
    this.code = code;
    this.ownerPlayerId = ownerPlayerId;
    this.opponentMode = opponentMode;
    this.engine = new PokerEngine();
  }

  /** Seats the server-controlled Bot. Only valid for LOCAL_BOT rooms. */
  addBot(): void {
    if (this.opponentMode !== "LOCAL_BOT") {
      throw new Error("Bot players are only supported in LOCAL_BOT rooms.");
    }
    if (this.botPlayerIds.has(LOCAL_BOT_ID)) return;

    const seat = this.nextFreeSeat();
    this.engine.state.players.push({
      playerId: LOCAL_BOT_ID,
      displayName: LOCAL_BOT_NAME,
      seat,
      stack: 2000,
      streetContribution: 0,
      totalContribution: 0,
      hasActedThisRound: false,
      state: "ACTIVE",
      holeCards: []
    });
    this.botPlayerIds.add(LOCAL_BOT_ID);
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
    this.cancelBotTimer();
    this.cancelResultTimer();
    this.bumpVersion("PLAYER_DISCONNECTED", { playerId });
  }

  startHand(): void {
    for (const player of this.engine.state.players) {
      if (this.botPlayerIds.has(player.playerId)) {
        if (player.stack > 0 && player.state === "SITTING_OUT") {
          player.state = "ACTIVE";
        }
        continue;
      }
      if (!this.connections.has(player.playerId)) {
        player.state = "SITTING_OUT";
      } else if (player.stack > 0 && player.state === "SITTING_OUT") {
        player.state = "ACTIVE";
      }
    }

    const seatedCount = this.engine.state.players.filter(
      p => this.connections.has(p.playerId) || this.botPlayerIds.has(p.playerId)
    ).length;

    if (seatedCount < 2) {
      throw new Error("At least two connected players are required.");
    }

    this.engine.startHand();
    this.processedActions.clear();
    this.bumpVersion("HAND_STARTED", {
      handId: this.engine.state.handId
    });
    this.maybeScheduleBot();
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

    const payouts = this.engine.settleIfReady();
    if (payouts) {
      this.bumpVersion("HAND_SETTLED", {
        payouts
      });
    }

    this.maybeScheduleBot();

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

  private maybeScheduleBot(): void {
    this.cancelBotTimer();
    if (this.opponentMode !== "LOCAL_BOT") return;

    const street = this.engine.state.street;
    if (street !== "PREFLOP" && street !== "FLOP" && street !== "TURN" && street !== "RIVER") {
      return;
    }

    const bot = this.engine.state.players.find(p => this.botPlayerIds.has(p.playerId));
    if (!bot || bot.seat !== this.engine.state.currentActionSeat) return;

    const token = ++this.botTimerToken;
    const handId = this.engine.state.handId;
    this.botTimer = setTimeout(() => this.fireBotAction(token, handId), BOT_ACTION_DELAY_MS);
  }

  private fireBotAction(token: number, handId: string | null): void {
    if (token !== this.botTimerToken) return;
    this.botTimer = null;
    if (this.opponentMode !== "LOCAL_BOT") return;
    if (handId === null || this.engine.state.handId !== handId) return;

    const bot = this.engine.state.players.find(p => this.botPlayerIds.has(p.playerId));
    if (!bot || bot.seat !== this.engine.state.currentActionSeat) return;

    const legal = this.engine.legalActions(bot.playerId);
    const action = pickConservativeAction(legal.actions);
    if (!action) return;

    const error = this.engine.apply(bot.playerId, action);
    if (error) return;

    this.bumpVersion("BOT_ACTION", { playerId: bot.playerId, action });

    const payouts = this.engine.settleIfReady();
    if (payouts) {
      this.bumpVersion("HAND_SETTLED", { payouts });
      this.scheduleNextHand();
    } else {
      this.maybeScheduleBot();
    }
  }

  private scheduleNextHand(): void {
    this.cancelResultTimer();
    this.resultTimer = setTimeout(() => {
      this.resultTimer = null;
      try {
        const ready = this.engine.state.players.filter(
          p => p.stack > 0 &&
            (this.connections.has(p.playerId) || this.botPlayerIds.has(p.playerId))
        );
        if (ready.length >= 2 && this.engine.state.street === "HAND_RESULT") {
          this.startHand();
        }
      } catch {
        // Room emptied or hand already running; safe to skip.
      }
    }, RESULT_PAUSE_MS);
  }

  private cancelBotTimer(): void {
    this.botTimerToken++;
    if (this.botTimer) {
      clearTimeout(this.botTimer);
      this.botTimer = null;
    }
  }

  private cancelResultTimer(): void {
    if (this.resultTimer) {
      clearTimeout(this.resultTimer);
      this.resultTimer = null;
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
        connected: this.connections.has(p.playerId) || this.botPlayerIds.has(p.playerId),
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
