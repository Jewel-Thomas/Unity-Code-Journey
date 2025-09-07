// Unity Design Pattern Example: AssetLabelingSystem
// This script demonstrates the AssetLabelingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Asset Labeling System** design pattern. It provides a robust, flexible, and type-safe way to categorize your GameObjects and Prefabs at edit-time and query them efficiently at runtime.

The system is broken down into four main parts:
1.  **AssetLabel (ScriptableObject):** Defines what a label is.
2.  **AssetLabeler (MonoBehaviour):** Assigns `AssetLabel`s to GameObjects/Prefabs.
3.  **AssetLabelRegistry (MonoBehaviour Singleton):** Registers and provides query functionality for labeled assets.
4.  **AssetLabelerEditor (Custom Editor):** Enhances the Unity Inspector for `AssetLabeler` for improved workflow.

To use this, simply save the entire code block below as `AssetLabelingSystem.cs` in your Unity project.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like Any(), All(), Select(), ToList()

#if UNITY_EDITOR
using UnityEditor; // Required for custom editor functionalities
#endif

// --- Part 1: Label Definition (ScriptableObject) ---
// This ScriptableObject represents a unique label that can be assigned to assets.
// Using ScriptableObjects for labels prevents issues with string typos, allows for
// type-safe references, and enables easier management and serialization within the Unity Editor.
[CreateAssetMenu(fileName = "NewAssetLabel", menuName = "Asset Label System/Asset Label", order = 1)]
public class AssetLabel : ScriptableObject
{
    // The name of the label. This is primarily for identification in the Editor
    // and for debugging purposes. The ScriptableObject instance itself serves as the unique ID.
    [Tooltip("A unique name for this label. Used for display and identification.")]
    public string labelName;

    // Optional: A description for the label, useful for documentation or tooltips.
    [Tooltip("An optional description explaining the purpose of this label.")]
    [TextArea(1, 3)]
    public string description;

    // Override ToString for better debugging output in the console.
    public override string ToString()
    {
        return $"Label: {labelName}";
    }

    // For ScriptableObjects, default Equals and GetHashCode often rely on reference equality.
    // Explicitly defining them here to ensure consistent behavior if AssetLabel instances
    // were ever compared by value (e.g., based on labelName), though typically you'd compare by reference.
    public override bool Equals(object other)
    {
        // Two AssetLabel ScriptableObjects are considered equal if they are the same instance.
        return ReferenceEquals(this, other);
    }

    public override int GetHashCode()
    {
        // Hash code based on the object's identity, which is standard for ScriptableObjects.
        return base.GetHashCode();
    }
}

// --- Part 2: Label Assignment (MonoBehaviour) ---
// This MonoBehaviour is attached to GameObjects or Prefabs to assign one or more labels to them.
// When an object with this component is active in the scene, it registers itself
// with the AssetLabelRegistry.
[DisallowMultipleComponent] // Typically, only one AssetLabeler per GameObject is sufficient.
[HelpURL("https://github.com/your-username/your-repo-link-here")] // Replace with a useful link for your project
public class AssetLabeler : MonoBehaviour
{
    [Tooltip("The list of Asset Labels assigned to this GameObject/Prefab.")]
    [SerializeField] // Use SerializeField to expose private field in Inspector
    private List<AssetLabel> labels = new List<AssetLabel>();

    // Public property to safely access the assigned labels without exposing the internal list directly.
    public IReadOnlyList<AssetLabel> Labels => labels;

    private void Awake()
    {
        // On Awake, attempt to register this asset and its labels with the global registry.
        // This makes the asset discoverable by the AssetLabelRegistry for runtime queries.
        if (AssetLabelRegistry.Instance != null)
        {
            AssetLabelRegistry.Instance.RegisterAsset(this);
        }
        else
        {
            Debug.LogError($"AssetLabelRegistry not found in scene for GameObject '{gameObject.name}'. " +
                           "Make sure an AssetLabelRegistry is present to use the labeling system.", this);
        }
    }

