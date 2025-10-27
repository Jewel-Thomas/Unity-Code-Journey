// Unity Design Pattern Example: VehicleUpgradeSystem
// This script demonstrates the VehicleUpgradeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the **Vehicle Upgrade System** design pattern. While not a classic Gang of Four pattern, it's a very common architectural pattern in game development for managing incremental changes to player entities (vehicles, characters, weapons, etc.).

The core idea is to:
1.  **Define a `Vehicle`**: An entity that can be upgraded and has quantifiable stats.
2.  **Define an `UpgradeDefinition`**: A blueprint for a single upgrade, specifying what it changes and its cost.
3.  **Manage Upgrades**: A system responsible for applying upgrades, tracking current stats, and handling game logic (like currency).

This example focuses on making it practical and educational, showcasing how to use ScriptableObjects, `MonoBehaviour` components, and a centralized manager.

---

### Project Setup in Unity

1.  **Create a new Unity project** (or open an existing one).
2.  **Create a folder structure**:
    *   `Assets/Scripts`
    *   `Assets/ScriptableObjects/Upgrades`
3.  **Create C# scripts**: Create the following C# scripts inside `Assets/Scripts`.
4.  **Create ScriptableObjects**: After creating `UpgradeDefinition.cs`, right-click in `Assets/ScriptableObjects/Upgrades`, go to `Create > Vehicle System > Upgrade Definition`, and create a few upgrade assets.
5.  **Set up Scene**:
    *   Create an empty GameObject named `_Managers`.
    *   Attach the `VehicleUpgradeManager.cs` script to `_Managers`.
    *   In the Inspector of `_Managers`, drag all your created `UpgradeDefinition` ScriptableObjects into the `All Available Upgrades` list.
    *   Create a 3D object (e.g., a Cube) and name it `PlayerVehicle`.
    *   Attach the `UpgradeableVehicle.cs` script to `PlayerVehicle`.
    *   In the Inspector of `PlayerVehicle`, set some initial `Base Stats`.
6.  **Run the scene**: Watch the console for upgrade logs.

---

### The Code

Here are the C# scripts:

#### 1. `VehicleStats.cs`

This script defines the basic stats for a vehicle. Using a `struct` makes it a value type, which can be useful for passing around copies, but a `class` also works. We make it `[Serializable]` so it can be viewed and edited in the Unity Inspector.

```csharp
using UnityEngine;
using System;

namespace VehicleUpgradeSystem
{
    /// <summary>
    /// Represents the core statistics of a vehicle.
    /// Marked [Serializable] so it can be shown and edited in the Unity Inspector.
    /// </summary>
    [Serializable]
    public struct VehicleStats
    {
        [Tooltip("The maximum speed the vehicle can reach.")]
        public float topSpeed;
        [Tooltip("How quickly the vehicle accelerates.")]
        public float acceleration;
        [Tooltip("How responsive the vehicle is to steering inputs.")]
        public float handling;
        [Tooltip("How effectively the vehicle can stop.")]
        public float braking;

        /// <summary>
        /// A convenient method to get a nicely formatted string of the stats.
        /// </summary>
        public override string ToString()
        {
            return $"Speed: {topSpeed:F1} | Accel: {acceleration:F1} | Handling: {handling:F1} | Braking: {braking:F1}";
        }
    }
}
```

#### 2. `UpgradeDefinition.cs`

This is a `ScriptableObject` that acts as a blueprint for a single upgrade.
**Why ScriptableObjects?**
*   **Data-driven**: Separates upgrade data from code logic.
*   **Reusable**: A single `UpgradeDefinition` asset can be applied to multiple vehicles.
*   **Editor-friendly**: Can be created and configured directly in the Unity Editor without writing code.
*   **Persistent**: Data persists between play sessions and builds.

