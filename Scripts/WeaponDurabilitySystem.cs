// Unity Design Pattern Example: WeaponDurabilitySystem
// This script demonstrates the WeaponDurabilitySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Weapon Durability System pattern** in Unity C#. While not a traditional Gang of Four design pattern, it represents a common, robust architectural approach for managing an item's durability in games.

**Core Idea of the "Weapon Durability System Pattern":**
The pattern focuses on:
1.  **Encapsulation:** All durability-related logic (current value, max value, damage, repair, break status) is contained within a dedicated component or class.
2.  **Modularity/Component-Based:** The durability logic is a standalone component (`WeaponDurabilityHandler`) that can be attached to any game object (weapons, armor, shields, tools) that needs durability.
3.  **Observability (Events):** The system provides events (`OnDurabilityChanged`, `OnWeaponBroken`) that other parts of the game (UI, sound, VFX, game logic) can subscribe to, reacting to changes without direct coupling to the durability component's internal state.
4.  **Interface-Based Interaction:** An interface (`IWeaponDurability`) defines the public contract for interacting with durability, promoting loose coupling and making it easier to swap implementations or mock for testing.

---

### Unity Setup Instructions:

1.  **Create a C# Script:** Create a new C# script in your Unity project, name it `WeaponDurabilitySystemExample`, and copy the entire code below into it.
2.  **Create UI Elements:**
    *   Right-click in the Hierarchy -> UI -> Canvas.
    *   Inside the Canvas, Right-click -> UI -> Text - TextMeshPro (requires importing TMP Essentials if prompted). Rename it `DurabilityText`.
    *   Inside the Canvas, Right-click -> UI -> Slider. Rename it `DurabilitySlider`.
    *   Adjust positions and sizes of Text and Slider for visibility.
3.  **Create a Weapon GameObject:**
    *   Create an empty GameObject (Right-click in Hierarchy -> Create Empty). Name it `Sword`.
    *   Add the `WeaponDurabilityHandler` component to `Sword`.
        *   Set `Max Durability` to `100`.
        *   Set `Initial Durability` to `100`.
    *   Add the `SwordWeapon` component to `Sword`.
        *   Set `Damage Per Attack` to `15`.
        *   Set `Durability Loss Per Attack` to `10`.
4.  **Create a Player GameObject:**
    *   Create an empty GameObject. Name it `Player`.
    *   Add the `PlayerWeaponHandler` component to `Player`.
        *   Drag the `Sword` GameObject from the Hierarchy into the `Current Weapon` slot of the `PlayerWeaponHandler` component.
5.  **Create a UI Manager GameObject:**
    *   Create an empty GameObject. Name it `UIManager`.
    *   Add the `WeaponDurabilityUI` component to `UIManager`.
        *   Drag your `DurabilityText` (TextMeshPro) into the `Durability Text` slot.
        *   Drag your `DurabilitySlider` into the `Durability Slider` slot.
        *   Drag the `Sword` GameObject (specifically, its `WeaponDurabilityHandler` component) into the `Target Durability Handler` slot of the `WeaponDurabilityUI` component.
6.  **Run the Scene:**
    *   Press Play.
    *   **Left-click (Mouse0):** The player will attack with the sword, reducing its durability.
    *   **Press 'R':** The player will repair the sword, increasing its durability.
    *   Observe the UI Text and Slider updating in real-time.
    *   Once durability hits 0, the weapon breaks and can no longer be used until repaired.
    *   Check the console for messages when attacking, repairing, and when the weapon breaks.

---

