import assert from "node:assert/strict";
import { PokerEngine } from "./pokerEngine.js";

const engine = new PokerEngine();

engine.state.players.push(
  {
    playerId: "p1",
    displayName: "Alice",
    seat: 0,
    stack: 2000,
    streetContribution: 0,
    totalContribution: 0,
    hasActedThisRound: false,
    state: "ACTIVE",
    holeCards: []
  },
  {
    playerId: "p2",
    displayName: "Bob",
    seat: 1,
    stack: 2000,
    streetContribution: 0,
    totalContribution: 0,
    hasActedThisRound: false,
    state: "ACTIVE",
    holeCards: []
  }
);

engine.startHand();

assert.equal(engine.state.street, "PREFLOP");
assert.equal(engine.state.pot, 30);
assert.equal(engine.state.players[0].holeCards.length, 2);
assert.equal(engine.state.players[1].holeCards.length, 2);

const actor = engine.state.players.find(
  p => p.seat === engine.state.currentActionSeat
)!;

const legal = engine.legalActions(actor.playerId);
assert.ok(legal.actions.includes("FOLD"));
assert.ok(legal.actions.includes("CALL"));

const callError = engine.apply(actor.playerId, "CALL");
assert.equal(callError, null);

const next = engine.state.players.find(
  p => p.seat === engine.state.currentActionSeat
)!;

const nextLegal = engine.legalActions(next.playerId);
assert.ok(nextLegal.actions.includes("CHECK"));

const checkError = engine.apply(next.playerId, "CHECK");
assert.equal(checkError, null);
assert.equal(engine.state.street, "FLOP");
assert.equal(engine.state.board.length, 3);

console.log("Server smoke test passed.");
