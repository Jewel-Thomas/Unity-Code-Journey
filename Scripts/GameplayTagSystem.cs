// Unity Design Pattern Example: GameplayTagSystem
// This script demonstrates the GameplayTagSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Gameplay Tag System is a powerful design pattern for adding flexible, hierarchical metadata to game entities, abilities, and events. It's heavily inspired by the Unreal Engine's Gameplay Tags.

This system allows you to:
*   **Tag anything:** Entities, abilities, states, events, projectiles, etc.
*   **Query efficiently:** Check if an entity has a specific tag, any of a set of tags, or all of a set of tags.
*   **Implement complex logic:** Create gameplay mechanics based on tags (e.g., "If target has 'Ability.Buff.StunImmunity', then 'Ability.Skill.Stun' fails").
*   **Support hierarchy:** Tags can represent a hierarchy (e.g., "Ability.Damage.Fire" implies "Ability.Damage" and "Ability").
*   **Decouple systems:** Avoid direct references to specific classes or enums, making your systems more modular.
*   **Designer-friendly:** Tags are often defined as ScriptableObjects, allowing designers to create and manage them without code changes.

---

### Project Setup and Usage Instructions:

1.  **Create C# Scripts:**
    *   Create a new C# script named `GameplayTag.cs`.
    *   Create a new C# script named `GameplayTagContainer.cs`.
    *   Create a new C# script named `GameplayTagManager.cs`.
    *   Create a new C# script named `TagSystemExample.cs`.
    *   Copy the respective code blocks below into these files.

2.  **Create Folder for Tags:**
    *   In your Unity Project window, create a folder: `Assets/Resources/GameplayTags`.
    *   This is where your `GameplayTag` ScriptableObjects will live. The `GameplayTagManager` automatically loads tags from here.

3.  **Create Gameplay Tags:**
    *   Right-click in the `Assets/Resources/GameplayTags` folder.
    *   Go to `Create > Gameplay > Gameplay Tag`.
    *   Create several tags for demonstration. For example:
        *   `Ability`
        *   `Ability.Damage`
        *   `Ability.Damage.Fire`
        *   `Ability.Damage.Ice`
        *   `Ability.Heal`
        *   `Effect.Buff`
        *   `Effect.Buff.Speed`
        *   `Effect.Debuff`
        *   `Effect.Debuff.Stun`
        *   `State.Alive`
        *   `State.Dead`
        *   `Character.Player`
        *   `Character.Enemy`
    *   **Important:** The `TagName` field in the Inspector for each `GameplayTag` ScriptableObject **must match its file name** exactly, including dots, for hierarchical checks to work correctly with `StartsWith`. E.g., for `Ability.Damage.Fire.asset`, the `TagName` field should be `Ability.Damage.Fire`.

4.  **Add `GameplayTagManager` to Scene:**
    *   Create an Empty GameObject in your scene (e.g., named "Managers").
    *   Add the `GameplayTagManager` component to this GameObject. This ensures it initializes and loads all your tags.

5.  **Use `TagSystemExample`:**
    *   Create another Empty GameObject (e.g., named "TagSystemDemo").
    *   Add the `TagSystemExample` component to it.
    *   In the Inspector for "TagSystemDemo", you will see fields for `My Tags`, `Tags To Add`, `Tags To Remove`, `Query Tag`, and `Query Container`.
    *   Drag and drop the `GameplayTag` ScriptableObjects you created into these lists/fields to configure the example.
    *   Run the scene and observe the Console output to see the system in action.

---

### 1. `GameplayTag.cs`

This ScriptableObject represents a single unique gameplay tag. It's designed to be created in the Unity Editor, making it designer-friendly. Overriding `Equals` and `GetHashCode` is crucial for tags to behave correctly when used in collections like `HashSet` and `Dictionary`.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single, unique Gameplay Tag.
/// Gameplay Tags are hierarchical, case-sensitive strings (e.g., "Ability.Damage.Fire").
/// They are created as ScriptableObjects in the Unity Editor for easy management by designers.
///
/// Implements IEquatable for efficient comparison in collections like HashSets.
/// </summary>
[CreateAssetMenu(fileName = "NewGameplayTag", menuName = "Gameplay/Gameplay Tag", order = 0)]
public class GameplayTag : ScriptableObject, IEquatable<GameplayTag>
{
    // The actual string identifier for this tag.
    // E.g., "Ability.Damage.Fire", "State.Buff.Speed", "Event.OnHit".
    // This field should match the asset file name for consistency and hierarchical checks.
    [Tooltip("The unique string identifier for this tag (e.g., 'Ability.Damage.Fire'). " +
             "Should ideally match the asset's file name for consistency.")]
    [SerializeField]
    private string tagName = "";

