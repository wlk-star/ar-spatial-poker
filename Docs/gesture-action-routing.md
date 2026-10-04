# Gesture action routing

Physical interactions do not map one-to-one to poker protocol actions.

The important example is chip pushing:

```text
physical: push 400 chips
        |
        v
PokerIntent.Bet(400)
        |
        v
PokerIntentNormalizer
        |
        +-- server legal BET   -> Bet(400)
        |
        +-- server legal RAISE -> Raise(400)
        |
        +-- neither            -> reject locally
```

Final server validation still remains authoritative.

## Recommended pipeline

```text
BettingZone / Fold gesture / Check tap / fallback button
        |
        v
PokerActionBridge or raw PokerIntent
        |
        v
PokerIntentNormalizer
        |
        v
LegalActionIntentSink
        |
        v
PokerActionClient
        |
        v
Server
```

## Fallback UI

`LegalActionHud` exposes the same server legal-action state through normal buttons.

This is intentional: hand tracking can fail without making the player unable to act.

The HUD shows:

- Fold enabled only when legal
- Check enabled only when legal
- Call amount from server
- Bet/raise minimum from server
- All-in availability from server
