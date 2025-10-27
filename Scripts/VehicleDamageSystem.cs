// Unity Design Pattern Example: VehicleDamageSystem
// This script demonstrates the VehicleDamageSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `VehicleDamageSystem` design pattern, in the context of game development, refers to a structured way of managing how damage affects a vehicle (or any complex object with multiple parts). It's not one of the traditional Gang of Four patterns, but rather a robust architectural approach often seen in component-based game engines like Unity.

The core idea is to break down a vehicle into multiple **damageable parts**, each with its own health and specific effects when damaged or destroyed. A central **damage system** orchestrates the damage application and aggregates the state of all parts to determine the vehicle's overall condition. **Effectors** (implementing an interface) then react to damage on specific parts, causing visual changes, performance degradation, or other game-specific consequences.

This approach provides:
*   **Modularity:** Each part is self-contained.
*   **Extensibility:** New types of damage effects can be added easily by implementing an interface.
*   **Granularity:** Damage can be precise (e.g., shoot a tire, blow up an engine).
*   **Decoupling:** The damage application logic is separate from its visual or functional consequences.

---

## VehicleDamageSystem Pattern: Complete C# Unity Example

This example provides a complete, working set of C# scripts that demonstrate the `VehicleDamageSystem` pattern in Unity.

### Core Components:

1.  **`IVehicleDamageEffector`**: An interface that defines how components react to damage events on a `DamageablePart`.
2.  **`DamageablePart`**: A `MonoBehaviour` representing a specific part of the vehicle (e.g., Engine, Wheel, Body Panel). It has health, can take damage, and notifies its local effectors and the central `VehicleDamageSystem` about its state changes.
3.  **`VehicleDamageSystem`**: A `MonoBehaviour` that manages all `DamageablePart`s on a vehicle. It provides methods to apply damage, tracks overall vehicle health, and offers global events for vehicle-wide damage states.
4.  **`VehicleController`**: A simplified `MonoBehaviour` representing a vehicle's movement and behavior. Its properties can be modified by `PerformanceDamageEffector`s.
5.  **`VisualDamageEffector`**: An implementation of `IVehicleDamageEffector` that changes visual aspects (e.g., materials, particle systems) when its associated `DamageablePart` takes damage.
6.  **`PerformanceDamageEffector`**: An implementation of `IVehicleDamageEffector` that modifies the `VehicleController`'s performance properties (e.g., engine power, steering sensitivity) when its associated `DamageablePart` takes damage.
7.  **`DamageTester`**: A simple script to simulate applying damage for demonstration purposes, typically used with UI buttons or input.

---

### Setup Instructions (How to use this in Unity):

1.  **Create a New Unity Project** or open an existing one.
2.  **Create a Folder** named `Scripts` (or similar) in your Project window.
3.  **Create all the C# scripts** listed below inside this folder. Copy and paste the code into each respective file.
4.  **Create a 3D Object** (e.g., a Cube) in your scene. Name it `Vehicle`. This will be our vehicle root.
5.  **Add `VehicleController.cs` and `VehicleDamageSystem.cs`** to the `Vehicle` GameObject.
6.  **Create Child GameObjects** under `Vehicle` to represent different damageable parts. For example:
    *   Right-click `Vehicle` -> `Create Empty` -> Name it `Engine`
    *   Right-click `Vehicle` -> `Create Empty` -> Name it `FrontLeftWheel`
    *   Right-click `Vehicle` -> `Create Empty` -> Name it `BodyPanel`
    *   (You can add more as needed: RearWheel, FuelTank, etc.)
7.  **Add `DamageablePart.cs`** to each of these child GameObjects (`Engine`, `FrontLeftWheel`, `BodyPanel`).
    *   Assign a `Part Name` (e.g., "Engine", "Front Left Wheel", "Body Panel").
    *   Set `Max Health` for each part.
8.  **Add `VisualDamageEffector.cs`** to `BodyPanel` and `FrontLeftWheel`.
    *   Assign a `Renderer` component (e.e., if `BodyPanel` is a Cube, add a `MeshRenderer`).
    *   Create some `Material` assets (e.g., `DefaultMaterial`, `DentedMaterial`, `SeverelyDentedMaterial`).
    *   Drag and drop these materials into the `Original Material` and `Damaged Materials` slots in the Inspector.
    *   Optionally, create a Particle System (e.g., `SmokeParticles`) and assign it to `Destroyed Effect To Activate`.
9.  **Add `PerformanceDamageEffector.cs`** to `Engine` and `FrontLeftWheel`.
    *   For `Engine`: Set `Engine Power Reduction Per Unit` to a value like `0.005f` (meaning 1 unit of damage reduces power by 0.5%).
    *   For `FrontLeftWheel`: Set `Steering Degradation Per Unit` to `0.003f`.
10. **Create a UI Canvas** (`GameObject` -> `UI` -> `Canvas`).
11. **Create some UI Buttons** (`GameObject` -> `UI` -> `Button`) as children of the Canvas.
    *   For each button, add the `DamageTester.cs` script.
    *   Drag the `Vehicle` GameObject (from the Hierarchy) into the `Vehicle Damage System` slot on the `DamageTester` component.
    *   Configure the `Target Part Name` and `Damage Amount` for each button in the Inspector. For example:
        *   Button 1: `Target Part Name` = "Engine", `Damage Amount` = 20
        *   Button 2: `Target Part Name` = "Front Left Wheel", `Damage Amount` = 15
        *   Button 3: `Target Part Name` = "Body Panel", `Damage Amount` = 30
        *   Button 4: `Target Part Name` = "Random", `Damage Amount` = 10
        *   Button 5: `Target Part Name` = "Repair All", `Damage Amount` = 0 (or some value)
    *   Assign the `DamageTester.ApplyDamage` (for damage buttons) or `DamageTester.RepairAll` (for repair button) method to the `OnClick()` event of each button.
