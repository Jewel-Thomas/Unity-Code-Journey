// Unity Design Pattern Example: StreamingVoiceOvers
// This script demonstrates the StreamingVoiceOvers pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'Streaming Voice-Overs' design pattern in Unity focuses on efficiently playing a sequence of audio (like dialogue or narration) by managing a queue, loading audio asynchronously, and crucially, **pre-loading the *next* audio clip while the *current* one is still playing.** This minimizes perceived loading delays, providing a smooth and continuous audio experience, especially important for large audio files or those streamed from a web server.

### Key Aspects of the Streaming Voice-Overs Pattern:

1.  **Queue Management:** Audio requests are placed into a queue, ensuring they are played in the correct order.
2.  **Asynchronous Loading:** Audio clips (especially large ones or those from web URLs) are loaded in the background using coroutines or asynchronous operations, preventing the main thread from blocking.
3.  **Pre-loading/Buffering:** While one voice-over is playing, the system actively starts loading the *next* voice-over from the queue. By the time the current voice-over finishes, the next one is likely already loaded or significantly buffered, allowing for an immediate transition.
4.  **Resource Management:** Load audio only when needed and release resources after playback to manage memory effectively.
5.  **Event-Driven Communication:** The system notifies other parts of the game (e.g., UI, character animations, dialogue manager) about the status of voice-overs (started, finished, error) using events.
6.  **Flexible Sources:** Support for different audio sources, such as local `Resources` folders or remote `Web URLs`.

---

Here's a complete C# Unity script implementing the Streaming Voice-Overs pattern:

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking; // Required for UnityWebRequestMultimedia

// Define the type of source for the voice-over audio
public enum VoiceOverLoadType
{
    ResourcesPath,  // Load from Unity's Resources folder
    WebUrl          // Stream from a URL (e.g., web server, CDN)
}

/// <summary>
/// Represents a single voice-over request in the queue.
/// This struct holds all necessary information to load and play a specific audio clip.
/// </summary>
[System.Serializable] // Make it serializable for potential editor use if needed
public class VoiceOverRequest
{
    public string identifier;               // A unique ID or description for this voice-over (e.g., "IntroDialogue_Line1")
    public VoiceOverLoadType loadType;      // How the audio should be loaded (from Resources or Web URL)
    public string pathOrUrl;                // Path in Resources (e.g., "VoiceOvers/intro") or full URL (e.g., "http://example.com/audio/line1.mp3")
    public float volume;                    // Playback volume for this specific voice-over (0.0 to 1.0)
    public float pitch;                     // Playback pitch for this specific voice-over (e.g., 1.0 for normal)
    public float delayBeforePlaying;        // Delay in seconds before starting playback after loading (useful for pacing)
    
    [NonSerialized] // Don't serialize this field, it's populated at runtime during pre-loading
    public AudioClip preloadedClip;         // The actual AudioClip object, once loaded into memory

    /// <summary>
    /// Constructor for a VoiceOverRequest.
    /// </summary>
    public VoiceOverRequest(string identifier, VoiceOverLoadType loadType, string pathOrUrl, float volume = 1f, float pitch = 1f, float delayBeforePlaying = 0f)
    {
        this.identifier = identifier;
        this.loadType = loadType;
        this.pathOrUrl = pathOrUrl;
        this.volume = volume;
        this.pitch = pitch;
        this.delayBeforePlaying = delayBeforePlaying;
        this.preloadedClip = null; // Ensure it starts null, will be populated by the manager
    }
}

