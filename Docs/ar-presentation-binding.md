# AR presentation binding

This milestone connects authoritative multiplayer snapshots to scene presentation.

## Added

- `TablePresentationBinder`
- `BoardPresentation`
- `SeatPresentation`
- `LocalHoleCardsBinder`
- `LegalActionGate`
- `LegalActionIntentSink`

## Snapshot -> scene

```text
GameStateSynchronizer
        |
        v
GameStateStore
        |
        +--> BoardPresentation
        |      - board
        |      - pot
        |      - dealer button
        |
        +--> SeatPresentation[]
        |      - name
        |      - stack
        |      - street bet
        |      - current turn highlight
        |      - connection indicator
        |
        +--> LocalHoleCardsBinder
               - local private cards only
```

## Legal actions

Gesture-generated intents should be routed through:

```text
Gesture / BettingZone / FoldZone
        |
        v
LegalActionIntentSink
        |
        v
LegalActionGate
        |
        v
PokerActionClient
```

This ensures the client uses server-authoritative legal actions before sending semantic intents.

Examples:

- table tap only becomes Check when `CHECK` is legal
- chip release only becomes Bet/ raise when amount is inside server min/max bounds
- fold gesture is ignored when Fold is not legal
- stale local UI still remains protected by server validation

## Scene recommendation

```text
TableRoot
├── BoardPresentation
├── DealerButton
├── Seat0
│   └── SeatPresentation
├── Seat1
│   └── SeatPresentation
├── Seat2
│   └── SeatPresentation
├── Seat3
│   └── SeatPresentation
└── LocalPrivateArea
    └── LocalHoleCardsBinder
```

For the first device-ready build, TMP text is acceptable as a placeholder. Replace it later with actual card/chip meshes and animations while keeping the same binder boundary.
