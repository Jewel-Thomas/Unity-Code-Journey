// Unity Design Pattern Example: BackgroundStreaming
// This script demonstrates the BackgroundStreaming pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Background Streaming** design pattern in Unity. The core idea is to load or process large amounts of data/assets in the background, *asynchronously*, to avoid freezing the main game thread. This keeps your game responsive, preventing stuttering and improving user experience during transitions, level loading, or asset instantiation.

**Real-world Use Case:** Loading multiple chunks of a large open world, instantiating complex prefabs, or fetching data from a server.

---

### Unity Setup Instructions:

1.  **Create a C# Script:**
    *   Create a new C# script named `BackgroundStreamingManager.cs` in your Project window.
    *   Copy and paste the code below into this script.

2.  **Create UI Elements:**
    *   In your scene, create a `Canvas` (Right-click in Hierarchy -> UI -> Canvas).
    *   Inside the Canvas, create:
        *   A `Slider` (Right-click Canvas -> UI -> Slider). Position it at the top.
        *   A `Text` element (Right-click Canvas -> UI -> Text - TextMeshPro if you have it, otherwise legacy Text). Position it above the slider. Rename it `StatusText`.
        *   A `Button` (Right-click Canvas -> UI -> Button - TextMeshPro or legacy Text). Position it below the slider. Rename it `StartStreamingButton`.
    *   Adjust the `Rect Transform` of these UI elements as needed for visibility (e.g., set anchors to middle-center).
    *   Change the `Text` component on the `StartStreamingButton` to "Start Streaming".

