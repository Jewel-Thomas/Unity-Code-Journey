// Unity Design Pattern Example: GeoLocationIntegration
// This script demonstrates the GeoLocationIntegration pattern in Unity
// Generated automatically - ready to use in your Unity project

The GeoLocationIntegration pattern encapsulates all the complexities of interacting with device location services (like GPS) into a single, well-defined component. This component provides a simplified, consistent interface for other parts of the application to access location data without needing to know the low-level details of the underlying platform's location APIs.

**Key benefits of this pattern:**

1.  **Separation of Concerns:** The core game logic doesn't need to worry about permissions, service status, initialization, or error handling related to location services.
2.  **Reusability:** The location service component can be easily reused across different projects or parts of the same project.
3.  **Maintainability:** Changes to the underlying location API (e.g., Unity updates or platform-specific nuances) only affect this single component.
4.  **Testability:** Components that *use* location data can be tested more easily by providing mock location data, rather than relying on a real device's GPS.
5.  **Decoupling:** Uses UnityEvents to notify listeners about location updates and status changes, promoting loose coupling between the location service and its consumers.

---

### **GeoLocationIntegration.cs**

This script acts as the central hub for all location-based operations.

```csharp
using UnityEngine;
using System.Collections;
using UnityEngine.Events; // Required for UnityEvent
using System;             // Required for EventHandler

/// <summary>
/// A struct to hold all relevant location data.
/// This makes passing location data around more organized.
/// </summary>
[System.Serializable]
public struct LocationData
{
    public float latitude;
    public float longitude;
    public float altitude;
    public float horizontalAccuracy; // Accuracy of latitude/longitude in meters
    public float verticalAccuracy;   // Accuracy of altitude in meters
    public double timestamp;         // Time of the fix in seconds since 1970-01-01 UTC

    public LocationData(LocationInfo info)
    {
        latitude = info.latitude;
        longitude = info.longitude;
        altitude = info.altitude;
        horizontalAccuracy = info.horizontalAccuracy;
        verticalAccuracy = info.verticalAccuracy;
        timestamp = info.timestamp;
    }

    public override string ToString()
    {
        return $"Lat: {latitude:F4}, Lon: {longitude:F4}, Alt: {altitude:F2}m, Acc: {horizontalAccuracy:F2}m";
    }
}

/// <summary>
/// Defines the various states of the location service.
/// </summary>
public enum LocationServiceStatus
{
    NotInitialized,
    Initializing,
    Running,
    Failed,
    PermissionDenied,
    DisabledByUser,
    TimedOut
}

/// <summary>
/// Custom UnityEvents for decoupling and flexibility.
/// </summary>
[System.Serializable]
public class LocationDataEvent : UnityEvent<LocationData> { }
[System.Serializable]
public class LocationServiceStatusEvent : UnityEvent<LocationServiceStatus> { }
[System.Serializable]
public class LocationServiceErrorEvent : UnityEvent<string> { }

/// <summary>
/// GeoLocationIntegration: A Singleton Manager for device location services.
/// This class encapsulates all interaction with Unity's Input.location API,
/// providing a clean, event-driven interface for other parts of the application.
/// It implements the GeoLocationIntegration design pattern.
/// </summary>
public class GeoLocationIntegration : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    // Ensures only one instance of the location service exists throughout the application.
    private static GeoLocationIntegration _instance;
    public static GeoLocationIntegration Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GeoLocationIntegration>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(GeoLocationIntegration).Name);
                    _instance = singletonObject.AddComponent<GeoLocationIntegration>();
                }
                DontDestroyOnLoad(_instance.gameObject); // Persist across scene loads
            }
            return _instance;
        }
    }

    // --- Configuration Parameters ---
    [Header("Service Configuration")]
    [Tooltip("The desired accuracy of location updates in meters. Lower values consume more power.")]
    [SerializeField]
    private float desiredAccuracyInMeters = 10f;

    [Tooltip("The minimum distance (in meters) the device must move horizontally before a new location event is generated.")]
    [SerializeField]
    private float updateDistanceInMeters = 10f;

    [Tooltip("Timeout in seconds for location service initialization.")]
    [SerializeField]
    private float initializationTimeout = 20f;

    // --- Current State Variables ---
    private LocationServiceStatus _currentStatus = LocationServiceStatus.NotInitialized;
    private LocationData _lastKnownLocation;
    private bool _isServiceRunning = false;

    // --- Public Events for Observers ---
    // These UnityEvents allow other scripts to subscribe to location updates and status changes
    // without directly knowing about the GeoLocationIntegration class implementation.
    [Header("Events")]
    [Tooltip("Fired when a new location update is available.")]
    public LocationDataEvent OnLocationUpdated;

    [Tooltip("Fired when the location service's status changes.")]
    public LocationServiceStatusEvent OnServiceStatusChanged;

    [Tooltip("Fired when an error occurs during location service operation.")]
    public LocationServiceErrorEvent OnLocationServiceError;

    // --- Public Properties to Access Current Data ---
    public LocationServiceStatus ServiceStatus => _currentStatus;
    public LocationData LastKnownLocation => _lastKnownLocation;
    public bool IsServiceRunning => _isServiceRunning;

    // --- MonoBehaviour Lifecycle Methods ---
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Enforce singleton
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize events if they haven't been in the inspector
        if (OnLocationUpdated == null) OnLocationUpdated = new LocationDataEvent();
        if (OnServiceStatusChanged == null) OnServiceStatusChanged = new LocationServiceStatusEvent();
        if (OnLocationServiceError == null) OnLocationServiceError = new LocationServiceErrorEvent();

        // Automatically start the service when the manager awakes
        // In a real project, you might want to call StartLocationService() explicitly
        // based on user interaction or game state.
        StartLocationService();
    }

    private void OnDisable()
    {
        StopLocationService();
    }

    // --- Public Methods to Control Service ---

    /// <summary>
    /// Initiates the device's location services.
    /// This is the primary method to call from other parts of your application
    /// to begin receiving location updates.
    /// </summary>
    public void StartLocationService()
    {
        if (_isServiceRunning || _currentStatus == LocationServiceStatus.Initializing)
        {
            Debug.LogWarning("GeoLocationIntegration: Service is already running or initializing.");
            return;
        }
        StartCoroutine(InitializeLocationServiceCoroutine());
    }

    /// <summary>
    /// Stops the device's location services.
    /// It's crucial to stop the service when not needed to conserve battery life.
    /// </summary>
    public void StopLocationService()
    {
        if (Input.location.isEnabledByUser)
        {
            Input.location.Stop();
            Debug.Log("GeoLocationIntegration: Location service stopped.");
        }
        SetServiceStatus(LocationServiceStatus.NotInitialized);
        _isServiceRunning = false;
        StopAllCoroutines(); // Stop any pending initialization coroutines
    }

    /// <summary>
    /// Coroutine to handle the asynchronous initialization of location services.
    /// This handles permission checks, service startup, and timeouts.
    /// </summary>
    private IEnumerator InitializeLocationServiceCoroutine()
    {
        SetServiceStatus(LocationServiceStatus.Initializing);
        Debug.Log("GeoLocationIntegration: Initializing location service...");

        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            SetServiceStatus(LocationServiceStatus.DisabledByUser);
            OnLocationServiceError?.Invoke("Location services are disabled by the user on the device.");
            Debug.LogError("GeoLocationIntegration: Location services are disabled by the user.");
            yield break;
        }

        // Request permission if not already granted (Android requires explicit request, iOS usually does it on first access)
        // Unity's Input.location.Start() implicitly requests permission on some platforms.
        // For more robust permission handling, especially on Android, consider using the Permissions API.
        // E.g., if (Application.platform == RuntimePlatform.Android) { ... }
        
        // Start service with desired accuracy and distance
        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);

        float timer = 0;
        while (Input.location.status == LocationServiceStatus.Initializing && timer < initializationTimeout)
        {
            yield return new WaitForSeconds(1);
            timer += 1;
        }

        // Check if the service timed out
        if (timer >= initializationTimeout)
        {
            SetServiceStatus(LocationServiceStatus.TimedOut);
            OnLocationServiceError?.Invoke($"Location service initialization timed out after {initializationTimeout} seconds.");
            Debug.LogError($"GeoLocationIntegration: Location service initialization timed out after {initializationTimeout} seconds.");
            StopLocationService();
            yield break;
        }

        // Check the final status
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            SetServiceStatus(LocationServiceStatus.Failed);
            OnLocationServiceError?.Invoke("Failed to initialize location service. Check device settings and permissions.");
            Debug.LogError("GeoLocationIntegration: Failed to initialize location service.");
            StopLocationService();
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            SetServiceStatus(LocationServiceStatus.Running);
            _isServiceRunning = true;
            Debug.Log("GeoLocationIntegration: Location service running!");
            // Start the update loop to continuously get location data
            StartCoroutine(UpdateLocationDataCoroutine());
        }
    }

    /// <summary>
    /// Coroutine that continuously polls for updated location data
    /// once the service is running.
    /// </summary>
    private IEnumerator UpdateLocationDataCoroutine()
    {
        while (_isServiceRunning)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                // Get the latest location data
                LocationInfo latestInfo = Input.location.lastData;
                LocationData newLocation = new LocationData(latestInfo);

                // Only invoke the event if the location has actually changed
                // (e.g., beyond a minimal threshold or if it's the first data)
                if (_lastKnownLocation.latitude != newLocation.latitude ||
                    _lastKnownLocation.longitude != newLocation.longitude ||
                    _lastKnownLocation.timestamp != newLocation.timestamp) // Timestamp is a good indicator of new data
                {
                    _lastKnownLocation = newLocation;
                    OnLocationUpdated?.Invoke(_lastKnownLocation);
                    Debug.Log($"GeoLocationIntegration: Location updated: {_lastKnownLocation}");
                }
            }
            else
            {
                // If service status changes to not running while this coroutine is active,
                // something went wrong or service was stopped externally.
                Debug.LogWarning($"GeoLocationIntegration: Location service status changed to {Input.location.status} while update was running. Stopping update coroutine.");
                SetServiceStatus((LocationServiceStatus)Input.location.status); // Cast to our enum if compatible
                _isServiceRunning = false;
            }
            yield return new WaitForSeconds(updateDistanceInMeters / desiredAccuracyInMeters); // Polling interval, could be fixed or dynamic
        }
        Debug.Log("GeoLocationIntegration: Location data update coroutine stopped.");
    }

    /// <summary>
    /// Helper to update and broadcast service status.
    /// </summary>
    /// <param name="newStatus">The new status of the location service.</param>
    private void SetServiceStatus(LocationServiceStatus newStatus)
    {
        if (_currentStatus != newStatus)
        {
            _currentStatus = newStatus;
            OnServiceStatusChanged?.Invoke(_currentStatus);
            Debug.Log($"GeoLocationIntegration: Service Status Changed to {_currentStatus}");
        }
    }

    /// <summary>
    /// Retrieves the most recently recorded location data.
    /// </summary>
    /// <returns>The last known LocationData.</returns>
    public LocationData GetLastKnownLocation()
    {
        return _lastKnownLocation;
    }
}
```

