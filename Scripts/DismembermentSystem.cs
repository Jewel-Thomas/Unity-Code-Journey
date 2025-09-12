// Unity Design Pattern Example: DismembermentSystem
// This script demonstrates the DismembermentSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'DismembermentSystem' is a design pattern in game development (often seen in Unity) that manages the state and visual representation of a character's body parts, allowing them to be "severed" or detached during gameplay, typically in response to damage. It encapsulates the logic for visually and physically separating a part from the main character model.

This pattern combines elements of the **Manager Pattern** (a central system managing parts) and the **State Pattern** (each part having an "attached" or "severed" state).

Here's a complete, practical C# Unity example:

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for potential future Linq queries, but currently dictionary lookup is more direct.

/// <summary>
/// Defines a single body part that can be severed from a character.
/// Marked [System.Serializable] so it can be configured directly in the Unity Inspector.
/// </summary>
[System.Serializable]
public class DismemberablePart
{
    [Tooltip("A unique identifier for this body part (e.g., 'LeftArm', 'Head', 'Torso'). This name will be used by other systems to target the part.")]
    public string partName;

    [Tooltip("The GameObject in the main character's hierarchy that represents this part when it is ATTACHED. " +
             "When the part is severed, this GameObject will be disabled to hide the attached version.")]
    public GameObject attachedRoot;

    [Tooltip("The Prefab that will be instantiated when this part is SEVERED. " +
             "This prefab MUST contain its own MeshFilter, MeshRenderer, Collider, and Rigidbody " +
             "to allow it to visually appear detached and interact with physics.")]
    public GameObject severedPrefab;

    [Tooltip("An offset from the 'attachedRoot's world position where the 'severedPrefab' should be instantiated. " +
             "Adjust this to make the cut point look natural.")]
    public Vector3 severPointOffset = Vector3.zero;

    [Tooltip("An offset in Euler angles (X, Y, Z) for the rotation of the 'severedPrefab' upon instantiation. " +
             "Use this to orient the detached part correctly relative to the cut.")]
    public Vector3 severRotationOffset = Vector3.zero;

    [Tooltip("Runtime flag: Is this part currently severed? Hidden in Inspector as it's managed by the system.")]
    [HideInInspector]
    public bool isSevered = false;

    // Internal reference to the instantiated severed part, allowing for management (e.g., despawning, reattachment).
    [HideInInspector]
    public GameObject currentSeveredInstance;
}

/// <summary>
/// The central manager for character dismemberment.
/// This script handles the logic for severing body parts, including visual changes,
/// physics application, and playing associated effects.
/// It provides a clean API for other systems (like combat or damage) to interact with dismemberment.
/// </summary>
[AddComponentMenu("Character/Dismemberment System")] // Adds a convenient menu entry in Unity's Component menu.
public class DismembermentSystem : MonoBehaviour
{
    [Header("Dismemberment Configuration")]
    [Tooltip("A list of all parts on this character that can potentially be severed.")]
    [SerializeField]
    private List<DismemberablePart> severableParts = new List<DismemberablePart>();

    [Tooltip("Optional: A particle system prefab (e.g., a blood splatter, sparks) to instantiate at the sever point.")]
    public GameObject severEffectPrefab;

    [Tooltip("Optional: An audio clip to play when a part is severed.")]
    public AudioClip severSound;

    private AudioSource _audioSource; // Used to play the sever sound.
    private Dictionary<string, DismemberablePart> _partLookup; // For efficient lookup of parts by name.

    /// <summary>
    /// Initializes the DismembermentSystem.
    /// Creates a dictionary for quick lookup of severable parts and sets up the AudioSource.
    /// </summary>
    void Awake()
    {
        _partLookup = new Dictionary<string, DismemberablePart>();
        foreach (var part in severableParts)
        {
            if (_partLookup.ContainsKey(part.partName))
            {
                Debug.LogWarning($"DismembermentSystem on {gameObject.name} has a duplicate part name: '{part.partName}'. " +
                                 "Only the first instance will be used. Ensure all part names are unique.", this);
                continue;
            }
            _partLookup.Add(part.partName, part);
            // Ensure runtime state is reset on Awake (important for editor play mode resets or object pooling scenarios).
            part.isSevered = false;
        }

        // Get or add an AudioSource component to play sounds.
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.spatialBlend = 1.0f; // Make it a 3D sound for immersive effects.
        _audioSource.playOnAwake = false; // Prevent it from playing automatically.
    }

