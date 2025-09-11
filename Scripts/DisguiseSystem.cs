// Unity Design Pattern Example: DisguiseSystem
// This script demonstrates the DisguiseSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The DisguiseSystem design pattern, while not one of the original Gang of Four patterns, is a common and practical pattern in game development. It allows a character or entity to temporarily change its appearance and/or behavioral attributes, often to interact with the game world or NPCs in a different way.

This Unity example demonstrates a DisguiseSystem where a player character can apply different disguises. Each disguise defines visual changes (material, mesh) and behavioral flags. Other game entities (like NPCs) can query the player's DisguiseSystem to react accordingly.

---

### Understanding the DisguiseSystem Pattern

1.  **`DisguiseProfile` (ScriptableObject):**
    *   **Purpose:** Encapsulates all the data related to a single disguise. This includes visual assets (material, mesh), a name, description, and crucial behavioral flags.
    *   **Benefits:** Allows designers to create and configure new disguises easily as assets in the Unity Editor without touching code. It promotes data-driven design.

2.  **`DisguiseSystem` (MonoBehaviour):**
    *   **Purpose:** The central component attached to the entity (e.g., player character) that can be disguised. It manages the current disguise state.
    *   **Responsibilities:**
        *   Stores the entity's original state (material, mesh) to revert to.
        *   Applies the visual and behavioral changes defined by a `DisguiseProfile`.
        *   Removes the current disguise, reverting to the original state.
        *   Provides methods for other systems to query the current disguise (e.g., `IsDisguised()`, `HasBehaviorFlag()`).
        *   Fires events (`onDisguiseChanged`) to notify other parts of the game when a disguise is applied or removed, promoting loose coupling.

3.  **Example Interactors (`DisguiseActivator`, `NPC_AI`):**
    *   **Purpose:** Demonstrate how to use the `DisguiseSystem`.
    *   `DisguiseActivator`: A simple component that allows a player to press a key to apply or remove a specific disguise.
    *   `NPC_AI`: An example of an NPC that queries the player's `DisguiseSystem` to determine how to react (e.g., allow passage, greet as a friend, or challenge as an intruder) based on the player's active disguise and its behavioral flags.

---

### Key Principles Demonstrated

*   **Separation of Concerns:** Disguise data (`DisguiseProfile`) is separate from the logic that applies/manages it (`DisguiseSystem`), and separate from the logic that reacts to it (`NPC_AI`).
*   **Data-Driven Design:** `DisguiseProfile` uses ScriptableObjects, allowing game designers to define new disguises entirely within the Unity Editor.
*   **Loose Coupling:** The `onDisguiseChanged` event in `DisguiseSystem` allows other components to react to disguise changes without needing direct references to the `DisguiseActivator` or specific disguise types. The `NPC_AI` queries the `DisguiseSystem` via its public interface, not its internal implementation.
*   **Extensibility:** New disguises can be added simply by creating new `DisguiseProfile` assets. More complex effects (e.g., particle systems, audio cues, temporary ability changes) could be added to `DisguiseProfile` and handled by `DisguiseSystem` without changing existing code significantly.

---

### Complete Unity C# Scripts

Below are the scripts required. To use them, create a new C# script file for each class (e.g., `DisguiseProfile.cs`, `DisguiseSystem.cs`, `DisguiseActivator.cs`, `NPC_AI.cs`) and copy the respective code into them.

