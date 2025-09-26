// Unity Design Pattern Example: StreamingAssets
// This script demonstrates the StreamingAssets pattern in Unity
// Generated automatically - ready to use in your Unity project

The `StreamingAssets` folder in Unity is a powerful design pattern for including pre-packaged data directly into your game's build. Unlike regular assets, files in `StreamingAssets` are *not* compressed, *not* processed by Unity's asset pipeline, and *not* included in Asset Bundles. They are copied *as-is* to a specific location in the build, allowing you to access them at runtime.

This makes `StreamingAssets` ideal for:
*   **Configuration Files:** JSON, XML, or plain text files that define game settings, level layouts, or data tables.
*   **Large Media Files:** Videos, audio files, or high-resolution textures that you want to stream or load directly without Unity's typical asset processing.
*   **Platform-Specific Files:** Data or binaries that need to be included untouched for certain platforms.
*   **Files for `System.IO` Operations:** Although `UnityWebRequest` is generally recommended for cross-platform compatibility, on some platforms (like PC/Mac/Linux standalone builds), you can use `System.IO.File` to read these files directly.

Below is a complete C# Unity script that demonstrates how to load a text file from the `StreamingAssets` folder, handling platform differences using `UnityWebRequest` for maximum compatibility.

---

### **`StreamingAssetsExample.cs`**

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines
using System.IO;        // Required for Path.Combine
using UnityEngine.Networking; // Required for UnityWebRequest

/*
--- HOW TO USE THIS SCRIPT (StreamingAssets Design Pattern) ---

1.  **Understand StreamingAssets:**
    *   The `StreamingAssets` folder is a special folder in a Unity project.
    *   Any files placed in this folder will be copied *as-is* to the build's data folder.
    *   They are *not* compressed, *not* processed by Unity's asset pipeline, and *not* included in Asset Bundles.
    *   **Use Cases:**
        *   Pre-packaged data that needs to be accessible at runtime (e.g., configuration files, large video/audio files, localized text).
        *   Files that need to be read directly using `System.IO` (on platforms where it's supported) or `UnityWebRequest`.
        *   Platform-specific files that you want to include without Unity processing them.
    *   **Important Considerations:**
        *   Files in `StreamingAssets` are **read-only** in a deployed build. You cannot write to them.
        *   The path to `StreamingAssets` differs by platform (`Application.streamingAssetsPath`).
        *   Accessing files from `StreamingAssets` often requires `UnityWebRequest` on platforms like Android, iOS, and WebGL, as they might be embedded in compressed archives (APK) or served from a web server. On Standalone builds (Windows, macOS, Linux, Editor), `System.IO.File` can often be used directly, but `UnityWebRequest` provides a more consistent cross-platform solution.

2.  **Setup in Unity Project:**
    a.  **Create the 'StreamingAssets' Folder:**
        *   In your Unity Project window, right-click on the "Assets" folder.
        *   Go to "Create" -> "Folder".
        *   Name it *exactly* `StreamingAssets` (case-sensitive on some platforms).
        *   The final path should be: `Assets/StreamingAssets`

    b.  **Create an Example File:**
        *   Inside the newly created `Assets/StreamingAssets` folder, create a new text file.
        *   Right-click in the folder -> "Create" -> "Text Document".
        *   Name it `example_data.txt` (or whatever you set `fileName` in the script's Inspector).
        *   Edit the file (e.g., using Notepad or any text editor) and add some content:
            ```
            This is some data loaded from StreamingAssets!
            It could be configuration, level data, dialogue, etc.
            This is line 3 of the example data.
            ```
        *   Save the file.

    c.  **Add this Script to a GameObject:**
        *   Create an empty GameObject in your scene (Right-click in Hierarchy -> "Create Empty").
        *   Rename it (e.g., "StreamingAssetsLoader").
        *   Drag and drop this `StreamingAssetsExample.cs` script onto the "StreamingAssetsLoader" GameObject in the Inspector.

    d.  **Run the Scene:**
        *   Press Play in the Unity Editor.
        *   Observe the Console window. You should see messages indicating the file path, and then the content of `example_data.txt` loaded successfully.
        *   In the Inspector of your "StreamingAssetsLoader" GameObject, the `Loaded Content` text area will also display the loaded text.

3.  **Build and Test (Important!):**
    *   Go to "File" -> "Build Settings..."
    *   Choose your target platform (e.g., Windows, Android, iOS).
    *   Build the project.
    *   Run the built application. You will see the same console output (or UI display) demonstrating that the file was correctly bundled and loaded from the `StreamingAssets` folder in a deployed build.

This script provides a robust and cross-platform way to access your pre-packaged data using the `StreamingAssets` design pattern in Unity.
*/