    private void OnDestroy()
    {
        // When the GameObject is destroyed, unregister it from the registry.
        // This prevents the registry from holding references to destroyed objects (dangling references).
        if (AssetLabelRegistry.Instance != null)
        {
            AssetLabelRegistry.Instance.UnregisterAsset(this);
        }
    }

    // --- Editor-only methods for programmatic label management (e.g., by custom tools) ---
    // These methods can be useful if you're building custom editor scripts to manage labels.
#if UNITY_EDITOR
    /// <summary>
    /// Adds a label to this AssetLabeler. Marks the object dirty for saving.
    /// </summary>
    /// <param name="label">The AssetLabel to add.</param>
    public void AddLabel(AssetLabel label)
    {
        if (label == null)
        {
            Debug.LogWarning("Attempted to add a null label.", this);
            return;
        }
        if (!labels.Contains(label))
        {
            labels.Add(label);
            EditorUtility.SetDirty(this); // Mark dirty to save changes in Editor
        }
    }

    /// <summary>
    /// Removes a label from this AssetLabeler. Marks the object dirty for saving.
    /// </summary>
    /// <param name="label">The AssetLabel to remove.</param>
    public void RemoveLabel(AssetLabel label)
    {
        if (label == null)
        {
            Debug.LogWarning("Attempted to remove a null label.", this);
            return;
        }
        if (labels.Remove(label))
        {
            EditorUtility.SetDirty(this);
        }
    }

    /// <summary>
    /// Replaces all existing labels with a new set of labels. Marks the object dirty for saving.
    /// </summary>
    /// <param name="newLabels">The new collection of AssetLabels to assign.</param>
    public void SetLabels(IEnumerable<AssetLabel> newLabels)
    {
        labels.Clear();
        if (newLabels != null)
        {
            // Add distinct labels to avoid duplicates in the list
            labels.AddRange(newLabels.Where(l => l != null).Distinct());
        }
        EditorUtility.SetDirty(this);
    }
#endif // UNITY_EDITOR
}

// --- Part 3: Label Registry and Query System (MonoBehaviour Singleton) ---
// This is the central component of the Asset Labeling System. It maintains a registry of all
// active GameObjects with AssetLabeler components and provides methods to query them by labels.
// It's implemented as a singleton to provide easy global access from any script.
public class AssetLabelRegistry : MonoBehaviour
{
    // Singleton instance. This provides a global access point to the registry.
    public static AssetLabelRegistry Instance { get; private set; }

    // Dictionary to store assets categorized by their labels.
    // Key: An AssetLabel (e.g., 'Enemy', 'Prop').
    // Value: A list of AssetLabeler components that have been assigned this specific label.
    private Dictionary<AssetLabel, List<AssetLabeler>> _labelToAssetsMap = new Dictionary<AssetLabel, List<AssetLabeler>>();

    // Dictionary to quickly find all labels associated with a specific AssetLabeler.
    // This is useful for efficient unregistration (when an asset is destroyed).
    private Dictionary<AssetLabeler, List<AssetLabel>> _assetToLabelsMap = new Dictionary<AssetLabeler, List<AssetLabel>>();

    // Defines how multiple labels should be matched during a query.
    public enum LabelMatchMode
    {
        // An asset must have ALL of the specified labels to be returned (AND logic).
        All,
        // An asset must have ANY of the specified labels to be returned (OR logic).
        Any
    }

    private void Awake()
    {
        // Implement the singleton pattern.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple instances of AssetLabelRegistry found. Destroying duplicate on {gameObject.name}.", this);
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, uncomment the line below if you want the registry to persist across scene loads.
            // DontDestroyOnLoad(this.gameObject); 
        }

