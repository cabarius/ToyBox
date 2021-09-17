# About
Toy Box is a cute and playful mod with 260+ cheats, tweaks and quality of life improvements for Pathfinder: WoTR.

It was created in the spirit of Bag of Tricks & Cheat Menu but with a little different focus, It of a powerful and convenient way to edit:
- the party composition
- stats
- search and add to party members:
  - Feats
  - Features
  - Items
  - etc.

# How to contribute

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
1. Locate the install folder of Pathfinder Wrath of the Righteous
1. Go to System Properties > Environment Variables and add WrathPath with a value that looks like this:
        `WrathPath`   `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure`
1.  First time and when the game updates make sure you clean the solution to trigger the publicize step
1. build the solution debug and it will automatically build and install into the mod folder in the game
1.  when you rebuild you can go to the mod and hit the reload button at the top to make it use the latest

**PS**: 

[Learn to mod Kingmaker Games here here](https://github.com/spacehamster/OwlcatModdingWiki/wiki/Beginner-Guide)
        
[Join our discord](https://discord.gg/bQVwsP7cky)