12. **Run the scene!** Click the buttons to observe how damage is applied to parts, affecting visuals and performance logs.

---

### 1. `IVehicleDamageEffector.cs`

```csharp
using UnityEngine;

/// <summary>
/// Interface for components that react to damage events on a DamageablePart.
/// This allows for extensible damage effects (visual, performance, audio, etc.)
/// without the DamageablePart or VehicleDamageSystem needing to know
/// the concrete types of these effects.
/// </summary>
public interface IVehicleDamageEffector
{
    /// <summary>
    /// Called when the associated DamageablePart takes damage.
    /// </summary>
    /// <param name="currentHealth">The part's current health.</param>
    /// <param name="maxHealth">The part's maximum health.</param>
    /// <param name="damageAmount">The amount of damage just taken.</param>
    void OnPartDamaged(float currentHealth, float maxHealth, float damageAmount);

    /// <summary>
    /// Called when the associated DamageablePart's health drops to 0 or below, marking it as destroyed.
    /// </summary>
    void OnPartDestroyed();

    /// <summary>
    /// Called when the associated DamageablePart is repaired and is no longer destroyed (health > 0).
    /// </summary>
    void OnPartRepaired();

    /// <summary>
    /// Called when the associated DamageablePart is fully repaired to max health.
    /// </summary>
    void OnPartFullyRepaired();
}
```

### 2. `DamageablePart.cs`

```csharp
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

/// <summary>
/// Represents a specific damageable part of a vehicle.
/// Each part has its own health, can take damage, and notifies
/// its local IVehicleDamageEffector components when its state changes.
/// </summary>
public class DamageablePart : MonoBehaviour
{
    [Tooltip("A unique identifier for this part (e.g., 'Engine', 'FrontLeftWheel').")]
    [SerializeField] private string partName = "New Part";
    [Tooltip("The maximum health this part can have.")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("If checked, this part is considered 'critical'. If all critical parts are destroyed, the vehicle might be considered destroyed.")]
    [SerializeField] private bool isCriticalPart = false;

    private float _currentHealth;
    private bool _isDestroyed;
    private IVehicleDamageEffector[] _localEffectors;

    // Events that other systems (like VehicleDamageSystem) can subscribe to
    public UnityAction<DamageablePart, float, float> OnPartDamaged; // Part, currentHealth, damageAmount
    public UnityAction<DamageablePart> OnPartDestroyed; // Part
    public UnityAction<DamageablePart> OnPartRepaired; // Part (from destroyed to alive)
    public UnityAction<DamageablePart> OnPartFullyRepaired; // Part (back to max health)

    /// <summary>
    /// Gets the unique name of this part.
    /// </summary>
    public string PartName => partName;

    /// <summary>
    /// Gets the current health of this part.
    /// </summary>
    public float CurrentHealth => _currentHealth;

    /// <summary>
    /// Gets the maximum health of this part.
    /// </summary>
    public float MaxHealth => maxHealth;

    /// <summary>
    /// Gets whether this part is currently destroyed (health <= 0).
    /// </summary>
    public bool IsDestroyed => _isDestroyed;

    /// <summary>
    /// Gets whether this part is marked as critical for vehicle functionality.
    /// </summary>
    public bool IsCriticalPart => isCriticalPart;

    private void Awake()
    {
        _currentHealth = maxHealth;
        _isDestroyed = false;
        // Collect all IVehicleDamageEffector components attached to this GameObject.
        // This allows multiple effects (visual, audio, performance) to be attached to one part.
        _localEffectors = GetComponents<IVehicleDamageEffector>();
    }

    /// <summary>
    /// Applies damage to this part, reducing its health.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0) return; // No damage taken

        float previousHealth = _currentHealth;
        _currentHealth -= amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

        Debug.Log($"{PartName} took {amount} damage. Health: {previousHealth} -> {_currentHealth}/{maxHealth}");

        // Notify local effectors about damage
        foreach (var effector in _localEffectors)
        {
            effector.OnPartDamaged(_currentHealth, maxHealth, amount);
        }

        // Check for destruction state change
        if (_currentHealth <= 0 && !_isDestroyed)
        {
            _isDestroyed = true;
            Debug.Log($"{PartName} is DESTROYED!");
            OnPartDestroyed?.Invoke(this); // Notify external listeners
            foreach (var effector in _localEffectors)
            {
                effector.OnPartDestroyed(); // Notify local effectors
            }
        }
        else if (_currentHealth > 0 && _isDestroyed && previousHealth <= 0)
        {
            // Part was repaired from a destroyed state
            _isDestroyed = false;
            Debug.Log($"{PartName} has been repaired from destroyed state.");
            OnPartRepaired?.Invoke(this); // Notify external listeners
            foreach (var effector in _localEffectors)
            {
                effector.OnPartRepaired(); // Notify local effectors
            }
        }

        OnPartDamaged?.Invoke(this, _currentHealth, amount); // Notify external listeners

        // Check for full repair
        if (_currentHealth >= maxHealth && previousHealth < maxHealth)
        {
            Debug.Log($"{PartName} is fully repaired.");
            OnPartFullyRepaired?.Invoke(this);
            foreach (var effector in _localEffectors)
            {
                effector.OnPartFullyRepaired();
            }
        }
    }

    /// <summary>
    /// Repairs this part, increasing its health.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Repair(float amount)
    {
        if (amount <= 0) return;

        float previousHealth = _currentHealth;
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

        Debug.Log($"{PartName} repaired {amount} health. Health: {previousHealth} -> {_currentHealth}/{maxHealth}");

        // Local effectors also react to "repair" as a form of "damage taken" in reverse,
        // so they can update their state (e.g., remove dents).
        // For simplicity, we'll re-call OnPartDamaged so effectors update based on new health.
        // A more explicit Repair event could be added to IVehicleDamageEffector if needed.
        foreach (var effector in _localEffectors)
        {
            effector.OnPartDamaged(_currentHealth, maxHealth, -amount); // Negative damage implies repair
        }

        // Check for state changes (e.g., moving from destroyed to not destroyed)
        if (_currentHealth > 0 && _isDestroyed && previousHealth <= 0)
        {
            _isDestroyed = false;
            Debug.Log($"{PartName} has been repaired from destroyed state.");
            OnPartRepaired?.Invoke(this);
            foreach (var effector in _localEffectors)
            {
                effector.OnPartRepaired();
            }
        }

        OnPartDamaged?.Invoke(this, _currentHealth, -amount); // Notify external listeners with negative 'damage'

        // Check for full repair
        if (_currentHealth >= maxHealth && previousHealth < maxHealth)
        {
            Debug.Log($"{PartName} is fully repaired.");
            OnPartFullyRepaired?.Invoke(this);
            foreach (var effector in _localEffectors)
            {
                effector.OnPartFullyRepaired();
            }
        }
    }

    /// <summary>
    /// Repairs this part to its maximum health.
    /// </summary>
    public void RepairFully()
    {
        Repair(maxHealth - _currentHealth);
    }
}
```

