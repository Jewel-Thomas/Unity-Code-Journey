// Unity Design Pattern Example: ProximityVoiceChat
// This script demonstrates the ProximityVoiceChat pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **ProximityVoiceChat** design pattern in Unity. The core idea is that players can only hear each other if they are within a certain range. This pattern is essential for creating immersive multiplayer experiences where spatial audio and communication are key.

We'll create three main components:
1.  **`ProximityVoiceChatManager`**: A singleton that acts as a central hub for all voice transmissions. It registers/unregisters voice sources and broadcasts all received transmissions.
2.  **`ProximityVoiceSource`**: Attached to any player GameObject that can "speak". It sends its voice messages (simulated here as text) to the manager.
3.  **`ProximityVoiceListener`**: Attached to the local player GameObject that "hears" others. It subscribes to the manager's transmissions, filters them by proximity, and simulates volume attenuation based on distance.

---

### **1. `ProximityVoiceChatManager.cs`**

This is the central hub. It's a singleton MonoBehaviour that manages the registration of all `ProximityVoiceSource` components and acts as a relay for all voice messages.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityDesignPatterns.ProximityVoiceChat
{
    /// <summary>
    /// Represents a single voice transmission event.
    /// This struct holds all necessary information about a voice message
    /// that has been "spoken" by a source.
    /// </summary>
    public struct VoiceTransmission
    {
        public string SenderID;         // Unique identifier of the player who sent the message.
        public string Message;          // The actual voice message (simulated as text here).
        public Vector3 SenderPosition;   // World position of the sender when the message was sent.
        public Color ChatColor;          // Color associated with the sender, for UI differentiation.
        public float TransmissionTime;   // The Unity time at which the message was sent.

        public VoiceTransmission(string senderId, string message, Vector3 senderPosition, Color chatColor)
        {
            SenderID = senderId;
            Message = message;
            SenderPosition = senderPosition;
            ChatColor = chatColor;
            TransmissionTime = Time.time;
        }
    }

    /// <summary>
    /// ProximityVoiceChatManager: Singleton responsible for managing all voice communication.
    /// It registers all active ProximityVoiceSource components and broadcasts all voice transmissions
    /// to interested ProximityVoiceListener components.
    /// </summary>
    public class ProximityVoiceChatManager : MonoBehaviour
    {
        // Singleton instance pattern
        private static ProximityVoiceChatManager _instance;
        public static ProximityVoiceChatManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ProximityVoiceChatManager>();
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(ProximityVoiceChatManager).Name);
                        _instance = singletonObject.AddComponent<ProximityVoiceChatManager>();
                        Debug.Log($"ProximityVoiceChatManager created as a new GameObject: {singletonObject.name}");
                    }
                }
                return _instance;
            }
        }

        // Action event that listeners can subscribe to.
        // This event is invoked whenever a ProximityVoiceSource sends a voice message.
        public event Action<VoiceTransmission> OnVoiceTransmissionReceived;

        // List to keep track of all active voice sources.
        // While not directly used for transmission routing (event does that),
        // it can be useful for other manager functionalities or debugging.
        private List<ProximityVoiceSource> _activeVoiceSources = new List<ProximityVoiceSource>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Ensure only one instance exists. Destroy new ones.
                Destroy(gameObject);
                Debug.LogWarning("Duplicate ProximityVoiceChatManager found and destroyed.");
            }
            else
            {
                _instance = this;
                // Optional: Make the manager persist across scenes if needed for a global chat system.
                // DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Registers a ProximityVoiceSource with the manager.
        /// Called by ProximityVoiceSource.OnEnable().
        /// </summary>
        /// <param name="source">The ProximityVoiceSource to register.</param>
        public void RegisterSource(ProximityVoiceSource source)
        {
            if (!_activeVoiceSources.Contains(source))
            {
                _activeVoiceSources.Add(source);
                Debug.Log($"ProximityVoiceSource '{source.PlayerID}' registered.");
            }
        }

        /// <summary>
        /// Unregisters a ProximityVoiceSource from the manager.
        /// Called by ProximityVoiceSource.OnDisable().
        /// </summary>
        /// <param name="source">The ProximityVoiceSource to unregister.</param>
        public void UnregisterSource(ProximityVoiceSource source)
        {
            if (_activeVoiceSources.Remove(source))
            {
                Debug.Log($"ProximityVoiceSource '{source.PlayerID}' unregistered.");
            }
        }

        /// <summary>
        /// Called by a ProximityVoiceSource when it "speaks".
        /// The manager packages the transmission details and broadcasts them via an event.
        /// </summary>
        /// <param name="senderID">Unique ID of the player sending the message.</param>
        /// <param name="message">The content of the voice message.</param>
        /// <param name="senderPosition">The world position of the sender.</param>
        /// <param name="chatColor">The chat color of the sender.</param>
        public void TransmitVoice(string senderID, string message, Vector3 senderPosition, Color chatColor)
        {
            VoiceTransmission transmission = new VoiceTransmission(senderID, message, senderPosition, chatColor);
            
            // Invoke the event, notifying all subscribed listeners.
            OnVoiceTransmissionReceived?.Invoke(transmission);
            
            // For debugging:
            // Debug.Log($"Transmission from '{senderID}': '{message}' at {senderPosition}");
        }

        // You might add other functionalities here, like:
        // - Get list of all active sources
        // - Mute/unmute specific sources globally
        // - Manage different voice channels (e.g., team chat)
    }
}
```

---

### **2. `ProximityVoiceSource.cs`**

This component is attached to any player character that can send voice. It registers itself with the `ProximityVoiceChatManager` and provides a method to "send" a voice message.

```csharp
using UnityEngine;
using System; // For Guid

