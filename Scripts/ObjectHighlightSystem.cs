// Unity Design Pattern Example: ObjectHighlightSystem
// This script demonstrates the ObjectHighlightSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **ObjectHighlightSystem** design pattern in Unity. It provides a centralized service for managing the visual highlighting of various game objects, abstracting away the specific highlighting technique (e.g., material swap, outline shader) from the objects themselves.

**Key Components of the Pattern:**

1.  **`HighlightStyle` Enum:** Defines different types of highlights (e.g., `DefaultOutline`, `InteractionGlow`).
2.  **`HighlightMaterialConfig` Struct:** A serializable struct to map `HighlightStyle` enums to actual Unity `Material` assets in the Inspector.
3.  **`ObjectHighlightSystem` (Singleton MonoBehaviour):**
    *   The central manager.
    *   Provides a public API (`Highlight`, `Unhighlight`, `ClearAllHighlights`).
    *   Stores references to original materials so they can be restored.
    *   Manages which objects are currently highlighted and with which style.
    *   Configurable in the Inspector to associate `HighlightStyle` with specific `Material` assets.
4.  **`HighlightableObject` (MonoBehaviour):**
    *   A component attached to game objects that can *be* highlighted.
    *   Acts as a "client" of the `ObjectHighlightSystem`.
    *   In this example, it uses `OnMouseEnter` and `OnMouseExit` to trigger highlighting based on mouse interaction.
    *   Can specify its preferred default highlight style.

---

### **1. HighlightStyle.cs**

This enum defines the various types of highlighting effects we might want to apply.

```csharp
// HighlightStyle.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines different types of highlight styles.
/// This enum can be expanded to include more specific styles
/// (e.g., "QuestTarget", "PuzzlePiece", "DamageWarning").
/// </summary>
public enum HighlightStyle
{
    None,            // No specific highlight style
    DefaultOutline,  // A standard outline highlight
    InteractionGlow, // A glow indicating an interactable object
    WarningFlash     // A flashing highlight for warnings
}

```

---

### **2. HighlightMaterialConfig.cs**

This struct allows us to easily configure the mapping between a `HighlightStyle` and a `Material` in the Unity Inspector.

```csharp
// HighlightMaterialConfig.cs
using System;
using UnityEngine;

/// <summary>
/// A serializable struct to map a HighlightStyle enum to a specific Material.
/// This allows for easy configuration in the Unity Inspector for the ObjectHighlightSystem.
/// </summary>
[Serializable]
public struct HighlightMaterialConfig
{
    public HighlightStyle style;
    public Material material;
}

```

---

### **3. ObjectHighlightSystem.cs**

This is the core of the pattern. It's a singleton responsible for managing all highlight states.

