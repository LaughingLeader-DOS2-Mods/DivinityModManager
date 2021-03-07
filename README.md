LaughingLeader's Divinity Mod Manager
=======

A work-in-progress mod manager for Divinity: Original Sin 2 - Definitive Edition.

# Setup

1. Run the game once if you haven't already, so a profile and the mods folders get created.
2. Make sure you have [Microsoft .NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) installed
3. [Grab the latest release.](https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/releases/latest/download/DivinityModManager_Latest.zip)
4. The Divinity Mod Manager is portable, so extract it wherever you wish.
5. Upon opening the program, pathways to the game data and exe should be automatically detected. If this fails, you can manually set the pathways in Settings -> Preferences.
6. Organize your active mods for a given profile, then click the first export button (Export Load Order to Game), or click File -> Export Order to Game, to export your active load order to the game.
 [![Exporting Load Orders](https://i.imgur.com/evJ9ulQl.jpg)](https://i.imgur.com/evJ9ulQ.png)

# Current Features:

* Reorganize mod load orders with a quick drag-and-drop interface. Allows reordering multiple mods at once.
  * View details about each mod, including the description and dependencies.
* Save your mod load orders to external json files for sharing or backing things up.
* Export your active mod order to various text formats (i.e. a spreadsheet). These formats will include extra data, such as the mod's steam workshop url, if any.
* Filter mods by name and properties (author, mode, etc.).
* Export load order mods to zip files (including editor mods), for easier sharing of a playthrough's mods between friends.
* Import load orders from save files.
* Shortcut buttons to all the various game-related folders (mods folder, workshop folder, game directory, etc).
* Dark and light theme support.

## Steam Workshop Support

* View pending Steam Workshop mod updates and update with a few clicks.
* Filter mods by Steam Workshop tags. Mods can also specify custom tags in their meta.lsx file.
* If a mod has a Steam Workshop page, you can open this by right clicking the mod and selecting "Open Steam Workshop Page".  
[![Opening a Workshop Page with Right Click](https://i.imgur.com/gs1BV0ym.jpg)](https://i.imgur.com/gs1BV0y.png)

## Script Extender Support

[Norbyte's Script Extender (ositools)](https://github.com/Norbyte/ositools) allows mods to mod the game even further, and is required by many newer mods. The Divinity Mod Manager makes this easier to install and configure with the following features:

* Install the Script Extender with a few clicks (Tools -> Download & Install the Script Extender).
* Configure Script Extender settings in Settings -> Preferences -> the Script Extender tab.
* See if mods use or require the script extender.

## Features for Mod Authors

* Extract selected mods with a few clicks. Useful for mod authors, or those wanting to study mod files for learning.
* Copy a mod's UUID or FolderName in the right click menu. Useful for if you're setting up Ext.IsModLoaded checks with the script extender, for mod support.
* You can specify custom tags in your project's meta.lsx (the "Tags" property"). Seperate tags with a semi-colon, and the mod manager will display them.


[![Custom Tags](https://i.imgur.com/bxkVqssl.jpg)](https://i.imgur.com/bxkVqss.png)

# Notes

* Divinity Engine 2 (editor) projects are highlighted in green. They can be used in the load order like regular mods, and even exported to zip files.
* New profiles must be made in-game. You should also run the game at least once, so all of the game's user folders are created.
* Highlight over mods to see their description and list of dependencies. Red dependencies are missing dependencies.

# Links

* [Latest Release](https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/releases/latest)
* [Changelog](https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/wiki/Changelog)
* [Divinity Mod Manager Discord](https://discord.gg/j5gp6MD)

# Support

If you're feeling generous, an easy way to show support is by tipping me a coffee:

[![Tip Me a Coffee](https://i.imgur.com/NkmwXff.png)](https://ko-fi.com/LaughingLeader)

All coffee goes toward fueling future and current development efforts. Thanks!

# Building From Source  
## External Libraries  
* [lslib](https://github.com/Norbyte/lslib)
* [tolk](https://github.com/dkager/tolk)

# Credits

* Thanks to [Norbyte](https://github.com/Norbyte) for creating [LSLib](https://github.com/Norbyte/lslib), which allows various features of the manager (getting data from paks, reading lsb files, just to name a few).
* [Dan Iorgulescu](https://www.artstation.com/daniorgulescu) (Concept Artist on Baldur's Gate III at Larian Studios) for the [beautiful key/box art](https://www.artstation.com/artwork/mV159) used for the app icon (Fane's head) as of 12/20/2019.
* [Divinity: Original Sin 2](http://store.steampowered.com/app/435150/Divinity_Original_Sin_2/), a wonderful game from [Larian Studios](http://larian.com/)
