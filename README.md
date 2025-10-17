# ToggleAim

A BepInEx mod for MycoPunk that changes weapon aiming from hold-to-aim to toggle aiming mode.

## Description

This client-side mod transforms the default hold-to-aim mechanics into a toggle mode where pressing the aim button once enters aiming mode and pressing again exits it. This is especially beneficial for controller users and players with accessibility needs who prefer not to hold buttons for extended periods.

The mod also includes smart sprint resumption - after you stop aiming and/or firing, the mod automatically resumes sprinting after a short cooldown, making movement fluidity much smoother during combat. Toggle state is properly reset on player death/resurrection.

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Option 1: Via Thunderstore (Recommended)**
1. Download and install using the Thunderstore Mod Manager
2. Search for "ToggleAim" under MycoPunk community
3. Install and enable the mod

**Option 2: Manual Installation**
1. Ensure BepInEx is installed for MycoPunk
2. Copy `ToggleAim.dll` from the build folder
3. Place it in `<MycoPunk Game Directory>/BepInEx/plugins/`
4. Launch the game

### Executing program

Once installed, the mod works automatically and affects all weapon aiming:

**Toggle Behavior:**
- **Enter Aim:** Press aim button once to start aiming (Right mouse/Left trigger)
- **Exit Aim:** Press aim button again to stop aiming
- **Visual Feedback:** Same aiming mechanics and crosshair as hold mode
- **ADS Functionality:** All weapon aim-down-sights features work normally

**Smart Sprint Resume:**
- After un-aiming and stopping fire for 0.5 seconds, auto-resumes sprinting
- Smooth transition between combat and movement
- No more forgetting to start sprinting again

**State Management:**
- Toggle resets to false on player death/resurrection
- Proper state tracking prevents stuck aiming states
- Compatible with all weapons and aiming mechanics

## Help

* **Feels different?** Mod changes aim from hold-to-toggle - that's intentional for accessibility
* **Controller/keyboard?** Particularly helps controller users and those preferring toggle inputs
* **Sprint not resuming?** Checks that you're not actively firing/aiming before auto-resuming
* **Performance impact?** Minimal - only monitors input states and manages toggle flag
* **Conflicts with mods?** Shouldn't conflict unless other mods heavily modify aiming or sprint logic
* **Input binding?** Uses the same aim button as default - no need to rebind controls
* **Stuck in aim?** Disable mod temporarily or toggle aim twice to fix state
* **Testing?** Press aim button, move around while aimed, press again to exit

## Plugin Configuration

The mod provides configuration options (though most users won't need to change defaults):

* **EnableAimToggle:** Toggle the entire mod on/off
* The rest of the configuration is handled through the mod itself

## Authors

* Sparroh
* funlennysub (original mod template)
* [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

* This project is licensed under the MIT License - see the LICENSE.md file for details
