# ZDPS - Damage Meter And Companion Tool
ZDPS is a Damage Meter and Companion Tool for Blue Protocol: Star Resonance. It's built on modern frameworks, making it fast and efficient at performing the role of a DPS Meter. It however also packs a lot of additional features in it such as complete [Encounter History](#Encounter-History-Browser), [Module Optimizer](#Module-Optimizer), [Log Reporting](#Integrations), [Field Boss/Magical Creature Spawn Tracking](#BPTimer-Integration), [Cooldown Tracking](#Cooldown-Priority-Tracker), [Raid Warnings](#Raid-Warnings), [Chat](#Chat), and more.

![Example of ZDPS in action on the DPS Meter tab](Screenshots/ZDPS_DPSMeter.png)

## Getting Started

> If you encounter issues, or have questions/feedback, the [BPSR Devs Discord](https://discord.gg/3UTC4pfCyC) is available to join.

### Prerequisites
Before ZDPS will run, you first must have Npcap installed. It can be found at [https://npcap.com](https://npcap.com/#download).
- Download and run the `Npcap 1.85 installer for Windows` file. If a newer version is available it will work as well.

If the game was already running when Npcap was installed, you will need to restart it.

ZDPS is built on `.NET 9` and requires it to be installed. If you do not have it installed already, head over to the Microsoft website [here](https://dotnet.microsoft.com/en-us/download/dotnet/9.0). Download and install the `.NET Desktop Runtime 9.X.X` for `x64`.
- Note: This not the SDK, or other "Runtime" versions on the site.
- If you fail to install this before running ZDPS you will receive a popup on launch prompting you to download it before it can be run.

### Installing ZDPS
1. Download the latest release of ZDPS by heading over to the [Releases](https://github.com/Blue-Protocol-Source/BPSR-Deeps/releases) section and downloading the latest `ZDPS - Damage Meter.zip`
   - You do not want to download the files named "Source Code" as those are just raw files for developers.
2. Once downloaded, extract the contents of the ZIP to a location of your choice.
   - Extracting into "Program Files" should be avoided as security permissions are far more strict there and can cause problems.
3. Once everything is extracted, run `BPSR-ZDPS.exe`
   - There is no actual installer for ZDPS, this is the entire application itself. It is entirely portable.

## Features
ZDPS has a very rich feature set which goes beyond what typical DPS Meters support but remains incredibly fast and memory efficient the entire time.
> [!NOTE]
> You can also run both Standalone and Steam game clients at the same time without causing data issues (you will need to specify which client to collect data from in the [Settings menu](#-General-Tab)).

When running ZDPS, Encounters are automatically split by "Phases." This means there's no need to remember to manually start new Encounters from a button, Keybind, or setup arbitrary "timeouts." As you clear dungeons, the mobbing/trash phase and the Boss phase will be automatically split into new Encounters to let you track each step without needing to remember to do anything. This applies to all content you do including Raids, Stimen Vault, Events, etc. If you happen to have a party wipe that will have the Encounter split automatically too!

Of course if you wish to do manual splitting a button exists at the top of the ZDPS window as well as a Keybind you can setup in the Settings menu.

If you want to see the metrics of a past Encounter, you can always open up the [Encounter History window](#Encounter-History-Browser) at any time, even if you've closed ZDPS before but wanted to check an Encounter from the prior day. All of the Encounters you take part in are automatically stored in a local database ready for your immediate viewing.

We also know sometimes you look away or go to another application while sitting in a queue. Or you're waiting on your party leader to enter a raid but it's taking a while and you go into another application. ZDPS supports playing [alert notification sounds](#Matchmaking-Alert-Notification-Sounds) when queues pop and are waiting for you to accept them. You'll never miss a queue again! You can also have a sound play when a Ready Check is performed. Both of these have a default sound effect but you can specify your own custom `MP3` or `WAV` file along with setting if it should loop or not.

Group content is a big part of Blue Protocol, as such, ZDPS supports sending automatic reports of your Encounters to a Discord channel of your choice. All you need to provide is the Webhook URL for the channel and you're good to go. If you're worried about multiple users sending the same report at once, ZDPS supports a Deduplication option to ensure only a single report is sent even when multiple players are running ZDPS in the same group.

If you are a Raider, be sure to also check out the [Raid Manager](#Raid-Manager) feature set, including [Raid Warnings](#Raid-Warnings) and [Countdowns](#Countdowns). Also [Chat](#Chat) can help you avoid missing important messages from your group when you're not looking at the game.

ZDPS also has it's own [Module Optimizer](#Module-Optimizer) that runs incredibly fast without any GPU Hardware Acceleration needed. You can now find the module combination that meets your stat priority preferences within seconds (or even less than a second it's so fast on modern CPUs).

There is also [integration support](#Integrations-Support) for third-party services to help provide a more complete experience. The current one supported at launch is [BPTimer](#BPTimer-Integration). You have the option to enable support in the Settings menu to allow viewing Field Boss and Magical Creatures spawns directly inside ZDPS. There is also the option to allow reporting data for those monsters back to [BPTimer.com](https://bptimer.com/) to help in the crowd sourced data efforts.

Many windows feature a number of buttons in the top right on their title bar. While some are unique to the window, the most common ones you will find are a `Thumbtack` for `Pin Window As Top Most` to make sure it remains above any other application/window, and `Two Arrows` for `Collapse To Content` which hides most of the "options" related elements of the window and leaves just the meaningful content visible. If you're ever unsure of what one of the buttons does, mouse over it and there is likely a tooltip telling you what it does.

> [!IMPORTANT]
> ZDPS does not send any web requests or data to any services automatically. Any web requests or data you want to send must first be enabled in the Settings menu. As such, it is strongly recommended to first go through the [Settings menu](#Settings-Menu) when first using ZDPS to ensure you have configured it to your liking.
>
> This includes [checking for updates](#ZDPS-Update-Checking).

For a more detailed breakdown of individual features in ZDPS, please see below:

> Most features including Settings are accessed from the "Cog Wheel" icon in the top right corner of ZDPS. This is called the "Features menu."

### Meters
There are 4 default meters: `DPS`, `Healing`, `Tanking`, and `NPC Taken`. Each of these meters displays realtime metrics about their relevant filters.
  - Entries are formatted with the Position, Class Icon (if enabled), Character Name, Profession/Spec Name, Ability Score, Active Per Second Value, and Total Value (Damage Done/Healing Done/Damage Taken).
  - Support for entirely custom meters is planned for the future.

### Encounter History Browser
> Accessed from `Encounter History` on the Features menu.

ZDPS stores the complete Encounter state history in a local database for any Encounter you take part in. Every single Encounter can be viewed on a detailed individual basis or Encounters can be grouped together into "Battles" which merge Wipes and Phases together for a single holistic view.

![Example of the Encounter History window showing a past fight against the Void Dragon raid boss](Screenshots/ZDPS_EncounterHistory.png)

Clicking on any Entity in the Historical Encounter allows you to open the [Entity Inspector](#Entity-Inspector) to that saved state. Additionally, you can filter what entities are displayed by right-clicking anywhere in the list and specifying the filter you want to use.

### Entity Inspector
> Accessed from clicking an `Entity` in any Meter or Historical report.

Any Entity (Players and NPCs) can be clicked on in the Meters or Encounter History to get a detailed breakdown of everything they did in the selected Encounter.

![Example of the Entity Inspector Damage Tab](Screenshots/ZDPS_EntityInspector_DamageTab.png)
> [!NOTE]
> By checking the `Persistent Tracking` box the window will continue to follow the selected Entity across multiple encounters so data is always the latest for them.

The details included in this window the following and more:
- Name / UID / Level / Ability Score
- Profession / Profession Specialization
- HP / Max HP
- Attack, Strength, Endurance, Armor, Crit, Haste, Luck, Mastery, Versatility, Block (These are primarily only for your own player but can also appear for others).
- Encounter specific metrics such as Total Damage, Total DPS, Total Shield Break, Total Hit Count, Total Crit Rate, Total Lucky Rate, Total Crit Count, and more.
- Damage, Healing, Taken skill tabs.
- A complete list of every skill cast with the Name, Total Damage (or Healing) dealt, DPS (or HPS) dealt, Number of Hits, Crit Rate, Average Damage/Healing Per Hit, and the Total % of Damage/Healing it did compared to the other skills the entity used.
- An indicator of what skill dealt the killing blow to the entity.
- All Attributes of the entity.
- Complete list of all Buffs that impacted the entity with indicators if they were Buffs, Debuffs, or Shields.
- Graphs for displaying Total Damage Over Time, Damage Per Second Over Time, and Hits By Source.

### Database Manager
> Accessed from `Database Manager` on then Features menu.

Since ZDPS features a local database, being able to manage how much content is stored in it is important. This allows direct realtime manual management of how much is actively in it. The Settings menu has options to allow automatic management of it though if you don't want to deal with manually clearing old data.
- Note that while deleting all Encounters are possible, it is often times not needed thanks to the database compression that allows thousands of Encounters to be stored with minimal impact on system storage.

### Raid Manager
To assist with Raid (Group) content, ZDPS has features tailored to those experiences.

#### Cooldown Priority Tracker
> Accessed from `Cooldown Priority Tracker` in the `Raid Manager` on the Features menu.

One of the group gameplay features is the Cooldown Priority Tracker. This allows you to specify a list of players (or NPCs) you wish to track and what skill casts you want to watch for. This can help organize what order players should use a utility skill like Airona or Tina - making it much easier to ensure the correct player uses their tools without the need for callouts.

![Example of the Raid Main Cooldown Priority Tracker for adding tracked entities and skills](Screenshots/ZDPS_CooldownPriorityTracker.png)
> This screenshot shows adding an entity by their name (in this case it's a character), and adding the Airona Battle Imagine (named "Arcane! Blessing of Life") to the tracker with a 150 second cooldown time.

When using the Cooldown Priority Tracker there are a few steps to follow first:
1. Open the `Add Tracked Entity` menu.
2. Type in the Name _or_ UID of the Entity you want to track casts for.
3. Click on the Green Plus to add the Entity, or the Red Minus to remove them.
4. Open the `Add Tracked Skill` menu.
5. Type in the Name _or_ Skill Id of the Skill you want to track being cast.
6. Verify the Cooldown Time displayed is not `0.0`.
   - If the Cooldown Time is `0.0` then you likely have selected the wrong skill. ZDPS will automatically calculate tier-based cooldown reductions so only the base value is needed (which is auto-filled for you when selecting a skill).
7. Click Add Skill To Tracker.

> [!NOTE]
> When adding Skills, the names used are the same as displayed in-game. If multiple results for the same name appear, you likely want to use the lower Skill Id version. In the screenshot above, Skill Id `3920` is the actual Skill Cast for the Airona Battle Imagine. For Tina the Skill Id would be `3921`.

> [!NOTE]
> Tracked skills go into a "pool" of skills. You do not need to add a unique instance of a skill per entity (and the UI will prevent you from trying to make that mistake).

In the actual Entity list at the top, you can freely change the order of Entities, giving them your priority cast order. Such as which player should cast Airona first in the event of a death.

![Example of the Cooldown Priority Tracker being Pinned over the Game, Collapsed To Content, and given Transparency](Screenshots/ZDPS_CooldownPriorityTracker_InGameExample.png)
> Above is a screenshot showing the window given some transparency via the Settings menu, Pinned on top of the Game, and making use of the Collapse To Content button on the top bar.

#### Raid Warnings
> Accessed from `Raid Warnings` in the `Raid Manager` on the Features menu.

Getting the attention of raid members can be difficult in the middle of a fight and your callouts may be easily missed, leading to unfortunate results. With ZDPS Raid Warnings, you can now elevate your messages to appearing on the screen in bold orange text. These function very similar to Raid Warnings in other games like World of Warcraft and are customizable by each user.

![Screenshot example of a Raid Warning message in-game](Screenshots/ZDPS_RaidWarning_Example.png)

- When you type a message starting with `/rw` it will automatically be sent at a Raid Warning to ZDPS users.
   - By default this only works when sent in Group chat. Other chat channels are disabled and must be manually enabled by each user.
- By default, a sound alert specifc to Raid Warnings will play alongside each Raid Warning message.
   - This sound alert can be changed in Raid Warnings settings menu, or completely disabled.
- The size and location of the Raid Warnings, along with the background Opacity can be changed.
- In order to help prevent abuse, a Raid Warning specific player Blacklist is also supported. This allows a user to completely block receiving any Raid Warnings from other specific users.

![Screenshot of the Raid Warnings Settings menu](Screenshots/ZDPS_RaidWarning_Settings.png)

#### Countdowns
> Accessed from `Countdowns` in the `Raid Manager` on the Features menu.

As a complement to Raid Warnings, there is also now the ability to display a sync'd Countdown on the screen for ZDPS users. This makes it easier than ever before to coordinate when to start a fight. While this can also be used to help indicate when mechanics needs to be done, it is strongly recommended to avoid using it for that purpose as it may distract users more than help them in the middle of a fight.

![Screenshot example of Countdowns in-game](Screenshots/ZDPS_Countdowns_Example.png)

- This can be started by entering a message starting with `/countdown` and followed by how long it should be (between 3 and 30 seconds).
   - For example `/countdown 30` will start a 30 second countdown for all ZDPS users.
   - You can also use `/ct` instead of spelling out `/countdown` as a shortcut. For example `/ct 10` to begin a 10 second countdown.
   - A Countdown in-progress can also be stopped early with `/countdown cancel`, `/countdown abort`, or `/countdown stop`.
- Users can pick to use either generic plain text or stylized text for the Countdown time.
- Just like with Raid Warnings, the location of the Countdown can be controlled by users.
- Only Countdowns sent via the Group chat are enabled by default. Other chat channels must be manually enabled.
- A blacklist for preventing Countdowns from specific players is also supported.

![Screenshot of the Countdowns Settings menu](Screenshots/ZDPS_Countdowns_Settings.png)

#### Threat Meter
> Accessed from `Threat Meter` in the `Raid Manager` on the Features menu.

Having a DPS steal threat from a Tank and cause the entire raid to be hit by a mechanic thankfully isn't too common here in BPSR. However, it can sometimes end up happening. With the Threat Meter you can now watch in realtime the different threat levels of each player against any target.

> [!IMPORTANT]
> Threat Meter is currently in a Beta state and while it is fully functional and accurate, it may not provide a lot of use to all users at this time. Please provide feedback to help shape the direction of it.

When using the Threat Meter, the current active target of the enemy will be shown at the top of the list, slightly further above everyone else. Their threat number will also typically be far higher than anyone else. Due to their incredibly high value when the main target, all other players threat bars are based on the _second_ highest threat value player.

### Benchmark
> Accessed from `Benchmark` on the Features menu.

Optimizing ones own rotation is a constant process. With the ability to define set Benchmark periods, you can perform consistent checks to see how much damage you can deal in a fixed time period and compare that with other players.

### Integrations Support
Blue Protocol: Star Resonance has a number of community driven efforts. In order to support those, ZDPS can integrate with them to provide a richer experience.

#### BPTimer Integration
> Accessed from `BPTimer` in `Integrations` on the Features menu.

> [!NOTE]
> BPTimer Integration, like most integrations, must first be enabled in the [Settings menu](#Settings-Menu) before it can be used.

With the BPTimer integration you are able to view Field Boss and Magical Creature spawns directly inside of ZDPS. You also have the ability to help contribute to that crowd sourced dataset.

![Screenshot of the BPTimer Spawn Tracker window](Screenshots/ZDPS_Integration_BPTimer_SpawnTracker.png)

In the Spawn Tracker, you can select what game region you want data for in addition to specifying what monsters to track and how many lines to show at once for each monster. If you wish to change the scale of the text or lines, settings for those can be found in the `Settings > Integrations` menu.

> [!TIP]
> If you find your connection to [BPTimer.com](https://bptimer.com/) to be interrupted and the Spawn Tracker does not automatically reconnect, try closing and opening the window again. If it is still a problem, click on the "Server" icon on the top of the menu bar to force a reconnection.

Like a number of other windows, you can pin this as top most, making it always be above other windows by clicking the "Thumbtack" icon. Additionally, it also supports the "Collapse To Content" feature, hiding the top options panel and leaving it just as a list of tracked spawns.

If you run into a monster which is already dead, but the tracker shows it as "Unknown" (or simply not already dead), you can right-click the Line for it and perform a manual report of the monster being dead. This helps keep the data on BPTimer accurate and all users benefit from accurate state reporting.

### Module Optimizer
> Accessed from `Module Optimizer` on the Features menu.

Directly within ZDPS you can load up your inventory of Modules and _within seconds_ find the best combinations based on your stat priority preferences to get the most out of your Modules.
- It really is fast! With nearly 1000 Modules ZDPS can find your results within seconds on a modern CPU. No GPU acceleration (like Nvidia CUDA) is needed to have incredibly fast times thanks to our CPU AVX2 optimized code.

![Example module combination results in the Module Optimizer](Screenshots/ZDPS_ModuleOptimizer_Results.png)

#### How To Use Module Optimizer
To get results immediately, using the tool is very straight forward.

1. With ZDPS running, log into your character, teleport to a new zone, or change Lines.
   - This will update your Module Inventory in ZDPS and save it to a local file so it can be used again without even having the game running.
2. Open the Module Optimizer if it is not already open.
3. Add any Stat Priorities you want for your build.
   - If you have a specific Stat that needs to be at least a specific level, enter it into the text box for that Stat. Such as `20` for `Armor` to ensure results given have that stat at 20 (or higher). If your requirement cannot be met, no result will be returned.
     - Values here can range between `0` (meaning it can be anything) and go up to `20` (or higher if you really wanted to but it's unlikely you would have a match then). Keep in mind these are the values, not the Breakpoint Levels (lvl 1-6).
   - You can use the Up and Down arrows on the left of each Stat Priority entry to shift their importance. Priorities closer to the top are weighted to be more valuable than those closer to the bottom of your list.
4. Click the `Calculate` button at the bottom.
5. Browse your results.
   - The Top 10 results found based on your inventory contents and Stat Priorities will be shown.

If you wish to share your Stat Priority list with other users, navigate over to the `Settings` tab within the window. Here you can copy your `Preset Share Code` for sending to others/posting online. Additionally, you can paste in a `Preset Share Code` from other users to instantly apply their setup to your window.

> [!NOTE]
> You can also change the `Link Level Boosts` for the calculations in the lower part of the Settings menu. The changes here ARE NOT included in a `Preset Share Code`. Be mindful of this when sharing/using `Preset Share Codes` to ensure you are getting the expected results.

> [!TIP]
> In the Settings menu for the Module Optimizer is a setting named `Include All Stats In Scoring`. By having this enabled (default), any stats not in your Stat Priority list are still scored and given value when calculating your best module combination. If you were to disable this setting (not recommended) then any stat not in your list would essentially be considered "worthless" and when you calculate your combination, the results will be different.

> [!IMPORTANT]
> The calculation phase of the Module Optimizer is entirely CPU based and _will_ use a significant amount of your CPU while the calculations are being performed. Once they are done, it will go back to barely using your CPU resources at all.

### Matchmaking Alert Notification Sounds
> Accessed from `Matchmaking` tab in `Settings`.

### Chat
> Accessed from `Chat` on the Features menu.

ZDPS features the ability to open a Chat window directly inside of it. This allows the game chat to integrate with ZDPS and provide a richer experience. It is worth noting this is just to _view_ chat, you will not be able to _send_ chat messages with this window.

![Screenshot example of the ZDPS Chat window](Screenshots/ZDPS_Chat_Example.png)

- Custom tabs with whatever Channel filters you want.
- Place the window anywhere on your PC and Pin it as Top Most so you can see it even without the game visible.
- Set your own Level filter per tab and filter out messages to only hide or show ones with specific keywords.
- Background and Window opacity can be changed.
- Clicking on the name of a player opens a context menu allowing you to easily copy their name or UID to the clipboard.

> [!NOTE]
> Plenty more ZDPS-exclusive features are in the works to help make the Chat experience even better!

### Settings Menu
> Accessed from `Settings` on the Features menu.

ZDPS supports a wide variety of settings. Most of these can be found in the dedicated Settings menu, though some windows may also contain their own smaller set of specific settings either in their own Settings tab or various other menus such as a right-click menu.

![Screenshot of the General tab in the Settings menu](Screenshots/ZDPS_Settings_GeneralTab.png)

Below will be descriptions for some, but not all, of the settings contained within this menu. It's strongly encouraged to go through the menu to see all that is offered as of the latest version. Many items within ZDPS also have tooltips to provide further clarification as well.

> [!NOTE]
> Settings are applied only when Save is clicked. They are not applied in realtime.

#### General Tab

`Network Device`

ZDPS will attempt to select the most likely network device on your machine as the default selection to capture game data from. However, if you find no data appears in ZDPS, you may need to come here and change the selection.

`Game Capture Preference`

By default, ZDPS captures game data from the automatically detected running game process. However, some users may decide to run multiple version of the game at once (`Standalone`, `Steam`, `Epic`, etc.). In these cases, you will want to change this setting to be for the specific version you want to actually capture the data of. Otherwise both clients will send their data to ZDPS and result in incorrect data reporting.

> [!IMPORTANT]
> If your game platform/region is missing from the dropdown list, or game data is not being detected while set to `Auto`, you may need to use the `Custom` option. When this is selected, a new textbox will appear for you to enter the _name_ of your game executable (without the `.exe` extension). For example, this would be `BPSR_STEAM` if running on Steam. Your game executable is located next to a file named `GameAssembly.dll` in the game installation directory.
>
> If you need to make use of `Custom` please contact us with the executable name you are having to use, along with what platform/region it is for. By doing so we can add proper support to it in the dropdown list and in `Auto` detection.

##### Keybinds
ZDPS supports setting Keybindings for specific features.

> [!IMPORTANT]
> Since the game itself runs as Administrator in order for your key presses to be seen by another application such as ZDPS (while the game is in focus), you need to run ZDPS as Administrator too. If you do not, your Keybinds will only work while the game is not in focus.

Currently you are able to set a Keybinding for `Encounter Reset` and `Pinned Window Clickthrough`.

As can be seen in the above screenshot of the Settings menu, the `Encounter Reset` Keybind (and button) are typically going to be unnecessary for ZDPS users as the tool is capable of detecting your Encounter states and splitting them automatically for you, including when you wipe.

In order to take advantage of having a window pinned on your screen, but interacting with the content behind it, you will want to setup a binding for `Pinned Window Clickthrough`. When you press this Keybind, any window which is Pinned will stop registering your input for it. To help identify when this mode is active, the `Pin` button will turn `Red` if Clickthrough is currently active. Otherwise it will be a white/grey color depending on Pinned state.

##### ZDPS Update Checking
ZDPS is capable of checking online for new versions and alerting you if one is found.

> [!IMPORTANT]
> As stated previously, ZDPS never sends any web requests or data without your permission first. That means Update Checking is _disabled_ by default. It is however recommended to enable this setting so you are always aware of when an update is available.

#### Combat

![Screenshot of the Combat tab in the Settings menu](Screenshots/ZDPS_Settings_CombatTab.png)

`Normalize Meter Contribution Bars`
- Having this enabled will present a familiar style of DPS bar filling. Where the bar for each player is their contribution percentage out of 100%. If this is disabled, the bars will fill their contribution amount to have a combined total of 100% instead.

`Use Short Width Number Formatting`
- This will keep the numbers displayed a short and consistent width, avoiding values being shifted around in relation to other entries, making it easier to directly compare vertically stacked numbers.

`Use Automatic Wipe Detection`
- This is one of the special features in ZDPS where it watches for player and boss states to determine when a party wipe has occurred and when found, splits your Encounter automatically for you. This is a setting should that should pretty much always be enabled.

`Skip Teleport State Check In Automatic Wipe Detection`
- This should very rarely ever be enabled. It exists primarily for reporting a wipe in very specific content that does not perform a "teleport" animation for the player when they are respawned. These instances are almost exclusively just Guild Hunts.

`Split Encounters On New Phases`
- This is perhaps one of the biggest strengths of ZDPS. While this is enabled, you never have to worry about manually splitting an Encounter again, or thinking about how long to set an arbitrary combat timeout timer. ZDPS watches for what objectives are happening in an Encounter, such as a dungeon or raid, and automatically will split your Encounters for you when it detects you've changed phases.
   - This also means for Raid Bosses with multiple phases each one is split up. For example:
     - Ice Dragon contains 3 Phases, initially fighting the boss, transitioning to the Ice Spear Pathways, and returning to the proper arena to finish the fight.
     - Void Dragon contains only 1 Phase, which is the entire fight including going through the Dimension Banishment.
     - Light Dragon contains 3 Phases, the initial fight, the crystal platforms, and returning to finish the fight.

`Display True Per Second Values In Meters`
- Most DPS Meters, including ZDPS, report the "active" Per Second Values of a fight. However, this means during downtime the DPS number is not actively dropping. For those that wish to see the true realtime DPS values, turning on this setting will display an additional field during combat that contains the actual DPS as it rises and falls every second. Both the "active" and "true" values are accurate but are different measurements of the data.

#### User Interface

`Show Class Icons In Meters`
- By default class icons will be shown next to players in the meters. This setting allows you to hide them if you wish.

`Color Class Icons By Role Type`
- Class icons in the Meters can be either colored by their role (Tank/Healer/DPS) or white like the rest of the text.

`Show Skill Icons In Details`
- Skills can be hard to figure out just by their names for classes you don't play. With this setting enabled the icon for the skill will be shown next to it, making it easier to identify what the skill is.

`Only Show Damage Contributors In Meters`
- By default, ZDPS will show all nearby entities in the Meters UI even if they never perform a single attack. If this is enabled, the entities will need to perform an attack to show up.
  - Even if an entity does not appear in the meter because it did not attack, they are still being tracked in the Encounter data.

`Show Ability Score In Meters`
- A player's ability score is shown after their name (and profession) in the meters. This setting will allow you to hide that.

`Show Sub Profession Name In Meters`
- By default, the profession for a player will be shown after their name. Once they use a skill, their sub profession will be detected and shown in place of that. By turning off this setting you can completely hide the (sub) profession names from being displayed in the meters.

`Allow Gamepad Navigation Input In ZDPS`
- By default only Mouse and Keyboard input is supported in ZDPS. This setting allows you to turn on Gamepad input if you feel the need for it. This can however cause issues with Gamepad input being read by ZDPS even if it's not in focus so use this setting carefully.

`Keep Past Encounter In Meter UI Until Next Damage`
- Normally when an Encounter ends, _and_ a new one begins, the Meter UI will switch over to the new Encounter right away even before any damage has been recorded. If this setting is enabled, it will instead hold onto the past Encounter until damage is finally dealt. Note that it will still switch over to the new Encounter if you change maps even with this setting enabled.

`Pinned (Top Most) Window Opacities`
- In here you can set how transparent you want a pinned window to be.

`Window Scales`
- In here you can change various scales for different windows. Not all windows support being scaled and some may have more options than others. These settings are typically useful for 2K and 4K resolution users.

`Low Performance Mode`
- In the event ZDPS is using abnormally high CPU usage, this setting will force it to run at a much lower rate. While in this mode you may experience suttering ZDPS windows or sometimes slow to respond actions. This setting should not be needed by the vast majority of users.

#### Matchmaking

ZDPS has the ability to play alert notification sounds when specific events are triggered. Alerts support changing their individual volume levels and looping the sound file until the event times out or you respond to it.

`Play Notification Sound On Matchmake`
- When a matchmaking (or Challenge) queue pop occurs, an alert sound can be played on your system to let you know it happened without the game being in focus or even visible.

`Player Notification Sound On Ready Check`
- When a party leader performs a Ready Check, you can setup a sound to play even without the game in focus or visible. This can help make sure you're aware of it and respond to it before it disappears.

> [!NOTE]
> If you do not want to change the volume level in ZDPS, you can use the Windows Volume Mixer to adjust the ZDPS volume - changing all sounds it plays at once. This however is not the recommended way of adjusting them.

> [!TIP]
> If you don't have a custom `MP3`/`WAV` file to use, but want a different sound, ZDPS has a few different audio files you can use located in the `Data/Audio` directory. The default file used for alerts is the `Data/Audio/LetsDoThis.mp3` file.

#### Integrations

ZDPS supports integrating with third-party services. In this menu you can control which ones are specifically enabled.

`Save Encounter Report To File`
- This specific setting isn't actually tied to any particular third-party service but is part of the local integration feature. When it's enabled, it'll save your Encounter Report to a local file in a `Reports` sub-directory as a `PNG`.
  - Even if other Report integrations are disabled, this will still write the Encounter Report file for you, as long as this setting is enabled.

`Minimum Player Count To Create Report`
- This setting is useful for when running solo content or when you only care about creating reports for specific player sizes such as 12 and 20 player raids.
  - Bots are considered part of this count when running content in 'Challenge' mode.

##### ZDPS Report Webhooks

Reports are able to be sent automatically to Webhooks of your choice. This is most commonly done via Discord Webhooks but a completely custom Webhook is also supported.

`Webhook Mode`
- By default the `Discord Webhook` mode it selected. This allows you to enter in a Discord Webhook for a specific channel and automatically send your Encounter Reports to it. However, if multiple users in your Encounter are running ZDPS with this setting, each user will send a report - resulting in duplicate messages.
  - To avoid duplicate messages, using the `Discord Deduplication` mode is suggested. This will first send a web request to the selected web server (defaulting to a ZDPS server), that determines if your request is allowed to be sent to Discord or if someone else has already reported that Encounter within the past few seconds. The message sent to Discord is ultimately coming from your own PC.
  - `Fallback Discord Deduplication` (Not Recommended) is another setting which is no longer supported by the current ZDPS web server. When this is used, your entire Encounter Report is sent off to the Deduplication Server and if it's allowed to be sent to Discord, the server itself would send the message rather than your own PC.
  - `Custom URL` is how you can send your Encounter Report to any arbitrary server you want and let it perform whatever actions it is setup for. This is likely not an option most users will want to use as the data sent is currently the same as the Discord messages and not something intended for third-party services to process _at this time_.
    - An option to send raw data consumable by a third-party service is planned to be added later.

> [!NOTE]
> Discord Webhooks need to be setup in the desired server by a user with high enough permissions. A single Webhook URL can then be provided to all users who want to automatically post their Encounter Reports to that channel. Discord's official documentation on creating Webhooks can be found [here](https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks).

##### BPTimer

As mentioned already, BPTimer is one of the launch Integrations for ZDPS. In this section you can configure if it is enabled and if so, what parts of it are functional.

`BPTimer Enabled`
- This setting alone just enables the BPTimer Integration feature - allowing the Spawn Tracker to be used, but you will not be reporting any data to help it.

`Include Own Character Data In Report`
- By default, your Character UID is not included in Reports sent to BPTimer. With this enabled, it will be sent. As it's currently optional to include, the setting is purely a choice for if you want to contribute that back to BPTimer or not.

`BPTimer Field Boss HP Reports`
- This setting allows your client to send data to BPTimer for helping crowd source the Field Boss and Magical Creature line status'. It is recommended that if you enable `BPTimer` that you also enable this to help the crowd sourcing data efforts.

#### Development

Most of the settings in this menu should be ignored by regular users. However, there is one noteworthy setting.

`Write Debug Log To File`
- Having this enabled (default), allows ZDPS to write a log file named `ZDPS_Log.txt` to the directory of ZDPS. This is important to supply when submitting bug reports or requesting help with an issue.
  - Running ZDPS while a `ZDPS_Log.txt` already exists will rename the last one to being `ZDPS_log_last_run.txt`. This can be useful for ensuring you provide the correct log, and gives a chance to avoid losing a potentially important debug log file.

## Additional Notes
ZDPS was internally developed for the Harmony guild before growing into a large scale project and public release. Special thanks to all of the Harmony members who helped with testing and providing feedback during the ZDPS initial development.

![Harmony guild representing ZDPS in the only way they know how](Screenshots/ZDPS_GuildPicture_Harmony.png)
> Harmony guild representing ZDPS in the only way they know how.