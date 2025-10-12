// Unity Design Pattern Example: ObjectScalingSystem
// This script demonstrates the ObjectScalingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates an 'ObjectScalingSystem' design pattern. This pattern focuses on creating a robust and flexible system for managing and applying scaling transformations to various objects in your game world.

**Core Idea of the ObjectScalingSystem Pattern:**

The 'ObjectScalingSystem' decouples the specific scaling logic from individual game objects. Instead of each object managing its own scaling, a central system (the `ScalingManager`) orchestrates scaling operations, while individual objects (via the `ScalableObject` component) define *how* they can be scaled (min/max limits, speed, etc.). This makes the system highly maintainable, extensible, and reusable.

**Pattern Components:**

1.  **`ScalingManager` (Singleton/Service):**
    *   Acts as the central hub for the scaling system.
    *   Maintains a registry of all `ScalableObject` components in the scene.
    *   Provides a public API for other parts of the game to request scaling (e.g., from UI, game logic).
    *   Can handle global scaling inputs (e.g., mouse wheel for the currently selected object).
    *   Decouples the source of the scaling command from the scaled object.

2.  **`ScalableObject` (Component):**
    *   A component attached to any GameObject that needs to be scalable.
    *   Encapsulates the specific scaling properties and constraints for that individual object (e.g., `_minScale`, `_maxScale`, `_scaleFactorPerScroll`).
    *   Handles the actual `transform.localScale` manipulation, often with smooth interpolation.
    *   Registers itself with the `ScalingManager` on `OnEnable` and deregisters on `OnDisable`.
    *   Can include logic for visual feedback (e.g., changing material when selected).

---

### 1. `ScalingManager.cs`

This script provides the central management for all scalable objects.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The 'ObjectScalingSystem' design pattern implementation in Unity.
/// 
/// This system provides a centralized way to manage and apply scaling transformations
/// to various GameObjects in the scene. It decouples the scaling logic from individual
/// objects and offers a unified interface for scaling operations, whether driven by
/// user input, events, or programmatic calls.
/// 
/// Pattern Components:
/// 1.  ScalingManager (Singleton/Service): The central hub responsible for:
///     -   Registering and deregistering scalable objects.
///     -   Providing global access to scaling functions.
///     -   Processing common scaling inputs (e.g., mouse scroll wheel for a selected object).
/// 2.  ScalableObject (Component): A component attached to any GameObject that can be scaled. It:
///     -   Defines specific scaling properties for that object (min/max scale, speed, etc.).
///     -   Exposes methods for the ScalingManager or other systems to call for scaling.
///     -   Handles the actual `transform.localScale` modification, often with interpolation.
/// 
/// Advantages of this pattern:
/// -   **Centralized Control**: All scaling orchestration is managed from one place, making it easier to debug and modify.
/// -   **Decoupling**: GameObjects don't need to know *how* they are scaled, only that they *can* be scaled.
///     The ScalableObject component handles the specifics.
/// -   **Flexibility**: Easily add new scalable objects by simply attaching the ScalableObject component.
///     New scaling behaviors (e.g., different input methods) can be added to the Manager or as variations of ScalableObject.
/// -   **Reusability**: The ScalableObject component is reusable across many different objects.
/// -   **Maintainability**: Changes to the core scaling mechanism are isolated to the ScalingManager.
/// 
/// Example Scenario:
/// A user can click on various objects in the scene. The currently selected object
/// can then be scaled up or down using the mouse scroll wheel, within predefined limits.
/// Other objects remain unaffected until selected.
/// </summary>
public class ScalingManager : MonoBehaviour
{
    // --- Singleton Setup ---
    // Provides a static instance for easy global access to the manager.
    public static ScalingManager Instance { get; private set; }

    [Header("Manager Settings")]
    [Tooltip("Global multiplier for scaling operations. Affects how quickly objects respond to scale changes.")]
    [SerializeField] private float _globalScalingSpeedMultiplier = 1.0f;

