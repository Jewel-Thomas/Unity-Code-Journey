// Unity Design Pattern Example: RestSystem
// This script demonstrates the RestSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'RestSystem' design pattern, in the context of Unity development, usually refers to the structured approach of integrating a game with an **external RESTful API (Representational State Transfer Application Programming Interface)**. This pattern helps organize code for making HTTP requests (GET, POST, PUT, DELETE), handling responses, serializing/deserializing data, and managing errors.

This example will demonstrate a practical `RestApiClient` that uses `UnityWebRequest` and `System.Text.Json` (which comes with .NET 6/7 that Unity 2021+ supports) for asynchronous API calls.

**Key Components of the RestSystem Pattern (for API Integration):**

1.  **Data Transfer Objects (DTOs):** Simple C# classes that mirror the structure of JSON data sent to and received from the API. They are typically plain objects with properties representing the API's fields.
2.  **API Client:** A dedicated class (or static utility) responsible for encapsulating the logic of making HTTP requests. It handles base URLs, headers, request methods (GET, POST, etc.), and parsing raw responses.
3.  **Request/Response Handling:** Uses asynchronous operations (like `async/await` with `UnityWebRequest` in Unity) to prevent blocking the main game thread. It includes serialization of request bodies and deserialization of API responses (often JSON).
4.  **Structured Responses:** A generic wrapper to encapsulate the outcome of an API call, including success/failure status, the actual data, and any error messages or HTTP status codes. This makes error handling consistent.
5.  **Integration Layer:** How game logic (e.g., a `MonoBehaviour`) interacts with the `RestApiClient` to trigger calls and process results.

---

### **`RestApiClient.cs`**

This script contains all the necessary components: DTOs, the `RestApiClient` itself, a structured `ApiResponse` class, and an example `MonoBehaviour` to demonstrate usage.

**To use this script in your Unity project:**

1.  Create a new C# script named `RestSystemExample.cs` (or any other name, just make sure the filename matches the primary class, if you choose to rename it) in your Assets folder.
2.  Copy and paste the entire code below into the script.
3.  Create an empty GameObject in your scene (e.g., "RestSystemDemo").
4.  Attach the `RestSystemExample` script to this GameObject.
5.  (Optional but Recommended for a UI Demo) Create a simple UI:
    *   Canvas -> Panel (to organize)
    *   Text (for status messages, name it `StatusText`)
    *   InputField (for entering a Post ID, name it `PostIdInput`)
    *   Button (for 'Get Post', name it `GetPostButton`)
    *   Button (for 'Create Post', name it `CreatePostButton`)
6.  Drag these UI elements from the Hierarchy to the corresponding public fields (`statusText`, `postIdInput`, `getPostButton`, `createPostButton`) on the `RestSystemDemo` component in the Inspector.
7.  Run the scene and try interacting with the buttons or observe the `Debug.Log` output.
    *   The example uses `https://jsonplaceholder.typicode.com`, a free fake online REST API, for demonstration purposes.

