export type PlayerActionType =
  | "FOLD"
  | "CHECK"
  | "CALL"
  | "BET"
  | "RAISE"
  | "ALL_IN";

export interface CreateRoomMessage {
  type: "CREATE_ROOM";
  playerId: string;
  displayName: string;
}

export interface JoinRoomMessage {
  type: "JOIN_ROOM";
  roomCode: string;
  playerId: string;
  displayName: string;
  reconnectToken?: string;
}

export interface StartHandMessage {
  type: "START_HAND";
  roomCode: string;
}

export interface PlayerActionMessage {
  type: "PLAYER_ACTION";
  roomCode: string;
  handId: string;
  playerId: string;
  clientActionId: string;
  expectedVersion: number;
  action: PlayerActionType;
  amount?: number;
}

export interface SyncRequestMessage {
  type: "SYNC_REQUEST";
  roomCode: string;
  playerId: string;
}

export type ClientMessage =
  | CreateRoomMessage
  | JoinRoomMessage
  | StartHandMessage
  | PlayerActionMessage
  | SyncRequestMessage;

export interface PublicPlayerSnapshot {
  playerId: string;
  displayName: string;
  seat: number;
  stack: number;
  streetContribution: number;
  totalContribution: number;
  state: "ACTIVE" | "FOLDED" | "ALL_IN" | "SITTING_OUT" | "DISCONNECTED";
  holeCardCount: number;
}

export interface PublicGameSnapshot {
  roomCode: string;
  version: number;
  handId: string | null;
  street: string;
  dealerSeat: number;
  smallBlindSeat: number;
  bigBlindSeat: number;
  currentActionSeat: number;
  currentBet: number;
  minimumRaiseIncrement: number;
  pot: number;
  board: string[];
  players: PublicPlayerSnapshot[];
}

export interface PrivateGameState {
  playerId: string;
  holeCards: string[];
  reconnectToken: string;
}

export type ServerMessage =
  | {
      type: "ROOM_CREATED";
      roomCode: string;
      seat: number;
      reconnectToken: string;
      snapshot: PublicGameSnapshot;
      privateState: PrivateGameState;
    }
  | {
      type: "ROOM_JOINED";
      roomCode: string;
      seat: number;
      reconnectToken: string;
      snapshot: PublicGameSnapshot;
      privateState: PrivateGameState;
    }
  | {
      type: "GAME_SNAPSHOT";
      snapshot: PublicGameSnapshot;
      privateState: PrivateGameState;
    }
  | {
      type: "GAME_EVENT";
      version: number;
      event: string;
      payload: Record<string, unknown>;
    }
  | {
      type: "ACTION_ACCEPTED";
      clientActionId: string;
      version: number;
    }
  | {
      type: "ACTION_REJECTED";
      clientActionId: string;
      reason:
        | "ROOM_NOT_FOUND"
        | "HAND_MISMATCH"
        | "STALE_VERSION"
        | "NOT_YOUR_TURN"
        | "ILLEGAL_ACTION"
        | "INVALID_AMOUNT"
        | "DUPLICATE_ACTION";
      currentVersion: number;
    }
  | {
      type: "ERROR";
      code: string;
      message: string;
    };
