// Unity Design Pattern Example: UserGeneratedContentSystem
// This script demonstrates the UserGeneratedContentSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **User Generated Content (UGC) System** design pattern in Unity. This pattern provides a structured way for your game to allow players to create, save, load, and manage their own content (like custom levels, characters, items, etc.).

**Key Components of the UGC System Pattern:**

1.  **Content Definition (Data Model):** A data structure (`UGCLevelData`, `LevelPieceData`) that defines what a piece of UGC looks like. It must be serializable to be saved.
2.  **Content Creation Interface (Conceptual):** The in-game tools or UI that users interact with to build/design their content. (Simulated in this code by creating `UGCLevelData` objects programmatically).
3.  **Content Storage/Persistence:** Mechanisms to save the created content to disk or a cloud service and load it back. (Here, we use `JsonUtility` for serialization and `Application.persistentDataPath` for local file storage).
4.  **Content Management/Discovery:** Functions to list, search, filter, and delete UGC.
5.  **Content Loading/Instantiation:** How the loaded UGC data is transformed back into playable game elements. (Simulated instantiation with debug logs).
6.  **Content Validation/Moderation (Optional but Recommended):** Logic to ensure UGC meets game rules, is safe, and isn't malicious. (Basic validation included).

---

To use this example:

1.  Create a new C# script named `UserGeneratedContentSystem.cs` in your Unity project.
2.  Copy and paste the entire code below into the script.
3.  Create an empty GameObject in your scene (e.g., named "UGCSysManager").
4.  Attach the `UserGeneratedContentSystem.cs` script to this GameObject.
5.  Run the scene.
6.  While the scene is running, select the "UGCSysManager" GameObject in the Hierarchy.
7.  In the Inspector, right-click on the `UserGeneratedContentSystem` component and select "Run Example UGC Flow".
8.  Observe the Console window for detailed logs of content creation, saving, listing, loading, and deletion.
9.  You can also navigate to `Application.persistentDataPath` (shown in the logs) on your system to see the generated JSON files.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO; // For file system operations (File, Directory, Path)
using System.Linq; // For LINQ operations if needed (e.g., filtering lists)

// This script demonstrates the UserGeneratedContentSystem design pattern in Unity.
// It allows users to create, save, load, and manage custom game levels locally.

// --- 1. Content Definition (Data Model) ---
// These are the core data structures that represent a single piece of user-generated content.
// In this example, we're defining a custom game level and its constituent pieces.
// They are marked [System.Serializable] so Unity's JsonUtility can convert them to/from JSON.

/// <summary>
/// Represents a single buildable piece within a UGC level.
/// This could be a block, a prop, a spawn point, etc.
/// </summary>
[System.Serializable]
public class LevelPieceData
{
    // A unique identifier for the type of prefab this piece represents (e.g., "Cube", "Wall_A", "SpawnPoint")
    public string prefabId; 
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale = Vector3.one; // Default scale

    public LevelPieceData(string id, Vector3 pos, Quaternion rot, Vector3 sc)
    {
        prefabId = id;
        position = pos;
        rotation = rot;
        scale = sc;
    }
}

/// <summary>
/// Represents a complete user-generated game level, containing its metadata and structure.
/// This is the primary unit of UGC managed by the system.
/// </summary>
[System.Serializable]
public class UGCLevelData
{
    public string levelId;          // Unique identifier for this specific level (e.g., a GUID)
    public string levelName;        // User-given name for the level
    public string author;           // Creator's name or ID
    public string creationDate;     // Date and time of creation (string for easy serialization)
    public int version;             // Version of the content data schema (useful for future updates/migrations)
    public List<LevelPieceData> pieces = new List<LevelPieceData>(); // The actual level layout

    // Constructor for easy creation of new levels
    public UGCLevelData(string name, string creator, List<LevelPieceData> levelPieces = null)
    {
        levelId = Guid.NewGuid().ToString(); // Generate a unique ID for the level
        levelName = name;
        author = creator;
        creationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Format date as string
        version = 1; // Initial version
        if (levelPieces != null)
        {
            // Create a new list to avoid modifying the passed-in list directly
            pieces = new List<LevelPieceData>(levelPieces); 
        }
    }

    // Default constructor is required for JsonUtility.FromJson to work correctly during deserialization.
    public UGCLevelData() { }

    // Override ToString for easier debugging and logging of level data.
    public override string ToString()
    {
        return $"Level ID: {levelId}\nName: {levelName}\nAuthor: {author}\nCreated: {creationDate}\nPieces: {pieces.Count}";
    }
}


