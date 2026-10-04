import { randomUUID } from "node:crypto";
import type { PlayerActionType } from "./protocol.js";

export type PlayerState =
  | "ACTIVE"
  | "FOLDED"
  | "ALL_IN"
  | "SITTING_OUT"
  | "DISCONNECTED";

export interface Player {
  playerId: string;
  displayName: string;
  seat: number;
  stack: number;
  streetContribution: number;
  totalContribution: number;
  hasActedThisRound: boolean;
  state: PlayerState;
  holeCards: string[];
}

export interface PokerState {
  handId: string | null;
  street: "WAITING" | "PREFLOP" | "FLOP" | "TURN" | "RIVER" | "SHOWDOWN" | "SETTLEMENT" | "HAND_RESULT";
  dealerSeat: number;
  smallBlindSeat: number;
  bigBlindSeat: number;
  smallBlind: number;
  bigBlind: number;
  currentActionSeat: number;
  currentBet: number;
  minimumRaiseIncrement: number;
  pot: number;
  board: string[];
  players: Player[];
}

const ranks = ["2","3","4","5","6","7","8","9","T","J","Q","K","A"];
const suits = ["c","d","h","s"];

export class PokerEngine {
  readonly state: PokerState;
  private deck: string[] = [];
  private deckIndex = 0;

  constructor(state?: Partial<PokerState>) {
    this.state = {
      handId: null,
      street: "WAITING",
      dealerSeat: -1,
      smallBlindSeat: -1,
      bigBlindSeat: -1,
      smallBlind: 10,
      bigBlind: 20,
      currentActionSeat: -1,
      currentBet: 0,
      minimumRaiseIncrement: 20,
      pot: 0,
      board: [],
      players: [],
      ...state
    };
  }

  startHand(): void {
    const seated = this.state.players
      .filter(p => p.stack > 0 && p.state !== "SITTING_OUT")
      .sort((a,b) => a.seat - b.seat);

    if (seated.length < 2) {
      throw new Error("At least two players are required");
    }

    this.state.handId = randomUUID();
    this.state.street = "PREFLOP";
    this.state.board = [];
    this.state.pot = 0;
    this.state.currentBet = 0;
    this.state.minimumRaiseIncrement = this.state.bigBlind;

    for (const p of this.state.players) {
      p.streetContribution = 0;
      p.totalContribution = 0;
      p.hasActedThisRound = false;
      p.holeCards = [];
      if (p.stack > 0 && p.state !== "SITTING_OUT") p.state = "ACTIVE";
    }

    this.shuffleDeck();
    this.state.dealerSeat = this.nextSeat(this.state.dealerSeat, seated);

    if (seated.length === 2) {
      this.state.smallBlindSeat = this.state.dealerSeat;
      this.state.bigBlindSeat = this.nextSeat(this.state.dealerSeat, seated);
    } else {
      this.state.smallBlindSeat = this.nextSeat(this.state.dealerSeat, seated);
      this.state.bigBlindSeat = this.nextSeat(this.state.smallBlindSeat, seated);
    }

    this.postBlind(this.state.smallBlindSeat, this.state.smallBlind);
    this.postBlind(this.state.bigBlindSeat, this.state.bigBlind);
    const bb = this.playerBySeat(this.state.bigBlindSeat);
    this.state.currentBet = bb?.streetContribution ?? 0;

    this.dealHoleCards(seated);

    this.state.currentActionSeat =
      seated.length === 2
        ? this.state.smallBlindSeat
        : this.nextSeat(this.state.bigBlindSeat, seated);
  }

  apply(playerId: string, action: PlayerActionType, amount?: number): string | null {
    const player = this.state.players.find(p => p.playerId === playerId);
    if (!player) return "ILLEGAL_ACTION";
    if (player.seat !== this.state.currentActionSeat) return "NOT_YOUR_TURN";
    if (player.state !== "ACTIVE") return "ILLEGAL_ACTION";

    switch (action) {
      case "FOLD":
        player.state = "FOLDED";
        player.hasActedThisRound = true;
        this.progressAfterAction();
        return null;

      case "CHECK":
        if (player.streetContribution !== this.state.currentBet) return "ILLEGAL_ACTION";
        player.hasActedThisRound = true;
        this.progressAfterAction();
        return null;

      case "CALL": {
        const needed = this.state.currentBet - player.streetContribution;
        if (needed <= 0) return "ILLEGAL_ACTION";
        this.commit(player, Math.min(needed, player.stack));
        player.hasActedThisRound = true;
        this.progressAfterAction();
        return null;
      }

      case "BET":
      case "RAISE": {
        if (amount == null || amount <= this.state.currentBet) return "INVALID_AMOUNT";
        const increment = amount - this.state.currentBet;
        const allInTarget = player.streetContribution + player.stack;
        const isAllInRaise = amount === allInTarget;

        if (increment < this.state.minimumRaiseIncrement && !isAllInRaise) {
          return "INVALID_AMOUNT";
        }

        const delta = amount - player.streetContribution;
        if (delta <= 0 || delta > player.stack) return "INVALID_AMOUNT";

        this.commit(player, delta);

        if (increment >= this.state.minimumRaiseIncrement) {
          this.state.minimumRaiseIncrement = increment;
          this.resetActedExcept(player.playerId);
        }

        this.state.currentBet = Math.max(this.state.currentBet, player.streetContribution);
        player.hasActedThisRound = true;
        this.progressAfterAction();
        return null;
      }

      case "ALL_IN": {
        if (player.stack <= 0) return "INVALID_AMOUNT";
        const oldBet = this.state.currentBet;
        const oldIncrement = this.state.minimumRaiseIncrement;
        this.commit(player, player.stack);
        player.hasActedThisRound = true;

        if (player.streetContribution > oldBet) {
          const increment = player.streetContribution - oldBet;
          this.state.currentBet = player.streetContribution;

          if (increment >= oldIncrement) {
            this.state.minimumRaiseIncrement = increment;
            this.resetActedExcept(player.playerId);
            player.hasActedThisRound = true;
          }
        }

        this.progressAfterAction();
        return null;
      }
    }
  }

