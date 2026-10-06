# Garden campaign: beta levels 1–100

The ordered campaign is Assets/Levels/MainCatalog.asset. Level1 and Level2 keep their original GUIDs; their settings have been retuned for onboarding. Level3–Level100 are new assets. FreePlay is unchanged. Existing completed IDs remain completed; use a fresh test profile to experience onboarding.

## Progression

| Levels | Chapter | Main introduction |
|---|---|---|
| 1–10 | Garden Gate | Unlimited score and common-color goals |
| 11–20 | Petal Path | Forgiving move budgets |
| 21–30 | Pebble Walk | Small groups of one-hit rocks |
| 31–40 | Morning Frost | Sparse frost with open swap space |
| 41–50 | Robin Grove | Bird rescues; open vertical lanes |
| 51–60 | Gift Garden | Helpful packages containing only specials |
| 61–70 | Crystal Spring | Slow frost machines and occasional double frost |
| 71–80 | Winding Beds | Small corner cutouts; combined mechanics |
| 81–90 | Garden Festival | Two-bird and two-machine challenge variants |
| 91–100 | Grand Conservatory | Mixed objectives; unlimited celebration at 100 |

Challenges: 18, 28, 38, 48, 58, 68, 78, 88, 98. Each is followed by an unlimited, blocker-free recovery level. Chapter introductions are unlimited too. Other budgets begin generously and increase with complexity. No more than three simultaneous objectives. All targets use existing runtime objective systems.

Classic birds exit at the bottom of their current column. Normal/Chaos birds use short travel-distance targets. Birds start away from frost, with columns 2 and 5 free of rocks and machines. Starting frost/machine objectives are derived from actual placements. Packages cannot introduce new frost, machines, or birds in this campaign.

## Validation and beta tuning

Asset validation and board construction are checked in every mode. Automated model playthroughs choose random legal swaps and prefer direct special activation. They include opening cascades, package contents and machine spawning; they do not use bank inventory or Hot Streak bonuses. These are smoke tests, not a human completion guarantee or reliable win-rate estimate. Boards/refill randomness means reruns differ.

Before release, playtest the introductions and every challenge/recovery pair in all three modes. Record attempts, wins, moves remaining, time, reshuffles, and objective left on losses. Aim for high first-attempt completion on onboarding/recovery, a moderate dip on challenge levels, and recovery immediately afterward. Avoid increasing difficulty purely by reducing moves. Retune levels with repeated failures before adding more content.

## Growing to 1,000

Append batches of 100 to MainCatalog; preserve existing .meta files and IDs. Keep ten-level chapters and challenge/recovery cadence, but alternate layout motifs and objective combinations rather than multiplying target numbers indefinitely. Introduce new mechanics with unlimited tutorials. Add more authored layout families before generating the next 900, and tune each batch with beta results. Do not regenerate released levels wholesale.

The authoring recipe and simulator for this batch are saved in the Codex workspace under work/create-campaign-100.cs and work/audit-campaign.cs. The authoring recipe overwrites levels 1–100 when rerun: use it only intentionally with backups. The recipe is a starting point, not an unattended 1,000-level generator.

### Recorded beta smoke test

After giving seven flagged levels 12 extra moves, the expanded audit ran three random playthroughs per level per mode (900 total). Completed within budget: Classic 289/300, Normal 298/300, Chaos 300/300; total 887/900. Every level/mode combination had at least one successful run. Unlimited levels used a 160-action simulation cap. These small samples are not measured human win rates. Levels 38, 43, 47, 48, 55, 58, 68, 70, 75, 78, and 83 had at least one failure and should receive attention in human beta testing. Full results: CampaignSimulation.csv. Final authored settings: Campaign100.csv.