// --- 2. Content Storage and Management System ---
// This MonoBehaviour acts as the central hub for managing UGC.
// It handles file I/O, listing, loading, saving, and deleting content.

public class UserGeneratedContentSystem : MonoBehaviour
{
    // --- Configuration ---
    [Tooltip("The subdirectory within Application.persistentDataPath where UGC will be stored.")]
    [SerializeField]
    private string ugcDirectoryName = "UGC_Levels";

    // Full path to the UGC directory, constructed in InitializeSystem().
    private string ugcPath;

    // --- Singleton Pattern (Optional but Recommended for Manager Systems) ---
    // A singleton makes this system easily accessible from anywhere in your game.
    public static UserGeneratedContentSystem Instance { get; private set; }

    private void Awake()
    {
        // Enforce the singleton pattern: only one instance of this system should exist.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple UserGeneratedContentSystem instances found. Destroying duplicate.");
            Destroy(this);
            return;
        }
        Instance = this;
        // Keep this GameObject alive across scene loads if UGC needs to be managed continuously.
        DontDestroyOnLoad(gameObject); 

        InitializeSystem();
    }

    /// <summary>
    /// Initializes the UGC system, ensuring the dedicated storage directory exists.
    /// This directory will be located inside Unity's Application.persistentDataPath.
    /// </summary>
    private void InitializeSystem()
    {
        // Combine the persistent data path with our custom directory name.
        ugcPath = Path.Combine(Application.persistentDataPath, ugcDirectoryName);

        // Check if the directory exists, and create it if it doesn't.
        if (!Directory.Exists(ugcPath))
        {
            try
            {
                Directory.CreateDirectory(ugcPath);
                Debug.Log($"UGC directory created at: {ugcPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create UGC directory at {ugcPath}: {e.Message}");
            }
        }
        else
        {
            Debug.Log($"UGC directory already exists at: {ugcPath}");
        }
    }

    /// <summary>
    /// Constructs the full file path for a given UGC level ID.
    /// Each level will be saved as a separate JSON file.
    /// </summary>
    /// <param name="levelId">The unique ID of the level.</param>
    /// <returns>The full path to the level's JSON file.</returns>
    private string GetFilePathForLevel(string levelId)
    {
        // Ensure the levelId is safe for use as a filename
        // (though GUIDs are usually fine, this is a good general practice)
        string safeLevelId = string.Join("_", levelId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(ugcPath, $"{safeLevelId}.json");
    }

    // --- Content Creation/Saving ---
    /// <summary>
    /// Saves a new or existing user-generated level to persistent storage.
    /// This involves validating the content, serializing it to JSON, and writing it to a file.
    /// </summary>
    /// <param name="levelData">The UGCLevelData object to save.</param>
    /// <returns>True if save was successful, false otherwise.</returns>
    public bool SaveUGCLevel(UGCLevelData levelData)
    {
        // 3. Content Validation (Basic Example)
        // Before saving, it's good practice to validate the content.
        // For shared UGC (online), this would also involve server-side validation.
        if (!ValidateUGCLevel(levelData))
        {
            Debug.LogError($"Validation failed for level '{levelData.levelName}'. Cannot save.");
            return false;
        }

        // Ensure the levelId is present. For new levels, it's generated in the constructor.
        // If loading and resaving, it should already exist.
        if (string.IsNullOrEmpty(levelData.levelId))
        {
            levelData.levelId = Guid.NewGuid().ToString();
            Debug.LogWarning($"Level ID was empty for '{levelData.levelName}', a new one has been generated: {levelData.levelId}");
        }

        // Convert the UGCLevelData object to a JSON string.
        // JsonUtility requires the class to be [System.Serializable].
        // The 'true' argument enables pretty printing for readability in the saved file.
        string json = JsonUtility.ToJson(levelData, true); 

        string filePath = GetFilePathForLevel(levelData.levelId);

        try
        {
            // Write the JSON string to the specified file path.
            // In a real production game, consider using asynchronous file I/O
            // (e.g., 'await File.WriteAllTextAsync(filePath, json);')
            // to prevent blocking the main thread, especially for larger files.
            File.WriteAllText(filePath, json);
            Debug.Log($"UGC Level '{levelData.levelName}' (ID: {levelData.levelId}) saved to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save UGC level '{levelData.levelName}' to {filePath}: {e.Message}");
            return false;
        }
    }

    // --- Content Loading ---
    /// <summary>
    /// Loads a user-generated level from persistent storage by its unique ID.
    /// This involves reading the JSON file and deserializing it into a UGCLevelData object.
    /// </summary>
    /// <param name="levelId">The unique ID of the level to load.</param>
    /// <returns>The loaded UGCLevelData object, or null if not found or an error occurred.</returns>
    public UGCLevelData LoadUGCLevel(string levelId)
    {
        string filePath = GetFilePathForLevel(levelId);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"UGC Level with ID '{levelId}' not found at: {filePath}");
            return null;
        }

        try
        {
            // Read the entire JSON string from the file.
            // As with saving, consider asynchronous reading for production.
            string json = File.ReadAllText(filePath);
            // Deserialize the JSON string back into a UGCLevelData object.
            UGCLevelData loadedLevel = JsonUtility.FromJson<UGCLevelData>(json);
            Debug.Log($"UGC Level '{loadedLevel.levelName}' (ID: {loadedLevel.levelId}) loaded from: {filePath}");
            return loadedLevel;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load UGC level with ID '{levelId}' from {filePath}: {e.Message}");
            return null;
        }
    }

    // --- Content Management/Discovery ---
    /// <summary>
    /// Retrieves a list of metadata for all available user-generated levels.
    /// This method loads each file individually, which might be slow for many levels.
    /// For production, consider storing metadata in a separate index file or only loading
    /// essential information (e.g., name, author, ID) for display.
    /// </summary>
    /// <returns>A list of UGCLevelData objects for all discovered levels, or an empty list.</returns>
    public List<UGCLevelData> ListAllUGCLevels()
    {
        List<UGCLevelData> availableLevels = new List<UGCLevelData>();
        if (!Directory.Exists(ugcPath))
        {
            Debug.LogWarning($"UGC directory '{ugcPath}' does not exist yet. No levels to list.");
            return availableLevels;
        }

        // Get all JSON files in the UGC directory.
        string[] levelFiles = Directory.GetFiles(ugcPath, "*.json");

        foreach (string filePath in levelFiles)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                // Deserialize the full object for simplicity in this demo.
                // In a real game, you might load a lighter "metadata" object first.
                UGCLevelData levelMetadata = JsonUtility.FromJson<UGCLevelData>(json);
                if (levelMetadata != null)
                {
                    availableLevels.Add(levelMetadata);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading UGC file '{filePath}': {e.Message}");
            }
        }
        Debug.Log($"Found {availableLevels.Count} UGC levels.");
        return availableLevels;
    }

    /// <summary>
    /// Deletes a user-generated level from persistent storage by its unique ID.
    /// </summary>
    /// <param name="levelId">The unique ID of the level to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    public bool DeleteUGCLevel(string levelId)
    {
        string filePath = GetFilePathForLevel(levelId);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"UGC Level with ID '{levelId}' not found for deletion at: {filePath}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            Debug.Log($"UGC Level (ID: {levelId}) deleted successfully from: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete UGC level with ID '{levelId}' from {filePath}: {e.Message}");
            return false;
        }
    }

    // --- Content Validation ---
    /// <summary>
    /// Performs basic validation on UGCLevelData.
    /// In a real system, this could involve more complex rules,
    /// such as checking for forbidden content, size limits, or structural integrity.
    /// This helps prevent corrupted or malicious content.
    /// </summary>
    /// <param name="levelData">The level data to validate.</param>
    /// <returns>True if the level is valid according to game rules, false otherwise.</returns>
    private bool ValidateUGCLevel(UGCLevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("UGC Level validation failed: Level data is null.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(levelData.levelName))
        {
            Debug.LogError("UGC Level validation failed: Level name cannot be empty.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(levelData.author))
        {
            Debug.LogError("UGC Level validation failed: Author name cannot be empty.");
            return false;
        }
        if (levelData.pieces == null) // Ensure the list itself is not null
        {
            Debug.LogError("UGC Level validation failed: Level pieces list is null.");
            return false;
        }

        // Example: Limit the number of pieces to prevent overly complex/performance-heavy levels.
        const int MAX_PIECES = 1000; 
        if (levelData.pieces.Count > MAX_PIECES)
        {
            Debug.LogError($"UGC Level '{levelData.levelName}' validation failed: Too many pieces ({levelData.pieces.Count}). Max allowed is {MAX_PIECES}.");
            return false;
        }

        // Example: Validate individual piece data (e.g., ensure prefab IDs are not empty).
        foreach (var piece in levelData.pieces)
        {
            if (piece == null)
            {
                Debug.LogError($"UGC Level '{levelData.levelName}' validation failed: Contains a null piece.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(piece.prefabId))
            {
                Debug.LogError($"UGC Level '{levelData.levelName}' validation failed: A piece has an empty prefab ID.");
                return false;
            }
            // Add more checks here: e.g., position within game bounds, valid rotation/scale values,
            // existence of 'prefabId' in a whitelist of allowed assets.
        }

        return true; // If all checks pass, the level is considered valid.
    }

    // --- Content Loading/Instantiation (Simulated) ---
    /// <summary>
    /// Simulates the instantiation of a loaded UGC level into the game world.
    /// In a real project, this would involve instantiating actual prefabs based on prefab IDs
    /// and positioning them according to the level data.
    /// </summary>
    /// <param name="levelData">The UGCLevelData to instantiate.</param>
    public void InstantiateUGCLevelInGame(UGCLevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("Cannot instantiate null UGC Level data.");
            return;
        }

        Debug.Log($"--- Instantiating UGC Level: {levelData.levelName} by {levelData.author} ---");

        // In a real game, you would typically use a dedicated LevelBuilder or WorldGenerator class here.
        // For each piece, you'd find the corresponding prefab and instantiate it.
        // Example (conceptual, requires actual prefabs and a loading mechanism like Addressables):
        /*
        GameObject levelRoot = new GameObject($"UGC_Level_{levelData.levelName}"); // Create a parent for all level pieces
        foreach (var piece in levelData.pieces)
        {
            // This is a placeholder. In a real game, you'd use Unity.Addressables, Resources.Load, or an Asset Bundle system.
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/{piece.prefabId}"); 
            if (prefab != null)
            {
                // Instantiate the prefab at the specified position, rotation, and parent it to the levelRoot.
                GameObject instance = Instantiate(prefab, piece.position, piece.rotation, levelRoot.transform);
                instance.transform.localScale = piece.scale; // Apply scale
                Debug.Log($"Instantiated '{piece.prefabId}' at {piece.position}");
            }
            else
            {
                Debug.LogWarning($"Prefab '{piece.prefabId}' not found for level '{levelData.levelName}'. Skipping piece.");
            }
        }
        */

        // For this example, we'll just log the "instantiation" of each piece.
        Debug.Log($"Simulating instantiation of {levelData.pieces.Count} pieces for '{levelData.levelName}'.");
        foreach (var piece in levelData.pieces)
        {
            Debug.Log($"  - Piece: {piece.prefabId} at {piece.position} with rotation {piece.rotation.eulerAngles} and scale {piece.scale}");
        }
        Debug.Log($"--- UGC Level '{levelData.levelName}' instantiation simulation complete. ---");
    }

    // --- Example Usage (for demonstration purposes, triggerable from Inspector) ---
    [ContextMenu("Run Example UGC Flow")]
    public void RunExampleUGCFlow()
    {
        Debug.Log("--- Starting UGC Example Flow ---");

        // 1. Simulate Content Creation
        Debug.Log("\n--- Creating a new UGC Level ---");
        List<LevelPieceData> myLevelPieces = new List<LevelPieceData>
        {
            new LevelPieceData("FloorTile_A", new Vector3(0, 0, 0), Quaternion.identity, Vector3.one),
            new LevelPieceData("Wall_Basic", new Vector3(0, 0.5f, 5), Quaternion.Euler(0,0,0), Vector3.one),
            new LevelPieceData("Wall_Basic", new Vector3(5, 0.5f, 0), Quaternion.Euler(0,90,0), Vector3.one),
            new LevelPieceData("SpawnPoint", new Vector3(0, 0.1f, -4), Quaternion.identity, Vector3.one * 0.5f),
            new LevelPieceData("ExitDoor", new Vector3(0, 0.5f, 4.5f), Quaternion.identity, Vector3.one)
        };
        UGCLevelData myCustomLevel = new UGCLevelData("My First Custom Level", "PlayerOne", myLevelPieces);
        string myLevelId = myCustomLevel.levelId; // Store its ID for later retrieval/deletion

        // 2. Save Content
        Debug.Log("\n--- Saving the created UGC Level ---");
        SaveUGCLevel(myCustomLevel);

        // Modify the level (e.g., in a level editor) and save it again.
        // The system will overwrite the existing file because the ID is the same.
        myCustomLevel.levelName = "My First Custom Level (Updated)";
        myCustomLevel.pieces.Add(new LevelPieceData("Collectible_Coin", new Vector3(1, 1, 1), Quaternion.identity, Vector3.one * 0.2f));
        Debug.Log("\n--- Saving an updated UGC Level ---");
        SaveUGCLevel(myCustomLevel);


        // 3. Simulate another user creating content (or another level by the same user)
        Debug.Log("\n--- Creating and saving a second UGC Level ---");
        List<LevelPieceData> secondLevelPieces = new List<LevelPieceData>
        {
            new LevelPieceData("FloorTile_B", new Vector3(0, 0, 0), Quaternion.identity, Vector3.one),
            new LevelPieceData("Platform_Small", new Vector3(2, 1, 0), Quaternion.identity, Vector3.one),
            new LevelPieceData("SpikeTrap", new Vector3(-2, 0.1f, 0), Quaternion.identity, Vector3.one)
        };
        UGCLevelData anotherLevel = new UGCLevelData("The Pit of Doom", "LevelDesignerX", secondLevelPieces);
        SaveUGCLevel(anotherLevel);


        // 4. Content Management/Discovery: List all available levels
        Debug.Log("\n--- Listing all available UGC Levels ---");
        List<UGCLevelData> allLevels = ListAllUGCLevels();
        foreach (var level in allLevels)
        {
            Debug.Log($"Found Level: '{level.levelName}' (ID: {level.levelId}) by {level.author} with {level.pieces.Count} pieces.");
        }


        // 5. Content Loading
        Debug.Log("\n--- Loading 'My First Custom Level (Updated)' by its ID ---");
        UGCLevelData loadedLevel = LoadUGCLevel(myLevelId);
        if (loadedLevel != null)
        {
            Debug.Log($"Successfully loaded level:\n{loadedLevel.ToString()}");
            // 6. Content Instantiation (Simulated)
            InstantiateUGCLevelInGame(loadedLevel);
        }
        else
        {
            Debug.LogError($"Could not load level with ID: {myLevelId}");
        }

        // 7. Content Deletion
        Debug.Log("\n--- Deleting 'The Pit of Doom' ---");
        DeleteUGCLevel(anotherLevel.levelId);


        // Verify deletion by listing again
        Debug.Log("\n--- Listing all available UGC Levels after deletion ---");
        allLevels = ListAllUGCLevels();
        foreach (var level in allLevels)
        {
            Debug.Log($"Remaining Level: '{level.levelName}' (ID: {level.levelId}) by {level.author}");
        }

        Debug.Log("--- UGC Example Flow Complete ---");
        Debug.Log($"UGC files are stored in: {ugcPath}");
    }


    // --- General Best Practices for a Real-World UGC System ---
    /*
    * 1. Asynchronous Operations: For production games, especially with large files or many files,
    *    file I/O (Read/WriteAllText) should be asynchronous (using C# async/await and Task-based operations)
    *    to prevent blocking the main thread and causing frame rate drops.
    *    Example: 'await File.WriteAllTextAsync(filePath, json);'
    *    This would require changes to method signatures (e.g., 'public async Task<bool> SaveUGCLevel(...)').
    *
    * 2. Asset Management: 'prefabId' in LevelPieceData would typically map to actual Prefabs
    *    loaded via Unity's Addressables system or Asset Bundles. This allows for efficient memory
    *    management, dynamic loading/unloading, and potentially content updates without full game patches.
    *
    * 3. UI Integration: A real UGC system requires a robust User Interface for:
    *    - **Content Creation/Editing:** An in-game editor where users build their levels/items.
    *    - **Content Browsing:** Listing available content with filters, search, and pagination.
    *    - **Content Interaction:** Providing options to load, edit, delete, or share content.
    *
    * 4. Cloud Integration (Optional but Common): For sharing UGC between players,
    *    the system would extend to interact with a backend server (e.g., PlayFab, Firebase, custom API).
    *    This involves uploading content, downloading content, and potentially server-side moderation.
    *
    * 5. Robust Error Handling: More detailed error messages, logging to file,
    *    and user-friendly error prompts for failed operations.
    *
    * 6. Versioning: The 'version' field in UGCLevelData is crucial. If your game updates
    *    and the structure of UGCLevelData changes, you'll need migration logic
    *    when loading older versions to convert them to the new format.
    *
    * 7. Thumbnails: For better user experience, UGC often includes a small image thumbnail.
    *    This could be saved as a separate file (e.g., PNG) next to the JSON, or embedded
    *    as a Base64 string within the JSON (though less efficient for large images).
    *
    * 8. Security and Trust: If UGC is shared, consider:
    *    - **Sanitizing input:** Prevent script injection or malicious data.
    *    - **Content review:** Manual or automated moderation for inappropriate content.
    *    - **Reporting system:** Allow users to report problematic UGC.
    */

    // --- Example of how to use this system from another script (e.g., a UI Manager) ---
    /*
    public class UGCUIManager : MonoBehaviour
    {
        private string currentLevelIdSelected; // Imagine this is set by clicking a UI element

        private void Start()
        {
            // Ensure the UGC system is initialized and accessible
            if (UserGeneratedContentSystem.Instance == null)
            {
                Debug.LogError("UserGeneratedContentSystem not found in scene!");
                return;
            }

            // Example: Hooking up UI button events (conceptual)
            // myCreateLevelButton.onClick.AddListener(OnCreateNewLevelClicked);
            // myLoadLevelButton.onClick.AddListener(() => OnLoadLevelClicked(currentLevelIdSelected));
            // myListLevelsButton.onClick.AddListener(OnListLevelsClicked);
            // myDeleteLevelButton.onClick.AddListener(() => OnDeleteLevelClicked(currentLevelIdSelected));
            
            // Immediately list levels when UI loads
            OnListLevelsClicked();
        }

        public void OnCreateNewLevelClicked()
        {
            // Simulate gathering data from UI input fields and a level editor
            string levelName = "New UI Level";   // e.g., from InputField levelNameInput.text
            string authorName = "UIGuy";        // e.g., from InputField authorNameInput.text
            
            // Imagine uiLevelPieces is generated by a visual level editor
            List<LevelPieceData> uiLevelPieces = new List<LevelPieceData>
            {
                new LevelPieceData("BasicBlock", new Vector3(0, 0, 0), Quaternion.identity, Vector3.one),
                new LevelPieceData("Ramp", new Vector3(2, 0, 1), Quaternion.Euler(0, 45, 0), Vector3.one),
                new LevelPieceData("PowerUp", new Vector3(0, 1, 0), Quaternion.identity, Vector3.one * 0.3f)
            }; 

            UGCLevelData newLevel = new UGCLevelData(levelName, authorName, uiLevelPieces);
            if (UserGeneratedContentSystem.Instance.SaveUGCLevel(newLevel))
            {
                Debug.Log($"UI: Level '{levelName}' saved successfully!");
                // Optionally, refresh the UI list of levels after saving
                OnListLevelsClicked(); 
            }
            else
            {
                Debug.LogError($"UI: Failed to save level '{levelName}'. Check console for details.");
            }
        }

        public void OnListLevelsClicked()
        {
            List<UGCLevelData> levels = UserGeneratedContentSystem.Instance.ListAllUGCLevels();
            Debug.Log("UI: Displaying available levels:");
            // Here you would typically clear existing UI elements and populate a scroll view
            // or list with new UI elements (e.g., 'LevelCard' prefabs) for each level.
            foreach (var level in levels)
            {
                Debug.Log($"- {level.levelName} by {level.author} (ID: {level.levelId})");
                // Example: Create a UI button for each level and assign its ID to 'currentLevelIdSelected' on click.
            }
            if (levels.Count > 0)
            {
                currentLevelIdSelected = levels[0].levelId; // Select the first one for demonstration
                Debug.Log($"UI: Automatically selected level: {levels[0].levelName}");
            }
        }

        public void OnLoadLevelClicked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogWarning("UI: No level selected to load.");
                return;
            }

            UGCLevelData loadedLevel = UserGeneratedContentSystem.Instance.LoadUGCLevel(levelId);
            if (loadedLevel != null)
            {
                Debug.Log($"UI: Loaded level '{loadedLevel.levelName}'. Now instantiating it in game...");
                UserGeneratedContentSystem.Instance.InstantiateUGCLevelInGame(loadedLevel);
            }
            else
            {
                Debug.LogError($"UI: Failed to load level with ID '{levelId}'.");
            }
        }

        public void OnDeleteLevelClicked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogWarning("UI: No level selected to delete.");
                return;
            }

            if (UserGeneratedContentSystem.Instance.DeleteUGCLevel(levelId))
            {
                Debug.Log($"UI: Level with ID '{levelId}' deleted successfully.");
                // Refresh the UI list after deletion
                OnListLevelsClicked();
                currentLevelIdSelected = null; // Clear selection
            }
            else
            {
                Debug.LogError($"UI: Failed to delete level with ID '{levelId}'.");
            }
        }
    }
    */
}
```