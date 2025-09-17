// Unity Design Pattern Example: HitboxSystem
// This script demonstrates the HitboxSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Hitbox System design pattern is crucial for action-oriented games, especially fighting games, and even RPGs or platformers with combat. It separates the areas that *deal* damage (hitboxes) from the areas that *receive* damage (hurtboxes). This allows for precise collision detection, distinct attack properties, and clear visual feedback.

This example provides a complete, practical C# Unity implementation. It includes:
*   **`HitData`**: A struct to define the properties of a hit (damage, knockback, etc.).
*   **`Hurtbox`**: A component representing a vulnerable area that can be hit. It emits an event when struck.
*   **`Hitbox`**: A component representing an offensive area that detects `Hurtbox` collisions. It can be activated and deactivated.
*   **`CharacterHitboxManager`**: A central component for a character to control all its `Hitbox` and `Hurtbox` components.
*   **`AttackerExample`**: Demonstrates how a character might activate a hitbox during an attack animation.
*   **`TargetExample`**: Demonstrates how a character might react when its hurtbox is hit.

---

### How the Hitbox System Pattern Works:

1.  **Hitbox (Attacker's Offensive Zone):**
    *   Attached to parts of an attacking character (e.g., a fist, a sword swing, a projectile).
    *   Often has a `Collider` component set to `Is Trigger`.
    *   When activated, it checks for overlaps with `Hurtbox` components.
    *   It carries `HitData` (damage, knockback, etc.) that defines the properties of the attack.
    *   Crucially, a hitbox typically has an "owner" to prevent self-hitting.
    *   Often cleared (deactivated) after a single hit, or after its active duration, to prevent spamming hits.

2.  **Hurtbox (Defender's Vulnerable Zone):**
    *   Attached to parts of a defending character (e.g., head, torso, legs).
    *   Also has a `Collider` component set to `Is Trigger`.
    *   It doesn't *deal* damage itself; it merely defines an area that *can be hit*.
    *   It also has an "owner" for context.
    *   When a `Hitbox` overlaps with it, the `Hurtbox` is notified (usually via an event or callback).

3.  **HitData (The Payload):**
    *   A struct or class that encapsulates all the relevant information about a specific hit.
    *   Examples: `damageAmount`, `knockbackForce`, `hitStunDuration`, `hitType` (e.g., Light, Heavy, Projectile), `attackerGameObject`.

4.  **Manager (Or Character Controller):**
    *   A central script on the character that manages the activation and deactivation of its `Hitbox` components.
    *   Often tied into animation events or attack logic.
    *   Provides a clean API for the character's main script to trigger attacks without needing to know the specifics of each individual hitbox.

---

### Unity Setup Instructions:

1.  **Create a new C# script** in your Unity project, name it `HitboxSystem`.
2.  **Copy and paste** the entire code block below into the `HitboxSystem.cs` file.
3.  **Create an Attacker GameObject:**
    *   Create an empty GameObject (e.g., "PlayerCharacter").
    *   Add the `CharacterHitboxManager` component to it.
    *   Add the `AttackerExample` component to it.
    *   As children of "PlayerCharacter", create new empty GameObjects for hitboxes (e.g., "PunchHitbox", "KickHitbox").
    *   Add a `Hitbox` component to each of these child GameObjects.
    *   Add a `Collider` component (e.g., `BoxCollider`, `SphereCollider`) to each hitbox child. **Crucially, mark it as `Is Trigger`**. Position and size these colliders appropriately.
    *   In the `Hitbox` component, assign the "PlayerCharacter" GameObject as the `Owner`.
    *   Also as children of "PlayerCharacter", create new empty GameObjects for hurtboxes (e.g., "BodyHurtbox", "HeadHurtbox").
    *   Add a `Hurtbox` component to each of these child GameObjects.
    *   Add a `Collider` component (e.g., `BoxCollider`, `SphereCollider`) to each hurtbox child. **Crucially, mark it as `Is Trigger`**. Position and size these colliders appropriately.
    *   In the `Hurtbox` component, assign the "PlayerCharacter" GameObject as the `Owner`.
    *   On the `CharacterHitboxManager`, ensure the `Hitboxes` and `Hurtboxes` arrays are populated (you might need to drag the child Hitbox/Hurtbox GameObjects into the slots, or use the "Populate from Children" button in Inspector if I implement one).
    *   On the `AttackerExample`, drag the `CharacterHitboxManager` into its slot.
    *   Make sure both attacker and target have a `Rigidbody` (can be kinematic if you only want trigger detection without physics simulation).

4.  **Create a Target GameObject:**
    *   Create another empty GameObject (e.g., "EnemyCharacter").
    *   Add the `CharacterHitboxManager` component to it.
    *   Add the `TargetExample` component to it.
    *   Similar to the attacker, create child GameObjects for hurtboxes (e.g., "EnemyBodyHurtbox").
    *   Add a `Hurtbox` component to each, attach a `Collider` (`Is Trigger`), set `Owner` to "EnemyCharacter".
    *   On the `TargetExample`, drag the main `Hurtbox` (e.g., "EnemyBodyHurtbox") into its slot.
    *   Make sure it also has a `Rigidbody`.

5.  **Run the scene!** Observe the debug logs and Gizmos. Press 'A' for the attacker to punch.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // For HashSet
using System.Linq; // For LINQ operations like ToArray and Find

// This single script file contains all necessary components for the Hitbox System.
// You can copy and paste this entire content into a new C# script named 'HitboxSystem.cs'
// in your Unity project.

#region HitData
/// <summary>
/// HitData struct defines the properties of a specific hit event.
/// This data is passed from a Hitbox to a Hurtbox upon collision.
/// </summary>
[System.Serializable] // Make it visible in the Inspector
public struct HitData
{
    public enum HitType
    {
        LightAttack,
        MediumAttack,
        HeavyAttack,
        Projectile,
        Special,
        Grab
    }

    [Tooltip("Amount of damage this hit inflicts.")]
    public int damageAmount;

    [Tooltip("Force applied for knockback. Direction will be calculated based on collision.")]
    public float knockbackForce;

    [Tooltip("Duration the target will be stunned/frozen after being hit.")]
    public float hitStunDuration;

    [Tooltip("Type of hit, useful for various game logic (e.g., different sound effects, reactions).")]
    public HitType hitType;

    [Tooltip("The GameObject that owns the Hitbox causing this hit.")]
    public GameObject attackerGameObject;

    /// <summary>
    /// Creates a new HitData instance.
    /// </summary>
    public HitData(int damage, float knockback, float stun, HitType type, GameObject attacker)
    {
        damageAmount = damage;
        knockbackForce = knockback;
        hitStunDuration = stun;
        hitType = type;
        attackerGameObject = attacker;
    }
}
#endregion

#region Hurtbox
/// <summary>
/// A Hurtbox represents a vulnerable area on a character that can be hit by a Hitbox.
/// It detects collisions and informs its owner when a hit occurs.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hurtbox : MonoBehaviour
{
    [Tooltip("The owner of this Hurtbox (e.g., the character GameObject). Used to prevent self-hitting.")]
    [SerializeField] private GameObject _owner;

    [Tooltip("The Collider component associated with this Hurtbox. Must be set to 'Is Trigger'.")]
    [SerializeField] private Collider _hurtCollider;

    /// <summary>
    /// Event triggered when this Hurtbox is hit by a Hitbox.
    /// Subscribers (e.g., a character's health script) can react to the hit data.
    /// </summary>
    public event Action<HitData> OnHit;

    public GameObject Owner => _owner;
    public Collider HurtCollider => _hurtCollider;

    private void Awake()
    {
        ValidateCollider();
    }

    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Ensures the collider is correctly set up.
    /// </summary>
    private void OnValidate()
    {
        if (_hurtCollider == null)
        {
            _hurtCollider = GetComponent<Collider>();
        }
        ValidateCollider();
    }

    /// <summary>
    /// Checks if the assigned collider is a trigger, logging an error if not.
    /// </summary>
    private void ValidateCollider()
    {
        if (_hurtCollider != null && !_hurtCollider.isTrigger)
        {
            Debug.LogError($"Hurtbox on '{name}' requires its Collider ('{_hurtCollider.name}') to be set to 'Is Trigger'.", this);
        }
    }

    /// <summary>
    /// Processes an incoming hit from a Hitbox.
    /// This method is typically called by a Hitbox when a collision occurs.
    /// </summary>
    /// <param name="hitData">The data describing the hit.</param>
    public void ReceiveHit(HitData hitData)
    {
        if (_owner == null)
        {
            Debug.LogWarning($"Hurtbox '{name}' received a hit but has no owner assigned!", this);
            return;
        }

        Debug.Log($"<color=red>{_owner.name}'s Hurtbox '{name}' was hit!</color> " +
                  $"Damage: {hitData.damageAmount}, Knockback: {hitData.knockbackForce}, Type: {hitData.hitType}");

        // Invoke the OnHit event, notifying any subscribers (e.g., character health, animation).
        OnHit?.Invoke(hitData);
    }

    /// <summary>
    /// Draws a visual representation of the Hurtbox in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_hurtCollider == null) return;

        Gizmos.color = Color.green * 0.5f; // Semi-transparent green
        DrawColliderGizmo(_hurtCollider);
    }

    /// <summary>
    /// Helper to draw gizmos for different collider types.
    /// </summary>
    private void DrawColliderGizmo(Collider col)
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(col.transform.position, col.transform.rotation, col.transform.lossyScale);

        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            // Capsule gizmos are a bit more complex. For simplicity, just draw wire sphere at top/bottom.
            // A perfect capsule gizmo would require more custom drawing.
            // This is a basic approximation.
            Vector3 center = capsule.center;
            float radius = capsule.radius;
            float height = capsule.height;

            Vector3 p1 = center;
            Vector3 p2 = center;

            switch (capsule.direction)
            {
                case 0: // X-axis
                    p1 += Vector3.right * (height / 2f - radius);
                    p2 += Vector3.left * (height / 2f - radius);
                    break;
                case 1: // Y-axis
                    p1 += Vector3.up * (height / 2f - radius);
                    p2 += Vector3.down * (height / 2f - radius);
                    break;
                case 2: // Z-axis
                    p1 += Vector3.forward * (height / 2f - radius);
                    p2 += Vector3.back * (height / 2f - radius);
                    break;
            }

            Gizmos.DrawSphere(p1, radius);
            Gizmos.DrawSphere(p2, radius);
            // Draw a line connecting the centers of the two spheres for the cylinder part, if desired.
            Gizmos.DrawLine(p1, p2);

            Gizmos.DrawWireSphere(p1, radius);
            Gizmos.DrawWireSphere(p2, radius);
        }

        Gizmos.matrix = oldMatrix;
    }
}
#endregion

