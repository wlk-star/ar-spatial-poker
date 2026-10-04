import assert from "node:assert/strict";
import { settleState } from "./settlement.js";
import type { PokerState } from "./pokerEngine.js";

const state: PokerState = {
  handId: "test",
  street: "SHOWDOWN",
  dealerSeat: 0,
  smallBlindSeat: 1,
  bigBlindSeat: 2,
  smallBlind: 10,
  bigBlind: 20,
  currentActionSeat: -1,
  currentBet: 0,
  minimumRaiseIncrement: 20,
  pot: 700,
  board: ["As","Ks","Qs","2d","3c"],
  players: [
    {
      playerId: "a",
      displayName: "A",
      seat: 0,
      stack: 0,
      streetContribution: 0,
      totalContribution: 100,
      hasActedThisRound: true,
      state: "ALL_IN",
      holeCards: ["Js","Ts"]
    },
    {
      playerId: "b",
      displayName: "B",
      seat: 1,
      stack: 0,
      streetContribution: 0,
      totalContribution: 300,
      hasActedThisRound: true,
      state: "ALL_IN",
      holeCards: ["Ah","Ad"]
    },
    {
      playerId: "c",
      displayName: "C",
      seat: 2,
      stack: 1700,
      streetContribution: 0,
      totalContribution: 300,
      hasActedThisRound: true,
      state: "ACTIVE",
      holeCards: ["Kh","Kd"]
    }
  ]
};

const payouts = settleState(state);
const a = payouts.find(p => p.playerId === "a");
const b = payouts.find(p => p.playerId === "b");

assert.equal(a?.amount, 300);
assert.equal(a?.category, "StraightFlush");
assert.equal(b?.amount, 400);

console.log("Settlement smoke test passed.");