```csharp
// ObjectHighlightSystem.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The ObjectHighlightSystem is a centralized service that manages the visual highlighting
/// of GameObjects. It follows the Singleton pattern for easy global access.
///
/// This system abstracts away the details of *how* an object is highlighted
/// (e.g., by swapping materials, using a shader outline, adding a particle effect)
/// from the objects themselves. Objects simply request to be highlighted with a given style.
/// </summary>
public class ObjectHighlightSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    public static ObjectHighlightSystem Instance { get; private set; }

    // --- Configuration for Highlight Materials ---
    [Header("Highlight Material Configuration")]
    [Tooltip("List of highlight styles and their corresponding materials.")]
    [SerializeField]
    private HighlightMaterialConfig[] _highlightMaterialConfigs;

    // A private dictionary to quickly look up highlight materials by style.
    private Dictionary<HighlightStyle, Material> _styleMaterialMap = new Dictionary<HighlightStyle, Material>();

    // --- Internal State Tracking ---
    // Stores the original materials for each currently highlighted GameObject.
    // This allows the system to restore the object's original appearance when unhighlighted.
    // Dictionary: GameObject -> Array of its original materials (for multiple sub-meshes)
    private Dictionary<GameObject, Material[]> _highlightedObjectsData = new Dictionary<GameObject, Material[]>();

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ObjectHighlightSystem instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Populate the style-material map from the Inspector-configured array
        InitializeMaterialMap();
    }

    /// <summary>
    /// Initializes the internal dictionary mapping HighlightStyles to actual Material assets.
    /// This is done once on Awake.
    /// </summary>
    private void InitializeMaterialMap()
    {
        _styleMaterialMap.Clear();
        foreach (var config in _highlightMaterialConfigs)
        {
            if (config.material == null)
            {
                Debug.LogWarning($"HighlightMaterialConfig for style '{config.style}' has no material assigned. It will not be usable.", this);
                continue;
            }
            if (_styleMaterialMap.ContainsKey(config.style))
            {
                Debug.LogWarning($"Duplicate HighlightMaterialConfig for style '{config.style}' found. Using the last one defined.", this);
            }
            _styleMaterialMap[config.style] = config.material;
        }
    }

    // --- Public API for Highlighting ---

    /// <summary>
    /// Highlights a specific GameObject with the given style.
    /// If the object is already highlighted, its highlight style will be updated.
    /// </summary>
    /// <param name="targetObject">The GameObject to highlight.</param>
    /// <param name="style">The desired highlight style.</param>
    public void Highlight(GameObject targetObject, HighlightStyle style)
    {
        if (targetObject == null || style == HighlightStyle.None)
        {
            Debug.LogWarning("Attempted to highlight a null object or with 'None' style.", this);
            return;
        }

        // Try to get the Renderer component(s) from the target object
        Renderer objectRenderer = targetObject.GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogWarning($"GameObject '{targetObject.name}' does not have a Renderer component. Cannot highlight.", targetObject);
            return;
        }

        // Check if we have a material for the requested style
        if (!_styleMaterialMap.TryGetValue(style, out Material highlightMaterial))
        {
            Debug.LogWarning($"No highlight material configured for style '{style}'. Cannot highlight '{targetObject.name}'.", this);
            return;
        }

        // If the object is already highlighted, unhighlight it first to ensure correct material restoration.
        // This also handles updating the highlight if the style changes.
        if (_highlightedObjectsData.ContainsKey(targetObject))
        {
            Unhighlight(targetObject); // Restore original materials before applying new highlight
        }

        // Store the object's current materials before changing them
        // We use .materials to get a *copy* of the array, so we don't modify shared asset.
        // We store this copy to restore later.
        _highlightedObjectsData[targetObject] = objectRenderer.materials;

        // Apply the highlight material to all sub-meshes (if any)
        Material[] newMaterials = new Material[objectRenderer.materials.Length];
        for (int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = highlightMaterial;
        }
        objectRenderer.materials = newMaterials; // Assign the new materials array
    }

    /// <summary>
    /// Unhighlights a specific GameObject, restoring its original materials.
    /// </summary>
    /// <param name="targetObject">The GameObject to unhighlight.</param>
    public void Unhighlight(GameObject targetObject)
    {
        if (targetObject == null) return;

        // Check if the object is currently being tracked as highlighted
        if (_highlightedObjectsData.TryGetValue(targetObject, out Material[] originalMaterials))
        {
            Renderer objectRenderer = targetObject.GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                // Restore the original materials
                objectRenderer.materials = originalMaterials;
            }
            else
            {
                Debug.LogWarning($"GameObject '{targetObject.name}' no longer has a Renderer component. Could not restore materials.", targetObject);
            }

            // Remove the object from our tracking dictionary
            _highlightedObjectsData.Remove(targetObject);
        }
    }

    /// <summary>
    /// Unhighlights all GameObjects currently being tracked by the system.
    /// </summary>
    public void ClearAllHighlights()
    {
        // Create a temporary list of objects to unhighlight to avoid modifying
        // the dictionary while iterating over it.
        List<GameObject> objectsToUnhighlight = new List<GameObject>(_highlightedObjectsData.Keys);

        foreach (GameObject obj in objectsToUnhighlight)
        {
            Unhighlight(obj);
        }
        _highlightedObjectsData.Clear(); // Ensure the dictionary is empty
    }

    /// <summary>
    /// Checks if a given GameObject is currently highlighted by the system.
    /// </summary>
    /// <param name="targetObject">The GameObject to check.</param>
    /// <returns>True if the object is highlighted, false otherwise.</returns>
    public bool IsHighlighted(GameObject targetObject)
    {
        if (targetObject == null) return false;
        return _highlightedObjectsData.ContainsKey(targetObject);
    }
}
```