```csharp
using UnityEngine;

namespace VehicleUpgradeSystem
{
    /// <summary>
    /// ScriptableObject defining a single vehicle upgrade.
    /// This acts as a blueprint for an upgrade, specifying its properties and the stat changes it provides.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Vehicle System/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        [Header("Upgrade Information")]
        [Tooltip("The user-friendly name of the upgrade.")]
        public string upgradeName = "New Upgrade";
        [Tooltip("A brief description of what this upgrade does.")]
        [TextArea(3, 6)]
        public string description = "A basic upgrade that improves vehicle performance.";
        [Tooltip("The cost of applying this upgrade.")]
        public int cost = 100;

        [Header("Stat Modifiers (Additive)")]
        [Tooltip("Amount to add to Top Speed when this upgrade is applied.")]
        public float speedModifier = 0f;
        [Tooltip("Amount to add to Acceleration when this upgrade is applied.")]
        public float accelerationModifier = 0f;
        [Tooltip("Amount to add to Handling when this upgrade is applied.")]
        public float handlingModifier = 0f;
        [Tooltip("Amount to add to Braking when this upgrade is applied.")]
        public float brakingModifier = 0f;

        /// <summary>
        /// Applies the modifiers defined by this upgrade to the provided VehicleStats.
        /// Note: This method directly modifies the passed-in VehicleStats struct.
        /// </summary>
        /// <param name="stats">The VehicleStats to modify.</param>
        public void ApplyModifiers(ref VehicleStats stats)
        {
            stats.topSpeed += speedModifier;
            stats.acceleration += accelerationModifier;
            stats.handling += handlingModifier;
            stats.braking += brakingModifier;
        }

        public override string ToString()
        {
            return $"{upgradeName} (Cost: {cost}) - " +
                   $"[Speed:{speedModifier:F1}, Accel:{accelerationModifier:F1}, Hndl:{handlingModifier:F1}, Brk:{brakingModifier:F1}]";
        }
    }
}
```

#### 3. `UpgradeableVehicle.cs`

