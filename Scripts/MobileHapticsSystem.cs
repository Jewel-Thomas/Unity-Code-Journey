// Unity Design Pattern Example: MobileHapticsSystem
// This script demonstrates the MobileHapticsSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Mobile Haptics System** design pattern in Unity. This pattern provides a centralized, platform-agnostic way to trigger haptic feedback, abstracting device-specific implementations and offering a consistent API for game developers. It also allows for global control over haptics (e.g., enable/disable from game settings).

While Unity's built-in `Handheld.Vibrate()` method is quite basic (it only triggers a default, short vibration without control over intensity or duration), this example shows how to build a *system* around it. If you later integrate more advanced haptic plugins (e.g., for iOS Taptic Engine or Android's more granular Vibrator API), you would modify only the internal `_InternalVibrate` or `VibratePattern` methods, keeping your game logic clean and untouched.

---

### `HapticsManager.cs`

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// Implements the Mobile Haptics System design pattern as a Singleton MonoBehaviour.
/// This system provides a centralized, platform-agnostic way to trigger haptic feedback on mobile devices.
/// </summary>
/// <remarks>
/// The core idea of the Mobile Haptics System is to abstract the underlying platform-specific haptics APIs
/// (e.g., iOS UIImpactFeedbackGenerator, Android Vibrator) behind a simple, consistent interface.
///
/// For this basic Unity example, it primarily uses `Handheld.Vibrate()`. Since `Handheld.Vibrate()`
/// offers no control over duration or intensity, we simulate different "feels" by calling it
/// multiple times with strategic delays to create distinct patterns.
///
/// The true power of this pattern shines when integrating more advanced haptic plugins.
/// You would modify the private internal vibration logic (e.g., `_InternalVibrate` or `VibratePattern`)
/// to use those plugins, while the public API (`PlayHaptic(HapticType type)`) remains unchanged for your game logic.
///
/// It also provides global control over haptics (enable/disable via `HapticsEnabled`).
/// </remarks>
public class HapticsManager : MonoBehaviour
{
    // ============================================================================================================
    // 1. Singleton Implementation
    // Ensures only one instance of the HapticsManager exists throughout the application.
    // ============================================================================================================

    private static HapticsManager _instance;

    /// <summary>
    /// Gets the singleton instance of the HapticsManager.
    /// Access this property from any script to trigger haptic feedback: `HapticsManager.Instance.PlayHaptic(...)`.
    /// </summary>
    public static HapticsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene.
                _instance = FindObjectOfType<HapticsManager>();

