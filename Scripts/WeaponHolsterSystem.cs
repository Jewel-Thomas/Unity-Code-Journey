// Unity Design Pattern Example: WeaponHolsterSystem
// This script demonstrates the WeaponHolsterSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Weapon Holster System** design pattern in Unity. The core idea is to have a central manager (`WeaponHolsterSystem`) that handles a collection of weapons, allowing the player to equip, unequip, and cycle through them. This system uses polymorphism via an `IWeapon` interface, making it flexible and extensible for any type of weapon.

For a complete and practical Unity project, the following C# classes would reside in separate `.cs` files in your Unity project. The comments throughout the code explain the pattern and Unity-specific considerations.

---

### 1. `IWeapon.cs` (Interface Definition)

This interface defines the contract for any object that can be considered a "weapon" within our `WeaponHolsterSystem`. It ensures that all weapons have common functionalities that the system can call polymorphically, without needing to know their specific type.

```csharp
// Filename: IWeapon.cs
using UnityEngine;

public interface IWeapon
{
    /// <summary>
    /// Equips the weapon, making it active and ready for use.
    /// This typically involves activating its GameObject, playing equip animations/sounds, etc.
    /// </summary>
    void Equip();

    /// <summary>
    /// Unequips the weapon, making it inactive and ready to be holstered.
    /// This typically involves deactivating its GameObject, playing unequip animations/sounds, etc.
    /// </summary>
    void Unequip();

    /// <summary>
    /// Performs the weapon's primary attack action.
    /// </summary>
    void Attack();

    /// <summary>
    /// Performs the weapon's reload action.
    /// </summary>
    void Reload();

    /// <summary>
    /// Returns the display name of the weapon.
    /// </summary>
    /// <returns>The weapon's name.</returns>
    string GetWeaponName();

    /// <summary>
    /// Returns the GameObject associated with this weapon.
    /// This is crucial for the HolsterSystem to manage its physical presence (parenting, activation).
    /// </summary>
    /// <returns>The weapon's root GameObject.</returns>
    GameObject GetGameObject();
}
```

---

### 2. `BaseWeapon.cs` (Abstract Base Class for Weapons)

This abstract class provides a common foundation for all concrete weapon implementations. It handles common properties and default behaviors that all weapons might share, such as managing the weapon's GameObject, name, and basic equip/unequip logic. Concrete weapons will inherit from this and override specific behaviors like `Attack()`.