    // A list to keep track of all ScalableObjects currently registered with this system.
    private readonly List<ScalableObject> _registeredScalableObjects = new List<ScalableObject>();

    // A reference to the currently selected scalable object. This object will typically
    // respond to user input like the mouse scroll wheel.
    private ScalableObject _currentlySelectedScalable;

    private void Awake()
    {
        // Enforce singleton pattern: ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ScalingManager instances found! Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, make this manager persist across scene loads if needed.
        // DontDestroyOnLoad(gameObject); 
    }

    private void Update()
    {
        // Check for user input that should trigger scaling on the selected object.
        HandleInputScaling();

        // Additional manager logic could go here, e.g.:
        // - Global scaling effects applied to all registered objects.
        // - Listening to specific game events to trigger scaling on certain objects.
    }

    /// <summary>
    /// Handles user input (e.g., mouse scroll) to scale the currently selected object.
    /// This method demonstrates how input can drive the scaling system.
    /// </summary>
    private void HandleInputScaling()
    {
        // If no object is selected, there's nothing to scale with input.
        if (_currentlySelectedScalable == null)
        {
            return;
        }

        // Get mouse scroll wheel input (positive for up, negative for down).
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        // Only proceed if there's significant scroll input to avoid jitter.
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Apply the scroll input, factoring in the global speed multiplier.
            // The ScalableObject itself will handle its specific scale factor and limits.
            _currentlySelectedScalable.ApplyScaleDelta(scrollInput * _globalScalingSpeedMultiplier);
        }
    }

    /// <summary>
    /// Registers a ScalableObject with the ScalingManager.
    /// ScalableObjects call this in their OnEnable method to become part of the system.
    /// </summary>
    /// <param name="scalableObject">The ScalableObject component to register.</param>
    public void RegisterScalableObject(ScalableObject scalableObject)
    {
        if (!_registeredScalableObjects.Contains(scalableObject))
        {
            _registeredScalableObjects.Add(scalableObject);
            // Debug.Log($"Registered: {scalableObject.name}. Total scalable objects: {_registeredScalableObjects.Count}");
        }
    }

    /// <summary>
    /// Deregisters a ScalableObject from the ScalingManager.
    /// ScalableObjects call this in their OnDisable method or when destroyed.
    /// </summary>
    /// <param name="scalableObject">The ScalableObject component to deregister.</param>
    public void DeregisterScalableObject(ScalableObject scalableObject)
    {
        if (_registeredScalableObjects.Contains(scalableObject))
        {
            _registeredScalableObjects.Remove(scalableObject);
            // If the deregistered object was the selected one, clear the selection.
            if (_currentlySelectedScalable == scalableObject)
            {
                _currentlySelectedScalable = null;
            }
            // Debug.Log($"Deregistered: {scalableObject.name}. Total scalable objects: {_registeredScalableObjects.Count}");
        }
    }

    /// <summary>
    /// Sets the currently active ScalableObject that will respond to user input.
    /// Other scripts (e.g., UI, player input) can call this to change which object is interactive.
    /// </summary>
    /// <param name="scalableObject">The ScalableObject to select. Pass null to deselect all.</param>
    public void SetSelectedScalable(ScalableObject scalableObject)
    {
        // Only update if the selection has actually changed.
        if (_currentlySelectedScalable != scalableObject)
        {
            // Optional: Provide visual feedback for the previously selected object, if any.
            if (_currentlySelectedScalable != null)
            {
                _currentlySelectedScalable.SetSelectionVisual(false);
            }

            _currentlySelectedScalable = scalableObject;

            // Optional: Provide visual feedback for the newly selected object, if any.
            if (_currentlySelectedScalable != null)
            {
                _currentlySelectedScalable.SetSelectionVisual(true);
                // Debug.Log($"Selected scalable object: {_currentlySelectedScalable.name}");
            }
            else
            {
                // Debug.Log("No scalable object selected.");
            }
        }
    }

    /// <summary>
    /// Programmatically scales a specific ScalableObject by a given delta.
    /// This method provides a way for other systems (e.g., UI buttons, game events)
    /// to trigger scaling without directly interacting with the ScalableObject.
    /// </summary>
    /// <param name="scalableObject">The ScalableObject to apply the scale change to.</param>
    /// <param name="scaleDelta">The amount to change the scale by.</param>
    public void ScaleObject(ScalableObject scalableObject, float scaleDelta)
    {
        if (scalableObject != null)
        {
            scalableObject.ApplyScaleDelta(scaleDelta * _globalScalingSpeedMultiplier);
        }
        else
        {
            Debug.LogWarning("Attempted to scale a null ScalableObject.");
        }
    }

    /// <summary>
    /// Programmatically sets a specific uniform scale value for a ScalableObject.
    /// Useful for resetting scale or setting a precise size.
    /// </summary>
    /// <param name="scalableObject">The ScalableObject to set the scale for.</param>
    /// <param name="newScale">The new target uniform scale value.</param>
    public void SetObjectScale(ScalableObject scalableObject, float newScale)
    {
        if (scalableObject != null)
        {
            scalableObject.SetScale(newScale);
        }
        else
        {
            Debug.LogWarning("Attempted to set scale for a null ScalableObject.");
        }
    }
}
```

---

### 2. `ScalableObject.cs`

This component defines the properties for individual scalable objects and handles the actual scaling transformation.

```csharp
using UnityEngine;