### 3. `VehicleDamageSystem.cs`

```csharp
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The central manager for a vehicle's damage.
/// It aggregates all DamageablePart components on the vehicle,
/// provides methods to apply damage, tracks overall vehicle health,
/// and offers global events for vehicle-wide damage states.
/// </summary>
public class VehicleDamageSystem : MonoBehaviour
{
    [Tooltip("If the overall health percentage drops below this value, OnVehicleSeverelyDamaged will be invoked.")]
    [SerializeField] private float severeDamageThreshold = 0.3f; // 30% health

    private List<DamageablePart> _allParts;
    private bool _isVehicleDestroyed = false;
    private bool _isVehicleSeverelyDamaged = false;

    // Global events for the entire vehicle's state
    public UnityAction<float> OnVehicleOverallDamaged; // Current overall health percentage
    public UnityAction OnVehicleDestroyed;
    public UnityAction OnVehicleRepaired; // From destroyed to operational
    public UnityAction OnVehicleSeverelyDamaged;
    public UnityAction OnVehicleRecoveredFromSevereDamage;

    /// <summary>
    /// Gets the current overall health percentage of the vehicle (0.0 to 1.0).
    /// </summary>
    public float OverallHealthPercentage { get; private set; }

    /// <summary>
    /// Gets whether the vehicle is currently considered destroyed (e.g., all critical parts destroyed or overall health very low).
    /// </summary>
    public bool IsVehicleDestroyed => _isVehicleDestroyed;

    /// <summary>
    /// Gets whether the vehicle is currently considered severely damaged.
    /// </summary>
    public bool IsVehicleSeverelyDamaged => _isVehicleSeverelyDamaged;

    private void Awake()
    {
        // Find all DamageablePart components in children.
        // Using GetComponentsInChildren allows for parts to be nested in complex hierarchies.
        _allParts = GetComponentsInChildren<DamageablePart>().ToList();

        if (_allParts.Count == 0)
        {
            Debug.LogWarning($"VehicleDamageSystem on {gameObject.name} found no DamageablePart components in its hierarchy. Is this intended?");
            return;
        }

        // Subscribe to each part's events to update overall vehicle state
        foreach (var part in _allParts)
        {
            part.OnPartDamaged += HandlePartDamaged;
            part.OnPartDestroyed += HandlePartDestroyed;
            part.OnPartRepaired += HandlePartRepaired;
            part.OnPartFullyRepaired += HandlePartFullyRepaired;
        }

        UpdateOverallVehicleState(); // Initial state update
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks if parts outlive the system
        foreach (var part in _allParts)
        {
            if (part != null) // Check if part still exists
            {
                part.OnPartDamaged -= HandlePartDamaged;
                part.OnPartDestroyed -= HandlePartDestroyed;
                part.OnPartRepaired -= HandlePartRepaired;
                part.OnPartFullyRepaired -= HandlePartFullyRepaired;
            }
        }
    }

    /// <summary>
    /// Handles damage events from individual parts and updates the overall vehicle state.
    /// </summary>
    private void HandlePartDamaged(DamageablePart part, float currentHealth, float damageAmount)
    {
        UpdateOverallVehicleState();
        OnVehicleOverallDamaged?.Invoke(OverallHealthPercentage); // Notify global listeners
    }

    /// <summary>
    /// Handles destruction events from individual parts and updates the overall vehicle state.
    /// </summary>
    private void HandlePartDestroyed(DamageablePart part)
    {
        Debug.Log($"Vehicle overall: {part.PartName} was destroyed.");
        UpdateOverallVehicleState();
    }

    /// <summary>
    /// Handles repair events from individual parts and updates the overall vehicle state.
    /// </summary>
    private void HandlePartRepaired(DamageablePart part)
    {
        Debug.Log($"Vehicle overall: {part.PartName} was repaired.");
        UpdateOverallVehicleState();
    }

    /// <summary>
    /// Handles full repair events from individual parts.
    /// </summary>
    private void HandlePartFullyRepaired(DamageablePart part)
    {
        Debug.Log($"Vehicle overall: {part.PartName} was fully repaired.");
        UpdateOverallVehicleState();
    }

    /// <summary>
    /// Recalculates the vehicle's overall health and checks for destruction/severe damage states.
    /// </summary>
    private void UpdateOverallVehicleState()
    {
        if (_allParts.Count == 0)
        {
            OverallHealthPercentage = 0f;
            SetVehicleDestroyed(true);
            return;
        }

        float totalCurrentHealth = _allParts.Sum(p => p.CurrentHealth);
        float totalMaxHealth = _allParts.Sum(p => p.MaxHealth);

        OverallHealthPercentage = totalMaxHealth > 0 ? totalCurrentHealth / totalMaxHealth : 0f;

        Debug.Log($"Vehicle Overall Health: {OverallHealthPercentage:P0}");

        // Check for severe damage state
        if (OverallHealthPercentage <= severeDamageThreshold && !_isVehicleSeverelyDamaged)
        {
            _isVehicleSeverelyDamaged = true;
            Debug.Log($"Vehicle is severely damaged!");
            OnVehicleSeverelyDamaged?.Invoke();
        }
        else if (OverallHealthPercentage > severeDamageThreshold && _isVehicleSeverelyDamaged)
        {
            _isVehicleSeverelyDamaged = false;
            Debug.Log($"Vehicle recovered from severe damage.");
            OnVehicleRecoveredFromSevereDamage?.Invoke();
        }

        // Determine if the vehicle is destroyed
        bool allCriticalPartsDestroyed = _allParts.Any(p => p.IsCriticalPart) && _allParts.Where(p => p.IsCriticalPart).All(p => p.IsDestroyed);
        bool overallHealthTooLow = OverallHealthPercentage <= 0; // Or some other threshold

        if ((allCriticalPartsDestroyed || overallHealthTooLow) && !_isVehicleDestroyed)
        {
            SetVehicleDestroyed(true);
        }
        else if (!allCriticalPartsDestroyed && !overallHealthTooLow && _isVehicleDestroyed)
        {
            SetVehicleDestroyed(false);
        }
    }

    /// <summary>
    /// Sets the vehicle's destroyed state and invokes relevant global events.
    /// </summary>
    /// <param name="destroyed">True to mark as destroyed, false to mark as operational.</param>
    private void SetVehicleDestroyed(bool destroyed)
    {
        if (_isVehicleDestroyed == destroyed) return;

        _isVehicleDestroyed = destroyed;
        if (_isVehicleDestroyed)
        {
            Debug.Log("VEHICLE DESTROYED!");
            OnVehicleDestroyed?.Invoke();
        }
        else
        {
            Debug.Log("VEHICLE REPAIRED (from destroyed state)!");
            OnVehicleRepaired?.Invoke();
        }
    }

    /// <summary>
    /// Applies damage to a specific part of the vehicle by its name.
    /// </summary>
    /// <param name="partName">The name of the part to damage.</param>
    /// <param name="damageAmount">The amount of damage to apply.</param>
    public void ApplyDamageToPart(string partName, float damageAmount)
    {
        DamageablePart targetPart = _allParts.FirstOrDefault(p => p.PartName.Equals(partName, System.StringComparison.OrdinalIgnoreCase));
        if (targetPart != null)
        {
            targetPart.TakeDamage(damageAmount);
        }
        else
        {
            Debug.LogWarning($"No part named '{partName}' found on vehicle '{gameObject.name}'.");
        }
    }

    /// <summary>
    /// Applies damage to a randomly selected non-destroyed part of the vehicle.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to apply.</param>
    public void ApplyDamageToRandomPart(float damageAmount)
    {
        List<DamageablePart> aliveParts = _allParts.Where(p => !p.IsDestroyed).ToList();
        if (aliveParts.Count > 0)
        {
            int randomIndex = Random.Range(0, aliveParts.Count);
            aliveParts[randomIndex].TakeDamage(damageAmount);
        }
        else
        {
            Debug.Log("All parts are already destroyed. Cannot apply damage to a random part.");
        }
    }

    /// <summary>
    /// Repairs a specific part of the vehicle by its name.
    /// </summary>
    /// <param name="partName">The name of the part to repair.</param>
    /// <param name="repairAmount">The amount of health to restore.</param>
    public void RepairPart(string partName, float repairAmount)
    {
        DamageablePart targetPart = _allParts.FirstOrDefault(p => p.PartName.Equals(partName, System.StringComparison.OrdinalIgnoreCase));
        if (targetPart != null)
        {
            targetPart.Repair(repairAmount);
        }
        else
        {
            Debug.LogWarning($"No part named '{partName}' found on vehicle '{gameObject.name}'.");
        }
    }

    /// <summary>
    /// Fully repairs all parts of the vehicle.
    /// </summary>
    public void RepairAllPartsFully()
    {
        Debug.Log("Repairing all vehicle parts fully.");
        foreach (var part in _allParts)
        {
            part.RepairFully();
        }
    }

    /// <summary>
    /// Gets the health percentage of a specific part.
    /// </summary>
    /// <param name="partName">The name of the part.</param>
    /// <returns>Health percentage (0.0 to 1.0) or -1 if part not found.</returns>
    public float GetPartHealthPercentage(string partName)
    {
        DamageablePart targetPart = _allParts.FirstOrDefault(p => p.PartName.Equals(partName, System.StringComparison.OrdinalIgnoreCase));
        if (targetPart != null && targetPart.MaxHealth > 0)
        {
            return targetPart.CurrentHealth / targetPart.MaxHealth;
        }
        return -1f; // Indicate not found or invalid
    }
}
```

