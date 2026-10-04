# 🦸 Desktop Character Pet (Marvel Edition)

> An animated, transparent Windows desktop overlay application featuring **Marvel superheroes & villains** battling across your screen above all applications, built with **C# and .NET 10 WPF**.

---

## ⚡ Quick Start: Download & Run (No Install Required)

You do **not** need to install Visual Studio or .NET to run this application!

1. Download the standalone executable directly:
   * **[DesktopCharacterPet.exe](./Release/DesktopCharacterPet.exe)** *(Direct 1-Click Executable)*
   * Or download **[DesktopCharacterPet-v1.0.0-win-x64.zip](./Release/DesktopCharacterPet-v1.0.0-win-x64.zip)** and extract it.
2. Double-click **`DesktopCharacterPet.exe`** to start.
3. Your Marvel heroes will immediately appear and begin battling across your desktop!

---

## 🌟 Features

* **100% Transparent Overlay**: No window borders, title bars, or white/black backgrounds. The heroes appear directly on your desktop over Chrome, VS Code, games, etc.
* **Always on Top**: Remains visible above background windows.
* **Full Marvel Roster**:
  * 💥 **Iron Man vs. Thanos**: Repulsor energy beam clashing against the Infinity Gauntlet cosmic blast with a mid-screen collision explosion and lightning arcs!
  * 🕷️ **Spider-Man**: Leaping acrobatics, firing dual web lines with "THWIP!" web bursts and superhero landing.
  * ⚡ **Thor (God of Thunder)**: Raising Mjolnir skyward as massive crackling lightning bolts strike down into the hammer!
  * 🟢 **The Incredible Hulk**: Leaping high and slamming fists into the ground with ground cracks, shockwaves, and flying debris!
  * 🛡️ **Captain America**: Blue suit and star shield, hurling the Vibranium shield in a spinning ricochet arc and catching it!
  * ⚡ **Iron Man Solo**: Firing high-intensity cyan repulsor beam cannons with weapon recoil.
  * 💀 *Bonus retro transparent characters: Walking Skeleton, Pikachu, Fox, Dog, Totoro.*
* **⚔️ 4 Battle Flight Paths**:
  * **Path 1: Ground Clash**: Fighting just above your Windows taskbar.
  * **Path 2: Mid-Air Dogfight**: Eye-level battle hovering across the center of your monitor.
  * **Path 3: Sky Strike**: Aerial dogfight patrolling the top edge of your screen.
  * **Path 4: Dynamic Wave**: Continuous swooping dive across the full height of your screen.
  * **🔄 Auto-Cycle Mode**: Seamlessly switches between all 4 flight paths on each screen bounce!
* **💥 Interactive Comic Sound Bursts**:
  * Left-click or drag your hero to trigger classic comic book action text bursts (`"💥 POW!"`, `"⚡ THWIP!"`, `"💥 SMASH!"`, `"🛡️ CLANG!"`, `"⚡ BOOM!"`).
* **🖱️ Mouse Scroll Wheel Zoom**:
  * Hover over any character and **scroll the mouse wheel UP or DOWN** to dynamically scale them to any size in real time (from 120px up to 720px cinema scale).
* **⚡ 3 Combat Speed Modes**:
  * 🚶 *Patrol Speed* (2.5 px)
  * 🏃 *Fast Combat* (4.5 px)
  * 🚀 *Super Sonic Blitz* (7.0 px)
* **🎨 Dark Glassmorphic Context Menu**:
  * Right-click anywhere on the hero or the Windows System Tray icon to switch characters, alter speed, change flight paths, pause, or resize.

---

## 🛠️ Controls & Shortcuts

| Action | Control |
| :--- | :--- |
| **Move Hero Anywhere** | Click and drag with **Left Mouse Button** |
| **Trigger Attack Burst** | Left-click on character |
| **Resize Hero** | **Mouse Scroll Wheel UP / DOWN** |
| **Open Menu** | **Right-Click** on hero or System Tray icon |
| **Pause / Resume** | Right-Click → `⏸ Pause` / `▶ Resume` |
| **Switch Flight Path** | Right-Click → `⚔️ Battle Flight Paths` |
| **Switch Character** | Right-Click → `🦸 Marvel Heroes & Characters` |
| **Exit** | Right-Click → `❌ Exit Application` |

---

## 💻 Building from Source

### Prerequisites
* Windows 10 / 11 (x64)
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Run in Development
```powershell
cd DesktopCharacterPet
dotnet run
```

### Publish Standalone Release Executable
```powershell
dotnet publish -c Release
```
The output executable will be generated at:
```text
DesktopCharacterPet\bin\Release\net10.0-windows\win-x64\publish\DesktopCharacterPet.exe
```

---

## 📜 License
MIT License. Created for desktop companionship and Marvel fan entertainment.