---

### **4. HighlightableObject.cs**

This component is attached to objects that the player can interact with and wants to highlight.

```csharp
// HighlightableObject.cs
using UnityEngine;

/// <summary>
/// This component makes a GameObject "highlightable" by the ObjectHighlightSystem.
/// It acts as a client to the highlighting service.
/// In this example, it demonstrates interaction via mouse hover.
/// </summary>
[RequireComponent(typeof(Renderer))] // Ensures the object has a Renderer to apply materials to
public class HighlightableObject : MonoBehaviour
{
    [Tooltip("The default style to use when this object is highlighted.")]
    [SerializeField]
    private HighlightStyle _defaultHighlightStyle = HighlightStyle.DefaultOutline;

    // Optional: Store a reference to the renderer for performance, though GetComponent is cached by Unity.
    // private Renderer _renderer;

    // private void Awake()
    // {
    //     _renderer = GetComponent<Renderer>();
    // }

    /// <summary>
    /// Called when the mouse cursor enters the collider of this GameObject.
    /// Requests the ObjectHighlightSystem to highlight this object.
    /// </summary>
    private void OnMouseEnter()
    {
        // Ensure the system exists before trying to use it
        if (ObjectHighlightSystem.Instance != null)
        {
            ObjectHighlightSystem.Instance.Highlight(gameObject, _defaultHighlightStyle);
            Debug.Log($"Mouse entered {gameObject.name}. Requesting highlight with style: {_defaultHighlightStyle}");
        }
        else
        {
            Debug.LogWarning("ObjectHighlightSystem.Instance is null. Cannot highlight.");
        }
    }

    /// <summary>
    /// Called when the mouse cursor exits the collider of this GameObject.
    /// Requests the ObjectHighlightSystem to unhighlight this object.
    /// </summary>
    private void OnMouseExit()
    {
        if (ObjectHighlightSystem.Instance != null)
        {
            ObjectHighlightSystem.Instance.Unhighlight(gameObject);
            Debug.Log($"Mouse exited {gameObject.name}. Requesting unhighlight.");
        }
        // No warning here, as it might exit after system is destroyed on app quit
    }

    /// <summary>
    /// Example of how another script could programmatically highlight this object.
    /// </summary>
    public void ProgrammaticHighlight(HighlightStyle style)
    {
        if (ObjectHighlightSystem.Instance != null)
        {
            ObjectHighlightSystem.Instance.Highlight(gameObject, style);
            Debug.Log($"Programmatic highlight for {gameObject.name} with style: {style}");
        }
    }

    /// <summary>
    /// Example of how another script could programmatically unhighlight this object.
    /// </summary>
    public void ProgrammaticUnhighlight()
    {
        if (ObjectHighlightSystem.Instance != null)
        {
            ObjectHighlightSystem.Instance.Unhighlight(gameObject);
            Debug.Log($"Programmatic unhighlight for {gameObject.name}.");
        }
    }
}
```

---

### **How to Use This in Unity:**

1.  **Create C# Scripts:**
    *   Create a new C# script named `HighlightStyle.cs` and paste its code.
    *   Create a new C# script named `HighlightMaterialConfig.cs` and paste its code.
    *   Create a new C# script named `ObjectHighlightSystem.cs` and paste its code.
    *   Create a new C# script named `HighlightableObject.cs` and paste its code.

2.  **Create Highlight Materials:**
    *   In your Unity Project window, right-click -> Create -> Material.
    *   Name them something descriptive, e.g., `Mat_Outline`, `Mat_Glow`, `Mat_Warning`.
    *   **For `Mat_Outline`:**
        *   Set its Shader to `Standard`.
        *   Set its Albedo color to a bright color (e.g., bright yellow, light blue).
        *   Set its Emissive color to the same bright color and increase the intensity slightly to make it glow a bit.
    *   **For `Mat_Glow`:**
        *   Set its Shader to `Standard`.
        *   Set its Albedo color to a different bright color (e.g., lime green, bright magenta).
        *   Set its Emissive color to the same bright color and increase the intensity even more for a strong glow.
    *   **For `Mat_Warning`:**
        *   Set its Shader to `Standard`.
        *   Set its Albedo color to a bright red.
        *   Set its Emissive color to bright red and increase intensity. (You could also animate the color with a separate script if you wanted a "flash" effect).

