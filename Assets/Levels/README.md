# Level definitions

Create a level from your current board:

1. Exit Play Mode and select the GameManager object.
2. Click **Create Level From Scene Settings** and save under `Assets/Levels`.
3. The new asset is assigned to GameManager automatically. Save the scene to keep that assignment.
4. Select the asset to edit its rules, then enter Play Mode.

To build a fresh level, right-click in the Project window and choose **Create > Match 3 > Level Definition**. You can also duplicate `StarterLevel.asset` or one of your own levels, then assign it to GameManager's **Level Definition** field.

The asset contains board dimensions/shape, blocked and locked cells, refill mode, color rarity, scoring, objectives, optional move budgets, bird placements/rescue rules, and Hot Streak settings. Coordinates begin at the bottom-left (0, 0).

**Validate Level** checks rule errors and reports placement warnings to the Console. Invalid bird or dead-cell coordinates are skipped with a warning, matching the previous scene behavior.

For a nest goal, add a bird under **Movable Objective Cells**, then use **Default Bird Rescue > Reach Destination**, or add a **Bird Rescue Overrides** entry using that bird's starting coordinate. Nests are highlighted at runtime. Add a **Rescue Birds** objective if rescuing birds should be tracked in the objective panel.

Assigned assets take precedence over scene gameplay settings. Clearing the assignment restores the saved scene settings. The export button always copies those scene settings. Camera fitting, animation timing, and logging stay on the scene components.

Enable **Use Move Limit** and set **Move Limit** under **Move budget** to create a challenge. Include at least one objective. A successful player swap, special combination, or direct special activation costs one move. Invalid swaps, hints, reshuffles, opening cascades, and Hot Streak moves are free.

Limited levels finish when all objectives are complete or the budget is exhausted. Results are checked after all cascades, rescues, and earned Hot Streak moves finish, so the last move can still win. The result panel's **Retry** button creates a new board and resets moves, score, and objectives. With **Use Move Limit** disabled, the board keeps the existing unlimited-play behavior.

Each game uses its own copy. Board progress never writes back to the asset, and Inspector edits to a level take effect on the next Play Mode run or retry. Campaign progress is saved as described below.


## Campaign and saved progress

1. Exit Play Mode and select GameManager.
2. Assign **Assets/Levels/MainCatalog.asset** to **Level Catalog**, then save the scene. This catalog contains Level1, Level2, and Level3 in that order.
3. Press Play. The first unfinished level loads automatically.
4. Win the level. Progress saves immediately, and **Next Level** loads the next unfinished entry.

Edit the catalog's ordered Levels array to add or reorder LevelDefinition assets. You can create another catalog with **Create > Match 3 > Level Catalog**. Every campaign level needs objectives. Unlimited-move campaign levels end when all objectives are complete; levels with a move limit can also end in defeat. Clear GameManager's Level Catalog to test a single level without changing campaign progress.

The local save is `progress.json` under Unity's `Application.persistentDataPath`. GameManager's Inspector shows the exact path and an **Open Save Folder** button. It contains a version number and completed level IDs. IDs come from Unity asset GUIDs, so renaming or moving an asset preserves completion; duplicated levels receive a new ID. Keep the `.meta` files with the assets.

The system writes a temporary file before replacing the primary save and keeps `progress.json.bak` as a backup. If the primary is missing or corrupt, it can recover from the backup. Unreadable save pairs or newer save formats are preserved and reported rather than overwritten. If saving fails after a win, use **Retry Save** once storage access is restored.

Closing during a level restarts that unfinished level on the next launch. Defeats do not advance progress. After all catalog entries are complete, the game shows **All levels complete** with an option to replay the last level. Replays do not remove completion records. To add a new campaign level, exit Play Mode, select MainCatalog, and append the new LevelDefinition asset to its Levels list in the Inspector. Creating a level asset in the folder alone does not add it to the campaign. The new catalog entry becomes available on the next launch; existing completed levels stay completed.

For testing, exit Play Mode and click **Reset Saved Progress** on GameManager. The confirmation deletes only this game's progress, backup, and temporary save files. It does not change level assets. Saves are local to the device; cloud sync and mid-level snapshots are not included.

## Special bank

In campaign play, single-click a special and press **Save piece**. Its special ability is stored immediately, leaving a normal piece of the same color on the board. Banking is free and does not activate the special or award points for removing it.

Open **Special bank** to inspect your stored pieces. Specials saved in the current level stay locked, including after a retry, restart, or replay of that level. After completing the source level, they are available in other levels. Each entry retains its color and special type.