**1. `DisguiseProfile.cs`**
*(ScriptableObject that defines the data for a single disguise)*

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DisguiseProfile is a ScriptableObject that defines all the data for a single disguise.
/// This allows designers to create and configure different disguises as assets in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewDisguiseProfile", menuName = "Disguise System/Disguise Profile")]
public class DisguiseProfile : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("The name of this disguise (e.g., 'Guard Uniform', 'Civilian Clothes').")]
    public string disguiseName = "Generic Disguise";
    [TextArea]
    [Tooltip("A short description of this disguise.")]
    public string description = "A basic disguise with no special attributes.";

    [Header("Visual Changes")]
    [Tooltip("The material to apply to the character's renderer when this disguise is active.")]
    public Material disguiseMaterial;
    [Tooltip("Optional: A specific mesh to apply to the character's renderer (e.g., for a different hat or model part).")]
    public Mesh disguiseMesh;

    [Header("Behavioral Modifiers")]
    [Tooltip("Flags that describe special behaviors or permissions granted by this disguise (e.g., 'CanEnterRestrictedArea', 'FriendlyToGuards').")]
    public List<string> behaviorFlags = new List<string>();

    [Header("UI / Overhead Display")]
    [Tooltip("Optional: Text to display above the character's head when this disguise is active (e.g., 'GUARD', 'JANITOR').")]
    public string overheadText = "";

    // You can extend this further with:
    // public AudioClip disguiseSound;
    // public GameObject particleEffectPrefab;
    // public float movementSpeedModifier;
    // ... any other game-specific attributes for a disguise.
}
```

**2. `DisguiseSystem.cs`**
*(The core MonoBehaviour that manages applying and removing disguises)*

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<string> if you use it directly
using TMPro; // Assuming TextMeshPro for overhead display; ensures proper using statement

/// <summary>
/// DisguiseSystem is the central component that manages a character's active disguise.
/// It handles applying and removing visual changes and provides an interface for other
/// game systems to query the current disguise's properties and behavioral flags.
/// </summary>
public class DisguiseSystem : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The SkinnedMeshRenderer (or MeshRenderer) component whose material and mesh will be swapped.")]
    public SkinnedMeshRenderer characterRenderer; // Can be MeshRenderer too

    [Tooltip("Optional: A TextMeshProUGUI or Text component to display text above the character's head.")]
    public TextMeshPro overheadDisplayName;

    // Private fields to store the character's original state
    private Material _originalMaterial;
    private Mesh _originalMesh; // Stores the original mesh of the SkinnedMeshRenderer
    private DisguiseProfile _activeDisguiseProfile;

    // Event for other systems to subscribe to when the disguise state changes
    // This allows for loose coupling: other systems don't need to know *how*
    // the disguise was changed, just *that* it changed.
    public delegate void OnDisguiseChanged(DisguiseProfile newDisguise, DisguiseProfile oldDisguise);
    public event OnDisguiseChanged onDisguiseChanged;

    private void Awake()
    {
        // Ensure characterRenderer is assigned, try to get it from children if not
        if (characterRenderer == null)
        {
            characterRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (characterRenderer == null)
            {
                Debug.LogError("DisguiseSystem requires a SkinnedMeshRenderer or MeshRenderer component in its children.", this);
                enabled = false; // Disable component if no renderer found
                return;
            }
        }

        // Store the original material and mesh to revert to later
        _originalMaterial = characterRenderer.sharedMaterial;
        _originalMesh = characterRenderer.sharedMesh;

        // Initialize overhead display (if present)
        if (overheadDisplayName != null)
        {
            overheadDisplayName.text = "";
            overheadDisplayName.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Applies a new disguise to the character based on the provided DisguiseProfile.
    /// If a disguise is already active, it will be removed before the new one is applied.
    /// </summary>
    /// <param name="profile">The DisguiseProfile asset containing the disguise data.</param>
    public void ApplyDisguise(DisguiseProfile profile)
    {
        if (profile == null)
        {
            Debug.LogWarning("DisguiseSystem: Attempted to apply a null DisguiseProfile. Removing current disguise instead.", this);
            RemoveDisguise();
            return;
        }

        // Store the current disguise before changing, for the event callback
        DisguiseProfile oldDisguise = _activeDisguiseProfile;

        // Apply new visual changes
        if (profile.disguiseMaterial != null)
        {
            characterRenderer.sharedMaterial = profile.disguiseMaterial;
        }
        else
        {
            Debug.LogWarning($"DisguiseProfile '{profile.disguiseName}' has no disguiseMaterial specified. Material will not change.", this);
        }

        if (profile.disguiseMesh != null)
        {
            characterRenderer.sharedMesh = profile.disguiseMesh;
        }
        // If profile.disguiseMesh is null, we keep the current (or original) mesh.

        // Update overhead text
        if (overheadDisplayName != null)
        {
            if (!string.IsNullOrEmpty(profile.overheadText))
            {
                overheadDisplayName.text = profile.overheadText;
                overheadDisplayName.gameObject.SetActive(true);
            }
            else
            {
                // If no specific text for this disguise, hide it
                overheadDisplayName.text = "";
                overheadDisplayName.gameObject.SetActive(false);
            }
        }

        _activeDisguiseProfile = profile;
        Debug.Log($"DisguiseSystem: Applied disguise '{profile.disguiseName}'.", this);

        // Notify all subscribed listeners about the disguise change
        onDisguiseChanged?.Invoke(_activeDisguiseProfile, oldDisguise);
    }

    /// <summary>
    /// Removes the current disguise and reverts the character to their original state
    /// (original material, mesh, and clear overhead display).
    /// </summary>
    public void RemoveDisguise()
    {
        if (_activeDisguiseProfile == null)
        {
            Debug.Log("DisguiseSystem: No active disguise to remove.", this);
            return;
        }

        // Store the current disguise before changing, for the event callback
        DisguiseProfile oldDisguise = _activeDisguiseProfile;

        // Revert visual changes
        characterRenderer.sharedMaterial = _originalMaterial;
        characterRenderer.sharedMesh = _originalMesh; // Revert to original mesh

        // Clear and hide overhead text
        if (overheadDisplayName != null)
        {
            overheadDisplayName.text = "";
            overheadDisplayName.gameObject.SetActive(false);
        }

        Debug.Log($"DisguiseSystem: Removed disguise '{_activeDisguiseProfile.disguiseName}'. Reverted to original state.", this);
        _activeDisguiseProfile = null; // No disguise is active

        // Notify all subscribed listeners that the disguise has been removed (newDisguise is null)
        onDisguiseChanged?.Invoke(null, oldDisguise);
    }

    /// <summary>
    /// Checks if the character is currently disguised.
    /// </summary>
    /// <returns>True if a disguise is active, false otherwise.</returns>
    public bool IsDisguised()
    {
        return _activeDisguiseProfile != null;
    }

    /// <summary>
    /// Gets the name of the currently active disguise.
    /// </summary>
    /// <returns>The name of the active disguise, or "None" if not disguised.</returns>
    public string GetActiveDisguiseName()
    {
        return _activeDisguiseProfile != null ? _activeDisguiseProfile.disguiseName : "None";
    }

    /// <summary>
    /// Gets the currently active DisguiseProfile.
    /// </summary>
    /// <returns>The active DisguiseProfile, or null if no disguise is active.</returns>
    public DisguiseProfile GetActiveDisguiseProfile()
    {
        return _activeDisguiseProfile;
    }

    /// <summary>
    /// Checks if the current disguise grants a specific behavior flag.
    /// This is crucial for other systems (like NPC AI or environmental triggers)
    /// to react differently based on the player's disguise.
    /// </summary>
    /// <param name="flag">The behavior flag string to check for (e.g., "CanEnterRestrictedArea").</param>
    /// <returns>True if disguised and the active disguise's behaviorFlags list contains the specified flag, false otherwise.</returns>
    public bool HasBehaviorFlag(string flag)
    {
        return _activeDisguiseProfile != null && _activeDisguiseProfile.behaviorFlags.Contains(flag);
    }
}
```

