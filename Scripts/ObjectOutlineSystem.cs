// Unity Design Pattern Example: ObjectOutlineSystem
// This script demonstrates the ObjectOutlineSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ObjectOutlineSystem' design pattern, while not one of the classic Gang of Four patterns, represents a common architecture in game development for managing visual highlights on objects. It typically involves a central system that controls outlining, individual components on objects that can be outlined, and client scripts that trigger these outlines.

This example implements the 'ObjectOutlineSystem' pattern in Unity by:
1.  **`OutlineManager` (Singleton):** A central point responsible for registering/unregistering outline-able objects and dispatching outline requests.
2.  **`OutlineObject` (Component):** Attached to game objects, it handles the actual visual outline effect by swapping or modifying materials. It registers itself with the `OutlineManager`.
3.  **`MouseOverOutline` (Client):** An example script that uses raycasting to determine which object should be outlined and communicates with the `OutlineManager`.

This setup provides a flexible and scalable way to manage object outlines in your Unity project.

---

### **How to Use This Example in Unity:**

1.  **Create C# Scripts:**
    *   Create three new C# scripts in your Unity project: `OutlineManager.cs`, `OutlineObject.cs`, and `MouseOverOutline.cs`.
    *   Copy and paste the code for each script into its respective file.

2.  **Create an Outline Material:**
    *   In your Unity Project window, right-click -> `Create` -> `Material`. Name it `OutlineMaterial`.
    *   Select `OutlineMaterial`. In the Inspector:
        *   Change its `Shader` to `Unlit/Color`.
        *   Set the `Color` to something distinct (e.g., bright pink, green, blue). This will be the base color for your outlines. *Note: For a more advanced outline effect (like a border around the object), you would use a custom shader designed for outlines. The Unlit/Color material serves as a simple placeholder to demonstrate the pattern's functionality.*

3.  **Set up the `OutlineManager`:**
    *   Create an empty GameObject in your scene (e.g., `GameObject` -> `Create Empty`). Name it `_OutlineSystem`.
    *   Attach the `OutlineManager.cs` script to this `_OutlineSystem` GameObject.

4.  **Make Objects Outline-able:**
    *   Select any 3D objects in your scene that you want to be outline-able (e.g., cubes, spheres, custom models).
    *   Attach the `OutlineObject.cs` script to each of these objects.
    *   For each `OutlineObject` component, drag and drop the `OutlineMaterial` you created earlier into the `Outline Material Prefab` slot in the Inspector.
    *   Ensure these objects have a `Collider` component (e.g., `Box Collider`, `Sphere Collider`, `Mesh Collider`) for raycasting to work.

5.  **Set up the `MouseOverOutline` Client:**
    *   Create an empty GameObject (e.g., `GameObject` -> `Create Empty`). Name it `OutlineInputHandler`.
    *   Attach the `MouseOverOutline.cs` script to this `OutlineInputHandler` GameObject.
    *   You can set the `Hover Color` in the Inspector of `MouseOverOutline` if you want a different color than the default yellow.

6.  **Run the Scene:**
    *   Play your Unity scene.
    *   Move your mouse cursor over the objects you configured. They should now light up with the specified outline color when hovered over.

---

### **1. `OutlineManager.cs`**

This script acts as the central hub for our Object Outline System. It's a Singleton, ensuring only one instance exists. It maintains a registry of all `OutlineObject` components in the scene and provides methods for other scripts to request outlining effects.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The central manager for the Object Outline System.
/// This is a Singleton responsible for registering/unregistering outline-able objects
/// and dispatching outline requests to the appropriate OutlineObject components.
/// </summary>
public class OutlineManager : MonoBehaviour
{
    // Public static property to access the singleton instance.
    // This allows other scripts to easily call OutlineManager.Instance.SetOutline(...).
    public static OutlineManager Instance { get; private set; }