This `MonoBehaviour` component is attached to any GameObject that represents a vehicle in your scene. It holds the vehicle's base stats and tracks all upgrades currently applied to it. It also calculates the `CurrentStats` based on base stats plus all applied upgrades.

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace VehicleUpgradeSystem
{
    /// <summary>
    /// Represents a vehicle in the game that can be upgraded.
    /// This component holds the vehicle's base stats and a list of applied upgrades.
    /// It recalculates the current effective stats whenever an upgrade is applied or removed.
    /// </summary>
    public class UpgradeableVehicle : MonoBehaviour
    {
        [Header("Base Vehicle Configuration")]
        [Tooltip("The initial, unmodified statistics of this vehicle.")]
        public VehicleStats baseStats;

        [Header("Applied Upgrades")]
        [Tooltip("List of UpgradeDefinition ScriptableObjects currently applied to this vehicle.")]
        // We use [SerializeField] for private fields to expose them in the Inspector
        // for debugging or initial setup.
        [SerializeField]
        private List<UpgradeDefinition> _appliedUpgrades = new List<UpgradeDefinition>();

        // Public property to safely access the list of applied upgrades.
        // It returns a new list to prevent external modification of the internal list.
        public IReadOnlyList<UpgradeDefinition> AppliedUpgrades => _appliedUpgrades;

        // The current, calculated stats of the vehicle after all upgrades are applied.
        private VehicleStats _currentStats;
        /// <summary>
        /// Gets the current, effective statistics of the vehicle,
        /// including all applied upgrades.
        /// </summary>
        public VehicleStats CurrentStats => _currentStats;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// We initialize stats and recalculate them based on any pre-assigned upgrades.
        /// </summary>
        private void Awake()
        {
            RecalculateStats();
            Debug.Log($"[Vehicle] '{gameObject.name}' Initialized. Base Stats: {baseStats}. Current Stats: {_currentStats}");
        }

        /// <summary>
        /// Applies an upgrade to this vehicle. This method is internal because
        /// the `VehicleUpgradeManager` should be the only component directly calling this,
        /// ensuring all upgrade rules (cost, availability, etc.) are enforced.
        /// </summary>
        /// <param name="upgrade">The UpgradeDefinition to apply.</param>
        public void ApplyUpgradeInternal(UpgradeDefinition upgrade)
        {
            if (!_appliedUpgrades.Contains(upgrade))
            {
                _appliedUpgrades.Add(upgrade);
                RecalculateStats();
                Debug.Log($"[Vehicle] '{gameObject.name}' applied '{upgrade.name}'. New stats: {_currentStats}");
            }
            else
            {
                Debug.LogWarning($"[Vehicle] '{gameObject.name}' already has '{upgrade.name}' applied.");
            }
        }

        /// <summary>
        /// Removes an upgrade from this vehicle. Similar to ApplyUpgradeInternal,
        /// this is intended to be called by a manager.
        /// </summary>
        /// <param name="upgrade">The UpgradeDefinition to remove.</param>
        public void RemoveUpgradeInternal(UpgradeDefinition upgrade)
        {
            if (_appliedUpgrades.Remove(upgrade))
            {
                RecalculateStats();
                Debug.Log($"[Vehicle] '{gameObject.name}' removed '{upgrade.name}'. New stats: {_currentStats}");
            }
            else
            {
                Debug.LogWarning($"[Vehicle] '{gameObject.name}' does not have '{upgrade.name}' applied to remove.");
            }
        }

        /// <summary>
        /// Recalculates the vehicle's current stats by starting with base stats
        /// and applying all currently active upgrades.
        /// This method should be called whenever an upgrade is added or removed.
        /// </summary>
        public void RecalculateStats()
        {
            // Start with base stats
            _currentStats = baseStats;

            // Apply modifiers from all active upgrades
            foreach (var upgrade in _appliedUpgrades)
            {
                upgrade.ApplyModifiers(ref _currentStats);
            }

            // In a real game, you might also update UI here or trigger events
            // for other systems that depend on vehicle stats (e.g., physics engine).
            Debug.Log($"[Vehicle] '{gameObject.name}' stats recalculated. Final: {_currentStats}");
        }
    }
}
```

#### 4. `VehicleUpgradeManager.cs`

This `MonoBehaviour` acts as the central hub for managing all upgrade-related logic. It handles currency, available upgrades, and the actual process of applying an upgrade to a vehicle. It uses the Singleton pattern for easy global access.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .Except()

namespace VehicleUpgradeSystem
{
    /// <summary>
    /// The central manager for the Vehicle Upgrade System.
    /// This singleton handles player currency, available upgrades, and orchestrates
    /// the application and removal of upgrades on `UpgradeableVehicle` instances.
    /// </summary>
    public class VehicleUpgradeManager : MonoBehaviour
    {
        // Singleton pattern for easy global access.
        public static VehicleUpgradeManager Instance { get; private set; }

        [Header("Manager Configuration")]
        [Tooltip("All UpgradeDefinition ScriptableObjects available in the game.")]
        // Populate this list in the Inspector with all your created UpgradeDefinition assets.
        [SerializeField]
        private List<UpgradeDefinition> allAvailableUpgrades = new List<UpgradeDefinition>();

        [Header("Player Economy")]
        [Tooltip("The player's current currency amount.")]
        [SerializeField]
        private int _playerCurrency = 500; // Example initial currency

        /// <summary>
        /// Gets the player's current currency.
        /// </summary>
        public int PlayerCurrency => _playerCurrency;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Implements the Singleton pattern to ensure only one instance exists.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple VehicleUpgradeManager instances found! Destroying duplicate.", this);
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optional: keep manager across scenes
                Debug.Log($"[Manager] VehicleUpgradeManager initialized. Player Currency: {_playerCurrency}");
            }
        }

        /// <summary>
        /// Attempts to apply an upgrade to a specific vehicle.
        /// Handles currency checks and ensures the upgrade isn't already applied.
        /// </summary>
        /// <param name="vehicle">The UpgradeableVehicle instance to upgrade.</param>
        /// <param name="upgrade">The UpgradeDefinition to apply.</param>
        /// <returns>True if the upgrade was successfully applied, false otherwise.</returns>
        public bool TryApplyUpgrade(UpgradeableVehicle vehicle, UpgradeDefinition upgrade)
        {
            if (vehicle == null)
            {
                Debug.LogError("[Manager] Cannot apply upgrade: Vehicle is null.");
                return false;
            }
            if (upgrade == null)
            {
                Debug.LogError("[Manager] Cannot apply upgrade: UpgradeDefinition is null.");
                return false;
            }

            // 1. Check if upgrade is available in the game (optional, but good for validation)
            if (!allAvailableUpgrades.Contains(upgrade))
            {
                Debug.LogWarning($"[Manager] Upgrade '{upgrade.name}' is not recognized as an available upgrade.");
                return false;
            }

            // 2. Check if the upgrade is already applied to this specific vehicle
            if (vehicle.AppliedUpgrades.Contains(upgrade))
            {
                Debug.LogWarning($"[Manager] Upgrade '{upgrade.name}' is already applied to '{vehicle.name}'.");
                return false;
            }

            // 3. Check if the player has enough currency
            if (_playerCurrency < upgrade.cost)
            {
                Debug.LogWarning($"[Manager] Not enough currency to apply '{upgrade.name}'. Needed: {upgrade.cost}, Has: {_playerCurrency}");
                return false;
            }

            // All checks passed, proceed with applying the upgrade
            _playerCurrency -= upgrade.cost; // Deduct cost
            vehicle.ApplyUpgradeInternal(upgrade); // Tell the vehicle to apply it
            Debug.Log($"[Manager] Successfully applied '{upgrade.name}' to '{vehicle.name}'. Remaining currency: {_playerCurrency}");

            // In a real game, you might trigger a UI update event here.
            // UIManager.Instance.UpdateCurrencyDisplay(_playerCurrency);

            return true;
        }

        /// <summary>
        /// Attempts to remove an upgrade from a specific vehicle and refund its cost.
        /// </summary>
        /// <param name="vehicle">The UpgradeableVehicle instance to modify.</param>
        /// <param name="upgrade">The UpgradeDefinition to remove.</param>
        /// <returns>True if the upgrade was successfully removed, false otherwise.</returns>
        public bool TryRemoveUpgrade(UpgradeableVehicle vehicle, UpgradeDefinition upgrade)
        {
            if (vehicle == null || upgrade == null)
            {
                Debug.LogError("[Manager] Cannot remove upgrade: Vehicle or UpgradeDefinition is null.");
                return false;
            }

            if (vehicle.AppliedUpgrades.Contains(upgrade))
            {
                vehicle.RemoveUpgradeInternal(upgrade);
                _playerCurrency += upgrade.cost; // Refund cost
                Debug.Log($"[Manager] Successfully removed '{upgrade.name}' from '{vehicle.name}'. Refunded: {upgrade.cost}. New currency: {_playerCurrency}");
                return true;
            }
            else
            {
                Debug.LogWarning($"[Manager] Upgrade '{upgrade.name}' is not applied to '{vehicle.name}', cannot remove.");
                return false;
            }
        }

        /// <summary>
        /// Gets a list of all available upgrades in the game.
        /// </summary>
        public IReadOnlyList<UpgradeDefinition> GetAllAvailableUpgrades()
        {
            return allAvailableUpgrades;
        }

        /// <summary>
        /// Gets a list of upgrades that have *not* yet been applied to a specific vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle to check for unapplied upgrades.</param>
        public IReadOnlyList<UpgradeDefinition> GetUnappliedUpgradesForVehicle(UpgradeableVehicle vehicle)
        {
            if (vehicle == null) return new List<UpgradeDefinition>();
            return allAvailableUpgrades.Except(vehicle.AppliedUpgrades).ToList();
        }

        /// <summary>
        /// Adds currency to the player's balance.
        /// </summary>
        public void AddCurrency(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[Manager] Attempted to add negative currency: {amount}. Use SpendCurrency for spending.");
                return;
            }
            _playerCurrency += amount;
            Debug.Log($"[Manager] Added {amount} currency. New balance: {_playerCurrency}");
            // UIManager.Instance.UpdateCurrencyDisplay(_playerCurrency);
        }

        /// <summary>
        /// Spends currency from the player's balance.
        /// </summary>
        /// <returns>True if currency was successfully spent, false if not enough.</returns>
        public bool SpendCurrency(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[Manager] Attempted to spend negative currency: {amount}. Use AddCurrency for adding.");
                return false;
            }
            if (_playerCurrency >= amount)
            {
                _playerCurrency -= amount;
                Debug.Log($"[Manager] Spent {amount} currency. New balance: {_playerCurrency}");
                // UIManager.Instance.UpdateCurrencyDisplay(_playerCurrency);
                return true;
            }
            else
            {
                Debug.LogWarning($"[Manager] Failed to spend {amount} currency. Not enough balance: {_playerCurrency}");
                return false;
            }
        }
    }
}
```

