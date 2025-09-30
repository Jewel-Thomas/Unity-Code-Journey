// Unity Design Pattern Example: WeaponCustomizationSystem
// This script demonstrates the WeaponCustomizationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements a 'Weapon Customization System' which, while not a single GoF design pattern, effectively uses **Composition** and **Scriptable Objects** to create a highly flexible and data-driven system for customizing weapons. It also leverages the **Observer Pattern** (via C# events) for loose coupling.

## WeaponCustomizationSystem - C# Unity Example

This complete example consists of several C# scripts and explains how to set them up in Unity.

```csharp
// --- 1. Core Data Structures ---

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Used for .Contains() on arrays

/// <summary>
/// Represents the fundamental statistics of a weapon or weapon modification.
/// All values are designed to be additive for easy aggregation.
/// </summary>
[System.Serializable] // Makes this struct visible and editable in the Unity Inspector
public struct WeaponStats
{
    public float Damage;        // Base damage dealt per hit
    public float FireRate;      // Rounds per second
    public float Accuracy;      // Lower value means tighter bullet spread (e.g., degrees)
    public float Recoil;        // Higher value means more vertical/horizontal camera kick
    public int MagazineSize;    // Number of rounds a magazine holds
    public float ReloadSpeed;   // Time in seconds to reload

    // Operator overloading for easy additive stat calculation.
    // This allows you to simply add WeaponStats objects together.
    public static WeaponStats operator +(WeaponStats a, WeaponStats b)
    {
        return new WeaponStats
        {
            Damage = a.Damage + b.Damage,
            FireRate = a.FireRate + b.FireRate,
            Accuracy = a.Accuracy + b.Accuracy,
            Recoil = a.Recoil + b.Recoil,
            MagazineSize = a.MagazineSize + b.MagazineSize,
            ReloadSpeed = a.ReloadSpeed + b.ReloadSpeed // Reload speed usually means less time, so +ve value here indicates longer reload
        };
    }

    // A convenient method to display stats for debugging or UI.
    public override string ToString()
    {
        return $"DMG: {Damage:F1}, FR: {FireRate:F1}, ACC: {Accuracy:F1}, REC: {Recoil:F1}, MAG: {MagazineSize}, RLD: {ReloadSpeed:F1}s";
    }

    // Static property for a default (zeroed) WeaponStats struct.
    public static WeaponStats Default => new WeaponStats { 
        Damage = 0, FireRate = 0, Accuracy = 0, Recoil = 0, MagazineSize = 0, ReloadSpeed = 0 
    };
}

/// <summary>
/// Defines different categories or types of mod slots a weapon can have.
/// This helps in managing which mods can attach where (e.g., only one Scope mod).
/// </summary>
public enum WeaponModType
{
    None,           // Default or invalid mod type
    Scope,          // Optical sights
    Barrel,         // Different barrel lengths or types
    Magazine,       // Extended magazines, drum mags
    Stock,          // Buttstocks for recoil control
    Underbarrel,    // Grips, grenade launchers
    LaserSight,     // Laser pointers
    Muzzle          // Suppressors, compensators
    // Add more mod types as your game requires.
}

// --- 2. Scriptable Objects for Data Definition ---

/// <summary>
/// Base ScriptableObject for defining a core weapon type (e.g., "Pistol", "Assault Rifle").
/// This acts as a blueprint, holding the weapon's default statistics and
/// specifying which types of modification slots it offers.
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponBase", menuName = "Weapon Customization/Weapon Base Definition")]
public class WeaponBaseSO : ScriptableObject
{
    public string WeaponName = "New Weapon";            // Display name of the base weapon
    public GameObject BasePrefab;                       // Optional: Visual prefab for the unmodded base weapon
    public WeaponStats BaseStats;                       // The default stats of this weapon
    
    [Tooltip("Defines which mod types this weapon can accept. E.g., a pistol might not accept a stock.")]
    public WeaponModType[] AllowedModSlots;             // Array of mod types this weapon supports
}

/// <summary>
/// ScriptableObject for defining a specific weapon modification (e.g., "Red Dot Sight", "Silencer").
/// This acts as a blueprint for a mod, detailing its type, visual representation, and
/// how it alters the weapon's statistics.
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponMod", menuName = "Weapon Customization/Weapon Mod Definition")]
public class WeaponModSO : ScriptableObject
{
    public string ModName = "New Mod";                  // Display name of the mod
    public GameObject ModPrefab;                        // Optional: Visual prefab for this mod
    
    [Tooltip("The type of slot this mod occupies. Only one mod of a given type can be active per weapon.")]
    public WeaponModType ModType = WeaponModType.None;  // The slot type this mod belongs to
    
    [Tooltip("The changes this mod applies to the weapon's base stats. These are additive.")]
    public WeaponStats StatChanges;                     // The statistical changes this mod provides
}

// --- 3. The Customization System (MonoBehaviour) ---

/// <summary>
/// The core MonoBehaviour component that manages weapon customization on a GameObject.
/// It combines a `WeaponBaseSO` with a collection of `WeaponModSO`s to calculate
/// and provide the weapon's current effective stats.
///
/// This component demonstrates the **Composition Pattern**: A `CustomizableWeapon`
/// is composed of a `WeaponBaseSO` and a collection of `WeaponModSO`s. Its overall
/// behavior (stats) is an aggregation of its constituent parts.
/// </summary>
[DisallowMultipleComponent] // Ensures only one customization system per weapon GameObject
public class CustomizableWeapon : MonoBehaviour
{
    // --- Public Properties & Events ---
    
    [Header("Base Weapon Setup")]
    [Tooltip("The base definition of this weapon (e.g., Assault Rifle, Pistol).")]
    public WeaponBaseSO BaseWeaponData;

    /// <summary>
    /// The current aggregated stats of the weapon, including all applied mods.
    /// This property is read-only from outside to ensure stats are only calculated
    /// internally by the system.
    /// </summary>
    public WeaponStats CurrentStats { get; private set; }

    /// <summary>
    /// Event fired whenever the weapon's stats change due to mod attachment/detachment.
    /// This is an example of the **Observer Pattern**. Other components (UI, firing logic)
    /// can subscribe to this event to react to changes without direct coupling.
    /// </summary>
    public event Action<WeaponStats> OnStatsChanged;

    // --- Private Members ---

    // Stores currently active mods, mapping mod type to the mod ScriptableObject.
    // This dictionary ensures that only one mod of a given `WeaponModType`
    // can be active in its designated "slot" at any given time.
    private Dictionary<WeaponModType, WeaponModSO> _activeMods = new Dictionary<WeaponModType, WeaponModSO>();
    
    // Stores references to the instantiated mod prefabs for visual representation.
    // This allows for dynamic visual changes when mods are attached/detached.
    private Dictionary<WeaponModType, GameObject> _activeModVisuals = new Dictionary<WeaponModType, GameObject>();

    // --- Unity Lifecycle ---

    private void Awake()
    {
        if (BaseWeaponData == null)
        {
            Debug.LogError($"CustomizableWeapon on {gameObject.name} has no BaseWeaponData assigned! Please assign a WeaponBaseSO asset.", this);
            enabled = false; // Disable the component if it can't function properly
            return;
        }

        // Initialize the weapon by calculating its initial stats based on the base data.
        CalculateCurrentStats();
    }

    // --- Public Customization Methods ---

    /// <summary>
    /// Attempts to attach a new mod to the weapon.
    /// If a mod of the same `ModType` is already present, it will be replaced by the new mod.
    /// This method manages both the data (which mods are active) and the visual representation.
    /// </summary>
    /// <param name="modToAttach">The ScriptableObject defining the mod to attach.</param>
    /// <returns>True if the mod was successfully attached or replaced, false otherwise (e.g., invalid mod, unsupported type).</returns>
    public bool AttachMod(WeaponModSO modToAttach)
    {
        if (modToAttach == null || modToAttach.ModType == WeaponModType.None)
        {
            Debug.LogWarning($"Attempted to attach an invalid mod to {gameObject.name}. Mod was null or its ModType was 'None'.");
            return false;
        }

        if (BaseWeaponData == null)
        {
            Debug.LogError($"Cannot attach mod '{modToAttach.ModName}' to {gameObject.name}: BaseWeaponData is missing.");
            return false;
        }

        // Check if the base weapon definition allows this specific type of mod.
        if (!BaseWeaponData.AllowedModSlots.Contains(modToAttach.ModType))
        {
            Debug.LogWarning($"Weapon '{BaseWeaponData.WeaponName}' does not support mods of type '{modToAttach.ModType}'. Cannot attach '{modToAttach.ModName}'.", this);
            return false;
        }

        // If a mod of the same type is already attached, detach it first to replace it.
        if (_activeMods.ContainsKey(modToAttach.ModType))
        {
            Debug.Log($"Replacing existing '{_activeMods[modToAttach.ModType].ModName}' with '{modToAttach.ModName}' in slot '{modToAttach.ModType}' on {BaseWeaponData.WeaponName}.", this);
            DetachMod(modToAttach.ModType); // This also handles destroying the old visual.
        }

        // Add the new mod to our active mods dictionary.
        _activeMods.Add(modToAttach.ModType, modToAttach);

        // Handle the visual aspect: Instantiate the mod's prefab as a child of this weapon.
        if (modToAttach.ModPrefab != null)
        {
            GameObject modVisual = Instantiate(modToAttach.ModPrefab, transform); // Attach as a child
            modVisual.name = $"{modToAttach.ModName}_Visual"; // Rename for clarity in Hierarchy
            _activeModVisuals.Add(modToAttach.ModType, modVisual); // Store reference to the visual
        }

        Debug.Log($"Attached mod: {modToAttach.ModName} to {BaseWeaponData.WeaponName}.", this);
        CalculateCurrentStats(); // Recalculate stats and notify subscribers.
        return true;
    }

    /// <summary>
    /// Detaches a mod of a specific type from the weapon.
    /// This removes the mod's statistical effects and its visual representation.
    /// </summary>
    /// <param name="modTypeToDetach">The type of mod to detach (e.g., WeaponModType.Scope).</param>
    /// <returns>True if a mod was successfully detached, false otherwise (e.g., no mod of that type was attached).</returns>
    public bool DetachMod(WeaponModType modTypeToDetach)
    {
        if (modTypeToDetach == WeaponModType.None)
        {
            Debug.LogWarning($"Attempted to detach an invalid mod type (None) from {gameObject.name}.");
            return false;
        }

        // Try to remove the mod from the active mods dictionary.
        if (_activeMods.Remove(modTypeToDetach))
        {
            Debug.Log($"Detached mod from slot: {modTypeToDetach} on {BaseWeaponData.WeaponName}.", this);

            // Handle the visual aspect: Destroy the instantiated mod prefab.
            if (_activeModVisuals.TryGetValue(modTypeToDetach, out GameObject visualObject))
            {
                Destroy(visualObject); // Remove the visual object from the scene
                _activeModVisuals.Remove(modTypeToDetach); // Remove its reference
            }

            CalculateCurrentStats(); // Recalculate stats and notify subscribers.
            return true;
        }
        else
        {
            Debug.Log($"No mod of type {modTypeToDetach} found to detach on {gameObject.name}.", this);
            return false;
        }
    }
    
    /// <summary>
    /// Retrieves the `WeaponModSO` currently attached to a specific slot type.
    /// </summary>
    /// <param name="modType">The type of slot to check.</param>
    /// <returns>The `WeaponModSO` if found in that slot, otherwise `null`.</returns>
    public WeaponModSO GetModInSlot(WeaponModType modType)
    {
        _activeMods.TryGetValue(modType, out WeaponModSO mod);
        return mod;
    }

    /// <summary>
    /// Removes all mods currently attached to the weapon, resetting it to its base state.
    /// </summary>
    public void ClearAllMods()
    {
        if (_activeMods.Count == 0) return; // No mods to clear

        Debug.Log($"Clearing all mods from {BaseWeaponData.WeaponName}.", this);

        _activeMods.Clear(); // Clear all data references

        // Destroy all instantiated mod visuals.
        foreach (var visual in _activeModVisuals.Values)
        {
            Destroy(visual);
        }
        _activeModVisuals.Clear(); // Clear all visual references

        CalculateCurrentStats(); // Recalculate stats and notify subscribers.
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Calculates the weapon's effective stats by combining its base stats
    /// with the statistical changes from all currently active modifications.
    /// Updates the `CurrentStats` property and fires the `OnStatsChanged` event.
    /// This is the core statistical aggregation logic of the system.
    /// </summary>
    private void CalculateCurrentStats()
    {
        // Start with the weapon's base statistics.
        WeaponStats newStats = BaseWeaponData.BaseStats;

        // Iterate through all active mods and add their statistical changes.
        foreach (var mod in _activeMods.Values)
        {
            newStats += mod.StatChanges; // Uses the overloaded '+' operator in WeaponStats.
        }

        // Apply any necessary clamping or special rules for stats.
        // For example, accuracy should never be negative, fire rate should have a minimum.
        newStats.Accuracy = Mathf.Max(0f, newStats.Accuracy);           // Accuracy can't be better than perfect
        newStats.FireRate = Mathf.Max(0.1f, newStats.FireRate);         // Minimum fire rate to prevent division by zero
        newStats.MagazineSize = Mathf.Max(1, newStats.MagazineSize);    // Minimum magazine size of 1

        CurrentStats = newStats; // Update the public property.
        OnStatsChanged?.Invoke(CurrentStats); // Fire the event, notifying any subscribers.
        
        Debug.Log($"Stats updated for {BaseWeaponData.WeaponName}: {CurrentStats}", this);
    }
    
    // Public accessor for debugging/displaying active mods in the Inspector/UI.
    public IReadOnlyDictionary<WeaponModType, WeaponModSO> GetActiveMods() => _activeMods;
}

// --- 4. Example Usage (MonoBehaviour for Demonstration and UI Interaction) ---

/// <summary>
/// This script demonstrates how to interact with the `CustomizableWeapon` system.
/// It acts as a bridge between UI elements (buttons) and the customization logic,
/// and updates a UI Text element to display the weapon's current stats.
///
/// Attach this script to the same GameObject as your `CustomizableWeapon` component
/// or link it via a public field.
/// </summary>
public class WeaponDemoManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CustomizableWeapon component this manager will control.")]
    public CustomizableWeapon TargetWeapon;

    [Tooltip("TextMeshProUGUI component to display weapon stats. Requires TextMeshPro package.")]
    public TMPro.TextMeshProUGUI StatsDisplayText; 
    
    [Tooltip("An optional UI Panel to ensure it's active when the demo starts.")]
    public GameObject DebugTextPanel; 
    
    [Header("Demo Mods (Drag your created WeaponModSO assets here)")]
    public WeaponModSO ScopeMod;
    public WeaponModSO SilencerMod;         // Example: Muzzle type mod
    public WeaponModSO ExtendedMagMod;      // Example: Magazine type mod
    public WeaponModSO LaserMod;            // Example: LaserSight type mod
    public WeaponModSO HeavyBarrelMod;      // Example: Barrel type mod
    public WeaponModSO AnotherScopeMod;     // Example: Another Scope type mod to demonstrate replacement

    private void Start()
    {
        // Auto-find CustomizableWeapon if not assigned in Inspector
        if (TargetWeapon == null)
        {
            TargetWeapon = GetComponent<CustomizableWeapon>();
            if (TargetWeapon == null)
            {
                Debug.LogError("WeaponDemoManager requires a CustomizableWeapon component on the same or a linked GameObject!");
                enabled = false;
                return;
            }
        }

        // Subscribe to the OnStatsChanged event. This demonstrates the Observer Pattern.
        // When the weapon's stats change, our UpdateStatsDisplay method will be called automatically.
        TargetWeapon.OnStatsChanged += UpdateStatsDisplay;

        // Perform an initial display update to show base stats.
        UpdateStatsDisplay(TargetWeapon.CurrentStats);
        
        // Ensure the debug UI panel is active if assigned.
        DebugTextPanel?.SetActive(true); 
    }

    private void OnDestroy()
    {
        // It's crucial to unsubscribe from events to prevent memory leaks,
        // especially if the TargetWeapon might outlive this manager.
        if (TargetWeapon != null)
        {
            TargetWeapon.OnStatsChanged -= UpdateStatsDisplay;
        }
    }

    /// <summary>
    /// Updates the TextMeshPro UI element with the current weapon statistics.
    /// This method is called whenever the `OnStatsChanged` event is fired.
    /// </summary>
    /// <param name="stats">The latest calculated WeaponStats.</param>
    private void UpdateStatsDisplay(WeaponStats stats)
    {
        if (StatsDisplayText != null)
        {
            string modDetails = FormatActiveMods();
            StatsDisplayText.text = $"<color=orange>{TargetWeapon.BaseWeaponData.WeaponName}</color>\n" +
                                    $"<size=12>Base Stats: {TargetWeapon.BaseWeaponData.BaseStats.ToString()}\n</size>" +
                                    $"<size=14><b>Current Stats:</b></size>\n" +
                                    $"Damage: {stats.Damage:F1}\n" +
                                    $"Fire Rate: {stats.FireRate:F1}\n" +
                                    $"Accuracy: {stats.Accuracy:F1}\n" +
                                    $"Recoil: {stats.Recoil:F1}\n" +
                                    $"Mag Size: {stats.MagazineSize}\n" +
                                    $"Reload: {stats.ReloadSpeed:F1}s\n" +
                                    $"<size=12><b>Active Mods:</b></size>\n" +
                                    modDetails;
        }
    }

    /// <summary>
    /// Helper to format the list of currently active mods for display.
    /// </summary>
    private string FormatActiveMods()
    {
        // Using the public accessor for the dictionary of active mods.
        var mods = TargetWeapon.GetActiveMods(); 
        if (mods.Count == 0) return "  - None";

        string modString = "";
        foreach (var entry in mods)
        {
            modString += $"  - {entry.Key}: {entry.Value.ModName}\n";
        }
        return modString;
    }

    // --- Public methods to be called by UI Buttons ---
    // These methods simply call the corresponding methods on the CustomizableWeapon.

    public void OnAttachScope() { TargetWeapon.AttachMod(ScopeMod); }
    public void OnDetachScope() { TargetWeapon.DetachMod(WeaponModType.Scope); }

    public void OnAttachSilencer() { TargetWeapon.AttachMod(SilencerMod); }
    public void OnDetachSilencer() { TargetWeapon.DetachMod(WeaponModType.Muzzle); } // Silencer is a Muzzle mod

    public void OnAttachExtendedMag() { TargetWeapon.AttachMod(ExtendedMagMod); }
    public void OnDetachExtendedMag() { TargetWeapon.DetachMod(WeaponModType.Magazine); }

    public void OnAttachLaser() { TargetWeapon.AttachMod(LaserMod); }
    public void OnDetachLaser() { TargetWeapon.DetachMod(WeaponModType.LaserSight); }

    public void OnAttachHeavyBarrel() { TargetWeapon.AttachMod(HeavyBarrelMod); }
    public void OnDetachHeavyBarrel() { TargetWeapon.DetachMod(WeaponModType.Barrel); }

    public void OnAttachAnotherScope() { TargetWeapon.AttachMod(AnotherScopeMod); } // This will replace the first scope

    public void OnClearAllMods() { TargetWeapon.ClearAllMods(); }
}

// --- 5. Simplified Weapon Firing Component (for completeness) ---

/// <summary>
/// A very basic example of a weapon firing component that utilizes the
/// `CustomizableWeapon`'s `CurrentStats` to drive its behavior (fire rate, damage, accuracy).
///
/// Attach this script to the same GameObject as your `CustomizableWeapon`.
/// </summary>
public class WeaponFireSystem : MonoBehaviour
{
    private CustomizableWeapon _customizableWeapon;
    private float _nextFireTime; // Tracks when the weapon can fire next

    [Header("Firing Settings")]
    [Tooltip("The point from which projectiles will spawn.")]
    public Transform FirePoint; 
    [Tooltip("The prefab for the projectile fired by this weapon.")]
    public GameObject ProjectilePrefab; 
    public float ProjectileSpeed = 50f; // Speed of the projectile

    private void Awake()
    {
        _customizableWeapon = GetComponent<CustomizableWeapon>();
        if (_customizableWeapon == null)
        {
            Debug.LogError("WeaponFireSystem requires a CustomizableWeapon component on the same GameObject!");
            enabled = false;
        }

        // If no specific fire point is set, use the weapon's transform.
        if (FirePoint == null)
        {
            FirePoint = transform; 
        }
    }

    private void Update()
    {
        // Example: Fire when left mouse button is held down and enough time has passed.
        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime)
        {
            Fire();
            // Calculate the next possible fire time based on the weapon's current fire rate.
            _nextFireTime = Time.time + (1f / _customizableWeapon.CurrentStats.FireRate);
        }
    }

    private void Fire()
    {
        if (ProjectilePrefab == null)
        {
            Debug.LogWarning("No ProjectilePrefab assigned to WeaponFireSystem! Cannot fire.");
            return;
        }

        // Log current firing stats for demonstration.
        Debug.Log($"Firing {gameObject.name} with Damage: {_customizableWeapon.CurrentStats.Damage:F1}, " +
                  $"Accuracy (spread): {_customizableWeapon.CurrentStats.Accuracy:F1} degrees.");

        // Calculate projectile rotation with spread based on weapon accuracy.
        // Random.insideUnitCircle gives a vector within a circle, we map it to degrees.
        Vector2 spread = UnityEngine.Random.insideUnitCircle * _customizableWeapon.CurrentStats.Accuracy;
        Quaternion spreadRotation = Quaternion.Euler(spread.y, spread.x, 0);

        // Instantiate the projectile at the fire point with the calculated spread.
        GameObject projectileGO = Instantiate(ProjectilePrefab, FirePoint.position, FirePoint.rotation * spreadRotation);
        
        // Try to get a Rigidbody or Projectile script to give it initial velocity.
        Rigidbody rb = projectileGO.GetComponent<Rigidbody>();
        Projectile projectileScript = projectileGO.GetComponent<Projectile>();

        if (rb != null)
        {
            rb.velocity = projectileGO.transform.forward * ProjectileSpeed;
        }
        
        if (projectileScript != null)
        {
            projectileScript.Initialize(projectileGO.transform.forward * ProjectileSpeed, _customizableWeapon.CurrentStats.Damage);
        }
        else if (rb == null) // If no Rigidbody and no Projectile script, destroy after a short time
        {
             Destroy(projectileGO, 5f);
        }
    }
}

// Minimal Projectile script for demo purposes.
// Attach this to your ProjectilePrefab. Requires a Rigidbody and Collider.
public class Projectile : MonoBehaviour
{
    public float speed;
    public float damage;
    public float lifeTime = 3f;

    // Initialize the projectile with direction and damage.
    public void Initialize(Vector3 direction, float dmg)
    {
        damage = dmg;
        // Assuming the projectile prefab has a Rigidbody.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction;
        }
        else
        {
            Debug.LogWarning("Projectile prefab is missing a Rigidbody component. Projectile will not move.");
        }
        Destroy(gameObject, lifeTime); // Destroy itself after its lifetime.
    }

    // Example of what happens when the projectile hits something.
    private void OnTriggerEnter(Collider other)
    {
        // Avoid hitting self or other projectiles.
        if (other.isTrigger || other.gameObject.layer == gameObject.layer) return; 

        // Apply damage to anything with a hypothetical 'Health' component.
        // For a real game, you'd add an interface or base class for damageable objects.
        Debug.Log($"Projectile hit {other.name}, dealing {damage:F1} damage.");
        // Example: other.GetComponent<HealthComponent>()?.TakeDamage(damage);

        Destroy(gameObject); // Destroy the projectile on impact.
    }
}
```