---

### **How to Use and Integrate (Example Usage)**

To demonstrate the GeoLocationIntegration pattern, we'll create a simple `LocationDisplay` script that consumes the location data and service status provided by `GeoLocationIntegration`.

#### 1. Create the `GeoLocationIntegration` GameObject

*   Create an Empty GameObject in your Unity scene.
*   Rename it to `_GeoLocationManager` (or similar).
*   Attach the `GeoLocationIntegration.cs` script to this GameObject.
*   The script will automatically set itself up as a singleton and persist across scenes. You can adjust `Desired Accuracy In Meters` and `Update Distance In Meters` in the Inspector.

#### 2. Create a Consumer Script: `LocationDisplay.cs`

This script will listen to the events from `GeoLocationIntegration` and display the information.

```csharp
using UnityEngine;
using TMPro; // Assuming you are using TextMeshPro for UI display
             // If not, use UnityEngine.UI.Text and import using UnityEngine.UI;

/// <summary>
/// A consumer script that demonstrates how to interact with the GeoLocationIntegration service.
/// It subscribes to location updates and status changes to display them on UI.
/// </summary>
public class LocationDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshProUGUI component to display location data.")]
    [SerializeField] private TextMeshProUGUI locationText;
    [Tooltip("TextMeshProUGUI component to display service status.")]
    [SerializeField] private TextMeshProUGUI statusText;
    [Tooltip("TextMeshProUGUI component to display error messages.")]
    [SerializeField] private TextMeshProUGUI errorText;

    private void OnEnable()
    {
        // --- GeoLocationIntegration Pattern Usage ---
        // Subscribe to events provided by the GeoLocationIntegration singleton.
        // This is how other parts of your application get notified about location changes
        // without needing to poll or know the implementation details.
        if (GeoLocationIntegration.Instance != null)
        {
            GeoLocationIntegration.Instance.OnLocationUpdated.AddListener(HandleLocationUpdate);
            GeoLocationIntegration.Instance.OnServiceStatusChanged.AddListener(HandleStatusChange);
            GeoLocationIntegration.Instance.OnLocationServiceError.AddListener(HandleError);
            Debug.Log("LocationDisplay: Subscribed to GeoLocationIntegration events.");

            // Initialize display with current status
            UpdateStatusDisplay(GeoLocationIntegration.Instance.ServiceStatus);
            if (GeoLocationIntegration.Instance.IsServiceRunning)
            {
                UpdateLocationDisplay(GeoLocationIntegration.Instance.LastKnownLocation);
            }
        }
        else
        {
            Debug.LogError("LocationDisplay: GeoLocationIntegration.Instance is null. Make sure the manager is in the scene.");
        }
    }

    private void OnDisable()
    {
        // --- GeoLocationIntegration Pattern Usage ---
        // Always unsubscribe from events to prevent memory leaks or calling
        // destroyed objects when the listener is disabled or destroyed.
        if (GeoLocationIntegration.Instance != null)
        {
            GeoLocationIntegration.Instance.OnLocationUpdated.RemoveListener(HandleLocationUpdate);
            GeoLocationIntegration.Instance.OnServiceStatusChanged.RemoveListener(HandleStatusChange);
            GeoLocationIntegration.Instance.OnLocationServiceError.RemoveListener(HandleError);
            Debug.Log("LocationDisplay: Unsubscribed from GeoLocationIntegration events.");
        }
    }

    /// <summary>
    /// Callback method for when a new location update is received.
    /// This method is invoked by the GeoLocationIntegration's OnLocationUpdated event.
    /// </summary>
    /// <param name="location">The latest location data.</param>
    private void HandleLocationUpdate(LocationData location)
    {
        UpdateLocationDisplay(location);
    }

    /// <summary>
    /// Callback method for when the location service status changes.
    /// This method is invoked by the GeoLocationIntegration's OnServiceStatusChanged event.
    /// </summary>
    /// <param name="status">The new status of the location service.</param>
    private void HandleStatusChange(LocationServiceStatus status)
    {
        UpdateStatusDisplay(status);
    }

    /// <summary>
    /// Callback method for when an error occurs in the location service.
    /// This method is invoked by the GeoLocationIntegration's OnLocationServiceError event.
    /// </summary>
    /// <param name="errorMessage">The description of the error.</param>
    private void HandleError(string errorMessage)
    {
        if (errorText != null)
        {
            errorText.text = $"Error: {errorMessage}";
            errorText.color = Color.red;
        }
        Debug.LogError($"LocationDisplay: Received Error from GeoLocationIntegration: {errorMessage}");
    }

    /// <summary>
    /// Updates the UI text with the latest location data.
    /// </summary>
    private void UpdateLocationDisplay(LocationData location)
    {
        if (locationText != null)
        {
            locationText.text = $"Current Location:\n" +
                                $"Latitude: {location.latitude:F6}\n" +
                                $"Longitude: {location.longitude:F6}\n" +
                                $"Altitude: {location.altitude:F2} m\n" +
                                $"Accuracy: {location.horizontalAccuracy:F2} m\n" +
                                $"Timestamp: {System.DateTimeOffset.FromUnixTimeSeconds((long)location.timestamp).ToLocalTime():HH:mm:ss}";
        }
    }

    /// <summary>
    /// Updates the UI text with the current service status.
    /// </summary>
    private void UpdateStatusDisplay(LocationServiceStatus status)
    {
        if (statusText != null)
        {
            statusText.text = $"Service Status: <color=white>{status}</color>";
            switch (status)
            {
                case LocationServiceStatus.Running:
                    statusText.color = Color.green;
                    break;
                case LocationServiceStatus.Initializing:
                    statusText.color = Color.yellow;
                    break;
                case LocationServiceStatus.Failed:
                case LocationServiceStatus.PermissionDenied:
                case LocationServiceStatus.DisabledByUser:
                case LocationServiceStatus.TimedOut:
                    statusText.color = Color.red;
                    break;
                default:
                    statusText.color = Color.white;
                    break;
            }
        }
        // Clear error text if status becomes healthy again
        if (status == LocationServiceStatus.Running || status == LocationServiceStatus.Initializing)
        {
            if (errorText != null) errorText.text = "";
        }
    }
}
```