    /// <summary>
    /// Gets the string name of this gameplay tag.
    /// </summary>
    public string TagName => tagName;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used here to ensure the tag name is set based on the asset name if it's empty.
    /// In a real project, you might want a more robust validation system (e.g., Editor script).
    /// </summary>
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(tagName))
        {
            tagName = name; // Set tagName to the asset's file name if not already set.
            #if UNITY_EDITOR
            // Mark dirty to save the change in editor if it was empty
            if (!string.IsNullOrEmpty(tagName))
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
            #endif
        }
    }

    /// <summary>
    /// Compares this GameplayTag to another object for equality.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object obj)
    {
        return Equals(obj as GameplayTag);
    }

    /// <summary>
    /// Compares this GameplayTag to another GameplayTag for equality.
    /// This is the primary comparison method. Tags are considered equal if their TagNames are identical.
    /// </summary>
    /// <param name="other">The other GameplayTag to compare with.</param>
    /// <returns>True if the tags are equal, false otherwise.</returns>
    public bool Equals(GameplayTag other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true; // Same instance
        return TagName == other.TagName;
    }

    /// <summary>
    /// Returns a hash code for this GameplayTag.
    /// Essential for efficient storage and retrieval in hash-based collections (e.g., HashSet, Dictionary).
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return TagName != null ? TagName.GetHashCode() : 0;
    }

    /// <summary>
    /// Implicit conversion from GameplayTag to string.
    /// Allows treating a GameplayTag directly as its string name.
    /// </summary>
    public static implicit operator string(GameplayTag tag)
    {
        return tag?.TagName;
    }

    /// <summary>
    /// Returns the string name of the tag for debugging purposes.
    /// </summary>
    public override string ToString()
    {
        return TagName;
    }

    // --- Hierarchical Tag Checks (Optional, can be moved to TagContainer for better encapsulation) ---
    // For simplicity, we implement basic hierarchical checks based on string prefixes.
    // A more robust system might involve explicit parent references in the ScriptableObject itself.

    /// <summary>
    /// Checks if this tag is a child or the same as the 'other' tag.
    /// E.g., "Ability.Damage.Fire" IsChildOf "Ability.Damage" is true.
    /// E.g., "Ability.Damage" IsChildOf "Ability.Damage" is true.
    /// </summary>
    /// <param name="other">The potential parent tag.</param>
    /// <returns>True if this tag is a child or the same as the other tag.</returns>
    public bool IsDescendantOf(GameplayTag other)
    {
        if (other == null) return false;
        if (Equals(other)) return true; // Same tag
        return TagName != null && other.TagName != null && TagName.StartsWith(other.TagName + ".");
    }

    /// <summary>
    /// Checks if this tag is a parent or the same as the 'other' tag.
    /// E.g., "Ability.Damage" IsParentOf "Ability.Damage.Fire" is true.
    /// E.g., "Ability.Damage" IsParentOf "Ability.Damage" is true.
    /// </summary>
    /// <param name="other">The potential child tag.</param>
    /// <returns>True if this tag is a parent or the same as the other tag.</returns>
    public bool IsParentOf(GameplayTag other)
    {
        if (other == null) return false;
        if (Equals(other)) return true; // Same tag
        return other.TagName != null && TagName != null && other.TagName.StartsWith(TagName + ".");
    }
}
```

---

### 2. `GameplayTagContainer.cs`

This class holds a collection of `GameplayTag`s. It's designed to be serializable so it can be exposed in the Unity Inspector. It uses an internal `HashSet` for efficient runtime operations (add, remove, check).

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Linq extensions like Any(), All()

/// <summary>
/// A container for multiple Gameplay Tags.
/// This class manages a collection of GameplayTag ScriptableObjects.
/// It uses a List for serialization in the Inspector and a HashSet for efficient runtime lookups.
/// Provides methods for adding, removing, checking, and querying tags.
/// </summary>
[System.Serializable]
public class GameplayTagContainer
{
    // The list of tags that will be serialized and shown in the Inspector.
    // Designers can drag and drop GameplayTag ScriptableObjects here.
    [Tooltip("The list of Gameplay Tags in this container. Drag GameplayTag assets here.")]
    [SerializeField]
    private List<GameplayTag> tags = new List<GameplayTag>();

    // A runtime HashSet for fast lookups.
    // This is built from the 'tags' list when needed (e.g., after deserialization).
    private HashSet<GameplayTag> runtimeTags;

    /// <summary>
    /// Initializes the runtime HashSet from the serialized list.
    /// This ensures the HashSet is always up-to-date with Inspector changes.
    /// </summary>
    private void EnsureRuntimeTagsInitialized()
    {
        if (runtimeTags == null)
        {
            runtimeTags = new HashSet<GameplayTag>();
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    if (tag != null && !runtimeTags.Contains(tag)) // Avoid duplicates if any
                    {
                        runtimeTags.Add(tag);
                    }
                }
            }
        }
    }

    // --- Constructors ---

    public GameplayTagContainer()
    {
        EnsureRuntimeTagsInitialized();
    }

    public GameplayTagContainer(IEnumerable<GameplayTag> initialTags)
    {
        tags = initialTags?.Where(t => t != null).ToList() ?? new List<GameplayTag>();
        EnsureRuntimeTagsInitialized();
    }

    public GameplayTagContainer(params GameplayTag[] initialTags) : this((IEnumerable<GameplayTag>)initialTags) { }

    // --- Public Methods for Tag Management ---

    /// <summary>
    /// Adds a single GameplayTag to the container.
    /// </summary>
    /// <param name="tag">The GameplayTag to add.</param>
    /// <returns>True if the tag was added (i.e., it wasn't already present), false otherwise.</returns>
    public bool AddTag(GameplayTag tag)
    {
        if (tag == null) return false;

        EnsureRuntimeTagsInitialized();
        if (runtimeTags.Add(tag))
        {
            // Keep serialized list in sync for Inspector visibility if necessary
            // For runtime changes, the serialized list isn't strictly needed to be updated immediately,
            // but for Editor changes, it's important.
            // If this container is part of a MonoBehaviour/ScriptableObject, you might need to
            // mark it dirty in editor to save changes.
            if (!tags.Contains(tag)) // Only add to serialized list if not already there
            {
                tags.Add(tag);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds multiple GameplayTags to the container.
    /// </summary>
    /// <param name="tagsToAdd">An IEnumerable of GameplayTags to add.</param>
    public void AddTags(IEnumerable<GameplayTag> tagsToAdd)
    {
        if (tagsToAdd == null) return;
        foreach (var tag in tagsToAdd)
        {
            AddTag(tag);
        }
    }

    /// <summary>
    /// Removes a single GameplayTag from the container.
    /// </summary>
    /// <param name="tag">The GameplayTag to remove.</param>
    /// <returns>True if the tag was removed (i.e., it was present), false otherwise.</returns>
    public bool RemoveTag(GameplayTag tag)
    {
        if (tag == null) return false;

        EnsureRuntimeTagsInitialized();
        if (runtimeTags.Remove(tag))
        {
            // Keep serialized list in sync
            tags.Remove(tag);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes multiple GameplayTags from the container.
    /// </summary>
    /// <param name="tagsToRemove">An IEnumerable of GameplayTags to remove.</param>
    public void RemoveTags(IEnumerable<GameplayTag> tagsToRemove)
    {
        if (tagsToRemove == null) return;
        foreach (var tag in tagsToRemove)
        {
            RemoveTag(tag);
        }
    }

    /// <summary>
    /// Removes all tags from the container.
    /// </summary>
    public void Clear()
    {
        EnsureRuntimeTagsInitialized();
        runtimeTags.Clear();
        tags.Clear(); // Keep serialized list in sync
    }

    // --- Public Methods for Tag Queries ---

    /// <summary>
    /// Checks if the container contains a specific GameplayTag (exact match or a descendant).
    /// If the tag is "Ability.Damage", and the container has "Ability.Damage.Fire", this returns true.
    /// If an exact match is preferred, use HasExactTag().
    /// </summary>
    /// <param name="tag">The GameplayTag to check for.</param>
    /// <returns>True if the container has the tag or a more specific version of it, false otherwise.</returns>
    public bool HasTag(GameplayTag tag)
    {
        if (tag == null) return false;
        EnsureRuntimeTagsInitialized();
        // Check for exact match or if any contained tag is a descendant of the queried tag.
        // E.g., if container has "Ability.Damage.Fire" and query is "Ability.Damage" -> true
        // If query is "Ability.Damage.Fire" and container has "Ability.Damage" -> false (use HasParentTag for this)
        return runtimeTags.Any(t => t.IsDescendantOf(tag) || t.Equals(tag));
    }

    /// <summary>
    /// Checks if the container contains an exact match for a specific GameplayTag.
    /// Hierarchy is NOT considered here. "Ability.Damage.Fire" does NOT satisfy "Ability.Damage".
    /// </summary>
    /// <param name="tag">The GameplayTag to check for an exact match.</param>
    /// <returns>True if the container has an exact match for the tag, false otherwise.</returns>
    public bool HasExactTag(GameplayTag tag)
    {
        if (tag == null) return false;
        EnsureRuntimeTagsInitialized();
        return runtimeTags.Contains(tag);
    }

    /// <summary>
    /// Checks if the container has ANY of the tags in a given other container.
    /// This query also considers hierarchical relationships (e.g., "Ability.Damage.Fire" satisfies "Ability.Damage").
    /// </summary>
    /// <param name="otherContainer">The container of tags to check against.</param>
    /// <returns>True if any tag in this container matches any tag (or its descendant) in the other container, false otherwise.</returns>
    public bool HasAny(GameplayTagContainer otherContainer)
    {
        if (otherContainer == null || otherContainer.IsEmpty()) return false;
        EnsureRuntimeTagsInitialized();
        otherContainer.EnsureRuntimeTagsInitialized();

        // Check if any tag in 'this' container satisfies any tag in 'otherContainer'
        foreach (var myTag in runtimeTags)
        {
            foreach (var otherTag in otherContainer.runtimeTags)
            {
                if (myTag.IsDescendantOf(otherTag) || myTag.Equals(otherTag))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the container has ALL of the tags in a given other container.
    /// This query also considers hierarchical relationships.
    /// </summary>
    /// <param name="otherContainer">The container of tags to check against.</param>
    /// <returns>True if all tags in the other container are present (or satisfied hierarchically) in this container, false otherwise.</returns>
    public bool HasAll(GameplayTagContainer otherContainer)
    {
        if (otherContainer == null || otherContainer.IsEmpty()) return true; // An empty query container is always satisfied
        EnsureRuntimeTagsInitialized();
        otherContainer.EnsureRuntimeTagsInitialized();

        foreach (var otherTag in otherContainer.runtimeTags)
        {
            if (!HasTag(otherTag)) // Use the HasTag logic that accounts for hierarchy
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks if the container is empty (contains no tags).
    /// </summary>
    /// <returns>True if the container is empty, false otherwise.</returns>
    public bool IsEmpty()
    {
        EnsureRuntimeTagsInitialized();
        return runtimeTags.Count == 0;
    }

    /// <summary>
    /// Returns a read-only collection of the tags currently in the container.
    /// </summary>
    /// <returns>An IEnumerable of GameplayTags.</returns>
    public IEnumerable<GameplayTag> GetTags()
    {
        EnsureRuntimeTagsInitialized();
        return runtimeTags; // Return the runtime HashSet directly (as IEnumerable) for safety
    }

    /// <summary>
    /// Provides a string representation of the container's tags.
    /// </summary>
    public override string ToString()
    {
        EnsureRuntimeTagsInitialized();
        if (runtimeTags.Count == 0) return "[Empty]";
        return $"[{string.Join(", ", runtimeTags.Select(t => t.TagName))}]";
    }

    /// <summary>
    /// Unity's callback for when the script is loaded or a value is changed in the Inspector.
    /// Used here to ensure the runtime HashSet is rebuilt from the serialized list.
    /// </summary>
    public void OnAfterDeserialize()
    {
        // Clear the runtime HashSet and rebuild it from the serialized list.
        // This handles cases where tags are added/removed in the Inspector.
        runtimeTags = null; // Mark for re-initialization
        EnsureRuntimeTagsInitialized();
    }
}
```

