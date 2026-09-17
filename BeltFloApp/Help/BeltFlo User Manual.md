# BeltFlo User Manual

BeltFlo is a yield monitoring application for potato and other root crops, working alongside AgOpenGPS (AOG). It weighs the crop crossing a conveyor scale on the harvester, records yield and GPS position continuously while digging, and produces spatial yield maps, truck load records and job reports.

BeltFlo receives GPS and section control data from AOG over a local network (UDP). A BeltFlo hardware module reads load cells under a weighed section of the harvester's conveyor, counts belt travel, and sends cumulative pounds to the PC application.

**Weight alone is not enough.** A load cell reads how much crop is sitting on the weighed section at that instant, not how much has gone past. BeltFlo multiplies that load by how far the belt has moved:

> pounds = load on the weighed section × (belt travel ÷ section length)

This is why belt travel is counted rather than timed. Conveyor speed varies with engine revs and hydraulic flow, so anything measured against the clock would be wrong whenever the machine changed speed.

> **Work in progress.** BeltFlo is under active development and has not yet been run on a harvester. Treat every number it produces as unproven until you have checked it against certified truck weights.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Main Screen](#2-main-screen)
3. [Jobs](#3-jobs)
4. [Crops](#4-crops)
5. [Conveyor Setup](#5-conveyor-setup)
6. [Profiles](#6-profiles)
7. [Fields](#7-fields)
8. [Scale Calibration](#8-scale-calibration)
9. [Loads](#9-loads)
10. [Yield Map](#10-yield-map)
11. [Job Report](#11-job-report)
12. [Settings](#12-settings)
13. [Language](#13-language)
14. [Module Web Portal](#14-module-web-portal)
15. [Troubleshooting](#15-troubleshooting)

---

## 1. Getting Started

### Requirements

| Item | Requirement |
|------|-------------|
| Operating system | Windows 10 or Windows 11 |
| .NET Framework | 4.8 (pre-installed on Windows 10 May 2019 Update and later) |
| AgOpenGPS | Running on the same PC or local network |

**No installer is required.** Extract the BeltFlo folder to any location and run `BeltFlo.exe`.

### Hardware module

- BeltFlo module (based on ESP32) mounted on the harvester
- Connected to load cells under a weighed section of the conveyor
- Connected to a proximity sensor counting belt travel
- Communicates with the PC via WiFi, wired Ethernet or CAN bus

### Network

- **WiFi mode:** connect the PC to the same WiFi network as the BeltFlo module, or directly to the module's access point
- AOG must be running and broadcasting GPS/section data on the local network

### Recommended first-run setup

On first launch BeltFlo creates a default crop and profile. The order below matters: each screen depends on the ones above it.

1. Open **Menu → Settings** and select your units — cwt/ac, tons/ac or t/ha
2. Open **Menu → Profiles** and describe the harvester: rows, row spacing, how far the digger sits ahead of the tractor's pivot, and whether the scale weighs into the truck or into a tank
3. Open **Menu → Conveyor Setup** and measure the belt, the weighed section and the thresholds
4. Open **Menu → Scale Calibration** and zero the scale on an empty running belt, then set the span with a known weight
5. Open **Menu → Crops** and add the crops you dig
6. Open **Menu → Fields** and add field names if desired

Steps 2 to 4 are the installation. They are done once per harvester and they decide whether every number after them is right.

---

## 2. Main Screen

The main screen displays live harvest data, the open truck load, and job status.

### Toolbar buttons

Left to right, as they appear on screen:

| Button | Action |
|--------|--------|
| **Exit** | Close BeltFlo |
| **─** | Switch to compact (mini) view — see below |
| **Menu** | Open the main menu |
| **▶** | Start a truck load, or resume counting after a pause |
| **⏸** | Pause — stop counting everything |
| **⏹** | Finish the open truck load (asks first) |

**▶ and ⏹ control the truck load, not the job.** Jobs are started and finished only on the Jobs screen, so a mistaken tap in a moving cab can never end a day's job.

The menu screen has a **?** button in its title bar that opens this manual.

### Data panels

| Panel | Description |
|-------|-------------|
| **YIELD** | Instantaneous yield rate, smoothed over recent readings |
| **LOAD n** | Weight in the truck being filled, numbered within the job. Blank between loads. Tap the title to open the Loads screen. |
| Totals area | Average yield, total harvested, and area worked for the current job |

While paused, the load weight turns orange and its unit is replaced by **PAUSE**, so a pause left on by mistake is hard to miss.

### Sensor bars

Two horizontal bar gauges below the data panels:

- **Flow:** crop crossing the scale, in lb/min or kg/min.
- **Belt:** belt speed, in ft/min or m/min. The bar turns blue while the module reports the belt running.

Both read as a fraction of the full-scale values set on the Conveyor Setup screen. Set those from what your machine actually reaches, or the bars tell you nothing.

### Status bar

| Indicator | Green | Orange | Red / Silver |
|-----------|-------|--------|---------------|
| **GPS** | AOG connected and sending position | — | No AOG data |
| **Module** | Packets arriving and the module confirms it is receiving settings | Module sends but is not hearing BeltFlo — check the connection | No module data |
| **Scale** | Converter answering, scale tared, belt sensor keeping up | **Zero** — not tared yet; **Belt** — weight moving with no belt pulses | Red: scale fault or overload |
| **Job name** | Job recording | Job paused | No active job (silver) |

**The Scale indicator disappears entirely when there is nothing to report** — no module, or no recent status from it. That is deliberate: the Module indicator beside it already says the link is down, and showing Scale in red as well would accuse the load cells of a fault that has not been observed.

**Orange Belt** deserves attention: it means weight is moving across the section but no belt pulses are arriving. Because BeltFlo multiplies weight by belt travel, a dead belt sensor records *zero pounds* while the harvester digs — it does not read high or low, it reads nothing.

When BeltFlo displays a notification, a full-width message overlays the status bar and clears itself. Yellow text = informational; red text = error. Repeating alarms clear the moment you deal with them.

### Compact (mini) view

Press **─** in the top-right corner of the main screen to switch to a small floating overlay that shows only the live yield reading. This is useful when running BeltFlo alongside AgOpenGPS on a single monitor.

The compact overlay can be dragged anywhere on screen. Press the restore button on the overlay to return to the full main screen. Its position is remembered between sessions.

---

## 3. Jobs

**Menu: Menu → Jobs**

A job records yield, GPS position, area and truck loads for a single harvest session. Each job is linked to a crop, a harvester profile, and the number of rows being harvested.

### Starting a job

1. Press **Menu** on the main screen, then **Jobs**
2. Press **New** — a draft job is prepared with a default name and the current crop and profile
3. Select a **Field** (optional) — this replaces the default job name with the field name; the **Job Name** can still be edited afterward
4. Adjust **Crop** and **Profile** if needed
5. Set **Rows** — see below
6. Enter any **Notes** for the job (optional)
7. Press **Start**

The buttons on this screen are **New**, **Start**, **Finish**, **Save**, **Delete** and **Close**. Starting and finishing a job happens only here, never from the run screen.

Recording begins when harvesting conditions are met: the machine moving above 0.5 km/h, AOG sections on, and crop crossing the scale above the empty-belt threshold.

### Rows harvested

**Rows** defaults to the harvester's own row count from the profile, and that is right for most fields. Raise it when digging a **windrowed** field, where the machine picks up rows that were lifted and laid together earlier — it is collecting crop from more rows than it has diggers, and the area worked per pass is correspondingly wider.

The width the job uses is rows × row spacing, shown beside the setting. Saving a new row count on a running job applies it immediately.

There is no rows control on the run screen. It is not needed: BeltFlo already divides by the width actually meeting standing crop, so a half-overlapped pass at the end of a field comes out at the right yield without anyone touching a control while driving.

### Managing jobs

The jobs list shows all saved jobs with their status, date, area and linked field. Click a column header to sort by that column.

| Action | Description |
|--------|-------------|
| **Start** | Make a saved job the active job (prompts if another job is already active) |
| **Finish** | Close the active job and write its totals |
| **Delete** | Permanently remove a job and all its recorded data — cannot be undone |

Only one job can be active at a time.

### Auto-pause

The job pauses itself when harvesting stops:

- Machine speed falls below 0.5 km/h, **or**
- AOG turns all sections off (turning on the headland, travelling over dug ground)

When auto-paused the job name turns orange in the status bar. Recording resumes automatically when harvesting conditions return.

### Scale fault pause

Recording also stops if the scale stops being trustworthy — the module reports a converter fault or an overload, or module data stops arriving. The Scale indicator turns red and a message appears.

The pass in progress is ended at the last good position, so the map shows a gap over the ground crossed while the scale was down rather than filling it with a yield that was never measured. That ground is not counted in the job's acres or pounds.

### Pause — and what it means

Press **⏸** on the main screen to pause. **Pause stops counting everything**: nothing goes to the job, nothing to the open truck load, and no map points are recorded. Press **▶** to resume.

Pause is for the times when crop crossing the scale should not be credited to anything:

- **Cleaning the belt**, where dirt and trash cross the section
- **Clearing a jam by reversing the belt** — a single proximity sensor cannot tell direction, so reversing counts as forward travel and would add phantom pounds
- **Dumping crop on purpose**

Pausing also drops whatever is in transit between the digger and the scale, and closes the current map pass, so resuming starts a fresh ribbon rather than bridging the gap with a straight line across ground that was never dug.

### Resume from Pause when Sections On

A setting on the Settings screen, **Off** by default.

**On:** if AOG's sections switch from off to on while paused, BeltFlo resumes counting by itself and says so. Only the off-to-on change counts, so pausing with the digger already down does not immediately undo itself.

**Off:** BeltFlo beeps and shows a red warning every two seconds — *PAUSED - sections on, nothing counted* — until you press **▶**, the sections go off, or AOG stops sending. This is the safer default: it assumes you meant to pause and makes sure you cannot forget.

### Resume on start

If **Resume Job on Start** is enabled in Settings, the active job is reloaded when BeltFlo restarts. Useful if the PC is rebooted during a harvest day.

---

## 4. Crops

**Menu: Menu → Crops**

Crops are a name and nothing more — Potato, Sugar Beet, Carrot, Onion. Each job records which crop it dug, and reports and the job list show it.

Unlike a grain monitor, BeltFlo needs no crop parameters at all. The scale delivers pounds directly, so there is no bushel weight, test weight or moisture standard to get wrong. Nothing BeltFlo records depends on which crop is selected.

### Adding a crop

1. Press **New** and enter a crop name
2. Press **Save**

Select a crop from the list to edit it. At least one crop must exist at all times.

Select a crop and press **Delete** to remove it — you will be asked to confirm. A crop used by a saved job cannot be deleted.

---

## 5. Conveyor Setup

**Menu: Menu → Conveyor Setup**

Conveyor Setup holds everything BeltFlo needs to turn a weight on the belt into pounds harvested: how far the belt moves per pulse, how long the weighed section is, and when the belt counts as empty or stopped. The settings belong to the **active harvester profile**, so changing profile changes them all. They cannot be saved while a job is running.

### Conveyor settings

| Field | Description |
|-------|-------------|
| **Belt per Pulse** | How far the belt travels for one pulse from the belt sensor, in inches or centimetres. Set it with **Measure Belt** rather than by hand. |
| **Pulses per Belt Turn** | Pulses counted in one full turn of the belt. Filled in by **Measure Belt**. Used to average the zero over exactly one belt turn on the Scale Calibration screen. |
| **Weigh Section Length** | Length of the belt the load cells carry, in inches or centimetres. Weight × belt travel over this length is what BeltFlo integrates into pounds. |
| **Empty Belt Below** | The empty-belt threshold. Flow below it is treated as an empty belt and is not counted. See below. |
| **Belt Stopped After** | How long without a pulse before the belt counts as stopped. |
| **Dig to Scale Delay** | Seconds crop takes to travel from the digger to the scale. Map points are matched back to the position the machine held that many seconds earlier. |
| **Flow Bar Full At** | The flow the run screen's Flow bar reads as full. |
| **Belt Bar Full At** | The belt speed the run screen's Belt bar reads as full. |

### Empty Belt Below — setting the empty-belt threshold

A conveyor is never truly empty. Dirt, small stones, trash and crop residue ride the belt between tubers, and the load cells weigh all of it. Without a threshold BeltFlo would keep adding that rubbish to the job, the open load and the yield map all day, including during the long stretches when the harvester is running but not digging.

**Empty Belt Below** is the cut-off. While flow across the scale stays below it, nothing is counted — not the job total, not the truck load, and no map points. As soon as flow rises above it, counting resumes.

**The default is 3 lb/min (0.05 lb/s), and it is almost certainly too low for a real machine.** It was chosen before any readings from a real belt existed. Set it from your own machine, once, the first time the harvester runs:

1. Start the harvester and run the conveyor at normal speed with **nothing being dug** — the belt turning empty, as it does on the headland or while waiting for a truck.
2. Open **Menu → Conveyor Setup** and watch the live readouts at the bottom of the screen for a minute. Let the belt carry whatever dirt and trash it normally carries.
3. Note the highest flow you see on the run screen's Flow bar over that minute — that is your empty-belt noise.
4. Tap **Empty Belt Below** and enter a little above that figure, roughly 1.5 times the highest reading. A margin matters more than precision: too low and dirt counts as crop; too high and the thin flow at the start and end of a pass is thrown away.
5. Press **Save**.

Check it against a truck ticket after the first few loads. If BeltFlo consistently reads heavier than the certified weight and the calibration is good, the threshold is too low and dirt is being counted.

The threshold is stored per profile and can be changed whenever a job is not running. Changing it does **not** start a new calibration revision, because it does not alter pounds already recorded.

### Live readout

The bottom of the screen shows what the module is reporting right now: pulses since the last reset, pulse rate, belt speed, distance travelled and whether the belt is running. Without a module connected it reads "No module". **Reset Distance** zeroes the distance counter.

### Measure Belt

Belt per pulse is measured, not calculated:

1. Mark the belt with chalk or tape where it is easy to see.
2. Press **Measure Belt**.
3. Run the belt exactly one full turn, until the mark comes back round.
4. Press **Stop**.
5. Enter the belt's total length in feet or metres.

BeltFlo divides the length by the pulses counted and fills in both **Belt per Pulse** and **Pulses per Belt Turn**. Press **Save** to keep them.

### Bar full scale

The two bars on the run screen read as a fraction of **Flow Bar Full At** and **Belt Bar Full At**. Set them from what your machine actually reaches when digging well, not from what it could reach in theory — a bar that never leaves the first third tells the operator as little as one that is always pegged. The live readouts on this screen and the run screen's own numbers are the figures to use. These are display settings only: they change nothing that is recorded.

Because of that, **these two can be changed while a job is running** — which is when you will want to, having watched a bar sit pegged or barely move for half a field. Change them and press Save; harvesting carries on untouched.

### Saving

Everything that decides what a recorded pound means is locked while a job is running, so a job cannot change conveyors halfway through. Those rows are greyed, tapping one says so, and **Measure Belt** is unavailable. The two bar full-scale rows stay live.

Belt per pulse and weigh section length start a new calibration revision when they change, because they alter what a recorded pound means; the zero, the delay, the threshold, the belt-stopped timeout and the bar scales update the current revision in place. Saving bar scales mid-job therefore never starts a revision. Settings the module needs go to it immediately on Save.

---

## 6. Profiles

**Menu: Menu → Profiles**

A profile describes one harvester. Everything about the machine lives here — its rows, its geometry, where its scale weighs to, and, through the two screens above, its conveyor settings and scale calibration. Use a separate profile for each harvester if BeltFlo is moved between machines.

**Changing profile changes all of it at once**, including the calibration, which is why the conveyor and calibration screens always work on the active profile and say which one in their title.

### Profile settings

| Field | Description |
|-------|-------------|
| **Name** | Profile name (e.g. Grimme SE 260, Spudnik 6640) |
| **Harvester ID** | Identifier for the module — must match the module configuration |
| **Rows** | How many rows the digger lifts. Root-crop harvesters have a fixed row count, which is why this belongs to the machine and not to a header record. |
| **Row Spacing** | Centre-to-centre row spacing, in inches or centimetres |
| **Ahead of Pivot** | How far the digger sits ahead of the position AOG broadcasts. Enter AOG's **pivot-to-implement** distance from its implement setup. **Negative** for a digger that trails behind the tractor's pivot, which is the usual case for a towed harvester. |
| **Scale weighs into** | **Truck** or **Tank** — see below. This one setting changes how loads are corrected and which alarms apply. |

The resulting digging width is shown as you edit, so a mistyped spacing is obvious before it is saved.

### Scale weighs into: Truck or Tank

This describes where the weighed crop goes after it crosses the scale, and it is the most consequential setting on this screen.

**Truck** — the harvester loads directly into the truck alongside. What crosses the scale between one truck leaving and the next arriving is exactly one load, so a certified ticket can be matched against that load and used to correct it, or to correct the scale.

**Tank** — the scale sits before an on-board tank or bunker. Crop crosses the scale into the tank, and the tank empties into trucks on its own schedule. The map is still right, because crop reaches the scale a fixed time after being dug, but a truck load no longer corresponds to anything the scale saw. So:

- Load weights are approximate, and per-load correction is hidden
- The whole job is corrected instead, against the total of all its tickets, once the job is finished and every load has a ticket — empty the tank before finishing
- The "No load open" alarm is switched off, because the scale cannot see truck changes and would sound at every one

If your machine has no tank, choose Truck. If it has one and the cells are after it rather than before, the load weights will match trucks but the map cannot work, because time spent in the tank varies.

### Adding a profile

1. Press **New** and enter a profile name
2. Enter the Harvester ID to match your module
3. Enter the rows, row spacing and Ahead of Pivot
4. Choose whether the scale weighs into the truck or a tank
5. Press **Save**

Saving the active profile applies the changes immediately. A new profile starts with its own, uncalibrated conveyor configuration — it needs Conveyor Setup and Scale Calibration run against it before it will weigh correctly.

At least one profile must exist at all times.

Select a profile and press **Delete** to remove it — you will be asked to confirm. A profile used by a saved job cannot be deleted.

---

## 7. Fields

**Menu: Menu → Fields**

Fields are optional location labels that can be assigned to jobs for organisation and reporting.

### Adding a field

1. Press **New** and enter a field name
2. Press **Save**

Fields are not required. A job can be created without a field selected.

Select a field and press **Delete** to remove it — you will be asked to confirm. A field used by a saved job cannot be deleted.

### Importing fields from AgOpenGPS

Press **Import** to open a checklist of field names found in AgOpenGPS's and TWOL's field folders (`Documents\AgOpenGPS\Fields` and `Documents\TWOL\Fields`). Fields already present in BeltFlo are shown but cannot be re-checked. Tick the fields to bring in (or press **Select All**), then press **Import** to add them as new field records — only the field name is imported, not boundary data.

---

## 8. Scale Calibration

**Menu: Menu → Scale Calibration**

Two numbers turn a raw converter reading into pounds:

> weight = (reading − **zero**) × **span**

The **zero** is what the scale reads with nothing on it. The **span** is what one count of reading is worth. They are found by different procedures and they fail in different ways, which is what makes them separable in the field.

Like Conveyor Setup, this screen works on the active profile and is refused while a job is running.

### Live readout

The top of the screen shows what the module is reporting: raw counts, the weight that implies after the current zero and span, and whether the reading is **Stable** or **Unstable**. Stability is judged over the last two seconds; wait for Stable before trusting anything you read here.

### Zero Scale

Zeroes the scale against an empty belt. **The belt must be running** — a stationary belt does not average out the splice, the worn patches or the dirt that sits on one part of it.

1. Run the conveyor at normal speed with nothing being dug
2. Press **Zero Scale**
3. Wait — BeltFlo averages over one full belt turn if **Pulses per Belt Turn** is set on the Conveyor Setup screen, or 30 seconds if it is not. One belt turn is much the better figure, which is why Measure Belt is worth doing first.
4. Press **Save** to keep it

Nothing changes until you press Save.

**Re-taring is routine, not an installation step.** Dirt builds up on the cells and the belt through a day, and a zero error adds a fixed amount for every inch of belt that passes — whatever is on it. Over a day of running that becomes phantom pounds. Re-zero when the belt is empty and running, as often as is convenient.

### Known Weight — setting the span

1. Stop the belt
2. Place a known weight on the weighed section, with nothing else touching it
3. Press **Known Weight** and enter what it weighs
4. BeltFlo averages for 5 seconds and works out pounds per count
5. Press **Save** to keep it

The span is refused if the reading moved by less than 100 counts — that means the weight is not on the section, or the scale was never zeroed.

**A static weight is only an approximation, and it is important to understand why.** The span absorbs the entire chain between the load cells and a pound: cell sensitivity, amplifier gain, any error in the weighed section length, any error in belt travel per pulse, and belt slip. All of those are proportional, so one number corrects all of them at once. But hanging a weight on the section exercises the cells and nothing else. Only crop actually running across the belt exercises the geometry and the drive.

So the known weight gets the scale into the right order of magnitude on installation day, when no truck has been filled yet. The real calibration comes from a truck ticket — see the Loads screen — and it is applied only when you ask for it.

### Which one is wrong?

If loads come back wrong against certified weights, the pattern tells you which number to fix:

| What you see | What it means |
|---|---|
| Light loads wrong, heavy loads about right | **Zero** error. Re-tare. A fixed offset per inch of belt barely shows on a heavy load and badly distorts a light one. |
| Everything off by the same percentage | **Span** error. Use Update Calibration on the Loads screen against a ticket you trust. |
| Neither pattern — errors scattered | Stop adjusting. Check the mechanical installation: rigidity, dirt build-up, the free roller end, and whether anything is fouling the weighed section. Repeated calibration will not rescue a scale that is mounted badly. |

### Calibration revisions

Changing the **span** starts a new calibration revision, because it changes what an already-recorded pound meant. Changing the **zero** alone updates the current revision in place.

Every map point and every load stores the revision it was recorded under. That is what lets a correction applied weeks later know exactly which calibration produced the number it is correcting, and it is why updating the span from an old load's ticket cannot double-correct a span that has moved since.

---

## 9. Loads

**Menu: Menu → Loads**, or tap the **LOAD** title on the run screen

A load is one truck. BeltFlo records what its own scale measured, and later — often days later, when the tickets come back from the scale house — what the truck actually weighed. The difference between the two is the most valuable number in the system, because it is the only independent check on everything else.

### Running loads from the cab

On the run screen: **▶** opens a load, **⏹** finishes it. The LOAD tile shows the weight climbing in the truck being filled.

On a machine that loads directly into trucks, one tap splits the trucks exactly. Weight is load × belt travel, so while the unloading conveyor is stopped for a truck change nothing crosses the scale and nothing is counted — there is no need to rush the tap.

**"No load open" alarm.** If crop is crossing the scale with no load open, a job running and the profile set to Truck, BeltFlo beeps and shows a red message every two seconds after five seconds of flow. The weight still counts to the job and the map; only the truck record is missing. Press **▶** and the message clears at once. The alarm never sounds on a Tank profile, where the scale cannot see truck boundaries.

### The list

Every job's loads, oldest first. The load being filled is green and its weight updates each second.

| Column | Meaning |
|---|---|
| **Load** | Number within its job — load 1 is that job's first truck |
| **Job** | The job it belongs to. A load belongs to exactly one job and finishes with it. |
| **Truck** | Set with **Rename**; blank until you name it |
| **Monitor** | What BeltFlo weighed |
| **Ticket** | The certified weight, once entered |
| **Diff %** | How far apart they are |
| **Status** | Filling, Waiting, Weighed or Corrected, plus any flag |

### Flags

One tap sets a flag on the selected load: **Wet**, **Spoiled**, **Trash** or **Stones**, or **None**. Flags are stored in English so reports read the same in any language. They do not change any number — they record why a load was unusual, which matters when you come back to a strange-looking correction factor later.

### Entering a ticket

Tap the **Ticket** box and enter the certified weight. BeltFlo shows the difference and the factor (ticket ÷ monitor). Then choose what it means:

**Save Weight Only** — the ticket is recorded and the map is left exactly as measured. Use this when you do not trust the ticket, or when the load was genuinely unusual — a flagged load, or one you know included a clean-out tail. The load is marked Weighed.

**Correct This Load** — the difference is real and belongs to this load. Its map points and the job total are rescaled by the factor. Use this when one load is out and the others are not. The load is marked Corrected.

**Update Calibration** — the difference is the scale itself. BeltFlo shows you the old and new span, changes it, sends it to the module, starts a new calibration revision, and corrects the load as well. Use this when load after load comes back off by about the same percentage.

The new span is worked out from the span of **the load's own revision**, not the current one, so a span you changed last week cannot be corrected twice by a ticket from before it.

> **The monitor weight is never overwritten**, and points always rescale from the factor they carry now to the one wanted. Entering a different ticket later, or going back to Save Weight Only, never compounds.

### Tank machines

On a profile whose scale weighs into a tank, Correct This Load and Update Calibration are hidden, because one truck does not correspond to anything the scale saw. **Correct Job** replaces them: it rescales the whole job against the total of all its tickets. It is available only when the job is finished and every load has a ticket, and it works from the current job total, so pressing it twice changes nothing.

Empty the tank before finishing the job, or the crop still in it will be counted as harvested but never appear on a ticket.

### Other actions

| Action | What it does |
|---|---|
| **Rename** | Name the truck. This is the only place truck names are entered — starting a load never asks, because you are watching a truck fill, not typing. |
| **Reopen** | Continue a Waiting load of the running job, for a truck pulled aside and brought back to be topped up. Its weight carries on from where it stopped. Only possible when no other load is open. |
| **Delete** | Remove the load record. Confirmed first. The map points stay in the job. |
## 10. Yield Map

**Menu: Menu → Yield Map**

The yield map displays a colour-coded spatial plot of yield rate across the harvested area. Each data point is drawn as a filled swath polygon sized to the digging width and oriented to the GPS heading at that point.

### Colour scale

Yield is mapped to a five-colour gradient:

| Colour | Yield |
|--------|-------|
| Blue | Lowest |
| Cyan | Low |
| Green | Mid |
| Yellow | High |
| Red | Highest |

The legend at the bottom of the full-screen map shows the low and high yield values for the current data set.

### Mini mode

When opened from the menu, the yield map starts as a small floating 300×300 overlay (mini mode). The title bar shows the active job name and zoom controls.

| Control | Action |
|---------|--------|
| **─** / **+** | Zoom out / in |
| **×** | Close the map overlay |
| Click the map | Expand to full-screen mode |
| Drag the title bar | Move the overlay |

The mini map position is remembered between sessions.

### Full-screen mode

Click the map to expand to full screen. The toolbar at the top provides:

| Control | Action |
|---------|--------|
| Job selector (drop-down) | Choose which job to display |
| **─** / **+** | Zoom out / in |
| **Recalculate** | Explains where correction is done — see below |
| **Print** | Export the map as a PNG image — see below |
| **Close** | Close the map |

Click the map again to return to mini mode. The map can be dragged in both mini and full-screen modes.

### Live update during harvest

When the yield map is open and a job is actively recording, the map updates automatically every 2 seconds. New swath polygons are added without re-centering the view.

### Recalculating yield

A map is corrected from the **Loads** screen, not from here, because in BeltFlo a correction comes from a certified truck weight rather than from a crop setting. Entering a ticket and pressing Correct This Load rescales that load's points and the job total; on a tank machine, Correct Job rescales the whole job. The Recalculate button says as much if pressed.

Corrections apply to the map as soon as they are made, so a map open on screen shows the corrected values the next time it refreshes.

### Exporting the map as a PNG

Press **Print** in the full-screen toolbar to export the current map view (including the legend) as a PNG image.

1. A save dialog opens with a default filename of `JobName_Date.png`
2. Choose a folder and confirm
3. The image is saved and a confirmation appears in the status bar

The export folder is remembered for subsequent exports.

---

## 11. Job Report

**Menu: Menu → Reports**

The job report shows a summary of a completed or active job. Select a job from the list on the left to view its details.

### Report contents

| Field | Description |
|-------|-------------|
| **Job name** | Name entered when the job was created |
| **Field** | Field name assigned to the job (if set) |
| **Crop** | Crop assigned to the job (if set) |
| **Area** | Total area harvested (ac or ha) |
| **Total** | Total harvested (cwt, tons or tonnes) |
| **Avg Yield** | Average yield rate across all data points |
| **Loads** | Number of truck loads recorded for the job |
| **Data Points** | Number of recorded GPS data points |

### Printing a report

Press **Print** to send the job summary to the system print dialog. The report is formatted as plain text suitable for any printer.

### CSV export

Press **Export CSV** to save all raw data points for the selected job.

1. A save dialog opens with a default filename of `JobName_Date.csv`
2. Choose a folder and confirm
3. The file is saved and a confirmation appears in the status bar

The export folder is remembered for subsequent exports. The CSV format is compatible with the AgOpenGPS Rate Controller yield overlay.

| Column | Description |
|--------|-------------|
| Timestamp | Date and time of the reading |
| Latitude | GPS latitude (decimal degrees) |
| Longitude | GPS longitude (decimal degrees) |
| WidthMeters | Digging width (metres) — rows harvested × row spacing |
| Yield_kgha | Yield rate in kg/ha |
| ElevationMeters | GPS elevation (metres) |
| Speed_kmh | Ground speed (km/h) |
| Heading | GPS heading (degrees) |
| PoundsInc | Pounds recorded at this point |
| HaAccumulated | Cumulative area at this point (ha) |
| BeltFtMin | Belt speed at this point (ft/min) |

---

## 12. Settings

**Menu: Menu → Settings**

### Units

| Quantity | Imperial | Metric |
|---|---|---|
| **Yield rate** | cwt/ac or tons/ac | t/ha |
| **Area** | acres | hectares |
| **Total harvested** | hundredweight or short tons | tonnes |
| **Truck loads and tickets** | pounds, or short tons with tons/ac | kilograms |
| **Flow** | lb/min | kg/min |
| **Belt speed** | ft/min | m/min |
| **Digging width, row spacing** | feet, inches | metres, centimetres |
| **Scale readings** | pounds | kilograms |

Changing units takes effect immediately. Everything is stored internally in pounds and acres and converted for display, so switching units never alters a recorded figure. Truck loads and tickets follow the same choice: pounds with cwt/ac, short tons with tons/ac, kilograms in metric.

### Module communication

| Setting | Description |
|---------|-------------|
| **WiFi / Ethernet** | Module communicates over UDP — the module sends on port 30300 and listens on 30400 — via the module's WiFi access point / a shared WiFi network, or via wired Ethernet (W5500 board on the module). |
| **CAN** | Module communicates over CAN bus via a USB CAN interface. |
| **CAN Driver** | Select the CAN interface driver (SLCAN, InnoMaker, or PCAN). |
| **COM Port** | Serial/USB port for the CAN interface (SLCAN driver only). |
| Rescan button | Beside **COM Port** (CAN mode only) — refreshes the list of connected serial ports. |

When **CAN** mode is selected, an **Adapter: Connected** / **Not Connected** indicator appears below the port settings, showing whether the CAN adapter that is actually running (from previously saved settings) is open and seeing bus traffic. It updates live and does not reflect the driver/port currently selected in the drop-downs until they are saved and applied.

The module can also send the same UDP packets over **wired Ethernet** (W5500 board attached to the module). No app setting is needed — select Ethernet mode in the module's web portal and give the PC's wired adapter a static IP on the module's subnet (default `192.168.1.x`). The app receives WiFi and Ethernet packets on the same port.

> **Module setup page:** the module's own settings (communication mode, WiFi credentials, firmware update) are configured on its built-in web page at `http://192.168.200.1`, reached while connected to the module's WiFi hotspot — see [Module Web Portal](#14-module-web-portal).

#### Supported CAN adapters

| Driver | Adapters | Notes |
|--------|----------|-------|
| **SLCAN** | CANable (slcan firmware), SH-C30A, other SLCAN adapters | Appears as a COM port — select it under **COM Port**. |
| **InnoMaker** | InnoMaker USB2CAN | Requires the vendor's driver and SDK files: copy `InnoMakerUsb2CanLib.dll` and `LibUsbDotNet.dll` from the InnoMaker USB2CAN C# SDK (`Lib` folder) into the BeltFlo application folder. Download them as raw binaries, not from a GitHub web page. The COM Port setting is ignored — the first adapter found is used. |
| **PCAN** | Peak PCAN-USB | Install the PEAK device driver package (includes PCAN-Basic) from peak-system.com — no files need to be copied. The COM Port setting is ignored — the first adapter found on PCAN USB channels 1–8 is used. |

If CAN fails to start, the reason is written to the error log in `Documents\BeltFlo\Logs`.

### Resume Job on Start

When enabled, BeltFlo automatically reloads the active job when the app starts. Useful if the PC is rebooted during a harvest day.

### Saving settings

Press **Save** to store all settings and put them into effect immediately.

---

## 13. Language

**Menu: Menu → Language**

BeltFlo supports the following languages:

| Code | Language |
|------|----------|
| en | English |
| de | German |
| fr | French |
| hu | Hungarian |
| nl | Dutch |
| pl | Polish |
| ru | Russian |
| lt | Lithuanian |

### Changing language

1. Press **Menu → Language**
2. Select the desired language from the list
3. Press **Save & Restart**

BeltFlo will restart automatically with the new language applied.

> **Note:** If AgOpenGPS is already installed, BeltFlo will automatically use the same language on first launch.

---

## 14. Module Web Portal

The BeltFlo module has a built-in settings page served from its own WiFi hotspot. Use it to configure the communication mode and WiFi credentials, to check the scale hardware, and to update the module firmware.

### Opening the portal

1. Power the module. It broadcasts a WiFi hotspot named `BeltFlo_ESP32_XXXXXXXX` (the suffix is unique to each module).
2. Connect the PC, tablet, or phone to that hotspot.
3. Browse to `http://192.168.200.1` (with the default module ID 0 — the address is 192.168.(200 + module ID).1).

### Settings

| Setting | Description |
|---------|-------------|
| **Communication — Mode** | WiFi (UDP), CAN bus, or Ethernet (UDP over a wired W5500 board). WiFi/Ethernet and CAN must match the Module communication setting in the PC app — the app treats WiFi and Ethernet identically. |
| **Communication — Ethernet subnet** | First three octets of the wired network (default `192.168.1`). The module takes IP `subnet.(50 + module ID)`; give the PC's wired adapter a static IP on the same subnet (e.g. `192.168.1.10`). In Ethernet mode the portal shows whether the W5500 board and cable link are detected. |

Press **Save / Restart** to store the settings in the module and restart it.

### Conveyor Scale panel

Below the settings the portal shows the scale, read-only: whether the converter answered, its raw counts, the live load on the weighed section, the belt pulse count with running or stopped, and whether settings are still arriving from BeltFlo.

Nothing here can be edited, because the calibration belongs to the application — zero, span, section length and belt travel per pulse are set on the Conveyor Setup and Scale Calibration screens and sent down every two seconds. The panel exists to answer one question in the yard, before driving to the field: is this module's hardware alive?

WiFi settings are on their own page — follow the **WiFi Network** link at the bottom of the portal.

### WiFi network

The **WiFi Network** page joins the module to an existing network and sets the password for the module's own hotspot. The module keeps its hotspot running whether or not it joins a network, so the portal stays reachable either way.

The top of the page shows the current connection.

To join a network:

1. Press **Scan for Networks**. The list appears after a few seconds, strongest first, with a padlock on networks that need a password.
2. Tap a network name to fill it in.
3. Enter the password.
4. Tick **Use this Network**.
5. Press **Save / Restart**.

| Setting | Description |
|---------|-------------|
| **Network** | Name of the network to join. Tap a scan result to fill this in, or type it in for a hidden network. |
| **Password** | Password for that network. |
| **Use this Network** | Joins the network. Stays on until you turn it off. |
| **Hotspot — Password** | Password for the module's own hotspot. Use 8–10 characters, or leave empty for an open hotspot. |

If the module cannot join, it keeps trying in the background and the page shows the reason. **Password refused** means the password is wrong — correct it and press **Save / Restart**. Press **Retry Connection Now** to try again straight away after changing something at the router.

### Firmware update

The **Update Firmware** link at the bottom of the portal opens the over-the-air update page. Select the compiled firmware file (`.bin`) and upload it — the module flashes itself and restarts. The installed firmware version is shown at the top of the portal page.

> **Note:** A firmware update that changes the stored settings layout resets the module to factory defaults. Reopen the portal afterwards and re-enter your settings.

---

## 15. Troubleshooting

### GPS indicator stays red

- Confirm AgOpenGPS is running on the same PC or local network
- Check that both apps are on the same subnet
- Verify the network configuration in BeltFlo Settings

### Module indicator stays off

- Check the module is powered and the WiFi or CAN connection is active
- In WiFi mode, confirm the PC is connected to the correct network
- Check the module LED for status indication
- Restart the module and wait 10–15 seconds for it to connect

### Module will not join a WiFi network

- Open the module portal and follow the **WiFi Network** link — the page shows the current state
- **Password refused** means the password is wrong; correct it and press **Save / Restart**
- Press **Scan for Networks** to confirm the network is in range
- Press **Retry Connection Now** after changing anything at the router

### Yield reads zero while digging

Work down this list in order — the first two are far more common than the rest.

- **Check the Scale indicator for orange "Belt".** Weight is moving but no belt pulses are arriving, so BeltFlo multiplies by zero travel and records nothing. Check the proximity sensor, its gap to the targets, and its wiring.
- **Check the empty-belt threshold.** If it is set far too high, ordinary flow is being treated as an empty belt. Watch the Flow bar while digging and compare against **Empty Belt Below** on Conveyor Setup.
- Check the Scale indicator for orange **Zero** — the module has no zero, so it is not weighing yet. Run Zero Scale.
- Check the job is actually recording: running, not paused, sections on, above 0.5 km/h.
- On the module portal, check the Conveyor Scale panel shows the converter found and the belt pulses climbing.

### Yield reads high

- **Suspect the empty-belt threshold first.** Set too low, dirt, stones and trash on a running belt count as crop all day. The symptom is BeltFlo reading consistently heavier than the truck tickets.
- Re-zero the scale with the belt running empty. A zero that has drifted with the day's dirt adds a little for every inch of belt that passes.
- Check nothing is fouling the weighed section — a stone wedged against the frame, or crop build-up bridging to a fixed part of the machine.

### Loads do not match the truck tickets

- Read the table in [Scale Calibration](#8-scale-calibration) — the pattern of the errors tells you whether to re-tare or to change the span.
- Check the load was not flagged: a wet, trashy or stony load is genuinely different, not a calibration error.
- On a tank machine, individual load weights are expected to be approximate. Correct the finished job as a whole instead.

### App crashes on start

- Check `BeltFlo_Crash.log` in the application folder for details

### Job does not resume after restart

- Ensure **Resume Job on Start** is enabled in Settings
- The job must have been active (not stopped) when BeltFlo was closed

### AOG UDP failed to start

- If AgOpenGPS Rate Controller (RC) is running, both RC and BeltFlo must be built from current source for UDP port sharing to work
- BeltFlo will still function for module data — only GPS reception will be affected

---

*BeltFlo is designed for use with AgOpenGPS. It is not a certified weighing system and must not be used as the basis for settlement, sale or any other transaction. Use a certified weighbridge for that. BeltFlo's purpose is to show you where the crop came from.*