3.  **Create a Simple Prefab (The "Chunk"):**
    *   Create a 3D Cube (Right-click in Hierarchy -> 3D Object -> Cube).
    *   Reset its position to (0,0,0).
    *   Rename it `ChunkPrefab`.
    *   Drag this `ChunkPrefab` from the Hierarchy into your Project window (e.g., into a new `Prefabs` folder) to make it a prefab.
    *   Delete the `ChunkPrefab` from your Hierarchy (it's now saved as a prefab asset).

4.  **Create an Empty GameObject for the Manager:**
    *   Create an empty GameObject in your scene (Right-click in Hierarchy -> Create Empty).
    *   Rename it `BackgroundStreamingManager`.
    *   Attach the `BackgroundStreamingManager.cs` script to this new GameObject.

5.  **Assign References in the Inspector:**
    *   Select the `BackgroundStreamingManager` GameObject in the Hierarchy.
    *   In the Inspector, drag and drop the following:
        *   `Chunk Prefab`: Drag your `ChunkPrefab` from the Project window.
        *   `Parent For Chunks`: Drag the `BackgroundStreamingManager` GameObject itself (or create another empty GameObject named `ChunkContainer` and drag that) into this field. This is where the instantiated chunks will be children of.
        *   `Progress Bar`: Drag your `Slider` from the Hierarchy.
        *   `Status Text`: Drag your `StatusText` from the Hierarchy.
        *   `Start Button`: Drag your `StartStreamingButton` from the Hierarchy.
    *   Populate `Chunk Names To Load`:
        *   Set its `Size` to, say, `10`.
        *   Fill in some arbitrary names, e.g., "Chunk_A", "Chunk_B", "Chunk_C", etc. (These are just identifiers in this example; in a real project, they might be actual file paths or asset IDs).
    *   Adjust `Simulated Load Time Per Item` (e.g., `0.1`) and `Max Items To Process Per Frame` (e.g., `2`).

6.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   Click the "Start Streaming" button.
    *   Observe the progress bar and status text updating smoothly, and the cubes appearing gradually without the game freezing.

---

### `BackgroundStreamingManager.cs` Code:

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Slider, Text, Button
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for List
using System; // Required for Action

/// <summary>
/// Demonstrates the Background Streaming design pattern in Unity.
/// This pattern allows loading or processing assets/data asynchronously
/// in the background, keeping the main game thread responsive and preventing UI freezes.
///
/// Use Cases:
/// - Loading new scenes or large sections of an open world.
/// - Instantiating multiple complex prefabs.
/// - Downloading data from a server.
/// - Processing large datasets before use.
///
/// Key Principles:
/// 1.  **Asynchronous Operation:** Work is done in a coroutine (or async/await)
///     that yields control back to Unity periodically.
/// 2.  **Responsiveness:** By yielding (`yield return null;` or `yield return new WaitForEndOfFrame();`),
///     the main thread gets cycles to render frames and process input,
///     preventing stuttering or freezing.
/// 3.  **Progress Tracking:** Provides feedback to the user on the loading status.
/// 4.  **Decoupling:** The streaming logic is separate from the game logic that initiates it.
/// </summary>
public class BackgroundStreamingManager : MonoBehaviour
{
    [Header("Asset Configuration")]
    [Tooltip("The prefab to instantiate representing a 'chunk' or loaded asset.")]
    public GameObject chunkPrefab;
    [Tooltip("The parent transform under which the instantiated chunks will be placed.")]
    public Transform parentForChunks;
    [Tooltip("A list of identifiers (e.g., names, paths) for the items to be loaded/processed.")]
    public List<string> chunkNamesToLoad = new List<string>();

    [Header("UI References")]
    [Tooltip("The UI Slider to display loading progress.")]
    public Slider progressBar;
    [Tooltip("The UI Text element to display status messages.")]
    public Text statusText;
    [Tooltip("The UI Button to start the streaming process.")]
    public Button startButton;

    [Header("Streaming Settings")]
    [Tooltip("Simulated time in seconds each item takes to 'load'.")]
    [Range(0.01f, 1.0f)]
    public float simulatedLoadTimePerItem = 0.1f;
    [Tooltip("Maximum number of items to process within a single frame before yielding control.")]
    [Range(1, 10)]
    public int maxItemsToProcessPerFrame = 2;


    // Private fields to manage the streaming state
    private bool _isStreaming = false;
    private float _currentProgress = 0f;

    /// <summary>
    /// Event fired when the background streaming process completes.
    /// External systems can subscribe to this to react to completion.
    /// </summary>
    public event Action OnStreamingComplete;

    void Awake()
    {
        // Initialize UI components
        if (progressBar != null)
        {
            progressBar.value = 0;
            progressBar.gameObject.SetActive(false); // Hide until streaming starts
        }
        if (statusText != null)
        {
            statusText.text = "Ready to Stream";
        }

        // Assign button listener
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartBackgroundStreaming);
        }

        // Basic validation
        if (chunkPrefab == null)
        {
            Debug.LogError("Chunk Prefab is not assigned! Please assign a prefab in the Inspector.", this);
            enabled = false; // Disable script if essential setup is missing
            return;
        }
        if (parentForChunks == null)
        {
            Debug.LogWarning("Parent For Chunks is not assigned. Instantiated chunks will be at world root.", this);
        }
    }

    void OnDestroy()
    {
        // Clean up button listener to prevent memory leaks
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartBackgroundStreaming);
        }
    }

    /// <summary>
    /// Initiates the background streaming process.
    /// </summary>
    public void StartBackgroundStreaming()
    {
        if (_isStreaming)
        {
            Debug.LogWarning("Already streaming. Cannot start a new stream.", this);
            return;
        }

        Debug.Log("Starting background streaming process...");
        _isStreaming = true;
        _currentProgress = 0f;

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true); // Show progress bar
            progressBar.value = 0f;
        }
        if (startButton != null)
        {
            startButton.interactable = false; // Disable button while streaming
        }

        // Start the coroutine that performs the background work
        StartCoroutine(StreamChunksCoroutine(chunkNamesToLoad));
    }

    /// <summary>
    /// The core coroutine for the background streaming.
    /// This method simulates loading and instantiating game chunks.
    /// </summary>
    /// <param name="itemsToStream">A list of identifiers for the items to process.</param>
    /// <returns>IEnumerator for coroutine management.</returns>
    private IEnumerator StreamChunksCoroutine(List<string> itemsToStream)
    {
        int totalItems = itemsToStream.Count;
        if (totalItems == 0)
        {
            Debug.LogWarning("No items to stream.", this);
            SetStreamingCompleteState();
            yield break;
        }

        int itemsProcessedThisFrame = 0;
        for (int i = 0; i < totalItems; i++)
        {
            // --- Simulate Loading/Processing ---
            // In a real scenario, this would involve:
            // 1. AssetBundle.LoadAssetAsync<T>()
            // 2. Resources.LoadAsync<T>()
            // 3. Web request (UnityWebRequest)
            // 4. File I/O operations
            // 5. Complex data calculations
            
            // For this example, we simulate by waiting a small amount of time.
            // If the actual loading operation is asynchronous (e.g., LoadAssetAsync),
            // you would yield its operation directly: `yield return asyncOperation;`
            yield return new WaitForSeconds(simulatedLoadTimePerItem);


            // --- Perform Instantiation/Post-Processing ---
            // Once the asset is "loaded" or data is processed, do the final setup.
            GameObject newChunk = Instantiate(chunkPrefab, parentForChunks);
            newChunk.name = "LoadedChunk_" + itemsToStream[i];
            
            // Give it a slightly different position so they don't all stack
            newChunk.transform.position = new Vector3(
                (i % 5) * 2f - 4f, // 5 chunks per row
                (i / 5) * 2f,     // new row after 5 chunks
                0f
            );

            // Optional: Add a random color to make them distinct
            Renderer chunkRenderer = newChunk.GetComponent<Renderer>();
            if (chunkRenderer != null)
            {
                chunkRenderer.material.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
            }

            // --- Update Progress ---
            _currentProgress = (float)(i + 1) / totalItems;
            UpdateUIProgress();

            itemsProcessedThisFrame++;

            // --- Yield Control to Main Thread ---
            // This is CRUCIAL for the Background Streaming pattern.
            // After processing a certain number of items, we yield control back to Unity.
            // This allows Unity to render a frame, process input, and keep the game responsive.
            // Without this, even a coroutine can block the main thread if it does too much work.
            if (itemsProcessedThisFrame >= maxItemsToProcessPerFrame)
            {
                yield return null; // Yield one frame to allow other Unity processes to run
                itemsProcessedThisFrame = 0; // Reset counter for the next batch
            }
        }

        // Streaming is complete
        SetStreamingCompleteState();
        Debug.Log("Background streaming completed.");
    }

    /// <summary>
    /// Helper method to update UI elements based on current streaming state.
    /// </summary>
    private void UpdateUIProgress()
    {
        if (progressBar != null)
        {
            progressBar.value = _currentProgress;
        }
        if (statusText != null)
        {
            statusText.text = $"Streaming: {(_currentProgress * 100):F0}%";
        }
    }

    /// <summary>
    /// Sets the final state of the UI and internal flags when streaming is complete.
    /// </summary>
    private void SetStreamingCompleteState()
    {
        _isStreaming = false;
        _currentProgress = 1f; // Ensure progress bar is full

        UpdateUIProgress(); // Final UI update
        if (statusText != null)
        {
            statusText.text = "Streaming Complete!";
        }
        if (startButton != null)
        {
            startButton.interactable = true; // Re-enable button
        }
        if (progressBar != null)
        {
            // progressBar.gameObject.SetActive(false); // Optionally hide progress bar
        }

        // Invoke the completion event, notifying any subscribers
        OnStreamingComplete?.Invoke();
    }
}
```