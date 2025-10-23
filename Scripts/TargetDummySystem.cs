// Unity Design Pattern Example: TargetDummySystem
// This script demonstrates the TargetDummySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'TargetDummySystem' pattern, while not one of the traditional Gang of Four patterns, is a practical design strategy often used in game development (and other domains) to provide a consistent interface for interacting with objects that *might* or *might not* exist, or *might* be a placeholder. It combines elements of the **Null Object Pattern** and the **Strategy Pattern**.

**Core Idea:**
The system ensures that any component requesting a 'target' always receives a valid object that implements a common `ITargetable` interface. If a real, interactive target (like an enemy or a player) is available, that object is provided. If no real target is present (e.g., nothing selected, target destroyed, or an editor-only placeholder), a special "dummy" target object is provided instead. This dummy object implements `ITargetable` but performs no actual actions and returns default/safe values.

**Benefits:**
1.  **Eliminates Null Checks:** Consumers of the `ITargetable` interface don't need to constantly check `if (target != null)` before calling methods or accessing properties.
2.  **Simplified Logic:** Code becomes cleaner and less cluttered, as it can treat both real and dummy targets uniformly.
3.  **Robustness:** Prevents `NullReferenceException` errors, making the system more stable.
4.  **Flexibility:** Easily swap between real targets and dummy targets without altering the calling code.
5.  **Testability/UI Display:** Useful for UI elements that always need to display target info (e.g., "No Target Selected") or for editor tools that need a placeholder.

---

Here's a complete, practical C# Unity example demonstrating the TargetDummySystem pattern. This script can be dropped directly into a Unity project.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // For List
using UnityEngine.UI; // For UI elements (Text, Slider)

/// <summary>
/// This script demonstrates the 'TargetDummySystem' design pattern in Unity.
/// The pattern ensures that any system requesting a 'target' always receives
/// a valid object implementing ITargetable, even if no real target exists.
/// If no real target is present, a 'TargetDummy' (Null Object) is provided.
///
/// Components:
/// 1. ITargetable: An interface defining the common contract for any object that can be targeted.
/// 2. RealTarget: A MonoBehaviour that implements ITargetable for actual game entities.
/// 3. TargetDummy: A plain C# class (singleton) that implements ITargetable, serving as the Null Object.
/// 4. TargetingSystem: A MonoBehaviour (singleton) that manages the current target.
///    It's responsible for providing either a RealTarget or the TargetDummy.Instance.
/// 5. TargetUI: A MonoBehaviour that demonstrates consuming the ITargetable from TargetingSystem
///    and updating UI without needing null checks.
/// 6. DemoInputManager: A simple script to facilitate switching targets for demonstration purposes.
/// </summary>

// =====================================================================================
// 1. ITargetable Interface
//    Defines the contract for anything that can be targeted.
// =====================================================================================
public interface ITargetable
{
    // A unique identifier for the target (e.g., GameObject.name)
    string TargetName { get; }
    // The Transform component of the target, useful for positioning, aiming etc.
    Transform TargetTransform { get; }
    // Current health of the target.
    float CurrentHealth { get; }
    // Maximum health of the target.
    float MaxHealth { get; }
    // True if the target is considered alive and active.
    bool IsAlive { get; }
    // True if this ITargetable is a dummy/placeholder and not a real entity.
    bool IsDummy { get; }

    // Methods that any targetable object should respond to.
    void TakeDamage(float amount);
    void OnTargeted();   // Called when this object becomes the active target.
    void OnUntargeted(); // Called when this object is no longer the active target.
}

// =====================================================================================
// 2. RealTarget Class
//    A concrete implementation of ITargetable for actual game objects.
// =====================================================================================
[RequireComponent(typeof(Collider))] // Ensure it can be clicked/selected
public class RealTarget : MonoBehaviour, ITargetable
{
    [Header("Target Properties")]
    [SerializeField] private string _targetName = "New Target";
    [SerializeField] private float _maxHealth = 100f;

    private float _currentHealth;
    private Material _originalMaterial;
    private Renderer _renderer;