/// <summary>
/// Manages the streaming and playback of a sequence of voice-overs.
/// Implements the Streaming Voice-Overs design pattern by:
/// 1. Asynchronously loading audio clips.
/// 2. Managing a queue of voice-over requests.
/// 3. Pre-loading the *next* audio clip while the *current* one is playing
///    to minimize perceived loading delays and provide a smooth experience.
/// 4. Notifying other systems via events (e.g., start, finish, error).
/// 5. Handling different audio sources (Unity Resources, Web URL).
/// </summary>
[RequireComponent(typeof(AudioSource))] // Ensures an AudioSource component is always present
public class StreamingVoiceOversManager : MonoBehaviour
{
    // Singleton pattern for easy global access to the manager instance
    public static StreamingVoiceOversManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Tooltip("The AudioSource component used for playing voice-overs.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Global volume multiplier for all voice-overs. (0.0 to 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float globalVolume = 1.0f;

    [Header("Queue Management")]
    [Tooltip("Maximum number of voice-overs to keep in the queue. 0 for unlimited.")]
    [SerializeField] private int maxQueueSize = 10;

    // Internal queue for voice-over requests
    private Queue<VoiceOverRequest> voiceOverQueue = new Queue<VoiceOverRequest>();
    private Coroutine processingCoroutine; // Reference to the main queue processing coroutine
    private Coroutine preloadingCoroutine; // Reference to the coroutine currently pre-loading the next clip
    private VoiceOverRequest currentPlayingRequest; // The request currently being played

    // --- Public Events for External Systems ---
    // These events allow other scripts (e.g., Dialogue UI, Game State Manager)
    // to react to voice-over events without needing direct references to the manager.
    public static event Action<string> OnVoiceOverStarted;              // Passes the identifier of the started VO
    public static event Action<string> OnVoiceOverFinished;             // Passes the identifier of the finished VO
    public static event Action<string, string> OnVoiceOverError;        // Passes identifier and error message for failed VOs
    public static event Action OnQueueFinished;                         // Invoked when the entire queue is empty and playback stops
    public static event Action OnQueueCleared;                          // Invoked when the queue is manually cleared

    void Awake()
    {
        // Implement singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
        Instance = this;
        // Optional: Keep the manager across scene loads if it's a global system (e.g., dialogue manager)
        DontDestroyOnLoad(gameObject); 

        // Get or add AudioSource component. RequireComponent ensures it's there, but this adds robustness.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Configure AudioSource defaults for voice-overs
        audioSource.playOnAwake = false; // Don't play automatically on start
        audioSource.loop = false;       // Voice-overs typically don't loop
        audioSource.spatialBlend = 0f;  // Usually 2D audio for dialogue
    }

    void OnDestroy()
    {
        // Clean up static instance on destroy to prevent issues if object is destroyed and recreated
        if (Instance == this)
        {
            Instance = null;
        }
        // It's good practice to stop all coroutines managed by this component upon destruction
        StopAllCoroutines();
    }

    /// <summary>
    /// Enqueues a new voice-over request to be played.
    /// If the queue is empty and no voice-over is currently processing, it starts playing immediately.
    /// </summary>
    /// <param name="request">The VoiceOverRequest object to enqueue.</param>
    public void EnqueueVoiceOver(VoiceOverRequest request)
    {
        // Check if the queue has reached its maximum size
        if (maxQueueSize > 0 && voiceOverQueue.Count >= maxQueueSize)
        {
            Debug.LogWarning($"[StreamingVoiceOversManager] Queue is full (max {maxQueueSize}). Request '{request.identifier}' was not added.");
            OnVoiceOverError?.Invoke(request.identifier, "Queue is full. Voice-over not added.");
            return;
        }

        voiceOverQueue.Enqueue(request);
        Debug.Log($"[StreamingVoiceOversManager] Enqueued voice-over: {request.identifier}. Current queue size: {voiceOverQueue.Count}");

        // If no processing coroutine is active, start it now
        if (processingCoroutine == null)
        {
            processingCoroutine = StartCoroutine(ProcessVoiceOverQueue());
        }
        else // If processing, attempt to pre-load the next item if possible
        {
            TryPreloadNext();
        }
    }

    /// <summary>
    /// Convenience method to enqueue a voice-over with common parameters.
    /// Creates a new VoiceOverRequest internally.
    /// </summary>
    public void EnqueueVoiceOver(string identifier, VoiceOverLoadType loadType, string pathOrUrl, float volume = 1f, float pitch = 1f, float delayBeforePlaying = 0f)
    {
        VoiceOverRequest newRequest = new VoiceOverRequest(identifier, loadType, pathOrUrl, volume, pitch, delayBeforePlaying);
        EnqueueVoiceOver(newRequest);
    }

    /// <summary>
    /// Stops the currently playing voice-over and clears the entire queue.
    /// Resets the manager to an idle state.
    /// </summary>
    public void ClearQueue()
    {
        // Stop all related coroutines (processing and pre-loading)
        StopAllCoroutines(); 
        processingCoroutine = null;
        preloadingCoroutine = null;

        audioSource.Stop(); // Stop current audio playback
        if (audioSource.clip != null)
        {
            audioSource.clip = null; // Release the AudioClip from the AudioSource
        }

        // Notify that the currently playing voice-over (if any) was interrupted
        if (currentPlayingRequest != null)
        {
            Debug.Log($"[StreamingVoiceOversManager] Current voice-over '{currentPlayingRequest.identifier}' stopped due to queue clear.");
            OnVoiceOverFinished?.Invoke(currentPlayingRequest.identifier); 
            currentPlayingRequest = null;
        }

        voiceOverQueue.Clear(); // Clear all pending requests
        Debug.Log("[StreamingVoiceOversManager] Voice-over queue cleared.");
        OnQueueCleared?.Invoke(); // Notify external systems
    }

    /// <summary>
    /// Stops the currently playing voice-over immediately without clearing the rest of the queue.
    /// The queue will automatically proceed to the next item if available.
    /// </summary>
    public void StopCurrentVoiceOver()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop(); // Stop current playback
            if (currentPlayingRequest != null)
            {
                Debug.Log($"[StreamingVoiceOversManager] Voice-over '{currentPlayingRequest.identifier}' stopped manually.");
                OnVoiceOverFinished?.Invoke(currentPlayingRequest.identifier); // Notify it finished (or was interrupted)
                currentPlayingRequest = null;
            }
            audioSource.clip = null; // Release the clip
        }
        
