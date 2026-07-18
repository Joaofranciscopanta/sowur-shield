# **Product Requirements Document (PRD)**
# **Sowur Shield: Animals & Combat System**

**Version:** 2.0 (Major Revision)
**Date:** 2025-10-21
**Author:** Claude Code + User

---

## **📝 Changelog**

### **Version 2.0 - Major System Overhaul**
- **Grid Size**: Changed from 7x4 to **9x5 grid** (9 columns, 5 rows)
  - 3 rows for player side (front/middle/back positioning)
  - 2 rows for enemy side
  - Supports 8-12 animals + player character per team

- **Speed System**: Upgraded to **Turn Gauge System**
  - Speed now determines turn frequency, not just order
  - Units with 2x speed act twice as often
  - Makes speed stat dramatically more valuable

- **Passive Skill Overhaul**: Complete redesign to **Family + Class + Happiness** structure
  - Every animal has EXACTLY 3 passives (predictable)
  - Family passives scale 1→2→3→4+ animals (powerful stacking)
  - Class passives provide strong identity + synergies
  - Happiness passives reward animal care (up to +50% all stats at 100 happiness)

- **Synergy System**: Elevated to **CORE MECHANIC** with massive power buffs
  - Family synergies: Up to +60% DEF, attack twice per turn, act first each round
  - Class synergies: Guardian, Execute, Rampage, Resurrection abilities
  - Combo synergies: Ultra-powerful effects when multiple synergies align
  - Seasonal bonuses increased to +40% ATK, +25% DEF, +30% Speed

- **Feeding System**: Changed to **Specific Food Requirements**
  - Each animal requires specific food type (no generic Hay)
  - Examples: Chicken needs Wheat, Cow needs Hay, Monkey needs Banana
  - 3 units per animal to recruit for battle
  - Food quality tiers provide combat buffs
  - Deep farming integration - must plan crop rotation around combat team

### **Version 1.0 - Initial Draft**
- Basic combat system design
- Team assembler UI concept
- Initial synergy ideas (replaced in v2.0)

---

## **📋 Executive Summary**

This PRD defines the Animals & Combat System for Sowur Shield, an auto-chess style combat system integrated with farming simulation and dating sim mechanics. Players raise animals through farming activities, assemble teams, and battle in tactical auto-combat encounters alongside their player character.

**Core Pillars:**
1. **Farming → Combat Loop**: Daily care (feeding, petting) directly impacts combat performance
2. **Auto-Chess Tactical Combat**: Positioning-based strategy with automatic battle execution
3. **Dating Sim Integration**: NPC relationships unlock animals, skills, and bonuses
4. **Synergistic Team Building**: Family/Class stacking creates powerful combinations

---

## **🎮 System Overview**

### **Game Flow Architecture**
```
[Farming Sim Scene]
    ↓
[Combat Trigger] ← (Farm Area / Quest Event)
    ↓
[Team Assembler UI] ← (Select animals, position team, recruit via feeding)
    ↓
[Combat Scene] ← (Auto-chess battle with player character)
    ↓
[Results & Rewards] ← (XP, items, unlocks)
    ↓
[Return to Farm]
```

### **Key Features Already Implemented**
✅ Animal base stats (Attack, Defense, Speed, Health, Accuracy)
✅ Happiness system (0-100, affects combat multiplier 0.5x-1.5x)
✅ Stat growth via farming activities (petting, feeding, production)
✅ Active skills (1 per animal)
✅ Passive skill foundation (new system: Family + Class + Happiness passives)
✅ Class system (Tank, DPS, Support, Utility)
✅ Family system (biological families like Galliformes, Bovidae)
✅ Seasonal bonuses
✅ Save/load integration
✅ AnimalInfoUI displaying combat stats

### **Features to Implement**
🔨 Combat Scene & Battle Manager
🔨 Auto-chess grid positioning system
✅ Team Assembler UI (IN PROGRESS - see Implementation Status below)
🔨 Player character combat integration
🔨 Animal recruitment/feeding cost system
🔨 Family/Class synergy buff calculation
🔨 Combat rewards & XP system
🔨 NPC gift/teaching system
✅ Combat triggers (zones + events) - COMPLETED

### **🚧 Current Implementation Status (2025-10-24)**

#### **Team Assembler UI - IN PROGRESS**
**Status:** Core functionality complete, visibility issues being resolved

**✅ Completed Components:**
- `TeamAssemblerUI.cs` - Main UI controller with grid setup
- `TeamAssemblerData.cs` - Singleton data management for team state
- `AnimalSelectionCard.cs` - Drag-and-drop animal cards with full event handling
- `GridPositionSlot.cs` - Grid cells with drop detection and swap logic
- `CombatTriggerZone.cs` - Zone-based combat triggers with collision detection
- Player movement blocking when UI is open
- E key input blocking to prevent UI destruction during drag operations
- Grid slot movement with self-swap detection
- Container width fixes (was 0px, now 400px)
- Runtime sprite generation for card backgrounds

**🐛 Known Issues:**
- **Card Visibility**: Cards are invisible in "Available Animals" panel until dragged
  - Symptoms: Cards exist, have correct size (200x120), are at correct position
  - Dragging works perfectly (cards become visible when dragged)
  - Logs show: No RectMask2D detected, container width correct (400px)
  - Position data: Card at (73-242, 234-335) world coords - should be visible
  - Likely cause: ScrollRect viewport configuration or parent canvas rendering order

**Files Created:**
- `Assets/Scripts/Combat/TeamAssemblerUI.cs` (380+ lines)
- `Assets/Scripts/Combat/TeamAssemblerData.cs` (180+ lines)
- `Assets/Scripts/Combat/AnimalSelectionCard.cs` (358+ lines)
- `Assets/Scripts/Combat/GridPositionSlot.cs` (284+ lines)
- `Assets/Scripts/Combat/CombatTriggerZone.cs` (150+ lines)
- `Assets/Prefabs/Combat/AnimalCardPrefab.prefab`
- `Assets/Prefabs/Combat/GridSlotPrefab.prefab` (likely exists)

**Technical Achievements:**
1. **Drag-and-Drop System**: Full Unity EventSystem integration with IBeginDragHandler, IDragHandler, IEndDragHandler
2. **Grid Management**: 9x5 grid (45 slots) with column-based positioning (cols 0-5 enemy, 6-8 player)
3. **Smart Swap Logic**: Animals can swap positions, prevents self-swapping
4. **Feed State Tracking**: Cards show "Fed ✓" vs food requirements
5. **Team Validation**: Max 15 animals, position conflict detection
6. **Layout Management**: VerticalLayoutGroup + ContentSizeFitter for auto-sizing

#### **Combat Triggers - COMPLETED**
**Status:** Fully functional

**Implementation:**
- Zone-based triggers using Collider2D OnTriggerEnter2D
- Prevents UI re-opening when already open
- Proper cleanup on zone exit
- Integration with TeamAssemblerUI