#region Hitbox
/// <summary>
/// A Hitbox represents an offensive area that, when active, can deal damage to Hurtboxes.
/// It uses Unity's trigger system for collision detection.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [Tooltip("The owner of this Hitbox (e.g., the character GameObject). Used to prevent self-hitting.")]
    [SerializeField] private GameObject _owner;

    [Tooltip("The Collider component associated with this Hitbox. Must be set to 'Is Trigger'.")]
    [SerializeField] private Collider _hitboxCollider;

    [Tooltip("Default hit data for this hitbox. Can be overridden when activated.")]
    [SerializeField] private HitData _defaultHitData;

    private bool _isActive;
    private HitData _currentHitData; // The actual hit data used when active

    // A set to keep track of Hurtboxes that have already been hit by THIS specific activation
    // This prevents a single hitbox activation from hitting the same Hurtbox multiple times.
    private HashSet<Hurtbox> _hitTargets = new HashSet<Hurtbox>();

    public GameObject Owner => _owner;
    public Collider HitboxCollider => _hitboxCollider;

    private void Awake()
    {
        ValidateCollider();
        _hitboxCollider.enabled = false; // Start inactive
    }

    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Ensures the collider is correctly set up.
    /// </summary>
    private void OnValidate()
    {
        if (_hitboxCollider == null)
        {
            _hitboxCollider = GetComponent<Collider>();
        }
        ValidateCollider();
    }

    /// <summary>
    /// Checks if the assigned collider is a trigger, logging an error if not.
    /// </summary>
    private void ValidateCollider()
    {
        if (_hitboxCollider != null && !_hitboxCollider.isTrigger)
        {
            Debug.LogError($"Hitbox on '{name}' requires its Collider ('{_hitboxCollider.name}') to be set to 'Is Trigger'.", this);
        }
    }

    /// <summary>
    /// Activates the hitbox, enabling collision detection.
    /// An optional HitData can be provided to override the default for this activation.
    /// </summary>
    /// <param name="overrideHitData">Optional HitData to use for this activation.</param>
    public void Activate(HitData? overrideHitData = null)
    {
        _isActive = true;
        _hitboxCollider.enabled = true; // Enable the collider to detect triggers
        _currentHitData = overrideHitData ?? _defaultHitData; // Use override if provided, else default
        _currentHitData.attackerGameObject = _owner; // Ensure attacker is always correct
        _hitTargets.Clear(); // Clear previously hit targets for a fresh activation

        Debug.Log($"<color=blue>Hitbox '{name}' activated</color> with Damage: {_currentHitData.damageAmount}");
    }

    /// <summary>
    /// Deactivates the hitbox, disabling collision detection.
    /// </summary>
    public void Deactivate()
    {
        _isActive = false;
        _hitboxCollider.enabled = false; // Disable the collider
        _hitTargets.Clear(); // Clear the hit targets when deactivating

        Debug.Log($"<color=blue>Hitbox '{name}' deactivated</color>");
    }

    /// <summary>
    /// Called when this hitbox's trigger collider enters another collider.
    /// </summary>
    /// <param name="other">The other collider.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return; // Only process if active

        // Attempt to get a Hurtbox component from the collided object
        Hurtbox hurtbox = other.GetComponent<Hurtbox>();

        if (hurtbox != null)
        {
            // IMPORTANT: Prevent self-hitting
            if (hurtbox.Owner == _owner)
            {
                // Debug.Log($"Hitbox '{name}' tried to hit its own Hurtbox '{hurtbox.name}'. Ignored.", this);
                return;
            }

            // Prevent hitting the same hurtbox multiple times during this activation
            if (_hitTargets.Contains(hurtbox))
            {
                // Debug.Log($"Hitbox '{name}' already hit Hurtbox '{hurtbox.name}' during this activation. Ignored.", this);
                return;
            }

            // A valid hit occurred!
            _hitTargets.Add(hurtbox); // Mark this hurtbox as hit for this activation

            // Pass the current hit data to the Hurtbox.
            hurtbox.ReceiveHit(_currentHitData);

            // Optional: If you want the hitbox to only hit once per activation, you could deactivate it here.
            // For example, a single-hit projectile or a short-duration attack that disappears after first contact.
            // If it's meant to hit multiple targets (e.g., a sweep attack), leave this commented.
            // Deactivate();
        }
    }

    /// <summary>
    /// Draws a visual representation of the Hitbox in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_hitboxCollider == null) return;

        Gizmos.color = _isActive ? Color.red : Color.red * 0.3f; // Red when active, dim red when inactive
        DrawColliderGizmo(_hitboxCollider);
    }

    /// <summary>
    /// Helper to draw gizmos for different collider types.
    /// </summary>
    private void DrawColliderGizmo(Collider col)
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(col.transform.position, col.transform.rotation, col.transform.lossyScale);

        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            Vector3 center = capsule.center;
            float radius = capsule.radius;
            float height = capsule.height;

            Vector3 p1 = center;
            Vector3 p2 = center;

            switch (capsule.direction)
            {
                case 0: // X-axis
                    p1 += Vector3.right * (height / 2f - radius);
                    p2 += Vector3.left * (height / 2f - radius);
                    break;
                case 1: // Y-axis
                    p1 += Vector3.up * (height / 2f - radius);
                    p2 += Vector3.down * (height / 2f - radius);
                    break;
                case 2: // Z-axis
                    p1 += Vector3.forward * (height / 2f - radius);
                    p2 += Vector3.back * (height / 2f - radius);
                    break;
            }

            Gizmos.DrawSphere(p1, radius);
            Gizmos.DrawSphere(p2, radius);
            Gizmos.DrawLine(p1, p2);

            Gizmos.DrawWireSphere(p1, radius);
            Gizmos.DrawWireSphere(p2, radius);
        }
        Gizmos.matrix = oldMatrix;
    }
}
#endregion