#### 3. Setup the UI (Canvas)

*   In your Unity scene, create a UI Canvas: `GameObject -> UI -> Canvas`.
*   Inside the Canvas, create three TextMeshPro - Text (UI) elements:
    *   `GameObject -> UI -> Text - TextMeshPro` (Requires importing TMP Essentials).
*   Rename them to `LocationText`, `StatusText`, and `ErrorText`.
*   Adjust their positions and sizes so they are visible on screen.
*   Attach the `LocationDisplay.cs` script to any GameObject in your scene (e.g., the Main Camera or a new Empty GameObject).
*   Drag the `LocationText`, `StatusText`, and `ErrorText` from your Canvas to the corresponding `TextMeshProUGUI` slots in the `LocationDisplay` component in the Inspector.

#### 4. Player Settings and Permissions

For location services to work on mobile devices, you need to enable them in Unity's Player Settings:

*   Go to `File -> Build Settings...`
*   Click `Player Settings...`
*   Navigate to `Other Settings`
*   Under the `Configuration` section, ensure `Location Usage Description` (for iOS) is filled out (e.g., "Your location is used to display your position on the map.").
*   For Android, Unity usually handles the `ACCESS_FINE_LOCATION` and `ACCESS_COARSE_LOCATION` permissions automatically when `Input.location.Start()` is called, but it's good to be aware that users will be prompted for permission.
*   **Crucially:** You must run this on a physical device (iOS or Android) with GPS enabled, as the Unity Editor and most desktop platforms do not simulate `Input.location` data.

#### 5. Run and Test

*   Build your Unity project to your mobile device.
*   Run the app.
*   You should see the "Service Status" change from "NotInitialized" to "Initializing", then "Running".
*   Once running, the "Current Location" display will update with your device's latitude, longitude, altitude, and accuracy.
*   If you deny permissions or disable location services on your device, the "Service Status" and "Error" texts will update accordingly.

This setup provides a robust and decoupled way to integrate and manage location services in your Unity applications, adhering to the GeoLocationIntegration pattern.