namespace UnityDesignPatterns.ProximityVoiceChat
{
    /// <summary>
    /// ProximityVoiceSource: Component attached to GameObjects that can "speak".
    /// It registers itself with the ProximityVoiceChatManager and provides a method
    /// to send simulated voice messages.
    /// </summary>
    public class ProximityVoiceSource : MonoBehaviour
    {
        [Tooltip("Unique identifier for this player. If empty, a GUID will be generated.")]
        public string PlayerID;

        [Tooltip("Color to display this player's chat messages.")]
        public Color ChatColor = Color.white;

        [Header("Simulated Voice (for testing)")]
        [Tooltip("If true, this source will periodically send a test message.")]
        public bool SimulateTalking = true;
        [Tooltip("Interval between simulated messages in seconds.")]
        public float SimulatedTalkInterval = 5f;
        private float _nextSimulatedTalkTime;
        private int _messageCounter = 0;

        private void Awake()
        {
            // Ensure PlayerID is unique. If not set in editor, generate one.
            if (string.IsNullOrEmpty(PlayerID))
            {
                PlayerID = Guid.NewGuid().ToString().Substring(0, 8); // Shorten for readability
                Debug.LogWarning($"ProximityVoiceSource on {gameObject.name} had no PlayerID, assigned '{PlayerID}'.", this);
            }

            // Set a random color if default (white)
            if (ChatColor == Color.white && SimulateTalking)
            {
                ChatColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            }
        }

        private void OnEnable()
        {
            // Register with the manager when enabled.
            ProximityVoiceChatManager.Instance.RegisterSource(this);
            _nextSimulatedTalkTime = Time.time + SimulatedTalkInterval; // Initialize for first simulated talk
        }

        private void OnDisable()
        {
            // Unregister from the manager when disabled or destroyed.
            if (ProximityVoiceChatManager.Instance != null)
            {
                ProximityVoiceChatManager.Instance.UnregisterSource(this);
            }
        }

        private void Update()
        {
            // Simulate sending voice messages for testing purposes.
            if (SimulateTalking && Time.time >= _nextSimulatedTalkTime)
            {
                _messageCounter++;
                SendVoiceMessage($"Hello from {PlayerID}! (Msg {_messageCounter})");
                _nextSimulatedTalkTime = Time.time + SimulatedTalkInterval + UnityEngine.Random.Range(-SimulatedTalkInterval / 2, SimulatedTalkInterval / 2);
            }
        }