        // Pre-populate the registry with all AssetLabelers already present in the scene.
        // This handles cases where AssetLabelers might Awake before the registry itself,
        // or when a new scene is loaded with existing labeled objects.
        PrePopulateRegistry();
    }

    /// <summary>
    /// Scans the current scene for all existing AssetLabeler components and registers them.
    /// This ensures that all already-active objects are accounted for when the registry initializes.
    /// </summary>
    private void PrePopulateRegistry()
    {
        AssetLabeler[] existingLabelers = FindObjectsOfType<AssetLabeler>();
        foreach (AssetLabeler labeler in existingLabelers)
        {
            RegisterAsset(labeler);
        }
        Debug.Log($"AssetLabelRegistry pre-populated with {existingLabelers.Length} labelers.");
    }

    /// <summary>
    /// Registers an AssetLabeler component and its associated labels with the registry.
    /// This is typically called automatically by the AssetLabeler's Awake method.
    /// </summary>
    /// <param name="assetLabeler">The AssetLabeler component to register.</param>
    public void RegisterAsset(AssetLabeler assetLabeler)
    {
        if (assetLabeler == null || assetLabeler.Labels == null || assetLabeler.Labels.Count == 0)
        {
            // Log a warning if attempting to register a null or unlabeled asset.
            Debug.LogWarning($"Attempted to register null or unlabeled asset: {assetLabeler?.name ?? "null"}", assetLabeler);
            return;
        }

        // If the assetLabeler is already registered, skip re-registration.
        // A more advanced system might handle label updates here.
        if (_assetToLabelsMap.ContainsKey(assetLabeler))
        {
            // Debug.LogWarning($"AssetLabeler '{assetLabeler.name}' already registered. Skipping re-registration.", assetLabeler);
            return;
        }

        List<AssetLabel> currentAssetLabels = new List<AssetLabel>();
        foreach (AssetLabel label in assetLabeler.Labels)
        {
            if (label == null)
            {
                Debug.LogWarning($"AssetLabeler '{assetLabeler.name}' has a null label in its list. Skipping registration for this label.", assetLabeler);
                continue;
            }

            // Ensure the list for this label exists in the map
            if (!_labelToAssetsMap.ContainsKey(label))
            {
                _labelToAssetsMap[label] = new List<AssetLabeler>();
            }
            // Add the asset to the list for this label, if it's not already there
            if (!_labelToAssetsMap[label].Contains(assetLabeler))
            {
                _labelToAssetsMap[label].Add(assetLabeler);
            }
            currentAssetLabels.Add(label);
        }
        
        // Store the asset's labels for quick lookup during unregistration.
        _assetToLabelsMap[assetLabeler] = currentAssetLabels; 
        // Debug.Log($"Registered asset: {assetLabeler.name} with labels: {string.Join(", ", assetLabeler.Labels.Select(l => l.labelName))}", assetLabeler);
    }

    /// <summary>
    /// Unregisters an AssetLabeler component from the registry.
    /// This is typically called automatically by the AssetLabeler's OnDestroy method.
    /// </summary>
    /// <param name="assetLabeler">The AssetLabeler component to unregister.</param>
    public void UnregisterAsset(AssetLabeler assetLabeler)
    {
        if (assetLabeler == null) return;

        // Retrieve the labels associated with this assetLabeler.
        if (_assetToLabelsMap.TryGetValue(assetLabeler, out List<AssetLabel> labelsToRemove))
        {
            foreach (AssetLabel label in labelsToRemove)
            {
                // Remove the asset from each label's list in the map.
                if (_labelToAssetsMap.ContainsKey(label))
                {
                    _labelToAssetsMap[label].Remove(assetLabeler);
                    // If a label has no more associated assets, clean up its entry in the map.
                    if (_labelToAssetsMap[label].Count == 0)
                    {
                        _labelToAssetsMap.Remove(label);
                    }
                }
            }
            // Remove the asset from the asset-to-labels map.
            _assetToLabelsMap.Remove(assetLabeler);
            // Debug.Log($"Unregistered asset: {assetLabeler.name}", assetLabeler);
        }
    }

    /// <summary>
    /// Retrieves all AssetLabeler components (and thus their GameObjects) that have a specific label.
    /// </summary>
    /// <param name="label">The AssetLabel to query for.</param>
    /// <returns>A list of AssetLabeler components with the specified label, or an empty list if none are found.</returns>
    public IReadOnlyList<AssetLabeler> GetAssetsByLabel(AssetLabel label)
    {
        if (label == null)
        {
            Debug.LogWarning("Attempted to query with a null label.");
            return new List<AssetLabeler>();
        }

        if (_labelToAssetsMap.TryGetValue(label, out List<AssetLabeler> assets))
        {
            // Return a new list to prevent external modification of the internal registry list.
            return assets.ToList();
        }
        return new List<AssetLabeler>();
    }

    /// <summary>
    /// Retrieves all AssetLabeler components (and their GameObjects) that match a set of labels
    /// based on the specified match mode.
    /// </summary>
    /// <param name="labels">The list of AssetLabels to query for.</param>
    /// <param name="matchMode">Specifies whether all labels must match (All) or any label must match (Any).</param>
    /// <returns>A list of AssetLabeler components matching the criteria, or an empty list.</returns>
    public IReadOnlyList<AssetLabeler> GetAssetsByLabels(IEnumerable<AssetLabel> labels, LabelMatchMode matchMode)
    {
        if (labels == null || !labels.Any())
        {
            Debug.LogWarning("Attempted to query with an empty or null list of labels.");
            return new List<AssetLabeler>();
        }

        // Filter out any null labels from the input list for robustness
        var validLabels = labels.Where(l => l != null).ToList();
        if (!validLabels.Any())
        {
            Debug.LogWarning("All labels in the query list were null after filtering.");
            return new List<AssetLabeler>();
        }

        HashSet<AssetLabeler> resultSet = new HashSet<AssetLabeler>();

        if (matchMode == LabelMatchMode.Any)
        {
            // OR logic: An asset must have *any* of the specified labels.
            foreach (AssetLabel label in validLabels)
            {
                if (_labelToAssetsMap.TryGetValue(label, out List<AssetLabeler> assetsForLabel))
                {
                    resultSet.UnionWith(assetsForLabel); // Add unique assets to the result set
                }
            }
        }
        else // LabelMatchMode.All (AND logic)
        {
            // AND logic: An asset must have *all* of the specified labels.
            // Start with assets for the first label, then intersect with subsequent labels.
            List<AssetLabeler> initialAssets = null;
            if (validLabels.Count > 0 && _labelToAssetsMap.TryGetValue(validLabels[0], out initialAssets))
            {
                resultSet.UnionWith(initialAssets); // Initialize with assets from the first label

                for (int i = 1; i < validLabels.Count; i++)
                {
                    AssetLabel currentLabel = validLabels[i];
                    if (_labelToAssetsMap.TryGetValue(currentLabel, out List<AssetLabeler> currentLabelAssets))
                    {
                        resultSet.IntersectWith(currentLabelAssets); // Keep only assets common to both sets
                    }
                    else
                    {
                        // If any label in an 'All' query is not present in the registry,
                        // no asset can have 'All' of the labels, so return empty.
                        return new List<AssetLabeler>();
                    }
                    // If at any point the intersection becomes empty, we can stop early.
                    if (resultSet.Count == 0)
                    {
                        break;
                    }
                }
            }
        }

        return resultSet.ToList();
    }

    /// <summary>
    /// Gets a list of all unique AssetLabels currently registered in the system (i.e., labels that have at least one asset).
    /// </summary>
    public IReadOnlyList<AssetLabel> GetAllRegisteredLabels()
    {
        return _labelToAssetsMap.Keys.ToList();
    }

    /// <summary>
    /// Gets a count of assets associated with a specific label.
    /// </summary>
    /// <param name="label">The AssetLabel to count assets for.</param>
    /// <returns>The number of assets with the given label, or 0 if the label is null or not found.</returns>
    public int GetAssetCountByLabel(AssetLabel label)
    {
        if (label == null) return 0;
        return _labelToAssetsMap.TryGetValue(label, out List<AssetLabeler> assets) ? assets.Count : 0;
    }
}