        // Stop any ongoing pre-loading, as the queue will re-evaluate what to preload
        if (preloadingCoroutine != null)
        {
            StopCoroutine(preloadingCoroutine);
            preloadingCoroutine = null;
            Debug.Log("[StreamingVoiceOversManager] Pre-loading stopped due to current voice-over being stopped.");
        }
    }

    /// <summary>
    /// The main coroutine that processes voice-over requests from the queue.
    /// This is where the core logic of playing and pre-loading resides.
    /// </summary>
    private IEnumerator ProcessVoiceOverQueue()
    {
        Debug.Log("[StreamingVoiceOversManager] Started processing voice-over queue.");

        // Loop as long as there are items in the queue
        while (voiceOverQueue.Count > 0)
        {
            currentPlayingRequest = voiceOverQueue.Dequeue(); // Get the next voice-over to play
            Debug.Log($"[StreamingVoiceOversManager] Dequeued voice-over: {currentPlayingRequest.identifier}. Remaining in queue: {voiceOverQueue.Count}");

            // --- Voice-over Loading ---
            // Check if the audio clip for the current request was already preloaded
            if (currentPlayingRequest.preloadedClip != null)
            {
                Debug.Log($"[StreamingVoiceOversManager] Using preloaded clip for: {currentPlayingRequest.identifier}");
            }
            else // If not preloaded (e.g., first item in queue, or pre-loading was skipped/failed)
            {
                // Start a coroutine to load the audio clip and wait for it to complete.
                // The LoadAudioClipInternal coroutine will populate currentPlayingRequest.preloadedClip.
                yield return StartCoroutine(LoadAudioClipInternal(currentPlayingRequest));
                
                // If loading failed (preloadedClip is still null), log an error and skip to the next item
                if (currentPlayingRequest.preloadedClip == null)
                {
                    OnVoiceOverError?.Invoke(currentPlayingRequest.identifier, "Failed to load audio clip.");
                    currentPlayingRequest = null; // Clear current request as it failed
                    continue; // Skip to the next item in the queue
                }
            }

            // Apply any specified delay before starting playback
            if (currentPlayingRequest.delayBeforePlaying > 0)
            {
                Debug.Log($"[StreamingVoiceOversManager] Waiting {currentPlayingRequest.delayBeforePlaying}s before playing '{currentPlayingRequest.identifier}'.");
                yield return new WaitForSeconds(currentPlayingRequest.delayBeforePlaying);
            }

            // --- Voice-over Playback ---
            audioSource.clip = currentPlayingRequest.preloadedClip;
            audioSource.volume = currentPlayingRequest.volume * globalVolume; // Apply individual and global volume
            audioSource.pitch = currentPlayingRequest.pitch;
            audioSource.Play();
            
            OnVoiceOverStarted?.Invoke(currentPlayingRequest.identifier); // Notify subscribers
            Debug.Log($"[StreamingVoiceOversManager] Started playing: {currentPlayingRequest.identifier}");

            // --- Streaming/Pre-loading Aspect ---
            // IMPORTANT: While the current voice-over is playing, immediately try to pre-load
            // the *next* one in the queue. This is the core of the "Streaming Voice-Overs" pattern.
            // By doing this, the next clip will likely be ready or almost ready when the current one finishes.
            TryPreloadNext();

            // Wait until the current voice-over finishes playing
            yield return new WaitWhile(() => audioSource.isPlaying);

            // --- Voice-over Completion ---
            Debug.Log($"[StreamingVoiceOversManager] Finished playing: {currentPlayingRequest.identifier}");
            OnVoiceOverFinished?.Invoke(currentPlayingRequest.identifier); // Notify subscribers

            // Clean up: Release the clip from the AudioSource and nullify the preloaded clip
            // This helps with memory management, especially for web-streamed clips.
            audioSource.clip = null;
            currentPlayingRequest.preloadedClip = null; 
            currentPlayingRequest = null; // No request is currently playing

            // Stop the pre-loading coroutine if it was running, as the next item (if any)
            // will now become the current, or the queue might be empty.
            if (preloadingCoroutine != null)
            {
                StopCoroutine(preloadingCoroutine);
                preloadingCoroutine = null;
            }
        }

        // If the loop finishes, the queue is now empty
        Debug.Log("[StreamingVoiceOversManager] Voice-over queue finished processing. All items played.");
        OnQueueFinished?.Invoke(); // Notify external systems
        processingCoroutine = null; // Indicate that processing has stopped
    }

    /// <summary>
    /// Attempts to start a pre-loading coroutine for the next item in the queue,
    /// but only if there's a next item and no pre-loading is currently active for it.
    /// </summary>
    private void TryPreloadNext()
    {
        // Only preload if there's at least one item waiting in the queue AND 
        // no preloading coroutine is currently active.
        if (voiceOverQueue.Count > 0 && preloadingCoroutine == null)
        {
            VoiceOverRequest nextRequest = voiceOverQueue.Peek(); // Look at the next item without removing it
            
            // Only preload if the clip hasn't been preloaded yet for this specific request
            if (nextRequest.preloadedClip == null)
            {
                Debug.Log($"[StreamingVoiceOversManager] Attempting to pre-load next voice-over: {nextRequest.identifier}");
                // Start a coroutine to load the audio and populate the request's preloadedClip field
                preloadingCoroutine = StartCoroutine(LoadAudioClipInternal(nextRequest));
            }
            else
            {
                Debug.Log($"[StreamingVoiceOversManager] Next voice-over '{nextRequest.identifier}' already preloaded.");
            }
        }
    }

    /// <summary>
    /// Handles the asynchronous loading of an audio clip based on its load type (Resources or Web URL).
    /// This coroutine directly populates the `preloadedClip` field of the provided `VoiceOverRequest`.
    /// </summary>
    /// <param name="request">The voice-over request whose preloadedClip field will be populated.</param>
    private IEnumerator LoadAudioClipInternal(VoiceOverRequest request)
    {
        request.preloadedClip = null; // Ensure clip is null before attempting to load

        switch (request.loadType)
        {
            case VoiceOverLoadType.ResourcesPath:
                // Asynchronously load the AudioClip from the Resources folder
                ResourceRequest resourceRequest = Resources.LoadAsync<AudioClip>(request.pathOrUrl);
                yield return resourceRequest; // Wait for the load operation to complete

                if (resourceRequest.asset != null)
                {
                    request.preloadedClip = resourceRequest.asset as AudioClip;
                    Debug.Log($"[StreamingVoiceOversManager] Loaded from Resources: {request.pathOrUrl}");
                }
                else
                {
                    Debug.LogError($"[StreamingVoiceOversManager] Failed to load audio from Resources path: '{request.pathOrUrl}' for request '{request.identifier}'. Make sure it's in a 'Resources' folder and the path is correct (without file extension).");
                }
                break;

            case VoiceOverLoadType.WebUrl:
                // Use UnityWebRequestMultimedia.GetAudioClip for streaming audio from a URL.
                // AudioType.MPEG is suitable for .mp3 files. For .wav, use AudioType.WAV.
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(request.pathOrUrl, AudioType.MPEG))
                {
                    Debug.Log($"[StreamingVoiceOversManager] Attempting to load from URL: {request.pathOrUrl}");
                    yield return www.SendWebRequest(); // Wait for the web request to complete

                    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError($"[StreamingVoiceOversManager] Error loading audio from URL: '{request.pathOrUrl}' for request '{request.identifier}'. Error: {www.error}");
                        OnVoiceOverError?.Invoke(request.identifier, $"Web request failed: {www.error}");
                    }
                    else
                    {
                        // Get the AudioClip content from the successful web request
                        request.preloadedClip = DownloadHandlerAudioClip.GetContent(www);
                        if (request.preloadedClip != null)
                        {
                            Debug.Log($"[StreamingVoiceOversManager] Loaded from URL: {request.pathOrUrl}");
                        }
                        else
                        {
                            Debug.LogError($"[StreamingVoiceOversManager] Failed to get AudioClip content from URL: '{request.pathOrUrl}' for request '{request.identifier}'. This might indicate a corrupted file or incorrect audio type.");
                        }
                    }
                }
                break;

            default:
                Debug.LogError($"[StreamingVoiceOversManager] Unknown load type: {request.loadType} for request '{request.identifier}'.");
                break;
        }
    }

    // --- Public Utility Methods ---
    /// <summary>
    /// Returns true if any voice-over is currently playing through the AudioSource.
    /// </summary>
    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }

    /// <summary>
    /// Returns true if the voice-over queue is currently being processed.
    /// This includes loading, delaying, and playing.
    /// </summary>
    public bool IsProcessingQueue()
    {
        return processingCoroutine != null;
    }

    /// <summary>
    /// Returns the number of voice-overs currently waiting in the queue.
    /// </summary>
    public int GetQueueCount()
    {
        return voiceOverQueue.Count;
    }

    /// <summary>
    /// Returns the identifier of the voice-over currently playing, or null if none.
    /// </summary>
    public string GetCurrentPlayingVoiceOverId()
    {
        return currentPlayingRequest?.identifier;
    }
}