```csharp
// Filename: BaseWeapon.cs
using UnityEngine;

// Example: Weapons might need physics or collision for pickups, or for their own projectiles.
// Add other common components as needed, e.g., AudioSource, Animator.
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public abstract class BaseWeapon : MonoBehaviour, IWeapon
{
    [Header("Weapon Settings")]
    [Tooltip("The display name of the weapon.")]
    [SerializeField] protected string _weaponName = "Default Weapon";

    [Tooltip("The damage dealt by this weapon per attack.")]
    [SerializeField] protected float _damage = 10f;

    [Tooltip("The rate of fire in attacks per second.")]
    [SerializeField] protected float _fireRate = 1f;

    [Tooltip("The maximum number of rounds in the magazine.")]
    [SerializeField] protected int _magazineSize = 30;

    [Tooltip("The current number of rounds in the magazine.")]
    [SerializeField] protected int _currentAmmo;

    [Tooltip("The time it takes to reload the weapon.")]
    [SerializeField] protected float _reloadTime = 2f;

    // Internal state variables for weapon logic
    protected bool _isReloading = false;
    protected float _nextFireTime = 0f;

    // Public properties to access weapon stats
    public string WeaponName => _weaponName;
    public float Damage => _damage;
    public float FireRate => _fireRate;
    public int MagazineSize => _magazineSize;
    public int CurrentAmmo => _currentAmmo;
    public float ReloadTime => _reloadTime;
    public bool IsReloading => _isReloading;


    protected virtual void Awake()
    {
        // Initialize current ammo to full magazine size by default
        _currentAmmo = _magazineSize;

        // Ensure the weapon's GameObject is initially inactive.
        // The WeaponHolsterSystem will manage its activation/deactivation.
        gameObject.SetActive(false);

        // Configure Rigidbody: Weapons should generally be kinematic when parented to the player.
        // If they are meant to be pickable items in the world, they would have non-kinematic RBs.
        // When picked up/holstered, they should become kinematic.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Configure Collider: Make it a trigger when equipped/holstered to avoid physics interactions.
        // It might be a solid collider when it's a world pickup.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    /// <summary>
    /// Implements IWeapon.Equip(). Activates the weapon's GameObject and resets internal state.
    /// </summary>
    public virtual void Equip()
    {
        Debug.Log($"<color=green>{_weaponName}</color> equipped.");
        gameObject.SetActive(true); // Activate the weapon's visual model
        _isReloading = false; // Cancel any active reload when equipping
        CancelInvoke(nameof(FinishReload)); // Ensure reload timer is reset
    }

    /// <summary>
    /// Implements IWeapon.Unequip(). Deactivates the weapon's GameObject and resets internal state.
    /// </summary>
    public virtual void Unequip()
    {
        Debug.Log($"<color=orange>{_weaponName}</color> unequipped.");
        gameObject.SetActive(false); // Deactivate the weapon's visual model
        _isReloading = false; // Cancel any active reload when unequipping
        CancelInvoke(nameof(FinishReload)); // Ensure reload timer is reset
    }

    /// <summary>
    /// Abstract method for weapon-specific attack logic. Must be implemented by concrete classes.
    /// </summary>
    public abstract void Attack();

    /// <summary>
    /// Implements IWeapon.Reload(). Initiates the reload process.
    /// </summary>
    public virtual void Reload()
    {
        if (_isReloading || _currentAmmo == _magazineSize)
        {
            Debug.Log($"<color=grey>{_weaponName}</color>: Cannot reload (already reloading or full).");
            return;
        }

        _isReloading = true;
        Debug.Log($"<color=cyan>{_weaponName}</color> reloading...");
        // Simulate reload time using Invoke. In a real game, this might use Coroutines or an animation event.
        Invoke(nameof(FinishReload), _reloadTime); 
    }

    /// <summary>
    /// Called when the reload duration is complete.
    /// </summary>
    protected virtual void FinishReload()
    {
        _currentAmmo = _magazineSize;
        _isReloading = false;
        Debug.Log($"<color=cyan>{_weaponName}</color> reloaded. Ammo: {_currentAmmo}/{_magazineSize}");
    }

    /// <summary>
    /// Implements IWeapon.GetWeaponName().
    /// </summary>
    public string GetWeaponName()
    {
        return _weaponName;
    }

    /// <summary>
    /// Implements IWeapon.GetGameObject().
    /// </summary>
    public GameObject GetGameObject()
    {
        return gameObject;
    }

    /// <summary>
    /// Utility method to check if the weapon can currently attack.
    /// </summary>
    /// <returns>True if the weapon can attack, false otherwise.</returns>
    protected bool CanAttack()
    {
        if (_isReloading)
        {
            // Debug.Log($"<color=red>{_weaponName}</color>: Cannot attack (reloading).");
            return false;
        }
        if (_currentAmmo <= 0)
        {
            Debug.Log($"<color=red>{_weaponName}</color>: Out of ammo! Reload required.");
            // Optionally, trigger an automatic reload here, or force unequip/switch.
            return false;
        }
        if (Time.time < _nextFireTime)
        {
            // Debug.Log($"<color=red>{_weaponName}</color>: Firing too fast. Wait for fire rate.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Decrements ammo and sets the next allowed fire time based on fire rate.
    /// </summary>
    protected void ConsumeAmmo()
    {
        _currentAmmo--;
        _nextFireTime = Time.time + 1f / _fireRate; // Calculate next fire time based on inverse fire rate
        Debug.Log($"<color=purple>{_weaponName}</color> fired! Ammo left: {_currentAmmo}");

        if (_currentAmmo <= 0)
        {
            Debug.Log($"<color=red>{_weaponName}</color> is out of ammo!");
            // Optionally, trigger an automatic reload or a "click" sound.
        }
    }
}
```

---

### 3. `Pistol.cs` (Concrete Weapon Implementation)

An example of a specific weapon type inheriting from `BaseWeapon`.

