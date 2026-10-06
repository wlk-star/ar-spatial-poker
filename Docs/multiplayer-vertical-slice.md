# Multiplayer vertical slice

## Server

The first multiplayer server is an in-memory Node/TypeScript WebSocket service.

Implemented boundaries:

- room owner
- 4-seat room
- reconnect token
- socket identity binding
- owner-only hand start
- authoritative poker engine
- client action idempotency
- expected game version
- public snapshots
- private per-player state
- legal actions calculated on the server
- connection presence separated from poker hand state

## Client

Unity now has transport-independent networking contracts:

- `IGameTransport`
- `GameStateStore`
- `GameStateSynchronizer`
- `PokerActionClient`
- wire DTOs for public/private snapshots

The Unity layer intentionally does not yet hard-code a WebSocket package.

A concrete transport can later use a supported Unity WebSocket implementation without changing the game-state layer.

## Version recovery

Normal sequence:

```text
snapshot v10
event v11
snapshot v11
event v12
snapshot v12
```

If the client sees:

```text
local v10
event v13
```

it sends:

```text
SYNC_REQUEST
```

and replaces local state with the latest server snapshot.

## Security

A client cannot:

- submit an action for another `playerId`
- request another player's private snapshot
- start a hand unless it owns the room
- change its stack directly
- choose dealt cards
- decide its legal actions
- see another player's hole cards in a normal snapshot

## Next executable integration

1. choose concrete Unity WebSocket transport package
2. bind transport to `PokerActionClient` and `GameStateSynchronizer`
3. connect two Unity instances to one Server process
4. render server snapshots into AR presentation
5. map server legal actions into button/gesture availability