                if (_instance == null)
                {
                    // If no instance exists, create a new GameObject and attach the manager.
                    GameObject go = new GameObject("HapticsManager");
                    _instance = go.AddComponent<HapticsManager>();
                    Debug.Log("HapticsManager: Created new instance in scene.");
                }
            }
            return _instance;
        }
    }

    [Tooltip("Globally enable or disable haptic feedback. This can be controlled via game settings.")]
    [SerializeField]
    private bool _hapticsEnabled = true;

    /// <summary>
    /// Gets or sets the global haptics enable state.
    /// Other scripts can check this to conditionally play haptics,
    /// or change it via user settings (e.g., `HapticsManager.Instance.HapticsEnabled = false;`).
    /// The manager itself also checks this before playing any haptic.
    /// </summary>
    public bool HapticsEnabled
    {
        get => _hapticsEnabled;
        set => _hapticsEnabled = value;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If an instance already exists and it's not this one, destroy this duplicate.
            Destroy(gameObject);
            Debug.LogWarning("HapticsManager: Destroying duplicate instance on '" + gameObject.name + "'. " +
                             "Only one HapticsManager should exist.");
        }
        else
        {
            // This is the one and only instance.
            _instance = this;
            // Make sure this GameObject persists across scene loads. This is crucial for a global manager.
            DontDestroyOnLoad(gameObject);
            Debug.Log("HapticsManager: Initialized and set to persist across scenes.");
        }
    }

    // ============================================================================================================
    // 2. Haptic Feedback Types and Public API
    // Defines different semantic types of haptic feedback and the public API to trigger them.
    // ============================================================================================================

    /// <summary>
    /// Defines various semantic types of haptic feedback.
    /// These types allow game logic to request a "feel" (e.g., success, impact) rather than a raw vibration.
    /// The HapticsManager maps these semantic types to actual device vibration patterns.
    /// </summary>
    public enum HapticType
    {
        None,           // No haptic feedback
        LightImpact,    // A soft, short vibration (e.g., UI tap, gentle feedback)
        MediumImpact,   // A noticeable, slightly more distinct vibration (e.g., button press, item collected)
        HeavyImpact,    // A strong, distinct vibration (e.g., hit, major event, heavy object interaction)
        Success,        // A unique pattern indicating success (e.g., quest complete, level up)
        Warning,        // A unique pattern indicating a warning (e.g., low health, near danger)
        Failure         // A unique pattern indicating failure (e.g., failed action, incorrect input)
    }

    /// <summary>
    /// Triggers a specific type of haptic feedback.
    /// This is the primary method for other scripts to request haptics.
    /// </summary>
    /// <param name="type">The semantic type of haptic feedback to play.</param>
    public void PlayHaptic(HapticType type)
    {
        if (!_hapticsEnabled || type == HapticType.None)
        {
            return; // Haptics are globally disabled or no haptic requested.
        }

        // Stop any currently playing haptic patterns to prevent overlapping.
        // This is important for Handheld.Vibrate() as new calls might override previous ones prematurely.
        StopAllCoroutines(); 

        // This switch statement maps the semantic HapticType to concrete vibration patterns.
        // In a more advanced system with native plugins, this is where you'd call
        // platform-specific feedback generators (e.g., iOS: UIImpactFeedbackGenerator, Android: Vibrator API).
        switch (type)
        {
            case HapticType.LightImpact:
                _InternalVibrate(); // A single, quick buzz.
                break;
            case HapticType.MediumImpact:
                // Two very quick pulses to give a slightly more distinct feel than 'LightImpact'.
                StartCoroutine(VibratePattern(0f, 1, 0.07f, 1)); 
                break;
            case HapticType.HeavyImpact:
                // A single, slightly longer 'felt' pulse, followed by another.
                // Simulating a more robust impact.
                StartCoroutine(VibratePattern(0f, 1, 0.15f, 1)); 
                break;
            case HapticType.Success:
                // A short buzz, a pause, then another short buzz.
                StartCoroutine(VibratePattern(0f, 1, 0.1f, 1)); 
                break;
            case HapticType.Warning:
                // Three very rapid, distinct buzzes.
                StartCoroutine(VibratePattern(0f, 1, 0.05f, 1, 0.05f, 1)); 
                break;
            case HapticType.Failure:
                // A noticeable buzz, a slightly longer pause, then two quick buzzes.
                StartCoroutine(VibratePattern(0f, 1, 0.2f, 2)); 
                break;
            default:
                Debug.LogWarning($"HapticsManager: Requested unknown HapticType: {type}");
                break;
        }
    }

    /// <summary>
    /// Provides a direct way to trigger the default device vibration (single pulse).
    /// Useful if you need to bypass predefined types for a simple, one-off buzz.
    /// </summary>
    public void PlayDefaultHaptic()
    {
        if (!_hapticsEnabled)
        {
            return;
        }
        _InternalVibrate();
    }

    // ============================================================================================================
    // 3. Internal Haptics Implementation (Platform-Specific)
    // These methods encapsulate the actual calls to platform-specific haptics APIs.
    // In a full project, this might involve conditional compilation (#if UNITY_IOS, #if UNITY_ANDROID)
    // and calls to native plugins or wrappers.
    // ============================================================================================================

    /// <summary>
    /// The actual call to Unity's built-in vibration API.
    /// This method is intentionally private to keep the public API clean and semantic.
    /// It's the lowest level of haptic feedback available directly in Unity.
    /// </summary>
    private void _InternalVibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        // This is the actual mobile platform vibration call.
        Handheld.Vibrate();