**Next Session Goals:**
1. **FIX CARD VISIBILITY**: Investigate ScrollRect viewport masking/rendering
2. **Test full team assembly flow**: Drag multiple animals, feed, position
3. **Implement "Start Battle" button**: Transition to CombatScene
4. **Create CombatScene**: Basic scene setup with grid display

---

## **⚔️ Combat System Design**

### **1. Combat Style: Auto-Chess**

**Grid Layout:**
- **9x5 rectangular grid** (9 columns, 5 rows)
- **Player side**: Rows 1-3 (front, middle, back)
- **Enemy side**: Rows 4-5 (front, back)
- **Positioning matters**: Front row tanks absorb damage, middle row balanced, back row DPS/Support protected
- **Total capacity**: Up to 27 player units possible (row 1-3), typically 8-12 animals + player character

**Turn-Based Auto-Battle:**
- **Speed stat determines turn order AND turn frequency** (highest speed acts first and more often)
  - **Multiple Turns System**: If unit's speed is 2x another unit's speed, it acts twice before that unit acts once
  - **Turn Gauge System**: Each unit has a "turn gauge" that fills at rate = Speed stat
  - When gauge reaches 100, unit takes action and gauge resets
  - Example: Speed 20 unit acts twice as often as Speed 10 unit
- **Actions are automatic** based on AI priorities:
  1. Active skills (if off cooldown)
  2. Basic attacks
  3. Target selection (highest threat/lowest HP/etc.)
- **Battle continues until**:
  - All enemies defeated (Victory)
  - All player units defeated (Defeat)
  - Turn limit reached (configurable per battle, e.g., 50 total turns)

**Combat Phases:**
```
1. Pre-Battle Setup (Team Assembler UI)
   - Select animals (feed to recruit)
   - Position on grid
   - Confirm player character position

2. Battle Initialization
   - Load combat scene
   - Spawn units at positions
   - Calculate synergy buffs
   - Display unit stats/skills

3. Combat Loop
   - Sort units by speed
   - Execute turn for each unit:
     * Check active skill availability
     * Select target
     * Execute action (skill or attack)
     * Apply damage/effects
     * Check for deaths
   - Repeat until win/loss condition

4. Battle End
   - Display results screen
   - Award XP to animals + player
   - Award items/currency
   - Update quest progress
   - Return to farm
```

### **2. Player Character in Combat**

**Player Stats:**
- **Class-Based**: Player can learn different combat classes from NPCs
  - Examples: Warrior, Mage, Ranger, Bard (support)
- **Stats Scale with Player Level** (existing system: `PlayerGameData.playerLevel`)
- **Skills Taught by NPCs** via relationship progression

**Player Positioning:**
- Player occupies **1 grid slot** like an animal
- Can be positioned anywhere on player's side
- **Stats comparable to animals** but uses different skill pool

**Player Abilities:**
- **1 Active Skill** (class-dependent)
- **Passive Bonuses** (taught by high-relationship NPCs)
- **Equipment/Items** (future expansion)

### **3. Combat Mechanics**

**Damage Calculation:**
```csharp
float baseDamage = attacker.CurrentAttack;
float damageReduction = 1 - (defender.CurrentDefense / (defender.CurrentDefense + 100));
float finalDamage = baseDamage * damageReduction;

// Accuracy check
if (Random.value <= attacker.CurrentAccuracy) {
    ApplyDamage(defender, finalDamage);
} else {
    ShowMiss();
}
```

**Critical Hits** (optional):
- 10% base crit chance
- Deals 1.5x damage
- Shown with special VFX

**Status Effects** (future expansion):
- Stun, Poison, Shield, Heal-over-time
- Applied by specific skills

### **4. Skill System**

**Active Skills** (already implemented in `AnimalSkill.cs`):
- **Cooldown-based** (`cooldownTurns` in AnimalSkill)
- **Damage multipliers** (`damageMultiplier`)
- **Healing** (`healAmount`)
- **Target selection**: Self, allies, enemies

**Passive Skills** (NEW SYSTEM - Core Mechanic):

**Every animal has EXACTLY 3 passive skills:**

1. **Family Passive** (Automatic)
   - Granted based on `AnimalData.animalFamily`
   - Example families:
     - **Galliformes** (Birds): "Flock Instinct" - +15% Speed when 2+ birds in team
     - **Bovidae** (Cattle): "Herd Strength" - +20% Defense when 2+ cattle in team
     - **Primates** (Monkeys): "Pack Tactics" - +15% Attack when 2+ primates in team

2. **Class Passive** (Automatic)
   - Granted based on `AnimalData.combatClass`
   - Example classes:
     - **Tank**: "Fortified" - +25% Defense, -10% Speed
     - **DPS**: "Aggressive" - +25% Attack, -10% Defense
     - **Support**: "Protective Aura" - Allies within 2 tiles gain +10% Defense
     - **Utility**: "Versatile" - +10% all stats

3. **Happiness Passive** (Unlocked by Happiness Threshold)
   - **60+ Happiness**: "Content" - +10% all stats
   - **80+ Happiness**: "Joyful" - +20% all stats, +5% crit chance
   - **100 Happiness**: "Blissful" - +35% all stats, +10% crit chance, skills cost -1 cooldown

**Design Philosophy:**
- **Predictable**: Players always know what passives their animals have
- **Build-enabling**: Synergies between family/class passives create team strategies
- **Care-rewarding**: Happiness directly translates to combat power

---

## **🎯 Team Assembler UI**

### **UI Flow**

**Pre-Combat Screen:**
```
┌──────────────────────────────────────────────────────────┐
│  ASSEMBLE YOUR TEAM - Zone: Dark Forest                  │
│                                                           │
│  Available Animals:                                       │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                       │
│  │ 🐔  │ │ 🐄  │ │ 🐵  │ │ 🐺  │ ... (scrollable)     │
│  │Cluck│ │Bessie│ │Mojo│ │Luna│                        │
│  │ ❤️50 │ │ ❤️85 │ │❤️100│ │ ❤️70 │                     │
│  │Need │ │Need │ │Fed✓│ │Need │                       │
│  │3🌾  │ │3🌿  │ │     │ │3🍖  │                       │
│  └─────┘ └─────┘ └─────┘ └─────┘                       │
│                                                           │
│  Combat Grid (9x5 - Drag animals here):                  │
│  ┌──┬──┬──┬──┬──┬──┬──┬──┬──┐  ← Back Row (3)          │
│  │  │  │🐵│  │  │  │  │  │  │                           │
│  ├──┼──┼──┼──┼──┼──┼──┼──┼──┤  ← Middle Row (2)         │
│  │  │🐺│  │👤│  │  │  │  │  │                           │
│  ├──┼──┼──┼──┼──┼──┼──┼──┼──┤  ← Front Row (1)          │
│  │🐔│  │  │  │  │🐄│  │  │  │                           │
│  └──┴──┴──┴──┴──┴──┴──┴──┴──┘                           │
│                                                           │
│  Active Synergies & Passives:                            │
│  ⚡ Galliformes x1: Flock Instinct (inactive - need 2+)  │
│  🦬 Bovidae x1: Herd Strength (inactive - need 2+)       │
│  🐵 Primates x1: Pack Tactics (inactive - need 2+)       │
│  🛡️ Tank x2: All Tanks +25% DEF                          │
│  ⚔️ DPS x1: +25% ATK, -10% DEF                           │
│  😊 High Happiness Bonus: 2 animals "Joyful" tier        │
│                                                           │
│  Required Food: 3🌾 Wheat, 3🌿 Hay, 3🍖 Meat             │
│  [Feed All] [Clear Grid] [Start Battle]                 │
└──────────────────────────────────────────────────────────┘
```

