# Brief Description
Toy Box is a cute and playful mod with 300+ cheats, tweaks and quality of life improvements for Pathfinder: WoTR. It was created in the spirit of Bag of Tricks & Cheat Menu but with a little different focus . It of a powerful and convenient way to edit the party composition, stats, search and add Feats, Features, Items, etc. to party members.

# Detailed Description

Now with 300+ Cheats, Tweaks and Quality of Life Improvements
    Tweaks: 98 (or 139 depending on how you count)
    Level Up & Multiclass: 34
    Party Editor: 67    
    Search 'n Pick: 75 ways to view, add, remove blueprints plus a fun global teleportation feature
    Crusade: 25
    Quest Resolution: 1
    
Ver 1.3.12
    Fixed issue where feat multiplier stopped working for certain cases such as mythic selections
    Renamed Feat Multiplier to Feature Selection Multiplier and added green explainer text to make it clear what this does
    (Truinto) Added Disable Dialog Restrictions (Mythic Path)
    (xyq565861)Add feature to unlock the upper limit of every class level to 40 in level up page.
Ver 1.3.11
    Found a grey that works for trash loot. The previous brown still looked too much like a meaningful loot color
    (ArcaneTrixter) Added a 'Brutal Unfair Difficulty' to Quality of Life. Toggle on if you miss the previous challenge of Unfair or if you thought it too easy there's a slider now.
    (ArcaneTrixter) Modified 'Disable Arcane Spell Failure' to set spell failure to 0 so you can Blink freely.
    (ArcaneTrixter) No longer squaring the multiplier from 'Spells Per Day' when applying to spellbooks.
    (Vek17) Fixed issues around the Feat Multiplier including
        - All story companions feats/backgrounds/etc. most notably a certain wizard who unlearns how to cast spells if your multiplier is at least 8. Also this is retroactive if you ever level up in the future with the multiplier on.
        - Messed up All mythic 'fake' companions like Skeleton Minion for lich or Azata summon.
        - Caused certain gestalt combinations to give sudden ridiculous level-ups of companions or sneak attack or kinetic blast.
Ver 1.3.10
    Added new top level tab: "Loot"
        Moved Loot Coloring Settings to Loot Tab
        With the kind permission and help from the legend himself, @Hambeard, ToyBox now supports a new version of TheLootCheckList
    Loot color tweaks: notable is now yellow and trash is now an appropriate sh_t brown color
    Hot Key Binding control now scales properly to Unity Mod Manager UI scale setting.
    (ArcaneTrixter) Added 'Ignore Prerequisite Features (like Race) when choosing Class' to levelup.
    (ArcaneTrixter) Bumped max burn for kineticist burn cheat to 30 so you never have to be burned again.
    (ArcaneTrixter) Reorganized stat section of party editor to not just be the random enum order.
    (ArcaneTrixter) Allowed multiple judgements to be active at once.
    (ArcaneTrixter) Added toggle to remove Level 20 Caster Level cap.
    (ArcaneTrixter) Added ability to merge standalone mythics into any spellbook. Spell slots and spells per day are still based on the original type, e.g. Magus gets 6th level spells and below.
    (ArcaneTrixter) Added Add All button to spellbooks when browsing new spells. It works with Search All Spellbooks and respects current search results.
    (Delth) hopefully fixed gestalt skillpoint calculations
Ver 1.3.9
    Made Alignment section in Bag of Tricks
        Fixing alignment shifts for neutral good and similar alignments
        Toggle allowing you to Prevent alignment shifts
    Added per character alignment locking in Party Editor > Char > Stats
    Books and other items that have info triggers are flagged as notable and show a circle around it
    Changed coloring for notable items to be a distinct lime green
    (ArcaneTrixter) Added mythic spellbooks to the new spellbook management section of Party Editor.
    (ArcaneTrixter) Added 'Ignore Alignment Requirement for Abilities' to Cheats menu for those struggling paladins and others.
    Note:  Freemantr80  mentioned in the nexus forums - Regarding romance, I've played twice now with 3 romances to the end. All you need to do is change the FLAG romance count value to 1 before coming back from abyss, and you won't get harassed by your lovers over having multiple partners.)  We are planning on better romance support including proper polyamory and same sex but are working on some needed infrastructure improvements to provide this and more