/// <summary>
/// Component for GameObjects that can be scaled by the ObjectScalingSystem.
/// Each ScalableObject defines its own scaling properties and manages its
/// local scale, interacting with the central ScalingManager.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure objects have a collider for mouse interaction
public class ScalableObject : MonoBehaviour
{
    [Header("Scaling Properties")]
    [Tooltip("The minimum uniform scale allowed for this object.")]
    [SerializeField] private float _minScale = 0.5f;
    [Tooltip("The maximum uniform scale allowed for this object.")]
    [SerializeField] private float _maxScale = 3.0f;
    [Tooltip("How much the scale changes per unit of input (e.g., mouse scroll wheel input).")]
    [SerializeField] private float _scaleFactorPerScroll = 0.1f;
    [Tooltip("The time in seconds it takes for the scale to smoothly reach the target scale.")]
    [SerializeField] private float _scaleSmoothTime = 0.15f;

    [Header("Selection Visuals (Optional)")]
    [Tooltip("The Material to apply when this object is selected.")]
    [SerializeField] private Material _selectionMaterial;
    
    // Stores the original material to revert to when deselected.
    private Material _originalMaterial;
    private Renderer _objectRenderer;    // Cached renderer component for material changes.

    private float _targetScale;         // The desired uniform scale we are interpolating towards.
    private float _currentScaleVelocity; // Used by Mathf.SmoothDamp for smooth scaling interpolation.

    private void Awake()
    {
        // Cache the renderer component.
        _objectRenderer = GetComponent<Renderer>();
        // Store the object's original material.
        if (_objectRenderer != null && _objectRenderer.sharedMaterial != null)
        {
            _originalMaterial = _objectRenderer.sharedMaterial; // Use sharedMaterial to avoid creating new material instances
        }

        // Initialize target scale to the current local scale.
        // Also, clamp it to ensure the object starts within its defined scale limits.
        _targetScale = Mathf.Clamp(transform.localScale.x, _minScale, _maxScale);
        transform.localScale = Vector3.one * _targetScale;
    }

    private void OnEnable()
    {
        // Register this object with the ScalingManager when it becomes active.
        // This allows the manager to keep track of all scalable entities.
        if (ScalingManager.Instance != null)
        {
            ScalingManager.Instance.RegisterScalableObject(this);
        }
        else
        {
            Debug.LogError("ScalableObject cannot register because ScalingManager.Instance is null. " +
                           "Make sure a ScalingManager GameObject is present in the scene and initialized.", this);
        }
    }

