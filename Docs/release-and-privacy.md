# Release, privacy, and fold interactions

## Card privacy

`CardPrivacyController` decides whether a local renderer may show the face.

Supported policies:

- `Public`
- `OwnerOnly`
- `Never`
- `ServerControlled`

An opponent hole-card proxy should use `Never` and must not be given a hidden `CardValue` at all.

The privacy controller is a rendering concern only. It does not determine authoritative card ownership or showdown rules.

## Release rules

`ReleaseResolver` translates a physical release location into a semantic result:

- `Snap`
- `Return`
- `Bet`
- `Fold`

### Card flow

```text
grab own card
  -> release in FoldZone
  -> PokerIntent.Fold
  -> visual card returns / later muck animation

grab own card
  -> release near HoleCard SnapPoint
  -> snap

grab own card
  -> release elsewhere
  -> return home
```

Opponent cards bypass semantic release actions and always return after cosmetic movement.

### Chip flow

```text
grab ChipGroup
  -> release in BettingZone
  -> BettingZone emits PokerIntent.Bet(total preview)

release near PlayerChip SnapPoint
  -> snap

release elsewhere
  -> return home
```

## Current limitation

The first engine still uses a simplified heads-up postflop action order and does not yet implement dealer-relative seat ordering, minimum-raise reopening rules, side pots, or showdown evaluation.
