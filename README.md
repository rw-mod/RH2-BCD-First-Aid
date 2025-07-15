# [RH2] BCD: First Aid (1.6 Update)

This is a **reimplementation** of the [original "[RH2] BCD: First Aid" mod](https://steamcommunity.com/sharedfiles/filedetails/?id=2563152474) for RimWorld, updated to support **version 1.6**.

In RimWorld 1.5, the original mod added a Float Menu option via **Harmony patching**.  
However, as of RimWorld 1.6, this functionality can now be added using the **native `FloatMenuOptionProvider` system**, which this mod takes full advantage of.

---

## ✅ Features

- Adds a **Perform first aid** Float Menu option when right-clicking on downed pawns.
- First aid tends to downed pawns without a bed.
- First aid will allow the medic to grab the nearest medicine resource, otherwise they will tend to the casualty with no medicine.
- Can be used on enemy pawns and friendlies.
- First aid tending speed is faster than normal tending, however the quality of tending is reduced.

---

## 📦 Differences from the Original Mod

- The Float Menu is built using RimWorld 1.6's **native modding API**, not the Harmony Patch.
- Refactored and cleaned up logic for better compatibility with the new FloatMenu system.
- Fully compatible with other FloatMenuOptionProviders in 1.6.

---

## 📌 Notes

- This is **not an official update** by the original author.
- This version was rebuilt by reviewing and understanding the original mod’s functionality, then adapting it using new 1.6 tools.
- The mod should be safe to use in existing saves, but always back up your game just in case.

---

## 📜 Credits

- Original mod by **[Chicken Plucker](https://steamcommunity.com/id/chickenplcker/)** ([Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2563152474))
- 1.6 update and native FloatMenu integration by **[Bounty](https://github.com/b0unt9)**

---

## 🔧 Compatibility

- RimWorld 1.6+
- Incompatible with the original mod (disable one or the other)
- Compatible with most other mods unless they interfere with inventory or FloatMenu logic

---

## 💬 Feedback

Issues or suggestions?  
Feel free to leave a comment or contact me via GitHub: [github.com/rw-mod/RH2-BCD-First-Aid](https://github.com/rw-mod/RH2-BCD-First-Aid)

---