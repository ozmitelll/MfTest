# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 (6000.3.11f1) — **Modfall**. Session roguelike. C# 9.0, .NET Framework 4.7.1.  
No ECS. No DOTS. No uGUI/Canvas. No Singletons.

## Build & Run

Unity Editor only — no CLI build commands.

- **Solution:** `Modfall-Test.sln` (Rider / Visual Studio)
- **Tests:** `com.unity.test-framework` 1.6.0 — Unity Test Runner window

## Stack

| Layer | Tool |
|---|---|
| Render | URP 17.3.0, Deferred, PC profile |
| Input | New Input System 1.19.0 |
| UI | UI Toolkit only (UXML + USS) — never uGUI |
| Navigation | AI Navigation 2.0.11 |
| DI / Services | ServiceLocator (custom) |
| Events | EventBus (custom, generic) |
| Data | ScriptableObjects |
| Animation | не определено |

## Code Structure

```
Assets/!Game/
├── Scripts/
│   ├── Core/
│   │   ├── ServiceLocator.cs
│   │   ├── EventBus.cs
│   │   └── Bootstrap.cs
│   ├── Entities/
│   │   ├── Entity.cs                        ← base: HealthSystem + StatsSystem
│   │   ├── Player/
│   │   │   ├── Player.cs                    ← : Entity, config from PlayerConfig
│   │   │   └── Systems/
│   │   │       ├── PlayerMovementSystem.cs
│   │   │       ├── PlayerCombatSystem.cs
│   │   │       ├── PlayerModificationSystem.cs
│   │   │       ├── PlayerSkillSystem.cs
│   │   │       └── PlayerWallet.cs
│   │   ├── Enemy/
│   │   │   ├── Enemy.cs                     ← : Entity, config from EnemyConfig
│   │   │   └── Systems/
│   │   │       ├── EnemyBehaviourSystem.cs
│   │   │       ├── EnemyMovementSystem.cs
│   │   │       └── EnemyCombatSystem.cs
│   │   └── Boss/
│   │       └── Boss.cs                      ← : Enemy + ModificationSystem + SkillSystem
│   ├── Systems/
│   │   ├── Health/
│   │   │   ├── HealthSystem.cs
│   │   │   └── StatusEffectSystem.cs
│   │   ├── Stats/
│   │   │   └── StatsSystem.cs
│   │   ├── Combat/
│   │   │   ├── DamageSystem.cs
│   │   │   └── DamageType.cs               ← enum: Pure, Magical, Elemental
│   │   └── Modifications/
│   │       ├── ModificationSystem.cs
│   │       └── ModificationCard.cs
│   ├── Session/
│   │   ├── Game.cs                         ← controls full run/session
│   │   ├── Stage.cs
│   │   ├── SessionTimer.cs
│   │   ├── DifficultySystem.cs
│   │   └── DirectorSystem.cs               ← spawns enemies from LevelConfig.EnemyGroup only
│   ├── Level/
│   │   ├── Level.cs                        ← on level prefab, initializes on load
│   │   ├── Portal.cs                       ← interaction: spawns boss → charges → next stage
│   │   └── Chest.cs                        ← interaction: costs coins → drops ModificationCard
│   ├── Configs/
│   │   ├── PlayerConfig.cs                 ← ScriptableObject
│   │   ├── EnemyConfig.cs                  ← ScriptableObject
│   │   ├── StageConfig.cs                  ← ScriptableObject, holds LevelConfig[]
│   │   └── LevelConfig.cs                  ← ScriptableObject, holds EnemyGroup[]
│   └── UI/
│       └── Controllers/
│           ├── HUDController.cs
│           ├── PauseMenuController.cs
│           ├── ModificationPanelController.cs
│           └── GameOverScreenController.cs
├── UI/
│   ├── UXML/
│   │   ├── HUD.uxml
│   │   ├── PauseMenu.uxml
│   │   ├── ModificationPanel.uxml
│   │   └── GameOverScreen.uxml
│   └── USS/
│       ├── Common.uss                      ← CSS variables: --color-primary, --font-size-base...
│       ├── HUD.uss
│       └── Panels.uss
├── Scenes/
└── Settings/
```

## Architecture Rules