3.  **Create the `ObjectHighlightSystem` GameObject:**
    *   In your scene Hierarchy, right-click -> Create Empty. Name it `_ObjectHighlightSystem`.
    *   Drag and drop the `ObjectHighlightSystem.cs` script onto this new GameObject.
    *   In the Inspector for `_ObjectHighlightSystem`:
        *   You'll see a `Highlight Material Configs` array. Set its `Size` to 3 (or however many styles you have).
        *   For each element:
            *   Set `Style` to `DefaultOutline` and drag your `Mat_Outline` material into the `Material` slot.
            *   Set `Style` to `InteractionGlow` and drag your `Mat_Glow` material into the `Material` slot.
            *   Set `Style` to `WarningFlash` and drag your `Mat_Warning` material into the `Material` slot.

4.  **Make Objects Highlightable:**
    *   Create some 3D objects in your scene (e.g., Cube, Sphere, Cylinder).
    *   Ensure they have a `Collider` component (which Unity 3D objects have by default). This is necessary for `OnMouseEnter`/`OnMouseExit` to work.
    *   Attach the `HighlightableObject.cs` script to each of these objects.
    *   In the Inspector for each `HighlightableObject`:
        *   You can choose its `Default Highlight Style` (e.g., `DefaultOutline` for some, `InteractionGlow` for others).

5.  **Run the Scene:**
    *   Play your scene.
    *   Move your mouse cursor over the objects you've set up as highlightable. They should now change their material to the configured highlight material when hovered over, and revert to their original material when the mouse leaves.

---

### **Example Usage in Other Scripts (Programmatic Highlighting):**

You might have other game logic scripts that need to trigger highlights. Here's how they would interact with the `ObjectHighlightSystem`:

```csharp
// ExampleGameLogic.cs
using UnityEngine;

public class ExampleGameLogic : MonoBehaviour
{
    [Tooltip("Reference to an object that can be highlighted.")]
    [SerializeField]
    private GameObject _importantObject;

    private void Start()
    {
        // Programmatically highlight an object after a delay
        Invoke(nameof(HighlightImportantObject), 3f);

        // Clear all highlights after another delay
        Invoke(nameof(ClearAllHighlightsInScene), 6f);
    }

    private void Update()
    {
        // Example: Highlight an object when a specific key is pressed
        if (Input.GetKeyDown(KeyCode.H) && _importantObject != null)
        {
            if (ObjectHighlightSystem.Instance != null)
            {
                // Toggle highlight based on its current state
                if (ObjectHighlightSystem.Instance.IsHighlighted(_importantObject))
                {
                    ObjectHighlightSystem.Instance.Unhighlight(_importantObject);
                }
                else
                {
                    ObjectHighlightSystem.Instance.Highlight(_importantObject, HighlightStyle.WarningFlash);
                }
            }
        }
    }

    private void HighlightImportantObject()
    {
        if (_importantObject != null && ObjectHighlightSystem.Instance != null)
        {
            // Request a highlight with a specific style
            ObjectHighlightSystem.Instance.Highlight(_importantObject, HighlightStyle.InteractionGlow);
            Debug.Log("Programmatically highlighted important object.");
        }
    }

    private void ClearAllHighlightsInScene()
    {
        if (ObjectHighlightSystem.Instance != null)
        {
            ObjectHighlightSystem.Instance.ClearAllHighlights();
            Debug.Log("Programmatically cleared all active highlights.");
        }
    }
}
```

To use `ExampleGameLogic`:
1.  Create a new C# script `ExampleGameLogic.cs` and paste the code.
2.  Create an empty GameObject in your scene, name it `GameManager`, and attach `ExampleGameLogic.cs` to it.
3.  Drag one of your `HighlightableObject`s from the Hierarchy into the `Important Object` slot in the `GameManager`'s Inspector.
4.  Run the scene and observe the timed highlights and try pressing 'H'.

This complete setup provides a flexible, maintainable, and scalable way to manage object highlighting in your Unity projects.