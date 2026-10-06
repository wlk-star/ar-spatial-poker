# Architecture

## Dependency direction

```text
Hand Tracking Provider
        |
        v
Gesture Recognition
        |
        v
Interaction Resolver
        |
        v
Poker Intent
        |
        v
Poker Domain / Game Server
```

AR presentation observes game state but does not own poker rules.

## Non-negotiable rules

1. Hand tracking providers are replaceable.
2. Hand coordinates are local-only and are never broadcast as gameplay truth.
3. AR objects produce semantic intents; they never directly mutate authoritative poker state.
4. Opponent hole-card views never contain the real card value.
5. Network sync uses semantic game events/snapshots, not Rigidbody/Transform streams.
6. Every important draggable object has a deterministic snap or return path.
7. AR failure must not corrupt poker state; 2D fallback remains possible.

## First lab

The first scene should contain:

- local player's two card backs / private card views
- opponent's two card-back proxy views
- local chip groups
- player betting zone
- pot zone
- table anchor
- hand cursor / pinch debug overlay

The first acceptance loop is:

```text
pinch own card -> drag -> release -> snap home
pinch chip group -> drag to betting zone -> emit BetIntent
touch/grab opponent card -> limited cosmetic pull -> locked feedback -> spring return
```