```csharp
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest
using System.Collections;      // Required for IEnumerator (though not directly used in async/await pattern)
using System.Collections.Generic;
using System.Text;             // Required for Encoding.UTF8
using System.Text.Json;        // Required for JSON serialization/deserialization
using System.Threading.Tasks;  // Required for async/await

// This namespace encapsulates all classes related to our RestSystem example.
namespace RestSystemExample
{
    // =========================================================================
    // 1. Data Transfer Objects (DTOs)
    //    These classes define the structure of the data we expect to send to
    //    and receive from the REST API. They are simple C# objects that map
    //    directly to the JSON structure.
    // =========================================================================

    /// <summary>
    /// DTO representing a 'Post' object from the JSONPlaceholder API.
    /// Properties are named to match the JSON keys.
    /// </    summary>
    [System.Serializable] // Generally good practice for Unity objects, though not strictly required by System.Text.Json.
    public class PlaceholderPost
    {
        public int userId { get; set; }
        public int id { get; set; } // ID is typically assigned by the server for new posts
        public string title { get; set; }
        public string body { get; set; }

        public override string ToString()
        {
            // Using System.Text.Json for consistent serialization in debug output.
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    /// <summary>
    /// DTO representing the request body for creating a new 'Post'.
    /// Note: 'id' is omitted as it's typically generated by the server.
    /// </summary>
    [System.Serializable]
    public class CreatePostRequest
    {
        public int userId { get; set; }
        public string title { get; set; }
        public string body { get; set; }
    }

    // =========================================================================
    // 2. Structured API Response Wrapper
    //    A generic class to encapsulate the result of any API call, providing
    //    a consistent way to check for success, retrieve data, or get error info.
    // =========================================================================

    /// <summary>
    /// A generic wrapper for API responses.
    /// Provides information about success status, data, error messages, and HTTP status code.
    /// </summary>
    /// <typeparam name="T">The type of data expected in a successful response.</typeparam>
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; }
        public string ErrorMessage { get; private set; }
        public long StatusCode { get; private set; } // HTTP Status Code

        private ApiResponse(T data, long statusCode, bool isSuccess = true, string errorMessage = null)
        {
            Data = data;
            StatusCode = statusCode;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        public static ApiResponse<T> Success(T data, long statusCode) => new ApiResponse<T>(data, statusCode);

        /// <summary>
        /// Creates an erroneous API response.
        /// </summary>
        public static ApiResponse<T> Error(string errorMessage, long statusCode, T defaultData = default) => new ApiResponse<T>(statusCode: statusCode, isSuccess: false, errorMessage: errorMessage, data: defaultData);
    }

    // =========================================================================
    // 3. The Core RestSystem Client
    //    This static class provides methods to perform common REST operations
    //    (GET, POST, etc.) against a defined base URL. It handles request setup,
    //    execution, response parsing, and error handling.
    // =========================================================================

    /// <summary>
    /// The static client for interacting with the REST API.
    /// This client encapsulates all HTTP request logic and data serialization/deserialization.
    /// </summary>
    public static class RestApiClient
    {
        // The base URL for the API. In a real project, this might come from a config file.
        private static readonly string BASE_URL = "https://jsonplaceholder.typicode.com";

        // JsonSerializerOptions for pretty printing JSON, useful for debugging.
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,      // Make the JSON output human-readable
            PropertyNameCaseInsensitive = true // Allow matching JSON properties without exact casing
        };

        /// <summary>
        /// Helper method to send a UnityWebRequest and await its completion.
        /// This centralizes request sending logic and allows for common header setup.
        /// </summary>
        /// <param name="request">The UnityWebRequest object to send.</param>
        /// <returns>The completed UnityWebRequest object.</returns>
        private static async Task<UnityWebRequest> SendRequest(UnityWebRequest request)
        {
            // Set common headers for all requests
            request.SetRequestHeader("Accept", "application/json"); // We expect JSON responses

            // Start the UnityWebRequest operation asynchronously.
            // UnityWebRequestAsyncOperation is not directly awaitable by System.Threading.Tasks.
            // We use a loop with Task.Yield() to wait for its completion without blocking the main thread.
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield(); // Yield control back to Unity, allowing other tasks to run.
            }

            return request;
        }

        /// <summary>
        /// Performs an asynchronous HTTP GET request.
        /// </summary>
        /// <typeparam name="T">The type of the expected response data (DTO).</typeparam>
        /// <param name="endpoint">The specific API endpoint relative to the base URL (e.g., "posts/1").</param>
        /// <returns>An ApiResponse containing the deserialized data or an error message.</returns>
        public static async Task<ApiResponse<T>> GetAsync<T>(string endpoint) where T : class
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{BASE_URL}/{endpoint}"))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer(); // Ensure we can read the response body
                UnityWebRequest response = await SendRequest(webRequest); // Send the request and wait

                if (response.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = response.downloadHandler.text;
                    Debug.Log($"<color=green>GET Success from {endpoint}:</color> {jsonResponse}");
                    try
                    {
                        // Deserialize the JSON string into the specified DTO type.
                        T data = JsonSerializer.Deserialize<T>(jsonResponse, _jsonSerializerOptions);
                        return ApiResponse<T>.Success(data, response.responseCode);
                    }
                    catch (System.Exception ex)
                    {
                        // Handle JSON deserialization errors.
                        return ApiResponse<T>.Error($"Failed to deserialize GET response from {endpoint}: {ex.Message}", response.responseCode);
                    }
                }
                else
                {
                    // Handle UnityWebRequest errors (network issues, HTTP errors).
                    Debug.LogError($"<color=red>GET Error from {endpoint}:</color> {response.error}, Status Code: {response.responseCode}, Response: {response.downloadHandler?.text}");
                    return ApiResponse<T>.Error(response.error ?? "Unknown Error", response.responseCode, default);
                }
            }
        }

        /// <summary>
        /// Performs an asynchronous HTTP POST request with a JSON request body.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request data (DTO) to be serialized and sent.</typeparam>
        /// <typeparam name="TResponse">The type of the expected response data (DTO).</typeparam>
        /// <param name="endpoint">The specific API endpoint relative to the base URL (e.g., "posts").</param>
        /// <param name="data">The object to be serialized into the request body.</param>
        /// <returns>An ApiResponse containing the deserialized response data or an error message.</returns>
        public static async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
            where TRequest : class
            where TResponse : class
        {
            string jsonBody = JsonSerializer.Serialize(data, _jsonSerializerOptions); // Serialize the request DTO to JSON
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody); // Convert JSON string to byte array

            using (UnityWebRequest webRequest = new UnityWebRequest($"{BASE_URL}/{endpoint}", UnityWebRequest.kHttpVerbPOST))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw) { contentType = "application/json" }; // Set request body and content type
                webRequest.downloadHandler = new DownloadHandlerBuffer(); // Ensure we can read the response body
                UnityWebRequest response = await SendRequest(webRequest); // Send the request and wait

                if (response.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = response.downloadHandler.text;
                    Debug.Log($"<color=green>POST Success to {endpoint}:</color> {jsonResponse}");
                    try
                    {
                        // Deserialize the JSON response into the specified DTO type.
                        TResponse responseData = JsonSerializer.Deserialize<TResponse>(jsonResponse, _jsonSerializerOptions);
                        return ApiResponse<TResponse>.Success(responseData, response.responseCode);
                    }
                    catch (System.Exception ex)
                    {
                        // Handle JSON deserialization errors.
                        return ApiResponse<TResponse>.Error($"Failed to deserialize POST response from {endpoint}: {ex.Message}", response.responseCode);
                    }
                }
                else
                {
                    // Handle UnityWebRequest errors.
                    Debug.LogError($"<color=red>POST Error to {endpoint}:</color> {response.error}, Status Code: {response.responseCode}, Response: {response.downloadHandler?.text}");
                    return ApiResponse<TResponse>.Error(response.error ?? "Unknown Error", response.responseCode, default);
                }
            }
        }

        // --- Additional REST Methods (can be added as needed) ---

        /// <summary>
        /// Performs an asynchronous HTTP PUT request with a JSON request body.
        /// </summary>
        /// <param name="endpoint">The specific API endpoint (e.g., "posts/1").</param>
        /// <param name="data">The object to be serialized into the request body.</param>
        public static async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
            where TRequest : class
            where TResponse : class
        {
            string jsonBody = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest webRequest = new UnityWebRequest($"{BASE_URL}/{endpoint}", UnityWebRequest.kHttpVerbPUT))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw) { contentType = "application/json" };
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                UnityWebRequest response = await SendRequest(webRequest);

                if (response.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = response.downloadHandler.text;
                    Debug.Log($"<color=green>PUT Success to {endpoint}:</color> {jsonResponse}");
                    try
                    {
                        TResponse responseData = JsonSerializer.Deserialize<TResponse>(jsonResponse, _jsonSerializerOptions);
                        return ApiResponse<TResponse>.Success(responseData, response.responseCode);
                    }
                    catch (System.Exception ex)
                    {
                        return ApiResponse<TResponse>.Error($"Failed to deserialize PUT response from {endpoint}: {ex.Message}", response.responseCode);
                    }
                }
                else
                {
                    Debug.LogError($"<color=red>PUT Error to {endpoint}:</color> {response.error}, Status Code: {response.responseCode}, Response: {response.downloadHandler?.text}");
                    return ApiResponse<TResponse>.Error(response.error ?? "Unknown Error", response.responseCode, default);
                }
            }
        }

        /// <summary>
        /// Performs an asynchronous HTTP DELETE request.
        /// </summary>
        /// <param name="endpoint">The specific API endpoint (e.g., "posts/1").</param>
        public static async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Delete($"{BASE_URL}/{endpoint}"))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer(); // To read any potential response body
                UnityWebRequest response = await SendRequest(webRequest);

                if (response.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"<color=green>DELETE Success from {endpoint}:</color> Status Code: {response.responseCode}");
                    // For DELETE, often just a 200/204 status indicates success.
                    return ApiResponse<bool>.Success(true, response.responseCode);
                }
                else
                {
                    Debug.LogError($"<color=red>DELETE Error from {endpoint}:</color> {response.error}, Status Code: {response.responseCode}, Response: {response.downloadHandler?.text}");
                    return ApiResponse<bool>.Error(response.error ?? "Unknown Error", response.responseCode, false);
                }
            }
        }
    }

    // =========================================================================
    // 4. Example Usage in a MonoBehaviour
    //    This script demonstrates how to call the RestApiClient methods from
    //    your game logic and handle the results, integrating with Unity's UI.
    // =========================================================================

    /// <summary>
    /// MonoBehaviour demonstrating the usage of the RestApiClient.
    /// Attach this to a GameObject in your scene to see it in action.
    /// </summary>
    public class RestSystemDemo : MonoBehaviour
    {
        [Header("UI References (Optional)")]
        [Tooltip("Text element to display status messages.")]
        public UnityEngine.UI.Text statusText;
        [Tooltip("Input field for entering a Post ID for GET requests.")]
        public UnityEngine.UI.InputField postIdInput;
        [Tooltip("Button to trigger a GET request for a post.")]
        public UnityEngine.UI.Button getPostButton;
        [Tooltip("Button to trigger a POST request to create a new post.")]
        public UnityEngine.UI.Button createPostButton;
        [Tooltip("Button to trigger a PUT request to update a post.")]
        public UnityEngine.UI.Button updatePostButton;
        [Tooltip("Button to trigger a DELETE request for a post.")]
        public UnityEngine.UI.Button deletePostButton;


        void Start()
        {
            // Bind UI button clicks to our async API methods.
            // Using 'async () => await Method()' allows async operations on button clicks.
            if (getPostButton != null)
            {
                getPostButton.onClick.AddListener(async () => await GetPostById(postIdInput != null && !string.IsNullOrEmpty(postIdInput.text) ? postIdInput.text : "1"));
            }
            if (createPostButton != null)
            {
                createPostButton.onClick.AddListener(async () => await CreateNewPost());
            }
            if (updatePostButton != null)
            {
                updatePostButton.onClick.AddListener(async () => await UpdateExistingPost(postIdInput != null && !string.IsNullOrEmpty(postIdInput.text) ? postIdInput.text : "1"));
            }
            if (deletePostButton != null)
            {
                deletePostButton.onClick.AddListener(async () => await DeletePost(postIdInput != null && !string.IsNullOrEmpty(postIdInput.text) ? postIdInput.text : "1"));
            }


            // Initialize status text.
            UpdateStatus("Ready to make API calls. Enter a Post ID or use buttons.");

            // Example of an API call on start (uncomment to test):
            // Note: When calling an async method from a non-async void method like Start(),
            // it's common to discard the Task using '_' if you don't need to await its completion
            // or handle exceptions directly within Start().
            // _ = GetPostById("1");
        }

        /// <summary>
        /// Updates the UI status text field.
        /// </summary>
        /// <param name="message">The message to display.</param>
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"Status: {message}");
        }

        /// <summary>
        /// Demonstrates fetching a single post by ID using the RestApiClient.
        /// </summary>
        /// <param name="postId">The ID of the post to fetch.</param>
        public async Task GetPostById(string postId)
        {
            UpdateStatus($"Fetching post {postId}...");
            Debug.Log($"Attempting to fetch post with ID: {postId}");

            int id = 1;
            if (!int.TryParse(postId, out id))
            {
                id = 1; // Default to 1 if input is invalid
                UpdateStatus("Invalid Post ID, defaulting to 1.");
            }

            // Call the generic GET method from our RestApiClient.
            ApiResponse<PlaceholderPost> response = await RestApiClient.GetAsync<PlaceholderPost>($"posts/{id}");

            if (response.IsSuccess)
            {
                UpdateStatus($"Fetched Post (ID: {response.Data.id}): {response.Data.title}");
                Debug.Log($"Successfully fetched post:\n{response.Data}");
            }
            else
            {
                UpdateStatus($"Error fetching post: {response.ErrorMessage} (Code: {response.StatusCode})");
                Debug.LogError($"Failed to fetch post {id}: {response.ErrorMessage} (Status: {response.StatusCode})");
            }
        }

        /// <summary>
        /// Demonstrates creating a new post using the RestApiClient.
        /// </summary>
        public async Task CreateNewPost()
        {
            UpdateStatus("Creating new post...");
            Debug.Log("Attempting to create a new post.");

            // Prepare the data to be sent in the POST request.
            CreatePostRequest newPost = new CreatePostRequest
            {
                userId = 1,
                title = "My New Awesome Post from Unity",
                body = "This is the body of the new post created via the RestSystem pattern example. It's exciting!"
            };

            // Call the generic POST method. The JSONPlaceholder API returns the created object
            // (often with a new 'id'), so we expect a PlaceholderPost as a response.
            ApiResponse<PlaceholderPost> response = await RestApiClient.PostAsync<CreatePostRequest, PlaceholderPost>("posts", newPost);

            if (response.IsSuccess)
            {
                UpdateStatus($"Created Post (ID: {response.Data.id}): {response.Data.title}");
                Debug.Log($"Successfully created post:\n{response.Data}");
            }
            else
            {
                UpdateStatus($"Error creating post: {response.ErrorMessage} (Code: {response.StatusCode})");
                Debug.LogError($"Failed to create post: {response.ErrorMessage} (Status: {response.StatusCode})");
            }
        }

        /// <summary>
        /// Demonstrates updating an existing post using the RestApiClient (PUT request).
        /// </summary>
        /// <param name="postId">The ID of the post to update.</param>
        public async Task UpdateExistingPost(string postId)
        {
            UpdateStatus($"Updating post {postId}...");
            Debug.Log($"Attempting to update post with ID: {postId}");

            int id = 1;
            if (!int.TryParse(postId, out id))
            {
                id = 1;
                UpdateStatus("Invalid Post ID, defaulting to 1 for update.");
            }

            // Prepare the updated data. Note: The API usually expects the full object for PUT.
            PlaceholderPost updatedPostData = new PlaceholderPost
            {
                userId = 1, // Assuming the same user
                id = id,    // Crucial to include the ID for PUT requests
                title = $"Updated Title for Post {id}",
                body = $"This post was updated at {System.DateTime.Now} via the RestSystem example."
            };

            // Call the generic PUT method. JSONPlaceholder returns the updated object.
            ApiResponse<PlaceholderPost> response = await RestApiClient.PutAsync<PlaceholderPost, PlaceholderPost>($"posts/{id}", updatedPostData);

            if (response.IsSuccess)
            {
                UpdateStatus($"Updated Post (ID: {response.Data.id}): {response.Data.title}");
                Debug.Log($"Successfully updated post:\n{response.Data}");
            }
            else
            {
                UpdateStatus($"Error updating post: {response.ErrorMessage} (Code: {response.StatusCode})");
                Debug.LogError($"Failed to update post {id}: {response.ErrorMessage} (Status: {response.StatusCode})");
            }
        }

        /// <summary>
        /// Demonstrates deleting a post using the RestApiClient (DELETE request).
        /// </summary>
        /// <param name="postId">The ID of the post to delete.</param>
        public async Task DeletePost(string postId)
        {
            UpdateStatus($"Deleting post {postId}...");
            Debug.Log($"Attempting to delete post with ID: {postId}");

            int id = 1;
            if (!int.TryParse(postId, out id))
            {
                id = 1;
                UpdateStatus("Invalid Post ID, defaulting to 1 for delete.");
            }

            // Call the DELETE method. We expect a simple success/failure.
            ApiResponse<bool> response = await RestApiClient.DeleteAsync($"posts/{id}");

            if (response.IsSuccess)
            {
                UpdateStatus($"Successfully deleted post {id}.");
                Debug.Log($"<color=green>Successfully deleted post {id}.</color>");
            }
            else
            {
                UpdateStatus($"Error deleting post: {response.ErrorMessage} (Code: {response.StatusCode})");
                Debug.LogError($"Failed to delete post {id}: {response.ErrorMessage} (Status: {response.StatusCode})");
            }
        }
    }
}
```