```csharp
// Filename: Pistol.cs
using UnityEngine;

public class Pistol : BaseWeapon
{
    [Header("Pistol Specifics")]
    [Tooltip("Does the pistol fire in semi-automatic or burst mode?")]
    [SerializeField] private bool _isSemiAutomatic = true; // True for typical pistol behavior

    // Override Awake to set specific pistol properties
    protected override void Awake()
    {
        base.Awake(); // Always call base.Awake() first!
        
        _weaponName = "9mm Pistol"; 
        _damage = 15f;
        _fireRate = 2f; // 2 shots per second
        _magazineSize = 12;
        _reloadTime = 1.5f;

        // Ensure current ammo is initialized to the pistol's specific magazine size
        _currentAmmo = _magazineSize;
    }

    /// <summary>
    /// Implements the pistol's attack logic.
    /// </summary>
    public override void Attack()
    {
        if (!CanAttack()) return;

        Debug.Log($"<color=red>Pistol ({_weaponName})</color> attacking! Mode: {(_isSemiAutomatic ? "Semi-Auto" : "Burst")}");
        
        // --- Implement Pistol-specific attack logic here ---
        // Example: Raycast from camera, instantiate a bullet prefab, play muzzle flash, etc.
        // For demonstration, we just consume ammo and log.

        ConsumeAmmo();
        // Play specific pistol sound, animation, muzzle flash etc.
    }
}
```

---

### 4. `Rifle.cs` (Concrete Weapon Implementation)

Another example of a specific weapon type, demonstrating different properties and behaviors.

```csharp
// Filename: Rifle.cs
using UnityEngine;

public class Rifle : BaseWeapon
{
    [Header("Rifle Specifics")]
    [Tooltip("The scope magnification level for this rifle.")]
    [SerializeField] private float _scopeMagnification = 2.5f;

    [Tooltip("Is the rifle capable of full-auto fire?")]
    [SerializeField] private bool _canFullAuto = true;

    // Override Awake to set specific rifle properties
    protected override void Awake()
    {
        base.Awake(); // Always call base.Awake() first!
        
        _weaponName = "Assault Rifle"; 
        _damage = 25f;
        _fireRate = 8f; // 8 shots per second (higher RoF)
        _magazineSize = 30;
        _reloadTime = 2.5f;

        // Ensure current ammo is initialized to the rifle's specific magazine size
        _currentAmmo = _magazineSize;
    }

    /// <summary>
    /// Implements the rifle's attack logic.
    /// </summary>
    public override void Attack()
    {
        if (!CanAttack()) return;

        Debug.Log($"<color=red>Rifle ({_weaponName})</color> attacking! Scope: {_scopeMagnification}x, Full-Auto: {_canFullAuto}");
        
        // --- Implement Rifle-specific attack logic here ---
        // Example: More rapid fire, different visual effects, recoil patterns.
        // For demonstration, we just consume ammo and log.

        ConsumeAmmo();
        // Play specific rifle sound, animation, muzzle flash etc.
    }
}
```

---

### 5. `WeaponHolsterSystem.cs` (Core Pattern Implementation)

This is the central component that manages all weapons. It adheres to the Weapon Holster System pattern by:
1.  Maintaining a collection of owned weapons.
2.  Keeping track of the currently equipped weapon.
3.  Providing methods to equip, unequip, and cycle through weapons.
4.  Using the `IWeapon` interface to interact with weapons polymorphically.