In a later level, choose **Use this piece**, then click a normal colored tile. Placement replaces that tile with the stored special, consumes one bank entry, and costs no normal move. It cannot overwrite another special, a bird, dead space, or a locked tile. If the placed color creates a match, the normal resolution pipeline handles its clears, score, objectives, and cascades; these bank actions cannot trigger Hot Streak. **Cancel placement** keeps the entry in the bank. A used entry stays spent if you lose or restart the level.

The bank shares the local progress save and its backup. Existing version-1 saves migrate automatically, preserving completed levels. Failed bank writes leave the board and inventory unchanged. The bank is available when GameManager has a Level Catalog. **Reset Saved Progress** also clears the bank.

## Frost

In a LevelDefinition, expand **Settings**, find **Frost Cells** under the Frost heading, add an entry, and set its **Cell** coordinates and **Layers** (1 or 2). Coordinates start at the bottom-left. Frost needs a playable cell; duplicate, out-of-bounds, blocked, and locked placements are skipped with warnings. Invalid layer counts stop validation.

Frost stays on its board cell as pieces swap, fall, or reshuffle. A cyan outline and F1/F2 corner badge show its remaining layers; two-layer frost also has an inner white frame. The overlay has no collider and keeps piece colors and special markers visible.

Matching on a frost cell removes a layer, including when that cell creates a new special. Direct special effects also chip frost; area effects can chip frost beneath a surviving bird. A cell loses at most one layer per resolution round even when blasts overlap. Later cascades can remove the next layer. Adjacent matches do not chip frost. Moving/rescuing a bird, banking a special, and placing a special without creating a match do not chip it.

Add an objective with **Type = Clear Frost**. Its target automatically includes every valid placed layer, so a two-layer cell contributes two. Generated layers increase this target. When Frost Machines are present, Clear Frost also requires destroying all of them. Clear Frost counts actual removed layers from opening cascades, player moves, Hot Streak, and bank-generated matches. Opening frost removal counts even when optional opening rewards are disabled, keeping the finite-terrain objective achievable. Frost removal itself awards no extra color-clear points.

**FrostDemo.asset** demonstrates 12 frosted cells / 16 layers with 25 moves. Add it to MainCatalog to include it in your campaign, or assign it as the single Level Definition while Level Catalog is empty for standalone testing. Frost returns to its authored layers on retry. Runtime progress does not edit the level asset.


## Generators and Frost Machines

In a LevelDefinition, expand **Settings > Generators** and add an entry. Set **Type = Frost Machine**, **Cell**, **Hits To Destroy** (default 2), and **Player Moves Per Spawn** (default 1). Scene fallback settings and Create Level From Scene Settings support the same fields.

A generator is a stationary blocker on an otherwise playable cell. It cannot move, match, swap, or enter the special bank. It splits gravity/refill sections like a wall. Out-of-bounds, duplicate, dead-space and locked-cell placements are ignored with warnings. A valid generator takes precedence over frost or bird placements on its cell; those conflicting placements are skipped. A board must still have at least one playable cell. Invalid types, nonpositive health and nonpositive cadence fail level validation.

An orthogonally adjacent match or a special blast directly crossing a machine damages it. It takes at most one hit per resolution round, even when several matches/blasts overlap. Later cascades can damage it again. Color-targeted specials do not directly target colorless machines; any resulting line/bomb effects can hit them. Machine labels show remaining hits, and the core turns orange at one hit. Destruction removes the machine and fills its cell with a normal piece, allowing that cell into later cascades. It produces no frost burst and awards no extra colored-piece points.

Once a valid player action finishes—including its full cascades and all Hot Streak moves—each surviving generator advances its timer. A Frost Machine that is due pulses and applies **one layer** to **one randomly chosen eligible cell**. Candidates are all eight neighbors (including diagonals) of that machine plus all eight neighbors of every frosted cell on the board, including separate frost patches. Each candidate appears once, even when it touches several frost cells. Eligible cells contain a piece, special, or bird. Existing frost is never thickened; dead space, locked cells, and machines are excluded. If there is no eligible neighbor it does nothing this cycle. Existing frost extends where machines can spread. Each surviving machine still creates at most one new frosted cell when due; newly added frost does not cause an extra production tick. Destroying all machines stops spreading, even if frost remains. Default cadence is every player action; set 2 to produce every second action, and so on.

