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

* Warning: ToyBox for Warhammer 40,000: Rogue Trader is a complex mod. Save early and often.
* Note: If you find non-functioning features, please report them.

### Usage
Here is a summarized list of features. This list only includes a part of the features contained in the mod.

-  **Bag of Tricks**: 
    * Allow both male and female RTs to romance Heinrix and Cassia
    * Allow Remote Companions to join into dialog
    * Modify Faction Reputation, Navigator's Insight, Scrap and more
    * Experience Multiplier
    * Dice Roll Cheats/Options
    * Enable Achievements with Mods installed
- **Level Up**: Make Respec start from Level 0 for you and Companions
- **Party Editor**: Modify your own conviction and/or stats or features or portraits or voices of party members or Respec Characters for free
- **Colonies**: Modify different Colony Resources
- **Search 'n Pick**: this lets you search through various available things (items, feats, abilities, unity and locations) and spawn/give/summon them
  and manipulate your game state in an almost limitless set of ways. You can add/remove items, feats, abilities,
  etc. You can spawn any unit. You can start/unstart/complete etudes, quests, etc. You can teleport to any area in the
  game. It is almost unimaginable how much you can do in here so keep digging!
- **Etudes**: this is a new and exciting feature that allows you to see for the first time the structure and some basic
  relationships of Etudes and other Elements that control the progression of your game story. Etudes are hierarchical in
  structure and additionally contain a set of Elements that can both conditions to check and actions to execute when the
  etude is started. As you browe you will notice there is a disclosure triangle next to the name which will show the
  children of the Etude. Etudes that have Elements will offer a second disclosure triangle next to the status that will
  show them to you.
  WARNING: this tool can both miraculously fix your broken progression or it can break it even further. Save and back up
  your save before using. Remember that "with great power comes great responsibility"
- **Quest Resolution**: this allows you to view your active quests and advance them as needed to work around bugs or
  skip quests you don't want to do. Be warned this may break your game progression if used carelessly.

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
