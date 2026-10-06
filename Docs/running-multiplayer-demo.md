# Running the multiplayer vertical slice

## 1. Start server

```bash
cd Server
npm install
npm run typecheck
npm run smoke
npm start
```

Server defaults to:

```text
ws://127.0.0.1:8080
```

Health check:

```text
http://127.0.0.1:8080/health
```

## 2. Unity scene objects

Add:

- `ClientWebSocketTransport`
- `GameStateSynchronizer`
- `PokerActionClient`
- `PokerSessionClient`

Wire the references in the Inspector.

For local Editor tests, keep:

```text
ws://127.0.0.1:8080
```

For physical phones, point the transport at the development machine's LAN/Tailscale address.

## 3. Two-client flow

Client A:

1. Connect
2. CreateRoom
3. read room code from the received snapshot/state

Client B:

1. Connect
2. JoinRoom(roomCode)

Client A:

1. StartHand

Both clients now receive:

- identical public board/table state
- separate private state
- only their own hole cards
- server-calculated legal actions

## 4. Action flow

AR interaction produces a `PokerIntent`.

`PokerActionClient` turns that into:

```text
PLAYER_ACTION
+ handId
+ clientActionId
+ expectedVersion
```

The server validates the action, increments the version and broadcasts fresh snapshots.

## 5. Recovery

If Unity observes a version gap it sends `SYNC_REQUEST` and replaces its local store with the authoritative snapshot.

## Current limitations

- in-memory rooms
- no TLS/authentication
- no Redis/PostgreSQL
- no action timer
- no automatic settlement in the TypeScript server yet
- no showdown reveal event yet
- no production reconnection backoff
