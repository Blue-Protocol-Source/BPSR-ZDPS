# bpsr-zdps TODO

## High Priority
- [ ] Add an indicator that shows which enemy skill caused the player's death.

### Monster Monitor: Boss DBM (Boss Mechanic Timer Bars)

Add a dedicated **Boss DBM** system inside **Containers** that displays boss mechanic countdown bars similar to Deadly Boss Mods (WOW addon).

Unlike Buff Monitor, Boss DBM should require **no manual configuration**. The overlay automatically receives boss mechanic events from the game and creates timer bars whenever supported mechanics occur.

### Features

- [ ] Add new **Container → Boss DBM**.
- [ ] Parse in-game `SyncSceneEvents` DBM events.
- [ ] Resolve skill names through `DbmTable.json`.
- [ ] Fallback to `#skillEffectId` for unknown entries.
- [ ] Automatically create/remove timer bars.
- [ ] Sort active mechanics by remaining duration.
- [ ] Progress bars showing remaining cast/mechanic time.
- [ ] Multiple simultaneous mechanics.
- [ ] Preview placeholder while editing layout.
- [ ] Separate enable/disable toggle.

### Customization

- [ ] Row spacing
- [ ] Column spacing
- [ ] Font size
- [ ] Font colors
- [ ] Countdown colors
- [ ] Progress bar color
- [ ] Progress opacity
- [ ] Progress height
- [ ] Border options
- [ ] Background color
- [ ] Padding
- [ ] Maximum visible mechanics
- [ ] Growth direction
- [ ] Alignment
- [ ] Animation options

---

## Dungeon Mechanics Overlay

Implement a dedicated **Dungeon Mechanics** overlay using the minimap overlay window.

Unlike Containers, this overlay focuses on dungeon-specific mechanics, positioning, callouts, and mechanic visualization.

### Overlay

- [ ] Standalone overlay window
- [ ] Automatic activation in supported scenes
- [ ] Automatic cleanup when leaving dungeon
- [ ] Scene-based parser loading

### Panels

#### Map Panel

- [ ] Player locations
- [ ] Party locations
- [ ] Boss location
- [ ] Safe zones
- [ ] Danger zones
- [ ] Stack markers
- [ ] Spread markers
- [ ] Direction indicators
- [ ] Mechanic objects
- [ ] Lines
- [ ] Area highlights

#### Mechanic Calls

- [ ] Mechanic names
- [ ] Countdown timers
- [ ] Target players
- [ ] Priority warnings
- [ ] Color coding
- [ ] Icons

### Options

- [ ] Auto-hide in daily scenes
- [ ] Show only relevant teammates
- [ ] Boss marker toggle
- [ ] Self highlight ring
- [ ] Separate visibility for map/calls
- [ ] Layout editor
- [ ] Resize
- [ ] Position saving

### Initial Dungeon Support

- [ ] Cursed Tomb
- [ ] Sea-Ringed Reef
- [ ] Forgotten Dreamwild
- [ ] S3 Giant Tower
- [ ] Tina's Mind Realm

---

# Automatic Boss Detection

Automatically detect when a boss encounter starts and allow the user to switch into a **Boss Combat Mode** that filters all combat statistics to the current boss only.

This solves a common issue where mobs, summoned enemies, adds, or environmental targets heavily skew DPS statistics during boss fights.

### Detection

- [ ] Detect boss spawn.
- [ ] Detect boss engagement.
- [ ] Detect encounter start/end.
- [ ] Automatically switch target if phases spawn a new boss entity.
- [ ] Support bosses with multiple IDs/forms.
- [ ] Return to normal mode after encounter ends.

### Boss-only Combat View

Provide an optional "Boss Only" mode.

Instead of showing all combat:

- [ ] Damage dealt to boss
- [ ] Damage received from boss
- [ ] Healing during boss fight
- [ ] Boss-only DPS
- [ ] Boss-only HPS
- [ ] Boss-only DTPS
- [ ] Boss uptime
- [ ] Phase duration
- [ ] Encounter timer

Ignore:
(optional toggles):
- [ ] Mobs
- [ ] Adds (optional toggle)
- [ ] Environmental objects
- [ ] Temporary summons
- [ ] Non-combat NPCs

