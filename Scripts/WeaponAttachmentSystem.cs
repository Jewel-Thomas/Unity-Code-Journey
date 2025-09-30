// Unity Design Pattern Example: WeaponAttachmentSystem
// This script demonstrates the WeaponAttachmentSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Weapon Attachment System design pattern, often implemented using the **Decorator Pattern**, allows you to dynamically add or remove functionalities and modify properties of an object (the weapon) without altering its core structure. In Unity, this is particularly useful for guns, armor, or character abilities where various upgrades and modifications can be applied.

**Core Idea:**

1.  **Base Component (IWeapon):** An interface or abstract class defining the common operations and properties of a weapon (e.g., damage, fire rate, accuracy, `Shoot()` method).
2.  **Concrete Component (BaseWeapon):** A specific weapon type that implements the `IWeapon` interface, providing initial stats and behavior.
3.  **Decorator (WeaponDecorator):** An abstract class that also implements `IWeapon` and holds a reference to another `IWeapon` object. Its methods typically delegate to the wrapped `IWeapon` object.
4.  **Concrete Decorators (GenericAttachmentDecorator):** Classes that extend `WeaponDecorator` and add or modify the behavior/properties of the wrapped `IWeapon`. We'll use ScriptableObjects to define attachment *data* and a single `GenericAttachmentDecorator` to apply those changes.
5.  **Manager (WeaponManager):** A component responsible for holding the base weapon and applying/removing attachments, rebuilding the decorated weapon chain as needed.

---

### **1. ScriptableObjects for Attachment Data**

These will define what each attachment *does*. Create these in your Unity project (e.g., `Assets/Attachments/Scope.asset`, `Assets/Attachments/Silencer.asset`).

**`WeaponAttachmentSO.cs`**
```csharp
using UnityEngine;
using System; // For Serializable, if needed later for more complex structs

/// <summary>
/// [SCRIPTABLE OBJECT]
/// Defines the data for a single weapon attachment.
/// This allows us to create multiple attachment types (e.g., Scope, Silencer, Extended Mag)
/// directly in the Unity Editor without writing new code for each.
///
/// This acts as the "Concrete Decorator Data" in the Decorator pattern context.
/// When applied, a GenericAttachmentDecorator will use this data to modify the weapon.
/// </summary>
[CreateAssetMenu(fileName = "NewAttachment", menuName = "Weapon System/Weapon Attachment")]
public class WeaponAttachmentSO : ScriptableObject
{
    [Header("Attachment Info")]
    public string attachmentName = "New Attachment";
    [TextArea]
    public string description = "A generic weapon attachment.";
    public Sprite icon; // Optional: for UI display

    [Header("Stat Modifiers")]
    [Tooltip("Additive damage modification (e.g., 5 for +5 damage).")]
    public float damageModifier = 0f;

    [Tooltip("Additive fire rate modification (e.g., 0.1 for +0.1 rounds/sec).")]
    public float fireRateModifier = 0f;

    [Tooltip("Additive accuracy modification (e.g., -0.05 for -5% spread).")]
    public float accuracyModifier = 0f; // Lower value = more accurate (less spread)

    // Example of a more complex modifier:
    // public int magazineCapacityModifier = 0;
    // public bool grantsZoomAbility = false;

    // You can add more complex behavior here, e.g., a reference to a visual prefab
    // public GameObject attachmentPrefab;
}
```

---

### **2. The Weapon Interface**

This defines the common contract for all weapons and decorated weapons.

**`IWeapon.cs`**
```csharp
using UnityEngine;

/// <summary>
/// [INTERFACE]
/// The 'Component' interface in the Decorator pattern.
/// Defines the common operations that both base weapons and decorated weapons must implement.
/// </summary>
public interface IWeapon
{
    string WeaponName { get; }
    float Damage { get; }
    float FireRate { get; }      // Rounds per second
    float Accuracy { get; }      // Lower value = more accurate (e.g., spread angle in degrees)
    // Add other relevant weapon properties (e.g., magazine size, reload time, range)

    void Shoot();
    // Add other weapon actions (e.g., Reload, Aim, Inspect)
}
```

---

### **3. The Base Weapon**

This is a concrete implementation of `IWeapon` with its default stats.

