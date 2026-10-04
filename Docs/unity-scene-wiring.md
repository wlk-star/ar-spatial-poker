# Unity scene wiring

Use one scene-level composition object instead of allowing individual Cards, Chips or gesture scripts to discover networking objects globally.

## Recommended hierarchy

```text
PokerInteractionLab
├── App
│   ├── SpatialPokerRuntimeInstaller
│   ├── ClientWebSocketTransport
│   ├── PokerSessionClient
│   ├── PokerActionClient
│   ├── GameStateSynchronizer
│   ├── LegalActionGate
│   ├── PokerIntentNormalizer
│   ├── LegalActionIntentSink
│   └── PokerActionBridge
│
├── AR
│   ├── AR Session
│   ├── XR Origin
│   ├── ARTablePlacementController
│   └── TrackingStateGuard
│
├── TableRoot
│   ├── BoardPresentation
│   ├── Seat0
│   ├── Seat1
│   ├── Seat2
│   ├── Seat3
│   ├── LocalPrivateArea
│   └── BettingZone
│
└── HUD
    ├── LegalActionHud
    └── NetworkStatusPresentation
```

## Intent wiring

Recommended chain:

```text
BettingZone / PokerActionBridge / Card Fold
     -> PokerIntentNormalizer
     -> LegalActionIntentSink
     -> PokerActionClient
     -> WebSocket server
```

The normalizer converts physical chip pushing into BET or RAISE according to server legal actions.

## Presentation wiring

```text
GameStateSynchronizer.Store
     -> TablePresentationBinder
     -> BoardPresentation + SeatPresentation[]

GameStateSynchronizer.Store.Private
     -> LocalHoleCardsBinder

LegalActionGate
     -> LegalActionHud
     -> gesture availability
```

## Failure modes

- WebSocket disconnected: show fallback status; do not mutate local poker state.
- AR tracking lost: keep networking and legal-action HUD alive.
- Gesture unavailable: player can use normal HUD buttons.
- State version gap: synchronizer requests authoritative snapshot.