---

### How to Set Up in Unity:

**1. Create the C# Scripts:**
   - Create a new C# script named `WeaponCustomizationSystem.cs`. Copy the entire code block above and paste it into this single script.
   - Alternatively, you can split it into multiple files (e.g., `WeaponStats.cs`, `WeaponBaseSO.cs`, `WeaponModSO.cs`, `CustomizableWeapon.cs`, `WeaponDemoManager.cs`, `WeaponFireSystem.cs`, `Projectile.cs`) but having them in one file makes it easier to copy-paste for this example.

**2. Install TextMeshPro (if you want UI display):**
   - Go to `Window > TextMeshPro > Import TMP Essential Resources` in the Unity Editor.

**3. Create Prefabs (Simple Visuals for Demo):**
   - **Base Weapon Prefab:** Create a 3D Cube (or a more complex model). Rename it `AssaultRifle_Visual`. This will be the `BasePrefab` for your weapon definition.
   - **Mod Prefabs:** Create several smaller 3D objects (e.g., small cubes, cylinders, spheres). Rename them `RedDot_Visual`, `Silencer_Visual`, `ExtendedMag_Visual`, `Laser_Visual`, `HeavyBarrel_Visual`, `SniperScope_Visual`. These will be `ModPrefab`s.
   - **Projectile Prefab:** Create a small 3D Sphere. Add a `Rigidbody` component to it (uncheck `Use Gravity`). Add a `Sphere Collider` component to it and set `Is Trigger` to true. Attach the `Projectile.cs` script to it. This will be your `ProjectilePrefab`.