**Food Icons Reference:**
- 🌾 Wheat (Chickens, Birds)
- 🌿 Hay/Grass (Cows, Sheep, Horses)
- 🍖 Meat (Dogs, Wolves, Cats)
- 🍌 Banana (Monkeys, Primates)
- 🐟 Fish (Cats, Penguins)
- 🌽 Corn (Pigs)
- 🍎 Apple (Horses)

### **Animal Selection Panel**

**Display for Each Animal:**
- **Portrait** (from `AnimalData.idleSprite`)
- **Name** (custom name or breed)
- **Happiness** (❤️ icon with percentage)
- **Feed Status**:
  - "Need 3🌾 Wheat" - not fed yet, shows required food
  - "Fed ✓" - already fed, ready to recruit
  - "Hungry ⚠️" - needs feeding (daily requirement not met)
  - "Missing Food ❌" - you don't have required food in inventory
- **Alternative Foods** (if applicable):
  - "Accepts: 3🌾 Wheat OR 5🌱 Seeds"
- **Stats Preview** (hover tooltip):
  ```
  Cluck the Chicken
  ⚔️ Attack: 15.2
  🛡️ Defense: 10.5
  ⚡ Speed: 18.0
  ❤️ Health: 100
  😊 Happiness: 85% (x1.35 multiplier)

  Food: Wheat (3 required)
  Accepts: Wheat, Seeds (5)
  ```

### **Grid Positioning**

**Drag & Drop System:**
- **Drag animals** from selection panel to grid
- **Visual feedback**:
  - Valid placement: Green highlight
  - Invalid: Red highlight
  - Occupied: Yellow "swap" prompt
- **Remove animals**: Drag back to panel or right-click

**Position Strategy Tips:**
- **Front Row**: High defense (Tanks)
- **Back Row**: High damage (DPS), Support

### **Feed to Recruit System**

**Feeding Requirement:**
- **Each animal requires SPECIFIC food** (from `AnimalData.dailyFoodRequirements`)
- **Cost per animal**: 3 units of their preferred food
- **No generic "Hay" shortcut** - Must farm/buy the exact food type

**Example Animal Food Requirements:**

| Animal Type | Required Food (3 units) | Where to Get |
|---|---|---|
| **Chicken** | Wheat | Grow on farm (Spring/Fall crop) |
| **Cow** | Grass/Hay | Grow on farm (Summer crop) or scythe grass |
| **Monkey** | Banana | Grow on farm (Summer crop) or buy from tropical merchant |
| **Dog** | Meat | Purchase from butcher or hunt in zones |
| **Cat** | Fish | Fishing or purchase from fishmonger |
| **Sheep** | Clover | Grow on farm (Spring crop) |
| **Pig** | Corn | Grow on farm (Summer/Fall crop) |
| **Horse** | Apple | Grow on farm (Fall crop) or buy from orchard |

**Premium Feeding Bonuses (Optional):**
```
Golden Wheat (Rare): +15% ATK for chicken this battle
Diamond Banana (Epic): +30% all stats for monkey this battle
Blessed Fish (Legendary): +50% XP gain for cat this battle + guaranteed skill scroll drop
```

**Multiple Food Preferences:**
- Some animals accept **2-3 different foods** for variety
- Example: Dog accepts Meat (primary) OR Bone (secondary, requires 4 instead of 3)
- Example: Chicken accepts Wheat (primary) OR Seeds (secondary, requires 5 instead of 3)

**Economy Balance:**
- **Deep farming integration**: Must plan crop rotation for combat team
- **Creates tension**: "Do I sell this wheat for 100g or save 3 for my chicken?"
- **Diverse farming**: Encourages growing variety of crops, not just cash crops
- **Progression curve**:
  - Early game = 1-2 animals with easy crops (wheat, grass)
  - Mid game = 4-6 animals requiring seasonal planning
  - Late game = 8-12 animals requiring advanced farming + buying rare foods

**Feeding UI:**
- **"Feed All" button**: Automatically feeds all selected animals if you have their required foods
  - Displays total cost: "Feed All (9 Wheat, 6 Hay, 3 Banana)"
- **Individual "Feed" button**: Feed one animal by selecting them + item in inventory
- **Food requirement display**: Each animal card shows "Needs: 3x Wheat" or "Fed ✓"
- **Insufficient food warning**:
  - "Missing: 2x Banana for Mojo"
  - "Cannot recruit all animals - missing ingredients"
- **Smart food detection**: If animal accepts multiple foods, shows cheapest option first

**Feeding Benefits:**
- **Required to recruit** animal for this battle
- **Optional bonus**: Feeding premium food gives temporary combat buff
  - Example: Feed Golden Wheat → +15% ATK for chicken this battle
  - Example: Feed Blessed Fish → +50% XP for cat + guaranteed rare drop

**Food Quality Tiers:**
```
Normal Food (3 units):     No bonus
High-Quality (2 units):    +10% all stats for this battle
Premium (1 unit):          +25% all stats for this battle
Legendary (1 unit):        +50% all stats + special effect
```

### **Synergy Display**

**Active Synergies Display:**
```
FAMILY PASSIVES:
⚡ Galliformes x3: "Flock Instinct" active (+15% Speed per bird)
🦬 Bovidae x2: "Herd Strength" active (+20% Defense per cattle)

CLASS PASSIVES:
🛡️ Tank x2: "Fortified" (+25% DEF, -10% Speed each)
⚔️ DPS x3: "Aggressive" (+25% ATK, -10% DEF each)

HAPPINESS PASSIVES:
😊 Content (60-79): 1 animal (+10% all stats)
🎉 Joyful (80-99): 2 animals (+20% all stats, +5% crit)
✨ Blissful (100): 1 animal (+35% all stats, +10% crit, -1 CD)

SEASONAL BONUSES:
🌸 Spring Animals x2: +20% ATK, +10% DEF, +15% Speed
```