Ver 1.3.8
    Loot Colors and Filters
        ToyBox gives WoTR a Diablo 2/Borderlands style loot rarity and coloring system including
        Checkbox to have the game color your loot according to rarity in inventory, containers, etc.
    "Identify All" added to Bag of Tricks > Common
    Added quality of life tweak to let you shift click to use items in your inventory
    Split Tweaks into Quality of Life and Cheats to make it less huge
    New categories for collating items by rarity, cost, enchant value
    Search 'n Pick now defaults to searching descriptions. Untick the checkbox to disable this
    Renamed the tweak 'Enable multiple romance (experimental)' to 'Disable Romance IsLocked Flag (experimental)' which is more accurate because we don't know what this does but it doesn't unlock multiple romances. That is under investigation.
    (ArcaneTrixter) Added support for Legendary Heroes throughout Toybox, made fake legendary flag per character.
    (ArcaneTrixter) Added toggles for immunity to negative levels and ability drain.
    (ArcaneTrixter) Revised + added functionality to spellbook tab in party editor. Can now add/remove from specific spellbooks, and can select spells from any spellbook based on toggle.
    (Delth) Explicitly removed certain negative buffs (Prone, Fatigued) that the game does not mark as harmful from buff duration multiplier
    (Truinto) Added tweak: Auto Start Rage When Entering Combat
    (Truinto) Added tweak: Mass Loot Shows Everything When Leaving Map
    (Truinto) Added tweak: Equipment No Weight
    (Truinto) Added tweak: Allow Item Use From Inventory During Combat
    (Truinto) Added tweak: Disallow Companions Leaving Party
    (Vikash) Fixed Respec Code, Can now Respec companions even if they are not in party.
Ver 1.3.7
    You can now add key binds to cheat buttons like "Rest All", "Full Bufs", "Reroll Perception", etc 
    HotKeys now recognize shift, alt, ctrl, alt and command key combos
    Teleport Party Key now works on Global or City Map
    Search 'n Pick - You can now Unstart Etudes which can help fix broken progression
    Added search 'n pick categories for Dialog, Cues and Answers
    Movement speed multiplier now affects when you interact with a map object or npc
    (ArcaneTrixter) Added option to use legendary hero leveling as non-legends.
    (ArcaneTrixter) Added a number of things around caster levels and spellbooks, including a +1/-1 caster level for any spellbook - Experimental
Ver 1.3.6
    Fixed party speed multipliers to work with latest update of the game
    (ArcaneTrixter) Added requested multiplier for Arcanist spell slots.
Ver 1.3.5
    Toggling search descriptions now updates search results
    No friendly fire toggle should work better with pets
    (ArcaneTrixter, Narria) Allies should no longer get piles of (sometimes contradictory) starting feats when initially recruited (Looking at you, Nenio!).
    (ArcaneTrixter, Narria) Party and pets should once again be affected by feat multiplier in town.
    (ArcaneTrixter) Added requested flag to allow Magus Spell Combat even in situations where their hands are full.
Ver 1.3.4
    Teleport Keys are now rebindable and shouldn't interfere with other windows that you might type in
    Cleaned up the UI for parameterized feats. Values now are sorted and the layout is improved
    Got rid of Max FoV and increased standard max to 5
    (ArcaneTrixter) Added multipliers for Army Leader Power. Remember that power is squared for most abilities!
    (ArcaneTrixter) Fixed enemy HP multiplier slider
    (SonicZentropy) Added ability to spawn new Crusade armies from Search n Pick and squad editor in Crusade tab
Ver 1.3.3
    Added separate zoom multiplier for cut scenes
    Increased MaxFov to 5
    Toggle to not charge campaign time when changing characters
    (ArcaneTrixter & Narria) Added button to show the group editor anywhere
    (ArcaneTrixter) Added an experimental "Large Player Armies" toggle to the Crusade tab, which will enable players to have up to 14 units in their army. This might have unintended side effects if reloading a game with large armies without this setting enabled.
    (ArcaneTrixter) Added Kineticist class cheats to gather power with hands full and to allow additional burn reduction.
    (ArcaneTrixter) Enabled initial parameterized feat support.
    (ArcaneTrixter) Added a toggle for teleport keys being active in the Tweaks section of Bag of Tricks.
    (Truinto) Witch/Shaman: Cackling/Shanting Extends Hexes By 10 min (out of combat)
    (Truinto) Allow Simultaneous Activatable Abilities  (like judgements)