    // ITargetable Properties
    public string TargetName => _targetName;
    public Transform TargetTransform => transform;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0;
    public bool IsDummy => false; // This is a real target

    void Awake()
    {
        _currentHealth = _maxHealth;
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material; // Store original material
        }
    }

    // ITargetable Methods
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        Debug.Log($"{TargetName} took {amount} damage. Health: {_currentHealth}/{_maxHealth}");

        if (!IsAlive)
        {
            Debug.Log($"{TargetName} has been defeated!");
            // Notify the targeting system to clear this target if it's currently selected
            if (TargetingSystem.Instance.CurrentTarget == this)
            {
                TargetingSystem.Instance.ClearTarget();
            }
            // Optionally disable or destroy the GameObject
            gameObject.SetActive(false);
        }
    }

    public void OnTargeted()
    {
        Debug.Log($"{TargetName} is now targeted!");
        if (_renderer != null)
        {
            // Example visual feedback: change color
            _renderer.material.color = Color.yellow;
        }
    }

    public void OnUntargeted()
    {
        Debug.Log($"{TargetName} is no longer targeted.");
        if (_renderer != null && _originalMaterial != null)
        {
            // Restore original material
            _renderer.material = _originalMaterial;
        }
    }

    // Unity specific method to make it selectable via mouse click for demo
    private void OnMouseDown()
    {
        TargetingSystem.Instance.SelectTarget(this);
    }
}

// =====================================================================================
// 3. TargetDummy Class
//    Implements ITargetable but acts as a Null Object.
//    It provides default values and performs no actions.
// =====================================================================================
public class TargetDummy : ITargetable
{
    // Singleton pattern for the TargetDummy
    private static TargetDummy _instance;
    public static TargetDummy Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new TargetDummy();
            }
            return _instance;
        }
    }

    // Private constructor to enforce singleton pattern
    private TargetDummy() { }

    // ITargetable Properties - always return safe, default values
    public string TargetName => "No Target Selected";
    public Transform TargetTransform => null; // A dummy doesn't have a real transform
    public float CurrentHealth => 0f;       // Indicate empty health
    public float MaxHealth => 1f;           // Provide a max for proportion calculations
    public bool IsAlive => false;           // A dummy is never 'alive'
    public bool IsDummy => true;            // Clearly marks this as a dummy

    // ITargetable Methods - perform no operations
    public void TakeDamage(float amount)
    {
        // Do nothing, a dummy cannot take damage
        Debug.Log("TargetDummy tried to take damage, but it's a dummy!");
    }

    public void OnTargeted()
    {
        // Do nothing
        Debug.Log("TargetDummy was 'targeted'.");
    }

    public void OnUntargeted()
    {
        // Do nothing
        Debug.Log("TargetDummy was 'untargeted'.");
    }
}

// =====================================================================================
// 4. TargetingSystem Class
//    Manages the currently selected ITargetable.
//    Crucially, it guarantees to always return a non-null ITargetable.
// =====================================================================================
public class TargetingSystem : MonoBehaviour
{
    // Singleton pattern for the TargetingSystem
    public static TargetingSystem Instance { get; private set; }

    // The currently selected real target. Can be null if nothing is selected.
    private ITargetable _selectedRealTarget;

    // Public property to access the current target.
    // This is the core of the pattern: it returns the real target if available,
    // otherwise it returns the TargetDummy.Instance.
    public ITargetable CurrentTarget
    {
        get { return _selectedRealTarget ?? TargetDummy.Instance; }
    }

    // Event to notify other systems when the target changes.
    public event Action<ITargetable> OnTargetChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple TargetingSystem instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Selects a new target. Can be null to clear the current target.
    /// </summary>
    /// <param name="newTarget">The new ITargetable to select, or null to deselect.</param>
    public void SelectTarget(ITargetable newTarget)
    {
        // If there was a previously selected *real* target, notify it that it's untargeted.
        if (_selectedRealTarget != null && _selectedRealTarget != TargetDummy.Instance)
        {
            _selectedRealTarget.OnUntargeted();
        }

        // Set the new target. If newTarget is null, _selectedRealTarget will become null.
        // The CurrentTarget property will then automatically provide TargetDummy.Instance.
        _selectedRealTarget = newTarget;

        // If the new target is a *real* target, notify it that it's targeted.
        if (_selectedRealTarget != null && _selectedRealTarget != TargetDummy.Instance)
        {
            _selectedRealTarget.OnTargeted();
        }

        // Notify all subscribers that the target has changed.
        // Always pass the result of CurrentTarget to ensure it's never null.
        OnTargetChanged?.Invoke(CurrentTarget);
    }