**Synergy Calculation** (to implement):
```csharp
public class PassiveSkillManager {
    public void ApplyAllPassives(List<CombatUnit> team) {
        // 1. Apply Family Passives
        Dictionary<string, int> familyCounts = GetFamilyCounts(team);
        foreach (var unit in team) {
            ApplyFamilyPassive(unit, familyCounts[unit.animal.AnimalData.animalFamily]);
        }

        // 2. Apply Class Passives
        foreach (var unit in team) {
            ApplyClassPassive(unit);
        }

        // 3. Apply Happiness Passives
        foreach (var unit in team) {
            ApplyHappinessPassive(unit);
        }

        // 4. Apply Seasonal Bonuses (if applicable)
        ApplySeasonalBonuses(team);
    }
}
```

### **Start Battle Flow**

**Validation Checks:**
1. ✅ At least 1 animal recruited (fed)
2. ✅ Player character positioned
3. ✅ All selected animals have valid grid positions

**Pre-Battle Confirmation:**
```
Ready to battle?
- 3 Animals recruited
- 15🌾 Hay consumed
- Synergies: 2 active

[Confirm] [Back]
```

---

## **🐾 Animal System Integration**

### **Existing Animal Stats → Combat**

**Direct Stat Usage:**
- `AnimalCombatStats.CurrentAttack` → Damage output
- `AnimalCombatStats.CurrentDefense` → Damage reduction
- `AnimalCombatStats.CurrentSpeed` → Turn order
- `AnimalCombatStats.MaxHealth` → HP pool
- `AnimalCombatStats.CurrentAccuracy` → Hit chance
- `AnimalCombatStats.happiness` → Stat multiplier (0.5x to 1.5x)

**Farming Activities → Combat Strength:**

| Farming Action | Combat Benefit | Already Implemented? |
|---|---|---|
| Daily petting | +1% all stats (cumulative) | ✅ Yes |
| Proper feeding | +2% ATK/DEF (cumulative) | ✅ Yes |
| High-quality production | +5% DEF/HP (cumulative) | ✅ Yes |
| Seasonal match | +20% ATK, +10% DEF, +15% Speed | ✅ Yes |

**Happiness Impact:**
- **100 Happiness** → 1.5x multiplier (max)
- **50 Happiness** → 1.0x multiplier (neutral)
- **20 Happiness** → 0.7x multiplier (min, from decay floor)

This creates **meaningful farming → combat loop**: Players must care for animals daily to maximize combat power.

### **Animal Acquisition Sources**

**1. Purchase from NPCs:**
- **Animal Shops**: Basic animals available for gold
  - Example: "Chicken Coop" NPC sells chickens for 500g
- **Prices scale** with animal rarity/base stats

**2. Quest Rewards:**
- **Main story quests**: Unique animals with special skills
- **NPC relationship quests**: Gift animals at friendship milestones
  - Example: "Reach 5 hearts with Emily → She gifts you her pet monkey"

**3. Future Expansion (Post-MVP):**
- Breeding system
- Rare encounters in combat zones
- Event-exclusive animals

### **Animal Roster Management**

**Barn/Housing System:**
- **Capacity limit**: Start with 10 animal slots, upgradeable
- **Animals stored in barn** when not in combat
- **Can view/manage all owned animals** in Farm UI

**Animal Management UI:**
- View all owned animals
- See combat stats, happiness, growth tracking
- Rename animals
- Release animals (free up space)

---

## **🎴 Passive Skill & Synergy System (CORE MECHANIC)**

> **Design Philosophy**: Synergies are the heart of team building. Every passive skill should feel POWERFUL and build-defining. Players should plan teams around maximizing synergy combinations.

### **1. Family Passive Skills**

**ALWAYS ACTIVE** - Every animal has their family passive automatically.

**Family passives SCALE** with number of same-family members in team:

| Family | 1 Animal (Solo) | 2 Animals | 3 Animals | 4+ Animals |
|---|---|---|---|---|
| **Galliformes** (Birds) | +10% Speed | +15% Speed to all birds | +25% Speed, +15% ATK to all birds | +35% Speed, +25% ATK to all birds, Birds act first each round |
| **Bovidae** (Cattle) | +15% DEF | +25% DEF to all cattle | +40% DEF, +20% HP to all cattle | +60% DEF, +30% HP to all cattle, Cattle gain "Immovable" (cannot be knocked back) |
| **Primates** (Monkeys) | +12% ATK | +20% ATK to all primates | +30% ATK, +15% Speed to all primates | +45% ATK, +25% Speed to all primates, Primates attack twice per turn |
| **Canidae** (Dogs/Wolves) | +10% all stats | +15% all stats to all canines | +25% all stats, +10% accuracy to all canines | +40% all stats, +20% accuracy, Canines counter-attack when hit |
| **Felidae** (Cats) | +15% crit chance | +25% crit chance to all felines | +35% crit chance, +50% crit damage to all felines | +50% crit chance, +100% crit damage, Felines always crit on first attack |

**Special Family Effects (4+ animals):**
- **Overpowered on purpose** - Encourages committing to a family
- **High risk, high reward** - Mono-family team loses diversity but gains massive power
- **Visual feedback** - Special aura/particle effects when 4+ threshold reached

### **2. Class Passive Skills**

**ALWAYS ACTIVE** - Every animal has their class passive automatically.

**Class passives provide STRONG identity:**

| Class | Base Effect (Always Active) | Synergy Bonus (2+ of same class) |
|---|---|---|
| **Tank** | +35% DEF, +25% HP, -15% Speed<br>Taunt: 30% chance enemies target this unit | 2+: All Tanks gain "Guardian" - Absorb 20% of damage dealt to adjacent allies<br>3+: Tanks become "Immovable" - Cannot be stunned or knocked back |
| **DPS** | +40% ATK, +15% Crit Chance, -15% DEF<br>Bloodlust: +5% ATK for each enemy killed | 2+: All DPS gain "Execute" - Deal 50% bonus damage to enemies below 30% HP<br>3+: DPS gain "Rampage" - On kill, immediately take another turn |
| **Support** | +20% all stats<br>Healing Aura: Heals adjacent allies for 5% max HP per turn | 2+: All Supports gain "Mass Heal" - Healing applies to all allies within 3 tiles<br>3+: Supports gain "Resurrection" - Can revive one dead ally per battle |
| **Utility** | +15% all stats, +20% Accuracy<br>Versatile: Active skills cost -1 cooldown | 2+: All Utility gain "Flexibility" - Can use active skill targeting both allies AND enemies<br>3+: Utility gain "Master Tactician" - Active skills cost -2 cooldown and affect 2 targets |

**Class Synergy Philosophy:**
- **Tank**: Protect team, absorb damage
- **DPS**: Kill fast, snowball with kills
- **Support**: Keep team alive, provide utility
- **Utility**: Flexible, skill-focused

### **3. Happiness Passive Skills**

**UNLOCKED BY HAPPINESS THRESHOLD** - Rewards caring for animals.

