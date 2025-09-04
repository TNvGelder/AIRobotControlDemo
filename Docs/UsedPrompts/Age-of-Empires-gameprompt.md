can you make me an age of empires style game in threejs where 2 ais play against eachother. 
- Put everything on one html page so I can easily copy it without dependencies on other files unless its a link to something available on the internet (like threejs).
Game Objective and strategy:
- If a player destroys the enemy towncenter, the player wins.
- Players will focus on both army (archers/knights) and the economy behind it (workers)
- Players also fight for control over valuable gold resources
Gold:
- The player should be able to purchase workers, archers and swordsmen/knights
- add 20 gold resources to collect with the workers. 
- When no gold resources are left create 3 new resources around the middle.
Units:
- . Archers should be able to shoot  like in AoE. Player should be able to control units of any of the ai if they want.
- It should be clear when units are fighting.
- units should never be standing idle, they should always be doing something.
- Both players start with 3 workers, 2 archers, 2 knights

- When under attack an AI will respond with non worker units.
Controls:
- Make it so that I can select units in a region similar to AoE.
- With right click i can send those to a target (Or i can press buttons for more ai driven options like Attack, Defend Workers, etc)
- I want to be able to zoom in and move the camera (WASD). 
- Add a button for resetting the camera.
- When controlling a player I can select how much of the gameplay i should let AI handle in a dropdown
- Allow the player to queue what the next purchases are going to be for a player, and allow removing things from queue. The queue will be followed regardless of controlsettings.
- If the money is available the unit in front of the queue will be created in 5 seconds (FIFO)