**3. `DisguiseActivator.cs`**
*(Example component to trigger disguise changes with key presses)*

```csharp
using UnityEngine;

/// <summary>
/// DisguiseActivator is a simple example component that demonstrates how to
/// interact with the DisguiseSystem. It allows a player to apply a specific
/// disguise or remove the current disguise using keyboard inputs.
/// </summary>
public class DisguiseActivator : MonoBehaviour
{
    [Tooltip("Reference to the DisguiseSystem component on the player character.")]
    public DisguiseSystem targetDisguiseSystem;

    [Tooltip("The DisguiseProfile to apply when the activate key is pressed.")]
    public DisguiseProfile disguiseToApply;

    [Header("Key Bindings")]
    public KeyCode activateKey = KeyCode.G; // G for Disguise
    public KeyCode removeKey = KeyCode.H;   // H for Revert

    private void Update()
    {
        // Check if the activate key is pressed
        if (Input.GetKeyDown(activateKey))
        {
            if (targetDisguiseSystem != null && disguiseToApply != null)
            {
                // Apply the selected disguise
                targetDisguiseSystem.ApplyDisguise(disguiseToApply);
            }
            else
            {
                Debug.LogWarning("DisguiseActivator: Target DisguiseSystem or DisguiseProfile is not assigned. Cannot apply disguise.", this);
            }
        }

        // Check if the remove key is pressed
        if (Input.GetKeyDown(removeKey))
        {
            if (targetDisguiseSystem != null)
            {
                // Remove the current disguise
                targetDisguiseSystem.RemoveDisguise();
            }
            else
            {
                Debug.LogWarning("DisguiseActivator: Target DisguiseSystem is not assigned. Cannot remove disguise.", this);
            }
        }
    }
}
```

