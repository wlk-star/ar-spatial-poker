import assert from "node:assert/strict";
import WebSocket from "ws";
import { RoomRegistry } from "./roomRegistry.js";
import type { ServerMessage, PlayerActionType } from "./protocol.js";

function peer() {
  const messages: ServerMessage[] = [];
  const socket = { readyState: WebSocket.OPEN, send: (raw: string) => messages.push(JSON.parse(raw)) } as WebSocket;
  return { socket, messages };
}

const registry = new RoomRegistry();
const human = peer();
// The cast keeps this executable against the pre-feature API for the red check.
const create = registry.createRoom.bind(registry) as (...args: unknown[]) => ReturnType<RoomRegistry["createRoom"]>;
const room = create(human.socket, "human", "You", "LOCAL_BOT");
const created = human.messages.find(m => m.type === "ROOM_CREATED");
assert.ok(created && created.type === "ROOM_CREATED");
assert.equal(created.snapshot.players.length, 2, "local Bot rooms seat an opponent");
assert.equal(created.snapshot.players[1].connected, true);
assert.equal(created.privateState.playerId, "human");

room.startHand();
const latest = () => human.messages.filter(m => m.type === "GAME_SNAPSHOT").at(-1)!;
let snap = latest();
assert.ok(snap.type === "GAME_SNAPSHOT");
const humanTurnVersion = snap.snapshot.version;
await new Promise(resolve => setTimeout(resolve, 850));
snap = latest();
assert.ok(snap.type === "GAME_SNAPSHOT");
assert.equal(snap.snapshot.version, humanTurnVersion, "Bot must wait for the human");
assert.equal(snap.privateState.holeCards.length, 2);
assert.ok(!("holeCards" in snap.snapshot.players[1]), "opponent values stay private");

let actions = 0;
const streets = new Set<string>();
while (actions < 40) {
  snap = latest();
  assert.ok(snap.type === "GAME_SNAPSHOT");
  streets.add(snap.snapshot.street);
  if (snap.snapshot.street === "HAND_RESULT") break;
  const legal = snap.privateState.legalActions;
  if (legal.actions.length) {
    const action: PlayerActionType = legal.actions.includes("CHECK") ? "CHECK" : "CALL";
    const accepted = room.applyAction({
      type: "PLAYER_ACTION", roomCode: room.code, playerId: "human",
      handId: snap.snapshot.handId!, expectedVersion: snap.snapshot.version,
      clientActionId: `test-${actions}`, action
    });
    assert.equal(accepted.type, "ACTION_ACCEPTED");
    actions++;
  }
  await new Promise(resolve => setTimeout(resolve, 850));
}
snap = latest();
assert.ok(snap.type === "GAME_SNAPSHOT");
assert.equal(snap.snapshot.street, "HAND_RESULT", "complete hand settles");
assert.ok(streets.has("FLOP") && streets.has("TURN") && streets.has("RIVER"));
assert.equal(snap.snapshot.players.reduce((sum, p) => sum + p.stack, 0), 4000);
room.disconnect("human");

const multiplayer = registry.createRoom(peer().socket, "owner", "Owner");
assert.equal(multiplayer.engine.state.players.length, 1);
assert.throws(() => multiplayer.startHand(), /two connected/);
console.log("Bot smoke passed: privacy, human turn, full hand, chip conservation, default rooms.");
