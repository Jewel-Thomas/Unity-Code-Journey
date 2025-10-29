// Unity Design Pattern Example: WebRequestSystem
// This script demonstrates the WebRequestSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **WebRequestSystem** design pattern in Unity. This pattern centralizes all HTTP/HTTPS requests in your application, providing a consistent API for making web calls, handling serialization/deserialization, and managing common concerns like error reporting and timeouts.

**Why use a WebRequestSystem?**

1.  **Centralization:** All web requests go through a single point, making it easier to manage, debug, and modify global request behaviors (e.g., adding authentication headers, logging, caching).
2.  **Abstraction:** Game logic doesn't need to know the specifics of `UnityWebRequest`. It just requests data or sends data via a clean, high-level API.
3.  **Consistency:** Ensures all requests follow the same error handling, timeout, and response processing logic.
4.  **Testability:** Easier to mock or replace the web request system for testing purposes.
5.  **Maintainability:** Changes to the underlying web request implementation (e.g., switching from `UnityWebRequest` to a custom HTTP client) only affect one class.

---

### `WebRequestSystem.cs`

This is the core script. It acts as a singleton `MonoBehaviour` so it can be easily accessed from anywhere in your game.

```csharp
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest
using System;
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for Dictionaries
using System.Text; // Required for Encoding

/// <summary>
/// The WebRequestSystem is a central hub for all HTTP/HTTPS requests in your Unity application.
/// It uses the Singleton pattern to provide easy access from anywhere and abstracts away
/// the complexities of UnityWebRequest.
/// </summary>
public class WebRequestSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    // Provides a global point of access to the WebRequestSystem instance.
    public static WebRequestSystem Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Default timeout for web requests in seconds.")]
    [SerializeField] private float _requestTimeoutSeconds = 15f;

    [Tooltip("Optional: Maximum number of times to retry a failed request. (Not implemented in this basic example, but a common feature.)")]
    [SerializeField] private int _maxRetries = 0; // Placeholder for future retry logic

    private void Awake()
    {
        // Enforce Singleton pattern:
        // If an instance already exists and it's not this one, destroy this GameObject.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate WebRequestSystem detected. Destroying the new one.");
            Destroy(gameObject);
        }
        else
        {
            // Set this instance as the singleton instance.
            Instance = this;
            // Make sure the WebRequestSystem persists across scene loads.
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Performs an HTTP GET request to the specified URL.
    /// Expects a JSON response that will be deserialized into type TResponse.
    /// </summary>
    /// <typeparam name="TResponse">The type to deserialize the JSON response into. Must be [System.Serializable].</typeparam>
    /// <param name="url">The URL to send the GET request to.</param>
    /// <param name="onSuccess">Callback invoked on successful response, providing the deserialized object.</param>
    /// <param name="onFailure">Callback invoked on failure, providing an error message.</param>
    public void GET<TResponse>(string url, Action<TResponse> onSuccess, Action<string> onFailure)
        where TResponse : new() // TResponse must have a parameterless constructor for JsonUtility to work reliably.
    {
        Debug.Log($"[WebRequestSystem] Sending GET request to: {url}");
        // Create a UnityWebRequest for a GET request.
        UnityWebRequest request = UnityWebRequest.Get(url);
        // Start the coroutine to handle the request lifecycle.
        StartCoroutine(PerformRequest(request, onSuccess, onFailure));
    }

    /// <summary>
    /// Performs an HTTP POST request to the specified URL, sending data as JSON.
    /// Expects a JSON response that will be deserialized into type TResponse.
    /// </summary>
    /// <typeparam name="TRequest">The type of the data to send in the request body. Must be [System.Serializable].</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the JSON response into. Must be [System.Serializable].</typeparam>
    /// <param name="url">The URL to send the POST request to.</param>
    /// <param name="data">The object to serialize into JSON and send in the request body.</param>
    /// <param name="onSuccess">Callback invoked on successful response, providing the deserialized object.</param>
    /// <param name="onFailure">Callback invoked on failure, providing an error message.</param>
    public void POST<TRequest, TResponse>(string url, TRequest data, Action<TResponse> onSuccess, Action<string> onFailure)
        where TResponse : new()
    {
        Debug.Log($"[WebRequestSystem] Sending POST request to: {url}");
        // Serialize the request data object into a JSON string.
        string jsonBody = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody); // Convert JSON string to byte array.

        // Create a UnityWebRequest for a POST request.
        // Using UploadHandlerRaw allows us to send a custom body.
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer(); // To receive the response body.

        // Set the content type header for JSON.
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json"); // Indicate that we prefer JSON response.

        // Start the coroutine to handle the request lifecycle.
        StartCoroutine(PerformRequest(request, onSuccess, onFailure));
    }

    // --- Internal Request Handling Logic ---

    /// <summary>
    /// Generic coroutine to send a UnityWebRequest and process its response.
    /// This method encapsulates the common logic for all request types.
    /// </summary>
    /// <typeparam name="TResponse">The expected type of the response data.</typeparam>
    /// <param name="request">The pre-configured UnityWebRequest object.</param>
    /// <param name="onSuccess">Callback for successful responses.</param>
    /// <param name="onFailure">Callback for failed responses.</param>
    private IEnumerator PerformRequest<TResponse>(UnityWebRequest request, Action<TResponse> onSuccess, Action<string> onFailure)
        where TResponse : new()
    {
        request.timeout = (int)_requestTimeoutSeconds; // Apply the configured timeout.

        // Send the web request and wait for it to complete.
        yield return request.SendWebRequest();

        // Check for different types of errors or success.
        // UnityWebRequest.Result is available from Unity 2018.2+.
        // For older versions, use request.isNetworkError || request.isHttpError.
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
        {
            // Network or data processing error (e.g., DNS resolution failure, no internet connection).
            Debug.LogError($"[WebRequestSystem] Network Error for {request.url}: {request.error}");
            onFailure?.Invoke($"Network Error: {request.error} (URL: {request.url})");
        }
        else if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            // HTTP error (e.g., 400 Bad Request, 404 Not Found, 500 Internal Server Error).
            // These are valid HTTP responses, but indicate an error on the server side or with the request itself.
            string responseBody = request.downloadHandler?.text ?? "No response body.";
            Debug.LogError($"[WebRequestSystem] HTTP Error for {request.url}: Status Code {request.responseCode} - {request.error}. Response: {responseBody}");
            onFailure?.Invoke($"HTTP Error: {request.responseCode} - {request.error} (URL: {request.url}, Response: {responseBody})");
        }
        else // request.result == UnityWebRequest.Result.Success
        {
            // Request was successful.
            string jsonResponse = request.downloadHandler?.text;

            if (string.IsNullOrEmpty(jsonResponse))
            {
                Debug.LogWarning($"[WebRequestSystem] Received empty response for {request.url}.");
                // If expecting a specific object type but got an empty string, this might be an issue.
                // If TResponse is string, an empty string is a valid success.
                if (typeof(TResponse) == typeof(string))
                {
                    onSuccess?.Invoke((TResponse)(object)string.Empty);
                }
                else
                {
                    onFailure?.Invoke($"Empty response received from {request.url}. Expected data for type {typeof(TResponse).Name}.");
                }
            }
            else
            {
                try
                {
                    // Special handling for when the expected response type is a raw string.
                    if (typeof(TResponse) == typeof(string))
                    {
                        onSuccess?.Invoke((TResponse)(object)jsonResponse);
                    }
                    else
                    {
                        // Attempt to deserialize the JSON response into the specified type.
                        TResponse result = JsonUtility.FromJson<TResponse>(jsonResponse);
                        onSuccess?.Invoke(result);
                    }
                    Debug.Log($"[WebRequestSystem] Request to {request.url} successful.");
                }
                catch (Exception e)
                {
                    // JSON deserialization failed. This is a common issue if the server sends malformed JSON
                    // or if the response structure doesn't match the C# class.
                    Debug.LogError($"[WebRequestSystem] JSON Deserialization Error for {request.url}: {e.Message}\nResponse: {jsonResponse}");
                    onFailure?.Invoke($"JSON Deserialization Error: {e.Message} (URL: {request.url}, Response: {jsonResponse})");
                }
            }
        }

        // IMPORTANT: Dispose the UnityWebRequest to release system resources.
        request.Dispose();
    }
}

// --- Example Data Structures ---
// These classes define the structure of the JSON data we expect to send or receive.
// They must be marked with [System.Serializable] for Unity's JsonUtility to work.

[System.Serializable]
public class ExampleGetResponse
{
    public int userId;
    public int id;
    public string title;
    public bool completed;
}

[System.Serializable]
public class ExamplePostRequest
{
    public string name;
    public int age;
    public string job;
}

[System.Serializable]
public class ExamplePostResponse
{
    // The server might add an 'id' or other fields upon successful creation.
    public string name;
    public int age;
    public string job;
    public int id; // Added by the JSONPlaceholder API
}
```