#region CharacterHitboxManager
/// <summary>
/// CharacterHitboxManager acts as a central control point for all Hitboxes and Hurtboxes
/// belonging to a single character. It provides methods to activate/deactivate hitboxes
/// by name, making it easier for character controllers or animation systems to interact
/// with the hitbox system.
/// </summary>
public class CharacterHitboxManager : MonoBehaviour
{
    [Tooltip("All Hitbox components associated with this character.")]
    [SerializeField] private Hitbox[] _hitboxes;

    [Tooltip("All Hurtbox components associated with this character.")]
    [SerializeField] private Hurtbox[] _hurtboxes;

    private Dictionary<string, Hitbox> _hitboxLookup = new Dictionary<string, Hitbox>();
    private Dictionary<string, Hurtbox> _hurtboxLookup = new Dictionary<string, Hurtbox>();

    private void Awake()
    {
        PopulateLookups();
        // Ensure all hitboxes start inactive
        foreach (var hitbox in _hitboxes)
        {
            if (hitbox != null) hitbox.Deactivate();
        }
    }

    /// <summary>
    /// Populates the dictionary lookups for quick access to hitboxes/hurtboxes by name.
    /// </summary>
    private void PopulateLookups()
    {
        _hitboxLookup.Clear();
        foreach (var hitbox in _hitboxes)
        {
            if (hitbox != null && !_hitboxLookup.ContainsKey(hitbox.name))
            {
                _hitboxLookup.Add(hitbox.name, hitbox);
            }
        }

        _hurtboxLookup.Clear();
        foreach (var hurtbox in _hurtboxes)
        {
            if (hurtbox != null && !_hurtboxLookup.ContainsKey(hurtbox.name))
            {
                _hurtboxLookup.Add(hurtbox.name, hurtbox);
            }
        }
    }

    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Useful for auto-populating arrays in the editor.
    /// </summary>
    private void OnValidate()
    {
        // Auto-populate hitboxes and hurtboxes from children if they are not already set.
        // This is a convenience for initial setup.
        if (_hitboxes == null || _hitboxes.Length == 0)
        {
            _hitboxes = GetComponentsInChildren<Hitbox>(true); // include inactive
            Debug.Log($"Auto-populated {name}'s Hitboxes: {_hitboxes.Length} found.");
        }
        if (_hurtboxes == null || _hurtboxes.Length == 0)
        {
            _hurtboxes = GetComponentsInChildren<Hurtbox>(true); // include inactive
            Debug.Log($"Auto-populated {name}'s Hurtboxes: {_hurtboxes.Length} found.");
        }

        PopulateLookups(); // Ensure lookups are up-to-date in editor as well.
    }