**4. Create Scriptable Objects (Data Definitions):**
   - In your Project window, right-click -> `Create` -> `Weapon Customization` -> `Weapon Base Definition`.
     - Name it `AR_Base`.
     - Set `Weapon Name`: `Assault Rifle`.
     - Drag your `AssaultRifle_Visual` prefab into the `Base Prefab` slot.
     - Fill in `Base Stats` (e.g., Damage: 20, Fire Rate: 8, Accuracy: 5, Recoil: 10, Magazine Size: 30, Reload Speed: 2.5).
     - Set `Allowed Mod Slots`: Click the `+` and add `Scope`, `Barrel`, `Magazine`, `Muzzle`, `Underbarrel`, `LaserSight`.
   - Right-click -> `Create` -> `Weapon Customization` -> `Weapon Mod Definition`. Repeat for each mod:
     - **`RedDot_Scope`**:
       - `Mod Name`: `Red Dot Sight`
       - `Mod Prefab`: Drag `RedDot_Visual`
       - `Mod Type`: `Scope`
       - `Stat Changes`: Accuracy: -2, Recoil: -1 (improves accuracy, reduces recoil slightly)
     - **`Silencer_Muzzle`**:
       - `Mod Name`: `Silencer`
       - `Mod Prefab`: Drag `Silencer_Visual`
       - `Mod Type`: `Muzzle`
       - `Stat Changes`: Damage: -3, Recoil: -2 (reduces damage, further reduces recoil)
     - **`Extended_Magazine`**:
       - `Mod Name`: `Extended Mag`
       - `Mod Prefab`: Drag `ExtendedMag_Visual`
       - `Mod Type`: `Magazine`
       - `Stat Changes`: Magazine Size: +15, Reload Speed: +0.5 (more ammo, slightly slower reload)
     - **`Laser_Sight`**:
       - `Mod Name`: `Laser Sight`
       - `Mod Prefab`: Drag `Laser_Visual`
       - `Mod Type`: `LaserSight`
       - `Stat Changes`: Accuracy: -2 (improves accuracy)
     - **`Heavy_Barrel`**:
       - `Mod Name`: `Heavy Barrel`
       - `Mod Prefab`: Drag `HeavyBarrel_Visual`
       - `Mod Type`: `Barrel`
       - `Stat Changes`: Damage: +5, Fire Rate: -1, Recoil: +5 (more damage, slower fire, more recoil)
     - **`Sniper_Scope`**:
       - `Mod Name`: `Sniper Scope`
       - `Mod Prefab`: Drag `SniperScope_Visual`
       - `Mod Type`: `Scope`
       - `Stat Changes`: Accuracy: -8, Recoil: -3 (greatly improves accuracy, reduces recoil)