```csharp
// Filename: WeaponHolsterSystem.cs
using UnityEngine;
using System.Collections.Generic; // For List
using System.Linq; // For extension methods like .Count(), potentially for filtering/searching

public class WeaponHolsterSystem : MonoBehaviour
{
    [Header("Weapon Holster Settings")]
    [Tooltip("The Transform where the currently equipped weapon will be parented (e.g., a hand bone).")]
    [SerializeField] private Transform _equipParent;

    [Tooltip("The Transform where unequipped weapons will be parented (e.g., a back slot or hip). " +
             "If null, unequipped weapons will simply be deactivated and parented to this HolsterSystem's GameObject.")]
    [SerializeField] private Transform _holsterParent;

    // The collection of all weapons owned by this system.
    // We use IWeapon to store any type of weapon that implements the interface,
    // demonstrating polymorphism.
    private List<IWeapon> _holsteredWeapons = new List<IWeapon>();

    // The index of the currently equipped weapon in the _holsteredWeapons list.
    // -1 indicates no weapon is currently equipped.
    private int _currentWeaponIndex = -1;

    // Reference to the currently active weapon. Null if no weapon is equipped.
    private IWeapon _currentlyEquippedWeapon;

    // Public getters for the current state of the holster system.
    public IWeapon CurrentlyEquippedWeapon => _currentlyEquippedWeapon;
    public int WeaponCount => _holsteredWeapons.Count;

    private void Awake()
    {
        // Essential validation for the equip parent.
        if (_equipParent == null)
        {
            Debug.LogError("WeaponHolsterSystem: Equip Parent is not assigned! " +
                           "Please assign a Transform in the Inspector for equipped weapons (e.g., a 'Hand' transform).", this);
            enabled = false; // Disable the script if essential setup is missing.
        }

        // Initialize the list (though it's already initialized by default, good practice to be explicit).
        _holsteredWeapons = new List<IWeapon>();
    }

    /// <summary>
    /// Adds a new weapon to the holster system. The weapon will be immediately unequipped
    /// and placed in its holstered state (inactive, parented to _holsterParent or this GameObject).
    /// </summary>
    /// <param name="newWeapon">The IWeapon instance (e.g., a Pistol or Rifle component) to add.</param>
    public void AddWeapon(IWeapon newWeapon)
    {
        if (newWeapon == null || _holsteredWeapons.Contains(newWeapon))
        {
            Debug.LogWarning("WeaponHolsterSystem: Attempted to add a null or duplicate weapon to the holster system. " +
                             (newWeapon != null ? newWeapon.GetWeaponName() : "Null Weapon"), newWeapon?.GetGameObject());
            return;
        }

        _holsteredWeapons.Add(newWeapon);
        Debug.Log($"Weapon <color=lime>'{newWeapon.GetWeaponName()}'</color> added to holster. Total weapons: {_holsteredWeapons.Count}");

        // When a weapon is added, ensure it's not active and is parented correctly for a holstered state.
        GameObject weaponGameObject = newWeapon.GetGameObject();
        if (weaponGameObject != null)
        {
            // Parent to the designated holster point, or to the HolsterSystem's own GameObject if no specific holster point.
            weaponGameObject.transform.SetParent(_holsterParent != null ? _holsterParent : transform);
            weaponGameObject.transform.localPosition = Vector3.zero;        // Reset local position
            weaponGameObject.transform.localRotation = Quaternion.identity; // Reset local rotation
            weaponGameObject.transform.localScale = Vector3.one;            // Reset local scale
            weaponGameObject.SetActive(false);                              // Make sure it's inactive
        }
    }

    /// <summary>
    /// Equips a weapon from the holster based on its index in the collection.
    /// If a weapon is currently equipped, it will be unequipped first.
    /// </summary>
    /// <param name="index">The 0-based index of the weapon to equip.</param>
    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= _holsteredWeapons.Count)
        {
            Debug.LogWarning($"WeaponHolsterSystem: Invalid weapon index <color=red>{index}</color>. No weapon equipped.");
            return;
        }

        // If the same weapon is already equipped, do nothing.
        if (_currentWeaponIndex == index)
        {
            // Debug.Log($"Weapon '{_holsteredWeapons[index].GetWeaponName()}' is already equipped.");
            return;
        }

        // 1. Unequip the currently active weapon, if any.
        UnequipCurrentWeapon();

        // 2. Set the new weapon as currently equipped.
        _currentlyEquippedWeapon = _holsteredWeapons[index];
        _currentWeaponIndex = index;

        // 3. Move the weapon's GameObject to the equip parent.
        GameObject weaponGameObject = _currentlyEquippedWeapon.GetGameObject();
        if (weaponGameObject != null)
        {
            weaponGameObject.transform.SetParent(_equipParent);
            weaponGameObject.transform.localPosition = Vector3.zero;        // Reset local position (e.g., to the hand's origin)
            weaponGameObject.transform.localRotation = Quaternion.identity; // Reset local rotation
            weaponGameObject.transform.localScale = Vector3.one;            // Reset local scale (ensure it's not inherited oddly)
        }

        // 4. Call the Equip method on the weapon itself.
        // This allows the weapon to perform its specific equipping logic (animations, sounds, enabling components).
        _currentlyEquippedWeapon.Equip();
        Debug.Log($"<color=lime>Equipped weapon: {_currentlyEquippedWeapon.GetWeaponName()}</color>");
    }

    /// <summary>
    /// Unequips the currently active weapon. It will be moved to the holster parent (if available)
    /// and deactivated. If no weapon is equipped, this method does nothing.
    /// </summary>
    public void UnequipCurrentWeapon()
    {
        if (_currentlyEquippedWeapon == null)
        {
            // Debug.Log("WeaponHolsterSystem: No weapon is currently equipped to unequip.");
            return;
        }

        // 1. Call the Unequip method on the weapon itself.
        // This allows the weapon to perform its specific unequipping logic (animations, sounds, disabling components).
        _currentlyEquippedWeapon.Unequip();

        // 2. Move the weapon's GameObject to the holster parent or just deactivate it.
        GameObject weaponGameObject = _currentlyEquippedWeapon.GetGameObject();
        if (weaponGameObject != null)
        {
            // Parent to the holster point or revert to the HolsterSystem's transform if no specific holster point.
            weaponGameObject.transform.SetParent(_holsterParent != null ? _holsterParent : transform);
            weaponGameObject.transform.localPosition = Vector3.zero;        // Reset local position
            weaponGameObject.transform.localRotation = Quaternion.identity; // Reset local rotation
            weaponGameObject.transform.localScale = Vector3.one;            // Reset local scale
            weaponGameObject.SetActive(false);                              // Ensure it's inactive when holstered
        }

        Debug.Log($"<color=orange>Unequipped weapon: {_currentlyEquippedWeapon.GetWeaponName()}</color>");

        // 3. Clear the current weapon reference and index.
        _currentlyEquippedWeapon = null;
        _currentWeaponIndex = -1;
    }

    /// <summary>
    /// Cycles to the next weapon in the holster collection. If at the end, it wraps around to the beginning.
    /// </summary>
    public void EquipNextWeapon()
    {
        if (_holsteredWeapons.Count == 0)
        {
            Debug.Log("WeaponHolsterSystem: No weapons in holster to equip.");
            return;
        }

        // Calculate the next index, wrapping around if needed.
        int nextIndex = (_currentWeaponIndex + 1) % _holsteredWeapons.Count;
        EquipWeapon(nextIndex);
    }

    /// <summary>
    /// Cycles to the previous weapon in the holster collection. If at the beginning, it wraps around to the end.
    /// </summary>
    public void EquipPreviousWeapon()
    {
        if (_holsteredWeapons.Count == 0)
        {
            Debug.Log("WeaponHolsterSystem: No weapons in holster to equip.");
            return;
        }

        // Calculate the previous index, handling negative result by adding count before modulo.
        int previousIndex = (_currentWeaponIndex - 1 + _holsteredWeapons.Count) % _holsteredWeapons.Count;
        EquipWeapon(previousIndex);
    }

    /// <summary>
    /// Allows the currently equipped weapon to perform its attack.
    /// This is the primary way the HolsterSystem interacts with the active weapon.
    /// </summary>
    public void AttackCurrentWeapon()
    {
        // Null-conditional operator (?.) ensures this call only happens if _currentlyEquippedWeapon is not null.
        _currentlyEquippedWeapon?.Attack();
    }

    /// <summary>
    /// Allows the currently equipped weapon to reload.
    /// </summary>
    public void ReloadCurrentWeapon()
    {
        _currentlyEquippedWeapon?.Reload();
    }

    /// <summary>
    /// Returns a specific weapon by its index.
    /// </summary>
    /// <param name="index">The 0-based index of the weapon.</param>
    /// <returns>The IWeapon at the specified index, or null if index is out of bounds.</returns>
    public IWeapon GetWeaponAtIndex(int index)
    {
        if (index >= 0 && index < _holsteredWeapons.Count)
        {
            return _holsteredWeapons[index];
        }
        return null;
    }
}
```

