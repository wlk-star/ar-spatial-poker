# PokerInteractionLab setup

The first Unity scene should be assembled manually from simple primitives before introducing final art.

## Scene hierarchy

```text
PokerInteractionLab
├── AR Session
├── XR Origin
├── TableRoot
│   ├── PublicZone
│   │   ├── BoardZone
│   │   └── PotZone
│   ├── LocalSeat
│   │   ├── HoleCardSlotA
│   │   ├── HoleCardSlotB
│   │   ├── ChipHome
│   │   └── BettingZone
│   └── OpponentSeat
│       ├── OpponentCardSlotA
│       └── OpponentCardSlotB
├── InteractionResolver
├── SnapManager
└── DebugHUD
```

## Suggested policies

### OwnHoleCardPolicy
- hover: true
- touch: true
- grab: Full
- drag: true
- flip: true
- reveal: OwnerOnly
- authority: ServerAuthoritative

### OpponentHoleCardPolicy
- hover: true
- touch: true
- grab: Cosmetic
- drag: true
- flip: false
- reveal: Never
- cosmetic distance: 0.025m

### OwnChipPolicy
- hover: true
- touch: true
- grab: Full
- drag: true
- reveal: Public
- authority: ServerAuthoritative

## Current implementation status

Implemented foundations:
- replaceable hand provider contract
- hand-frame model
- pinch hysteresis recognizer
- interactable contract
- interaction policies
- spatial resolver
- card entity
- chip group
- betting zone
- poker intents
- snap points / snap manager

Not implemented yet:
- AR Foundation scene bootstrap
- mobile hand provider implementation
- grab coordinator that connects pinch events to interactables
- animated return/spring feedback
- card privacy rendering
- actual poker engine/server