---

### Example Usage (Test Script)

You can create a simple test script to interact with the system at runtime.

#### 5. `UpgradeTester.cs`

Attach this script to an empty GameObject or the `PlayerVehicle` itself.

```csharp
using UnityEngine;
using System.Linq; // For .FirstOrDefault()

namespace VehicleUpgradeSystem
{
    /// <summary>
    /// Simple test script to demonstrate interaction with the VehicleUpgradeSystem.
    /// Attach this to a GameObject in your scene and assign a vehicle.
    /// </summary>
    public class UpgradeTester : MonoBehaviour
    {
        [Tooltip("The vehicle instance to test upgrades on.")]
        public UpgradeableVehicle targetVehicle;

        [Header("Test Actions (Press these keys in Play Mode)")]
        [Tooltip("The name of an upgrade to try applying when 'A' is pressed.")]
        public string upgradeToApplyA = "Engine Boost";
        [Tooltip("The name of an upgrade to try applying when 'S' is pressed.")]
        public string upgradeToApplyS = "Sport Tires";
        [Tooltip("The name of an upgrade to try removing when 'D' is pressed.")]
        public string upgradeToRemoveD = "Engine Boost";
        [Tooltip("Amount of currency to add when 'C' is pressed.")]
        public int currencyToAdd = 200;

        void Update()
        {
            if (targetVehicle == null)
            {
                Debug.LogWarning("[Tester] Target Vehicle not assigned.", this);
                return;
            }

            if (VehicleUpgradeManager.Instance == null)
            {
                Debug.LogError("[Tester] VehicleUpgradeManager instance not found. Make sure it's in the scene.", this);
                return;
            }

            // Apply Upgrade A
            if (Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log($"--- Attempting to apply '{upgradeToApplyA}' ---");
                UpgradeDefinition upgrade = VehicleUpgradeManager.Instance.GetAllAvailableUpgrades()
                                            .FirstOrDefault(u => u.upgradeName == upgradeToApplyA);
                if (upgrade != null)
                {
                    VehicleUpgradeManager.Instance.TryApplyUpgrade(targetVehicle, upgrade);
                }
                else
                {
                    Debug.LogError($"[Tester] Upgrade '{upgradeToApplyA}' not found in manager's available upgrades.");
                }
            }

            // Apply Upgrade S
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log($"--- Attempting to apply '{upgradeToApplyS}' ---");
                UpgradeDefinition upgrade = VehicleUpgradeManager.Instance.GetAllAvailableUpgrades()
                                            .FirstOrDefault(u => u.upgradeName == upgradeToApplyS);
                if (upgrade != null)
                {
                    VehicleUpgradeManager.Instance.TryApplyUpgrade(targetVehicle, upgrade);
                }
                else
                {
                    Debug.LogError($"[Tester] Upgrade '{upgradeToApplyS}' not found in manager's available upgrades.");
                }
            }

            // Remove Upgrade D
            if (Input.GetKeyDown(KeyCode.D))
            {
                Debug.Log($"--- Attempting to remove '{upgradeToRemoveD}' ---");
                UpgradeDefinition upgrade = VehicleUpgradeManager.Instance.GetAllAvailableUpgrades()
                                            .FirstOrDefault(u => u.upgradeName == upgradeToRemoveD);
                if (upgrade != null)
                {
                    VehicleUpgradeManager.Instance.TryRemoveUpgrade(targetVehicle, upgrade);
                }
                else
                {
                    Debug.LogError($"[Tester] Upgrade '{upgradeToRemoveD}' not found in manager's available upgrades.");
                }
            }

            // Add Currency
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log($"--- Attempting to add {currencyToAdd} currency ---");
                VehicleUpgradeManager.Instance.AddCurrency(currencyToAdd);
            }

            // Log Current Stats and Currency
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("--- Current Status ---");
                Debug.Log($"Vehicle '{targetVehicle.name}' Current Stats: {targetVehicle.CurrentStats}");
                Debug.Log($"Applied Upgrades: {string.Join(", ", targetVehicle.AppliedUpgrades.Select(u => u.upgradeName))}");
                Debug.Log($"Player Currency: {VehicleUpgradeManager.Instance.PlayerCurrency}");
                Debug.Log("--------------------");
            }
        }
    }
}
```