### General
- **No Singletons** — use ServiceLocator for services (AudioService, SceneService)
- **No ECS / DOTS**
- **No uGUI / Canvas** — UI Toolkit only
- Systems are MonoBehaviour components on the same GameObject as their owner Entity
- Configs are ScriptableObjects — never hardcode values

### ServiceLocator
Registers and resolves services only — not gameplay objects.  
Usage: `ServiceLocator.Get<AudioService>()`

### EventBus
Generic static bus. Used for cross-system communication.  
**Always unsubscribe in OnDisable.**

```csharp
void OnEnable()  => EventBus.Subscribe<BossDiedEvent>(OnBossDied);
void OnDisable() => EventBus.Unsubscribe<BossDiedEvent>(OnBossDied);
```

Events are plain structs:
```csharp
public struct BossDiedEvent    { public Boss Boss; }
public struct EnemyDiedEvent   { public Enemy Enemy; }
public struct OnTimerTick      { public float Time; }
```

### Key EventBus events

| Event | Publisher | Subscribers |
|---|---|---|
| `OnEnemyDied(Enemy)` | `Enemy` | `DirectorSystem`, `PlayerWallet` |
| `OnBossDied(Boss)` | `Boss` | `Portal` |
| `OnPortalCharged` | `Portal` | `Game` |
| `OnModificationPickedUp(ModificationCard)` | `ModificationCard` | `PlayerModificationSystem` |
| `OnChestOpened` | `Chest` | `HUDController` |
| `OnTimerTick(float)` | `SessionTimer` | `DifficultySystem` |
| `OnPlayerHealthChanged(float, float)` | `HealthSystem` | `HUDController` |
| `OnCoinsChanged(int)` | `PlayerWallet` | `HUDController` |
| `OnSessionEnded` | `Game` | `GameOverScreenController`, Audio |
| `OnPlayerDied` | `HealthSystem` | `Game`, `GameOverScreenController` |

### Entities
- `Entity` — base class with `HealthSystem` + `StatsSystem`
- `Player : Entity` — adds movement, combat, modifications, skills, wallet
- `Enemy : Entity` — adds behaviour, movement, combat. Drops coins on death (from EnemyConfig)
- `Boss : Enemy` — adds ModificationSystem (preset mods) + SkillSystem. Publishes `OnBossDied`

### Session / Game Loop
```
Game.StartSession()
  → LoadStage(stages[0])
    → Stage.GetRandomLevel()         ← picks one LevelConfig from StageConfig
      → SceneService.LoadLevel()
        → Level.Initialize()
          → spawn Player at playerSpawnPoint
          → spawn Chests at chestSpawnPoints
          → spawn Portal at portalSpawnPoint
```

Portal states: `Idle → WaitingForBoss → Charging → Ready`

### Director
`DirectorSystem` spawns enemies using credits over time.  
**Spawns only from `LevelConfig.EnemyGroup[]`** — never from a global pool.  
Difficulty scaling comes from `DifficultySystem` which ticks on `OnTimerTick`.

### Combat
- `DamageType`: `Pure` (ignores armor), `Magical` (vs MagicRes), `Elemental` (vs ElementalRes, triggers effects)
- `DamageSystem` computes final damage from stats and calls `HealthSystem.TakeDamage()`
- `StatusEffectSystem` handles DoT, debuffs, slows — per Entity

### UI (UI Toolkit)
- Each screen = one `.uxml` + one Controller class
- Controllers query elements via `rootVisualElement.Q<T>("name")`
- Controllers **never hold references to Entity** — data comes only through EventBus
- USS variables defined in `Common.uss`, imported in other USS files
- `UIDocument` prefabs live in `Assets/!Game/Prefabs/UI/`

## Naming Conventions

| Type | Convention | Example |
|---|---|---|
| Systems | `[Owner][Function]System` | `PlayerMovementSystem` |
| Configs | `[Subject]Config` | `EnemyConfig` |
| Events | `On[Subject][Action]` | `OnBossDied` |
| UI Controllers | `[Screen]Controller` | `HUDController` |
| UXML/USS | PascalCase, match controller | `HUD.uxml` / `HUD.uss` |
| ScriptableObjects | Configs in `Assets/!Game/Configs/` | — |

## Compiler Settings

- Warnings 0169 and 0649 suppressed
- Unsafe code disabled