**`BaseWeapon.cs`**
```csharp
using UnityEngine;

/// <summary>
/// [MONOBEHAVIOUR]
/// The 'Concrete Component' in the Decorator pattern.
/// Represents a basic, un-modified weapon with its default statistics.
/// This will be a component on a GameObject (e.g., "PlayerWeapon").
/// </summary>
public class BaseWeapon : MonoBehaviour, IWeapon
{
    [Header("Base Weapon Stats")]
    [SerializeField] private string _weaponName = "Default Rifle";
    [SerializeField] private float _baseDamage = 10f;
    [SerializeField] private float _baseFireRate = 5f; // Rounds per second
    [SerializeField] private float _baseAccuracy = 0.5f; // Spread angle in degrees, lower is better

    // IWeapon Properties
    public string WeaponName => _weaponName;
    public float Damage => _baseDamage;
    public float FireRate => _baseFireRate;
    public float Accuracy => _baseAccuracy;

    // Last time the weapon was fired
    private float _lastFireTime;

    /// <summary>
    /// Implements the Shoot method from IWeapon.
    /// This is the base behavior for shooting. Decorated versions will call this.
    /// </summary>
    public virtual void Shoot()
    {
        if (Time.time - _lastFireTime < 1f / FireRate)
        {
            // Not enough time has passed since last shot
            Debug.Log($"[{WeaponName}] Click! (Weapon on cooldown)");
            return;
        }

        _lastFireTime = Time.time;
        Debug.Log($"[{WeaponName}] Base weapon fired! Damage: {Damage}, Fire Rate: {FireRate}, Accuracy: {Accuracy}");

        // In a real game, this would:
        // - Play muzzle flash animation
        // - Play shooting sound
        // - Instantiate a projectile or perform a raycast
        // - Apply recoil
    }

    /// <summary>
    /// Displays current weapon stats in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        gameObject.name = _weaponName;
    }
}
```

---

### **4. The Weapon Decorator (Abstract)**

The base class for all attachments that will wrap `IWeapon` objects.

**`WeaponDecorator.cs`**
```csharp
using UnityEngine;

/// <summary>
/// [ABSTRACT CLASS]
/// The 'Decorator' base class in the Decorator pattern.
/// This abstract class implements IWeapon and holds a reference to another IWeapon object.
/// Its purpose is to provide a common interface for all concrete decorators.
///
/// All concrete attachments (GenericAttachmentDecorator) will inherit from this.
/// </summary>
public abstract class WeaponDecorator : IWeapon
{
    protected IWeapon _wrappedWeapon;

    public WeaponDecorator(IWeapon wrappedWeapon)
    {
        _wrappedWeapon = wrappedWeapon;
    }

    // Default implementations that simply delegate to the wrapped weapon.
    // Concrete decorators will override these to modify properties or behavior.
    public virtual string WeaponName => _wrappedWeapon.WeaponName;
    public virtual float Damage => _wrappedWeapon.Damage;
    public virtual float FireRate => _wrappedWeapon.FireRate;
    public virtual float Accuracy => _wrappedWeapon.Accuracy;

    public virtual void Shoot()
    {
        _wrappedWeapon.Shoot();
    }
}
```

---

### **5. The Generic Attachment Decorator**

This concrete decorator applies the modifiers defined in a `WeaponAttachmentSO`. This is where the magic of dynamically changing stats happens.

**`GenericAttachmentDecorator.cs`**
```csharp
using UnityEngine;

/// <summary>
/// [CONCRETE DECORATOR]
/// This class applies the modifications defined by a WeaponAttachmentSO
/// to the wrapped IWeapon. It's 'generic' because it can apply any
/// modifications specified in the ScriptableObject data, rather than needing
/// a specific class for 'ScopeDecorator', 'SilencerDecorator', etc.
/// </summary>
public class GenericAttachmentDecorator : WeaponDecorator
{
    private WeaponAttachmentSO _attachmentData;

    public GenericAttachmentDecorator(IWeapon wrappedWeapon, WeaponAttachmentSO attachmentData)
        : base(wrappedWeapon)
    {
        _attachmentData = attachmentData;
    }

    // --- Overriding Properties to Apply Modifiers ---

    public override string WeaponName
    {
        get { return _wrappedWeapon.WeaponName + $" +{_attachmentData.attachmentName}"; }
    }

    public override float Damage
    {
        get { return _wrappedWeapon.Damage + _attachmentData.damageModifier; }
    }

    public override float FireRate
    {
        get { return _wrappedWeapon.FireRate + _attachmentData.fireRateModifier; }
    }

    public override float Accuracy
    {
        get { return _wrappedWeapon.Accuracy + _attachmentData.accuracyModifier; }
    }

    // --- Overriding Behavior (if needed) ---
    // Example: A silencer might modify the Shoot() sound or visual effect.
    public override void Shoot()
    {
        Debug.Log($"[{_attachmentData.attachmentName}] attachment applied its effect.");
        // Example: If a silencer, maybe play a different sound or remove muzzle flash.
        // if (_attachmentData.attachmentName == "Silencer")
        // {
        //     Debug.Log("Playing suppressed gunshot sound.");
        // }

        // Always call the base shoot to continue the chain
        base.Shoot();
    }
}
```