    /// <summary>
    /// Public API for other systems to check if a specific part can be severed.
    /// </summary>
    /// <param name="partName">The unique name of the part to check (e.g., "LeftArm").</param>
    /// <returns>True if the part exists and is not currently severed, false otherwise.</returns>
    public bool CanSeverPart(string partName)
    {
        if (_partLookup.TryGetValue(partName, out DismemberablePart part))
        {
            return !part.isSevered;
        }
        return false; // Part not found or already severed.
    }

    /// <summary>
    /// Initiates the severing process for a specified body part. This is the primary method
    /// to call from a combat, damage, or interaction system.
    /// </summary>
    /// <param name="partName">The unique name of the part to sever (e.g., "LeftArm").</param>
    /// <param name="severForce">The physics force (as an impulse) to apply to the newly detached part. This makes it fly off.</param>
    /// <param name="hitPoint">Optional: The world position where the severing occurred. Used for spawning effects. If null, the part's configured offset is used.</param>
    public void SeverPart(string partName, Vector3 severForce, Vector3? hitPoint = null)
    {
        // 1. Validate the request and check the part's current state.
        if (!_partLookup.TryGetValue(partName, out DismemberablePart part))
        {
            Debug.LogWarning($"Attempted to sever unknown part: '{partName}' on {gameObject.name}. " +
                             "Check the DismembermentSystem configuration.", this);
            return;
        }

        if (part.isSevered)
        {
            Debug.LogWarning($"Part '{partName}' on {gameObject.name} is already severed. Ignoring request.", this);
            return;
        }

        // --- Dismemberment System Pattern Logic ---
        Debug.Log($"Severing part: '{partName}' on {gameObject.name}!");

        // 2. Update the internal state: Mark the part as severed.
        part.isSevered = true;

        // 3. Handle the attached representation: Disable the original part on the main character model.
        if (part.attachedRoot != null)
        {
            part.attachedRoot.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"AttachedRoot for part '{partName}' is null on {gameObject.name}. " +
                             "The original part will not be hidden from the main model.", this);
        }

        // 4. Instantiate the severed representation: Spawn the detached part prefab.
        if (part.severedPrefab != null)
        {
            // Calculate the spawn position and rotation for the severed part.
            // It uses the attachedRoot's transform as a base, applying configured offsets.
            Vector3 spawnPosition;
            Quaternion spawnRotation;

            if (part.attachedRoot != null)
            {
                spawnPosition = part.attachedRoot.transform.position + part.attachedRoot.transform.TransformDirection(part.severPointOffset);
                spawnRotation = part.attachedRoot.transform.rotation * Quaternion.Euler(part.severRotationOffset);
            }
            else // Fallback if no attachedRoot reference is provided.
            {
                spawnPosition = transform.position + transform.TransformDirection(part.severPointOffset);
                spawnRotation = transform.rotation * Quaternion.Euler(part.severRotationOffset);
            }

            // The effect (e.g., blood) might want to spawn precisely at the hit point,
            // while the actual severed piece needs to originate from the character's body.
            Vector3 effectPosition = hitPoint ?? spawnPosition; // Use hitPoint for effects if available.

            // Create the new, detached GameObject.
            GameObject severedInstance = Instantiate(part.severedPrefab, spawnPosition, spawnRotation);
            part.currentSeveredInstance = severedInstance; // Store reference for potential later management (e.g., despawn).

            // 5. Apply physics: Make the severed part fly off with an impulse.
            Rigidbody rb = severedInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(severForce, ForceMode.Impulse);
                // Add some random rotational force for more natural-looking detachment.
                rb.AddTorque(Random.insideUnitSphere * Random.Range(3f, 7f), ForceMode.Impulse);
            }
            else
            {
                Debug.LogWarning($"Severed prefab for '{partName}' ('{part.severedPrefab.name}') has no Rigidbody. " +
                                 "No physics force will be applied to it.", this);
            }

            // 6. Play optional visual effects (e.g., blood particles) and sound effects.
            if (severEffectPrefab != null)
            {
                GameObject effect = Instantiate(severEffectPrefab, effectPosition, Quaternion.identity);
                // Destroy the effect after a short duration to clean up the scene.
                Destroy(effect, 3f);
            }

