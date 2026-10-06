import type { Player, PokerState } from "./pokerEngine.js";

export interface Payout {
  playerId: string;
  amount: number;
  category: string;
}

interface RankedPlayer {
  player: Player;
  score: bigint;
  category: string;
}

const rankValue: Record<string, number> = {
  "2": 2, "3": 3, "4": 4, "5": 5, "6": 6, "7": 7,
  "8": 8, "9": 9, "T": 10, "J": 11, "Q": 12, "K": 13, "A": 14
};

export function settleState(state: PokerState): Payout[] {
  const contenders = state.players.filter(
    p => p.state !== "FOLDED" && p.state !== "SITTING_OUT"
  );

  if (contenders.length === 1) {
    return [{
      playerId: contenders[0].playerId,
      amount: state.pot,
      category: "Uncontested"
    }];
  }

  const pots = buildPots(state.players);
  const payouts = new Map<string, Payout>();

  for (const pot of pots) {
    const eligible = contenders
      .filter(p => pot.eligiblePlayerIds.includes(p.playerId))
      .map(player => rankPlayer(player, state.board))
      .sort((a,b) => a.score === b.score ? 0 : a.score > b.score ? -1 : 1);

    if (eligible.length === 0) continue;

    const best = eligible[0].score;
    const winners = eligible.filter(x => x.score === best)
      .sort((a,b) => a.player.seat - b.player.seat);

    const share = Math.floor(pot.amount / winners.length);
    let remainder = pot.amount % winners.length;

    for (const winner of winners) {
      const amount = share + (remainder-- > 0 ? 1 : 0);
      const existing = payouts.get(winner.player.playerId);

      if (existing) {
        existing.amount += amount;
      } else {
        payouts.set(winner.player.playerId, {
          playerId: winner.player.playerId,
          amount,
          category: winner.category
        });
      }
    }
  }

  return [...payouts.values()];
}

function buildPots(players: Player[]): Array<{
  amount: number;
  eligiblePlayerIds: string[];
}> {
  const levels = [...new Set(
    players
      .filter(p => p.totalContribution > 0)
      .map(p => p.totalContribution)
  )].sort((a,b) => a-b);

  const pots: Array<{amount:number; eligiblePlayerIds:string[]}> = [];
  let previous = 0;

  for (const level of levels) {
    const contributors = players.filter(p => p.totalContribution >= level);
    const amount = (level - previous) * contributors.length;

    if (amount > 0) {
      pots.push({
        amount,
        eligiblePlayerIds: contributors
          .filter(p => p.state !== "FOLDED" && p.state !== "SITTING_OUT")
          .map(p => p.playerId)
      });
    }

    previous = level;
  }

  return pots;
}

function rankPlayer(player: Player, board: string[]): RankedPlayer {
  const best = evaluateBest([...player.holeCards, ...board]);

  return {
    player,
    score: best.score,
    category: best.category
  };
}

function evaluateBest(cards: string[]): { score: bigint; category: string } {
  if (cards.length < 5 || cards.length > 7) {
    throw new Error("Expected 5 to 7 cards for evaluation.");
  }

  let best = { score: -1n, category: "HighCard" };

  for (let a=0; a<cards.length-4; a++)
  for (let b=a+1; b<cards.length-3; b++)
  for (let c=b+1; c<cards.length-2; c++)
  for (let d=c+1; d<cards.length-1; d++)
  for (let e=d+1; e<cards.length; e++) {
    const candidate = evaluateFive([
      cards[a], cards[b], cards[c], cards[d], cards[e]
    ]);

    if (candidate.score > best.score) best = candidate;
  }

  return best;
}

function evaluateFive(cards: string[]): { score: bigint; category: string } {
  const parsed = cards.map(card => ({
    rank: rankValue[card[0]],
    suit: card[1]
  }));

  const ranks = parsed.map(c => c.rank).sort((a,b) => b-a);
  const counts = new Map<number, number>();

  for (const rank of ranks) {
    counts.set(rank, (counts.get(rank) ?? 0) + 1);
  }

  const groups = [...counts.entries()]
    .map(([rank,count]) => ({rank,count}))
    .sort((a,b) => b.count - a.count || b.rank - a.rank);

  const flush = parsed.every(c => c.suit === parsed[0].suit);
  const straightHigh = getStraightHigh(ranks);

  if (flush && straightHigh > 0) {
    return buildScore(8, "StraightFlush", [straightHigh]);
  }

  if (groups[0].count === 4) {
    return buildScore(7, "FourOfAKind", [groups[0].rank, groups[1].rank]);
  }

  if (groups[0].count === 3 && groups[1].count === 2) {
    return buildScore(6, "FullHouse", [groups[0].rank, groups[1].rank]);
  }

  if (flush) {
    return buildScore(5, "Flush", ranks);
  }

  if (straightHigh > 0) {
    return buildScore(4, "Straight", [straightHigh]);
  }

  if (groups[0].count === 3) {
    const kickers = groups
      .filter(g => g.count === 1)
      .map(g => g.rank)
      .sort((a,b) => b-a);

    return buildScore(3, "ThreeOfAKind", [groups[0].rank, ...kickers]);
  }

  const pairs = groups
    .filter(g => g.count === 2)
    .sort((a,b) => b.rank-a.rank);

  if (pairs.length >= 2) {
    const kicker = groups
      .filter(g => g.count === 1)
      .map(g => g.rank)
      .sort((a,b) => b-a)[0];

    return buildScore(2, "TwoPair", [pairs[0].rank, pairs[1].rank, kicker]);
  }

  if (pairs.length === 1) {
    const kickers = groups
      .filter(g => g.count === 1)
      .map(g => g.rank)
      .sort((a,b) => b-a);

    return buildScore(1, "OnePair", [pairs[0].rank, ...kickers]);
  }

  return buildScore(0, "HighCard", ranks);
}

function getStraightHigh(ranks: number[]): number {
  const distinct = [...new Set(ranks)].sort((a,b) => b-a);
  if (distinct.includes(14)) distinct.push(1);

  let run = 1;

  for (let i=1; i<distinct.length; i++) {
    if (distinct[i-1] - 1 === distinct[i]) {
      run++;
      if (run >= 5) return distinct[i-4];
    } else {
      run = 1;
    }
  }

  return 0;
}

function buildScore(
  category: number,
  categoryName: string,
  tieBreakers: number[]
): { score: bigint; category: string } {
  let score = BigInt(category) << 24n;
  let shift = 20n;

  for (const value of tieBreakers.slice(0,5)) {
    score |= BigInt(value) << shift;
    shift -= 4n;
  }

  return { score, category: categoryName };
}