  private progressAfterAction(): void {
    const contenders = this.state.players.filter(
      p => p.state !== "FOLDED" && p.state !== "SITTING_OUT"
    );

    if (contenders.length <= 1) {
      this.state.currentActionSeat = -1;
      this.state.street = "SETTLEMENT";
      return;
    }

    if (this.isBettingRoundComplete()) {
      this.advanceStreet();
      return;
    }

    const actionable = this.state.players
      .filter(p => p.state === "ACTIVE" && p.stack > 0)
      .sort((a,b) => a.seat - b.seat);

    if (actionable.length === 0) {
      this.runBoardToShowdown();
      return;
    }

    this.state.currentActionSeat = this.nextSeat(this.state.currentActionSeat, actionable);
  }

  private isBettingRoundComplete(): boolean {
    const active = this.state.players.filter(p => p.state === "ACTIVE" && p.stack > 0);
    if (active.length === 0) return true;

    return active.every(
      p => p.hasActedThisRound && p.streetContribution === this.state.currentBet
    );
  }

  private advanceStreet(): void {
    if (this.state.street === "PREFLOP") {
      this.burn();
      this.state.board.push(this.draw(), this.draw(), this.draw());
      this.state.street = "FLOP";
    } else if (this.state.street === "FLOP") {
      this.burn();
      this.state.board.push(this.draw());
      this.state.street = "TURN";
    } else if (this.state.street === "TURN") {
      this.burn();
      this.state.board.push(this.draw());
      this.state.street = "RIVER";
    } else if (this.state.street === "RIVER") {
      this.state.street = "SHOWDOWN";
      this.state.currentActionSeat = -1;
      return;
    }

    this.state.currentBet = 0;
    this.state.minimumRaiseIncrement = this.state.bigBlind;

    for (const p of this.state.players) {
      p.streetContribution = 0;
      p.hasActedThisRound = false;
    }

    const actionable = this.state.players
      .filter(p => p.state === "ACTIVE" && p.stack > 0)
      .sort((a,b) => a.seat - b.seat);

    if (actionable.length <= 1) {
      this.runBoardToShowdown();
      return;
    }

    this.state.currentActionSeat = this.nextSeat(this.state.dealerSeat, actionable);
  }

  private runBoardToShowdown(): void {
    while (this.state.street !== "SHOWDOWN") {
      this.advanceStreet();
      if (this.state.street === "SHOWDOWN") break;
    }
    this.state.currentActionSeat = -1;
  }

  private commit(player: Player, amount: number): void {
    const safe = Math.max(0, Math.min(amount, player.stack));
    player.stack -= safe;
    player.streetContribution += safe;
    player.totalContribution += safe;
    this.state.pot += safe;
    if (player.stack === 0) player.state = "ALL_IN";
  }

  private postBlind(seat: number, amount: number): void {
    const player = this.playerBySeat(seat);
    if (!player) return;
    this.commit(player, Math.min(amount, player.stack));
  }

  private dealHoleCards(players: Player[]): void {
    let seat = this.nextSeat(this.state.dealerSeat, players);

    for (let round = 0; round < 2; round++) {
      for (let i = 0; i < players.length; i++) {
        const player = this.playerBySeat(seat);
        if (player) player.holeCards.push(this.draw());
        seat = this.nextSeat(seat, players);
      }
    }
  }

  private shuffleDeck(): void {
    this.deck = [];
    for (const r of ranks) for (const s of suits) this.deck.push(r+s);

    for (let i = this.deck.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [this.deck[i], this.deck[j]] = [this.deck[j], this.deck[i]];
    }

    this.deckIndex = 0;
  }

  private draw(): string {
    const card = this.deck[this.deckIndex++];
    if (!card) throw new Error("Deck exhausted");
    return card;
  }

  private burn(): void {
    this.draw();
  }

  private playerBySeat(seat: number): Player | undefined {
    return this.state.players.find(p => p.seat === seat);
  }

  private resetActedExcept(playerId: string): void {
    for (const p of this.state.players) {
      if (p.state === "ACTIVE") p.hasActedThisRound = p.playerId === playerId;
    }
  }

  private nextSeat(fromSeat: number, players: Player[]): number {
    const ordered = [...players].sort((a,b) => a.seat - b.seat);
    const next = ordered.find(p => p.seat > fromSeat);
    return (next ?? ordered[0]).seat;
  }
}