| Happiness Range | Passive Name | Effect |
|---|---|---|
| **0-59** (Unhappy) | "Demoralized" | -15% all stats, -10% accuracy, 20% chance to skip turn |
| **60-79** (Content) | "Focused" | +15% all stats, +5% accuracy |
| **80-99** (Joyful) | "Inspired" | +30% all stats, +10% crit chance, +15% accuracy |
| **100** (Blissful) | "Transcendent" | +50% all stats, +20% crit chance, +25% accuracy, Active skills cost -2 cooldown, Immune to debuffs |

**Happiness Impact:**
- **MASSIVE difference** between 0 and 100 happiness
- **Caring for animals is REQUIRED** for competitive teams
- **Visual feedback**: Happy animals glow, unhappy animals look tired

### **4. Seasonal Bonuses**

**AUTOMATIC** - Based on current in-game season + animal's preferred season.

**In Preferred Season:**
- **+40% ATK, +25% DEF, +30% Speed** (VERY STRONG)
- **Season-Specific Bonus**:
  - **Spring**: +20% Healing received
  - **Summer**: +15% Crit damage
  - **Fall**: +25% Gold/XP from battles
  - **Winter**: +20% Damage reduction

**Team Seasonal Synergy:**
- **3+ animals in preferred season**: "Seasonal Harmony" - All team members gain +20% all stats
- **All animals in preferred season**: "Perfect Season" - All team members gain +40% all stats, +50% XP

### **5. Combo Synergies (Advanced)**

**Ultra-Powerful combos when multiple synergies align:**

**Family + Class Combos:**
- **3+ Galliformes + 3+ DPS**: "Aerial Assault" - Birds dive-bomb enemies for 200% damage on first attack
- **3+ Bovidae + 3+ Tank**: "Stampede Wall" - Cattle form impenetrable defense, reflecting 50% damage back to attackers
- **3+ Primates + 3+ Utility**: "Tactical Genius" - Monkeys reduce ALL cooldowns by 3 turns at battle start

**Happiness + Family Combos:**
- **All animals 100 happiness + 4+ same family**: "Unstoppable Force" - Team gains +100% all stats for first 5 turns

**Season + Family Combos:**
- **All animals in preferred season + mono-family team**: "Apex Predator" - Team deals 3x damage and takes 0.5x damage

### **Synergy Calculation Architecture**

```csharp
public class PassiveSkillManager {
    public void ApplyAllPassives(List<CombatUnit> team) {
        // 1. Count family/class occurrences
        var familyCounts = GetFamilyCounts(team);
        var classCounts = GetClassCounts(team);

        // 2. Apply Family Passives (scaled by count)
        foreach (var unit in team) {
            string family = unit.animal.AnimalData.animalFamily;
            int count = familyCounts[family];
            ApplyFamilyPassive(unit, family, count);
        }

        // 3. Apply Class Passives (base + synergy)
        foreach (var unit in team) {
            string combatClass = unit.animal.AnimalData.combatClass;
            int count = classCounts[combatClass];
            ApplyClassPassive(unit, combatClass, count);
        }

        // 4. Apply Happiness Passives
        foreach (var unit in team) {
            float happiness = unit.animal.GetHappiness();
            ApplyHappinessPassive(unit, happiness);
        }

        // 5. Apply Seasonal Bonuses
        ApplySeasonalBonuses(team);

        // 6. Check for Combo Synergies (Advanced)
        CheckComboSynergies(team, familyCounts, classCounts);
    }

    private void CheckComboSynergies(List<CombatUnit> team,
        Dictionary<string, int> familyCounts,
        Dictionary<string, int> classCounts) {

        // Check for ultra-powerful combo synergies
        // Example: 3+ Galliformes + 3+ DPS = "Aerial Assault"
        if (familyCounts["Galliformes"] >= 3 && classCounts["DPS"] >= 3) {
            ApplyComboSynergy(team, "Aerial Assault");
        }

        // Add more combos as needed...
    }
}
```

### **Synergy Design Goals**

✅ **Build-Defining**: Players construct entire teams around synergies
✅ **High Power Fantasy**: Synergies feel STRONG and exciting
✅ **Strategic Depth**: Tradeoffs between mono-family (max synergy) vs. diverse (flexibility)
✅ **Farming Integration**: Happiness passives reward daily animal care
✅ **Seasonal Variety**: Meta shifts with seasons, keeping gameplay fresh

---

## **💞 Dating Sim Integration**

### **NPC → Animal Pipeline**

**1. NPCs Gift Animals:**

**Relationship Milestones:**
```
❤️ 2 Hearts: NPC mentions they have a pet
❤️ 4 Hearts: Special event - meet the NPC's animal
❤️ 6 Hearts: NPC gifts you their animal (or egg/baby)
❤️ 8 Hearts: NPC teaches you advanced care techniques
❤️ 10 Hearts (Max): Unlock NPC's signature animal skill
```

**Example Flow:**
```
Player befriends "Farmer Emily"
→ At 4 hearts: Cutscene where Emily shows her prized monkey "Mojo"
→ At 6 hearts: Emily says "I trust you with Mojo. Take good care of him!"
→ Player receives Mojo (unique stats/skill)
```

**NPC-Specific Animals:**
- Each romanceable NPC has **signature animal**
- These animals may have **exclusive skills** or **higher base stats**
- Encourages players to pursue all relationships

**2. NPCs Teach Player Skills:**

**Combat Class Training:**
```
NPC "Knight Roland" → Teaches "Warrior" class
- Requirement: 5 hearts relationship
- Quest: "Prove Your Strength" (win 3 battles)
- Reward: Unlock Warrior class for player character
```

**Skill Unlocks:**
```
NPC "Mage Aria" → Teaches "Fireball" active skill
- Requirement: 8 hearts relationship
- Quest: "Study Magic Tomes" (collect 3 rare books)
- Reward: Unlock Fireball skill (high damage AOE)
```

**3. Romance Benefits:**

**Dating Bonuses:**
- **Current Partner**: If romancing an NPC, gain passive buff
  - Example: Dating Emily → "Farmer's Blessing" - +5% farming stat growth
- **Married**: Permanent stronger buff
  - Example: Married to Emily → +10% farming growth, +5% animal happiness gain

**Romantic Events:**
- **Battle Dates**: Take your partner to watch your combat (cutscene)
- **Gift Exchanges**: Partner gives rare combat items on special days
- **Support Cheers**: During combat, partner appears as spectator giving temporary buff

### **Integration with Existing Dialogue System**

**Leverage Existing System:**
- Use `DialogueTree` to create relationship progression dialogues
- `DialogueEffect` can trigger "Gift Animal" action
- `DialogueCondition` checks combat victories for quest requirements

**Example Dialogue Effect:**
```csharp
// In DialogueEffect.cs
public enum EffectType {
    // ... existing effects
    GiftAnimal,        // NEW: Give player an animal
    TeachPlayerSkill,  // NEW: Unlock player combat skill
    UnlockCombatZone   // NEW: Open new combat area
}

// Implementation
case EffectType.GiftAnimal:
    AnimalManager.Instance.GiftAnimalToPlayer(effectValue);
    break;
```