    /// <summary>
    /// Activates a specific hitbox by its name.
    /// </summary>
    /// <param name="hitboxName">The name of the Hitbox GameObject.</param>
    /// <param name="overrideHitData">Optional HitData to use for this activation.</param>
    public void ActivateHitbox(string hitboxName, HitData? overrideHitData = null)
    {
        if (_hitboxLookup.TryGetValue(hitboxName, out Hitbox hitbox))
        {
            hitbox.Activate(overrideHitData);
        }
        else
        {
            Debug.LogWarning($"Hitbox '{hitboxName}' not found on '{name}'.", this);
        }
    }

    /// <summary>
    /// Deactivates a specific hitbox by its name.
    /// </summary>
    /// <param name="hitboxName">The name of the Hitbox GameObject.</param>
    public void DeactivateHitbox(string hitboxName)
    {
        if (_hitboxLookup.TryGetValue(hitboxName, out Hitbox hitbox))
        {
            hitbox.Deactivate();
        }
        else
        {
            Debug.LogWarning($"Hitbox '{hitboxName}' not found on '{name}'.", this);
        }
    }

    /// <summary>
    /// Deactivates all hitboxes currently managed by this character.
    /// </summary>
    public void DeactivateAllHitboxes()
    {
        foreach (var hitbox in _hitboxes)
        {
            if (hitbox != null)
            {
                hitbox.Deactivate();
            }
        }
    }