/// --- Example Usage (How to integrate this into another script) ---
/*
  To use the StreamingVoiceOversManager in your Unity project, follow these steps:

  1.  **Create Manager GameObject:**
      In your Unity scene, create an empty GameObject (e.g., named "VoiceOverManager").
      Attach the 'StreamingVoiceOversManager.cs' script to this GameObject.
      (An AudioSource component will be automatically added if not already present due to `[RequireComponent(typeof(AudioSource))]`).

  2.  **Prepare Audio Files (for Resources.Load examples):**
      In your Unity Project window, create a folder named 'Resources' (if you don't have one).
      Inside 'Resources', you can create a subfolder, e.g., 'VoiceOvers'.
      Drag some .mp3 or .wav audio files into the 'Resources/VoiceOvers' folder.
      For example: 'intro.mp3', 'line1.mp3', 'line2.mp3'.
      When referencing these, you'll use the path relative to 'Resources' *without* the file extension (e.g., "VoiceOvers/intro").

  3.  **Create a Client Script (e.g., a Dialogue System or Game Manager):**
      Create another empty GameObject (e.g., "GameManager").
      Create a new C# script (e.g., 'DialogueSystemExample.cs') and attach it to "GameManager".
      Copy the following 'DialogueSystemExample' code into that script.

  4.  **Run the Scene:**
      Play your scene. Observe the Console for messages about voice-over loading, playing, and pre-loading.
      Use the key presses defined in the example to test different functionalities.

  --- DialogueSystemExample.cs ---
*/
/*
using UnityEngine;
using System.Collections; // Required for StartCoroutine

public class DialogueSystemExample : MonoBehaviour
{
    void Start()
    {
        // --- IMPORTANT: Subscribe to events ---
        // Subscribing to these static events allows this script to react to voice-over
        // events (start, finish, error) without needing a direct reference to the manager.
        // This promotes loose coupling, a key benefit of the event-driven approach.
        StreamingVoiceOversManager.OnVoiceOverStarted += HandleVoiceOverStarted;
        StreamingVoiceOversManager.OnVoiceOverFinished += HandleVoiceOverFinished;
        StreamingVoiceOversManager.OnVoiceOverError += HandleVoiceOverError;
        StreamingVoiceOversManager.OnQueueFinished += HandleQueueFinished;
        StreamingVoiceOversManager.OnQueueCleared += HandleQueueCleared;

        // Ensure the StreamingVoiceOversManager instance exists in the scene
        if (StreamingVoiceOversManager.Instance == null)
        {
            Debug.LogError("StreamingVoiceOversManager not found in scene. Please add it to a GameObject (e.g., 'VoiceOverManager').");
            return;
        }

        EnqueueExampleVoiceOvers();
    }

    /// <summary>
    /// Enqueues a set of example voice-overs to demonstrate functionality.
    /// </summary>
    private void EnqueueExampleVoiceOvers()
    {
        Debug.Log("--- Enqueuing Resources Voice-Overs ---");
        // Example 1: Enqueue voice-overs from the Unity 'Resources' folder.
        // Paths are relative to any 'Resources' folder and exclude the file extension.
        // Make sure you have audio files like "VoiceOvers/intro", "VoiceOvers/line1", "VoiceOvers/line2"
        // (without the extension, e.g., .mp3 or .wav) inside a 'Resources' folder in your Unity project.
        StreamingVoiceOversManager.Instance.EnqueueVoiceOver("Intro_Greeting", VoiceOverLoadType.ResourcesPath, "VoiceOvers/intro", 0.8f);
        StreamingVoiceOversManager.Instance.EnqueueVoiceOver("Dialogue_Line1", VoiceOverLoadType.ResourcesPath, "VoiceOvers/line1", 1.0f);
        // This voice-over includes a pitch change and a delay before playing.
        StreamingVoiceOversManager.Instance.EnqueueVoiceOver("Dialogue_Line2", VoiceOverLoadType.ResourcesPath, "VoiceOvers/line2", 0.9f, 1.05f, 0.5f); 
        StreamingVoiceOversManager.Instance.EnqueueVoiceOver("Conclusion", VoiceOverLoadType.ResourcesPath, "VoiceOvers/outro", 0.85f);


        Debug.Log("--- Enqueuing Web URL Voice-Overs ---");
        // Example 2: Enqueue voice-overs from a web URL.
        // UnityWebRequestMultimedia will stream these. Replace with actual public audio URLs for testing.
        // NOTE: These URLs are examples from soundhelix.com for demonstration.
        // Ensure the web server sends correct MIME types (e.g., audio/mpeg for .mp3).
        StreamingVoiceOversManager.Instance.EnqueueVoiceOver("Web_Song1", VoiceOverLoadType.WebUrl, "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3", 0.7f);
        StreamingVoiceOversManager.Instance.EnqueueVoiceOver("Web_Song2", VoiceOverLoadType.WebUrl, "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3", 0.6f);

        // Example 3: Enqueue a voice-over that will likely fail to load (bad path/URL).
        // This demonstrates the error handling and event notification.
        StreamingVoiceOversManager.Instance.EnqueueVoiceOver("Failed_VO", VoiceOverLoadType.ResourcesPath, "NonExistent/bad_path", 1.0f);

        // You can also enqueue requests using the VoiceOverRequest class directly for more control:
        // var customRequest = new VoiceOverRequest("CustomLine", VoiceOverLoadType.ResourcesPath, "VoiceOvers/custom_message", 0.9f);
        // StreamingVoiceOversManager.Instance.EnqueueVoiceOver(customRequest);
    }

    void Update()
    {
        // Example: Stop current voice-over and clear queue on 'Space' key press
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (StreamingVoiceOversManager.Instance.IsProcessingQueue())
            {
                Debug.Log("[DialogueSystem] Space pressed: Stopping current voice-over and clearing queue.");
                StreamingVoiceOversManager.Instance.ClearQueue();
            }
            else if (!StreamingVoiceOversManager.Instance.IsPlaying())
            {
                Debug.Log("[DialogueSystem] Space pressed: Queue is idle. Re-enqueuing example voice-overs.");
                // Re-enqueue for demonstration if the queue is finished
                EnqueueExampleVoiceOvers(); 
            }
        }
        
        // Example: Stop only the current voice-over, letting the queue continue with the next item
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (StreamingVoiceOversManager.Instance.IsPlaying())
            {
                Debug.Log("[DialogueSystem] 'S' pressed: Stopping current voice-over. Queue will continue with the next item.");
                StreamingVoiceOversManager.Instance.StopCurrentVoiceOver();
            }
        }
    }

    // --- Event Handlers ---
    // These methods are called when the corresponding events are invoked by the StreamingVoiceOversManager.
    private void HandleVoiceOverStarted(string identifier)
    {
        Debug.Log($"<color=cyan>[DialogueSystem Event]</color> Voice-over STARTED: <color=yellow>{identifier}</color>. Current playing: {StreamingVoiceOversManager.Instance.GetCurrentPlayingVoiceOverId()}");
        // Here, you would update your UI (e.g., show a character portrait, display text),
        // trigger animations, or unpause game elements.
    }

    private void HandleVoiceOverFinished(string identifier)
    {
        Debug.Log($"<color=cyan>[DialogueSystem Event]</color> Voice-over FINISHED: <color=yellow>{identifier}</color>");
        // Here, you would hide UI elements, trigger post-dialogue events,
        // or proceed to the next stage of your game logic.
    }

    private void HandleVoiceOverError(string identifier, string errorMessage)
    {
        Debug.LogError($"<color=red>[DialogueSystem Event]</color> Voice-over ERROR for '<color=yellow>{identifier}</color>': {errorMessage}");
        // Handle errors gracefully: perhaps display subtitle text instead, log to analytics,
        // or play a generic error sound.
    }

    private void HandleQueueFinished()
    {
        Debug.Log("<color=green>[DialogueSystem Event]</color> All voice-overs in the queue have finished!");
        // This is a good point to reset dialogue states, close dialogue UI, etc.
    }

    private void HandleQueueCleared()
    {
        Debug.Log("<color=orange>[DialogueSystem Event]</color> Voice-over queue was cleared manually.");
        // Perform cleanup specific to a queue clear, like hiding all dialogue related UI immediately.
    }

    void OnDisable()
    {
        // --- IMPORTANT: Unsubscribe from events ---
        // Always unsubscribe from static events when the GameObject or script is disabled or destroyed
        // to prevent memory leaks and unexpected behavior (e.g., calling methods on a destroyed object).
        StreamingVoiceOversManager.OnVoiceOverStarted -= HandleVoiceOverStarted;
        StreamingVoiceOversManager.OnVoiceOverFinished -= HandleVoiceOverFinished;
        StreamingVoiceOversManager.OnVoiceOverError -= HandleVoiceOverError;
        StreamingVoiceOversManager.OnQueueFinished -= HandleQueueFinished;
        StreamingVoiceOversManager.OnQueueCleared -= HandleQueueCleared;
    }
}
*/