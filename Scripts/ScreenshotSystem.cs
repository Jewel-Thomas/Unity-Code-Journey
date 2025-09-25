// Unity Design Pattern Example: ScreenshotSystem
// This script demonstrates the ScreenshotSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of a 'ScreenshotSystem' design pattern. It centralizes screenshot functionality, making it reusable, configurable, and easy to integrate into any Unity project.

**ScreenshotSystem Design Pattern Explained:**

The 'ScreenshotSystem' isn't a standard Gang-of-Four design pattern, but rather a common architectural pattern for managing a specific feature (screenshots) within a game engine like Unity. It typically involves:

1.  **Singleton Access:** A single, globally accessible instance to ensure consistent behavior and easy API calls from anywhere in the project.
2.  **Encapsulation:** All screenshot-related logic (capture, encoding, saving, error handling) is contained within this system.
3.  **Configuration:** Exposing settings (resolution, format, path) in the Unity Inspector for easy customization without code changes.
4.  **Asynchronous Operation:** Screenshots can be resource-intensive. Performing the operations (especially file I/O) asynchronously (via Unity Coroutines in this case) prevents the game from freezing.
5.  **Events/Callbacks:** Providing events that other systems can subscribe to, allowing them to react to successful captures or failures (e.g., displaying UI feedback, logging).
6.  **Robustness:** Handling potential errors like disk full, permission issues, or invalid paths.

---

