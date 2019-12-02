# 1.2.0.0

## Auto-Updating

DivinityModManager can now auto-update! This is a little bit experimental, but it should hopefully facilitate an easier time with updating.

## Mod Updater Panel

If the DOS2 Steam Workshop folder is found (or set), the mod manager will check the current paks within that folder with your paks in your mods folder. Pending updates will display on the updater button, and you can click on it to open the new update panel, for automatically copying in new updates.

## Automatic Pathways

The app will now try and determine the location of the game data, executable, and workshop folder. Failing that, these pathways can be set manually.

## New Buttons

### Refresh

Refreshes the UI, reloading all mods, profiles, load orders, and workshop updates.

### Shortcuts

* Open Mod Folder
* Open Steam Workshop Folder
* Open Divinity: Original Sin 2 - Definitive Edition

### Support

* Coffee Donation Link
* Github Repostiry Link

## New Preferences (Settings -> Preferences)

New settings have been added to the preferences window. You'll need to open the program before you see these in the setting.json file.

### General

#### Game Executable Path

The path to EoCApp.exe. For using a new shortcut button to launch the game.

#### DOS2 Workshop Path

The Steam DOS2 workshop directory. Used to check for mod updates.

#### Enable Story Log

When launching Divinity: Original Sin 2 - Definitive Edition through the shortcut, this will enable the story log.

### Debug

#### Enable Logging

The automatic logs can now be enabled/disabled at will (defaults to off).