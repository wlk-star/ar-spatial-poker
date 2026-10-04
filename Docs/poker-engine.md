# Poker engine milestone

This milestone introduces a small authoritative domain model that is intentionally independent of AR, Unity physics and networking.

## Current capabilities

- active/folded/all-in player states
- turn ownership
- fold
- check validation
- call
- bet / raise amount validation
- all-in
- stack deduction
- street contribution
- pot accumulation
- turn advancement

## Not yet implemented

- blind posting lifecycle
- betting-round completion detection
- street transitions
- deck/shuffle/deal
- community cards
- minimum raise sizing
- side pots
- showdown/hand evaluation
- split pots
- disconnected/sitting-out policy
- immutable snapshots and event versions

The current engine is deliberately small so AR interactions can already submit semantic intents into a testable rules layer.