    /// <summary>
    /// Clears the currently selected target.
    /// </summary>
    public void ClearTarget()
    {
        SelectTarget(null);
        Debug.Log("Targeting system cleared current target.");
    }
}

// =====================================================================================
// 5. TargetUI Class
//    Demonstrates consuming the ITargetable from TargetingSystem without null checks.
// =====================================================================================
public class TargetUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text targetNameText;
    public Slider healthSlider;
    public Text healthValueText;
    public GameObject healthBarPanel; // Panel to show/hide health bar

    void Start()
    {
        if (TargetingSystem.Instance != null)
        {
            // Subscribe to target changes
            TargetingSystem.Instance.OnTargetChanged += UpdateTargetUI;
            // Immediately update UI with initial target (which will be the TargetDummy initially)
            UpdateTargetUI(TargetingSystem.Instance.CurrentTarget);
        }
        else
        {
            Debug.LogError("TargetUI requires a TargetingSystem instance in the scene!");
        }
    }

    void OnDestroy()
    {
        if (TargetingSystem.Instance != null)
        {
            TargetingSystem.Instance.OnTargetChanged -= UpdateTargetUI;
        }
    }

    /// <summary>
    /// Updates the UI based on the provided ITargetable.
    /// Notice there are NO null checks for 'target' here!
    /// </summary>
    /// <param name="target">The current ITargetable (either RealTarget or TargetDummy).</param>
    private void UpdateTargetUI(ITargetable target)
    {
        targetNameText.text = target.TargetName;
        healthSlider.value = target.CurrentHealth / target.MaxHealth;
        healthValueText.text = $"{target.CurrentHealth:F0} / {target.MaxHealth:F0}";

        // If it's a dummy, hide the health bar and ensure text is appropriate.
        healthBarPanel.SetActive(!target.IsDummy);
        if (target.IsDummy)
        {
            // Override health text for dummy if needed, or just let it show 0/1
            // healthValueText.text = ""; 
        }

        // Example: If the dummy is active, maybe grey out the name text
        targetNameText.color = target.IsDummy ? Color.grey : Color.white;

        Debug.Log($"UI Updated: Displaying {target.TargetName}. Is Dummy: {target.IsDummy}");
    }
}

// =====================================================================================
// 6. DemoInputManager Class
//    A simple script to demonstrate selecting and interacting with targets via input.
// =====================================================================================
public class DemoInputManager : MonoBehaviour
{
    [Header("Demo Targets")]
    public List<RealTarget> selectableTargets;

    private int _currentIndex = -1;

    void Update()
    {
        // Press 'N' to select the Next target in the list
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (selectableTargets == null || selectableTargets.Count == 0)
            {
                Debug.Log("No selectable targets assigned for demo.");
                return;
            }

            _currentIndex++;
            if (_currentIndex >= selectableTargets.Count)
            {
                _currentIndex = 0; // Loop back to the first target
            }

            if (selectableTargets[_currentIndex] != null && selectableTargets[_currentIndex].IsAlive)
            {
                TargetingSystem.Instance.SelectTarget(selectableTargets[_currentIndex]);
            }
            else
            {
                Debug.Log($"Skipping target {_currentIndex} as it's null or not alive. Trying next...");
                // Recursively call to find the next valid target
                Update(); 
            }
        }

        // Press 'C' to Clear the target
        if (Input.GetKeyDown(KeyCode.C))
        {
            TargetingSystem.Instance.ClearTarget();
        }