    // A dictionary to store all OutlineObject components, mapped by their GameObject.
    // This allows for quick lookup when an outline request comes in.
    private Dictionary<GameObject, OutlineObject> _outlineableObjects = new Dictionary<GameObject, OutlineObject>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures that only one instance of OutlineManager exists (Singleton pattern).
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one to maintain singleton integrity.
            Debug.LogWarning("OutlineManager: Another instance of OutlineManager found. Destroying this duplicate.", this);
            Destroy(this);
        }
        else
        {
            // Set this instance as the singleton.
            Instance = this;
            // Ensure this manager persists across scene loads if needed (optional, uncomment if required).
            // DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed.
    /// Clears the singleton instance reference.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Registers an OutlineObject component with the manager.
    /// This allows the manager to keep track of all objects that can be outlined.
    /// </summary>
    /// <param name="outlineObject">The OutlineObject component to register.</param>
    public void RegisterOutlineable(OutlineObject outlineObject)
    {
        if (outlineObject == null || outlineObject.gameObject == null)
        {
            Debug.LogWarning("Attempted to register a null or destroyed OutlineObject.", this);
            return;
        }

        if (!_outlineableObjects.ContainsKey(outlineObject.gameObject))
        {
            _outlineableObjects.Add(outlineObject.gameObject, outlineObject);
            // Debug.Log($"Registered OutlineObject: {outlineObject.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"OutlineManager: GameObject '{outlineObject.gameObject.name}' already registered.", this);
        }
    }

    /// <summary>
    /// Unregisters an OutlineObject component from the manager.
    /// This should be called when an outline-able object is destroyed or no longer needs to be managed.
    /// </summary>
    /// <param name="outlineObject">The OutlineObject component to unregister.</param>
    public void UnregisterOutlineable(OutlineObject outlineObject)
    {
        if (outlineObject == null || outlineObject.gameObject == null)
        {
            // Object might have already been destroyed, no need for warning.
            return;
        }

        if (_outlineableObjects.ContainsKey(outlineObject.gameObject))
        {
            _outlineableObjects.Remove(outlineObject.gameObject);
            // Debug.Log($"Unregistered OutlineObject: {outlineObject.gameObject.name}");
        }
    }

    /// <summary>
    /// Requests the OutlineManager to enable the outline for a specific GameObject.
    /// </summary>
    /// <param name="targetGameObject">The GameObject to outline.</param>
    /// <param name="color">The color of the outline.</param>
    public void SetOutline(GameObject targetGameObject, Color color)
    {
        if (targetGameObject == null)
        {
            // Debug.LogWarning("OutlineManager: Attempted to set outline for a null GameObject.");
            return;
        }

        if (_outlineableObjects.TryGetValue(targetGameObject, out OutlineObject outlineObject))
        {
            outlineObject.EnableOutline(color);
        }
        // else
        // {
        //     Debug.LogWarning($"OutlineManager: GameObject '{targetGameObject.name}' is not registered as outline-able.");
        // }
    }

    /// <summary>
    /// Requests the OutlineManager to disable the outline for a specific GameObject.
    /// </summary>
    /// <param name="targetGameObject">The GameObject to clear the outline from.</param>
    public void ClearOutline(GameObject targetGameObject)
    {
        if (targetGameObject == null)
        {
            // Debug.LogWarning("OutlineManager: Attempted to clear outline for a null GameObject.");
            return;
        }

        if (_outlineableObjects.TryGetValue(targetGameObject, out OutlineObject outlineObject))
        {
            outlineObject.DisableOutline();
        }
        // else
        // {
        //     Debug.LogWarning($"OutlineManager: GameObject '{targetGameObject.name}' is not registered as outline-able.");
        // }
    }

    /// <summary>
    /// Disables outlines for all currently outlined objects managed by the system.
    /// Useful for resetting or when changing modes.
    /// </summary>
    public void ClearAllOutlines()
    {
        foreach (var pair in _outlineableObjects)
        {
            pair.Value.DisableOutline();
        }
    }
}
```

---

### **2. `OutlineObject.cs`**

This script is a component attached to any GameObject you want to be outline-able. It manages the actual rendering of the outline by swapping materials or modifying shader properties. It registers itself with the `OutlineManager` and listens for outline requests.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List and Dictionary

/// <summary>
/// Component that enables a GameObject to display an outline.
/// It manages material swapping to show/hide the outline effect.
/// </summary>
[RequireComponent(typeof(Renderer))] // An OutlineObject must have a Renderer to display anything.
public class OutlineObject : MonoBehaviour
{
    [Tooltip("The material that will be used for outlining. Assign an 'Unlit/Color' material, or a custom outline shader material.")]
    [SerializeField] private Material _outlineMaterialPrefab;

    private Renderer _renderer; // Reference to the object's Renderer component.
    private Material[] _originalMaterialsInstance; // Stores the object's original materials (instance-specific).
    private Material[] _instantiatedOutlineMaterials; // Stores instances of the outline material.
    private bool _isOutlined = false; // Flag to track current outline state.

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes references and registers with the OutlineManager.
    /// </summary>
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("OutlineObject requires a Renderer component!", this);
            enabled = false; // Disable script if no Renderer found.
            return;
        }

        // Store the *instance-specific* materials to revert to.
        // .materials creates a new array of material *instances* if they were shared,
        // ensuring we don't modify shared assets.
        _originalMaterialsInstance = _renderer.materials;

        // Register with the manager if it's available.
        // The manager might not be fully initialized yet if this object awakes first.
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.RegisterOutlineable(this);
        }
        else
        {
            Debug.LogWarning("OutlineManager instance not found during Awake. OutlineObject might not be managed correctly.", this);
        }
    }

    /// <summary>
    /// Ensures registration with the OutlineManager if Awake was called before the manager was ready.
    /// </summary>
    private void OnEnable()
    {
        if (OutlineManager.Instance != null && !_isOutlined && !_renderer.materials.Equals(_originalMaterialsInstance))
        {
            // If the object was enabled after manager, ensure it's registered
            // and its materials are restored to original state if they were somehow left outlined.
            OutlineManager.Instance.RegisterOutlineable(this);
            DisableOutline(); // Ensure it starts in a non-outlined state.
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed.
    /// Unregisters from the OutlineManager and cleans up instantiated materials to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        // Unregister from manager
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.UnregisterOutlineable(this);
        }

        // Clean up instantiated outline materials
        // It's crucial to Destroy materials instantiated with 'new Material()'.
        if (_instantiatedOutlineMaterials != null)
        {
            foreach (Material mat in _instantiatedOutlineMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat); // Destroy the actual material instances created by us
                }
            }
        }
        _instantiatedOutlineMaterials = null; // Clear reference
    }

    /// <summary>
    /// Enables the outline effect for this object with a specified color.
    /// </summary>
    /// <param name="color">The color of the outline.</param>
    public void EnableOutline(Color color)
    {
        if (_renderer == null || _outlineMaterialPrefab == null)
        {
            Debug.LogError("OutlineObject: Renderer or Outline Material Prefab is missing on " + gameObject.name, this);
            return;
        }

        if (!_isOutlined)
        {
            // If the number of submeshes changes (unlikely at runtime for static meshes, but good practice),
            // or if the outline materials haven't been created yet.
            if (_instantiatedOutlineMaterials == null || _instantiatedOutlineMaterials.Length != _originalMaterialsInstance.Length)
            {
                // Clean up any old instantiated materials before creating new ones.
                if (_instantiatedOutlineMaterials != null)
                {
                    foreach (Material mat in _instantiatedOutlineMaterials) { if (mat != null) Destroy(mat); }
                }
                
                _instantiatedOutlineMaterials = new Material[_originalMaterialsInstance.Length];
                for (int i = 0; i < _originalMaterialsInstance.Length; i++)
                {
                    // Instantiate the outline material prefab for each submesh.
                    // This creates a *copy* of the _outlineMaterialPrefab, allowing us to modify its color
                    // per object without affecting other objects that use the same prefab.
                    _instantiatedOutlineMaterials[i] = new Material(_outlineMaterialPrefab);
                }
            }

            // Apply the desired color to all instantiated outline materials.
            foreach (var mat in _instantiatedOutlineMaterials)
            {
                // Check if the material's shader has a "_Color" property.
                // Most simple shaders (like Unlit/Color) will have this.
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", color);
                }
                // If using a custom outline shader, you might set other properties here,
                // e.g., mat.SetFloat("_OutlineThickness", 0.05f);
                // mat.SetColor("_OutlineColor", color);
            }

            // Assign the new materials array to the renderer, applying the outline effect.
            _renderer.materials = _instantiatedOutlineMaterials;
            _isOutlined = true;
        }
    }

    /// <summary>
    /// Disables the outline effect for this object, restoring its original materials.
    /// </summary>
    public void DisableOutline()
    {
        if (_renderer == null) return;

        if (_isOutlined)
        {
            // Restore original materials.
            _renderer.materials = _originalMaterialsInstance;
            _isOutlined = false;
        }
    }

    /// <summary>
    /// Returns true if the object is currently outlined, false otherwise.
    /// </summary>
    public bool IsOutlined()
    {
        return _isOutlined;
    }
}
```

