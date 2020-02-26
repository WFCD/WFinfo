# Description


WFInfo V9 is a companion app for Warframe, based on the [original](https://github.com/Schwaxx/WFInfo) which is no being longer developed. 
WFinfo is designed to provide quick access to both Platinum and Ducat prices for all fissure rewards to make selecting the best reward easy.

WFInfo does this by screenshotting the game window, cropping out the part text, then passing it to an Optical Character Recognition Engine, specifically Google's Tesseract. The OCR Engine will then send back the text it found, and we will pull out the part name from that text. From there, we display the stats for each part in an overlay or on a separate window.

# Usage

1. Download the [latest release](https://github.com/random-facades/WFInfo/releases)
1. Run WFInfo.exe and wait for it load the databases
1. Click the Cog Icon to open Settings, and configure your hotkey, scaling, etc
1. Press the hotkey on a fissure reward screen to show the display (Or wait if you turned on auto mode)

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

### Relic Panel

![Relic Panel](https://i.imgur.com/k2yEW87.png)

The Relics panel allows you to look at all relics and see statistics for each. We show basic info such as whether a relic is vaulted and the relic's rewards and the rarities of those rewards, and we have three complex statistics for every relic:
1. The average platinum price of an Intact relic's rewards, which are based on drop chance.
1. The average platinum price of a Radiant relic's rewards, which are as well.
1. The difference between the two above values, which can be used to find which relics will give you more plat, on average, when refined.

### Equipment Panel

![Equipment Panel](https://i.imgur.com/bQRDYvR.png)

The Equipment window allows you to look at each prime equipment and its parts. It shows how much each costs currently on warframe.market, and also shows how much it will cost to purchase a whole set of equipment. You also can mark items that you own, and it will adjust the total cost to only the remaining items.

##### Note: For both of these info panels, there are several sorting options that allow you to find what is best for you. Also they have grouping features that allow you to see and sort all relics, or to only sort from one era, i.e. Lith.

# Will I get Banned?

DE[Adian] has confirmed in a forum post that this will not ban you. 

[![Image of post](https://i.imgur.com/ZGD8ISp.jpg)](https://forums.warframe.com/topic/875096-wfinfo-in-game-ducats-and-platinum-prices/?do=findComment&comment=9176107)

Reference:

https://forums.warframe.com/topic/875096-wfinfo-in-game-ducats-and-platinum-prices/?do=findComment&comment=9176107

# Credits/Contact:

**Kekasi:** u/RandomFacades (Reddit), Kekasi (Warframe), Kek#5390 (Discord)

**Dapal-003:** u/Dapal-003 (Reddit), Dapal003 (Warframe), ダパール・Dapal-003#0001 (Discord)

[Discord](https://discord.gg/qfd3eFb)

[Website](https://random-facades.github.io/WFInfo/)