---

## **📈 Progression Systems**

### **Animal Leveling**

**Experience Sources:**
1. **Combat XP**: Animals gain XP per battle
   - Victory: 100 XP
   - Defeat: 50 XP
   - Per enemy defeated: 20 XP
   - Damage dealt: 0.1 XP per point

2. **Farming XP** (indirect via stat growth):
   - Already implemented via growth multipliers

**Level Up Benefits:**
```
Level Up → Choose one:
- +5 Base Attack
- +5 Base Defense
- +5 Base Speed
- +10 Base Health
```

**Level Thresholds:**
```
Level 1→2: 100 XP
Level 2→3: 200 XP
Level 3→4: 350 XP
Formula: XP = 100 * (level^1.5)
```

### **Player Character Progression**

**Player Level** (already exists: `PlayerGameData.playerLevel`):
- **XP from combat**: 50 XP per battle won
- **XP from quests**: Varies by quest
- **Level up**: Unlock skill points, new equipment slots

**Combat Skill Tree** (future expansion):
- Spend points on passive bonuses
- Unlock new active skills
- Specialize in combat classes

### **Unlock Progression**

**Combat Zones Unlocked by:**
1. **Story Progression**: Complete main quests
2. **Relationship Progression**: NPC unlocks new area
3. **Combat Victories**: Win X battles to unlock harder zones

**Example Progression:**
```
Zone 1: "Peaceful Meadow" (Tutorial)
   ↓ Win 3 battles
Zone 2: "Dark Forest" (Unlocked)
   ↓ Reach 5 hearts with Ranger NPC
Zone 3: "Haunted Ruins" (Unlocked)
   ↓ Win 10 total battles + Main Quest
Zone 4: "Dragon's Lair" (Boss Fight)
```

---

## **💰 Economy & Resources**

### **Feeding Cost System**

**Per-Battle Recruitment Fee:**
- **Each animal requires 3 units of SPECIFIC food** (no generic Hay shortcut)
- **Food types match animal's diet** (from `AnimalData.dailyFoodRequirements`)
- **Examples**:
  - Chicken: 3x Wheat
  - Cow: 3x Hay/Grass
  - Monkey: 3x Banana
  - Dog: 3x Meat
  - Cat: 3x Fish

**Food Acquisition Sources:**

| Food Type | Primary Source | Secondary Source | Difficulty |
|---|---|---|---|
| Wheat | Grow on farm (Spring/Fall) | Buy from grain merchant (50g each) | Easy |
| Hay/Grass | Grow on farm (Summer) or scythe wild grass | Buy from ranch (30g each) | Easy |
| Banana | Grow on farm (Summer, tropical) | Buy from exotic merchant (100g each) | Medium |
| Meat | Hunt in combat zones | Buy from butcher (150g each) | Hard |
| Fish | Fishing minigame | Buy from fishmonger (80g each) | Medium |
| Corn | Grow on farm (Summer/Fall) | Buy from grain merchant (60g each) | Easy |
| Apple | Grow on farm (Fall) | Buy from orchard (70g each) | Medium |

**Premium Food Quality Tiers:**

| Quality | Cost to Obtain | Battle Buff | Drop Rate |
|---|---|---|---|
| Normal | 3 units | No bonus | N/A |
| High-Quality | 2 units (harder to grow) | +15% all stats this battle | 10% from perfect crops |
| Premium | 1 unit (very rare) | +30% all stats this battle | 3% from golden crops |
| Legendary | 1 unit (quest rewards) | +50% stats + bonus XP/drops | Quest/NPC gifts only |

**Food Variety Benefits:**
- **Diverse team = diverse farming**: Encourages crop rotation
- **Seasonal strategy**: Build teams around current season's crops
  - Spring team: Wheat-eating animals (Chickens, Birds)
  - Summer team: Hay/Banana animals (Cows, Monkeys)
  - Fall team: Corn/Apple animals (Pigs, Horses)
- **Trade-offs**: Powerful rare animals need expensive food (Meat, Fish)

**Economy Balance:**
- **Deep farming integration**: Must plan weeks ahead for combat teams
- **Creates tension**:
  - "Do I sell 10 Wheat for 500g or save for 3 battles?"
  - "Should I grow cash crops or combat food?"
- **NPC relationships matter**: High-friendship NPCs may gift rare foods
- **Progression curve**:
  - Early: 1-2 animals, easy crops (Wheat, Hay) = 180g worth
  - Mid: 4-6 animals, mixed crops = 600g worth + farming time
  - Late: 8-12 animals, rare foods = 2000g+ worth OR advanced farming

### **Combat Rewards**

**Victory Rewards:**

| Reward Type | Amount/Chance |
|---|---|
| **Gold** | 100-500g (scales with zone difficulty) |
| **Items** | 1-3 random items (seeds, materials, rare food) |
| **Animal XP** | 100 XP per animal |
| **Player XP** | 50 XP |
| **Quest Progress** | Depends on active quests |

**Rare Drops:**
- **Skill Scrolls**: Teach new animal skills (5% drop rate)
- **Evolution Stones**: Boost animal stats permanently (2% drop rate)
- **Cosmetic Items**: Animal accessories (10% drop rate)

**First-Time Clear Bonuses:**
- **Zone Clear Rewards**: 1000g + Unique Item
- **Achievement**: "Zone Name Conqueror"

### **Resource Sinks**

**What Combat Consumes:**
1. **Food for recruitment** (primary sink)
2. **Healing items** (if animals take damage, need healing before next battle - future)
3. **Revival items** (if animal dies, need to revive - future)

**What Combat Produces:**
1. **Gold** (for purchasing animals, upgrades)
2. **Rare items** (for crafting, gifting to NPCs)
3. **Quest progress** (for story advancement)

---

## **🏗️ Technical Architecture**

### **Scene Structure**

```
Scenes/
├── FarmScene.unity           (Main farming gameplay)
├── TeamAssemblerScene.unity  (Pre-combat setup UI)
└── CombatScene.unity         (Auto-chess battle)
```

**Scene Flow Manager:**
```csharp
public class CombatSceneManager : MonoBehaviour {
    public void InitiateCombat(CombatZoneData zoneData) {
        // Save current farm state
        SaveManager.Instance.SaveGame();

        // Load Team Assembler with zone info
        SceneTransitionManager.Instance.LoadScene(
            "TeamAssemblerScene",
            zoneData
        );
    }
}
```

### **Core Classes to Implement**

**1. BattleManager.cs**
```csharp
public class BattleManager : MonoBehaviour {
    public GridManager gridManager;
    public TurnManager turnManager;
    public SynergyManager synergyManager;

    public void StartBattle(List<Animal> playerTeam, List<Enemy> enemies) {
        // Initialize grid
        gridManager.SpawnUnits(playerTeam, enemies);

        // Calculate synergies
        synergyManager.CalculateTeamSynergies(playerTeam);

        // Start turn loop
        turnManager.BeginCombat();
    }
}
```