### 4. `VehicleController.cs` (Simplified Example)

```csharp
using UnityEngine;

/// <summary>
/// A simplified VehicleController to demonstrate how performance
/// effectors can alter vehicle behavior based on damage.
/// In a real game, this would be a complex physics-based controller.
/// </summary>
public class VehicleController : MonoBehaviour
{
    [Header("Base Vehicle Stats")]
    [SerializeField] private float baseMaxSpeed = 50f;
    [SerializeField] private float baseEnginePower = 100f;
    [SerializeField] private float baseSteeringSensitivity = 10f;

    [Header("Current Performance Multipliers (Modified by Damage)")]
    // These multipliers are adjusted by PerformanceDamageEffectors
    public float enginePowerMultiplier = 1.0f;
    public float steeringSensitivityMultiplier = 1.0f;
    public float maxSpeedMultiplier = 1.0f;

    [Header("Live Calculated Stats")]
    public float currentMaxSpeed;
    public float currentEnginePower;
    public float currentSteeringSensitivity;

    void Awake()
    {
        // Initialize with base values
        UpdateCalculatedStats();
    }

    void Update()
    {
        // In a real game, this would apply physics, input, etc.
        // For this example, we'll just log the current stats.
        UpdateCalculatedStats();
        // Example: Log current performance
        // Debug.Log($"Vehicle Performance: Speed={currentMaxSpeed:F1}, Power={currentEnginePower:F1}, Steering={currentSteeringSensitivity:F1}");
    }

    /// <summary>
    /// Updates the current effective performance stats based on base values and multipliers.
    /// Multipliers should be set by PerformanceDamageEffectors.
    /// </summary>
    public void UpdateCalculatedStats()
    {
        currentMaxSpeed = baseMaxSpeed * maxSpeedMultiplier;
        currentEnginePower = baseEnginePower * enginePowerMultiplier;
        currentSteeringSensitivity = baseSteeringSensitivity * steeringSensitivityMultiplier;

        // Ensure values don't go below zero or become excessively large due to multipliers
        currentMaxSpeed = Mathf.Max(0, currentMaxSpeed);
        currentEnginePower = Mathf.Max(0, currentEnginePower);
        currentSteeringSensitivity = Mathf.Max(0, currentSteeringSensitivity);
    }

    /// <summary>
    /// Resets all performance multipliers to their default (1.0) state.
    /// </summary>
    public void ResetPerformanceMultipliers()
    {
        enginePowerMultiplier = 1.0f;
        steeringSensitivityMultiplier = 1.0f;
        maxSpeedMultiplier = 1.0f;
        UpdateCalculatedStats();
        Debug.Log("VehicleController: Performance multipliers reset.");
    }
}
```