#else
        // For Editor or other platforms, log a message instead of vibrating.
        // This makes debugging easier without needing to build to device every time.
        Debug.Log("HapticsManager: Vibrating (simulated on non-mobile platform)");
#endif
    }

    /// <summary>
    /// Coroutine to play a sequence of `Handheld.Vibrate()` calls with specified delays.
    /// This allows us to simulate more complex haptic "patterns" using the very basic `Handheld.Vibrate()`.
    /// Each pair in the 'pattern' array defines a delay before a set of pulses, and the number of pulses.
    /// </summary>
    /// <param name="pattern">An array of `object`s representing a sequence of {delay (float), pulseCount (int)}.
    ///     - `delay`: A float specifying the time in seconds to wait *before* the subsequent `pulseCount` vibrations.
    ///                The first delay in the sequence (at index 0) is effectively a starting offset.
    ///     - `pulseCount`: An integer specifying how many `_InternalVibrate()` calls to make in this segment.
    ///
    ///     Example: `VibratePattern(0f, 1, 0.1f, 2)` means:
    ///     1. Vibrate immediately (delay 0f), once.
    ///     2. Wait 0.1 seconds.
    ///     3. Vibrate twice.
    /// </param>
    private IEnumerator VibratePattern(params object[] pattern)
    {
        if (pattern == null || pattern.Length == 0 || pattern.Length % 2 != 0)
        {
            Debug.LogError("HapticsManager: Invalid haptic pattern format. Requires pairs of {delay, pulseCount}.");
            yield break;
        }

        for (int i = 0; i < pattern.Length; i += 2)
        {
            float delay = (float)pattern[i];
            int pulseCount = (int)pattern[i + 1];

            // Wait for the specified delay before this set of pulses.
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            for (int j = 0; j < pulseCount; j++)
            {
                _InternalVibrate();
                // Add a very short internal delay between individual pulses within a set
                // to make them feel distinct, rather than a single continuous buzz.
                if (j < pulseCount - 1)
                {
                    yield return new WaitForSeconds(0.05f); // Fixed short delay between pulses
                }
            }
        }
    }
}