---

### **6. The Weapon Manager**

This `MonoBehaviour` will be attached to your player or weapon object. It manages the current weapon and applies/removes attachments.

**`WeaponManager.cs`**
```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .ToList() and .Any()

/// <summary>
/// [MONOBEHAVIOUR]
/// The main manager for the Weapon Attachment System.
/// This component will be attached to the player or a dedicated weapon GameObject.
/// It holds the base weapon and a list of active attachments.
/// When attachments are added or removed, it rebuilds the decorated weapon chain.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Tooltip("The base weapon component on this GameObject.")]
    [SerializeField] private BaseWeapon _baseWeapon;

    [Tooltip("Initial attachments applied to the weapon.")]
    [SerializeField] private List<WeaponAttachmentSO> _initialAttachments = new List<WeaponAttachmentSO>();

    // The currently equipped and decorated weapon instance.
    // This is the 'head' of our decorator chain.
    private IWeapon _currentEquippedWeapon;

    // A runtime list of attachments currently applied.
    private List<WeaponAttachmentSO> _activeAttachments = new List<WeaponAttachmentSO>();

    private void Awake()
    {
        // Ensure we have a BaseWeapon component
        if (_baseWeapon == null)
        {
            _baseWeapon = GetComponent<BaseWeapon>();
            if (_baseWeapon == null)
            {
                Debug.LogError("WeaponManager requires a BaseWeapon component on the same GameObject.", this);
                enabled = false;
                return;
            }
        }

        // Initialize active attachments with initial ones
        _activeAttachments.AddRange(_initialAttachments);

        // Build the initial weapon chain
        RebuildWeaponChain();
    }

    /// <summary>
    /// Rebuilds the entire weapon decorator chain from the base weapon and active attachments.
    /// This method is called whenever an attachment is added, removed, or the weapon is initialized.
    /// </summary>
    private void RebuildWeaponChain()
    {
        // Start with the base weapon
        IWeapon decoratedWeapon = _baseWeapon;

        // Apply each active attachment in order
        foreach (WeaponAttachmentSO attachment in _activeAttachments)
        {
            decoratedWeapon = new GenericAttachmentDecorator(decoratedWeapon, attachment);
        }

        _currentEquippedWeapon = decoratedWeapon;
        Debug.Log($"Weapon chain rebuilt. Current weapon: {_currentEquippedWeapon.WeaponName}");
        Debug.Log($"Stats: Damage={_currentEquippedWeapon.Damage}, FireRate={_currentEquippedWeapon.FireRate}, Accuracy={_currentEquippedWeapon.Accuracy}");
    }

    /// <summary>
    /// Adds a new attachment to the weapon.
    /// If the attachment is already present, it won't be added again.
    /// </summary>
    /// <param name="attachment">The ScriptableObject defining the attachment to add.</param>
    public void AddAttachment(WeaponAttachmentSO attachment)
    {
        if (attachment == null)
        {
            Debug.LogWarning("Attempted to add a null attachment.");
            return;
        }

        if (!_activeAttachments.Contains(attachment))
        {
            _activeAttachments.Add(attachment);
            Debug.Log($"Added attachment: {attachment.attachmentName}");
            RebuildWeaponChain();
        }
        else
        {
            Debug.Log($"Attachment '{attachment.attachmentName}' is already on the weapon.");
        }
    }

    /// <summary>
    /// Removes an attachment from the weapon.
    /// </summary>
    /// <param name="attachment">The ScriptableObject defining the attachment to remove.</param>
    public void RemoveAttachment(WeaponAttachmentSO attachment)
    {
        if (attachment == null)
        {
            Debug.LogWarning("Attempted to remove a null attachment.");
            return;
        }

        if (_activeAttachments.Remove(attachment))
        {
            Debug.Log($"Removed attachment: {attachment.attachmentName}");
            RebuildWeaponChain();
        }
        else
        {
            Debug.Log($"Attachment '{attachment.attachmentName}' was not found on the weapon.");
        }
    }

    /// <summary>
    /// Removes all attachments from the weapon.
    /// </summary>
    public void ClearAttachments()
    {
        if (_activeAttachments.Any())
        {
            _activeAttachments.Clear();
            Debug.Log("All attachments cleared.");
            RebuildWeaponChain();
        }
        else
        {
            Debug.Log("No attachments to clear.");
        }
    }

    /// <summary>
    /// Initiates the firing action of the current weapon.
    /// This is the primary way to interact with the weapon's behavior.
    /// </summary>
    public void Shoot()
    {
        _currentEquippedWeapon?.Shoot();
    }

    /// <summary>
    /// Utility method to get the currently decorated weapon's properties.
    /// </summary>
    public IWeapon GetCurrentWeapon()
    {
        return _currentEquippedWeapon;
    }

    // --- Example Usage / Debugging ---
    private void Update()
    {
        // Example: Press space to shoot
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }

        // Example: Press 'A' to add a specific attachment (requires an attachment SO assigned)
        // For demonstration, let's just make a placeholder. In a real game,
        // you'd get this from an inventory system or a pickup.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // IMPORTANT: Assign a WeaponAttachmentSO to this field in the Inspector for testing.
            Debug.Log("Add Attachment 1 pressed.");
            // Example: Assumes you have a public field for a test attachment
            // AddAttachment(yourTestAttachmentSO);
        }

        // Example: Press 'R' to remove an attachment
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Remove Attachment 2 pressed.");
            // Example: RemoveAttachment(yourTestAttachmentSO);
        }

        // Example: Press 'C' to clear all attachments
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Clear Attachments pressed.");
            // ClearAttachments();
        }
    }
}
```