---

### `WebRequestDemo.cs`

This script demonstrates how to use the `WebRequestSystem` to make different types of web requests. Attach this to any GameObject in your scene to see it in action.

```csharp
using UnityEngine;

/// <summary>
/// A simple demonstration script to show how to use the WebRequestSystem.
/// Attach this to any GameObject in your scene to test the web requests.
/// </summary>
public class WebRequestDemo : MonoBehaviour
{
    private const string BASE_URL = "https://jsonplaceholder.typicode.com"; // A public fake REST API for testing

    void Start()
    {
        // Ensure the WebRequestSystem is initialized and available.
        if (WebRequestSystem.Instance == null)
        {
            Debug.LogError("WebRequestSystem.Instance is null. Make sure the WebRequestSystem GameObject is in the scene!");
            return;
        }

        // --- Example 1: HTTP GET Request ---
        // Fetch a 'todo' item from the API.
        Debug.Log("\n--- Initiating GET Request (Todo Item) ---");
        WebRequestSystem.Instance.GET<ExampleGetResponse>(
            $"{BASE_URL}/todos/1", // Endpoint for a specific todo item
            (response) => {
                // This callback is executed if the GET request is successful.
                Debug.Log($"<color=green>GET Success!</color>");
                Debug.Log($"Received Todo: ID={response.id}, UserID={response.userId}, Title='{response.title}', Completed={response.completed}");
            },
            (error) => {
                // This callback is executed if the GET request fails.
                Debug.LogError($"<color=red>GET Failed!</color> Error: {error}");
            }
        );

        // --- Example 2: HTTP POST Request ---
        // Create a new 'post' item on the API.
        Debug.Log("\n--- Initiating POST Request (New Post) ---");
        var postData = new ExamplePostRequest
        {
            name = "Alice",
            age = 28,
            job = "Software Engineer"
        };
        WebRequestSystem.Instance.POST<ExamplePostRequest, ExamplePostResponse>(
            $"{BASE_URL}/posts", // Endpoint for creating new posts
            postData,
            (response) => {
                // This callback is executed if the POST request is successful.
                Debug.Log($"<color=green>POST Success!</color>");
                Debug.Log($"Created Post: ID={response.id}, Name={response.name}, Age={response.age}, Job={response.job}");
            },
            (error) => {
                // This callback is executed if the POST request fails.
                Debug.LogError($"<color=red>POST Failed!</color> Error: {error}");
            }
        );

        // --- Example 3: HTTP GET Request expecting raw string response ---
        // Sometimes you might just want the raw JSON string or any other string data.
        Debug.Log("\n--- Initiating GET Request (Raw String Response) ---");
        WebRequestSystem.Instance.GET<string>(
            $"{BASE_URL}/comments/1", // Endpoint for a specific comment
            (rawResponse) => {
                // This callback is executed if the GET request is successful.
                Debug.Log($"<color=green>RAW GET Success!</color>");
                // Displaying only the first 200 characters for brevity
                Debug.Log($"Raw Response (first 200 chars): {rawResponse.Substring(0, Mathf.Min(rawResponse.Length, 200))}...");
            },
            (error) => {
                // This callback is executed if the GET request fails.
                Debug.LogError($"<color=red>RAW GET Failed!</color> Error: {error}");
            }
        );

        // --- Example 4: Deliberate Error Test (404 Not Found) ---
        Debug.Log("\n--- Initiating GET Request (Intentionally Fails with 404) ---");
        WebRequestSystem.Instance.GET<ExampleGetResponse>(
            $"{BASE_URL}/nonexistent-endpoint-123", // This URL does not exist
            (response) => {
                Debug.LogWarning($"This GET should not succeed: ID={response.id}, Title='{response.title}'");
            },
            (error) => {
                // This will be called because the request will return a 404 Not Found.
                Debug.LogError($"<color=red>404 Test Failed (Expected)!</color> Error: {error}");
            }
        );
    }
}
```

---

### How to use in Unity:

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., name it `_WebRequestSystem`).
2.  **Attach `WebRequestSystem.cs`:** Drag and drop the `WebRequestSystem.cs` script onto this `_WebRequestSystem` GameObject.
3.  **Attach `WebRequestDemo.cs`:** Create another empty GameObject (e.g., name it `WebRequestTester`) and drag and drop the `WebRequestDemo.cs` script onto it.
4.  **Run the Scene:** Play your Unity scene. You will see debug messages in the Console window showing the results of the web requests.

This setup ensures that the `WebRequestSystem` is initialized as a singleton when the scene starts, and the `WebRequestDemo` component can then access it via `WebRequestSystem.Instance` to make requests.