// --- Part 4: Editor Integration (Custom Inspector for AssetLabeler) ---
// This section provides a user-friendly way to assign AssetLabel ScriptableObjects
// to your AssetLabeler component directly in the Unity Inspector.
// This code is only compiled in the Unity Editor, not in builds.
#if UNITY_EDITOR
[CustomEditor(typeof(AssetLabeler))]
[CanEditMultipleObjects] // Allow editing multiple AssetLabeler components at once
public class AssetLabelerEditor : Editor
{
    private SerializedProperty _labelsProperty;
    private AssetLabel[] _allAssetLabels; // Cache all available AssetLabels in the project

    private void OnEnable()
    {
        _labelsProperty = serializedObject.FindProperty("labels");
        // Load all AssetLabel ScriptableObjects from the project.
        // This can be an expensive operation on very large projects, so caching is good.
        // It's done once when the Inspector is enabled.
        _allAssetLabels = AssetDatabase.FindAssets("t:AssetLabel") // Find all assets of type AssetLabel
                                      .Select(guid => AssetDatabase.LoadAssetAtPath<AssetLabel>(AssetDatabase.GUIDToAssetPath(guid))) // Load them
                                      .Where(label => label != null) // Filter out any null results
                                      .OrderBy(label => label.labelName) // Order alphabetically for better UI
                                      .ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // Always start with updating the serialized object data.

        EditorGUILayout.LabelField("Asset Labels", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use this component to tag GameObjects/Prefabs with Asset Labels for runtime queries. " +
                                "Create new Asset Labels via 'Assets/Create/Asset Label System/Asset Label'.", MessageType.Info);

        // Display the current labels using Unity's default list drawing, which is functional.
        EditorGUILayout.PropertyField(_labelsProperty, true); // True to allow children to be drawn (e.g., list size controls)

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Available Labels Quick Selector", EditorStyles.boldLabel);

        // This button opens a GenericMenu (context menu) to easily toggle labels on/off.
        if (_allAssetLabels != null && _allAssetLabels.Length > 0)
        {
            if (GUILayout.Button("Open Label Selector"))
            {
                GenericMenu menu = new GenericMenu();
                
                // Get the currently assigned labels for the selected AssetLabeler(s)
                // Need to handle multi-object editing: if any selected object has a label, it's checked.
                HashSet<AssetLabel> currentLabelsOnTargets = new HashSet<AssetLabel>();
                foreach (AssetLabeler currentTarget in targets.Cast<AssetLabeler>())
                {
                    if (currentTarget != null && currentTarget.Labels != null)
                    {
                        foreach (AssetLabel label in currentTarget.Labels)
                        {
                            if (label != null) currentLabelsOnTargets.Add(label);
                        }
                    }
                }

                foreach (AssetLabel label in _allAssetLabels)
                {
                    if (label == null) continue; // Skip null labels if any somehow appeared
                    
                    // Determine if the label should be checked in the menu.
                    // For multi-editing, if *any* target has the label, it's displayed as checked.
                    bool isChecked = currentLabelsOnTargets.Contains(label);
                    
                    // Add item to the menu, using ToggleLabel as the callback.
                    menu.AddItem(new GUIContent(label.labelName), isChecked, ToggleLabel, label);
                }
                menu.ShowAsContext(); // Display the menu at the current mouse position
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No Asset Labels found in the project. Create them via 'Assets/Create/Asset Label System/Asset Label'.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties(); // Apply changes from the Inspector back to the serialized object.
    }

    /// <summary>
    /// Callback method for the GenericMenu to add or remove an AssetLabel.
    /// Handles multiple selected objects by applying the toggle action to all of them.
    /// </summary>
    /// <param name="labelObject">The AssetLabel object passed from the menu item.</param>
    private void ToggleLabel(object labelObject)
    {
        AssetLabel label = labelObject as AssetLabel;
        if (label == null) return;

        // Iterate over all selected AssetLabeler components to apply the change.
        foreach (AssetLabeler targetLabeler in targets.Cast<AssetLabeler>())
        {
            if (targetLabeler == null) continue;

            // Use the editor-only methods on AssetLabeler to manage the labels.
            if (targetLabeler.Labels.Contains(label))
            {
                targetLabeler.RemoveLabel(label);
                Debug.Log($"Removed label: '{label.labelName}' from '{targetLabeler.name}'", targetLabeler);
            }
            else
            {
                targetLabeler.AddLabel(label);
                Debug.Log($"Added label: '{label.labelName}' to '{targetLabeler.name}'", targetLabeler);
            }
        }
        
        // Refresh the Inspector to reflect changes immediately.
        Repaint();
    }
}
#endif // UNITY_EDITOR

/*
 * --- AssetLabelingSystem Design Pattern Explanation ---
 *
 * The Asset Labeling System is a design pattern used in game development (especially Unity)
 * to categorize and manage game assets (like GameObjects, Prefabs, ScriptableObjects, etc.)
 * using flexible, extensible, and runtime-queryable labels, rather than relying solely
 * on string tags, layers, or folder structures.
 *
 * Benefits of this Pattern:
 * 1.  **Flexibility:** An asset can have multiple labels (e.g., "Enemy", "Melee", "Level1 Content").
 *     Standard Unity string tags are usually limited to one per GameObject.
 * 2.  **Type Safety:** Using ScriptableObjects for labels (`AssetLabel`) prevents common typos
 *     associated with string-based tags. The Unity editor can provide direct references to
 *     these objects, making selection safer and refactoring easier.
 * 3.  **Extensibility:** `AssetLabel` ScriptableObjects can easily be expanded to hold additional
 *     data (e.g., 'description', 'icon', 'color') specific to that label, which isn't possible
 *     with simple string tags.
 * 4.  **Runtime Querying:** Provides efficient methods to find assets based on one or multiple labels,
 *     with support for "All" (AND) and "Any" (OR) matching logic.
 * 5.  **Decoupling:** Game logic can interact with groups of assets based on their role (label)
 *     without needing to know their specific type, name, or exact location. E.g., a "SpawnManager" can
 *     ask for all "Enemy" prefabs, regardless of whether they are "Goblin", "Orc", or "Slime".
 * 6.  **Maintainability:** Easier to manage and refactor large numbers of assets. Changing a label's
 *     name in the `AssetLabel` ScriptableObject automatically updates its display everywhere.
 * 7.  **Editor Workflow Enhancement:** Custom Editor tooling makes assigning and managing labels intuitive.
 *
 * Components of this Implementation:
 *
 * 1.  **`AssetLabel` (ScriptableObject):**
 *     -   **Purpose:** The definition of a single, unique label.
 *     -   **How it works:** You create instances of `AssetLabel` (e.g., "Enemy", "Prop", "UI Element")
 *         in your Project window via `Assets/Create/Asset Label System/Asset Label`. Each instance
 *         is a distinct label reference.
 *     -   **Why ScriptableObject:** Allows creating concrete assets for labels, which can
 *         be referenced by other objects, serialized, and managed through the Editor. This
 *         is the core of its type safety and extensibility.
 *
 * 2.  **`AssetLabeler` (MonoBehaviour):**
 *     -   **Purpose:** The component that actually *assigns* `AssetLabel`s to a GameObject or Prefab.
 *     -   **How it works:** Attach this component to any GameObject you want to categorize.
 *         In its Inspector, you assign one or more `AssetLabel` ScriptableObjects to its `Labels` list.
 *     -   **Runtime behavior:** On `Awake()`, it registers itself with the `AssetLabelRegistry`.
 *         On `OnDestroy()`, it unregisters itself. This keeps the registry up-to-date
 *         with active and inactive objects in the scene.
 *
 * 3.  **`AssetLabelRegistry` (MonoBehaviour Singleton):**
 *     -   **Purpose:** The central management system for all labeled assets. It stores which
 *         assets have which labels and provides query methods.
 *     -   **How it works:**
 *         -   It's a **singleton** (`Instance`) for easy global access from any script. You should
 *             have only one instance of this component in your scene (e.g., on a `_GameManager` object).
 *         -   It maintains internal Dictionaries (`_labelToAssetsMap`, `_assetToLabelsMap`)
 *             to efficiently map labels to `AssetLabeler` components and vice-versa.
 *         -   **`RegisterAsset()` / `UnregisterAsset()`:** These methods are called by `AssetLabeler`
 *             components to keep the registry's internal maps up-to-date with active/inactive objects.
 *         -   **`GetAssetsByLabel()`:** Retrieves all assets associated with a single label.
 *         -   **`GetAssetsByLabels()`:** Retrieves assets based on multiple labels,
 *             supporting "All" (AND logic) and "Any" (OR logic) matching modes.
 *     -   **Why Singleton:** Provides a global, easily accessible point for other scripts
 *         to register and query assets without needing direct references or searching.
 *
 * 4.  **`AssetLabelerEditor` (Custom Editor - Editor Only):**
 *     -   **Purpose:** Enhances the user experience in the Unity Inspector specifically for the `AssetLabeler` component.
 *     -   **How it works:** Replaces the default Inspector for `AssetLabeler` with a custom one.
 *         It provides a user-friendly "Open Label Selector" button that opens a dropdown menu
 *         allowing you to easily toggle `AssetLabel`s on and off from all available labels in your project.
 *         This simplifies label assignment compared to manually dragging individual `ScriptableObject` assets.
 *     -   **Why Editor Code:** This improves the workflow significantly without adding any runtime overhead.
 *         It is wrapped in `#if UNITY_EDITOR` to ensure it's not included in your game builds.
 *
 * --- Practical Usage Steps in a Unity Project ---
 *
 * 1.  **Create the Script File:**
 *     -   Save the entire code block above as `AssetLabelingSystem.cs` in your Unity project
 *         (e.g., in `Assets/Scripts/DesignPatterns/AssetLabelingSystem/`).
 *
 * 2.  **Create `AssetLabel` ScriptableObjects:**
 *     -   In the Unity Editor, go to `Assets/Create/Asset Label System/Asset Label`.
 *     -   Create several `AssetLabel` assets to define your categories.
 *     -   **Examples:**
 *         -   Create `Assets/Labels/Label_Enemy.asset` (Set its `labelName` to "Enemy")
 *         -   Create `Assets/Labels/Label_Prop.asset` (Set its `labelName` to "Prop")
 *         -   Create `Assets/Labels/Label_Interactable.asset` (Set its `labelName` to "Interactable")
 *         -   Create `Assets/Labels/Label_Level1.asset` (Set its `labelName` to "Level 1 Content")
 *         -   Create `Assets/Labels/Label_Boss.asset` (Set its `labelName` to "Boss")
 *
 * 3.  **Setup the `AssetLabelRegistry` in Your Scene:**
 *     -   Create an empty GameObject in your current scene (e.g., name it `_GameManager`).
 *     -   Add the `AssetLabelRegistry` component to this GameObject.
 *     -   Ensure this GameObject is always active in your scene during runtime.
 *
 * 4.  **Label Your Assets (GameObjects/Prefabs):**
 *     -   Select a GameObject or Prefab in your scene or project (e.g., your "Goblin" prefab).
 *     -   Add the `AssetLabeler` component to it.
 *     -   In the `AssetLabeler`'s Inspector:
 *         -   You'll see a list for `Labels`. You can drag `AssetLabel` assets directly into this list.
 *         -   **For easier selection:** Click the "Open Label Selector" button. A dropdown menu will appear.
 *             Check the `AssetLabel`s you want to assign to this GameObject (e.g., "Enemy", "Level 1 Content").
 *             You can select multiple labels for a single asset.
 *     -   Repeat this process for other assets (e.g., "Barrel" prefab: assign "Prop", "Interactable" labels).
 *
 * 5.  **Query Assets at Runtime from Another Script:**
 *     -   You can now use the `AssetLabelRegistry` to find assets based on their labels from any other script.
 *
 *     ```csharp
 *     // Example: GameManager.cs (attach this to your _GameManager object, or any script needing to query assets)
 *     using UnityEngine;
 *     using System.Collections.Generic;
 *     using System.Linq; // For .Select() and .ToList()
 *
 *     public class GameManager : MonoBehaviour
 *     {
 *         // Drag your AssetLabel ScriptableObjects here in the Inspector
 *         // This provides direct references to the label definitions for querying.
 *         public AssetLabel enemyLabel;
 *         public AssetLabel propLabel;
 *         public AssetLabel interactableLabel;
 *         public AssetLabel level1Label;
 *         public AssetLabel bossLabel;
 *
 *         void Start()
 *         {
 *             // Always ensure the registry exists and is ready before querying.
 *             if (AssetLabelRegistry.Instance == null)
 *             {
 *                 Debug.LogError("AssetLabelRegistry not found in the scene! Cannot query assets. " +
 *                                "Make sure you have an AssetLabelRegistry component on an active GameObject.", this);
 *                 return;
 *             }
 *
 *             Debug.Log("--- Asset Labeling System: Runtime Query Examples ---");
 *
 *             // --- Example 1: Get all assets with a single specific label ---
 *             if (enemyLabel != null)
 *             {
 *                 IReadOnlyList<AssetLabeler> enemies = AssetLabelRegistry.Instance.GetAssetsByLabel(enemyLabel);
 *                 Debug.Log($"Found {enemies.Count} GameObjects labeled as '{enemyLabel.labelName}':");
 *                 foreach (var enemyLabeler in enemies)
 *                 {
 *                     Debug.Log($"- {enemyLabeler.gameObject.name} (All Labels: {string.Join(", ", enemyLabeler.Labels.Select(l => l.labelName))})");
 *                 }
 *             }
 *
 *             // --- Example 2: Get all assets with ANY of multiple labels (OR logic) ---
 *             // Find assets that are either "Interactable" OR "Prop".
 *             List<AssetLabel> interactiveOrPropLabels = new List<AssetLabel> { interactableLabel, propLabel };
 *             if (interactiveOrPropLabels.All(l => l != null)) // Ensure all labels are assigned in Inspector
 *             {
 *                 IReadOnlyList<AssetLabeler> interactivesOrProps = AssetLabelRegistry.Instance.GetAssetsByLabels(
 *                     interactiveOrPropLabels, AssetLabelRegistry.LabelMatchMode.Any);
 *                 Debug.Log($"\nFound {interactivesOrProps.Count} GameObjects that are '{interactableLabel.labelName}' OR '{propLabel.labelName}':");
 *                 foreach (var itemLabeler in interactivesOrProps)
 *                 {
 *                     Debug.Log($"- {itemLabeler.gameObject.name} (All Labels: {string.Join(", ", itemLabeler.Labels.Select(l => l.labelName))})");
 *                 }
 *             }
 *
 *             // --- Example 3: Get all assets with ALL of multiple labels (AND logic) ---
 *             // Find assets that are an "Enemy" AND a "Boss" AND "Level 1 Content".
 *             List<AssetLabel> level1BossEnemyLabels = new List<AssetLabel> { enemyLabel, bossLabel, level1Label };
 *             if (level1BossEnemyLabels.All(l => l != null))
 *             {
 *                 IReadOnlyList<AssetLabeler> level1Bosses = AssetLabelRegistry.Instance.GetAssetsByLabels(
 *                     level1BossEnemyLabels, AssetLabelRegistry.LabelMatchMode.All);
 *                 Debug.Log($"\nFound {level1Bosses.Count} GameObjects that are '{enemyLabel.labelName}' AND '{bossLabel.labelName}' AND '{level1Label.labelName}':");
 *                 foreach (var bossLabeler in level1Bosses)
 *                 {
 *                     Debug.Log($"- {bossLabeler.gameObject.name} (All Labels: {string.Join(", ", bossLabeler.Labels.Select(l => l.labelName))})");
 *                 }
 *             }
 *
 *             // You can then get the actual GameObject from the AssetLabeler reference:
 *             // GameObject firstEnemyGameObject = enemies.FirstOrDefault()?.gameObject;
 *             // if (firstEnemyGameObject != null)
 *             // {
 *             //     Debug.Log($"\nFirst enemy GameObject found: {firstEnemyGameObject.name}");
 *             // }
 *         }
 *     }
 *     ```
 *
 * This setup provides a robust and flexible way to manage asset categorization and queries
 * in your Unity projects, significantly improving game logic, content management, and maintainability.
 */

```