### 5. `VisualDamageEffector.cs`

```csharp
using UnityEngine;
using System.Linq; // For .Where and .ToArray

/// <summary>
/// Implements IVehicleDamageEffector to provide visual feedback
/// when a DamageablePart takes damage. This can include swapping materials,
/// activating particle effects, or enabling/disabling child objects.
/// </summary>
[RequireComponent(typeof(DamageablePart))] // Ensures a DamageablePart exists on this GameObject
public class VisualDamageEffector : MonoBehaviour, IVehicleDamageEffector
{
    [Header("Visual Components")]
    [Tooltip("The MeshRenderer (or SkinnedMeshRenderer) component whose material will be swapped.")]
    [SerializeField] private Renderer targetRenderer;
    [Tooltip("The original material of the part before any damage.")]
    [SerializeField] private Material originalMaterial;

    [Header("Damage States (Health Thresholds)")]
    [Tooltip("Health percentages (from 0.0 to 1.0) at which the visual state changes. Must match Damaged Materials/Objects count.")]
    [SerializeField] private float[] damageThresholds = { 0.7f, 0.4f, 0.1f }; // e.g., 70%, 40%, 10% health
    [Tooltip("Materials to swap to when damage thresholds are met. Order corresponds to Damage Thresholds.")]
    [SerializeField] private Material[] damagedMaterials;

    [Header("Destruction Effects")]
    [Tooltip("Particle System to activate when the part is destroyed (health <= 0).")]
    [SerializeField] private ParticleSystem destroyedEffectToActivate;
    [Tooltip("GameObjects to activate/deactivate when the part is destroyed.")]
    [SerializeField] private GameObject[] destroyedObjectsToActivate;
    [SerializeField] private GameObject[] destroyedObjectsToDeactivate;

    private int _currentDamageStateIndex = -1; // -1 for original state
    private DamageablePart _damageablePart;

    void Awake()
    {
        _damageablePart = GetComponent<DamageablePart>();
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer != null && originalMaterial == null)
        {
            originalMaterial = targetRenderer.material;
            Debug.LogWarning($"VisualDamageEffector on {gameObject.name}: Original Material not set, using current material from Renderer. " +
                             $"It's recommended to set originalMaterial explicitly in the Inspector.");
        }

        // Sort thresholds in descending order to apply highest damage visuals first
        // And ensure corresponding materials exist.
        if (damageThresholds.Length != damagedMaterials.Length)
        {
            Debug.LogError($"VisualDamageEffector on {gameObject.name}: Damage Thresholds count ({damageThresholds.Length}) " +
                           $"does not match Damaged Materials count ({damagedMaterials.Length}). Please correct this in the Inspector.");
            // To prevent errors, truncate or pad based on the smaller array
            int minLength = Mathf.Min(damageThresholds.Length, damagedMaterials.Length);
            damageThresholds = damageThresholds.Take(minLength).ToArray();
            damagedMaterials = damagedMaterials.Take(minLength).ToArray();
        }
        
        // Combine thresholds and materials, sort by threshold descending, then separate.
        var sortedStates = damageThresholds.Zip(damagedMaterials, (threshold, material) => new { threshold, material })
                                          .OrderByDescending(x => x.threshold)
                                          .ToList();
        damageThresholds = sortedStates.Select(x => x.threshold).ToArray();
        damagedMaterials = sortedStates.Select(x => x.material).ToArray();

        // Ensure effects are initially off
        if (destroyedEffectToActivate != null) destroyedEffectToActivate.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var obj in destroyedObjectsToActivate) { if (obj != null) obj.SetActive(false); }
        foreach (var obj in destroyedObjectsToDeactivate) { if (obj != null) obj.SetActive(true); } // Assume active by default
    }

    /// <summary>
    /// Updates visual state based on the part's current health.
    /// </summary>
    /// <param name="currentHealth">The part's current health.</param>
    /// <param name="maxHealth">The part's maximum health.</param>
    /// <param name="damageAmount">The amount of damage taken (positive for damage, negative for repair).</param>
    public void OnPartDamaged(float currentHealth, float maxHealth, float damageAmount)
    {
        if (targetRenderer == null || originalMaterial == null) return;

        float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;

        // Determine which damage state to apply
        int newDamageStateIndex = -1; // -1 means original/no damage
        for (int i = 0; i < damageThresholds.Length; i++)
        {
            if (healthPercentage <= damageThresholds[i])
            {
                newDamageStateIndex = i;
                break; // Found the highest damage state applicable
            }
        }

        if (newDamageStateIndex != _currentDamageStateIndex)
        {
            _currentDamageStateIndex = newDamageStateIndex;
            ApplyVisualState();
        }

        // Handle destroyed state explicitly if damageAmount makes it destroyed, even if it wasn't exactly 0
        if (currentHealth <= 0)
        {
            OnPartDestroyed();
        }
        else if (currentHealth > 0 && damageAmount < 0 && _damageablePart.IsDestroyed == false) // Repaired from destroyed state
        {
            OnPartRepaired();
        }
    }

    /// <summary>
    /// Applies the visual state based on _currentDamageStateIndex.
    /// </summary>
    private void ApplyVisualState()
    {
        if (targetRenderer == null) return;

        if (_currentDamageStateIndex == -1)
        {
            // Revert to original material
            targetRenderer.material = originalMaterial;
            Debug.Log($"{_damageablePart.PartName} visuals: Reverted to original.");
        }
        else if (_currentDamageStateIndex < damagedMaterials.Length)
        {
            // Apply a damaged material
            targetRenderer.material = damagedMaterials[_currentDamageStateIndex];
            Debug.Log($"{_damageablePart.PartName} visuals: Applied damaged material (state {_currentDamageStateIndex}).");
        }
    }

    /// <summary>
    /// Called when the part's health drops to 0 or below.
    /// Activates destruction effects.
    /// </summary>
    public void OnPartDestroyed()
    {
        if (destroyedEffectToActivate != null)
        {
            destroyedEffectToActivate.Play();
        }
        foreach (var obj in destroyedObjectsToActivate) { if (obj != null) obj.SetActive(true); }
        foreach (var obj in destroyedObjectsToDeactivate) { if (obj != null) obj.SetActive(false); }

        if (targetRenderer != null && damagedMaterials.Length > 0)
        {
            // Always set to the most severely damaged material if available
            targetRenderer.material = damagedMaterials[damagedMaterials.Length - 1];
        }
        Debug.Log($"{_damageablePart.PartName} visuals: Destruction effects activated.");
    }

    /// <summary>
    /// Called when the part is repaired from a destroyed state (health > 0).
    /// Deactivates destruction effects and reverts to appropriate damage state.
    /// </summary>
    public void OnPartRepaired()
    {
        if (destroyedEffectToActivate != null)
        {
            destroyedEffectToActivate.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        foreach (var obj in destroyedObjectsToActivate) { if (obj != null) obj.SetActive(false); }
        foreach (var obj in destroyedObjectsToDeactivate) { if (obj != null) obj.SetActive(true); } // Re-activate original objects

        // Re-evaluate and apply the correct visual state based on current health
        OnPartDamaged(_damageablePart.CurrentHealth, _damageablePart.MaxHealth, 0); // Re-trigger update
        Debug.Log($"{_damageablePart.PartName} visuals: Destruction effects deactivated, restoring visuals.");
    }

    /// <summary>
    /// Called when the part is fully repaired to max health.
    /// </summary>
    public void OnPartFullyRepaired()
    {
        _currentDamageStateIndex = -1; // Back to original state
        ApplyVisualState();
        OnPartRepaired(); // Ensure any destruction effects are cleared
        Debug.Log($"{_damageablePart.PartName} visuals: Fully repaired.");
    }
}
```

