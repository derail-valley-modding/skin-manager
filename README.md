# Skin Manager Mod (For Users)
## How to Install the Mod:
1) Download & install Unity Mod Manager from Nexus Mods.
2) Download the latest Skin Manager release from Github or Nexus Mods. Find the downloaded zip file and drag it into the mods tab of Unity Mod Manager.

## How to Add Skins:
### v3.1+ skins with an Info.json file:
As of v3.1, skins are now treated as their own mods by Unity Mod Manager. If the skin is compatible, you can simply drag and drop the zip into the mods tab of UMM. If this results in an error, you are likely dealing with an older format of skin. See the instructions below.
### v3.0 and Earlier Skins:
The easiest way to install earlier skins is to use the configurator app:
1) Download and launch the SkinConfigurator application
2) In the menu bar, click Import > Import Old Zip...
3) Use the file picker to select the skin pack that you want to install
4) Any skins found in the pack will be listed in the window. You may need to adjust the Names and Car IDs by selecting a skin in the list and using the inputs at the bottom of the window. You can also remove skins by selecting them and clicking the "Remove Skin" menu button.
5) If you only want to use the skin on your own computer, you can click the yellow "Install Unzipped..." button.
6) A folder picker should appear - if it is not already pointed at your steamapps/common/Derail Valley/Mods folder, navigate there yourself. Select or create a new folder under Mods to hold the skin pack.
7) Once you've picked the destination, hit Select Folder and you should get a message saying the export was successful.
8) Run the game

### Last Resort Instructions:
The initial release of skin manager for Simulator was based on BepInEx. Skins made for this period of time should have a BepInEx folder inside the archive. Install the skin by extracting the archive and merging its BepInEx folder into the BepInEx folder of the Game (*\Steam\steamapps\common\Derail Valley\BepInEx*).

If you are using an older skin (one that contains either a Skins folder or just car type folders), navigate to the skins folder in your DV install (*\Steam\steamapps\common\Derail Valley\Mods\SkinManagerMod\Skins\\*). Extract each of the car type folders from the archive into the *SkinManagerMod\Skins* folder. The proper skin path should look like this - *Derail Valley\Mods\SkinManagerMod\Skins\content\skins\<train-car-type>\<skin-name>\<skin-files>*.


# Skin Configurator (For Creators)
## Exporting Textures
1) Start the game
2) Hit ESC to open Main Menu (Default is ESC)
3) Open Mod Manager (Default is Ctrl-F10)
4) Go to 'Skin Manager' and click the ... icon to toggle the options
5) Select a car from the dropdown and click 'Export Textures'
6) Close the game and navigate inside `Derail Valley\Mods\SkinManagerMod\Exported` folder
7) Get the exported texture(s) and edit it using an image editor

## Creating a Pack
1) If you haven't already, download and extract the SkinConfigurator zip. You can run it from anywhere, but you might need to also install the .NET 6 runtime.
2) At this point, you should have one or more folders containing edited textures, one for each individual skin
3) Launch the SkinConfigurator exe. It will bring up a GUI (shown below) where you can select a Name, Version, and Author (optional) for your skin pack.
4) Use the "Add Skin..." button to select each of the folders containing your skin files. They will be added to the list in the GUI and you will be able to select a Name and the Car ID that the skin should apply to. You can remove a skin by selecting it and clicking "Remove Skin". You can add multiple skin folders for the same car type using the "Add Multiple..." button - this will allow you to select a folder and create an entry for each subfolder inside it.
5) Once you have set your pack and skin properties, you are ready to export. The large Package button should be green and enabled - if it is grayed out, there is a required field that hasn't yet been filled in.
6) Click the "Package..." button to save your pack to a .zip file that users can install with UMM. The export process may take a few seconds, especially when dealing with lots of textures.
7) You can use the "Upgrade Pack..." button to repackage an older skin from Overhauled or BepInEx. The older skin pack must be unzipped somewhere on your computer. Click the button and select the top folder containing the skin files, and the app will do its best to infer which skins are contained inside. If this fails, you can also manually add each skin folder from inside the older pack.

![image](https://github.com/derail-valley-modding/skin-manager/assets/11540268/1c36cd57-ada5-4707-9ef3-fa3e3a223d1e)
