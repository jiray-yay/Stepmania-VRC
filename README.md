# Stepmania-VRC
This project aims to recreate stepmania (https://github.com/stepmania/stepmania) into vrchat. It basically is a combination of a parser for sm files + visualizers and gameplay manager using the parsed data + virtual arcade pads.
This project uses Udon# (https://github.com/vrchat-community/UdonSharp)

## Song/Chart format
Songs and charts needs to be embedded inside the world. Chart format is Stepmania's .sm format.
Stepmania is a rhythm game engine/simulator mainly (but not only) used for ddr-like games, it had multiple derivation of it's file format (such as the .ssc format for stepmania 5) but this VRC simulator only works with the .sm format.
To make/edit your own charts or to convert .ssc to .sm, I recommend using "ArrowVortex" : https://arrowvortex.ddrnl.com/

## Adding songs
Each cab prefab has a "StepfilesManager" (CabPrefab->CommonPlayersPrefab->StepfilesManager), you need to fill the TextAssets and AudioFiles arrays by yourself. textAssets should be .sm files renamed as .sm.txt (because Unity can't recognize .sm as text assets and it's impossible to read a folder/files with Udon).
The indexes of the files in the TextAssets array and AudioFiles array needs to match properly (let's say that you cant to add the song "AwesomeSong" as the first song of the StepfilesManager, you should have "AwesomeSong.txt" at index 0 of textAssets array and "AwesomeSong.ogg" at index 0 of AudioFiles array)
If you need help to find/add songs for any of the game mode, feel free to contact me. I can also give you the parapara charts of my demo world if needed as these are custom made.

## Chart choice and performances
Despites optimizations attempt, this project can be very cpu intensive (probably due to the Udon VM?) both at loading phase and while playing depending on the chart. To optimize performance the most simple thing you can do when using the prefab is being careful with the chartfile/.sm size.
Basically, you want to avoid "grey notes" (notes that are not the usual 1/4th 1/8th 1/16th of a bar) in your chart and to keep your chart size as low as possible, having grey notes usually adds absurd bar dividers which adds a lot of empty lines to the .sm file and the higher the line count for a bar => the longer it takes for the game to check for a note
Another potential source of cpu usage depending on the machine is the abundance of stops or bpm changes.
(To be more in-depth, the simulator creates arrays representing the partition out of the lines in the .sm file, when playing the chart in-game, you have to go through each line that is in the "visualization range" of the current time. 
Calculating the visualization range (partition position to time and vice-versa) depends on the number of time stops and bpm changes, which add algorithm complexity. To check for note presence you have to verify the lines, so the bigger the line divider is means more (eventually empty) lines to check, which also adds to the complexity)

## About license
This project is under the MIT License, so basically you can just do anything with it for free!

## Other links
A demo VRC world is up if you want to try the prefab, be sure to calibrate the settings before playing (square button below the play button to make settings appears behing the pads): https://vrchat.com/home/launch?worldId=wrld_2cbe2c07-15f1-4c66-a5b9-aac37aa0446d
You can also find this project on booth: https://jiraymin.booth.pm/items/3652408
And here's the promotion tweet of this project: https://twitter.com/Jiraymin/status/1492582105854926850

## Contact
If you need to contact me for help, suggestion or whatever, fastest ways are via twitter ( https://twitter.com/Jiraymin ) or discord ( Jiraymin#3172 , I'm in the official VRChat server if you need a common server to dm me)