**5. Set Up the Scene:**
   - Create an empty GameObject in your scene. Name it `Customizable_AR`.
   - Add the `CustomizableWeapon` component to `Customizable_AR`.
     - Drag your `AR_Base` ScriptableObject into the `Base Weapon Data` slot.
   - Add the `WeaponDemoManager` component to `Customizable_AR`.
     - Drag all your `WeaponModSO` assets into their corresponding slots on `WeaponDemoManager`.
   - Add the `WeaponFireSystem` component to `Customizable_AR`.
     - Drag your `ProjectilePrefab` into the `Projectile Prefab` slot.
     - (Optional) Create an empty GameObject named `FirePoint` as a child of `Customizable_AR` and drag it to the `Fire Point` slot (or leave it empty to use the weapon's own transform).
   - **Create UI:**
     - Right-click in Hierarchy -> `UI` -> `Canvas`.
     - On the Canvas, right-click -> `UI` -> `TextMeshPro - Text`. Rename it `StatsDisplay_Text`. Adjust its size and position.
     - On the Canvas, create a `UI Panel` (right-click -> `UI` -> `Panel`). Rename it `ButtonsPanel`. Resize and position it below the text.
     - On the `ButtonsPanel`, create several `UI Button - TextMeshPro` objects. For each button:
       - Set the Button's text (e.g., "Attach Scope", "Detach Scope", "Attach Silencer").
       - In the Button's `OnClick()` event list:
         - Drag `Customizable_AR` into the Object slot.
         - Select `WeaponDemoManager` from the dropdown.
         - Choose the corresponding method (e.g., `OnAttachScope()`, `OnDetachScope()`).
   - Drag `StatsDisplay_Text` into the `Stats Display Text` slot on `Customizable_AR`'s `WeaponDemoManager` component.
   - Drag `ButtonsPanel` into the `Debug Text Panel` slot on `Customizable_AR`'s `WeaponDemoManager` component.

**6. Run the Scene:**
   - You should see the `Customizable_AR` GameObject. Its children will automatically update as you attach/detach mods.
   - The UI Text will display the current stats.
   - Clicking the UI buttons will apply/remove mods, updating stats and visuals.
   - Holding down the left mouse button will fire the weapon, demonstrating its current fire rate, damage, and accuracy.

This comprehensive example provides a practical and educational demonstration of a 'Weapon Customization System' in Unity, built on robust design principles.