---

### How the `VehicleUpgradeSystem` Pattern Works

1.  **Vehicle Definition (`UpgradeableVehicle`)**:
    *   It defines a `baseStats` (e.g., initial speed, acceleration).
    *   It keeps a list (`_appliedUpgrades`) of `UpgradeDefinition` objects that have been applied to it.
    *   Crucially, it has a `RecalculateStats()` method. When an upgrade is applied or removed, this method is called. It *resets* `_currentStats` to `baseStats` and then iterates through *all* `_appliedUpgrades`, applying their modifiers sequentially. This ensures that the vehicle's stats are always consistently derived from its base and current upgrades, preventing errors from cumulative modifications or incorrect removal.

2.  **Upgrade Blueprint (`UpgradeDefinition`)**:
    *   This `ScriptableObject` describes *what* an upgrade is (name, cost, description) and *how* it modifies vehicle stats (e.g., `speedModifier`).
    *   By using a `ScriptableObject`, upgrade data is independent of code. Designers can create new upgrades in the editor without touching a single line of code.

3.  **Upgrade Orchestrator (`VehicleUpgradeManager`)**:
    *   This acts as the central brain. It's often implemented as a Singleton so other parts of the game (e.g., UI, player input) can easily access it.
    *   It manages the overall `allAvailableUpgrades` pool and the `_playerCurrency`.
    *   The `TryApplyUpgrade()` method is key: it encapsulates all the business logic (cost check, already applied check, etc.) before calling the `UpgradeableVehicle.ApplyUpgradeInternal()` method. This centralizes upgrade rules, making them easy to modify and ensuring consistency.
    *   It provides methods to query available upgrades, check current currency, etc.

