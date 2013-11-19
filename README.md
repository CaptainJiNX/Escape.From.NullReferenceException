Escape.From.NullReferenceException
==================================

You need to put your api key in the bin/debug folder of the ConsoleApp.exe to make it run...

User manual
-----------

When the game starts it will try to initialize a party with 3 players called "JiNX the first", "JiNX the second" and "JiNX the third". If your party already contains any player with that name, it will find it and use it. If any other players are in the party they will be deleted...
If you happen to die, you can re-initialize the party with the **M-key**.
To switch between the different players, you can press **1,2** or **3** on the keyboard.

Wield your weapon with **V** and put on your armor with **R**.
To move around in the dungeon, use the **arrow keys** or **A,S,D** and **W**. To move diagonally, use **Q,E,Z** and **C**. To move down stairs, press **N**, and to move up stairs, press **U**. Your player will automatically attack anything if you move into it. If your health is low you can quaff any potion with **F**, but it is much quicker to press **H** for quick heal. To pick upp stuff, move on to it and press **P**. Drop anything fromyour inventory with **O**. Any rings will affect you just by being in your inventory.

Any time you get a popup window with digits (like when wielding or equipping), just press the digit to select the row.

...now the fun stuff. To use commands in this client, press **- (minus)** and type the command. The following commands are available:

- **findup** (short **fu**)<br>Finds the position for the stairs up, and sets that as the goal for the current player.
- **finddown** (short **fd**)<br>Finds the position for the stairs down, and sets that as the goal for the current player.
- **setgoal** (short **sg**) *{x,y}*<br>Sets a position on the map as the goal for the current player. Coordinates can be specified as an argument, or if no argument is present, select a position from the map with the arrow keys and enter.
- **findpath** (short **fp**) *{x1,y1 {x2,y2}}*<br>Display a path between two positions. Specify arguments or select from map with arrow keys and enter.
- **highscores** (short **hs**)<br>Displays the wall of fame! Get up there!

When the player has a goal set, you can press the **X** key to automatically move towards the goal. If you drink a *Gaseous Potion* you may move through walls, and reach your goal quicker. When moving this way you can set a couple of different modes. Press **0** to toggle **attack mode** and **9** to toggle **pvp mode**. These modes are only useful in combination with moving towards a goal, or scouting (**Y**). When in attack mode, player will attack any visible monsters. In pvp mode, the player will attack any visible enemy. In both attack and pvp mode, the player will also move to pick up healing potions if visible (and enough space in inventory). This is because in any of these modes, the player will automatically heal when HP is dropping below 50%. :)

Available keys
--------------

Key|Action
:--|:-----
**A,S,D,W (or arrow keys)** | Move around
**Q,E,Z,C** | Move diagonally
**X** | Move towards goal (if any)
**Y** | Scout (or moves to random position)
**U** | Move up stairs
**N** | Move down stairs
**P** | Pick up item
**R** | Equip armor
**T** | Unequip armor
**V** | Wield weapon
**B** | Unwield weapon
**F** | Quaff potion
**G** | Quick gaseous potion
**H** | Quick heal
**0** | Toggle "attack monsters" on/off
**9** | Toggle "attack players" on/off
**+ (plus)** | Increase attribute after a level up
**J** | Planeshift (untested)
**I** | List visible items
**K** | List visible entities (monsters and players)
**1** | Switch to player 1
**2** | Switch to player 2
**3** | Switch to player 3
**M** | Re-initialize party
**- (minus)** | Enter a command (see above)
**Space** | Do nothing (just scan)


**Have phun!**

/JiNX
