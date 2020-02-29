# Description


WFInfo V9 is a companion app for Warframe, based on the [original](https://github.com/Schwaxx/WFInfo) which is no being longer developed. 
WFinfo is designed to provide quick access to both Platinum and Ducat prices for all fissure rewards to make selecting the best reward easy.

WFInfo does this by screenshotting the game window, cropping out the part text, then passing it to an Optical Character Recognition Engine, specifically Google's Tesseract. The OCR Engine will then send back the text it found, and we will pull out the part name from that text. From there, we display the stats for each part in an overlay or on a separate window.

# Usage

1. Download the [latest release](https://github.com/random-facades/WFInfo/releases)
1. Run WFInfo.exe and wait for it to complete the initial load (databases + OCR data)
1. Click the Cog Icon to open Settings, and configure your hotkey, scaling, etc
1. Press the hotkey on a fissure reward screen to show the display (Or wait if you turned on auto mode)

# Will I get Banned?

DE[Adian] has confirmed in a forum post that this will not ban you. 

[![Image of post](https://i.imgur.com/ZGD8ISp.jpg)](https://forums.warframe.com/topic/875096-wfinfo-in-game-ducats-and-platinum-prices/?do=findComment&comment=9176107)

Reference:

https://forums.warframe.com/topic/875096-wfinfo-in-game-ducats-and-platinum-prices/?do=findComment&comment=9176107

# Features

### Expansive Part Information

![Overlays](https://i.imgur.com/Fl2z7qS.png)

![Reward Window](https://i.imgur.com/Fl2z7qS.png)

When WFInfo displays the part information, through an overlay or a separate window, a large selection of data is displayed. 

Here's a quick highlight of the information shown:

- Owned count (based on the Equipment Window)
- Vaulted tag (shows up if the part is vaulted)
- Part name (use this to confirm no errors occured)
- Plat Value (based on average prices on warframe.market)
- Ducat Value
- Volume Sold (amount of parts sold in the last 48 hours on warframe.market)

### Various Conveniences

* Auto Mode
  * When enabled, this will listen to the debug log (EE.log) and wait for a message to appear saying that the rewards are displayed. Currently, it is "Got rewards". Once it sees that message, it will trigger the display function and bring up your overlay/window with the reward info.
  * If it can't detect the reward screen within 5 seconds, then it will assume an error happened.
    * If it doesn't display, you can just hit the key and force it to activate.

![Clipboard](https://i.imgur.com/Fl2z7qS.png)

* Clipboard Copy
  * When enabled, WFInfo will copy the plat prices and the part names into the clipboard so you can just paste them into Warframe's Chat for your party-mates.
  * This does attempt to link the parts themselves so that it's easier to see.

![AutoUpdate](https://i.imgur.com/Fl2z7qS.png)

* Auto Update!
  * During the initial load of WFInfo, it will query our site to check for any updates available.
  * If any are found, it will display the prompt above and you can let it download and update WFInfo for you!
  * We're currently working on slimming the download file down, so you don't have to worry about a massive download with every update. Current estimates are in the range of 15MB.

### Relic Panel

![Relic Panel](https://i.imgur.com/k2yEW87.png)

The Relics panel allows you to look at all relics and see statistics for each. We show basic info such as whether a relic is vaulted and the relic's rewards and the rarities of those rewards, and we have three complex statistics for every relic:
1. The average platinum price of an Intact relic's rewards, which are based on drop chance.
1. The average platinum price of a Radiant relic's rewards, which are as well.
1. The difference between the two above values, which can be used to find which relics will give you more plat, on average, when refined.

### Equipment Panel

![Equipment Panel](https://i.imgur.com/bQRDYvR.png)

The Equipment window allows you to look at each prime equipment and its parts. It shows how much each costs currently on warframe.market, and also shows how much it will cost to purchase a whole set of equipment. 

If you mark an item here, it will show up during a fissure reward screen.

##### Note: For both of these info panels, there are several sorting options that allow you to find what is best for you. Also they have grouping features that allow you to see and sort all relics, or to only sort from one era, i.e. Lith.

# Credits/Contact:

**Kekasi:** u/RandomFacades (Reddit), Kekasi (Warframe), Kek#5390 (Discord)

**Dapal-003:** u/Dapal-003 (Reddit), Dapal003 (Warframe), ダパール・Dapal-003#0001 (Discord)

**Dimon222:** u/dimon222 (Reddit), dimon222 (Warframe), dimon222#8256 (Discord)

**Discord:** https://discord.gg/qfd3eFb

**Website:** https://wfinfo.warframestat.us/
