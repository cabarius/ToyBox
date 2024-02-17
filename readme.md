# ToyBox

### Install & Setup (Rogue)

1. Download the ToyBox mod file and unzip
1. If the folder is not already named 0ToyBox0 please rename it to that
1. Launch the game at least once.
1. **Please note that the game comes with its own built in Unity Mod Manager so you do not need to install another one**
1. Navigate to %userprofile%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\UnityModManager\
1. An example path is C:\Users\PC\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\UnityModManager\
1. Copy 0ToyBox0 into the UnityModManagerFolder
1. Launch Rogue Trader and you may need to hit ctrl+F10 to see the mod manager window
1. Load a save or start a new game to get the most out of of the mod

### Install & Setup (Wrath)

1. Install the Unity Mod Manager﻿﻿.
1. Install the mod using the Unity Mod Manager﻿ or extract the archive to your game's mod folder (e.g. '
   \Steam\steamapps\common\Pathfinder Second Adventure\Mods').
1. Start the game and load a save or start a new save (the mod's functions can't accessed from the main menu).
1. Open the Unity Mod Manager﻿ by pressing CTRL + F10.
1. Adjust the settings in the mod's menu
1. Important this mod is designed to be run at 1920x1080 or higher.
1. Please set your resolution to at least 1920x1080
1. Go to Settings tab on Unity Mod Manager to set your screen width to at least 1920 wide

* Warning: This is an early version of ToyBox for Rogue Trader. Save early and often.
* Note: Not all features are functional at this time. The ToyBox team is working hard to get as much working as fast as
  possible

### Usage

* **Bag of Tricks**: this is a collection of quality of life, quick cheats, settings, multipliers, etc from the awesome
  Kingmaker mod of the same name plus a bag or two of new tricks ^_^
* **Level Up & Multiclass**: a variety of character creation, level up, unlock mythic paths plus support for multiple
  classes per level up and gestalt gameplay
* **Party Editor**: lets you edit almost any aspect of your character. Make sure you explore all the different
  disclosure toggles. You can edit classes, stats, facts (feats and more), buffs, abilities, spells and spellbooks as
  well as the composition of your party
* **Loot Coloring & Checklist**: this lets you enable a loot grading and coloring system similar to Borderlands or
  Diablo. It also gives you a screen where view all the items in an area that you have not looted yet.
  Enchantment: allows you to add or remove enchantments from the items in your inventory
* **Search 'n Pick**: this lets you search through all the available resources (items, feats, abilities, spells and many
  more) and manipulate your game state in an almost limitless set of ways. You can add/remove items, feats, abilities,
  etc. You can spawn any unit. You can start/unstart/complete etudes, quests, etc. You can teleport to any area in the
  game. It is almost unimaginable how much you can do in here so keep digging!
* **Etudes**: this is a new and exciting feature that allows you to see for the first time the structure and some basic
  relationships of Etudes and other Elements that control the progression of your game story. Etudes are hierarchical in
  structure and additionally contain a set of Elements that can both conditions to check and actions to execute when the
  etude is started. As you browe you will notice there is a disclosure triangle next to the name which will show the
  children of the Etude. Etudes that have Elements will offer a second disclosure triangle next to the status that will
  show them to you.
  WARNING: this tool can both miraculously fix your broken progression or it can break it even further. Save and back up
  your save before using. Remember that "with great power comes great responsibility"
* **Quest Resolution**: this allows you to view your active quests and advance them as needed to work around bugs or
  skip quests you don't want to do. Be warned this may break your game progression if used carelessly.

Find it on Nexus: [nexusmods.com](https://www.nexusmods.com/warhammer40kroguetrader/mods/1)

There are preview builds available on the official [Owlcat Discord](https://discord.gg/Owlcat) pinned in the [#mod-user-general](https://discord.com/channels/645948717400064030/815735034514112512) channel

# How to contribute
- Make sure you have Visual Studio 22 (or current) installed and other tools you might want.  See [WotR Modding Beginners Guide](https://github.com/WittleWolfie/OwlcatModdingWiki/wiki/Beginner-Guide) for more info
- on the [main repository page](https://github.com/cabarius/ToyBox) click on "fork" in the upper-right corner
![alt text](./documentation-assets/github-fork.jpg "github fork button position")
- on your personal fork of the repository clone to your computer ([multiple method avaiable, chose the one you prefeer](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository))
![alt text](./documentation-assets/github-clone-1.jpg "github code button position")
![alt text](./documentation-assets/github-clone-2.jpg "github code button preview")
- create a branch in your fork with a short description of your contribution (e.g. `git checkout -b my-contribution-descriptor`)

![alt text](./documentation-assets/github-new-branch.jpg "git checkout -b example")
- do your magic with the code
- push to your fork
- open a pull-request from your fork to the main repository

# Development Setup
1. Install ToyBox mod into your game via Unity Mod Manager
1. Clone the git repo
1. Build the solution twice and restart Visual Studio
1. Now you can just change things and build the solution and it will automatically build and install into the mod folder in the game
1. **Important** If you are adding a feature or fixing a bug please add a release note entry to the *ReadMe.md*.  This is how we tell our the world about your great work^_^
    1. Find the highest release name near the top. It will look something like `### Ver 1.3.7 (Coming Soon)`. 
    1. Usually it is 0.0.1 higher than the current release but please check on Nexus.  If not please add a new Version entry at the top for the next release using proper markdown. 
    1. Don't worry too much about getting this right as Narria Cabarius will clean this up on merge so when in doubt add a new version.
    1. format your release note like like this:
    >```* (Jane/JoeCoolCollaborator) added a Coolness Multiplier in Bag of Tricks that makes your main character that much more awesome!```

**PS**: 

Learn to mod Owlcat Games (Kingmaker and Wrath of the Righteous) [here](https://github.com/spacehamster/OwlcatModdingWiki/wiki/Beginner-Guide)
        
[Join our discord](https://discord.gg/owlcat) and go to #mod-dev-technical and #mod-user-general meet the mod authors: **Narria** and her kind collaborators **ArcaneTrixter**, **Aphelion (SonicZentropy)**, **Delth** and others.  We love to chat about modding and teaching others to mod.