            if (severSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(severSound);
            }
        }
        else
        {
            Debug.LogWarning($"Severed prefab for part '{partName}' is null on {gameObject.name}. " +
                             "No physical detached part will be spawned.", this);
        }

        // Optional: Trigger any custom events or callbacks here for other systems to react.
        // For example: OnPartSevered?.Invoke(partName); if you implement a UnityEvent.
    }

    /// <summary>
    /// Resets the dismemberment state of all parts managed by this system.
    /// This will re-enable the attached models and destroy any instantiated severed parts.
    /// Useful for character respawn or scene resets.
    /// </summary>
    public void ResetDismembermentState()
    {
        Debug.Log($"Resetting dismemberment state for {gameObject.name}...");
        foreach (var part in severableParts)
        {
            if (part.isSevered)
            {
                Debug.Log($"Reattaching '{part.partName}'.");
                part.isSevered = false; // Reset the internal state.

                // Re-enable the original attached model.
                if (part.attachedRoot != null)
                {
                    part.attachedRoot.SetActive(true);
                }

                // Destroy the previously instantiated severed part.
                if (part.currentSeveredInstance != null)
                {
                    Destroy(part.currentSeveredInstance);
                    part.currentSeveredInstance = null;
                }
            }
        }
    }
}

/*
 * --- EXAMPLE USAGE IN COMMENTS ---
 *
 * This section provides instructions on how to set up and use the DismembermentSystem
 * in a real Unity project, along with a hypothetical combat script demonstrating its API.
 *
 * =====================================================================================
 * STEP-BY-STEP INTEGRATION GUIDE
 * =====================================================================================
 *
 * 1.  **Prepare your Character Model:**
 *     *   Your character's 3D model should be structured so that each severable body part
 *         (e.g., 'Head', 'LeftArm', 'RightLeg', 'Torso') exists as a *separate GameObject*.
 *         These GameObjects will typically be children of bones in a skeletal animation rig,
 *         or simply direct children of your character's root.
 *     *   Each of these "attached" GameObjects should have its own MeshFilter and MeshRenderer
 *         (or be a specific sub-mesh that can be toggled). For this example, we assume `SetActive(false)`
 *         on the `attachedRoot` effectively hides the part.
 *     *   Example Hierarchy:
 *         `CharacterRoot`
 *         `├── Body` (GameObject with MeshFilter/Renderer for torso)
 *         `├── LeftArm` (GameObject with MeshFilter/Renderer for left arm)
 *         `├── RightLeg` (GameObject with MeshFilter/Renderer for right leg)
 *         `└── Head` (GameObject with MeshFilter/Renderer for head)
 *
 * 2.  **Create Severed Prefabs:**
 *     *   For *each* severable part, create a *separate Prefab* in your Project window.
 *     *   This prefab should represent the *detached* version of the part.
 *     *   It *must* contain:
 *         *   A `MeshFilter` and `MeshRenderer` (to make it visible, often with a "cut" texture).
 *         *   A `Collider` (e.g., `BoxCollider`, `CapsuleCollider`, `MeshCollider`) to enable physics interaction.
 *         *   A `Rigidbody` (so it can be affected by gravity and forces).
 *     *   Example: A "Severed_LeftArm_Prefab" would be a 3D model of a left arm stump, with a collider and Rigidbody.
 *
 * 3.  **Attach `DismembermentSystem` to your Character:**
 *     *   Select your character's root GameObject in the Hierarchy.
 *     *   Add the `DismembermentSystem` component to it (either drag the script or use "Add Component").
 *
 * 4.  **Configure `DismembermentSystem` in the Inspector:**
 *     *   In the Inspector, locate the `Dismemberment System` component.
 *     *   **Severable Parts:** Expand this list. For each part you want to make severable:
 *         *   Click the '+' button to add a new entry.
 *         *   **Part Name:** Enter a unique string identifier (e.g., "LeftArm", "Head"). This name is crucial for other scripts to reference the part.
 *         *   **Attached Root:** Drag the corresponding GameObject from your character model (e.g., the `LeftArm` GameObject you prepared) into this slot.
 *         *   **Severed Prefab:** Drag the corresponding severed prefab you created (e.g., `Severed_LeftArm_Prefab`) into this slot.
 *         *   **Sever Point Offset:** Adjust this `Vector3` value. This specifies *where* the `severedPrefab` will appear relative to the `attachedRoot`'s pivot. Experiment to make the cut look seamless.
 *         *   **Sever Rotation Offset:** Adjust this `Vector3` (Euler angles) to correctly orient the `severedPrefab` upon spawning.
 *     *   **Sever Effect Prefab (Optional):** Drag a particle system prefab (like a blood effect or impact effect) here.
 *     *   **Sever Sound (Optional):** Drag an AudioClip for a severing sound effect here.
 *
 * 5.  **Integrate with your Combat/Damage System:**
 *     *   Your combat system, weapon script, or any script that deals damage will need a reference to the target character's `DismembermentSystem`.
 *     *   When damage is applied and you determine a part *should* be severed (e.g., critical hit, specific attack type, damage threshold met):
 *
 *     ```csharp
 *     // ExampleCombatSystem.cs: An example script that would be on a weapon, bullet, or damage handler.
 *     // This script demonstrates how to interact with the DismembermentSystem.
 *     public class ExampleCombatSystem : MonoBehaviour
 *     {
 *         [Tooltip("Reference to the DismembermentSystem on the character we are attacking.")]
 *         public DismembermentSystem targetDismembermentSystem;
 *
 *         [Tooltip("Minimum damage required to trigger a dismemberment attempt.")]
 *         public float dismembermentDamageThreshold = 30f;
 *
 *         [Tooltip("Multiplier for the force applied to the severed part.")]
 *         public float severForceMultiplier = 50f;
 *
 *         // Call this method from your damage application logic (e.g., when a projectile hits a body part).
 *         public void ApplyDamageToBodyPart(string partName, float damageAmount, Vector3 hitPoint, Vector3 forceDirection)
 *         {
 *             Debug.Log($"Applying {damageAmount} damage to {partName} at {hitPoint.ToString()}.");
 *
 *             // --- Placeholder for actual health/damage calculation ---
 *             // In a real game, you would also apply damage to the character's health,
 *             // check for death, or other status effects here.
 *             // For this example, we focus on the dismemberment aspect.
 *             // --------------------------------------------------------
 *
 *             // Check if the damage is sufficient to attempt dismemberment.
 *             if (damageAmount >= dismembermentDamageThreshold)
 *             {
 *                 // Check if the target has a DismembermentSystem and if the specific part can currently be severed.
 *                 if (targetDismembermentSystem != null && targetDismembermentSystem.CanSeverPart(partName))
 *                 {
 *                     Debug.Log($"Dismemberment criteria met for '{partName}'! Attempting to sever...");
 *                     // Call the public API method on the target's DismembermentSystem.
 *                     targetDismembermentSystem.SeverPart(partName, forceDirection * severForceMultiplier, hitPoint);
 *                 }
 *                 else if (targetDismembermentSystem != null)
 *                 {
 *                     Debug.Log($"Part '{partName}' on target cannot be severed (either already severed or not configured).");
 *                 }
 *                 else
 *                 {
 *                     Debug.LogWarning("No DismembermentSystem found or assigned on the target character.");
 *                 }
 *             }
 *             else
 *             {
 *                 Debug.Log($"Damage ({damageAmount}) was not enough to trigger dismemberment (threshold: {dismembermentDamageThreshold}).");
 *             }
 *         }
 *
 *         // --- Example of how you might trigger damage on a specific part via mouse click ---
 *         void Update()
 *         {
 *             if (Input.GetMouseButtonDown(0)) // Left mouse button click
 *             {
 *                 Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
 *                 RaycastHit hit;
 *
 *                 // Perform a raycast to detect what was hit.
 *                 if (Physics.Raycast(ray, out hit, 100f))
 *                 {
 *                     // Attempt to find a DismembermentSystem on the hit object's hierarchy.
 *                     // This assumes the collider is on a body part or a child of the character's root.
 *                     DismembermentSystem hitSystem = hit.collider.GetComponentInParent<DismembermentSystem>();
 *
 *                     if (hitSystem != null)
 *                     {
 *                         targetDismembermentSystem = hitSystem; // Set the found system as our target.
 *
 *                         // The name of the hit GameObject is assumed to be the 'partName'.
 *                         // In a more robust system, you might use tags, a specific script on each part,
 *                         // or a lookup table to map collider names to part names.
 *                         string hitPartName = hit.collider.gameObject.name;
 *
 *                         // Calculate a force direction (e.g., away from the camera or based on impact normal).
 *                         Vector3 forceDir = (hit.point - Camera.main.transform.position).normalized;
 *
 *                         // Apply damage, potentially triggering dismemberment.
 *                         ApplyDamageToBodyPart(hitPartName, Random.Range(20f, 60f), hit.point, forceDir);
 *                     }
 *                     else
 *                     {
 *                         Debug.Log("Hit object or its parent does not have a DismembermentSystem: " + hit.collider.gameObject.name);
 *                     }
 *                 }
 *             }
 *
 *             if (Input.GetKeyDown(KeyCode.R)) // Press 'R' to reset the character's dismemberment state
 *             {
 *                 if (targetDismembermentSystem != null)
 *                 {
 *                     targetDismembermentSystem.ResetDismembermentState();
 *                 }
 *                 else
 *                 {
 *                     Debug.Log("No target DismembermentSystem to reset.");
 *                 }
 *             }
 *         }
 *     }
 *     ```
 *
 * =====================================================================================
 * DESIGN PATTERN EXPLANATION: The DismembermentSystem
 * =====================================================================================
 *
 * The `DismembermentSystem` pattern, as implemented here, combines principles from the
 * **Manager Pattern** and the **State Pattern**.
 *
 * 1.  **Manager Pattern Aspect:**
 *     *   **Centralized Control:** The `DismembermentSystem` script acts as a single,
 *         authoritative point for managing all severable body parts on a character.
 *         Instead of each body part managing its own detachment logic, the central
 *         system orchestrates the process.
 *     *   **Public API:** It provides clear, public methods (`SeverPart`, `CanSeverPart`,
 *         `ResetDismembermentState`) that other game systems (like a combat or weapon system)
 *         can call. This exposes only what's necessary, hiding the complex internal
 *         details of how dismemberment occurs.
 *     *   **Resource Management:** It manages the instantiation of severed prefabs
 *         and effects, and can track these instances for later cleanup or reattachment.
 *
 * 2.  **State Pattern Aspect:**
 *     *   **`DismemberablePart` as State Object:** Each `DismemberablePart` class
 *         represents a specific body part and holds its state (`isSevered`). It also
 *         stores the necessary references (attached model, severed prefab) to transition
 *         between states.
 *     *   **State Transition Logic:** The `SeverPart` method defines the specific steps
 *         to transition a part from an "attached" state to a "severed" state:
 *         1.  Update internal `isSevered` flag.
 *         2.  Disable the attached model (`attachedRoot.SetActive(false)`).
 *         3.  Instantiate the severed model (`severedPrefab`).
 *         4.  Apply physics (`Rigidbody.AddForce`).
 *         5.  Play visual/audio effects.
 *     *   The `ResetDismembermentState` method handles the inverse transition.
 *
 * **Benefits of this Pattern:**
 *
 * *   **Encapsulation:** All the intricate logic for *how* a part is severed (disabling meshes,
 *     instantiating new prefabs, applying physics, playing effects) is contained within
 *     the `DismembermentSystem`. External systems only need to request *what* part to sever.
 * *   **Modularity:** The dismemberment logic is decoupled from core combat, health,
 *     and animation systems. This makes each system easier to develop, test, and maintain
 *     independently.
 * *   **Flexibility:** Easily add, remove, or modify severable parts by simply adjusting
 *     the `severableParts` list in the Inspector. Different severed prefabs, effects,
 *     and offsets can be configured per part.
 * *   **Reusability:** The `DismembermentSystem` script can be reused across various
 *     character types (humanoids, monsters, robots), each with its own configuration.
 * *   **Maintainability:** If the core mechanism of dismemberment needs to change
 *     (e.g., new physics parameters, different effect handling), modifications are
 *     centralized in one script.
 * *   **Clarity:** The pattern clearly defines the responsibilities: the `DismembermentSystem`
 *     manages the parts, and external systems simply inform it when a part should be severed.
 *
 * **Considerations for Production Use:**
 *
 * *   **Performance (Object Pooling):** For games with frequent dismemberment (e.g., zombie hordes),
 *     instantiating and destroying severed parts and effects can become a performance bottleneck.
 *     Implement an **Object Pool** for `severedPrefab`s and `severEffectPrefab`s to reuse objects
 *     instead of constantly creating new ones.
 * *   **Character Model Complexity:** This example assumes your character model has distinct
 *     GameObjects for severable parts. For characters using a single `SkinnedMeshRenderer`,
 *     disabling/hiding parts becomes more complex (e.g., modifying bone hierarchies,
 *     using shader clipping techniques, or replacing the entire skinned mesh).
 * *   **Animation Integration:** When an `attachedRoot` GameObject is disabled, any
 *     animations directly affecting it will stop. Consider how this interacts with
 *     your animation system (e.g., blending to a "missing limb" animation state).
 * *   **Networked Multiplayer:** In a multiplayer game, dismemberment events would need
 *     to be synchronized across the network to ensure all clients see the same state changes.
 * *   **Optimization for Detached Parts:** Severed parts often don't need to exist forever.
 *     Implement a system to despawn or destroy `currentSeveredInstance`s after a certain
 *     time or distance from the player to keep scene complexity low.
 */
```