---

### **How to Use in Unity (Step-by-Step):**

1.  **Create C# Scripts:**
    *   Create a folder named `Scripts` (or similar) in your Unity project.
    *   Create all the `.cs` files listed above (`IWeapon.cs`, `BaseWeapon.cs`, `WeaponAttachmentSO.cs`, `WeaponDecorator.cs`, `GenericAttachmentDecorator.cs`, `WeaponManager.cs`) inside this folder.

2.  **Create Attachment ScriptableObjects:**
    *   In the Unity Editor, go to `Assets -> Create -> Weapon System -> Weapon Attachment`.
    *   Create a few:
        *   **Scope:** `attachmentName = "Scope"`, `accuracyModifier = -0.2f`
        *   **Silencer:** `attachmentName = "Silencer"`, `damageModifier = -2f` (for realism), `accuracyModifier = 0.1f` (slight increase in spread for being less rigid)
        *   **ExtendedMag:** `attachmentName = "Extended Mag"`, `fireRateModifier = -0.5f` (heavier mag, slower manipulation), `damageModifier = 0f` (no change to damage)
    *   You can set these values as desired in the Inspector.

3.  **Set up your Player/Weapon GameObject:**
    *   Create an empty GameObject in your scene, rename it to `PlayerWeapon` (or whatever represents your weapon carrier).
    *   Add the `BaseWeapon` component to this `PlayerWeapon` GameObject.
    *   Configure its base stats (e.g., `_weaponName = "Assault Rifle"`, `_baseDamage = 15`, `_baseFireRate = 8`, `_baseAccuracy = 1.0`).
    *   Add the `WeaponManager` component to the *same* `PlayerWeapon` GameObject.

4.  **Assign Components in WeaponManager:**
    *   In the `WeaponManager` component's Inspector, drag the `PlayerWeapon` GameObject itself into the `_baseWeapon` slot. (Alternatively, the `Awake` method will try to find it automatically if it's on the same GameObject).
    *   Drag some of your created `WeaponAttachmentSO` assets into the `_initialAttachments` list to see them applied at startup.

5.  **Test:**
    *   Run the scene.
    *   Check the console. You should see messages indicating the weapon chain was rebuilt with initial attachments and its calculated stats.
    *   Press `Spacebar` to call `Shoot()`. You'll see the weapon firing with its final calculated stats.
    *   **To dynamically add/remove attachments during runtime for testing:**
        *   In `WeaponManager.cs`, add public fields for test attachments:
            ```csharp
            [Header("Test Attachments (for runtime debugging)")]
            public WeaponAttachmentSO testAttachment1;
            public WeaponAttachmentSO testAttachment2;
            ```
        *   Assign your `WeaponAttachmentSO`s to these new fields in the Inspector of your `PlayerWeapon`.
        *   Uncomment and modify the `Update()` method in `WeaponManager` to use these:
            ```csharp
            // Example: Press '1' to add Test Attachment 1
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                AddAttachment(testAttachment1);
            }

            // Example: Press '2' to remove Test Attachment 1
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                RemoveAttachment(testAttachment1);
            }

            // Example: Press '3' to add Test Attachment 2
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                AddAttachment(testAttachment2);
            }

            // Example: Press '4' to clear all attachments
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ClearAttachments();
            }
            ```
        *   Run the game and press the keys to see the stats change in the console.

This setup provides a robust, flexible, and easily extensible Weapon Attachment System using the Decorator pattern in Unity. You can add new types of attachments by simply creating new `WeaponAttachmentSO` assets without touching code. For more complex behaviors (e.g., an attachment that grants a unique ability), you might introduce more specialized `WeaponDecorator` subclasses, but the `GenericAttachmentDecorator` handles most stat modifications efficiently.