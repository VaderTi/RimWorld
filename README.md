# 🌡️ RimWorld Mods — by Vader

> A collection of mods for [RimWorld](https://rimworldgame.com/) — the sci-fi colony simulator by Ludeon Studios.

---

## 📦 Mods in This Repository

### Climate Control

> *"Система климат контроля. Устанавливает минимальную и максимальную комфортную температуру и удерживает её."*
> *(Climate control system. Sets the minimum and maximum comfortable temperature and keeps it.)*

A smart climate management building that **automatically heats or cools a room** to keep it within a configurable comfortable temperature range.

---

## 🏗️ Climate Control — Overview

In RimWorld, colonists suffer mood and health penalties when temperatures fall outside their comfort zone. Vanilla heating (electric heater, campfire) and cooling (cooler) devices simply push heat in one direction and require manual management. **Climate Control** replaces that hassle with a single, intelligent unit that handles both heating and cooling automatically.

### ✨ Features

| Feature | Details |
|---|---|
| 🌡️ **Dual-mode thermostat** | Heats when too cold, cools when too hot — all in one building |
| 🎛️ **Configurable temperature range** | Set your own Min / Max comfortable temperature via in-game buttons |
| ⚡ **Dynamic power consumption** | Power draw scales with how far the room temperature is from the target |
| 📊 **Status display** | Shows current state (Idle / Heating / Cooling / Installed outside) in the building inspector |
| 🗺️ **Room highlight overlay** | Selectable color-coded overlay shows which room is being controlled |
| 🔢 **Celsius & Fahrenheit** | Respects your in-game temperature display preference |
| 🌐 **Bilingual** | Full English and Russian (Русский) language support |
| 🔧 **Breakdownable** | Can malfunction like other electrical buildings — keep an engineer handy! |

---

## 🏠 In-Game Information

### Building Stats

| Property | Value |
|---|---|
| **Category** | Temperature |
| **Research prerequisite** | Electricity |
| **Construction skill** | 5+ |
| **Build cost** | 100 × Steel, 5 × Components |
| **Build time** | 1 000 work units |
| **Hit points** | 100 |
| **Mass** | 10 kg |
| **Flammability** | 25 % |
| **Passability** | Pass-through only |

### Power Consumption

Power draw is not fixed — it increases the further the room temperature deviates from the target range:

```
Power = |ΔT × roomCells × energyMul × 0.333| × (modeFactor + 0.0077 × |ΔT|) × 5
```

**Variable legend:**

| Variable | Meaning |
|---|---|
| `ΔT` | Temperature difference between current room temp and the target boundary |
| `roomCells` | Number of map cells that make up the room |
| `energyMul` | Base energy multiplier — 1.0 normally, bumped to 1.5 when `|ΔT| < 3 °C` for a quick burst |
| `modeFactor` | 1.1 when heating, 1.6 when cooling |
| `0.0077` | Efficiency loss per degree of temperature difference (`1/130`) |

- **Cooling mode** uses ~45 % more power than heating (power factor 1.6 vs 1.1).
- If the temperature difference is less than **3 °C**, the energy multiplier gets a **+0.5** boost for a quick burst to reach target faster.
- When the room is within range, the unit idles at just **1 W**.

### Status Colors (Room Overlay)

| Status | Color | Meaning |
|---|---|---|
| ⏸️ **Idle** | 🟡 Yellow-Orange | Temperature is within the comfortable range |
| 🔥 **Heating** | 🔴 Red | Room is too cold — actively warming up |
| ❄️ **Cooling** | 🔵 Blue | Room is too hot — actively cooling down |
| 🌿 **Outside** | *(no overlay)* | Unit is placed outdoors — cannot control temperature |

---

## 🎮 How to Use

1. **Research** the **Electricity** technology tree node.
2. Open the **Architect** menu → **Temperature** tab.
3. Place the **Climate Control** unit inside an enclosed room.
4. Select the building to open its inspector panel.
5. Use the **four temperature buttons** to set your desired range:
   - ➕ / ➖ buttons next to **Min** — adjust the minimum comfortable temperature.
   - ➕ / ➖ buttons next to **Max** — adjust the maximum comfortable temperature.
6. Connect the building to a **power grid** — it will start working automatically.

> ⚠️ **Note:** The unit must be placed **inside an enclosed room**. If placed outdoors, it will display the *"Installed outside"* status and will not function.

---

## 🌐 Language Support

| Language | Status |
|---|---|
| 🇬🇧 English | ✅ Full support |
| 🇷🇺 Russian (Русский) | ✅ Full support |

---

## 🔧 Technical Details

| Property | Value |
|---|---|
| **Mod namespace** | `GCRD` |
| **Main class** | `GCRD.Building_ClimateControl` |
| **Place worker** | `GCRD.PlaceWorker_ClimateControl` |
| **Target version** | RimWorld Alpha 18 (0.18.1722) |
| **Assembly version** | 0.18.1.0 |
| **Author** | Vader |
| **Copyright** | © 2018 Vader |

### Project Structure

```
Climate Control/
├── About/
│   └── About.xml               # Mod metadata
├── Assemblies/
│   └── ClimateControl.dll      # Compiled mod assembly
├── Defs/
│   └── ThingDefs/
│       └── Building_ClimateControl.xml   # Building definition
├── Languages/
│   ├── English/                # English translations
│   └── Russian/                # Russian translations
├── Source/
│   └── ClimateControl/         # C# source code
│       ├── Building_ClimateControl.cs    # Core building logic
│       └── PlaceWorker_ClimateControl.cs # Placement preview logic
└── Textures/
    ├── Things/Building/        # Building sprite
    └── UI/Commands/            # Temperature control button icons
```

---

## 📝 License

Copyright © 2018 **Vader**. All rights reserved.

---

## 🙏 About RimWorld

[RimWorld](https://rimworldgame.com/) is a sci-fi colony management simulator developed by **Ludeon Studios** (Tynan Sylvester). Players manage a group of colonists stranded on a distant planet, constructing bases, managing resources, and surviving threats ranging from raids to extreme weather. Temperature management is a core game mechanic — colonists require comfortable temperatures to stay healthy and productive, making the Climate Control mod a quality-of-life improvement for any colony.
