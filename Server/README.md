# AR Spatial Poker Server

In-memory authoritative multiplayer vertical slice.

## Run

```bash
cd Server
npm install
npm run typecheck
npm start
```

Default port: `8080`.

Health endpoint:

```text
GET /health
```

## WebSocket flow

### Create room

```json
{
  "type": "CREATE_ROOM",
  "playerId": "p1",
  "displayName": "Alice"
}
```

### Create local Bot room (single-client practice)

```json
{
  "type": "CREATE_ROOM",
  "playerId": "p1",
  "displayName": "Alice",
  "opponentMode": "LOCAL_BOT"
}
```

The server seats `Dealer Bot` (`local-bot`) in the second seat, reports it
as connected, and drives its actions from the authoritative legal-action list
(never initiates aggression: CHECK, then CALL, then FOLD). The Bot only acts
when the current action seat belongs to it, and a settled hand auto-starts a
new one after a short pause while both players still have chips.

### Join room

```json
{
  "type": "JOIN_ROOM",
  "roomCode": "123456",
  "playerId": "p2",
  "displayName": "Bob"
}
```

### Start hand

```json
{
  "type": "START_HAND",
  "roomCode": "123456"
}
```

### Player action

```json
{
  "type": "PLAYER_ACTION",
  "roomCode": "123456",
  "handId": "<current hand id>",
  "playerId": "p1",
  "clientActionId": "client-0001",
  "expectedVersion": 4,
  "action": "CALL"
}
```

## Important behavior

- server owns poker state
- clients submit semantic actions only
- every state change increments `version`
- action messages include `expectedVersion`
- duplicate `clientActionId` values are rejected
- public snapshots never include opponent hole-card values
- each connection receives a separate private state containing only its own hole cards
- reconnect requires the server-issued reconnect token

## Current scope

This server is deliberately in-memory. Restarting the process clears rooms.

Next production layers:
- Redis room/presence state
- PostgreSQL hand history
- signed authentication
- action timeout / auto-check / auto-fold
- server-side showdown reveal events
- metrics and audit log