---

### 6. `PlayerInputController.cs` (Demonstration Script)

This script simulates player input to interact with the `WeaponHolsterSystem`. It acts as a client to the HolsterSystem, demonstrating how to use its public methods.

```csharp
// Filename: PlayerInputController.cs
using UnityEngine;
using System.Collections.Generic; // For List

public class PlayerInputController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the WeaponHolsterSystem component on this GameObject or in the scene.")]
    [SerializeField] private WeaponHolsterSystem _weaponHolsterSystem;

    [Tooltip("Drag the initial weapon GameObjects (Pistol, Rifle, etc.) here from your scene hierarchy. " +
             "These will be added to the holster system on Start. Ensure they have a BaseWeapon-derived script attached.")]
    [SerializeField] private List<BaseWeapon> _initialWeapons;

    private void Start()
    {
        // Try to get the WeaponHolsterSystem if not assigned in the Inspector.
        if (_weaponHolsterSystem == null)
        {
            _weaponHolsterSystem = GetComponent<WeaponHolsterSystem>();
            if (_weaponHolsterSystem == null)
            {
                Debug.LogError("PlayerInputController: WeaponHolsterSystem not found on this GameObject! " +
                               "Please assign it or ensure it's on the same GameObject.", this);
                enabled = false;
                return;
            }
        }

        // Add initial weapons to the holster system from the serialized list.
        if (_initialWeapons != null && _initialWeapons.Count > 0)
        {
            foreach (var weapon in _initialWeapons)
            {
                _weaponHolsterSystem.AddWeapon(weapon);
            }
            // Equip the first weapon after adding all initial ones for an immediate playable state.
            _weaponHolsterSystem.EquipWeapon(0);
        }
        else
        {
            Debug.LogWarning("PlayerInputController: No initial weapons assigned. " +
                             "Add some weapon GameObjects (with BaseWeapon scripts) to the '_initialWeapons' list in the Inspector.", this);
        }

        Debug.Log("\n--- PlayerInputController Ready ---");
        Debug.Log("Controls:");
        Debug.Log("  <color=yellow>Mouse Left Click</color>: Attack Current Weapon");
        Debug.Log("  <color=yellow>R Key</color>: Reload Current Weapon");
        Debug.Log("  <color=yellow>Scroll Wheel Up / Q Key</color>: Equip Next Weapon");
        Debug.Log("  <color=yellow>Scroll Wheel Down / E Key</color>: Equip Previous Weapon");
        Debug.Log("  <color=yellow>1, 2, 3...</color>: Equip Weapon by Slot Number (0-based index)");
        Debug.Log("  <color=yellow>F1 Key</color>: Debug Current Weapon Status");
    }

    private void Update()
    {
        // --- Input for Attack ---
        // GetMouseButton(0) for continuous fire while held down
        if (Input.GetMouseButton(0)) 
        {
            _weaponHolsterSystem.AttackCurrentWeapon();
        }

        // --- Input for Reload ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            _weaponHolsterSystem.ReloadCurrentWeapon();
        }

        // --- Input for Cycling Weapons ---
        // Cycle next weapon (Scroll Wheel Up or 'Q' key)
        if (Input.mouseScrollDelta.y > 0 || Input.GetKeyDown(KeyCode.Q))
        {
            _weaponHolsterSystem.EquipNextWeapon();
        }

        // Cycle previous weapon (Scroll Wheel Down or 'E' key)
        if (Input.mouseScrollDelta.y < 0 || Input.GetKeyDown(KeyCode.E))
        {
            _weaponHolsterSystem.EquipPreviousWeapon();
        }

        // --- Input for Equipping Specific Weapons by Number Key ---
        // Loop through available weapon slots (1, 2, 3...)
        for (int i = 0; i < _weaponHolsterSystem.WeaponCount; i++)
        {
            // KeyCode.Alpha1 corresponds to '1', Alpha2 to '2', etc.
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                _weaponHolsterSystem.EquipWeapon(i);
                break; // Exit loop once a key is pressed to avoid processing further keys.
            }
        }

        // --- Optional: Debug Current Weapon Status ---
        if (Input.GetKeyDown(KeyCode.F1))
        {
            IWeapon current = _weaponHolsterSystem.CurrentlyEquippedWeapon;
            if (current != null)
            {
                BaseWeapon bw = current.GetGameObject().GetComponent<BaseWeapon>(); // Cast back to BaseWeapon for more details
                if (bw != null)
                {
                    Debug.Log($"<color=white>DEBUG:</color> Current Weapon: {bw.GetWeaponName()} | " +
                              $"Ammo: {bw.CurrentAmmo}/{bw.MagazineSize} | " +
                              $"Reloading: {bw.IsReloading}");
                }
            }
            else
            {
                Debug.Log("<color=white>DEBUG:</color> No weapon currently equipped.");
            }
        }
    }
}
```

