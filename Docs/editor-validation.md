# Editor validation

This milestone is intentionally testable before integrating a native mobile hand SDK.

## Prerequisites

- Unity 6000.0.75f1
- Open the `UnityClient` folder as the Unity project
- Allow Package Manager to restore AR Foundation, ARCore, ARKit and XR Plugin Management

## Build the temporary lab scene

Create a scene named `PokerInteractionLab`.

### 1. Interaction plane

Create an empty `TableRoot` at world origin.

Create a child plane/cube representing the tabletop. The exact art does not matter.

### 2. Local cards

Create:
- `LocalCardA`
- `LocalCardB`

Each needs:
- a collider
- `CardEntity`
- an `InteractionPolicy` asset configured with `GrabMode = Full`
- a home anchor

### 3. Opponent cards

Create:
- `OpponentCardA`
- `OpponentCardB`

Use a policy with:
- `GrabMode = Cosmetic`
- `RevealPolicy = Never`
- `MaxCosmeticGrabDistance = 0.025`

The card should move only a small amount and smoothly return after release.

### 4. Chip groups

Create one or more chip-stack primitives.

Each needs:
- collider
- `ChipGroup`
- value such as 100 or 200
- local-player ownership
- full-grab interaction policy

### 5. Betting zone

Create a trigger collider in front of the local player and attach `BettingZone`.

When a chip group enters:
- preview amount increases once, regardless of child collider count
- leaving removes it from the preview
- releasing while inside emits `PokerIntent.Bet(amount)`

Attach `PokerIntentDebugSink` to see the semantic intent in the Console.

### 6. Mouse hand provider

Create an object with:
- `MockMouseHandTrackingProvider`
- `GestureInteractionController`
- `InteractionResolver`

Assign:
- scene camera
- TableRoot as interaction plane
- provider and resolver references

Editor interaction:
- mouse position = projected fingertip position
- hold left mouse button = pinch
- drag while held = grab/drag
- release = release object

## Acceptance checks

### Own card
1. hover near own card
2. press mouse button
3. drag
4. release
5. card smoothly returns to home anchor

### Opponent card
1. grab opponent card
2. drag far away
3. card never moves farther than the configured cosmetic distance
4. release
5. card smoothly returns
6. no card value exists on the proxy entity

### Chips
1. grab a chip group
2. drag into BettingZone
3. release
4. Console receives a semantic Bet intent

## AR device setup

After Editor validation:

1. Create AR Session and XR Origin (Mobile AR).
2. Put `ARRaycastManager` and `ARPlaneManager` on the XR Origin.
3. Add `ARTablePlacementController`.
4. Assign the AR camera and a table-root prefab.
5. In XR Plug-in Management, enable ARKit for iOS and ARCore for Android as applicable.

Native hand tracking is deliberately the next provider implementation; it does not change the interaction layer.