**2. GridManager.cs**
```csharp
public class GridManager : MonoBehaviour {
    public GridCell[,] grid; // 7x4 grid

    public void SpawnUnits(List<Animal> playerTeam, List<Enemy> enemies) {
        // Place units at assigned grid positions
    }

    public List<CombatUnit> GetTargetsInRange(CombatUnit attacker, int range) {
        // Calculate valid targets based on grid distance
    }
}
```

**3. TurnManager.cs**
```csharp
public class TurnManager : MonoBehaviour {
    public void BeginCombat() {
        while (!CheckWinCondition()) {
            List<CombatUnit> turnOrder = GetTurnOrder(); // Sort by speed

            foreach (var unit in turnOrder) {
                if (unit.IsAlive) {
                    ExecuteTurn(unit);
                }
            }
        }
    }

    private void ExecuteTurn(CombatUnit unit) {
        // Check for active skill usage
        // Select target
        // Execute action
        // Apply damage/effects
    }
}
```

**4. CombatUnit.cs**
```csharp
public class CombatUnit : MonoBehaviour {
    public Animal animal; // Reference to Animal data
    public float currentHealth;
    public Vector2Int gridPosition;

    public void TakeDamage(float damage) {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    public void UseActiveSkill(CombatUnit target) {
        AnimalSkill skill = animal.GetActiveSkill();
        // Execute skill effect
    }
}
```

**5. TeamAssemblerUI.cs**
```csharp
public class TeamAssemblerUI : MonoBehaviour {
    public List<Animal> availableAnimals;
    public List<Animal> selectedAnimals;

    public void FeedAnimal(Animal animal) {
        // Check inventory for food
        // Deduct food
        // Mark animal as recruited
    }

    public void StartBattle() {
        // Validate team
        // Pass selected animals to CombatScene
        SceneTransitionManager.Instance.LoadScene("CombatScene", selectedAnimals);
    }
}
```

### **Data Persistence**

**Combat Session Data:**
```csharp
[System.Serializable]
public class CombatSessionData {
    public List<string> selectedAnimalIDs; // Animal unique IDs
    public Dictionary<string, Vector2Int> gridPositions;
    public Vector2Int playerPosition;
    public string combatZoneID;
    public int difficultyLevel;
}
```

**Save After Combat:**
```csharp
// In BattleManager.cs
public void EndBattle(bool victory) {
    // Award XP to animals
    foreach (var animal in playerTeam) {
        animal.combatStats.experience += 100;
    }

    // Save game state
    SaveManager.Instance.SaveGame();

    // Return to farm
    SceneTransitionManager.Instance.LoadScene("FarmScene");
}
```

### **Integration with Existing Systems**

**Leverage Existing Code:**
- ✅ `Animal.cs` → Already has all combat stats
- ✅ `AnimalCombatStats.cs` → Complete stat calculation
- ✅ `AnimalSkill.cs` → Skill data structure ready
- ✅ `GameData.cs` → `CombatGameData` section ready
- ✅ `SaveManager.cs` → Save/load infrastructure exists
- ✅ `SceneTransitionManager.cs` → Scene loading ready

**New Dependencies:**
- 🔨 Grid visualization (hex or rectangular tilemap)
- 🔨 Combat VFX (damage numbers, skill effects, hit animations)
- 🔨 Combat UI (health bars, turn indicator, skill cooldowns)
- 🔨 Team Assembler drag-and-drop system

---

## **🎯 Implementation Phases**

### **Phase 1: MVP Combat System (Core Loop)**

**Goal:** Playable combat from start to finish

**Tasks:**
1. ✅ Animal combat stats (DONE)
2. 🔨 Create CombatScene with basic grid (7x4)
3. 🔨 Implement BattleManager + TurnManager
4. 🔨 Basic combat loop (attack → damage → death)
5. 🔨 Simple enemy AI
6. 🔨 Victory/defeat conditions
7. 🔨 Basic combat UI (health bars, turn order)
8. 🔨 Team Assembler UI (select 3 animals, position on grid)
9. 🔨 Feed-to-recruit system (deduct hay from inventory)
10. 🔨 Combat trigger zone in farm (collider area)
11. 🔨 Reward system (gold + XP)

**Success Criteria:**
- Player can select 3 animals, feed them, position on grid
- Battle executes automatically
- Player wins/loses based on battle outcome
- Animals gain XP after battle
- Player returns to farm with rewards

**Estimated Time:** 3-4 weeks

---

### **Phase 2: Skills & Synergies**

**Goal:** Deep tactical combat with meaningful choices

**Tasks:**
1. 🔨 Implement active skill usage in combat
2. 🔨 Implement passive skill effects
3. 🔨 Family synergy calculation
4. 🔨 Class synergy calculation
5. 🔨 Synergy display in Team Assembler UI
6. 🔨 Skill VFX (particles, animations)
7. 🔨 Cooldown tracking UI
8. 🔨 Target selection logic for skills (AOE, single-target, ally)

**Success Criteria:**
- Animals use active skills automatically when off cooldown
- Passive skills apply correctly
- Synergies display and calculate correctly
- Players can see skill effects visually

**Estimated Time:** 2-3 weeks

---

### **Phase 3: Player Character Combat**

**Goal:** Player fights alongside animals

**Tasks:**
1. 🔨 Create PlayerCombatUnit class (similar to CombatUnit)
2. 🔨 Add player to Team Assembler grid
3. 🔨 Implement player active skills
4. 🔨 Create player skill UI (manual trigger option? or also auto?)
5. 🔨 Player visual representation in combat
6. 🔨 Player equipment system (future-proofing)

**Success Criteria:**
- Player character appears on grid
- Player has combat stats (scales with player level)
- Player contributes to battle damage/healing

**Estimated Time:** 1-2 weeks

---

### **Phase 4: NPC Integration & Animal Acquisition**

**Goal:** Dating sim → combat pipeline functional

**Tasks:**
1. 🔨 NPC animal shop UI (purchase animals)
2. 🔨 DialogueEffect: GiftAnimal implementation
3. 🔨 DialogueEffect: TeachPlayerSkill implementation
4. 🔨 Quest system integration (combat objectives)
5. 🔨 Relationship milestones → animal unlocks
6. 🔨 Animal roster management UI (view all owned animals)

**Success Criteria:**
- Player can buy animals from shops
- NPCs gift animals at relationship milestones
- Quests can require combat victories
- High-relationship NPCs teach player skills

**Estimated Time:** 2-3 weeks

---

### **Phase 5: Multiple Combat Zones & Progression**

**Goal:** Varied combat experiences with progression curve