public class StreamingAssetsExample : MonoBehaviour
{
    // --- Public Fields for Unity Editor configuration ---
    [Tooltip("The name of the file to load from StreamingAssets (e.g., 'mydata.txt', 'config.json').")]
    public string fileName = "example_data.txt";

    [Tooltip("Content loaded from the StreamingAssets file. Displayed in the Inspector for debugging.")]
    [TextArea(5, 10)] // Make it a multi-line text area in the inspector
    public string loadedContent = "No content loaded yet.";

    // --- Unity Lifecycle Method ---
    void Start()
    {
        Debug.Log($"<color=cyan>StreamingAssets Example Started:</color> Attempting to load '{fileName}'");
        StartCoroutine(LoadStreamingAssetFile()); // Start the coroutine to load the file asynchronously
    }

    // --- Core Coroutine to Load the File ---
    private IEnumerator LoadStreamingAssetFile()
    {
        string filePath;

        // Step 1: Construct the correct file path based on the platform.
        // Application.streamingAssetsPath points to the StreamingAssets folder.
        // The way to access this path differs by platform:

        // For Android, iOS, and WebGL, Application.streamingAssetsPath points to a compressed or
        // web-served location, requiring UnityWebRequest (which implicitly handles prefixes like "jar:file://").
        // For Editor, Windows, macOS, Linux Standalone builds, it's a direct file system path.
        // However, using "file://" + Path.Combine with UnityWebRequest is the most robust cross-platform approach,
        // as UnityWebRequest handles the actual file access correctly.

        if (Application.platform == RuntimePlatform.Android)
        {
            // On Android, the path is inside the APK, requiring UnityWebRequest.
            // No explicit "file://" prefix is needed, as UnityWebRequest handles "jar:file://" internally.
            filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        }
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // On WebGL, StreamingAssets are served by the web server. UnityWebRequest is essential.
            // No explicit "file://" prefix is needed.
            filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        }
        else
        {
            // For Editor, iOS, Windows, macOS, Linux Standalone builds, UWP:
            // Application.streamingAssetsPath is a direct file system path, but for consistency
            // and future-proofing with UnityWebRequest, it's best to prepend "file://".
            filePath = "file://" + Path.Combine(Application.streamingAssetsPath, fileName);
        }

        Debug.Log($"<color=yellow>Attempting to load from path:</color> {filePath}");

        // Step 2: Use UnityWebRequest to load the file.
        // UnityWebRequest is the modern and recommended way to load assets from StreamingAssets
        // across all platforms, especially Android, iOS, and WebGL where direct file access
        // (using System.IO.File) does not work reliably or at all.
        using (UnityWebRequest www = UnityWebRequest.Get(filePath))
        {
            // Send the request and wait for it to complete.
            yield return www.SendWebRequest();

            // Step 3: Check for errors.
            // UnityWebRequest.Result enum was introduced in Unity 2018.3.
            // For older Unity versions, you might use www.isNetworkError || www.isHttpError.
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"<color=red>Error loading StreamingAsset '{fileName}':</color> {www.error}");
                loadedContent = $"ERROR: Could not load file. {www.error}";
            }
            else
            {
                // Step 4: File loaded successfully, retrieve content.
                loadedContent = www.downloadHandler.text;
                Debug.Log($"<color=green>Successfully loaded StreamingAsset '{fileName}':</color>\n--- Content Start ---\n{loadedContent}\n--- Content End ---");

                // --- Practical Use Case Example: Parsing Loaded Data ---
                // If your file was JSON, you would parse it here:
                // MyData data = JsonUtility.FromJson<MyData>(loadedContent);
                // For example:
                // [System.Serializable]
                // public class MyData
                // {
                //     public string message;
                //     public int value;
                // }
                //
                // If it was XML, you would parse it similarly:
                // using System.Xml;
                // XmlDocument doc = new XmlDocument();
                // doc.LoadXml(loadedContent);
                // XmlNode node = doc.SelectSingleNode("/root/item");
                // Debug.Log("XML Item: " + node.InnerText);
                //
                // For this example, we simply display the text content.
            }
        }
    }
}
```