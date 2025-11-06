# OmoriSandbox
![OmoriSandbox Logo](./assets/logo.png)

A battle simulator/sandbox for Omori, written in C# for the Godot engine. It aims to recreate the battle system in the game as accurately as possible, allowing users to create any kind of battle scenario they desire. I started this project in my attempt to learn the Godot engine through recreating a game that I thorougly enjoy.
## Installation
Simply download the latest release archive from the "Releases" section and extract the contents to any folder. There are two versions to choose from:
### For Windows Users:
- `OmoriSandbox.zip` uses the Vulkan renderer and works best on newer systems. Recommended for most users.
- `OmoriSandbox_Compat.zip` uses the OpenGL renderer and works best on older systems. Use this if you're having issues with the Vulkan version.

Either archive should contain two executables:
- `OmoriSandbox.console.exe`: Runs the Sandbox alongside a seperate console window. Useful for viewing debug information and any errors that may occur while using the Sandbox. (Recommended)
- `OmoriSandbox.exe`: Runs just the Sandbox without a console.

### For Linux Users
- `OmoriSandbox_Linux.zip` uses the Vulkan renderer and works best on newer systems. Recommended for most users.
- `OmoriSandbox_Linux_Compat.zip` uses the OpenGL renderer and works best on older systems. Use this if you're having issues with the Vulkan version.

To run the Sandbox, you can either use the provided `OmoriSandbox.sh` script, or run the `OmoriSandbox.x86_64` executable directly.

