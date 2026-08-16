# ALZ-112 — incompatible animals (including humans)

How exposure works **now** for any pawn that is not a compatible ordinary race
and is not already an `Uplifted_*` race.

A pawn is **incompatible** when there is no `Uplifted_<DefName>` ThingDef
for their current race. Humans are always incompatible. A cow, cat, or other
unmapped animal is incompatible the same way. Already-uplifted animals
(`Uplifted_Raccoon`, …) are **not** on this table.

```
                    ingest ALZ-112_Drug
                            |
                            v
                 +---------------------+
                 |  ALZ112Exposure     |
                 |  (Hediff_ALZ112)    |
                 +----------+----------+
                            |
              has Uplifted_<race>   OR   def already Uplifted_* ?
                     |                            |
                    yes                          no
                     |                            |
                     v                            v
              COMPATIBLE                   INCOMPATIBLE
              (not this doc)               humans + unmapped
                                           animals
```

## Dose

ALZ-112 is a **Medical** ingestible. Colonists can be given it from the
operations / administer bill (`Administer_ALZ-112_Drug`). Addictiveness is 0;
there is no chemical need and they do not take extra doses to feed an
addiction. A second dose while exposure is already present is absorbed and
does nothing (no stacked severity).

On first add:

- Hediff `ALZ112Exposure` is applied.
- Severity starts at **25%**.
- A private lethality roll picks `deathMultiple` in `{3, 4, 5}` and stays
  with that pawn for the rest of the exposure.

## While exposed

The bioactive stage is the same for every species:

| Effect | Value |
|---|---|
| Consciousness | −50% (awake; downed only below 30%) |
| Sight | −90% |
| Moving | `setMax` 35% so they can still walk to a bed |
| Hunger rate offset | +5 |
| Rest fall offset | +5 |
| Pain | +20% |
| Vomit | MTB 1.5 days |
| Sick thought | yes (humans feel/act sick) |
| Medical rest | they always seek a hospital / animal bed and stay there (administer bills need `InBed`) |
| Rage | Animals: Manhunter every 8–10 hours (hunt humanlikes); otherwise short Berserk (~every 2.5 hours, attack any pawn that moves). Humans: Berserk every ~9 hours. Downed / waiting for the administer bill cancels rage. After an episode they return to a hospital bed. |
| Life-threatening | yes (doctors treat it as an emergency) |

Vanilla `lethalSeverity` is off. The hediff never kills by crossing a vanilla
threshold. Death from the drug is only the dice + 100% bar below. Berserk
can still get them killed in a fight.

## The 90-minute clock

Default interval is **90 in-game minutes** (mod setting
`Minutes between Uplift attempts`). Each fire is one **Uplift Attempt**.
The health-tab counter (`Uplift Attempt #`) goes up on every attempt, whether
or not severity moves.

```
  t=0     90m      180m     270m     ...
   |-------|--------|--------|-------->
   dose   att #1   att #2   att #3
```

## One attempt, two rolls

```
                    Uplift Attempt #N
                          |
                          v
              +-----------------------+
              |  DEATH SAVE           |
              |  2 dice, N sides each |
              |  N = deathMultiple-1  |
              |  (N is 2, 3, or 4)    |
              +-----------+-----------+
                          |
              snake eyes (1,1)?
                 /                \
               yes                 no
                |                   |
                v                   v
     severity += increment     REROLL first two
     (never a full bar)        as 1d6 + 1d6
                |                   |
                v                   v
        severity >= 100%?    + third 1d6
           /         \              |
         yes          no            v
          |            |     raw = d1+d2+d3
          v            v     adj = raw + floor(N/10)
        KILL      wait 90m          |
        (done)    (no 3d6           v
                   this cycle)  adj >= 18 ?
                                   /    \
                                 yes     no
                                  |       |
                                  v       v
                              SUCCESS   wait 90m
```

Snake eyes **skips** the uplift 3d6 for that cycle. A death tick is not
also a rewrite tick.

Incompatible death-save fails are **1.5×** the raw snake-eyes rate: if
the two dice are not (1,1), a second roll can still count as a fail.

## Lethality (picked once at dose)

`deathMultiple` is rolled once: 3, 4, or 5. Sides = `deathMultiple − 1`,
minimum 2. Fail chance is 1.5× snake-eyes. Increment is still raw `p²` —
more frequent hits, not bigger steps. This is not shown on the health tab.

```
  deathMultiple   sides   P(1,1)   fail (×1.5)   increment (p²)
  -------------   -----   ------   -----------   --------------
        3           2     25%      37.5%         6.25%
        4           3     11.1%    16.7%         ~1.23%
        5           4     6.25%    9.4%          ~0.39%
```

The increment is how much the bar moves **on a death-save fail only**.
It is capped at 8% per hit so a single fail cannot jump the bar to lethal.

```
  Severity bar (incompatible starts here)

  0%         25%                         100%
  |-----------[===========...............]|
              ^ start                    ^ only then: Kill()
```

The number on the health tab **is** this bar. Drug death happens only
when it reaches 100%.

## Uplift roll (after a survived death save)

Incompatible pawns do **not** keep the small death dice for the rewrite.
They throw them away and roll a fair **3d6**, then add
`floor(Uplift Attempt # / 20)` with no cap. Compatible animals still use
`/ 10`; the slower bonus is what doubles incompatible time-to-uplift.

Need **18+** after the bonus.

```
  3d6 + bonus   (incompatible: +1 per 20 attempts)
  -----------
   attempts    bonus    need on the dice    ~P(success)
        0        +0     18                  1/216
       20        +1     17                  4/216
       40        +2     16                 10/216
       ...
      300       +15      3                  ~100%
```

The health tab **Uplift Chance** is that 3d6 probability, not the death
save.

## Success

Shared for every incompatible pawn:

1. Remove cataracts, dementia, Alzheimer*, and `ALZ112Exposure`.
2. Add `ALZ112Uplifted` (consciousness / sight / moving / hearing /
   metabolism bonuses; not life-threatening).
3. Anchor to the player faction.

Then it branches:

```
                    SUCCESS
                       |
          pawn.RaceProps.Humanlike ?
                 /              \
               yes               no
                |                 |
                v                 v
         stay Human            keep current race
         keep name             (no Uplifted_* swap)
         letter:               GiveUpliftName
         "Uplifted Human"      letter + save/reload
         no reload dialog      dialog
```

Humans stay the `Human` race and keep their `NameTriple`. There is no
`Uplifted_Human`. An incompatible cow that somehow succeeds also keeps
its ordinary race (there is nothing to swap to).

`ALZ112Uplifted` on a human is the cap-mod hediff only. The longer
README list (lifespan, luciferium, global work speed, …) is not applied
by this path.

## Health tab (what the numbers mean)

```
  ALZ-112 Exposure
    • Uplift Attempt #42     attempt count (every 90 min)
    • Uplift Bonus: +2       floor(42/20)
    • Uplift Chance: 4.63%   P(3d6 + 2 >= 18)
    • Severity: 37.50%       death bar; kill at 100%
```

**Severity** is accumulated death-save fail progress. **Uplift Chance**
ignores the death save (a fail never rolls 3d6).

## Humans vs other incompatible animals

Same clock, same dice, same 25% start, same kill-at-100% rule. The only
differences after a 18+ are naming, race (humans stay human), and the
letter / reload dialog.
```