### 6. `PerformanceDamageEffector.cs`

```csharp
using UnityEngine;

/// <summary>
/// Implements IVehicleDamageEffector to modify the VehicleController's
/// performance properties based on the health of the associated DamageablePart.
/// </summary>
[RequireComponent(typeof(DamageablePart))] // Ensures a DamageablePart exists on this GameObject
public class PerformanceDamageEffector : MonoBehaviour, IVehicleDamageEffector
{
    [Tooltip("Reference to the VehicleController on the root vehicle GameObject.")]
    [SerializeField] private VehicleController vehicleController;

    [Header("Performance Degradation Settings")]
    [Tooltip("How much engine power multiplier is reduced per unit of health lost on this part.")]
    [SerializeField] private float enginePowerReductionPerHealthUnit = 0f;
    [Tooltip("How much steering sensitivity multiplier is reduced per unit of health lost on this part.")]
    [SerializeField] private float steeringDegradationPerHealthUnit = 0f;
    [Tooltip("How much max speed multiplier is reduced per unit of health lost on this part.")]
    [SerializeField] private float maxSpeedReductionPerHealthUnit = 0f;

    private DamageablePart _damageablePart;
    private float _initialHealth; // Store initial max health to calculate health lost

    // Store the last calculated impact to allow for repair and re-calculation
    private float _lastEnginePowerImpact = 0f;
    private float _lastSteeringImpact = 0f;
    private float _lastMaxSpeedImpact = 0f;

    void Awake()
    {
        _damageablePart = GetComponent<DamageablePart>();
        _initialHealth = _damageablePart.MaxHealth;

        if (vehicleController == null)
        {
            // Try to find the VehicleController on the root parent GameObject
            vehicleController = GetComponentInParent<VehicleController>();
            if (vehicleController == null)
            {
                Debug.LogError($"PerformanceDamageEffector on {gameObject.name}: No VehicleController found in parent hierarchy. " +
                               "Please assign it in the Inspector or ensure one exists on the vehicle root.", this);
            }
        }
        
        // Initialize performance impact based on current health
        ApplyPerformanceImpact(_damageablePart.CurrentHealth);
    }

    /// <summary>
    /// Called when the associated part takes damage.
    /// Recalculates and applies performance penalties.
    /// </summary>
    /// <param name="currentHealth">The part's current health.</param>
    /// <param name="maxHealth">The part's maximum health.</param>
    /// <param name="damageAmount">The amount of damage taken.</param>
    public void OnPartDamaged(float currentHealth, float maxHealth, float damageAmount)
    {
        ApplyPerformanceImpact(currentHealth);
    }

    /// <summary>
    /// Called when the associated part is destroyed.
    /// Applies maximum performance penalties.
    /// </summary>
    public void OnPartDestroyed()
    {
        ApplyPerformanceImpact(0); // Max degradation
        Debug.Log($"{_damageablePart.PartName} performance: Max degradation applied due to destruction.");
    }

    /// <summary>
    /// Called when the associated part is repaired from a destroyed state.
    /// Re-evaluates and applies performance based on new health.
    /// </summary>
    public void OnPartRepaired()
    {
        ApplyPerformanceImpact(_damageablePart.CurrentHealth);
        Debug.Log($"{_damageablePart.PartName} performance: Recalculated after repair.");
    }

    /// <summary>
    /// Called when the associated part is fully repaired.
    /// Removes all performance penalties from this part.
    /// </summary>
    public void OnPartFullyRepaired()
    {
        ApplyPerformanceImpact(_initialHealth); // Restore to original state
        Debug.Log($"{_damageablePart.PartName} performance: Fully repaired, restoring performance.");
    }

    /// <summary>
    /// Calculates the performance impact based on current health and applies it to the VehicleController.
    /// This method ensures that the *net change* from this specific effector is correctly applied.
    /// </summary>
    /// <param name="health">The current health of the part.</param>
    private void ApplyPerformanceImpact(float health)
    {
        if (vehicleController == null || _initialHealth <= 0) return;

        // Calculate health lost relative to max health
        float healthLost = _initialHealth - health;
        healthLost = Mathf.Max(0, healthLost); // Ensure it's not negative

        // Calculate current degradation values
        float currentEnginePowerImpact = healthLost * enginePowerReductionPerHealthUnit;
        float currentSteeringImpact = healthLost * steeringDegradationPerHealthUnit;
        float currentMaxSpeedImpact = healthLost * maxSpeedReductionPerHealthUnit;

        // Apply changes to the vehicle controller's multipliers
        // We subtract the *difference* between the new impact and the last impact
        // This ensures multiple effectors don't 'stack' in an uncontrolled way,
        // rather each effector defines its own *contribution* to the multiplier's reduction.
        
        // Make sure multipliers don't go below 0 (though ideally capped at a minimum like 0.1)
        vehicleController.enginePowerMultiplier -= (currentEnginePowerImpact - _lastEnginePowerImpact);
        vehicleController.enginePowerMultiplier = Mathf.Max(0f, vehicleController.enginePowerMultiplier);

        vehicleController.steeringSensitivityMultiplier -= (currentSteeringImpact - _lastSteeringImpact);
        vehicleController.steeringSensitivityMultiplier = Mathf.Max(0f, vehicleController.steeringSensitivityMultiplier);

        vehicleController.maxSpeedMultiplier -= (currentMaxSpeedImpact - _lastMaxSpeedImpact);
        vehicleController.maxSpeedMultiplier = Mathf.Max(0f, vehicleController.maxSpeedMultiplier);

        _lastEnginePowerImpact = currentEnginePowerImpact;
        _lastSteeringImpact = currentSteeringImpact;
        _lastMaxSpeedImpact = currentMaxSpeedImpact;

        vehicleController.UpdateCalculatedStats(); // Recalculate vehicle's derived stats
        
        Debug.Log($"{_damageablePart.PartName} performance: Engine Power Impact: {currentEnginePowerImpact:F2}, Steering Impact: {currentSteeringImpact:F2}, Speed Impact: {currentMaxSpeedImpact:F2}");
        Debug.Log($"VehicleController current multipliers: Engine={vehicleController.enginePowerMultiplier:F2}, Steering={vehicleController.steeringSensitivityMultiplier:F2}, Speed={vehicleController.maxSpeedMultiplier:F2}");
    }
}
```