### Overlay Integration

Allow users to open a dedicated Boss overlay window.

Possible widgets:

- Group DPS
- Group Damage taken
- Group Healing received
- Interrupt count

### Configuration

- [ ] Automatic mode
- [ ] Manual override
- [ ] Include adds
- [ ] Exclude adds
- [ ] Merge multi-phase bosses
- [ ] Separate phases
- [ ] Boss history
- [ ] Last encounter summary

---

# Overlay & Container Customization

Expand every overlay to become a complete HUD replacement.

The goal is allowing players to disable in-game HUD elements while relying on ZDPS overlays.

## General

- [ ] More modular overlay system
- [ ] Widget-based layout
- [ ] Drag & drop widgets
- [ ] Independent scaling
- [ ] Independent opacity
- [ ] Independent visibility
- [ ] Anchor system
- [ ] Layer ordering
- [ ] Multi-monitor support
- [ ] Import/export layouts
- [x] Layout presets

## Additional Trackers

Add optional widgets for:

### Player

- [ ] HP
- [ ] Shield
- [ ] Energy
- [ ] Stamina
- [ ] Class resources

### Combat

- [x] Cooldowns
- [x] Buffs
- [x] Debuffs
- [ ] Cast bar
- [ ] Target cast bar
- [ ] Boss cast bar
- [ ] Interrupt cooldown
- [ ] Resurrection availability

### Party

- [ ] HP
- [ ] Resources
- [x] Important buffs
- [ ] Death status
- [ ] Distance
- [ ] Mechanic markers

### Boss

- [ ] HP
- [ ] Shield
- [ ] Break gauge
- [ ] Phase
- [ ] Casts
- [ ] Enrage timer
- [ ] Vulnerability windows

---

# Sorting Options

Allow users to decide how rankings are sorted across every statistics table and overlay.

### Supported Modes

- [ ] Sort by Total Damage
- [ ] Sort by DPS
- [ ] Sort by Real DPS
- [ ] Sort by Healing
- [ ] Sort by HPS
- [ ] Sort by Damage Taken
- [ ] Sort by DTPS
- [ ] Sort by Buff Uptime
- [ ] Sort by Critical Rate
- [ ] Sort by Skill Count
- [ ] Custom sorting

### Additional Options

- [ ] Ascending
- [ ] Descending
- [ ] Sticky local player
- [ ] Preserve manual order
- [ ] Remember last selection

---

# Bottom Bar Improvements

Allow users to choose what is displayed in the bottom summary bar instead of always showing their own values.

## Display Modes

### Personal

- [x] My statistics

### Group Subtotal

- [ ] **Subtotal every visible player**
- [ ] Aggregate every visible player

Possible uses:

- **Compare group healing versus group damage taken.**
- Compare total group damage against boss HP.
- Measure raid DPS progression.
- Track total healing output.
- Monitor incoming raid damage.
- Evaluate whether the group can meet DPS checks or enrage timers.
- Estimate remaining encounter duration based on current raid DPS.

### Available Summary Types

- [ ] Total Damage
- [ ] Total DPS
- [ ] Total Real DPS
- [ ] Total Healing
- [ ] Total HPS
- [ ] Total Damage Taken
- [ ] Total DTPS
- [ ] Total Shields
- [ ] Total Interrupts
- [ ] Total Revives
- [ ] Boss Remaining HP Percentage
- [ ] Estimated Time to Kill (TTK)

### Options

- [ ] Switch bottom bar between Personal and Group modes.
- [ ] Custom summary widgets.
- [ ] Multiple bottom bar layouts.
- [ ] Compact mode.
- [ ] Detailed mode.
- [ ] Color thresholds.
- [ ] Automatic boss-mode summary switching.

---

# Future Ideas

- [ ] Encounter replay support
- [ ] Combat timeline
- [ ] Phase analysis
- [ ] Death recap
- [ ] Performance comparison against previous pulls
- [x] Historical encounter database
- [ ] Shareable combat reports
- [x] Plugin/API system
- [ ] Advanced overlay themes
- [ ] Profile sync/import/export