    /// <summary>
    /// Gets a specific Hurtbox component by name.
    /// Useful if a character's main script needs direct access to a specific hurtbox's event.
    /// </summary>
    /// <param name="hurtboxName">The name of the Hurtbox GameObject.</param>
    /// <returns>The Hurtbox component if found, otherwise null.</returns>
    public Hurtbox GetHurtbox(string hurtboxName)
    {
        _hurtboxLookup.TryGetValue(hurtboxName, out Hurtbox hurtbox);
        return hurtbox;
    }
}
#endregion

#region AttackerExample
/// <summary>
/// Example usage: Attacker demonstrates how a character's controller might
/// use the CharacterHitboxManager to activate and deactivate hitboxes during an attack.
/// </summary>
public class AttackerExample : MonoBehaviour
{
    [Tooltip("Reference to the CharacterHitboxManager on this character.")]
    [SerializeField] private CharacterHitboxManager _hitboxManager;

    [Tooltip("Default HitData for a punch attack.")]
    [SerializeField] private HitData _punchHitData = new HitData(10, 5f, 0.5f, HitData.HitType.LightAttack, null);

    [Tooltip("Name of the hitbox for the punch attack.")]
    [SerializeField] private string _punchHitboxName = "PunchHitbox";

    [Tooltip("Duration the punch hitbox stays active.")]
    [SerializeField] private float _punchDuration = 0.2f;