### 7. `DamageTester.cs` (Example Usage)

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple script to demonstrate applying damage to the VehicleDamageSystem
/// via UI buttons or input, for testing purposes.
/// </summary>
public class DamageTester : MonoBehaviour
{
    [Header("Target Configuration")]
    [Tooltip("Reference to the VehicleDamageSystem in the scene.")]
    [SerializeField] private VehicleDamageSystem vehicleDamageSystem;
    [Tooltip("Name of the part to target (e.g., 'Engine', 'FrontLeftWheel'). Use 'Random' to hit a random part. Use 'Repair All' to fully repair.")]
    [SerializeField] private string targetPartName = "Engine";
    [Tooltip("Amount of damage to apply when triggered. For repair, this value is ignored and part is fully repaired.")]
    [SerializeField] private float damageAmount = 20f;

    [Header("UI Feedback (Optional)")]
    [SerializeField] private Text overallHealthText;
    [SerializeField] private Text vehicleStateText;
    [SerializeField] private Button damageButton; // Reference to the button itself for enabling/disabling

    private void Start()
    {
        if (vehicleDamageSystem == null)
        {
            vehicleDamageSystem = FindObjectOfType<VehicleDamageSystem>();
            if (vehicleDamageSystem == null)
            {
                Debug.LogError("DamageTester: VehicleDamageSystem not found in scene. Please assign it or add one.", this);
                enabled = false;
                return;
            }
        }

        // Subscribe to global vehicle events for UI feedback
        vehicleDamageSystem.OnVehicleOverallDamaged += UpdateOverallHealthUI;
        vehicleDamageSystem.OnVehicleDestroyed += HandleVehicleDestroyed;
        vehicleDamageSystem.OnVehicleRepaired += HandleVehicleRepaired;
        vehicleDamageSystem.OnVehicleSeverelyDamaged += HandleVehicleSeverelyDamaged;
        vehicleDamageSystem.OnVehicleRecoveredFromSevereDamage += HandleVehicleRecoveredFromSevereDamage;

        UpdateOverallHealthUI(vehicleDamageSystem.OverallHealthPercentage);
        UpdateVehicleStateUI();

        // If this script is on a button, assign its OnClick event
        Button button = GetComponent<Button>();
        if (button != null)
        {
            if (targetPartName.Equals("Repair All", System.StringComparison.OrdinalIgnoreCase))
            {
                button.onClick.AddListener(RepairAllParts);
            }
            else
            {
                button.onClick.AddListener(ApplyDamage);
            }
        }
    }