**Tasks:**
1. 🔨 Create 4-5 combat zones with unique enemies
2. 🔨 Zone unlock system (via quests, battles, relationships)
3. 🔨 Enemy variety (different stats, skills, classes)
4. 🔨 Boss battles (unique mechanics)
5. 🔨 Difficulty scaling
6. 🔨 Zone-specific rewards
7. 🔨 Combat statistics tracking (wins, losses, enemies defeated)

**Success Criteria:**
- Multiple zones accessible with unique challenges
- Clear progression path
- Difficulty increases appropriately
- Rewards justify difficulty

**Estimated Time:** 3-4 weeks

---

### **Phase 6: Polish & Expansion**

**Goal:** Professional presentation and depth

**Tasks:**
1. 🔨 Combat VFX polish (hit effects, death animations, skill visuals)
2. 🔨 Combat music & SFX
3. 🔨 Advanced synergies (season-based, conditional)
4. 🔨 Premium feeding bonuses
5. 🔨 Rare combat drops (skill scrolls, evolution stones)
6. 🔨 Achievement system for combat
7. 🔨 Tutorial for combat system
8. 🔨 Balance tuning (damage numbers, XP curves, costs)

**Success Criteria:**
- Combat feels polished and juicy
- Audio enhances experience
- Tutorials explain mechanics clearly
- Game balance feels fair and engaging

**Estimated Time:** 2-3 weeks

---

## **📊 Success Metrics**

**Player Engagement:**
- **Combat Participation Rate**: % of players who engage with combat zones
  - Target: 70%+ of players try combat within first hour
- **Repeat Battle Rate**: % of players who return to combat after first battle
  - Target: 60%+ replay combat

**Farming → Combat Loop:**
- **Animal Care Correlation**: Do players who pet/feed daily win more battles?
  - Target: Clear positive correlation
- **Food Economy**: Are players producing enough food to sustain combat?
  - Target: Players maintain 50+ food items on average

**NPC Integration:**
- **Relationship-Driven Combat**: % of animals acquired via NPC gifts
  - Target: 30%+ of animals come from relationships
- **Skill Unlocks**: % of players who unlock player skills via NPCs
  - Target: 50%+ unlock at least 1 NPC skill

**Progression Satisfaction:**
- **Win Rate**: Overall player win rate in combat
  - Target: 60-70% (challenging but achievable)
- **XP Gain Perception**: Do players feel their animals grow stronger?
  - Target: Survey 4+/5 stars on "progression feels rewarding"

---

## **🎨 Visual Design Notes**

**Combat Grid Aesthetic:**
- **Hex Grid**: Stylized honeycomb pattern (fits farming theme)
- **Grid Highlights**:
  - Player side: Warm green/yellow tones
  - Enemy side: Cool red/purple tones
- **Unit Presentation**:
  - Animals displayed as chibi sprites on grid
  - Health bars above each unit
  - Status effect icons (buffs/debuffs)

**Team Assembler UI:**
- **Color Coding**:
  - Green: Ready to recruit
  - Yellow: Needs feeding
  - Red: Cannot recruit (capacity, missing)
- **Drag Visuals**: Ghost preview of animal while dragging
- **Synergy Highlights**: Glowing borders around animals contributing to synergies

**Combat VFX:**
- **Damage Numbers**: Pop-up floating text
- **Critical Hits**: Red numbers, extra particles
- **Healing**: Green numbers, sparkles
- **Skill Effects**: Unique per skill (fireball, shield bubble, etc.)

---

## **❓ Open Questions & Future Considerations**

**Questions Resolved in v2.0:**
1. ✅ **Grid Type**: 9x5 rectangular grid confirmed
2. ✅ **Speed Mechanics**: Turn gauge system - faster units act more frequently
3. ✅ **Passive Skill System**: Family + Class + Happiness (3 passives total)
4. ✅ **Synergy Power**: Elevated to core mechanic with strong bonuses

**Remaining Questions:**
1. **Player Control in Combat**: Fully hands-off, or allow manual skill triggers?
2. **Animal Death**: Permanent death, or just "knocked out" for battle?
3. **PvP**: Should there be player vs player combat eventually?
4. **Breeding**: Should animals be breedable for new combinations?
5. **Critical Hit System**: Should crits be percentage-based or guaranteed by passives?
6. **Status Effects**: Which status effects to implement first (stun, poison, shield, etc.)?

**Future Expansion Ideas:**
- **Item System**: Equipment for animals (saddles, armor, accessories)
- **Formations**: Preset team formations for quick assembly
- **Combo Attacks**: Animals with high friendship can chain skills
- **Spectator Mode**: Watch AI vs AI battles for rewards
- **Seasonal Tournaments**: Limited-time PvP events
- **Animal Fusion**: Combine animals for rare hybrids

---

## **📝 Summary**

This PRD defines a comprehensive **Auto-Chess Combat System** deeply integrated with **Farming Sim** and **Dating Sim** mechanics for Sowur Shield.

**Version 2.0 Key Features:**
- ✅ **9x5 Grid System**: Large battlefield with strategic positioning (front/middle/back rows)
- ✅ **Turn Gauge Combat**: Speed determines action frequency - 2x speed = twice as many turns
- ✅ **Powerful Synergies**: CORE mechanic with +60% stat bonuses, special abilities, combo effects
- ✅ **3-Passive System**: Family + Class + Happiness passives (predictable, build-defining)
- ✅ **Farming → Combat Loop**: Happiness directly impacts combat (up to +50% all stats at 100 happiness)
- ✅ **NPC Integration**: Relationships unlock animals, skills, and bonuses
- ✅ **Economy Balance**: Feed-to-recruit system (3 Hay per animal)
- ✅ **Clear Progression**: Animals level from combat, zones unlock via quests/relationships

**Power Fantasy Design:**
- **Mono-family teams**: +60% DEF, attack twice per turn, act first each round
- **Class synergies**: Resurrection, Execute, Rampage, Guardian abilities
- **100 Happiness**: +50% all stats, immune to debuffs, -2 cooldown on skills
- **Seasonal bonuses**: +40% ATK when in preferred season
- **Combo synergies**: 3x damage with perfect setup

**Implementation Path:**
1. **Phase 1**: MVP combat (3-4 weeks) → Playable loop
2. **Phase 2**: Skills & synergies (2-3 weeks) → Tactical depth
3. **Phase 3**: Player character (1-2 weeks) → Full team integration
4. **Phase 4**: NPC integration (2-3 weeks) → Dating sim pipeline
5. **Phase 5**: Multiple zones (3-4 weeks) → Content variety
6. **Phase 6**: Polish (2-3 weeks) → Professional quality

**Total Estimated Time: 14-19 weeks (3.5-5 months)**

---

**Next Steps:**
1. Review and approve PRD
2. Create technical design documents for Phase 1
3. Set up basic combat scene and grid system
4. Begin Team Assembler UI implementation
5. Iterate based on playtesting feedback

---

*End of PRD - Ready for Implementation*