    private Coroutine _attackCoroutine;

    private void Start()
    {
        if (_hitboxManager == null)
        {
            _hitboxManager = GetComponent<CharacterHitboxManager>();
            if (_hitboxManager == null)
            {
                Debug.LogError("AttackerExample requires a CharacterHitboxManager component!", this);
                enabled = false;
                return;
            }
        }
        // Ensure the attacker GameObject reference in HitData is set correctly
        _punchHitData.attackerGameObject = gameObject;
    }

    private void Update()
    {
        // Simulate an attack input
        if (Input.GetKeyDown(KeyCode.A)) // Press 'A' to punch
        {
            PerformPunchAttack();
        }
    }

    /// <summary>
    /// Initiates a punch attack.
    /// </summary>
    private void PerformPunchAttack()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
        }
        _attackCoroutine = StartCoroutine(PunchAttackRoutine());
    }

    /// <summary>
    /// Coroutine to manage the activation and deactivation of the punch hitbox.
    /// This simulates an animation window where the hitbox is active.
    /// </summary>
    private System.Collections.IEnumerator PunchAttackRoutine()
    {
        Debug.Log($"{name} starts punch attack!");
        // Activate the specific hitbox using the manager
        // We can pass an override HitData here if this specific punch has unique properties.
        _hitboxManager.ActivateHitbox(_punchHitboxName, _punchHitData);

        yield return new WaitForSeconds(_punchDuration); // Hitbox is active for this duration

        Debug.Log($"{name} ends punch attack!");
        // Deactivate the hitbox
        _hitboxManager.DeactivateHitbox(_punchHitboxName);
        _attackCoroutine = null;
    }
}
#endregion