## Usage
**Important Note**: As of writing, the project is currently in an "alpha" state, allowing users to try out the sandbox for themselves during active development. While battles are functional from start to finish, __many features including skills, weapons, items, enemies, etc. are missing from the current build.__ Expect bugs, glitches, or potential inaccuracies. See the [To-Dos](#To-Dos) section to see the current status of any missing features. If you happen to find a bug, please report it in the "Issues" tab.

When the Sandbox opens for the first time, it may appear to hang for a couple of seconds. This is **normal** and is simply the Sandbox loading all of its necessary files. After loading, the title screen will appear. There are four buttons available:
1. "Play" will run the currently selected battle preset.
2. "Configure" will open the visual preset configuration GUI.
3. "Open Preset Folder" will open the folder where your presets are stored. **You should only directly modify these files if you know what you're doing.**
4. "Open Mods Folder" will open the folder where your mods are stored. Modding is explained further below.

## Configuring Battles
Battles can be configured via the visual editor accessed by clicking the "Configure" button on the title screen. There are five tabs at the top of the screen, and various buttons laid out across the screen. When you are satisfied with your edits, you can enter a preset name into the input box and click `Save Preset` to save your preset. The preset will then appear in the dropdown in the main menu. Selecting your newly created preset and clicking "Play" will begin the battle! During a battle, you can select "Run" to return back to the title screen. If you click "Play" again, the battle will restart from the beginning.

Each section of the editor is explained below:

### Settings
General settings for the battle. Most of the options are self-explainatory. 

Choosing a Battleback will update the background of the editor to give you a preview of the currently selected battleback. 

The BGM "Preview" button will play the currently selected audio track in the background. Clicking the "Stop" button will stop the audio.

If `Use Basil Followups` is checked, the character in the bottom right corner will use Basil's Followups instead of Kel's.

If `Use Basil Release Energy` is checked, Omori's "Release Energy" Followup will use the Basil version instead of the Omori version.

### Items
Click the `+ Add Item` button to add an item to your inventory. This section handles both Toys and Snacks. You can use the input box to set the amount of each item that you want, or press the `X` button to remove the item from your inventory.

### Skill Search
Allows you to search for skills to put into actor skill inputs as explained below. Type your search query into the search bar and click `Search` to pull up all internal skill names that match your query. It should be noted that skill names are case-sensitive and should be input as such.

### Actors
Clicking the `+ Add Actor` button in one of the corners will create a tab in this section. All aspects of said actor can be customized in each respective named tab.

If `Disable Followups` is checked, that actor will not be able to use any followups. Useful for Real World actors.

As mentioned in the previous section, each entry in the Skills section is case-sensitive, and must be entered exactly as they appear in the Skill Search. The `Attack Skill` section is the skill that gets used whenever the `Attack` button is selected. While this can be any skill, the regular attack skills follow the format of `XAttack`, where `X` is the actor's first initial. Real World actors use the format `XRWAttack`.

For example, Omori would use `OAttack` for his attack, and Real World Aubrey would use `ARWAttack` for hers.

### Enemies
Clicking the `+ Add Enemy` button at the bottom will create a tab in this section. All aspects of said enemy can be customized in each respective named tab.

The `XPos` and `YPos` boxes dictate the screen coordinates the enemy appears at, relative to the center of their sprite. `(0, 0)` is the top left, and `(640, 480)` is the bottom right of the screen.

If `FallsOffScreen` is checked, the enemy will fall off the screen when defeated.

The `Visible` checkbox is useful for when there are multiple enemies on the screen, or when an enemy is blocking something you need to see. This checkbox has no effect in battle.

## Modding
As of update v0.8, official modding is now supported! You can read more about creating file driven, JSON, and fully fledged C# mods on the official [Modding Wiki](https://github.com/EBro912/OmoriSandbox/wiki).

If you are looking to port your custom battlebacks and BGM from an older version, you will need to create a basic "mod" in order to load these. This process is very similar to the old `/custom` folder system and requires no coding and minimal JSON configuration. See the above wiki for more info.

**Important Note:**
When it comes to loading C# mods, **OmoriSandbox does not perform any kind of sandboxing or malware checking when loading mods**, meaning a malicious actor can create a mod that may harm your system. When using C#/`.dll` driven mods, ensure that you trust the author. You can use a program such as [dnSpy](https://github.com/dnSpy/dnSpy) or [VirusTotal](https://www.virustotal.com/gui/) in order to read the mod code or check the file for viruses before loading it into OmoriSandbox.

## To-Dos
The following features are currently missing and/or not fully functional in the Sandbox, and will be periodically added through updates. This list may shrink or grow at any time depending on updates and bug reports:
### Missing Completely
- [ ] Some Screen Tint/Wave Effects
- [ ] Faraway Town Snacks and Toys
- [ ] Sales Tag, Chef's Hat, Contract, Abbi's Eye, Unused Charms
### In Progress/Partially Functional
- [ ] Boss-Specific Behavior
- [ ] Game Over
- [ ] General Refactoring and Code Improvements
- [ ] Official Mod Support/Modding API + Documentation
### Planned Changes/Additions
- [ ] Porting more enemies and boss fights
### Finished
- [X] Dots animation on the Energy Bar
- [x] Tier 2 and 3 Followups
- [x] Omori's special skills (Vertigo, Cripple, Suffocate)
- [X] In-Battle Dialogue
- [X] Any skills that perform a Taunt
- [x] Afraid and Stressed Out
- [X] Party sizes below 4
- [x] Skills that use the `<Not User>` tag
- [X] Skill/Item descriptions that use character names
- [X] Title Screen
- [X] A GUI driven config system

## Contributing
Contributions to the project are welcome! You can help contribute to the project in three main ways:
### Bug Reporting
If you find a bug or issue while using the Sandbox, please open an issue in the **Issues** tab. When opening a new issue, please keep the following in mind:
- Search for any other existing issues that may have already reported the issue you found.
- Please fill out as much as the issue template as you can, including any relevant info and screenshots/video if possible.
- Please be on the lookout for any replies to your issue that may ask for additional information.
- Ensure your issue uses the proper tags.
### Feature Requests
If there is a feature missing or not fully implemented in the Sandbox that is not listed in the **To-Dos** section, feel free to open an issue in the **Issues** tab. Similar to bug reports, please ensure that you use the proper tags and fill out the issue template as much as you can.
### Code Contributions
If you would like to contribute code to the Sandbox, you must first install the latest **.NET Version** of [Godot](https://godotengine.org/download/).

After installation, simply clone the repository and open the project folder in Godot. All of the necessary assets should already be available to you. 

If you need any other assets from the game that the Sandbox currently does not provide, you must retrieve them yourself from a valid copy of Omori.

When you are ready to submit your contribution, please open a pull request in the **Pull Requests** tab with a detailed description of what your PR accomplishes. While any contributions are welcome, PRs that target anything in the **To-Dos** section will most likely take priority. 
Please refrain from modifying anything that impacts the core functionality of the Sandbox, including logos, important filepaths, modifications of vanilla assets, and anything else that negatively impacts the goal of 100% accuracy. Any PRs that are deemed to do so will be rejected.

## Third Party Assets
The assets used by the project were obtained via a legitimate copy of Omori, and are only meant to be used as fair-use and free of charge for this project alone. You may not use the assets contained within this project for any other purpose.
This project is in no way meant to replace the original game, it is for practice, speedrunning, and educational purposes only. It is heavily recommended that you purchase and play through Omori before using this program.
If you are the owner of the aforementioned assets and would like them removed from the repository, please contact me on Discord at `alltoasters` or submit an issue in the **Issues** tab.