**4. `NPC_AI.cs`**
*(Example component for an NPC that reacts to the player's disguise)*

```csharp
using UnityEngine;
using TMPro; // Assuming TextMeshPro for NPC speech bubble

/// <summary>
/// NPC_AI is an example component that demonstrates how other game systems
/// (like AI) can react to the player's active disguise by querying the DisguiseSystem.
/// </summary>
public class NPC_AI : MonoBehaviour
{
    [Tooltip("Reference to the player's DisguiseSystem component.")]
    public DisguiseSystem playerDisguiseSystem;

    [Tooltip("The Transform of the player character, used for distance checks.")]
    public Transform playerTransform;

    [Header("Behavioral Flags for Reaction")]
    [Tooltip("Flag required for the NPC to be friendly (e.g., 'FriendlyToGuards').")]
    public string requiredFriendlyFlag = "FriendlyToGuards";
    [Tooltip("Flag required for the NPC to grant special access (e.g., 'CanEnterRestrictedArea').")]
    public string requiredAccessFlag = "CanEnterRestrictedArea";

    [Header("NPC Specifics")]
    [Tooltip("TextMeshPro component to display the NPC's speech bubble.")]
    public TextMeshPro npcSpeechBubble;

    private float _reactionTimer = 0f;
    private const float _reactionInterval = 2.5f; // Check player's disguise every X seconds
    private const float _reactionDistance = 10f;  // Distance within which NPC reacts

    private void Start()
    {
        if (playerDisguiseSystem == null)
        {
            Debug.LogWarning("NPC_AI: Player DisguiseSystem not assigned. This NPC won't react to player's disguise changes.", this);
        }
        if (playerTransform == null)
        {
            Debug.LogWarning("NPC_AI: Player Transform not assigned. This NPC won't be able to check player distance.", this);
        }

        // Initially hide the speech bubble
        if (npcSpeechBubble != null)
        {
            npcSpeechBubble.text = "";
            npcSpeechBubble.gameObject.SetActive(false);
        }

        // Optional: Subscribe to the player's disguise change event for immediate reactions
        // if (playerDisguiseSystem != null)
        // {
        //     playerDisguiseSystem.onDisguiseChanged += OnPlayerDisguiseChanged;
        // }
    }

    // Example of reacting immediately to a disguise change (alternative to polling)
    // private void OnPlayerDisguiseChanged(DisguiseProfile newDisguise, DisguiseProfile oldDisguise)
    // {
    //     if (Vector3.Distance(transform.position, playerTransform.position) <= _reactionDistance)
    //     {
    //         ReactToPlayer();
    //     }
    // }

    private void Update()
    {
        // Periodically check player's disguise
        _reactionTimer += Time.deltaTime;
        if (_reactionTimer >= _reactionInterval)
        {
            _reactionTimer = 0f;
            ReactToPlayer();
        }
    }

    /// <summary>
    /// NPC reacts to the player's current disguise and displays a message.
    /// </summary>
    private void ReactToPlayer()
    {
        if (playerDisguiseSystem == null || playerTransform == null) return;

        // Only react if the player is within a certain distance
        if (Vector3.Distance(transform.position, playerTransform.position) > _reactionDistance)
        {
            if (npcSpeechBubble != null)
            {
                npcSpeechBubble.gameObject.SetActive(false); // Hide bubble if player is far
            }
            return;
        }

        if (playerDisguiseSystem.IsDisguised())
        {
            // Check for specific behavioral flags from the active disguise
            if (playerDisguiseSystem.HasBehaviorFlag(requiredAccessFlag))
            {
                DisplaySpeech("Welcome, you may pass!", Color.green);
            }
            else if (playerDisguiseSystem.HasBehaviorFlag(requiredFriendlyFlag))
            {
                DisplaySpeech("Greetings, fellow comrade!", Color.blue);
            }
            else
            {
                DisplaySpeech("Who are you? I don't recognize that uniform...", Color.yellow);
            }
        }
        else
        {
            // Player is not disguised
            DisplaySpeech("Halt! Identify yourself!", Color.red);
        }
    }

    /// <summary>
    /// Helper method to display text in the NPC's speech bubble.
    /// </summary>
    private void DisplaySpeech(string message, Color color)
    {
        if (npcSpeechBubble != null)
        {
            npcSpeechBubble.gameObject.SetActive(true);
            npcSpeechBubble.text = message;
            npcSpeechBubble.color = color;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks if subscribed
        // if (playerDisguiseSystem != null)
        // {
        //     playerDisguiseSystem.onDisguiseChanged -= OnPlayerDisguiseChanged;
        // }
    }
}
```

---

### How to Implement and Test in Unity

1.  **Create Scripts:** Copy each of the four code blocks above into separate C# script files in your Unity project (e.g., `Assets/Scripts/DisguiseSystem/DisguiseProfile.cs`, etc.).
2.  **Import TextMeshPro Essentials:** If you haven't already, go to `Window > TextMeshPro > Import TMP Essential Resources`. This is needed for `TextMeshPro` components.
3.  **Create Disguise Profiles (Assets):**
    *   In your Project window, right-click and choose `Create > Disguise System > Disguise Profile`.
    *   Create at least two profiles, e.g., "GuardUniform" and "CivilianClothes".
    *   **For "GuardUniform":**
        *   Set `disguiseName` to "Guard Uniform".
        *   Create a new Material (e.g., "GuardMaterial", make it blue) and assign it to `disguiseMaterial`.
        *   Add a `behaviorFlag`: "FriendlyToGuards".
        *   Add another `behaviorFlag`: "CanEnterRestrictedArea".
        *   Set `overheadText` to "GUARD".
    *   **For "CivilianClothes":**
        *   Set `disguiseName` to "Civilian Clothes".
        *   Create a new Material (e.g., "CivilianMaterial", make it green) and assign it.
        *   No special `behaviorFlags`.
        *   Set `overheadText` to "CITIZEN".
4.  **Player Setup:**
    *   Create a 3D object for your player (e.g., a Capsule, or import a character model). Ensure it has a `SkinnedMeshRenderer` or `MeshRenderer` component.
    *   Add the `DisguiseSystem` component to your player GameObject.
    *   Drag the player's `SkinnedMeshRenderer` (or `MeshRenderer`) into the `Character Renderer` slot on the `DisguiseSystem` component.
    *   Create a `TextMeshPro - Text (Ugui)` or `TextMeshPro - Text` (if using 3D text) object as a child of your player, positioned slightly above its head. Name it "PlayerOverheadDisplay".
    *   Drag this "PlayerOverheadDisplay" `TextMeshPro` component to the `Overhead Display Name` slot on the `DisguiseSystem`.
    *   Add the `DisguiseActivator` component to your player.
    *   Drag the player's `DisguiseSystem` to the `Target Disguise System` slot.
    *   Drag one of your `DisguiseProfile` assets (e.g., "GuardUniform") to the `Disguise To Apply` slot.
5.  **NPC Setup:**
    *   Create another 3D object for an NPC (e.g., another Capsule). Position it a short distance from the player.
    *   Add the `NPC_AI` component to this NPC GameObject.
    *   Drag the **player's** `DisguiseSystem` component to the `Player Disguise System` slot on the NPC.
    *   Drag the **player's** `Transform` component to the `Player Transform` slot on the NPC.
    *   Create a `TextMeshPro - Text (Ugui)` or `TextMeshPro - Text` object as a child of your NPC, positioned slightly above its head. Name it "NPCSpeechBubble".
    *   Drag this "NPCSpeechBubble" `TextMeshPro` component to the `NPC Speech Bubble` slot on the `NPC_AI` component.
    *   Ensure the `Required Friendly Flag` and `Required Access Flag` on the `NPC_AI` match the flags you used in your `DisguiseProfile` (e.g., "FriendlyToGuards", "CanEnterRestrictedArea").
6.  **Run the Scene:**
    *   Play the scene.
    *   The NPC should initially challenge the player ("Halt! Identify yourself!").
    *   Press the `G` key (or your assigned `activateKey`) to apply the disguise. Observe the player's material change and the overhead text.
    *   The NPC should now react differently based on the applied disguise's `behaviorFlags` (e.g., "Greetings, fellow comrade!" or "Welcome, you may pass!").
    *   Press the `H` key (or your assigned `removeKey`) to remove the disguise. The player reverts to their original look, and the NPC will challenge them again.

This setup provides a robust and expandable DisguiseSystem, perfect for many game scenarios from stealth games to RPGs.