        // Press 'D' to Deal damage to the current target
        if (Input.GetKeyDown(KeyCode.D))
        {
            // Get the current target (either real or dummy)
            ITargetable currentTarget = TargetingSystem.Instance.CurrentTarget;

            // Check if it's a real target before attempting to deal damage
            // (While TakeDamage on Dummy does nothing, this shows common usage)
            if (!currentTarget.IsDummy)
            {
                currentTarget.TakeDamage(20f);
                // After damage, if it's still the current target, update UI
                if (TargetingSystem.Instance.CurrentTarget == currentTarget)
                {
                    TargetingSystem.Instance.OnTargetChanged?.Invoke(currentTarget);
                }
            }
            else
            {
                Debug.Log("Cannot deal damage: No real target selected.");
            }
        }
    }
}

/*
/// =====================================================================================
/// HOW TO SET UP AND USE IN UNITY:
/// =====================================================================================
///
/// 1.  Create a new C# script named 'TargetDummySystemExample' (or any name),
///     copy and paste ALL the code above into it.
///
/// 2.  **Scene Setup:**
///     a.  **GameManager:** Create an empty GameObject named "GameManager".
///         Attach the `TargetingSystem` script to it.
///         Attach the `DemoInputManager` script to it.
///
///     b.  **Real Targets:** Create a few 3D objects (e.g., Cubes, Spheres).
///         Rename them (e.g., "Enemy1", "PlayerTarget", "Boss").
///         Add a `RealTarget` component to each of them.
///         Ensure each has a `Collider` component (e.g., Box Collider, Sphere Collider)
///         so they can be clicked with `OnMouseDown`.
///         Adjust their `_targetName` and `_maxHealth` in the Inspector.
///
///     c.  **UI Canvas:**
///         i.  Create a UI Canvas (Right-click in Hierarchy -> UI -> Canvas).
///         ii. Inside the Canvas, create a UI Panel (Right-click Canvas -> UI -> Panel).
///             Rename it "TargetInfoPanel" or similar. This will be the `healthBarPanel`.
///             Set its `Rect Transform` anchors to something suitable (e.g., top-center).
///         iii. Inside "TargetInfoPanel", create a UI Text object (Right-click Panel -> UI -> Text).
///              Rename it "TargetNameText". Assign this to `targetNameText` in `TargetUI`.
///         iv. Inside "TargetInfoPanel", create a UI Slider (Right-click Panel -> UI -> Slider).
///             Rename it "HealthSlider". Assign this to `healthSlider` in `TargetUI`.
///             Remove the `Handle` component from the Slider's children if you just want a bar.
///         v.  Inside "TargetInfoPanel", create another UI Text object.
///             Rename it "HealthValueText". Assign this to `healthValueText` in `TargetUI`.
///         vi. Attach the `TargetUI` script to the "TargetInfoPanel" GameObject.
///             Drag and drop the created UI elements into their respective fields in the `TargetUI` Inspector.
///
/// 3.  **DemoInputManager Setup:**
///     On the "GameManager" GameObject, select the `DemoInputManager` component.
///     Drag all your `RealTarget` GameObjects from the Hierarchy into the `Selectable Targets` list in the Inspector.
///
/// 4.  **Run the Scene:**
///     -   Initially, the UI will show "No Target Selected" and an empty/hidden health bar.
///         This is the `TargetDummy` in action, providing default values.
///     -   Click on any of your `RealTarget` GameObjects. The UI will update to show its name and health.
///         The `RealTarget` will change its material color (e.g., to yellow).
///     -   Press 'N' (Next) to cycle through the targets you added to the `DemoInputManager`.
///     -   Press 'D' (Damage) to deal 20 damage to the currently selected *real* target.
///         Observe the health bar and health value update.
///         If you press 'D' when "No Target Selected" is shown, it will print a debug message but do nothing to the UI or any real target.
///     -   Press 'C' (Clear) to deselect the current target. The UI will revert to showing "No Target Selected".
///     -   If a target's health reaches 0, it will be deactivated, and the targeting system will automatically clear it, reverting the UI to the `TargetDummy` state.
///
/// This setup provides a complete and interactive demonstration of the TargetDummySystem pattern, highlighting its benefits in managing target selection and UI updates robustly.
*/
```