---

### Unity Scene Setup Instructions:

To get this example running in your Unity project:

1.  **Create C# Scripts:**
    *   Create a new C# script named `IWeapon.cs` and paste the `IWeapon` interface code into it.
    *   Create a new C# script named `BaseWeapon.cs` and paste the `BaseWeapon` abstract class code into it.
    *   Create a new C# script named `Pistol.cs` and paste the `Pistol` class code into it.
    *   Create a new C# script named `Rifle.cs` and paste the `Rifle` class code into it.
    *   Create a new C# script named `WeaponHolsterSystem.cs` and paste the `WeaponHolsterSystem` class code into it.
    *   Create a new C# script named `PlayerInputController.cs` and paste the `PlayerInputController` class code into it.

2.  **Create Player GameObject:**
    *   In your Unity scene, create an empty GameObject and name it `Player`.

3.  **Add Core Components to Player:**
    *   Drag and drop the `WeaponHolsterSystem.cs` script onto the `Player` GameObject in the Inspector.
    *   Drag and drop the `PlayerInputController.cs` script onto the `Player` GameObject in the Inspector.

4.  **Create Equip and Holster Points:**
    *   As children of the `Player` GameObject, create two empty GameObjects:
        *   Name one `Hand` (or `EquipPoint`). This will represent where the weapon is held.
        *   Name the other `HolsterPoint` (or `BackHolster`). This will represent where unequipped weapons are stored.
    *   (Optional: You can adjust their positions relative to the `Player` to better visualize where weapons would appear.)