Opening cascades, hints, invalid swaps, banking/placing specials, individual bonus moves, and generator-generated resolutions do not advance timers. Destruction during any source of resolution counts and stops production immediately. The one generation phase runs before final win/loss evaluation, including on the last available move; it spends no additional move and cannot trigger another bonus. The cascade display retains the player's/bonus move's result during frost-only generation.

**Destroy Frost Machines** automatically targets all valid placed machines. It does not require clearing their leftover frost. **Clear Frost** requires both destroying every Frost Machine and removing every frost layer, including generated layers. Its HUD shows the growing layer target and the number of machines still active. Both objectives count opening destruction/removal even if optional opening rewards are disabled. Bird/score/color objectives can be used without either machine objective, so a level can finish while machines remain alive. Retry recreates original machine health, timers, and frost.

**FrostMachineDemo.asset** contains two machines, four starting frost layers, both objectives, and 30 moves. Add it to MainCatalog for campaign play, or clear GameManager's Level Catalog and assign it as Level Definition for standalone testing. Creating the asset alone does not add it to your catalog.

The shared generator system owns player-action timing, replay protection, health and location. `IGeneratorOutput` separates what is produced and where it can go. Frost is the first output; a future bird house or column/region injector can add another output and generator type while reusing the same timing and resolution pipeline. Bird generation is not implemented in this version.


## Mystery packages

In **LevelDefinition > Settings > Packages**, enable **Enabled**. Choose **Random Chance** for one chance roll per completed player action, or **Every Player Moves** for one package after every X player actions. **Chance Percent** applies only to Random Chance; **Player Moves Per Spawn** applies only to Every Player Moves. **Max Active Packages** caps unopened packages (default 3). If the board has no eligible normal piece or is at the cap, that spawn opportunity is skipped. There is no backlog.

A package replaces one randomly chosen ordinary colored piece without awarding score for replacement. It cannot overwrite specials, birds, other packages, machines, locks, or dead space. Existing frost stays on the cell. Packages are colorless, movable pieces: they fall in every refill mode and can be swapped when the swap produces a match or activates a neighboring special. They cannot form colored matches, activate directly, or be stored/overwritten through the special bank. Hints use the same valid-swap rules.

One orthogonally adjacent match or a direct line/bomb hit opens a package. Overlapping hits only open it once. Color-targeted specials do not directly target colorless packages; any resulting line/bomb effects can open them. The package opens on its current cell after that round's special blasts finish, before gravity. Its contents can then fall or participate in later cascades normally. Package opening itself awards no colored-piece points and consumes no extra move.

**Contents** is a weighted list: higher **Weight** means more likely, and equal weights are equally likely. Contents are rolled when a package opens. Supported contents are horizontal line, vertical line, bomb, target, color-clear, frost, Frost Machine, and bird. Remove any unwanted options or keep only one for a guaranteed result.

- Specials keep the color of the ordinary piece originally replaced by the package.
- Frost adds one layer on the package cell, up to two, with a normal colored piece beneath it.
- Frost Machines appear on the package cell as stationary blockers. Configure their health and cadence in the package settings. Existing frost remains underneath; it can be cleared by direct hits or after the machine is destroyed. A new machine can spread at the end of that player action if it survives.
- Birds use **Bird Travel Distance** (default 8 spaces) and receive unique bird numbers. They survive blasts and use the existing rescue rules and objectives.

Package-produced frost increases Clear Frost's layer target. Package-produced machines increase Destroy Frost Machines' target and Clear Frost's active-machine requirement. Those objectives continue tracking the new terrain even if their earlier targets had already been reached during the action. Clear Frost and Destroy Frost Machines still require starting frost/machines when authoring those objectives; future random contents alone are not a starting target. Score or bird objectives can be used for levels driven by package surprises. Unopened packages do not prevent a win when the actual level objectives are complete.

Spawn timing advances only once after a valid player action finishes, including all its cascades and Hot Streak moves. Opening cascades, hints, rejected swaps, bank actions, and bonus moves do not advance it independently. Generators act first, then the package spawn opportunity, then win/loss is checked. Retry resets the timer and board. Packages are disabled by default on existing levels, and scene export preserves the settings and content weights.

**PackageDemo.asset** uses one package every 2 player actions, at most 3 unopened packages, equal odds for all eight contents, a 5,000-point goal and 30 moves. For standalone testing, clear GameManager's Level Catalog and assign PackageDemo as Level Definition. Add it to MainCatalog only when you want it included in your campaign.