**Extensibility and Further Considerations:**

*   **Complex Upgrade Effects (`IUpgradeEffect` Strategy)**: For more advanced effects (e.g., "On taking damage, gain speed for 2 seconds"), you could introduce an `IUpgradeEffect` interface. `UpgradeDefinition` would then hold a reference to an instance of a class implementing this interface (using `[SerializeReference]` for polymorphism or having specific `ScriptableObject` types for effects). The `ApplyModifiers` method would then call `IUpgradeEffect.Apply(vehicle)`.
*   **Multiplicative Modifiers**: Currently, modifiers are additive. You might need percentage-based or multiplicative modifiers. You could add flags (`isPercentage`, `isMultiplicative`) or separate modifier fields in `UpgradeDefinition` and adjust `RecalculateStats()` accordingly (e.g., apply additive first, then multiplicative).
*   **Upgrade Tiers/Prerequisites**: `UpgradeDefinition` could include a `prerequisiteUpgrade` field to enforce upgrade paths (e.g., "Engine Boost II" requires "Engine Boost I").
*   **UI Integration**: You'd typically have a UI script that fetches available upgrades from the `VehicleUpgradeManager` and displays them, allowing the player to click a button to `TryApplyUpgrade()`.
*   **Saving and Loading**: When saving game state, you'd save the `_playerCurrency` from the manager and the `_appliedUpgrades` list for each `UpgradeableVehicle` (e.g., by saving a list of `UpgradeDefinition.name` or unique IDs).
*   **Dynamic Upgrade Loading**: Instead of populating `allAvailableUpgrades` manually, you could load them all from a specific folder at runtime using `Resources.LoadAll<UpgradeDefinition>()` or Addressables.
*   **Events**: The manager could use C# events (`OnUpgradeApplied`, `OnCurrencyChanged`) to notify other systems (like UI) about changes, reducing direct dependencies.

This example provides a robust and flexible foundation for building a comprehensive vehicle upgrade system in Unity.