---

### **3. `MouseOverOutline.cs`**

This script acts as a client to the `ObjectOutlineSystem`. It uses raycasting to detect when the mouse cursor hovers over an `OutlineObject` and then tells the `OutlineManager` to apply or clear the outline.

```csharp
using UnityEngine;

/// <summary>
/// Example client script demonstrating how to interact with the ObjectOutlineSystem.
/// This script casts a ray from the mouse position and outlines any OutlineObject it hits.
/// </summary>
public class MouseOverOutline : MonoBehaviour
{
    [Tooltip("The color to use when an object is hovered over.")]
    [SerializeField] private Color _hoverColor = Color.yellow;
    [Tooltip("The layer mask to use for raycasting. Helps filter which objects can be hovered.")]
    [SerializeField] private LayerMask _outlineLayerMask = ~0; // ~0 means "Everything"

    private GameObject _currentOutlineTarget = null; // Stores the currently hovered outline-able object.

    /// <summary>
    /// Called once per frame. Handles raycasting and outline state updates.
    /// </summary>
    private void Update()
    {
        // Ensure the OutlineManager is available.
        if (OutlineManager.Instance == null)
        {
            Debug.LogWarning("MouseOverOutline: OutlineManager instance not found. Cannot outline objects.", this);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Perform raycast.
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _outlineLayerMask))
        {
            // Check if the hit object has an OutlineObject component or is managed by the system.
            // We can directly check if OutlineManager knows about it, or check for the component.
            // For this example, we'll check for the component to filter hits initially.
            // A more robust system might have a custom tag or interface check.
            OutlineObject hitOutlineObject = hit.collider.GetComponent<OutlineObject>();

            if (hitOutlineObject != null)
            {
                // We hit an outline-able object.
                if (_currentOutlineTarget != hit.collider.gameObject)
                {
                    // If we were previously hovering over a different object, clear its outline.
                    if (_currentOutlineTarget != null)
                    {
                        OutlineManager.Instance.ClearOutline(_currentOutlineTarget);
                    }

                    // Set outline on the new target.
                    OutlineManager.Instance.SetOutline(hit.collider.gameObject, _hoverColor);
                    _currentOutlineTarget = hit.collider.gameObject;
                }
                // If _currentOutlineTarget == hit.collider.gameObject, no change needed (already outlined).
            }
            else
            {
                // We hit something, but it's not an outline-able object.
                ClearCurrentOutline();
            }
        }
        else
        {
            // Raycast hit nothing.
            ClearCurrentOutline();
        }
    }

    /// <summary>
    /// Clears the outline from the currently targeted object, if any.
    /// </summary>
    private void ClearCurrentOutline()
    {
        if (_currentOutlineTarget != null)
        {
            OutlineManager.Instance.ClearOutline(_currentOutlineTarget);
            _currentOutlineTarget = null;
        }
    }

    /// <summary>
    /// Ensures any active outline is cleared when this script is disabled or destroyed.
    /// </summary>
    private void OnDisable()
    {
        ClearCurrentOutline();
    }
}
```