        /// <summary>
        /// Sends a voice message through the ProximityVoiceChatManager.
        /// In a real application, this would be triggered by microphone input or a push-to-talk button.
        /// </summary>
        /// <param name="message">The text message to simulate the voice content.</param>
        public void SendVoiceMessage(string message)
        {
            // Ensure the manager exists before trying to send.
            if (ProximityVoiceChatManager.Instance != null)
            {
                ProximityVoiceChatManager.Instance.TransmitVoice(PlayerID, message, transform.position, ChatColor);
            }
            else
            {
                Debug.LogWarning("ProximityVoiceChatManager instance not found. Cannot send voice message.", this);
            }
        }
    }
}
```

---

### **3. `ProximityVoiceListener.cs`**

This component is attached to the local player GameObject (the one currently controlled by the user) that needs to hear other players. It subscribes to all transmissions from the manager and then filters them based on its `HearingRange` and simulates volume.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshPro, install via Window -> TextMeshPro -> Import TMP Essential Resources
using System.Collections.Generic;

namespace UnityDesignPatterns.ProximityVoiceChat
{
    /// <summary>
    /// ProximityVoiceListener: Component attached to the local player that "hears" other players.
    /// It subscribes to all voice transmissions from the manager, filters them by proximity,
    /// and simulates volume attenuation based on distance.
    /// </summary>
    [RequireComponent(typeof(AudioSource))] // Optional: for playing simulated audio
    public class ProximityVoiceListener : MonoBehaviour
    {
        [Header("Listening Settings")]
        [Tooltip("The maximum distance at which this listener can hear a voice source.")]
        public float HearingRange = 20f;
        [Tooltip("The distance at which a voice source will be heard at maximum (100%) volume.")]
        public float MaxVolumeDistance = 5f;
        [Tooltip("Minimum simulated volume percentage for a message to be heard (e.g., 0.1 for 10%).")]
        [Range(0f, 1f)] public float MinHeardVolume = 0.1f;

        [Header("UI Display")]
        [Tooltip("TextMeshProUGUI component to display received chat messages.")]
        public TextMeshProUGUI ChatDisplay;
        [Tooltip("Maximum number of messages to display in the chat window.")]
        public int MaxDisplayedMessages = 5;
        [Tooltip("How long a message stays visible in the chat window (in seconds).")]
        public float MessageDisplayDuration = 10f;

        // Stores messages along with their display end time.
        private List<(string text, Color color, float endTime)> _currentDisplayedMessages = new List<(string, Color, float)>();

        // For optional simulated audio feedback
        private AudioSource _audioSource;
        [Tooltip("Optional AudioClip to play when a message is heard.")]
        public AudioClip SimulatedVoiceClip;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                Debug.LogError("ProximityVoiceListener requires an AudioSource component.", this);
            }
            _audioSource.spatialBlend = 1.0f; // Make it 3D sound
            _audioSource.volume = 0.5f; // Base volume for simulated clips
            _audioSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            // Subscribe to the manager's voice transmission event when enabled.
            if (ProximityVoiceChatManager.Instance != null)
            {
                ProximityVoiceChatManager.Instance.OnVoiceTransmissionReceived += HandleVoiceTransmission;
            }
            else
            {
                Debug.LogError("ProximityVoiceChatManager not found. Ensure it's in the scene.", this);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from the event when disabled or destroyed to prevent memory leaks.
            if (ProximityVoiceChatManager.Instance != null)
            {
                ProximityVoiceChatManager.Instance.OnVoiceTransmissionReceived -= HandleVoiceTransmission;
            }
        }

        private void Update()
        {
            // Update the UI display to remove expired messages
            UpdateChatDisplay();
        }

        /// <summary>
        /// Handles incoming voice transmissions from the ProximityVoiceChatManager.
        /// This method acts as the core of the proximity check and volume attenuation.
        /// </summary>
        /// <param name="transmission">The voice transmission data.</param>
        private void HandleVoiceTransmission(VoiceTransmission transmission)
        {
            // Do not process messages sent by this listener itself.
            // In a real networked game, this check might be more sophisticated (e.g., matching net IDs).
            ProximityVoiceSource localSource = GetComponent<ProximityVoiceSource>();
            if (localSource != null && localSource.PlayerID == transmission.SenderID)
            {
                // This is our own transmission, we don't need to "hear" ourselves through the system.
                return;
            }

            // Calculate distance between this listener and the sender.
            float distance = Vector3.Distance(transform.position, transmission.SenderPosition);

            // Check if the sender is within hearing range.
            if (distance <= HearingRange)
            {
                // Calculate simulated volume based on distance.
                // Linear falloff: 100% volume at MaxVolumeDistance, 0% at HearingRange.
                float volume = 1f;
                if (distance > MaxVolumeDistance)
                {
                    volume = 1f - ((distance - MaxVolumeDistance) / (HearingRange - MaxVolumeDistance));
                }
                
                // Clamp volume to ensure it's within [MinHeardVolume, 1]
                volume = Mathf.Clamp(volume, MinHeardVolume, 1f);

                // If the volume is too low to be heard, skip processing.
                if (volume <= 0.001f) // Use a small epsilon to avoid floating point issues with 0
                {
                    return;
                }

                // Log the message to the console with simulated volume.
                Debug.Log($"<color={ColorUtility.ToHtmlStringRGB(transmission.ChatColor)}>[LISTENER {gameObject.name}]</color> Heard '{transmission.SenderID}': '{transmission.Message}' (Distance: {distance:F2}m, Volume: {volume:P0})", this);

                // Add message to display list
                string displayMessage = $"[{transmission.SenderID}] {transmission.Message}";
                _currentDisplayedMessages.Add((displayMessage, transmission.ChatColor, Time.time + MessageDisplayDuration));

                // Optional: Play a simulated audio clip.
                if (SimulatedVoiceClip != null && _audioSource != null)
                {
                    // For more precise 3D audio, you'd set the AudioSource position to the sender's position
                    // and use _audioSource.PlayOneShot(SimulatedVoiceClip, volume);
                    // For simplicity, we're just playing it at our own listener's position.
                    // Or, even better, play a one-shot clip at sender's position:
                    AudioSource.PlayClipAtPoint(SimulatedVoiceClip, transmission.SenderPosition, volume * _audioSource.volume);
                }
            }
        }

        /// <summary>
        /// Updates the UI chat display by removing expired messages and formatting current ones.
        /// </summary>
        private void UpdateChatDisplay()
        {
            if (ChatDisplay == null) return;

            // Remove expired messages
            _currentDisplayedMessages.RemoveAll(msg => Time.time > msg.endTime);

            // Keep only the newest messages if we exceed the max count
            while (_currentDisplayedMessages.Count > MaxDisplayedMessages)
            {
                _currentDisplayedMessages.RemoveAt(0); // Remove the oldest message
            }

            // Build the string for the TextMeshProUGUI display
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var msg in _currentDisplayedMessages)
            {
                sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(msg.color)}>{msg.text}</color>");
            }
            ChatDisplay.text = sb.ToString();
        }

        /// <summary>
        /// Visualizes the hearing range in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, HearingRange);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, MaxVolumeDistance);
        }
    }
}
```

