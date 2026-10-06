# AR Spatial Poker

Spatial Texas Hold'em prototype: real-world tabletop + virtual cards/chips + hand interaction.

## Current milestone

Build a mobile AR interaction lab that validates:

- tabletop placement
- replaceable hand-tracking providers
- pinch / grab / drag / release
- private hole-card interactions
- cosmetic interaction with opponent cards
- chip groups and betting zones
- semantic poker intents instead of networking object transforms

## Unity baseline

- Unity: 6000.0.75f1
- AR Foundation: 6.4.1
- ARKit XR Plugin: 6.4.1
- ARCore XR Plugin: 6.4.1
- XR Plugin Management: 4.5.3

Open the `UnityClient` folder as the Unity project.

## Validate without a phone

The repository includes `MockMouseHandTrackingProvider`, so the interaction pipeline can be tested in the Editor before integrating a native hand-tracking SDK.

See:

- `Docs/editor-validation.md`
- `Docs/interaction-lab.md`
- `Docs/architecture.md`

## Editor Networked Poker Lab (Bot)

One-click playable heads-up table against the server Bot:

1. Start the server: `cd Server && npm install && npm start`
2. In Unity, run menu **SpatialPoker / Build PokerInteractionLab Scene**
3. Open `Assets/SpatialPoker/Scenes/PokerInteractionLab.unity` and press Play

The bootstrap connects, creates a `LOCAL_BOT` room, and starts the hand once
both seats are present. Mouse pinch drives the interaction pipeline; the HUD
buttons are the fallback action path. Chip stacks can be dragged into the
betting zone to bet.

Expected first loop:

```text
mouse pinch own card
-> drag
-> release
-> smooth return

mouse pinch chip group
-> drag into BettingZone
-> release
-> PokerIntent.Bet(amount)

mouse pinch opponent card
-> limited cosmetic pull
-> release
-> smooth return
```

## Architecture rule

```text
Hand Tracking
    -> Gesture Recognition
    -> Interaction Resolver
    -> Poker Intent
    -> Authoritative Poker State
```

AR objects never directly mutate authoritative poker state. Opponent card proxies never store hidden card values.

Development work is staged on feature branches and merged through pull requests.