#region TargetExample
/// <summary>
/// Example usage: Target demonstrates how a character might react when its hurtbox is hit.
/// It subscribes to the Hurtbox's OnHit event.
/// </summary>
public class TargetExample : MonoBehaviour
{
    [Tooltip("Reference to the main Hurtbox component of this character.")]
    [SerializeField] private Hurtbox _mainHurtbox;

    [Tooltip("Rigidbody component for applying knockback.")]
    [SerializeField] private Rigidbody _rigidbody;

    [Tooltip("Current health of the target.")]
    [SerializeField] private int _currentHealth = 100;

    private void Start()
    {
        if (_mainHurtbox == null)
        {
            // Try to find the first Hurtbox child if not assigned
            _mainHurtbox = GetComponentInChildren<Hurtbox>();
            if (_mainHurtbox == null)
            {
                Debug.LogError("TargetExample requires a main Hurtbox component!", this);
                enabled = false;
                return;
            }
        }
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogWarning("TargetExample has no Rigidbody. Knockback will not be applied.", this);
            }
        }

        // Subscribe to the OnHit event of the main Hurtbox
        _mainHurtbox.OnHit += TakeDamage;
        Debug.Log($"{name}'s Hurtbox is ready to receive hits. Current Health: {_currentHealth}");
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks if the target is destroyed
        if (_mainHurtbox != null)
        {
            _mainHurtbox.OnHit -= TakeDamage;
        }
    }

    /// <summary>
    /// This method is called when the _mainHurtbox receives a hit.
    /// It processes the HitData and applies effects like damage and knockback.
    /// </summary>
    /// <param name="hitData">The data describing the hit.</param>
    private void TakeDamage(HitData hitData)
    {
        _currentHealth -= hitData.damageAmount;
        Debug.Log($"<color=orange>{name} took {hitData.damageAmount} damage from {hitData.attackerGameObject.name}'s {hitData.hitType} attack!</color> " +
                  $"Remaining Health: {_currentHealth}. Hit Stun: {hitData.hitStunDuration}s.");

        // Apply knockback if a Rigidbody is present
        if (_rigidbody != null && hitData.knockbackForce > 0)
        {
            // Calculate knockback direction: away from the attacker
            Vector3 knockbackDirection = (transform.position - hitData.attackerGameObject.transform.position).normalized;
            // Optionally, make it slightly upwards for a more game-like feel
            knockbackDirection.y = Mathf.Max(knockbackDirection.y, 0.5f); // Ensure some upward force
            knockbackDirection.Normalize();

            _rigidbody.AddForce(knockbackDirection * hitData.knockbackForce, ForceMode.Impulse);
            Debug.Log($"Applied knockback to {name} in direction {knockbackDirection} with force {hitData.knockbackForce}.");
        }

        // Simulate hit stun (e.g., pause character movement, play stun animation)
        // For this example, we'll just log it. In a real game, you'd disable input,
        // change animation state, etc.
        StartCoroutine(HandleHitStun(hitData.hitStunDuration));

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the hit stun duration.
    /// </summary>
    private System.Collections.IEnumerator HandleHitStun(float duration)
    {
        Debug.Log($"{name} is stunned for {duration} seconds.");
        // In a real game, you would set a flag like _isStunned = true;
        // and disable character controls here.
        yield return new WaitForSeconds(duration);
        // Then reset _isStunned = false; and re-enable controls.
        Debug.Log($"{name} recovered from stun.");
    }

    /// <summary>
    /// Handles the death of the character.
    /// </summary>
    private void Die()
    {
        Debug.Log($"<color=red>{name} has been defeated!</color>");
        // Implement death animation, disable game object, etc.
        Destroy(gameObject, 2f); // Destroy self after 2 seconds for demonstration
    }
}
#endregion
```