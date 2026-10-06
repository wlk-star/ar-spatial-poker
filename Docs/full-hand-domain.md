# Full-hand domain status

The poker domain now covers the main lifecycle required to complete a Texas Hold'em hand without AR.

## Implemented

- rotating dealer
- heads-up blind rule
- multi-player SB / BB positions
- forced blinds
- two-card hole dealing
- preflop action start
- burn + flop
- burn + turn
- burn + river
- automatic runout when no further betting is possible
- street contribution tracking
- total contribution tracking
- minimum raise increment
- short all-in handling without automatically reopening action
- fold / check / call / bet / raise / all-in
- betting-round completion
- side-pot construction
- best 5-card evaluation from 5–7 cards
- showdown ranking
- split-pot payout
- uncontested-pot payout
- payout application back to player stacks
- hand result state

## Current simplifications

- odd split-pot chips are assigned by current winner ordering rather than dealer-relative rules
- no rake
- no ante / straddle
- no tournament blind schedule
- no disconnected-player time bank
- no muck/show-card choice
- no persistent hand history
- no server event/version layer yet

## Security boundary

The domain engine may know all hole cards on the authoritative server.

Client snapshots must be filtered:

- local player's own hole cards may be included
- public board cards may be included
- opponent hole cards must be omitted until server-authorized showdown reveal