---

### 3. `GameplayTagManager.cs`

This is a singleton responsible for loading all `GameplayTag` ScriptableObjects from the project and providing a centralized lookup. It should be present in your scene.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For OrderBy

/// <summary>
/// The central manager for all Gameplay Tags in the project.
/// This is a singleton that loads all GameplayTag ScriptableObjects from a Resources folder
/// and provides methods to retrieve them by name.
/// </summary>
public class GameplayTagManager : MonoBehaviour
{
    // Singleton instance for global access.
    public static GameplayTagManager Instance { get; private set; }

    // Dictionary to store all loaded tags, mapping their string names to the GameplayTag ScriptableObject.
    private Dictionary<string, GameplayTag> allTags = new Dictionary<string, GameplayTag>();

    // Path within the Resources folder where GameplayTag ScriptableObjects are stored.
    private const string TagsResourcesPath = "GameplayTags"; // E.g., Assets/Resources/GameplayTags/

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Implements the singleton pattern and loads all gameplay tags.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GameplayTagManagers found. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep the manager alive across scene loads.

        LoadAllGameplayTags();
    }

    /// <summary>
    /// Loads all GameplayTag ScriptableObjects from the specified Resources path.
    /// </summary>
    private void LoadAllGameplayTags()
    {
        allTags.Clear();
        var loadedTags = Resources.LoadAll<GameplayTag>(TagsResourcesPath);

        if (loadedTags == null || loadedTags.Length == 0)
        {
            Debug.LogWarning($"No Gameplay Tags found in Resources/{TagsResourcesPath}. " +
                             "Please create some GameplayTag ScriptableObjects.", this);
            return;
        }

        foreach (var tag in loadedTags)
        {
            if (tag == null || string.IsNullOrWhiteSpace(tag.TagName))
            {
                Debug.LogWarning($"Found a null or improperly named GameplayTag asset in " +
                                 $"Resources/{TagsResourcesPath}. Skipping.", tag);
                continue;
            }

            if (allTags.ContainsKey(tag.TagName))
            {
                Debug.LogError($"Duplicate Gameplay Tag '{tag.TagName}' found! " +
                               $"Only the first instance will be used. Please ensure unique tag names.", tag);
            }
            else
            {
                allTags.Add(tag.TagName, tag);
            }
        }

        Debug.Log($"GameplayTagManager initialized. Loaded {allTags.Count} unique tags.");
    }

    /// <summary>
    /// Retrieves a GameplayTag ScriptableObject by its string name.
    /// </summary>
    /// <param name="tagName">The string name of the tag (e.g., "Ability.Damage.Fire").</param>
    /// <returns>The GameplayTag ScriptableObject if found, otherwise null.</returns>
    public GameplayTag GetTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return null;

        if (allTags.TryGetValue(tagName, out GameplayTag tag))
        {
            return tag;
        }

        Debug.LogWarning($"Attempted to get unknown Gameplay Tag: '{tagName}'. " +
                         "Make sure it's created as a ScriptableObject and placed in Resources/GameplayTags.", this);
        return null;
    }

    /// <summary>
    /// Returns a read-only collection of all loaded GameplayTags.
    /// </summary>
    public IReadOnlyCollection<GameplayTag> GetAllTags()
    {
        return allTags.Values.OrderBy(t => t.TagName).ToList().AsReadOnly();
    }

    /// <summary>
    /// Convenience method to create a GameplayTagContainer from a list of tag names.
    /// Uses the manager to resolve string names to actual GameplayTag ScriptableObjects.
    /// </summary>
    /// <param name="tagNames">A list of tag names to include in the container.</param>
    /// <returns>A new GameplayTagContainer.</returns>
    public GameplayTagContainer CreateTagContainer(IEnumerable<string> tagNames)
    {
        var containerTags = new List<GameplayTag>();
        if (tagNames != null)
        {
            foreach (var tagName in tagNames)
            {
                GameplayTag tag = GetTag(tagName);
                if (tag != null)
                {
                    containerTags.Add(tag);
                }
            }
        }
        return new GameplayTagContainer(containerTags);
    }

    /// <summary>
    /// Convenience method to create a GameplayTagContainer from a list of existing GameplayTag objects.
    /// </summary>
    public GameplayTagContainer CreateTagContainer(IEnumerable<GameplayTag> tags)
    {
        return new GameplayTagContainer(tags);
    }
}
```

---

### 4. `TagSystemExample.cs`

This MonoBehaviour demonstrates how to use the `GameplayTagSystem` in a practical scenario. Attach this script to an empty GameObject in your scene.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List

/// <summary>
/// Demonstrates the usage of the GameplayTagSystem in Unity.
/// Attach this to a GameObject, assign tags in the Inspector, and run to see it in action.
/// </summary>
public class TagSystemExample : MonoBehaviour
{
    [Header("My Entity's Tags")]
    [Tooltip("Tags currently on this entity.")]
    [SerializeField]
    private GameplayTagContainer myTags = new GameplayTagContainer();

    [Header("Actions")]
    [Tooltip("Tags to add to 'My Tags' on Start().")]
    [SerializeField]
    private List<GameplayTag> tagsToAddOnStart;

    [Tooltip("Tags to remove from 'My Tags' on Start().")]
    [SerializeField]
    private List<GameplayTag> tagsToRemoveOnStart;

    [Header("Query Examples")]
    [Tooltip("A single tag to query against 'My Tags'.")]
    [SerializeField]
    private GameplayTag queryTag;

    [Tooltip("A container of tags to query against 'My Tags' (for HasAny/HasAll).")]
    [SerializeField]
    private GameplayTagContainer queryContainer;

    void Start()
    {
        // Ensure the GameplayTagManager is initialized.
        // It's usually set up in a scene GameObject with DontDestroyOnLoad,
        // so it should be available by now.
        if (GameplayTagManager.Instance == null)
        {
            Debug.LogError("GameplayTagManager is not initialized! Please add it to a GameObject in your scene.");
            return;
        }

        Debug.Log("--- Gameplay Tag System Demonstration ---");

        // --- Initial State ---
        Debug.Log($"My initial tags: {myTags}");

        // --- Adding Tags ---
        if (tagsToAddOnStart != null && tagsToAddOnStart.Count > 0)
        {
            Debug.Log($"Attempting to add tags: {FormatTagsList(tagsToAddOnStart)}");
            myTags.AddTags(tagsToAddOnStart);
            Debug.Log($"My tags after adding: {myTags}");
        }

        // --- Removing Tags ---
        if (tagsToRemoveOnStart != null && tagsToRemoveOnStart.Count > 0)
        {
            Debug.Log($"Attempting to remove tags: {FormatTagsList(tagsToRemoveOnStart)}");
            myTags.RemoveTags(tagsToRemoveOnStart);
            Debug.Log($"My tags after removing: {myTags}");
        }

        // --- Querying Tags ---
        Debug.Log("\n--- Tag Queries ---");

        // Query for a single tag (hierarchical)
        if (queryTag != null)
        {
            bool hasTagResult = myTags.HasTag(queryTag);
            Debug.Log($"Does my entity have '{queryTag.TagName}' (hierarchical)? {hasTagResult}");

            bool hasExactTagResult = myTags.HasExactTag(queryTag);
            Debug.Log($"Does my entity have EXACTLY '{queryTag.TagName}'? {hasExactTagResult}");
        }
        else
        {
            Debug.LogWarning("No 'Query Tag' assigned for single tag queries.");
        }


        // Query for any tags from a container
        if (queryContainer != null && !queryContainer.IsEmpty())
        {
            Debug.Log($"Querying if my entity has ANY of: {queryContainer}");
            bool hasAnyResult = myTags.HasAny(queryContainer);
            Debug.Log($"Result (HasAny): {hasAnyResult}");

            // Query for all tags from a container
            Debug.Log($"Querying if my entity has ALL of: {queryContainer}");
            bool hasAllResult = myTags.HasAll(queryContainer);
            Debug.Log($"Result (HasAll): {hasAllResult}");
        }
        else
        {
            Debug.LogWarning("No 'Query Container' assigned or it's empty for container queries.");
        }

        Debug.Log("\n--- Example Real-World Scenarios (Hardcoded for clarity) ---");

        // Example: Character state
        GameplayTag aliveTag = GameplayTagManager.Instance.GetTag("State.Alive");
        GameplayTag deadTag = GameplayTagManager.Instance.GetTag("State.Dead");
        if (aliveTag != null)
        {
            Debug.Log($"Is entity alive? {myTags.HasTag(aliveTag)}"); // Example: true if has State.Alive
        }

        // Example: Ability costs (checking for resource tags)
        GameplayTag abilityTag = GameplayTagManager.Instance.GetTag("Ability.Damage.Fire");
        if (abilityTag != null)
        {
            // Simulate an ability requiring "Ability.Damage"
            GameplayTagContainer abilityCostTags = GameplayTagManager.Instance.CreateTagContainer(
                new string[] { "Ability.Damage", "Effect.Buff" }
            );

            Debug.Log($"Does entity have all required tags for an Ability requiring '{abilityCostTags}'? {myTags.HasAll(abilityCostTags)}");
            // If myTags has "Ability.Damage.Fire" and "Effect.Buff.Speed", this would be true
        }

        // Example: Applying/Removing buffs/debuffs
        GameplayTag stunTag = GameplayTagManager.Instance.GetTag("Effect.Debuff.Stun");
        if (stunTag != null)
        {
            Debug.Log($"Applying Stun debuff...");
            myTags.AddTag(stunTag);
            Debug.Log($"My tags: {myTags}");
            Debug.Log($"Is entity stunned? {myTags.HasTag(stunTag)}");

            Debug.Log($"Removing Stun debuff...");
            myTags.RemoveTag(stunTag);
            Debug.Log($"My tags: {myTags}");
            Debug.Log($"Is entity stunned? {myTags.HasTag(stunTag)}");
        }
    }

    /// <summary>
    /// Helper to format a list of tags for logging.
    /// </summary>
    private string FormatTagsList(List<GameplayTag> tagsList)
    {
        if (tagsList == null || tagsList.Count == 0) return "[None]";
        return $"[{string.Join(", ", tagsList)}]";
    }

    // You could add buttons in the Inspector in Editor mode for runtime testing:
    /*
    #if UNITY_EDITOR
    [ContextMenu("Add All Configured Tags")]
    private void AddConfiguredTags()
    {
        if (Application.isPlaying && tagsToAddOnStart != null)
        {
            myTags.AddTags(tagsToAddOnStart);
            Debug.Log($"Added configured tags. Current tags: {myTags}");
        }
    }

    [ContextMenu("Remove All Configured Tags")]
    private void RemoveConfiguredTags()
    {
        if (Application.isPlaying && tagsToRemoveOnStart != null)
        {
            myTags.RemoveTags(tagsToRemoveOnStart);
            Debug.Log($"Removed configured tags. Current tags: {myTags}");
        }
    }
    #endif
    */
}
```

---

### Example Unity Inspector Setup for `TagSystemExample`

Assume you've created these `GameplayTag` ScriptableObjects in `Resources/GameplayTags/`:
*   `Ability.Damage.Fire`
*   `Ability.Heal`
*   `Effect.Buff.Speed`
*   `Effect.Debuff.Stun`
*   `Character.Player`

On your `TagSystemExample` GameObject in the Inspector:

**My Entity's Tags:**
*   `myTags` (Drag `Character.Player`, `Ability.Heal` here)

**Actions:**
*   `tagsToAddOnStart` (Drag `Ability.Damage.Fire`, `Effect.Buff.Speed` here)
*   `tagsToRemoveOnStart` (Drag `Ability.Heal` here)

**Query Examples:**
*   `queryTag` (Drag `Ability.Damage` here)
*   `queryContainer` (Drag `Ability.Damage.Fire`, `Effect.Debuff.Stun` here)

When you run the scene, the Console will output a detailed log of the tag manipulations and queries based on this setup.

This complete example provides a robust and practical foundation for implementing a Gameplay Tag System in your Unity projects, emphasizing reusability, performance, and designer workflow.