---

### **Example Usage in Unity Project:**

1.  **Create a New Unity Project** (or open an existing one).
2.  **Install TextMeshPro:** Go to `Window > TextMeshPro > Import TMP Essential Resources`.
3.  **Create a Folder** named `Scripts/UnityDesignPatterns/ProximityVoiceChat` and place the three C# files (`ProximityVoiceChatManager.cs`, `ProximityVoiceSource.cs`, `ProximityVoiceListener.cs`) inside it.
4.  **Create the Manager:**
    *   Create an empty GameObject in your scene and name it `ProximityVoiceChatManager`.
    *   Attach the `ProximityVoiceChatManager.cs` script to it.
5.  **Create Player GameObjects (Sources):**
    *   Create a 3D Cube GameObject and name it `Player1`.
    *   Attach `ProximityVoiceSource.cs` to `Player1`.
    *   In the Inspector for `Player1`'s `ProximityVoiceSource`:
        *   Set `Player ID` to `Alice`.
        *   Set `Chat Color` to a distinct color (e.g., Red).
        *   Ensure `Simulate Talking` is checked.
    *   Duplicate `Player1` (Ctrl+D / Cmd+D), rename it `Player2`.
    *   For `Player2`'s `ProximityVoiceSource`:
        *   Set `Player ID` to `Bob`.
        *   Set `Chat Color` to another distinct color (e.g., Blue).
        *   Ensure `Simulate Talking` is checked.
    *   Position `Player1` and `Player2` a good distance apart (e.g., `Player1` at `(0, 0, 0)`, `Player2` at `(25, 0, 0)`).