Ver 1.3.2
    Enemy armies no longer speed up with movement speed multiplier. Only your armies become speedsters ^_^
    Initial teleport to cursor for party and main character support.  Use comma ',' to teleport the party and '.' to teleport the main char
    Move party together is now disabled for turn based combat (it broke it before)
    (ArcaneTrixter) Moved Crusade stuff to a new Crusade tab (more like army editing coming soon)
Ver 1.3.1.2
    Fixed crash on loading into a game that can sometimes happen due to multiclass config being set to a party member higher than the number available in the game you are loading.
Ver 1.3.1.1
    Fixed slow movement speed when movement modifiers are off or when move as one is turned on with no change to speed multiplier
Ver 1.3.1
    You can now view, lock, unlock, increment and decrement UnlockableFlags in Search 'n Pick - be careful as there are 1400+ of these and we don't know what most of them do.
    (ArcaneTrixter) Added sliders for increased recruitment amounts and modifiable recruitment costs
    (ArcaneTrixter) Added max dex and arcane spell failure toggles requested in issue #106
    (ArcaneTrixter) Added a multiplier ('After Army Battle Raise Multiplier') for the number of units raised by Lich's Necromancy and similar effects. This code only gets triggered when players win, so shouldn't have any additional side effects.
    (ArcaneTrixter) This increases the speed at which generals level up and the amount of units spawned by features like Necromancy after battles. I pegged it to the existing experience multiplier to be consistent, but it could be its own multiplier if others prefer.