    private void OnDisable()
    {
        // Deregister this object from the ScalingManager when it becomes inactive or is destroyed.
        if (ScalingManager.Instance != null)
        {
            ScalingManager.Instance.DeregisterScalableObject(this);
        }
    }

    private void Update()
    {
        // Smoothly interpolate the current scale towards the target scale.
        // This creates a visually pleasing animation for scaling changes.
        float currentScale = transform.localScale.x;
        float newScale = Mathf.SmoothDamp(currentScale, _targetScale, ref _currentScaleVelocity, _scaleSmoothTime);
        transform.localScale = new Vector3(newScale, newScale, newScale);
    }

    /// <summary>
    /// Applies a delta (change) to the object's target scale.
    /// This method is typically called by the ScalingManager based on input or events.
    /// </summary>
    /// <param name="delta">The amount to change the scale by. Positive for up, negative for down.</param>
    public void ApplyScaleDelta(float delta)
    {
        // Calculate the proposed new target scale based on the delta and this object's specific scale factor.
        float proposedScale = _targetScale + (delta * _scaleFactorPerScroll);

        // Clamp the proposed scale within the defined minimum and maximum limits.
        _targetScale = Mathf.Clamp(proposedScale, _minScale, _maxScale);
    }

    /// <summary>
    /// Sets the object's target scale to a specific uniform value directly.
    /// This can be called programmatically (e.g., a "reset scale" button).
    /// </summary>
    /// <param name="newScale">The desired new uniform scale value.</param>
    public void SetScale(float newScale)
    {
        // Clamp the new scale within the defined minimum and maximum limits.
        _targetScale = Mathf.Clamp(newScale, _minScale, _maxScale);
    }

    /// <summary>
    /// Toggles visual feedback (e.g., changing material) for when this object is selected or deselected.
    /// </summary>
    /// <param name="isSelected">True if the object is selected, false otherwise.</param>
    public void SetSelectionVisual(bool isSelected)
    {
        if (_objectRenderer != null && _selectionMaterial != null)
        {
            // Apply the selection material if selected, otherwise revert to the original.
            _objectRenderer.sharedMaterial = isSelected ? _selectionMaterial : _originalMaterial;
        }
    }

    /// <summary>
    /// Example interaction: When this object is clicked, it informs the ScalingManager that it's selected.
    /// This requires a Collider component on the GameObject to detect mouse clicks.
    /// </summary>
    private void OnMouseDown()
    {
        if (ScalingManager.Instance != null)
        {
            ScalingManager.Instance.SetSelectedScalable(this);
        }
    }
}
```

---

### How to Implement and Use in Unity:

1.  **Create Scripts**:
    *   In your Unity Project window, create a new C# script named `ScalingManager` and paste the `ScalingManager.cs` code into it.
    *   Create another new C# script named `ScalableObject` and paste the `ScalableObject.cs` code into it.

2.  **Setup the Scaling Manager**:
    *   In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty).
    *   Rename this GameObject to `_Managers` or `ScalingSystem`.
    *   Drag and drop the `ScalingManager` script onto this new GameObject in the Inspector. This will make it the central hub for your scaling system.

3.  **Prepare Scalable Objects**:
    *   Create some 3D objects in your scene (e.g., `GameObject -> 3D Object -> Cube`, `Sphere`, `Cylinder`).
    *   For **each** object you want to be scalable:
        *   Select the object in the Hierarchy.
        *   Drag and drop the `ScalableObject` script onto it in the Inspector.
        *   **Configure Properties**: In the Inspector, adjust the following parameters for each `ScalableObject`:
            *   **Min Scale**: The smallest uniform scale it can have.
            *   **Max Scale**: The largest uniform scale it can have.
            *   **Scale Factor Per Scroll**: How much its scale changes with each unit of input (e.g., mouse scroll).
            *   **Scale Smooth Time**: How smoothly it interpolates to the target scale.
        *   **Selection Visual (Optional but Recommended)**:
            *   Create a new Material (e.g., right-click in Project window -> Create -> Material). Name it `SelectedMaterial`.
            *   Change its color (e.g., to a bright yellow or green) to make it visually distinct.
            *   Drag this `SelectedMaterial` into the `Selection Material` slot of your `ScalableObject` components in the Inspector.
        *   **Collider**: Ensure each scalable object has a `Collider` component (e.g., `Box Collider` for a Cube, `Sphere Collider` for a Sphere). The `[RequireComponent(typeof(Collider))]` attribute will automatically add one if missing when you add the `ScalableObject` component.

4.  **Run the Scene**:
    *   Press the Play button in Unity.
    *   Click on any of your 3D objects that have the `ScalableObject` component. You should see its material change (if you set up the `Selection Material`).
    *   While the object is selected, use your **mouse scroll wheel** to scale it up or down. Observe how it respects the `Min Scale` and `Max Scale` limits.
    *   Click on another object, and repeat. The previously selected object should revert to its original material.

### Example Programmatic Usage:

You can also control scaling from other scripts:

```csharp
using UnityEngine;