6.  **Create the Local Player GameObject (Listener):**
    *   Create another 3D Cube GameObject and name it `LocalPlayer`.
    *   Attach `ProximityVoiceListener.cs` to `LocalPlayer`.
    *   Attach `ProximityVoiceSource.cs` to `LocalPlayer` (this player can also talk).
    *   For `LocalPlayer`'s `ProximityVoiceSource`:
        *   Set `Player ID` to `You`.
        *   Set `Chat Color` to Green.
        *   You can toggle `Simulate Talking` on/off for yourself.
    *   Attach an `AudioSource` component to `LocalPlayer` (it's required by `ProximityVoiceListener`). You can drag any short `AudioClip` into the `Simulated Voice Clip` slot on the `ProximityVoiceListener` if you want sound feedback.
    *   Position `LocalPlayer` somewhere in between `Player1` and `Player2` (e.g., `(10, 0, 0)`).
7.  **Create the UI for Chat Display:**
    *   Create a new UI Canvas (`GameObject > UI > Canvas`).
    *   Inside the Canvas, create a `TextMeshPro - Text (UI)` (`GameObject > UI > Text - TextMeshPro`).
    *   Rename it `ChatDisplay`.
    *   Adjust its size and position on the screen (e.g., anchor to bottom-left, width 400, height 200).
    *   Set `Alignment` to `Bottom Left`.
    *   Drag this `ChatDisplay` TextMeshPro object into the `Chat Display` slot of the `LocalPlayer`'s `ProximityVoiceListener` component.
8.  **Add a Simple Movement Script (Optional but Recommended for Testing Proximity):**
    *   Create a new C# script named `PlayerMovement.cs`.
    *   Add the following code:
        ```csharp
        using UnityEngine;

        public class PlayerMovement : MonoBehaviour
        {
            public float Speed = 5f;
            public float RotationSpeed = 100f;

            void Update()
            {
                // Basic movement (WASD)
                float horizontalInput = Input.GetAxis("Horizontal");
                float verticalInput = Input.GetAxis("Vertical");

                Vector3 moveDirection = transform.forward * verticalInput;
                transform.position += moveDirection * Speed * Time.deltaTime;

                // Rotation (Q/E or mouse X)
                float rotationInput = 0;
                if (Input.GetKey(KeyCode.Q)) rotationInput = -1;
                if (Input.GetKey(KeyCode.E)) rotationInput = 1;
                // Alternatively: rotationInput = Input.GetAxis("Mouse X");

                transform.Rotate(Vector3.up, rotationInput * RotationSpeed * Time.deltaTime);

                // Press 'T' to make this player speak (if ProximityVoiceSource is attached)
                if (Input.GetKeyDown(KeyCode.T))
                {
                    UnityDesignPatterns.ProximityVoiceChat.ProximityVoiceSource source = GetComponent<UnityDesignPatterns.ProximityVoiceChat.ProximityVoiceSource>();
                    if (source != null)
                    {
                        source.SendVoiceMessage("Testing manual talk!");
                    }
                }
            }
        }
        ```
    *   Attach `PlayerMovement.cs` to `LocalPlayer`.
    *   You can also attach it to `Player1` and `Player2` if you want to manually move them. Alternatively, you could add a simple `Rigidbody` and basic physics to your cubes to allow them to move around with collisions or just manually drag them in the editor.

**Run the Scene:**

*   Press Play.
*   You will see messages appearing in the `ChatDisplay` UI from `Alice` and `Bob` only when `LocalPlayer` is within their `HearingRange`.
*   Move `LocalPlayer` closer to `Alice` (or `Bob`). You should see their messages appear in the chat UI and in the console log.
*   Move `LocalPlayer` further away, and their messages will stop appearing.
*   The `OnDrawGizmos` in `ProximityVoiceListener` will show a **cyan sphere** representing the `HearingRange` and a **green sphere** representing the `MaxVolumeDistance` around `LocalPlayer` in the Scene view.
*   If you press 'T' while controlling `LocalPlayer`, you will send a message (which only other listeners would hear, if you had them, as the listener script ignores its own source's messages).

This setup provides a complete and practical demonstration of the ProximityVoiceChat design pattern, showcasing how to manage sources, transmit messages, and filter/process them based on spatial proximity and simulated volume.