```csharp
using UnityEngine;
using System;
using TMPro; // Required for TextMeshPro UI elements
using UnityEngine.UI; // Required for Slider UI elements

namespace GameSystems.Durability
{
    // =====================================================================================
    // 1. IWeaponDurability Interface
    //    Defines the contract for any object that has durability.
    //    This promotes loose coupling and makes the system flexible and testable.
    // =====================================================================================
    public interface IWeaponDurability
    {
        float CurrentDurability { get; }
        float MaxDurability { get; }
        bool IsBroken { get; }

        /// <summary>
        /// Event fired when durability changes.
        /// Parameters: (currentDurability, maxDurability)
        /// </summary>
        event Action<float, float> OnDurabilityChanged;

        /// <summary>
        /// Event fired when the item's durability reaches 0.
        /// </summary>
        event Action OnWeaponBroken;

        /// <summary>
        /// Reduces the item's durability.
        /// </summary>
        /// <param name="amount">The amount of durability to lose.</param>
        void Damage(float amount);

        /// <summary>
        /// Increases the item's durability.
        /// </summary>
        /// <param name="amount">The amount of durability to gain.</param>
        void Repair(float amount);

        /// <summary>
        /// Immediately sets durability to 0, marking the item as broken.
        /// </summary>
        void Break();

        /// <summary>
        /// Restores durability to its maximum value.
        /// </summary>
        void RestoreMaxDurability();
    }

    // =====================================================================================
    // 2. WeaponDurabilityHandler Component
    //    The core component that manages durability state and logic.
    //    It can be attached to any GameObject needing durability.
    //    This embodies the "Component Pattern" aspect of the system.
    // =====================================================================================
    [DisallowMultipleComponent] // Only one durability handler per GameObject
    public class WeaponDurabilityHandler : MonoBehaviour, IWeaponDurability
    {
        [Header("Durability Settings")]
        [Tooltip("The maximum durability this item can have.")]
        [SerializeField] private float _maxDurability = 100f;
        [Tooltip("The initial durability when the item is created/initialized.")]
        [SerializeField] private float _initialDurability = 100f;

        private float _currentDurability;
        private bool _isBroken = false;

        // Public properties implementing IWeaponDurability
        public float CurrentDurability => _currentDurability;
        public float MaxDurability => _maxDurability;
        public bool IsBroken => _isBroken;

        // Events implementing IWeaponDurability
        public event Action<float, float> OnDurabilityChanged;
        public event Action OnWeaponBroken;

        private void Awake()
        {
            // Ensure initial durability doesn't exceed max.
            _currentDurability = Mathf.Min(_initialDurability, _maxDurability);
            CheckBrokenStatus();
        }

        private void Start()
        {
            // Fire initial event to update any subscribers (e.g., UI)
            OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
        }

        /// <summary>
        /// Reduces the item's durability.
        /// </summary>
        /// <param name="amount">The amount of durability to lose.</param>
        public void Damage(float amount)
        {
            if (IsBroken)
            {
                Debug.LogWarning($"{gameObject.name} is already broken and cannot take further damage.");
                return;
            }

            if (amount < 0) return; // Prevent negative damage

            _currentDurability -= amount;
            _currentDurability = Mathf.Max(0, _currentDurability); // Clamp to minimum 0

            CheckBrokenStatus();
            OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
        }

        /// <summary>
        /// Increases the item's durability.
        /// </summary>
        /// <param name="amount">The amount of durability to gain.</param>
        public void Repair(float amount)
        {
            if (amount < 0) return; // Prevent negative repair

            _currentDurability += amount;
            _currentDurability = Mathf.Min(_maxDurability, _currentDurability); // Clamp to maximum durability

            CheckBrokenStatus(); // Repairing might un-break the item
            OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
        }

        /// <summary>
        /// Immediately sets durability to 0, marking the item as broken.
        /// </summary>
        public void Break()
        {
            if (_isBroken) return; // Already broken

            _currentDurability = 0;
            CheckBrokenStatus(); // This will set _isBroken to true and fire event
            OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
        }

        /// <summary>
        /// Restores durability to its maximum value.
        /// </summary>
        public void RestoreMaxDurability()
        {
            _currentDurability = _maxDurability;
            CheckBrokenStatus(); // This will un-break the item if it was broken
            OnDurabilityChanged?.Invoke(_currentDurability, _maxDurability);
        }

        /// <summary>
        /// Internal method to update the broken status and fire the OnWeaponBroken event.
        /// </summary>
        private void CheckBrokenStatus()
        {
            bool wasBroken = _isBroken;
            _isBroken = _currentDurability <= 0;

            if (_isBroken && !wasBroken)
            {
                Debug.Log($"{gameObject.name} has broken!");
                OnWeaponBroken?.Invoke();
            }
            else if (!_isBroken && wasBroken)
            {
                Debug.Log($"{gameObject.name} has been repaired and is no longer broken!");
            }
        }

        // Example: Gizmos for visualizing durability in editor (optional)
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return; // Only show in play mode for dynamic updates

            Gizmos.color = IsBroken ? Color.red : Color.green;
            Vector3 center = transform.position + Vector3.up * 0.5f;
            float fillAmount = _currentDurability / _maxDurability;
            Gizmos.DrawWireCube(center, new Vector3(1, 0.2f, 0.2f));
            Gizmos.DrawCube(center + Vector3.left * (0.5f - 0.5f * fillAmount), new Vector3(fillAmount, 0.2f, 0.2f));
        }
    }

    // =====================================================================================
    // 3. Weapon Base Class
    //    An abstract base class for all weapons, integrating the durability handler.
    //    It requires a WeaponDurabilityHandler to be present on the same GameObject.
    // =====================================================================================
    [RequireComponent(typeof(WeaponDurabilityHandler))]
    public abstract class Weapon : MonoBehaviour
    {
        // Protected reference to the durability handler, making it accessible to derived classes.
        // Marked [SerializeField] so it can be assigned in Inspector, or found at runtime.
        [SerializeField] protected WeaponDurabilityHandler durabilityHandler;

        // Public property to expose the durability interface for external interaction.
        public IWeaponDurability Durability => durabilityHandler;

        protected virtual void Awake()
        {
            // If durabilityHandler isn't set in the Inspector, find it on this GameObject.
            if (durabilityHandler == null)
            {
                durabilityHandler = GetComponent<WeaponDurabilityHandler>();
            }
            if (durabilityHandler == null)
            {
                Debug.LogError($"Weapon '{gameObject.name}' requires a WeaponDurabilityHandler component!", this);
                enabled = false; // Disable script if essential component is missing
            }
        }

        /// <summary>
        /// Abstract method for weapon's primary attack action.
        /// Concrete weapon types will implement this.
        /// </summary>
        public abstract void Attack();

        /// <summary>
        /// Optional virtual method called when weapon is equipped.
        /// </summary>
        public virtual void OnEquip()
        {
            Debug.Log($"{gameObject.name} equipped.");
        }

        /// <summary>
        /// Optional virtual method called when weapon is unequipped.
        /// </summary>
        public virtual void OnUnequip()
        {
            Debug.Log($"{gameObject.name} unequipped.");
        }
    }

    // =====================================================================================
    // 4. Concrete Weapon Example: SwordWeapon
    //    Demonstrates a specific weapon type that uses the durability system.
    // =====================================================================================
    public class SwordWeapon : Weapon
    {
        [Header("Sword Specific Settings")]
        [Tooltip("The amount of damage this sword deals to an enemy.")]
        [SerializeField] private float _damagePerAttack = 15f;
        [Tooltip("The amount of durability lost per attack.")]
        [SerializeField] private float _durabilityLossPerAttack = 10f;

        public override void Attack()
        {
            // Check if the weapon is broken before attempting to attack.
            if (Durability.IsBroken)
            {
                Debug.Log($"{gameObject.name} is broken and cannot be used!");
                return;
            }

            Debug.Log($"{gameObject.name} attacks! Deals {_damagePerAttack} damage.");
            // Simulate dealing damage to an enemy here (e.g., enemy.TakeDamage(_damagePerAttack);)

            // Reduce weapon durability after use.
            Durability.Damage(_durabilityLossPerAttack);
        }
    }

    // =====================================================================================
    // 5. PlayerWeaponHandler
    //    A component representing a player that can equip and interact with weapons.
    // =====================================================================================
    public class PlayerWeaponHandler : MonoBehaviour
    {
        [Header("Player Weapon Control")]
        [Tooltip("The weapon currently equipped by the player.")]
        [SerializeField] private Weapon _currentWeapon;
        [Tooltip("Key to trigger weapon attack.")]
        [SerializeField] private KeyCode _attackKey = KeyCode.Mouse0; // Left mouse button
        [Tooltip("Key to trigger weapon repair.")]
        [SerializeField] private KeyCode _repairKey = KeyCode.R;
        [Tooltip("Amount of durability to restore per repair action.")]
        [SerializeField] private float _repairAmount = 20f;

        private void Start()
        {
            if (_currentWeapon != null)
            {
                _currentWeapon.OnEquip();
            }
            else
            {
                Debug.LogWarning("Player has no weapon equipped at start.", this);
            }
        }

        private void Update()
        {
            if (_currentWeapon == null) return;

            // Handle attack input
            if (Input.GetKeyDown(_attackKey))
            {
                _currentWeapon.Attack();
            }

            // Handle repair input
            if (Input.GetKeyDown(_repairKey))
            {
                _currentWeapon.Durability.Repair(_repairAmount);
                Debug.Log($"Repairing {_currentWeapon.name} by {_repairAmount} durability.");
            }
        }

        /// <summary>
        /// Equips a new weapon, handling unequip/equip events.
        /// </summary>
        /// <param name="newWeapon">The weapon to equip.</param>
        public void EquipWeapon(Weapon newWeapon)
        {
            if (_currentWeapon != null)
            {
                _currentWeapon.OnUnequip();
            }

            _currentWeapon = newWeapon;

            if (_currentWeapon != null)
            {
                _currentWeapon.OnEquip();
            }
        }
    }

    // =====================================================================================
    // 6. WeaponDurabilityUI Component
    //    A UI component that subscribes to durability changes and updates the UI.
    //    This demonstrates the "Observer Pattern" aspect.
    // =====================================================================================
    public class WeaponDurabilityUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("TextMeshProUGUI element to display durability text.")]
        [SerializeField] private TextMeshProUGUI _durabilityText;
        [Tooltip("Slider UI element to display durability progress.")]
        [SerializeField] private Slider _durabilitySlider;

        [Header("Target Durability")]
        [Tooltip("The WeaponDurabilityHandler this UI should observe.")]
        [SerializeField] private WeaponDurabilityHandler _targetDurabilityHandler;

        private void OnEnable()
        {
            // Subscribe to durability change event when this script is enabled.
            if (_targetDurabilityHandler != null)
            {
                _targetDurabilityHandler.OnDurabilityChanged += UpdateDurabilityUI;
                _targetDurabilityHandler.OnWeaponBroken += OnTargetWeaponBroken;
                // Initial update in case UI activates after handler
                UpdateDurabilityUI(_targetDurabilityHandler.CurrentDurability, _targetDurabilityHandler.MaxDurability);
            }
            else
            {
                Debug.LogWarning("WeaponDurabilityUI: Target Durability Handler is not assigned.", this);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from durability change event to prevent memory leaks
            // when this script is disabled or destroyed.
            if (_targetDurabilityHandler != null)
            {
                _targetDurabilityHandler.OnDurabilityChanged -= UpdateDurabilityUI;
                _targetDurabilityHandler.OnWeaponBroken -= OnTargetWeaponBroken;
            }
        }

        /// <summary>
        /// Callback method for the OnDurabilityChanged event.
        /// Updates the TextMeshPro text and Slider value.
        /// </summary>
        /// <param name="currentDurability">The new current durability value.</param>
        /// <param name="maxDurability">The maximum durability value.</param>
        private void UpdateDurabilityUI(float currentDurability, float maxDurability)
        {
            if (_durabilityText != null)
            {
                _durabilityText.text = $"Durability: {Mathf.CeilToInt(currentDurability)} / {Mathf.CeilToInt(maxDurability)}";
                if (currentDurability <= 0)
                {
                    _durabilityText.color = Color.red;
                    _durabilityText.text += " (BROKEN!)";
                }
                else if (currentDurability < maxDurability * 0.25f)
                {
                    _durabilityText.color = Color.yellow; // Low durability warning
                }
                else
                {
                    _durabilityText.color = Color.white;
                }
            }

            if (_durabilitySlider != null)
            {
                _durabilitySlider.maxValue = maxDurability;
                _durabilitySlider.value = currentDurability;
            }
        }

        /// <summary>
        /// Callback method for the OnWeaponBroken event.
        /// </summary>
        private void OnTargetWeaponBroken()
        {
            Debug.Log($"UI Notified: {_targetDurabilityHandler.gameObject.name} is broken!");
            // Potentially play a sound, show a special broken icon, etc.
        }
    }
}
```