    private void OnDestroy()
    {
        if (vehicleDamageSystem != null)
        {
            vehicleDamageSystem.OnVehicleOverallDamaged -= UpdateOverallHealthUI;
            vehicleDamageSystem.OnVehicleDestroyed -= HandleVehicleDestroyed;
            vehicleDamageSystem.OnVehicleRepaired -= HandleVehicleRepaired;
            vehicleDamageSystem.OnVehicleSeverelyDamaged -= HandleVehicleSeverelyDamaged;
            vehicleDamageSystem.OnVehicleRecoveredFromSevereDamage -= HandleVehicleRecoveredFromSevereDamage;
        }
    }

    /// <summary>
    /// Applies damage to the configured part or a random part.
    /// This method is typically called by a UI Button's OnClick event.
    /// </summary>
    public void ApplyDamage()
    {
        if (vehicleDamageSystem == null || vehicleDamageSystem.IsVehicleDestroyed) return;

        if (targetPartName.Equals("Random", System.StringComparison.OrdinalIgnoreCase))
        {
            vehicleDamageSystem.ApplyDamageToRandomPart(damageAmount);
        }
        else
        {
            vehicleDamageSystem.ApplyDamageToPart(targetPartName, damageAmount);
        }
        UpdateVehicleStateUI();
    }

    /// <summary>
    /// Fully repairs all parts of the vehicle.
    /// This method is typically called by a UI Button's OnClick event.
    /// </summary>
    public void RepairAllParts()
    {
        if (vehicleDamageSystem == null) return;
        
        vehicleDamageSystem.RepairAllPartsFully();
        UpdateVehicleStateUI();
    }

    private void UpdateOverallHealthUI(float healthPercentage)
    {
        if (overallHealthText != null)
        {
            overallHealthText.text = $"Overall Health: {healthPercentage:P0}";
            overallHealthText.color = Color.Lerp(Color.red, Color.green, healthPercentage);
        }
    }

    private void UpdateVehicleStateUI()
    {
        if (vehicleStateText != null)
        {
            string state = "";
            if (vehicleDamageSystem.IsVehicleDestroyed)
            {
                state = "DESTROYED";
                vehicleStateText.color = Color.black; // Or a specific destroyed color
            }
            else if (vehicleDamageSystem.IsVehicleSeverelyDamaged)
            {
                state = "SEVERELY DAMAGED";
                vehicleStateText.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
            }
            else
            {
                state = "Operational";
                vehicleStateText.color = Color.green;
            }
            vehicleStateText.text = $"Vehicle State: {state}";
        }

        if (damageButton != null)
        {
            damageButton.interactable = !vehicleDamageSystem.IsVehicleDestroyed;
        }
    }

    private void HandleVehicleDestroyed()
    {
        Debug.Log("UI: Vehicle Destroyed!");
        UpdateVehicleStateUI();
    }

    private void HandleVehicleRepaired()
    {
        Debug.Log("UI: Vehicle Repaired!");
        UpdateVehicleStateUI();
    }

    private void HandleVehicleSeverelyDamaged()
    {
        Debug.Log("UI: Vehicle Severely Damaged!");
        UpdateVehicleStateUI();
    }

    private void HandleVehicleRecoveredFromSevereDamage()
    {
        Debug.Log("UI: Vehicle Recovered from Severe Damage!");
        UpdateVehicleStateUI();
    }
}
```