Ver 1.3.0
    Multiple Classes On Level Up (Gestalting) is live.
        This is a complex new feature so it is still marked experimental
        Note that configuration such as which classes you will multiclass on level up has been reset. 
        Any past results of multiclassing will remain intact however.
    Cleaned up Multiclass UI for release
    Archetypes now work in char gen and fixed other bugs with multiclass
    New tweak in Level up to let you hit next on any feature selector that runs out of feats. 
        This is activated automatically if feat selection multiplier is greater than 1
    Additional tweak to let you skip any feat selection you wish
    Improved run speed multiplier to work when moving and now when looting chests
    Fixed 'Whole Team Moves At Same Speed' to only happen when it is ticked.
    Moved 'Whole Team Moves At Same Speed' tick box to be next to the run speed multiplier slider
    Added Time Scale Multiplier which affects the whole game
    Characters you add to the party via party editor now are fully controllable and viewable (can't add non companions permanently to party yet but working on it)    
    Made adding characters to party cease to be fighting and aggressive
    Blueprints now load on the mac so you can now fully use the mod
Ver 1.2.13
    Added sliders for Crusade Morale, Max Morale and Min Morale (more improvements to crusade are on their way)
    Tweak to refill belt consumables from your inventory
    Toggle in Search 'n Pick to let you search descriptions
    Reduced log spam which can fill up Player.log which can grow huge so you may want to quit and delete the file from time to time.  It lives in $User\AppData\LocalLow\Owlcat Games\Pathfinder Wrath of the Righteous\
Ver 1.2.12
    Increased max buff duration multiplier to 999 to match original Bag of Tricks
    Quest editor now sorts completed quests below active
    Pets can now multiclass if you turn on ignore class and feat restrictions so make that bully thug or barbarian wardog!
    (Fire) Tweak to block tutorials if you have them turned off in settings
    (Delth) Added flag to possibly enable multiple romances (experimental and untested, volunteers welcome)
    (Delth) Spiders Begone ported from Bag of Tricks! Spider looks changed to wolves, spider swarm looks to rat swarms. Experimental.
Ver 1.2.11
    Unified gestalt flag in party editor with gestalt config so if you level up with a chosen set of multiclasses it will mark them as gestalt.  If you mark a class as gestalt it will get added to the multiclass set.
    Companions can now multiclass
    Fixed calculation error in multiclass skill points - note if you choose a gestalt class to level up as primary then it treats it as if you did not gain a character level
    Your dear pets can once again get more feats with feat selection multiplier
    Fixed gestalt level up rules for hp/bab/saves/skills and crasher affecting respec mods
    Added a URL describing gestalt classes in Level Up & Multiclass section
    Multiclass skill point calculation no longer gets used when multiclass is disabled
    (Delth) Fixed roll1/roll20 on initative to actually set result of the roll, not whole iniative result
    (Delth) "All hits critical" was in fact "All attacks hit and are critical" - split into two separate options
Ver 1.2.10
    Added feature to exclude classes in character level calculation, which helps gestalt class tinkering
    Search 'n Pick - Update match count when you change collation category
    Fixed Performance issue caused by showing combat challenge rating in party editor so removed it for now
    Raised max caster level to 20 for spellbook calculations (thanks Delth)
    Made buff length multiplier not apply to harmful buffs (thanks Delth)
Ver 1.2.9.1
    Restored some missing patches I forgot to put back in when working on 1.2.9
Ver 1.2.9
    Renamed 'Cheap Tricks' Tab to 'Bag of Tricks' in honor of m0nster and the great mod that helped make ToyBox what it is today
    Fixed bug affecting turn based commands like Studied Target or Gather Power
Ver 1.2.8
    Fixed bug that would spawn enemies more powerful than they should
    Tweak to allow you to control summons, 
    Adjust summon duration and modify level for 2 different unit factions such as your party and enemies
    Added ability to view (not modify yet) Archetypes and Parametrized Features to Search 'n Pick
    Added party stats and encounterCR (when in a fight) to party editor
Ver 1.2.7
    Fixed a bug that broke feat selection on level up
    Fixed bugs with multiclassing and feat selection (experimental version)
Ver 1.2.6
    Added ability to see and set your experience to match your current character level 
        See Party Editor > Character > Classes
    Added support for Roll Only 20s out of combat
    Made scribable scrolls show up in containers or corpses in addition to inventory and vendors
    Got rid of html tags from descriptions
Ver 1.2.5
    Added tweak to highlight scrolls that can be copied into your spell book (or recipes)
    Improved stability of fov (zoom multiplier) when you disable and renable the mod multiple times
    Added experimental slider to increase max FoV multiplier.  This can cause perf issues in some maps
    Fixed out of range bug in dialog preview
    Released experimental version of 1.2.5 with multiple classes per level turned on (please download the experimental file to try this out and file bugs here: https://github.com/cabarius/ToyBox/issues)
Ver 1.2.4
    Added Field of View Multiplier to increase zoom range or even flip it (try a FoV Multiplier of 0.4)
    Fixed bug in buff duration multiplier
Ver 1.2.3
    Recompiled for game release
    Added tweak to allow achievements for modded games
Ver 1.2.2
    Ported to Beta 3

Ver 1.2.1
    Fixed disappearing doll in inventory screen and map refresh bugs due to NoFogOfWar flag

Ver 1.2.0
    Ported to Beta 2
    UI improvements
    - More beautiful check-boxes and disclosure controls
    - Much better support for mid and lower resolution screens
    Tweaks
    - Armies also are affected by Travel Speed Multiplier
    Level Up
    - Ignore Attribute and Skill Caps
    - Ignore Remaining Attribute and Skill Points
    Teleportation
    - Instant travel to global or city map (Seach 'n Pick > Global Map)
    - Teleport to points on global map (Search 'n Pick > Map Points)
    Other Search 'n Pick Improvements
    - More categories have sub categories
    - Improved layout of results
    - Start and complete Etudes (Experimental)
    - Start and complete quests, quest objectives (Experimental)

Ver 1.1.12
    Search 'n Pick
        Teleport to GlobalMap (World or Kenabres)
        Teleport to different global map points
        Start and complete Etudes
        Can not start Cut Scenes which have various fun effects
Ver 1.1.11
    Getting ready for 1.2.0 but doing a pre-release as 1.1.11
    Moved Level Up related tweaks to a new tab called 'Level Up'
    Pick & Search UI has been revamped
        Categories are now in a column on the left
        Most categories have a subtype filter
        Areas can now be teleported directly. 
        Added some useful filters for Areas too
        Toggles for showing components and elements
        You can now add BlueprintActivatableAbility
        Added category for browsing Wrath's in memory cache
    Experimental Multiclass on level up preview (experimental binary only)
        This is very limited right now as it only gives you the classes
        It does not let you choose feats, abilities, spells
        If you want to check it out please download the side experimental version from nexus
    Various other small improvements

Ver 1.1.10
    Tweak for autoloading last save on app launch
    Improved layout for mod window sizes < 2000 and < 1600
    Reverted mouse wheel patch that broke horizontal scrolling

Ver 1.1.9
    Kingdom Resource Editing (Finances, Materials, Divine Favors)
    Selector to allow you to move through other units during combat (real-time and turn based)
    Infinite Abilities and Spell Casts
    Disable Fog of War
    When you enable 'Show GUIDs' in Search 'n Pick they can now be copied to the clipboard
    Multiplier Sliders now give you a text field to edit the value
    Multiplier Sliders should work better for adjusting for small and big values
    Fixed bug with item sell price multiplier where it was selling for too much when multiplier is 1
    Mouse Wheel scrolling should work reliably now

Ver 1.1.8
    Added Dice Roll Tweaks to Cheap Tricks
    Added friendly (Non Hostile) as a category to which you can apply tweaks
    Fixed bug where mercenaries where getting main character build points instead of mercenary

ver 1.1.7
    Added Attack of Opportunity Disable to Cheap Tricks
    Can now choose Alignment in Party Editor > Stats
    Can now choose Size in Party Editor > Stats
    Added Item and Ingrediant as options in Search 'n Pick
    In search and pick some actions (add/remove item and spawn) now support a parameter do it up to 100 times
    Improved legibility of colors
    Fixed bug with movement speed multiplier where it made the main character go slow with moveAsOne off

Ver 1.1.6
    Ignore Class And Feat Restrictions now allows you to choose any mythic class each time you level up starting from level 1
Ver 1.1.5
    Fixed bug with meta magic where if Free Meta Magic is on it doubles the metamagic and if off it hides it
    (I fixed this before but it was lost somewhere along the way)
Ver 1.1.3 (and 1.1.4)
    Party Editor > Classes
        Can now adjust character level, mythic level, and indivual class level without triggering level up 
        Reset character level to current xp
        Added text field for direct stat editing
    Cheap Tricks
        Gold and party experience increase
        Added Cheap Trick for Unlocking All Mythic Paths
        Increased max feat multiplier to 10
    Search 'n Pick
        Added a fun teleport feature to blueprint browser (Entr Points)
        Added more filter categories
    Misc
        Added divider lines (optional) to make it easier to look through lists of blueprints, features, abilities, spells, etc
        Improved look of checkboxes (got rid of distracting red x and made them grey)
        Improved formating of spellbook names and avoid blank names where possible
    Fixed issue during level up where the game might demand you pick a feature (feat, boon, etc) from a list where nothing was available.  This makes feat multiplier work in many cases where it didn't even in bag of tricks.  
    Fixed issue where movement speed multiplier was not being applied correctly
    Fixed bug that was preventing bard/azata build from spending resource points
    Fixed issue that prevented adding 9th level spells

Ver 1.1.2
    Fixed bug where you could only toggle to show stats/facts/abilities/etc for main character even though you selected a toggle on another
    Fixed bug that broke abilities and cooldowns during combat
    Added bonus feature 'Unlimited Actions During Turn' for Turn Based Combat
    Tweaks to party editor UI and labels
Ver 1.1.1
    Fixed Party Editor bug where it would get out of sync with the party, especially when loading other saves
    Added Nearby Units and show distance to other units
Ver 1.1.0
    Cheap Tricks
        Ported ~3 dozen flags, multipliers, etc patches from Bag of Tricks (see below for full list)
    Search 'n Pick 
        New things to browse: Abilities, Spellbooks, Many specific equipment types
        Can now Add/Remove abilities, spells, spellbooks
    Party Editor
        Can now browse and add blueprints in the party editor for various toggles (Show All)
        Toggles to show classes, buffs, abilities and spellbook and can edit all but classes
        Toggles in party editor now close other toggles
        Show counts of some toggles like classes and spells
        Can see Friendlies, Enemies and All Units!
    Performance
        Switched blueprint loading to async (no more freezing of app when you do first searcy)
        Loading indicator on startup
        Massively improved performance on Party Editor and Search 'n Pick lists
    Cleaned up UI and bug fixes
    Ported BoT Features - this is experimental so please report bugs https://github.com/cabarius/ToyBox/issues
        Flags
            Whole Team Moves Same Speed
            Instant Cooldown
            Spontaneous Caster Scroll Copy
            Disable Equipment Restrictions
            Disable Dialog Restrictions
            Infinite Charges On Items
            No Friendly Fire On AOEs
            Free Meta-Magic
            No Material Components
            Instant Rest After Combat
        Multipliers 
            Experience
            Money Earned
            Sell Price
            Encumberance
            Spells Per Day
            Movement Speed
            Travel Speed
            Companion Cost
            Enemy HP Multiplier
            Buff Duration
        Level Up
            Feats Multiplier
            Always Able To Level Up
            Add Full Hit Die Value
            Ignore Class And Feat Restrictions
            Ignore Prerequisites When Choosing A Feat
            Ignore Caster Type And Spell Level Restrictions
            Ignore Forbidden Archetypes
            Ignore Required Stat Values
            Ignore Alignment When Choosing A Class
            Skip Spell Selection
Ver 1.0.5
    Ported Dialog Preview from Kingdom Resolution Mod. Now you can get a preview of results from Dialog, Alignment Restricted Dialog, Events and Random Encounters
Ver 1.0.6
    Major Overhaul of the UI.  Each major area of features is in a separate tab (Cheap Tricks, Party Editor, Blueprint Search, Quest Resolution)
    Added experimental Respec Feature
    Fixed bug where the arrows were backwards in add/remove rank and also made sure it couldn't take you below 1
    Various stability improvements 
Ver 1.0.4
    Search Character Picker - Can now add features to a specifically chosen party member
    Quest Resolution - browse and modify progress in your quests (great for dealing with bugged quests)
    Improved layout at lower resolutions (not perfect yet)
    Improved search performance
    Misc other improvements
Ver 1.0.3
    Party picker now lets you browse Party, Party & Pets, All Characters, Active Companions. Remote Companions, Mercs, Pets
    Add/Remove party members
    Teleport Party To You
    Run Perception Check
    ToggleTabHighlightsMode is ported from Spacehamster's awesome Kingdom Resolution Mod for Kingmaker https://www.nexusmods.com/pathfinderkingmaker/mods/36 based on code originally by fireundubh
Ver 1.0.2
    Browse and  remove features by party member (back up before using)
    Various cheats based on console commands
Ver 1.0.0
    Search and entire blueprint catalog for feats, features, items and more
    Browse party members, level up, mythic level up, modify stats


To use search type a string into the search field at the bottom and hit enter or click the search button.  At first you have to wait 10 seconds or so for the blueprints to load but after that, it is fast.  You choose a category with the provided toolbar.

Search results will offer you actions such as adding a feature or item.

Install & Use
    - Install the Unity Mod Manager﻿.
    - Install the mod using the Unity Mod Manager﻿ or extract the archive to your game's mod folder (e.g. '\Steam\steamapps\common\Pathfinder Second Adventure\Mods').
    - Start the game and load a save or start a new save (the mod's functions can't accessed from the main menu).
    - Open the Unity Mod Manager﻿ by pressing CTRL + F10.
    - Adjust the settings in the mod's menu
Acknowledgments:  
    ArcaneTrixter for many awesome improvements and bug fixes
    fire & m0nster for lots of awesome code from bag of tricks
    Truinto, Delth, Aphelion, fire for great contributions to the ToyBox project
    Owlcat Games - for making fun and amazing games
    Paizo - for carrying the D20 3.5 torch
    Pathfinder Wrath of The Righteous Discord channel members
        @Spacehamster - awesome tutorials and taking time to teach me modding WoTR, and letting me port stuff from Kingdom Resolution Mod
        @m0nster - for giving me permission to port stuff from Back of Tricks
        @hambeard - for permission to port the great KingMaker mod, TheLootCheckList
        @Vek17, @Bubbles, @Balkoth, @swizzlewizzle and the rest of our great Discord modding community - help, moral support and just general awesomeness
    @m0nster, @Hsinyu, @fireundubh for Bag of Tricks which inspired me to get into modding WoTR because I missed this mod so much

    PS: Learn to mod Kingmaker Games here https://github.com/spacehamster/OwlcatModdingWiki/wiki/Beginner-Guide 
        Join our discord https://discord.gg/bQVwsP7cky

Report Bugs: https://github.com/cabarius/ToyBox/issues
Source Code: https://github.com/cabarius/ToyBox

Development Setup
    1 - Install ToyBox mod into your game via Unity Mod Manager
    2 - Clone the git repo
    3 - Locate the install folder of Pathfinder Wrath of the Righteous
    4 - Go to System Properties > Environment Variables and add WrathPath with a value that looks like this:
        WrathPath   C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure
    5 - First time and when the game updates make sure you clean the solution to trigger the publicize step
    6 - build the solution debug and it will automatically build and install into the mod folder in the game
    7 - when you rebuild you can go to the mod and hit the reload button at the top to make it use the latest

License: MIT

Copyright <2021> Narria (github user Cabarius)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