```csharp
using UnityEngine;
using System;
using System.IO;
using System.Collections; // Required for Coroutines

/// <summary>
/// The ScreenshotSystem design pattern provides a centralized, reusable, and configurable
/// way to manage screenshot operations within a Unity project.
///
/// Key characteristics of this pattern:
/// 1.  Singleton Access: A single, globally accessible instance ensures consistent behavior.
/// 2.  Encapsulation: All screenshot logic (capture, encoding, saving, error handling)
///     is self-contained within this system.
/// 3.  Configuration: Settings like resolution, file format, and save path are
///     exposed in the Inspector for easy customization.
/// 4.  Asynchronous Operation: Uses Unity Coroutines to perform potentially
///     long-running tasks (like file I/O) without freezing the game.
/// 5.  Event-Driven Feedback: Provides events for other systems to subscribe to,
///     allowing them to react to successful captures or failures (e.g., UI notifications).
/// 6.  Error Handling: Includes basic error handling for common issues like file access.
///
/// To use:
/// 1.  Create an empty GameObject in your scene (e.g., "Managers" or "ScreenshotManager").
/// 2.  Attach this 'ScreenshotSystem' script to it.
/// 3.  Configure desired settings in the Inspector.
/// 4.  Call 'ScreenshotSystem.Instance.TakeScreenshot()' from any other script.
/// 5.  Optionally, subscribe to 'OnScreenshotTaken' and 'OnScreenshotFailed' events
///     to get feedback.
/// </summary>
public class ScreenshotSystem : MonoBehaviour
{
    // --- Singleton Implementation ---
    private static ScreenshotSystem _instance;

    /// <summary>
    /// Provides the singleton instance of the ScreenshotSystem.
    /// Access this system via `ScreenshotSystem.Instance`.
    /// </summary>
    public static ScreenshotSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<ScreenshotSystem>();

                if (_instance == null)
                {
                    // If no instance exists, create a new GameObject and add the component
                    GameObject singletonObject = new GameObject("ScreenshotSystem");
                    _instance = singletonObject.AddComponent<ScreenshotSystem>();
                    Debug.Log($"[ScreenshotSystem] New instance created on GameObject '{singletonObject.name}'.");
                }
                else
                {
                    Debug.Log($"[ScreenshotSystem] Found existing instance on GameObject '{_instance.name}'.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure only one instance exists. If another one tries to awake, destroy it.
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[ScreenshotSystem] Duplicate instance found! Destroying GameObject '{gameObject.name}'.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        // Optional: Uncomment this line if you want the ScreenshotSystem to persist across scene loads.
        // This is useful for systems that should always be available.
        // Don'tDestroyOnLoad(gameObject);

        // Ensure the base screenshot directory exists when the system initializes.
        // If the path is changed later, it will be re-checked before saving.
        string fullDirPath = Path.Combine(Application.persistentDataPath, _screenshotSubfolder);
        if (!Directory.Exists(fullDirPath))
        {
            try
            {
                Directory.CreateDirectory(fullDirPath);
                Debug.Log($"[ScreenshotSystem] Created screenshot directory: {fullDirPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenshotSystem] Failed to create screenshot directory '{fullDirPath}'. Error: {e.Message}");
            }
        }
    }

    // --- Configuration (Editable in Unity Inspector) ---
    [Header("Screenshot Settings")]
    [Tooltip("The subfolder within Application.persistentDataPath where screenshots will be saved.")]
    [SerializeField] private string _screenshotSubfolder = "Screenshots";

    [Tooltip("Prefix for screenshot filenames (e.g., 'MyGame_').")]
    [SerializeField] private string _filenamePrefix = "Screenshot_";

    [Tooltip("Resolution multiplier (e.g., 1 for screen resolution, 2 for 2x, 4 for 4x).")]
    [SerializeField] private int _resolutionMultiplier = 1;

    /// <summary>
    /// Defines the supported image file formats for screenshots.
    /// </summary>
    public enum ImageFileFormat { PNG, JPG }
    [Tooltip("The file format for the saved screenshots.")]
    [SerializeField] private ImageFileFormat _fileFormat = ImageFileFormat.PNG;

    [Tooltip("JPEG quality (0-100) if JPG format is selected. Higher values mean larger files and better quality.")]
    [Range(0, 100)]
    [SerializeField] private int _jpegQuality = 90;

    [Tooltip("If true, a debug message will be logged to console on success/failure.")]
    [SerializeField] private bool _logFeedbackToConsole = true;

    // --- Events for External Subscribers ---
    /// <summary>
    /// Event fired when a screenshot is successfully taken and saved.
    /// Provides the full path to the saved file.
    /// Subscribe to this event to receive success notifications.
    /// </summary>
    public static event Action<string> OnScreenshotTaken;

    /// <summary>
    /// Event fired when a screenshot operation fails.
    /// Provides an error message and the exception that occurred.
    /// Subscribe to this event to handle errors.
    /// </summary>
    public static event Action<string, Exception> OnScreenshotFailed;

    // --- Internal State ---
    private bool _isCapturing = false; // Flag to prevent multiple concurrent capture operations.

    // --- Public API ---
    /// <summary>
    /// Initiates a screenshot capture and save operation.
    /// This method is the primary entry point for requesting a screenshot.
    /// </summary>
    /// <param name="showFeedback">Overrides the inspector setting to show/hide console feedback for this specific call.</param>
    public void TakeScreenshot(bool showFeedback = true)
    {
        if (_isCapturing)
        {
            if (_logFeedbackToConsole)
            {
                Debug.LogWarning("[ScreenshotSystem] Already capturing a screenshot. Please wait.");
            }
            return; // Exit if a capture is already in progress.
        }

        _isCapturing = true; // Set flag to indicate capture is starting.
        StartCoroutine(CaptureAndSaveRoutine(showFeedback)); // Begin the asynchronous process.
    }

    // --- Internal Coroutine for Asynchronous Capture and Save ---
    /// <summary>
    /// This coroutine handles the entire screenshot process:
    /// 1. Generating a unique filename and path.
    /// 2. Capturing the screen into a Texture2D.
    /// 3. Encoding the Texture2D into the specified image format (PNG/JPG).
    /// 4. Saving the encoded bytes to a file on disk.
    /// 5. Notifying subscribers via events and logging feedback.
    /// </summary>
    /// <param name="showFeedback">Whether to show console feedback for this specific operation.</param>
    private IEnumerator CaptureAndSaveRoutine(bool showFeedback)
    {
        string fullPath = ""; // Initialize to empty string for error reporting clarity.
        try
        {
            // 1. Generate unique filename and path
            string filename = GenerateFilename();
            fullPath = Path.Combine(Application.persistentDataPath, _screenshotSubfolder, filename);

            // Ensure the target directory for this specific screenshot exists.
            // This handles cases where the subfolder might be changed during runtime.
            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                if (_logFeedbackToConsole && showFeedback)
                {
                    Debug.Log($"[ScreenshotSystem] Created directory for screenshot: {directoryPath}");
                }
            }

            // 2. Capture the screen texture
            // ScreenCapture.CaptureScreenshotAsTexture() captures the screen and returns a Texture2D.
            // This approach is more flexible than ScreenCapture.CaptureScreenshot() (which saves directly),
            // as it allows manipulation (e.g., resizing, applying effects) before saving.
            Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture(_resolutionMultiplier);

            // Yield control for a frame. While CaptureScreenshotAsTexture is mostly synchronous,
            // a small yield can sometimes help ensure the texture data is fully ready,
            // especially in more complex rendering pipelines or with async GPU readbacks.
            yield return null; 

            byte[] bytes;
            // 3. Encode the texture into the desired format
            // Encoding can be CPU-intensive for large textures, but is done off the main thread if possible,
            // or quickly within the coroutine.
            if (_fileFormat == ImageFileFormat.PNG)
            {
                bytes = screenTexture.EncodeToPNG();
            }
            else // JPG
            {
                bytes = screenTexture.EncodeToJPG(_jpegQuality);
            }

            // Important: Clean up the temporary Texture2D immediately after encoding
            // to prevent memory leaks.
            Destroy(screenTexture);

            // 4. Save the bytes to file
            // File.WriteAllBytes is a synchronous operation. For extremely large files
            // or high-performance requirements, you might consider offloading this
            // to a separate C# Task/Thread to avoid any potential hitch on the main thread.
            // However, for typical screenshot sizes, this is usually acceptable within a coroutine.
            File.WriteAllBytes(fullPath, bytes);

            // 5. Notify success
            if (_logFeedbackToConsole && showFeedback)
            {
                Debug.Log($"[ScreenshotSystem] Screenshot successfully saved: {fullPath}");
            }

            // Invoke the success event, providing the full path to the new file.
            OnScreenshotTaken?.Invoke(fullPath);
        }
        catch (Exception e)
        {
            // 6. Notify failure
            string errorMessage = $"[ScreenshotSystem] Failed to save screenshot to {fullPath}. Error: {e.Message}";
            if (_logFeedbackToConsole && showFeedback)
            {
                Debug.LogError(errorMessage);
            }
            // Invoke the failure event, providing an error message and the exception.
            OnScreenshotFailed?.Invoke(errorMessage, e);
        }
        finally
        {
            // Always reset the capturing flag, regardless of success or failure,
            // so new screenshots can be taken.
            _isCapturing = false;
        }
    }

    /// <summary>
    /// Generates a unique filename for the screenshot based on the configured prefix
    /// and a timestamp to avoid overwriting previous captures.
    /// </summary>
    /// <returns>A string representing the unique filename (e.g., "Screenshot_2023-10-27_14-30-05.png").</returns>
    private string GenerateFilename()
    {
        // Format: Prefix_YYYY-MM-DD_HH-MM-SS.Extension
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string extension = _fileFormat == ImageFileFormat.PNG ? ".png" : ".jpg";
        return $"{_filenamePrefix}{timestamp}{extension}";
    }

    // --- Example Usage for Testing/Debugging ---
    void Update()
    {
        // Example: Press the 'K' key to trigger a screenshot capture.
        // This demonstrates how a simple input system might interact with the ScreenshotSystem.
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_logFeedbackToConsole)
            {
                Debug.Log("[ScreenshotSystem] 'K' key pressed. Attempting to take screenshot...");
            }
            TakeScreenshot(true); // Call the public API, showing feedback.
        }
    }
}

/*
/// --- How to Implement and Use the ScreenshotSystem in Your Unity Project ---

/// 1. Setup in Unity Editor:
///    a. Create an empty GameObject in your scene (e.g., name it "SystemManagers").
///    b. Drag and drop the 'ScreenshotSystem.cs' script onto this new GameObject.
///    c. In the Inspector, configure the settings for the ScreenshotSystem:
///       - Screenshot Subfolder: e.g., "GameScreenshots"
///       - Filename Prefix: e.g., "MyGame_"
///       - Resolution Multiplier: 1 (for native resolution), 2 (for 2x), etc.
///       - Image File Format: PNG (recommended for quality) or JPG (for smaller file sizes).
///       - JPEG Quality: (If JPG selected) 0-100.
///       - Log Feedback To Console: Check this for debugging output.

/// 2. Taking a Screenshot from Another Script (e.g., a UI button handler, or a game event):

///    Example: A script that handles a UI button click to take a screenshot.
///    Attach this script to a UI Button GameObject or any other GameObject that needs to take screenshots.
///
///    public class ScreenshotButtonHandler : MonoBehaviour
///    {
///        // You can link this method directly to a UI Button's OnClick() event in the Inspector.
///        public void OnClickTakeScreenshot()
///        {
///            Debug.Log("[ScreenshotButtonHandler] UI Button clicked. Requesting screenshot...");
///            // The most straightforward way to request a screenshot is to call the public method
///            // on the ScreenshotSystem's singleton instance.
///            ScreenshotSystem.Instance.TakeScreenshot(true); // Pass 'true' to ensure console feedback is shown for this specific call.
///        }
///
///        // Optional: Subscribe to events to get feedback on screenshot operations.
///        // This is crucial for showing UI notifications, handling errors, etc.
///        private void OnEnable()
///        {
///            // Subscribe to the static events when this component becomes enabled.
///            ScreenshotSystem.OnScreenshotTaken += HandleScreenshotSuccess;
///            ScreenshotSystem.OnScreenshotFailed += HandleScreenshotError;
///        }
///
///        private void OnDisable()
///        {
///            // Unsubscribe from the static events when this component is disabled or destroyed
///            // to prevent memory leaks and ensure event handlers are not called on a null object.
///            ScreenshotSystem.OnScreenshotTaken -= HandleScreenshotSuccess;
///            ScreenshotSystem.OnScreenshotFailed -= HandleScreenshotError;
///        }
///
///        private void HandleScreenshotSuccess(string filePath)
///        {
///            Debug.Log($"[ScreenshotButtonHandler] Screenshot successfully saved to: {filePath}");
///            // Example: Display a temporary UI message to the user.
///            // UIManager.Instance.ShowNotification("Screenshot Saved!", 2f);
///        }
///
///        private void HandleScreenshotError(string errorMessage, Exception ex)
///        {
///            Debug.LogError($"[ScreenshotButtonHandler] Screenshot failed! {errorMessage}");
///            // Example: Display an error message to the user.
///            // UIManager.Instance.ShowErrorMessage("Failed to save screenshot. Please check disk space.", 5f);
///        }
///    }

/// 3. Where Screenshots Are Saved:
///    Screenshots will be saved in a subfolder within Unity's `Application.persistentDataPath`.
///    The exact location varies by platform:
///    - **Windows:** `C:\Users\<username>\AppData\LocalLow\<companyname>\<productname>\Screenshots\`
///    - **macOS:** `~/Library/Application Support/<companyname>/<productname>/Screenshots/`
///    - **Android:** `/storage/emulated/0/Android/data/<packagename>/files/Screenshots/`
///    - **iOS:** Application's sandboxed data folder (access typically via iTunes File Sharing for debugging)
///    - **Linux:** `~/.config/unity3d/<companyname>/<productname>/Screenshots/`
///
///    `Application.persistentDataPath` is the recommended path for user-generated content that needs to persist across game sessions.
*/
```