/*
// ============================================================================================================
// 4. Example Usage in another script (for demonstration and understanding)
// Copy this into a separate C# script (e.g., 'MyGameEventHandler.cs') and attach to a GameObject.
// ============================================================================================================

using UnityEngine;
using UnityEngine.UI; // If using UI Buttons

public class MyGameEventHandler : MonoBehaviour
{
    [Header("UI Elements (Optional)")]
    public Button lightImpactButton;
    public Button heavyImpactButton;
    public Button successButton;
    public Button failureButton;
    public Toggle hapticsToggle;

    void Start()
    {
        // Initialize the HapticsManager instance if it doesn't already exist.
        // This ensures the manager is ready even if it's not pre-placed in the scene.
        // The 'Instance' getter handles creation.
        _ = HapticsManager.Instance; 

        // Set initial toggle state based on manager
        if (hapticsToggle != null)
        {
            hapticsToggle.isOn = HapticsManager.Instance.HapticsEnabled;
            hapticsToggle.onValueChanged.AddListener(ToggleHapticsSetting);
        }

        // Add listeners to example buttons (if assigned)
        lightImpactButton?.onClick.AddListener(OnLightImpactButtonClick);
        heavyImpactButton?.onClick.AddListener(OnHeavyImpactButtonClick);
        successButton?.onClick.AddListener(OnSuccessButtonClick);
        failureButton?.onClick.AddListener(OnFailureButtonClick);
    }

    // --- Example methods for triggering haptics ---

    public void OnLightImpactButtonClick()
    {
        Debug.Log("Triggering Light Impact Haptic");
        HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.LightImpact);
    }

    public void OnMediumImpactEvent()
    {
        Debug.Log("Triggering Medium Impact Haptic");
        // This could be called when collecting a common item, or a standard UI button press.
        HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.MediumImpact);
    }

    public void OnHeavyImpactButtonClick()
    {
        Debug.Log("Triggering Heavy Impact Haptic");
        // This could be called when the player takes damage, or a major UI element is interacted with.
        HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.HeavyImpact);
    }

    public void OnSuccessButtonClick()
    {
        Debug.Log("Triggering Success Haptic");
        // This could be called when a task is completed, or a critical action succeeds.
        HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.Success);
    }

    public void OnWarningEvent()
    {
        Debug.Log("Triggering Warning Haptic");
        // This could be called when health is low, or a timer is running out.
        HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.Warning);
    }

    public void OnFailureButtonClick()
    {
        Debug.Log("Triggering Failure Haptic");
        // This could be called when an action fails, or an error occurs.
        HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.Failure);
    }

    // --- Example method for controlling global haptics setting ---

    public void ToggleHapticsSetting(bool enable)
    {
        // Update the global setting based on user preference, typically from a settings menu.
        HapticsManager.Instance.HapticsEnabled = enable;
        Debug.Log($"Haptics are now: {(enable ? "Enabled" : "Disabled")}");
    }

    // --- Example usage in Unity's Event System or other game logic ---

    // Imagine this is called when an enemy hits the player.
    public void PlayerTookDamage(int damageAmount)
    {
        if (damageAmount > 50) // Heavy damage
        {
            HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.HeavyImpact);
        }
        else if (damageAmount > 10) // Light damage
        {
            HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.MediumImpact);
        }
        // No haptic for very minor damage.
    }

    // Imagine this is called when a coin is picked up.
    public void PickedUpCoin()
    {
        HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.LightImpact);
    }

    // Imagine this is called when a puzzle is solved.
    public void PuzzleSolved()
    {
        HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.Success);
    }
}


// ============================================================================================================
// How to set up and use this Haptics System in your Unity project:
// ============================================================================================================
//
// 1. Create a C# script named `HapticsManager.cs` in your Unity project and copy the content of the
//    `HapticsManager` class into it.
//
// 2. (Optional but recommended for initial setup) Create an empty GameObject in your scene (e.g., named "Managers").
//    Attach the `HapticsManager.cs` script to this GameObject.
//    (If you don't do this, the HapticsManager.Instance will automatically create a GameObject for itself
//    the first time it's accessed.)
//
// 3. The `HapticsManager` will automatically ensure it's a singleton and persists across scene loads.
//
// 4. To use the example `MyGameEventHandler.cs` script:
//    a. Create another C# script named `MyGameEventHandler.cs` and copy its content into it.
//    b. Create another empty GameObject (e.g., "GameEventsListener") and attach `MyGameEventHandler.cs` to it.
//    c. Optionally, create some UI Buttons (GameObject -> UI -> Button) and a Toggle (GameObject -> UI -> Toggle)
//       in your scene.
//    d. Drag these UI elements from your Hierarchy onto the corresponding public fields
//       (lightImpactButton, hapticsToggle, etc.) in the `MyGameEventHandler` script's Inspector.
//    e. Run your game on a mobile device (Android/iOS) to experience actual vibrations.
//       In the Unity editor, you will see "HapticsManager: Vibrating (simulated...)" log messages.
//
// 5. From any other script in your game, you can now easily trigger haptic feedback like this:
//
//    // Play a haptic for a button click
//    HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.LightImpact);
//
//    // Play a haptic for taking heavy damage
//    HapticsManager.Instance.PlayHaptic(HapticsManager.HapticType.HeavyImpact);
//
//    // Disable all haptics from your game settings
//    HapticsManager.Instance.HapticsEnabled = false;
//
// This structured approach makes your haptic system maintainable, scalable, and easy to modify
// as mobile haptic technologies evolve or as you introduce platform-specific plugins.
*/
```