5.  **Configure WeaponHolsterSystem:**
    *   Select the `Player` GameObject.
    *   In the Inspector, for the `WeaponHolsterSystem` component:
        *   Drag the `Hand` GameObject into the `_equipParent` slot.
        *   Drag the `HolsterPoint` GameObject into the `_holsterParent` slot.

6.  **Create Weapon GameObjects:**
    *   In your scene, create a 3D Cube (GameObject -> 3D Object -> Cube) and rename it `Pistol_Model`.
    *   Drag the `Pistol.cs` script onto the `Pistol_Model` GameObject.
    *   Create another 3D Cube (or Sphere for variation) and rename it `Rifle_Model`.
    *   Drag the `Rifle.cs` script onto the `Rifle_Model` GameObject.
    *   (Optional: You can replace the default Cubes/Spheres with actual weapon models and adjust their scale/rotation within the `Pistol_Model` and `Rifle_Model` GameObjects.)
    *   **Crucial:** Ensure the `Pistol_Model` and `Rifle_Model` GameObjects are **not** children of `Player`, `Hand`, or `HolsterPoint` initially. Place them at (0,0,0) or anywhere in the scene root. The `WeaponHolsterSystem` will manage their parenting.

7.  **Add Weapons to PlayerInputController:**
    *   Select the `Player` GameObject.
    *   In the Inspector, for the `PlayerInputController` component:
        *   Locate the `_initialWeapons` list. Set its size to `2`.
        *   Drag the `Pistol_Model` GameObject from your Hierarchy into the first element of the `_initialWeapons` list.
        *   Drag the `Rifle_Model` GameObject from your Hierarchy into the second element of the `_initialWeapons` list.

8.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   Observe the Debug.Log output in the Console, which will guide you on controls and weapon actions.
    *   Use **Left Mouse Click** to attack, **R** to reload, **Q/E** or **Scroll Wheel** to cycle weapons, and **1/2** to directly select weapons. The equipped weapon will appear at the `Hand` position, and unequipped ones will go to the `HolsterPoint`.

---

This setup provides a complete, practical, and educational example of the Weapon Holster System design pattern in Unity. You can easily extend it by creating more `BaseWeapon`-derived classes for different weapon types, adding visual effects, sounds, and animations.