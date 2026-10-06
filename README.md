# Match Garden

*A little match. A little magic.*

We made a thing. Enjoy!

Match Garden is a garden-themed match-3 game built in Unity. This repository shares the source project so you can explore how it works, learn from it, and make your own changes.

## Play in the Editor

1. Clone this repository.
2. Install **Unity 6000.5.8f1** through Unity Hub (the exact version is recorded in `ProjectSettings/ProjectVersion.txt`).
3. In Unity Hub, choose **Add project from disk** and select this repository folder.
4. Let Unity restore packages and import assets. First import takes longer; `Library` is generated locally.
5. Open `Assets/Scenes/Main.unity` and press **Play**.

The repository contains a Unity source project, not a prebuilt executable. Package versions are pinned in `Packages`. An internet connection is needed for the first package restore. The Unity Pipeline package is development tooling; no Codex account is needed to play the game.

## What's inside

- 100 campaign levels, plus Free Play.
- Classic, Normal, and Chaos refill modes.
- Rockets, bombs, seekers, and vine color-clears.
- Birds, rocks, frost, frost machines, and mystery packages.
- Local campaign progress and a special-piece bank.
- Campaign boards start without matches; Free Play keeps opening cascades.

## Explore the code

| Location | What to learn |
| --- | --- |
| `Assets/Scripts/Board` | Board model, matches, swaps, refills, special effects and resolution records |
| `Assets/Scripts/Game` | Game flow, input, objectives and progression |
| `Assets/Scripts/View` | Animation, menus, HUD and presentation |
| `Assets/Scripts/Levels` | Level data, construction and campaign catalog |
| `Assets/Levels/MainCatalog.asset` | Ordered campaign list |
| `Assets/Levels/CampaignDesign.md` | Difficulty progression and beta test notes |
| `Assets/Scenes/Main.unity` | Main game scene |

The board simulation records what happened; the presentation layer animates those records. This separation is a useful starting point for studying the project.

## Report a bug

[Report a bug here](https://github.com/ohuweih/Match-Garden/issues/new?template=bug_report.yml).

Include the level, mode, what you expected, what happened, and steps to reproduce. Screenshots or a short recording help. Check existing issues first.

## Contributing

Small fixes, clear bug reports, and learning questions are welcome. For substantial changes, open an issue first. Keep Unity `.meta` files with their assets. Do not commit `Library`, `Temp`, logs, local saves, credentials, or build outputs. Test your changes in the main scene and describe what you checked in your pull request.

## License and credits

Original project code is available under the [MIT license](LICENSE). Third-party fonts, TextMesh Pro content, and Unity packages retain their own licenses; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Some garden artwork was created with generative image tools. This is a beta learning project, and difficulty is still being playtested.
