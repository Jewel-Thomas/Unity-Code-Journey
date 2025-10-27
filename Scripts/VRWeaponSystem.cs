// Unity Design Pattern Example: VRWeaponSystem
// This script demonstrates the VRWeaponSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'VRWeaponSystem' design pattern in VR Unity development focuses on creating a flexible, extensible, and maintainable system for handling various weapons. It leverages principles like abstraction, polymorphism, and separation of concerns to allow easy integration of new weapon types and managing their behavior.

**Core Principles of the VRWeaponSystem Pattern:**

1.  **Abstraction (IVRWeapon):** Defines a common interface or contract that all weapons must adhere to. This ensures that the weapon manager can interact with any weapon type in a uniform way, regardless of its specific implementation (e.g., pistol, rifle, sword).
2.  **Base Implementation (VRWeaponBase):** An abstract base class that implements the `IVRWeapon` interface and provides common functionalities shared by most weapons (e.g., ammo management, firing cooldowns, reloading logic, equip/unequip events). This reduces code duplication.
3.  **Concrete Weapons (PistolVRWeapon, AssaultRifleVRWeapon):** Specific weapon classes that inherit from `VRWeaponBase` and implement weapon-unique behaviors (e.g., how a pistol fires versus a rifle, unique visual/audio effects).
4.  **Weapon Management (VRWeaponManager):** A central component responsible for holding a collection of weapons, managing the currently equipped weapon, switching between weapons, and translating player input (from VR controllers) into weapon actions.
5.  **Separation of Concerns:**
    *   **Weapon Manager:** Focuses on *which* weapon is active and *when* to perform an action based on input.
    *   **Individual Weapons:** Focus on *how* they perform their actions (firing, reloading, etc.) and manage their internal state.
    *   **VR Input System (External):** Provides the raw input signals which the `VRWeaponManager` then interprets. (In this example, we'll use keyboard input as a stand-in for a VR input system).

---

## Complete C# Unity Example: VRWeaponSystem

This example provides a complete, ready-to-use set of scripts that demonstrate the VRWeaponSystem pattern.

### 1. `IVRWeapon.cs` (Interface)

This interface defines the contract that all weapons in our system must follow. It ensures that the `VRWeaponManager` can interact with any weapon polymorphically.

```csharp
using UnityEngine;
using System;

namespace VRWeaponSystem
{
    // Define an enumeration for different fire modes a weapon might have.
    public enum FireMode { SemiAuto, FullAuto }

    /// <summary>
    /// The IVRWeapon interface defines the essential contract for any weapon in the VRWeaponSystem.
    /// It specifies properties and methods that all weapons must implement, allowing the VRWeaponManager
    /// to interact with them polymorphically.
    /// </summary>
    public interface IVRWeapon
    {
        // --- Properties ---

        /// <summary>
        /// Gets the name of the weapon.
        /// </summary>
        string WeaponName { get; }

        /// <summary>
        /// Gets the current amount of ammo in the weapon's magazine.
        /// </summary>
        int CurrentAmmoInMagazine { get; }

        /// <summary>
        /// Gets the maximum capacity of the weapon's magazine.
        /// </summary>
        int MagazineSize { get; }

        /// <summary>
        /// Gets the total amount of spare ammo the player has for this weapon.
        /// </summary>
        int TotalSpareAmmo { get; }

        /// <summary>
        /// Gets the maximum total ammo (magazine + spare) the player can carry for this weapon.
        /// </summary>
        int MaxTotalAmmo { get; }

        /// <summary>
        /// Gets the current firing mode of the weapon (e.g., Semi-Auto, Full-Auto).
        /// </summary>
        FireMode CurrentFireMode { get; }

        /// <summary>
        /// Gets a value indicating whether the weapon is currently equipped and active.
        /// </summary>
        bool IsEquipped { get; }

        /// <summary>
        /// Gets a value indicating whether the weapon is currently reloading.
        /// </summary>
        bool IsReloading { get; }

        /// <summary>
        /// Gets a value indicating whether the weapon can currently fire.
        /// This considers ammo, cooldown, and reloading state.
        /// </summary>
        bool CanFire { get; }

        // --- Methods ---

        /// <summary>
        /// Attempts to fire the weapon. The actual firing logic (bullet instantiation, raycast)
        /// is handled by the concrete weapon implementation.
        /// </summary>
        void Fire();

        /// <summary>
        /// Attempts to reload the weapon.
        /// </summary>
        void Reload();

        /// <summary>
        /// Switches the weapon's firing mode to the next available mode.
        /// </summary>
        void SwitchFireMode();

        /// <summary>
        /// Equips the weapon, making it active and visible.
        /// </summary>
        void Equip();

        /// <summary>
        /// Unequips the weapon, making it inactive and potentially invisible.
        /// </summary>
        void Unequip();

        /// <summary>
        /// Adds ammo to the weapon's total spare ammo pool.
        /// </summary>
        /// <param name="amount">The amount of ammo to add.</param>
        void AddAmmo(int amount);

        // --- Events ---

        /// <summary>
        /// Event triggered when the weapon successfully fires.
        /// </summary>
        event Action OnFire;

        /// <summary>
        /// Event triggered when the weapon starts reloading.
        /// </summary>
        event Action OnReloadStart;

        /// <summary>
        /// Event triggered when the weapon finishes reloading.
        /// </summary>
        event Action OnReloadComplete;

        /// <summary>
        /// Event triggered when the weapon's ammo count (in magazine or total spare) changes.
        /// Provides the current magazine ammo and total spare ammo.
        /// </summary>
        event Action<int, int> OnAmmoChanged;

        /// <summary>
        /// Event triggered when the weapon is equipped.
        /// </summary>
        event Action OnEquip;

        /// <summary>
        /// Event triggered when the weapon is unequipped.
        /// </summary>
        event Action OnUnequip;

        /// <summary>
        /// Event triggered when the weapon's fire mode changes.
        /// Provides the new fire mode.
        /// </summary>
        event Action<FireMode> OnFireModeChanged;
    }
}
```

### 2. `VRWeaponBase.cs` (Abstract Base Class)

This abstract class provides a common implementation for the `IVRWeapon` interface, handling shared logic like ammo, cooldowns, and general state management. Concrete weapons will inherit from this.

```csharp
using UnityEngine;
using System;
using System.Collections; // Required for Coroutines

namespace VRWeaponSystem
{
    /// <summary>
    /// VRWeaponBase is an abstract base class that provides common functionality for all VR weapons.
    /// It implements the IVRWeapon interface and handles core weapon logic such as ammo management,
    /// firing cooldowns, reloading, and equipping/unequipping.
    /// Concrete weapon types will inherit from this class and implement weapon-specific behaviors.
    /// </summary>
    public abstract class VRWeaponBase : MonoBehaviour, IVRWeapon
    {
        // --- Editor-Configurable Properties ---
        [Header("Weapon General Settings")]
        [Tooltip("The display name of the weapon.")]
        [SerializeField] protected string _weaponName = "Default Weapon";
        
        [Tooltip("The damage inflicted by a single shot from this weapon.")]
        [SerializeField] protected float _damage = 10f;

        [Tooltip("The rate of fire in rounds per second.")]
        [SerializeField] protected float _fireRate = 5f; // Rounds per second

        [Tooltip("The time it takes to complete a reload in seconds.")]
        [SerializeField] protected float _reloadTime = 2f;

        [Header("Ammo Settings")]
        [Tooltip("The capacity of a single magazine.")]
        [SerializeField] protected int _magazineSize = 10;

        [Tooltip("The initial amount of ammo loaded into the magazine when spawned.")]
        [SerializeField] protected int _initialAmmoInMagazine = 10;
        
        [Tooltip("The initial total spare ammo carried for this weapon.")]
        [SerializeField] protected int _initialSpareAmmo = 30;

        [Tooltip("The maximum total ammo (magazine + spare) the player can carry for this weapon.")]
        [SerializeField] protected int _maxTotalAmmo = 60;

        [Header("Fire Mode Settings")]
        [Tooltip("The default firing mode when the weapon is equipped.")]
        [SerializeField] protected FireMode _defaultFireMode = FireMode.SemiAuto;

        [Tooltip("Allows the weapon to switch between Semi-Auto and Full-Auto modes.")]
        [SerializeField] protected bool _canSwitchFireMode = true;

        // --- Internal State ---
        protected int _currentAmmoInMagazine;
        protected int _totalSpareAmmo;
        protected FireMode _currentFireMode;
        protected float _lastFireTime;
        protected bool _isReloading;
        protected bool _isEquipped;

        // --- IVRWeapon Interface Implementation ---

        public string WeaponName => _weaponName;
        public int CurrentAmmoInMagazine => _currentAmmoInMagazine;
        public int MagazineSize => _magazineSize;
        public int TotalSpareAmmo => _totalSpareAmmo;
        public int MaxTotalAmmo => _maxTotalAmmo;
        public FireMode CurrentFireMode => _currentFireMode;
        public bool IsEquipped => _isEquipped;
        public bool IsReloading => _isReloading;
        
        // Checks if the weapon can fire based on ammo, cooldown, and reloading status.
        public bool CanFire
        {
            get
            {
                bool hasAmmo = _currentAmmoInMagazine > 0;
                bool cooldownReady = Time.time >= _lastFireTime + (1f / _fireRate);
                return hasAmmo && cooldownReady && !_isReloading;
            }
        }

        // --- Events ---
        public event Action OnFire;
        public event Action OnReloadStart;
        public event Action OnReloadComplete;
        public event Action<int, int> OnAmmoChanged; // magazine, spare
        public event Action OnEquip;
        public event Action OnUnequip;
        public event Action<FireMode> OnFireModeChanged;

        // --- Unity Lifecycle Methods ---

        protected virtual void Awake()
        {
            // Initialize ammo counts and fire mode
            _currentAmmoInMagazine = Mathf.Min(_initialAmmoInMagazine, _magazineSize);
            _totalSpareAmmo = Mathf.Min(_initialSpareAmmo, _maxTotalAmmo - _currentAmmoInMagazine);
            _currentFireMode = _defaultFireMode;
            _lastFireTime = -Mathf.Infinity; // Ensure weapon can fire immediately on start
            _isReloading = false;
            _isEquipped = false; // Initially not equipped
            
            // Ensure the weapon GameObject is initially inactive
            gameObject.SetActive(false);
        }

        // --- IVRWeapon Method Implementations ---

        public void Fire()
        {
            if (!IsEquipped)
            {
                Debug.LogWarning($"{WeaponName} is not equipped and cannot fire.");
                return;
            }

            // For full-auto, we check `CanFire` every frame the trigger is held.
            // For semi-auto, `Fire()` should only be called once per trigger press.
            if (CanFire)
            {
                _currentAmmoInMagazine--;
                _lastFireTime = Time.time;

                DoFire(); // Call the abstract method for specific weapon effects
                OnFire?.Invoke();
                OnAmmoChanged?.Invoke(_currentAmmoInMagazine, _totalSpareAmmo);

                Debug.Log($"{WeaponName} Fired! Ammo: {_currentAmmoInMagazine}/{MagazineSize}, Spare: {TotalSpareAmmo}");
            }
            else if (_currentAmmoInMagazine <= 0 && !IsReloading)
            {
                Debug.Log($"{WeaponName} Click! Out of ammo. Reloading...");
                Reload(); // Auto-reload attempt if out of ammo
            }
            else if (IsReloading)
            {
                Debug.Log($"{WeaponName} is reloading...");
            }
        }

        public void Reload()
        {
            if (!IsEquipped)
            {
                Debug.LogWarning($"{WeaponName} is not equipped and cannot reload.");
                return;
            }

            if (_isReloading)
            {
                Debug.Log($"{WeaponName} already reloading.");
                return;
            }

            int neededAmmo = _magazineSize - _currentAmmoInMagazine;
            if (neededAmmo <= 0 || _totalSpareAmmo <= 0)
            {
                Debug.Log($"{WeaponName} no ammo needed or no spare ammo to reload.");
                return;
            }

            StartCoroutine(ReloadCoroutine(neededAmmo));
        }

        private IEnumerator ReloadCoroutine(int neededAmmo)
        {
            _isReloading = true;
            OnReloadStart?.Invoke();
            DoReloadStart(); // Call virtual method for weapon-specific reload start effects

            Debug.Log($"{WeaponName} Reloading...");
            yield return new WaitForSeconds(_reloadTime);

            int ammoToTransfer = Mathf.Min(neededAmmo, _totalSpareAmmo);
            _currentAmmoInMagazine += ammoToTransfer;
            _totalSpareAmmo -= ammoToTransfer;

            _isReloading = false;
            OnReloadComplete?.Invoke();
            DoReloadComplete(); // Call virtual method for weapon-specific reload complete effects
            OnAmmoChanged?.Invoke(_currentAmmoInMagazine, _totalSpareAmmo);

            Debug.Log($"{WeaponName} Reload Complete! Ammo: {_currentAmmoInMagazine}/{MagazineSize}, Spare: {TotalSpareAmmo}");
        }

        public void SwitchFireMode()
        {
            if (!_canSwitchFireMode)
            {
                Debug.Log($"{WeaponName} cannot switch fire mode.");
                return;
            }

            _currentFireMode = (_currentFireMode == FireMode.SemiAuto) ? FireMode.FullAuto : FireMode.SemiAuto;
            OnFireModeChanged?.Invoke(_currentFireMode);
            Debug.Log($"{WeaponName} Fire Mode Switched to: {_currentFireMode}");
        }

        public void Equip()
        {
            if (_isEquipped) return;

            _isEquipped = true;
            gameObject.SetActive(true); // Make weapon visible/active
            OnEquip?.Invoke();
            DoEquip(); // Call virtual method for weapon-specific equip effects
            OnAmmoChanged?.Invoke(_currentAmmoInMagazine, _totalSpareAmmo); // Update UI
            OnFireModeChanged?.Invoke(_currentFireMode); // Update UI
            Debug.Log($"{WeaponName} Equipped!");
        }

        public void Unequip()
        {
            if (!_isEquipped) return;

            _isEquipped = false;
            gameObject.SetActive(false); // Make weapon invisible/inactive
            OnUnequip?.Invoke();
            DoUnequip(); // Call virtual method for weapon-specific unequip effects
            Debug.Log($"{WeaponName} Unequipped.");
        }
        
        public void AddAmmo(int amount)
        {
            if (amount < 0) return;

            int oldTotalSpare = _totalSpareAmmo;
            _totalSpareAmmo = Mathf.Min(_totalSpareAmmo + amount, _maxTotalAmmo - _currentAmmoInMagazine);

            if (_totalSpareAmmo != oldTotalSpare)
            {
                OnAmmoChanged?.Invoke(_currentAmmoInMagazine, _totalSpareAmmo);
                Debug.Log($"{WeaponName} picked up {amount} ammo. Total spare: {_totalSpareAmmo}");
            }
        }

        // --- Abstract/Virtual Methods for Concrete Implementations ---

        /// <summary>
        /// This abstract method must be implemented by concrete weapon classes
        /// to define their specific firing effects (e.g., spawning a projectile, raycasting, sound).
        /// </summary>
        protected abstract void DoFire();

        /// <summary>
        /// Virtual method for weapon-specific effects when reloading starts.
        /// (e.g., playing a reload sound, animation trigger).
        /// </summary>
        protected virtual void DoReloadStart() { }

        /// <summary>
        /// Virtual method for weapon-specific effects when reloading completes.
        /// (e.g., playing a bolt-rack sound, animation trigger).
        /// </summary>
        protected virtual void DoReloadComplete() { }

        /// <summary>
        /// Virtual method for weapon-specific effects when equipped.
        /// </summary>
        protected virtual void DoEquip() { }

        /// <summary>
        /// Virtual method for weapon-specific effects when unequipped.
        /// </summary>
        protected virtual void DoUnequip() { }
    }
}
```

### 3. `PistolVRWeapon.cs` (Concrete Weapon Example)

A simple pistol implementation. It fires in semi-auto mode only.

```csharp
using UnityEngine;

namespace VRWeaponSystem
{
    /// <summary>
    /// A concrete implementation of VRWeaponBase for a pistol.
    /// Demonstrates how to specialize weapon behavior and properties.
    /// </summary>
    public class PistolVRWeapon : VRWeaponBase
    {
        [Header("Pistol Specific Settings")]
        [Tooltip("Prefab for the bullet projectile fired by the pistol.")]
        [SerializeField] private GameObject _bulletPrefab;
        [Tooltip("The transform where bullets will be spawned (muzzle).")]
        [SerializeField] private Transform _muzzlePoint;
        [Tooltip("The force with which the bullet is fired.")]
        [SerializeField] private float _bulletForce = 50f;

        protected override void Awake()
        {
            // Set pistol-specific defaults before calling base Awake
            _weaponName = "Pistol";
            _magazineSize = 12;
            _initialAmmoInMagazine = 12;
            _initialSpareAmmo = 36;
            _maxTotalAmmo = 60;
            _fireRate = 4f; // 4 shots per second
            _reloadTime = 1.5f;
            _damage = 15f;
            _defaultFireMode = FireMode.SemiAuto;
            _canSwitchFireMode = false; // Pistols often don't switch fire mode

            base.Awake(); // Call base Awake to apply these settings and initialize common state
        }

        /// <summary>
        /// Pistol-specific firing logic: Instantiate a projectile.
        /// </summary>
        protected override void DoFire()
        {
            Debug.Log($"<color=cyan>{WeaponName}</color> fires a projectile!");
            if (_bulletPrefab != null && _muzzlePoint != null)
            {
                // Instantiate a simple visual projectile for demonstration
                GameObject bullet = Instantiate(_bulletPrefab, _muzzlePoint.position, _muzzlePoint.rotation);
                if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.AddForce(_muzzlePoint.forward * _bulletForce, ForceMode.Impulse);
                }
                // Destroy bullet after some time
                Destroy(bullet, 5f); 
            }
            // Add sound effect, recoil animation, muzzle flash etc. here
        }

        protected override void DoReloadStart()
        {
            Debug.Log($"<color=cyan>{WeaponName}</color> plays reload start animation/sound.");
        }

        protected override void DoReloadComplete()
        {
            Debug.Log($"<color=cyan>{WeaponName}</color> plays reload complete animation/sound.");
        }
    }
}
```

### 4. `AssaultRifleVRWeapon.cs` (Concrete Weapon Example)

An assault rifle implementation. It can switch between semi-auto and full-auto. It uses a raycast for firing.

```csharp
using UnityEngine;

namespace VRWeaponSystem
{
    /// <summary>
    /// A concrete implementation of VRWeaponBase for an assault rifle.
    /// Demonstrates raycast firing and fire mode switching.
    /// </summary>
    public class AssaultRifleVRWeapon : VRWeaponBase
    {
        [Header("Assault Rifle Specific Settings")]
        [Tooltip("The range of the rifle's raycast.")]
        [SerializeField] private float _range = 100f;
        [Tooltip("LayerMask to filter what the rifle's raycast can hit.")]
        [SerializeField] private LayerMask _hitMask;

        protected override void Awake()
        {
            // Set assault rifle-specific defaults before calling base Awake
            _weaponName = "Assault Rifle";
            _magazineSize = 30;
            _initialAmmoInMagazine = 30;
            _initialSpareAmmo = 90;
            _maxTotalAmmo = 120;
            _fireRate = 10f; // 10 shots per second (higher than pistol)
            _reloadTime = 3f;
            _damage = 20f;
            _defaultFireMode = FireMode.FullAuto;
            _canSwitchFireMode = true; // Assault rifles typically can switch

            base.Awake(); // Call base Awake to apply these settings and initialize common state
        }

        /// <summary>
        /// Assault Rifle-specific firing logic: Perform a raycast.
        /// </summary>
        protected override void DoFire()
        {
            Debug.Log($"<color=orange>{WeaponName}</color> fires a raycast!");
            
            // For demonstration, we'll raycast from the weapon's forward direction
            // In a real VR game, this would typically be from the camera/player's view or a specific aim point.
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, _range, _hitMask))
            {
                Debug.Log($"<color=red>Hit: {hit.collider.name} at {hit.point} for {_damage} damage!</color>");
                // Example: Apply damage to a health component on the hit object
                // if (hit.collider.TryGetComponent<HealthComponent>(out HealthComponent hc))
                // {
                //     hc.TakeDamage(_damage);
                // }

                // Draw a debug line for visualization (only visible in Scene view during play)
                Debug.DrawLine(transform.position, hit.point, Color.red, 0.1f);
            }
            else
            {
                Debug.DrawLine(transform.position, transform.position + transform.forward * _range, Color.yellow, 0.1f);
            }

            // Add sound effect, recoil animation, muzzle flash, shell casing ejection etc. here
        }

        protected override void DoReloadStart()
        {
            Debug.Log($"<color=orange>{WeaponName}</color> plays reload start animation/sound.");
        }

        protected override void DoReloadComplete()
        {
            Debug.Log($"<color=orange>{WeaponName}</color> plays reload complete animation/sound.");
        }
    }
}
```

### 5. `VRWeaponManager.cs` (The Core Manager)

This is the central component that manages the collection of weapons, handles input (simulated via keyboard), and switches between equipped weapons.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for LINQ extensions like .FirstOrDefault()

namespace VRWeaponSystem
{
    /// <summary>
    /// VRWeaponManager is the central component that manages all weapons for the player.
    /// It holds a collection of available weapons, manages the currently equipped weapon,
    /// handles weapon switching, and translates player input (simulated here with keyboard)
    /// into weapon actions.
    /// This adheres to the pattern by separating weapon management from individual weapon logic.
    /// </summary>
    public class VRWeaponManager : MonoBehaviour
    {
        [Header("Weapon Management")]
        [Tooltip("Drag all VRWeaponBase components (e.g., PistolVRWeapon, AssaultRifleVRWeapon) here.")]
        [SerializeField] private List<VRWeaponBase> _availableWeapons = new List<VRWeaponBase>();

        private int _currentWeaponIndex = -1;
        public IVRWeapon CurrentWeapon { get; private set; }

        [Header("UI References (Optional)")]
        [Tooltip("Text element to display current weapon name.")]
        [SerializeField] private TMPro.TextMeshProUGUI _weaponNameText;
        [Tooltip("Text element to display current ammo.")]
        [SerializeField] private TMPro.TextMeshProUGUI _ammoText;
        [Tooltip("Text element to display current fire mode.")]
        [SerializeField] private TMPro.TextMeshProUGUI _fireModeText;

        // --- Unity Lifecycle Methods ---

        private void Awake()
        {
            // Ensure all weapons in the list are properly initialized and linked
            foreach (var weapon in _availableWeapons)
            {
                if (weapon == null) continue;
                // Subscribe to weapon events to update UI or handle global events
                weapon.OnAmmoChanged += UpdateWeaponUI;
                weapon.OnFireModeChanged += UpdateWeaponUI;
                weapon.OnEquip += UpdateWeaponUI; // Update UI when a new weapon is equipped
            }

            // Equip the first weapon on start, if any exist
            if (_availableWeapons.Count > 0)
            {
                EquipWeapon(0);
            }
            else
            {
                Debug.LogWarning("VRWeaponManager has no weapons assigned in the list!");
                UpdateWeaponUI(); // Clear UI if no weapons
            }
        }

        private void Update()
        {
            // --- Simulated VR Input (Using Keyboard for demonstration) ---

            // Fire input
            if (Input.GetMouseButtonDown(0)) // Left mouse click for semi-auto/first shot of full-auto
            {
                HandleTriggerPressed();
            }
            if (Input.GetMouseButton(0)) // Left mouse held for full-auto
            {
                HandleTriggerHeld();
            }

            // Reload input
            if (Input.GetKeyDown(KeyCode.R))
            {
                HandleReloadButtonPressed();
            }

            // Fire Mode switch input
            if (Input.GetKeyDown(KeyCode.F))
            {
                HandleFireModeButtonPressed();
            }

            // Weapon switching inputs
            if (Input.GetKeyDown(KeyCode.Alpha1)) // Equip first weapon
            {
                EquipWeapon(0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) // Equip second weapon
            {
                EquipWeapon(1);
            }
            if (Input.GetKeyDown(KeyCode.Q)) // Previous weapon
            {
                SwitchToPreviousWeapon();
            }
            if (Input.GetKeyDown(KeyCode.E)) // Next weapon
            {
                SwitchToNextWeapon();
            }
            
            // Simulate picking up ammo
            if (Input.GetKeyDown(KeyCode.P))
            {
                HandlePickupAmmo();
            }
        }

        // --- Public Methods for VR Input Integration ---
        // These methods would typically be called by your specific VR input system (e.g., XR Interaction Toolkit, Oculus SDK).
        // They provide a clean API for external systems to interact with the weapon manager.

        /// <summary>
        /// Call this when the VR trigger button is initially pressed.
        /// Handles semi-auto fire or the first shot of full-auto.
        /// </summary>
        public void HandleTriggerPressed()
        {
            if (CurrentWeapon == null) return;

            // In semi-auto, fire once per press. In full-auto, this can also initiate firing.
            // The VRWeaponBase's CanFire check handles cooldown for both.
            if (CurrentWeapon.CurrentFireMode == FireMode.SemiAuto || CurrentWeapon.CanFire)
            {
                CurrentWeapon.Fire();
            }
        }

        /// <summary>
        /// Call this continuously while the VR trigger button is held down.
        /// Handles full-auto fire.
        /// </summary>
        public void HandleTriggerHeld()
        {
            if (CurrentWeapon == null) return;

            // Only fire continuously if in full-auto mode and able to fire.
            if (CurrentWeapon.CurrentFireMode == FireMode.FullAuto)
            {
                CurrentWeapon.Fire(); // VRWeaponBase's CanFire check will limit fire rate
            }
        }

        /// <summary>
        /// Call this when the VR reload button is pressed.
        /// </summary>
        public void HandleReloadButtonPressed()
        {
            if (CurrentWeapon == null) return;
            CurrentWeapon.Reload();
        }

        /// <summary>
        /// Call this when the VR fire mode switch button is pressed.
        /// </summary>
        public void HandleFireModeButtonPressed()
        {
            if (CurrentWeapon == null) return;
            CurrentWeapon.SwitchFireMode();
        }
        
        /// <summary>
        /// Call this when the player picks up an ammo crate or similar.
        /// </summary>
        public void HandlePickupAmmo()
        {
            if (CurrentWeapon == null) return;
            // For demonstration, adds 30 ammo to the current weapon
            CurrentWeapon.AddAmmo(30); 
        }

        // --- Weapon Switching Logic ---

        /// <summary>
        /// Equips the weapon at the specified index in the available weapons list.
        /// </summary>
        /// <param name="index">The index of the weapon to equip.</param>
        public void EquipWeapon(int index)
        {
            if (index < 0 || index >= _availableWeapons.Count)
            {
                Debug.LogWarning($"Attempted to equip weapon at invalid index: {index}");
                return;
            }

            // Unequip the current weapon if one is equipped
            if (CurrentWeapon != null)
            {
                CurrentWeapon.Unequip();
            }

            // Equip the new weapon
            _currentWeaponIndex = index;
            CurrentWeapon = _availableWeapons[_currentWeaponIndex];
            CurrentWeapon.Equip();
            
            Debug.Log($"Equipped: {CurrentWeapon.WeaponName}");
            UpdateWeaponUI(); // Ensure UI is updated immediately on equip
        }

        /// <summary>
        /// Switches to the next weapon in the list (loops around).
        /// </summary>
        public void SwitchToNextWeapon()
        {
            if (_availableWeapons.Count <= 1) return; // No other weapon to switch to

            int nextIndex = (_currentWeaponIndex + 1) % _availableWeapons.Count;
            EquipWeapon(nextIndex);
        }

        /// <summary>
        /// Switches to the previous weapon in the list (loops around).
        /// </summary>
        public void SwitchToPreviousWeapon()
        {
            if (_availableWeapons.Count <= 1) return; // No other weapon to switch to

            int prevIndex = (_currentWeaponIndex - 1 + _availableWeapons.Count) % _availableWeapons.Count;
            EquipWeapon(prevIndex);
        }

        // --- UI Update ---

        /// <summary>
        /// Updates the UI elements to reflect the current weapon's state.
        /// Called when ammo changes, fire mode changes, or a new weapon is equipped.
        /// </summary>
        private void UpdateWeaponUI(int currentAmmo = 0, int totalSpare = 0)
        {
            if (CurrentWeapon == null)
            {
                if (_weaponNameText != null) _weaponNameText.text = "No Weapon";
                if (_ammoText != null) _ammoText.text = "Ammo: -/- (-)";
                if (_fireModeText != null) _fireModeText.text = "Mode: -";
                return;
            }

            // Use CurrentWeapon properties for UI updates
            if (_weaponNameText != null) _weaponNameText.text = $"Weapon: {CurrentWeapon.WeaponName}";
            if (_ammoText != null) _ammoText.text = $"Ammo: {CurrentWeapon.CurrentAmmoInMagazine}/{CurrentWeapon.MagazineSize} ({CurrentWeapon.TotalSpareAmmo})";
            if (_fireModeText != null) _fireModeText.text = $"Mode: {CurrentWeapon.CurrentFireMode}";
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks when the manager is destroyed
            foreach (var weapon in _availableWeapons)
            {
                if (weapon == null) continue;
                weapon.OnAmmoChanged -= UpdateWeaponUI;
                weapon.OnFireModeChanged -= UpdateWeaponUI;
                weapon.OnEquip -= UpdateWeaponUI;
            }
        }
    }
}
```

### 6. `BulletProjectile.cs` (Simple Projectile for Pistol)

A very basic script to make the pistol's spawned object move.

```csharp
using UnityEngine;

namespace VRWeaponSystem
{
    /// <summary>
    /// Simple script for a bullet projectile.
    /// Ensures it has a Rigidbody and is destroyed after a timeout.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BulletProjectile : MonoBehaviour
    {
        [SerializeField] private float _lifeTime = 5f; // How long the bullet exists

        private void Awake()
        {
            Destroy(gameObject, _lifeTime);
        }

        // Optional: Implement OnTriggerEnter/OnCollisionEnter for impact effects/damage
        // private void OnTriggerEnter(Collider other)
        // {
        //     Debug.Log($"Bullet hit {other.name}");
        //     // Example: Destroy on impact
        //     Destroy(gameObject); 
        // }
    }
}
```

---

## Unity Project Setup Instructions:

To get this example working in a Unity project:

1.  **Create Folders:**
    *   Create a new folder named `VRWeaponSystem` in your `Assets` directory.
    *   Inside `VRWeaponSystem`, create a subfolder named `Scripts`.

2.  **Add C# Scripts:**
    *   Copy and paste each of the C# script contents (`IVRWeapon.cs`, `VRWeaponBase.cs`, `PistolVRWeapon.cs`, `AssaultRifleVRWeapon.cs`, `VRWeaponManager.cs`, `BulletProjectile.cs`) into new C# script files with the corresponding names within the `Assets/VRWeaponSystem/Scripts` folder. Ensure the namespace `VRWeaponSystem` matches.

3.  **Create 3D Objects (Optional but Recommended for Visuals):**
    *   **Bullet Prefab:**
        *   Right-click in the Hierarchy -> `3D Object` -> `Sphere`.
        *   Rename it to `BulletProjectile`.
        *   Set its `Scale` to `0.1`, `0.1`, `0.1`.
        *   Remove the `Sphere Collider` (or make it a trigger).
        *   Add a `Rigidbody` component.
        *   Add the `BulletProjectile.cs` script to it.
        *   Drag this `BulletProjectile` GameObject from the Hierarchy into your `Assets` folder to create a Prefab. Delete it from the Hierarchy afterwards.

4.  **Scene Setup:**

    *   **VRWeaponManager GameObject:**
        *   Create an empty GameObject in your scene (Right-click in Hierarchy -> `Create Empty`).
        *   Rename it to `VRWeaponManager`.
        *   Drag and drop the `VRWeaponManager.cs` script onto this new GameObject in the Inspector.

    *   **Weapon GameObjects:**
        *   Create two empty GameObjects as children of the `VRWeaponManager` GameObject:
            *   Right-click on `VRWeaponManager` in Hierarchy -> `Create Empty`.
            *   Rename the first child to `Pistol`.
            *   Drag `PistolVRWeapon.cs` onto the `Pistol` GameObject in the Inspector.
            *   **Assign Bullet Prefab:** In the `PistolVRWeapon` component, drag your `BulletProjectile` Prefab into the `Bullet Prefab` slot.
            *   **Create Muzzle Point:** Create another empty GameObject as a child of `Pistol` named `MuzzlePoint`. Position it slightly forward of where the pistol's barrel would be. Drag this `MuzzlePoint` GameObject into the `Muzzle Point` slot on the `PistolVRWeapon` component.
            *   Rename the second child to `AssaultRifle`.
            *   Drag `AssaultRifleVRWeapon.cs` onto the `AssaultRifle` GameObject in the Inspector.
            *   Set the `Hit Mask` for the Assault Rifle to `Default` (or any layer your objects are on).

    *   **Assign Weapons to Manager:**
        *   Select the `VRWeaponManager` GameObject.
        *   In its `VR Weapon Manager` component, find the `Available Weapons` list.
        *   Set its `Size` to `2`.
        *   Drag the `Pistol` GameObject (from the Hierarchy, which is a child of `VRWeaponManager`) into `Element 0`.
        *   Drag the `AssaultRifle` GameObject into `Element 1`.

    *   **UI Setup (Optional but recommended for feedback):**
        *   Create a UI Canvas: Right-click in Hierarchy -> `UI` -> `Canvas`.
        *   Set the `Canvas Render Mode` to `Screen Space - Overlay`.
        *   Add three UI Text (TextMeshPro) elements as children of the Canvas:
            *   Right-click Canvas -> `UI` -> `TextMeshPro - Text`. (If prompted, import TMP Essentials).
            *   Rename them `WeaponNameText`, `AmmoText`, `FireModeText`.
            *   Position them on the screen (e.g., top-left corner) and adjust font size for visibility.
        *   Drag these `TextMeshProUGUI` components into the corresponding slots (`Weapon Name Text`, `Ammo Text`, `Fire Mode Text`) on the `VRWeaponManager` component.

5.  **Test the Scene:**
    *   Press Play in the Unity Editor.
    *   Observe the Debug.Log output in the Console and (if set up) the UI.
    *   **Keyboard Controls:**
        *   `Left Mouse Click`: Fire (Semi-auto) or initial Full-auto shot.
        *   `Left Mouse Hold`: Continue firing (Full-auto).
        *   `R`: Reload current weapon.
        *   `F`: Switch fire mode (Assault Rifle only).
        *   `1`: Equip Pistol.
        *   `2`: Equip Assault Rifle.
        *   `Q`: Switch to previous weapon.
        *   `E`: Switch to next weapon.
        *   `P`: Add spare ammo to current weapon.

You now have a fully functional `VRWeaponSystem` demonstration in your Unity project!