public class MyGameController : MonoBehaviour
{
    // Assign a ScalableObject from the scene in the Inspector
    public ScalableObject specificScalableObject; 

    void Start()
    {
        // Example: Scale an object up by a small amount after 2 seconds
        Invoke("ScaleUpSpecificObject", 2.0f);

        // Example: Reset a different object's scale after 5 seconds
        Invoke("ResetAnotherObjectScale", 5.0f);
    }

    void ScaleUpSpecificObject()
    {
        if (ScalingManager.Instance != null && specificScalableObject != null)
        {
            Debug.Log("Programmatically scaling up specific object.");
            ScalingManager.Instance.ScaleObject(specificScalableObject, 0.5f); // Scale up by 0.5 units
        }
    }

    void ResetAnotherObjectScale()
    {
        // Find a scalable object by tag or name, for example
        GameObject anotherObjectGO = GameObject.Find("MySphere"); // Assuming you have a sphere named MySphere
        if (anotherObjectGO != null)
        {
            ScalableObject anotherScalable = anotherObjectGO.GetComponent<ScalableObject>();
            if (ScalingManager.Instance != null && anotherScalable != null)
            {
                Debug.Log("Programmatically resetting another object's scale.");
                ScalingManager.Instance.SetObjectScale(anotherScalable, 1.0f); // Set scale to 1.0
            }
        }
    }

    void Update()
    {
        // Example: Scale the currently selected object rapidly with 'Q' and 'E' keys
        if (Input.GetKey(KeyCode.Q))
        {
            if (ScalingManager.Instance != null && ScalingManager.Instance.gameObject.activeInHierarchy) // Check if manager is active
            {
                // To scale the currently selected object using manager's current selection:
                // This requires direct access to the _currentlySelectedScalable, which is private.
                // Instead, you'd typically select it first then apply scale.
                // Or, expose a public method in ScalingManager to get the selected object, or directly apply scale to it.
                // For demonstration, let's assume we want to scale 'specificScalableObject' if Q is pressed.
                 ScalingManager.Instance.ScaleObject(specificScalableObject, 0.05f);
            }
        }
        if (Input.GetKey(KeyCode.E))
        {
             if (ScalingManager.Instance != null && ScalingManager.Instance.gameObject.activeInHierarchy)
             {
                 ScalingManager.Instance.ScaleObject(specificScalableObject, -0.05f);
             }
        }
    }
}
```

This comprehensive example provides a practical and educational foundation for implementing the 'ObjectScalingSystem' pattern in your Unity projects, promoting cleaner code and more manageable game systems.