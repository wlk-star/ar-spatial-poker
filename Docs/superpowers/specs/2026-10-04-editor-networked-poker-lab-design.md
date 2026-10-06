# Editor Networked Poker Lab Design

## Goal

Deliver a playable Unity Editor vertical slice of heads-up Texas Hold'em. One
Unity client represents the human player. It connects to the local Node server,
which remains the authority for rooms, legal actions, state versions, cards, and
settlement. The server supplies one controlled Bot opponent so a single Editor
instance can play a complete hand.

## Scope

The deliverable includes:

- a saved `PokerInteractionLab` scene with a table, two seats, community cards,
  private local cards, opponent card backs, chip stacks, pot text, turn/status
  text, and legal-action controls;
- automatic local-server connection, room creation, Bot enrollment, and hand
  start;
- server-owned Bot actions after a brief visible delay;
- Unity presentation driven solely by public/private snapshots;
- HUD actions for fold, check, call, bet/raise, and all-in;
- mouse chip drag into the betting zone as a second source of the same semantic
  poker intent;
- a visible disconnected/error state that does not locally change poker state.

The deliverable excludes physical-device AR plane placement, native hand
tracking, matchmaking, persistent rooms, TLS/authentication, and a strategic
or learning-based Bot.

## Design

### Server Bot

`PokerRoom` owns a Bot player with an internal action scheduler. The Bot joins
the same room state as an ordinary player, receives no privileged game-state
mutation path, and takes actions through `PokerEngine.apply`. The scheduler
only runs after a valid hand starts and only when the Bot owns the current turn.

The first strategy is deterministic and conservative:

- check when legal;
- call a non-zero required amount when affordable;
- otherwise fold;
- use a minimum legal bet or raise only when no call/check action applies;
- use all-in only when it is the only legal non-fold action.

Every Bot action increments the same room version and sends the same snapshots
as a human action. The Bot is present only for the local single-player room;
normal multiplayer room flows remain unchanged.

### Unity Scene

`PokerInteractionLab.unity` composes existing transport, session, action,
synchronizer, legal-action, presentation, mouse-hand, chip, and betting-zone
components. A scene-level bootstrap coordinates the local session lifecycle:
connect, create room, wait for the Bot, and start the hand. It does not encode
poker rules or mutate snapshots.

Presentation has separate local/private and public paths. Local hole-card text
is rendered from the private snapshot. Opponent cards are rendered as backs
using only the public `holeCardCount`; they never receive the opponent's values.
Seat stacks, street contribution, pot, board cards, current actor, and legal
action labels refresh from server snapshots.

HUD controls call the existing semantic `PokerActionBridge`. Chip drag uses
`BettingZone` and reaches the same route:

```text
HUD or chip interaction
  -> PokerIntentNormalizer
  -> LegalActionIntentSink
  -> PokerActionClient
  -> WebSocket server
  -> PokerRoom / PokerEngine
  -> authoritative snapshots
  -> Unity presentation
```

### Error Handling

The Unity bootstrap exposes connection, room, and hand-start failures in the
HUD. When disconnected, controls remain visible but disabled and no local
action state is advanced. Invalid, stale, or out-of-turn actions are accepted
only as server messages and shown as status text; the next snapshot is the
source of truth.

### Verification

Automated server coverage verifies Bot enrollment, turn ownership, valid action
submission, version progression, and no Bot action when a human is active.
Unity Editor verification checks scene compilation, Play Mode entry, successful
connection/room/hand lifecycle, one player action, one Bot response, and no
Console errors. The existing server smoke and typecheck commands remain green.

## Success Criteria

With `Server` running on port 8080, entering Play Mode in
`PokerInteractionLab` produces a heads-up hand without a second Unity instance.
The player can take only server-legal actions, the Bot responds on its turn,
public and private presentation obey card-visibility rules, and the hand reaches
settlement without compiler errors or unhandled runtime exceptions.
