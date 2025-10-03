// Unity Design Pattern Example: CharacterMoodSystem
// This script demonstrates the CharacterMoodSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **CharacterMoodSystem** design pattern in Unity using a combination of the **State Pattern** (character's mood is a state) and the **Observer Pattern** (other systems react to mood changes via events).

This setup ensures that:
1.  The character's mood is centrally managed.
2.  Any part of your game can react to mood changes without needing to know *how* or *why* the mood changed (decoupling).
3.  Adding new moods or new reactions is easy and doesn't require modifying existing core logic.

```csharp
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using TMPro; // Required for TextMeshProUGUI (Unity's modern UI text system)
using System.Collections.Generic; // Required for Dictionary

namespace CharacterMoodSystemExample
{
    /// <summary>
    /// Defines the possible mood states for a character.
    /// This enum acts as the core data type for our Character Mood System.
    /// New moods can be added here easily.
    /// </summary>
    public enum MoodType
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Scared,
        Excited
    }

    /// <summary>
    /// Represents the Character Mood System design pattern.
    /// This is the central component responsible for managing a character's current mood.
    ///
    /// It implements the **State Pattern** by holding the 'CurrentMood' state
    /// and the **Observer Pattern** by using a UnityEvent to notify other systems
    /// when the character's mood changes, effectively decoupling the mood state from its reactions.
    /// </summary>
    [DisallowMultipleComponent] // Ensures only one mood system per character GameObject
    public class CharacterMoodSystem : MonoBehaviour
    {
        [Header("Mood Settings")]
        [Tooltip("The initial mood of the character when the game starts.")]
        [SerializeField] private MoodType _initialMood = MoodType.Neutral;

        [Tooltip("The current mood of the character. Can only be changed via public methods.")]
        private MoodType _currentMood;

        /// <summary>
        /// Gets the current mood of the character. This is read-only from outside.
        /// </summary>
        public MoodType CurrentMood => _currentMood;

        /// <summary>
        /// An event that is invoked whenever the character's mood changes.
        /// Other components can subscribe to this event to react to mood changes.
        /// This is the core of the **Observer pattern** implementation:
        /// - CharacterMoodSystem is the 'Subject' or 'Publisher'.
        /// - Components that subscribe are 'Observers' or 'Subscribers'.
        /// </summary>
        [Header("Events")]
        [Tooltip("Event fired when the character's mood changes. Listeners receive the new MoodType.")]
        public UnityEvent<MoodType> OnMoodChanged = new UnityEvent<MoodType>();

        private void Awake()
        {
            // Initialize the current mood with the specified initial mood.
            _currentMood = _initialMood;
        }

        private void Start()
        {
            // Immediately invoke the event to notify all subscribers of the initial mood.
            // This ensures that reaction components start in the correct state (e.g., showing the initial sprite).
            OnMoodChanged.Invoke(_currentMood);
            Debug.Log($"[CharacterMoodSystem] Initialized with mood: {_currentMood}");
        }

        /// <summary>
        /// Sets the character's mood to a new state.
        /// This is the primary method for changing the character's mood from any external system.
        /// </summary>
        /// <param name="newMood">The new mood type to set.</param>
        public void SetMood(MoodType newMood)
        {
            // Only change mood and invoke event if the mood is actually different
            // to avoid unnecessary updates and event calls, optimizing performance.
            if (_currentMood != newMood)
            {
                MoodType oldMood = _currentMood;
                _currentMood = newMood;
                Debug.Log($"[CharacterMoodSystem] Mood changed from {oldMood} to {_currentMood}");

                // Invoke the event, notifying all subscribed components of the mood change.
                // This is where the Observer pattern truly shines, broadcasting the state change.
                OnMoodChanged.Invoke(_currentMood);
            }
            else
            {
                Debug.Log($"[CharacterMoodSystem] Mood is already {_currentMood}. No change needed.");
            }
        }

        // --- Example Public Methods for Editor/UI Interaction ---
        // These methods provide easy ways to hook up UI buttons, animation events,
        // or other game events directly in the Unity Editor Inspector
        // to change the character's mood without writing extra code.

        public void MakeHappy() { SetMood(MoodType.Happy); }
        public void MakeSad() { SetMood(MoodType.Sad); }
        public void MakeAngry() { SetMood(MoodType.Angry); }
        public void MakeScared() { SetMood(MoodType.Scared); }
        public void MakeExcited() { SetMood(MoodType.Excited); }
        public void MakeNeutral() { SetMood(MoodType.Neutral); }

        // --- Further Enhancements (Ideas for a more complex system) ---
        // - Mood decay over time: Gradually shift mood back to neutral.
        // - Mood thresholds/modifiers: An emotional value that contributes to current mood.
        // - Complex mood transition logic: Rules for how moods can change (e.g., can't go directly from Happy to Angry without passing Neutral).
        // - Duration for moods: Some moods might only last for a specific time.
    }

    /// <summary>
    /// A component that demonstrates how different game elements can react to mood changes
    /// from a CharacterMoodSystem.
    ///
    /// This component acts as an 'Observer' or 'Subscriber' in the **Observer pattern**.
    /// It subscribes to the CharacterMoodSystem's 'OnMoodChanged' event and updates
    /// visual, audio, and UI elements based on the new mood.
    /// </summary>
    public class MoodReactionComponent : MonoBehaviour
    {
        [Header("References (Drag components here)")]
        [Tooltip("The CharacterMoodSystem this component will observe.")]
        [SerializeField] private CharacterMoodSystem _moodSystem;

        [Tooltip("SpriteRenderer component to change character's visual appearance.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Tooltip("TextMeshProUGUI component to display the current mood as text.")]
        [SerializeField] private TextMeshProUGUI _moodText;

        [Tooltip("AudioSource component to play sounds associated with moods.")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Mood Specific Data")]
        [Tooltip("Default sprite to use if no specific mood sprite is assigned.")]
        [SerializeField] private Sprite _defaultSprite;

        /// <summary>
        /// A serializable struct to hold all data associated with a specific mood type.
        /// Making it System.Serializable allows it to be edited directly in the Unity Inspector.
        /// </summary>
        [System.Serializable]
        private struct MoodData
        {
            public MoodType moodType;
            public Sprite sprite;
            public Color color;
            public AudioClip sound;
            public GameObject particlePrefab; // Example: for particle effects specific to a mood
        }

        [Tooltip("Define visual, audio, and particle reactions for each mood type.")]
        [SerializeField] private MoodData[] _moodDefinitions; // Exposed in Inspector
        private Dictionary<MoodType, MoodData> _moodDataMap = new Dictionary<MoodType, MoodData>(); // For fast runtime lookup

        private GameObject _activeParticlesInstance; // To keep track of instantiated particles for cleanup

        private void Awake()
        {
            // Populate the dictionary from the inspector-defined array for efficient lookups at runtime.
            foreach (var data in _moodDefinitions)
            {
                // Warn if duplicate mood types are found, as this indicates a potential setup error.
                if (_moodDataMap.ContainsKey(data.moodType))
                {
                    Debug.LogWarning($"[MoodReactionComponent] Duplicate MoodType '{data.moodType}' found in _moodDefinitions. " +
                                     "Only the last entry for this mood will be used.", this);
                }
                _moodDataMap[data.moodType] = data;
            }

            // Basic error checking to ensure essential references are set in the Inspector.
            if (_moodSystem == null)
            {
                Debug.LogError("[MoodReactionComponent] MoodSystem reference is not set! This component will not function.", this);
                enabled = false; // Disable component if it cannot observe the MoodSystem.
                return;
            }
            // Optional components: log warnings if not set, but don't disable.
            if (_spriteRenderer == null) Debug.LogWarning("[MoodReactionComponent] SpriteRenderer reference is not set. Visual feedback will be limited.", this);
            if (_moodText == null) Debug.LogWarning("[MoodReactionComponent] TextMeshProUGUI reference is not set. UI text feedback will be missing.", this);
            if (_audioSource == null) Debug.LogWarning("[MoodReactionComponent] AudioSource reference is not set. Audio feedback will be missing.", this);
        }

        private void OnEnable()
        {
            // Subscribe to the mood change event when this component is enabled.
            // This is crucial for the Observer pattern to work: 'AddListener' makes this component an Observer.
            if (_moodSystem != null)
            {
                _moodSystem.OnMoodChanged.AddListener(HandleMoodChanged);
                Debug.Log("[MoodReactionComponent] Subscribed to CharacterMoodSystem.OnMoodChanged event.");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from the mood change event when this component is disabled.
            // This is essential to prevent memory leaks and ensure proper cleanup,
            // especially when objects are destroyed or components are disabled.
            if (_moodSystem != null)
            {
                _moodSystem.OnMoodChanged.RemoveListener(HandleMoodChanged);
                Debug.Log("[MoodReactionComponent] Unsubscribed from CharacterMoodSystem.OnMoodChanged event.");
            }
        }

        /// <summary>
        /// This method is called by the CharacterMoodSystem whenever the mood changes.
        /// It contains the specific logic for how this component reacts to different moods.
        /// </summary>
        /// <param name="newMood">The new mood type of the character.</param>
        private void HandleMoodChanged(MoodType newMood)
        {
            Debug.Log($"[MoodReactionComponent] Reacting to new mood: {newMood}");

            // Retrieve mood-specific data from the dictionary.
            _moodDataMap.TryGetValue(newMood, out MoodData moodSpecificData);

            // 1. Update visual (sprite, color) if a SpriteRenderer is present.
            if (_spriteRenderer != null)
            {
                // If a specific sprite is defined for the mood, use it; otherwise, use the default.
                _spriteRenderer.sprite = moodSpecificData.sprite != null ? moodSpecificData.sprite : _defaultSprite;
                // If a specific color is defined, use it; otherwise, use white.
                _spriteRenderer.color = moodSpecificData.color != default ? moodSpecificData.color : Color.white;
            }

            // 2. Update UI text if a TextMeshProUGUI component is present.
            if (_moodText != null)
            {
                _moodText.text = $"Mood: {newMood}";
                // Optional: change text color based on mood.
                _moodText.color = moodSpecificData.color != default ? moodSpecificData.color : Color.black;
            }

            // 3. Play sound if an AudioSource is present and a sound is defined for the mood.
            if (_audioSource != null && moodSpecificData.sound != null)
            {
                _audioSource.PlayOneShot(moodSpecificData.sound);
            }

            // 4. Handle particle effects (example: instantiate and clean up previous ones).
            // First, destroy any previously active particle instances to ensure only one runs at a time.
            if (_activeParticlesInstance != null)
            {
                Destroy(_activeParticlesInstance);
                _activeParticlesInstance = null;
            }
            // If a particle prefab is defined for the new mood, instantiate it.
            if (moodSpecificData.particlePrefab != null)
            {
                // Instantiate particles at the character's position and make them a child for easier management.
                _activeParticlesInstance = Instantiate(moodSpecificData.particlePrefab, transform.position, Quaternion.identity, transform);
            }

            // --- Further reaction examples (conceptual) ---
            // You can extend this method to trigger AI behavior, play animations, or update game logic:
            // if (newMood == MoodType.Angry)
            // {
            //     GetComponent<CharacterAI>()?.EnterAggressiveState(); // Example: AI becomes aggressive
            // }
            // else if (newMood == MoodType.Scared)
            // {
            //     GetComponent<CharacterAI>()?.Flee(); // Example: AI flees
            // }
            // GetComponent<Animator>()?.SetTrigger(newMood.ToString()); // Example: Trigger specific animation
        }
    }

    /// <summary>
    /// A simple demonstration script to control the character's mood using keyboard input.
    /// This simulates external game events, player input, or other game systems
    /// interacting with the CharacterMoodSystem to change the character's emotional state.
    /// </summary>
    public class MoodControllerInput : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The CharacterMoodSystem to control.")]
        [SerializeField] private CharacterMoodSystem _moodSystem;

        [Header("Key Bindings")]
        [SerializeField] private KeyCode _happyKey = KeyCode.H;
        [SerializeField] private KeyCode _sadKey = KeyCode.S;
        [SerializeField] private KeyCode _angryKey = KeyCode.A;
        [SerializeField] private KeyCode _neutralKey = KeyCode.N;
        [SerializeField] private KeyCode _scaredKey = KeyCode.C;
        [SerializeField] private KeyCode _excitedKey = KeyCode.E;

        private void Update()
        {
            if (_moodSystem == null) return; // Ensure the mood system is assigned.

            // Check for key presses and tell the MoodSystem to change its mood.
            // Note: This script only interacts with the CharacterMoodSystem.
            // It does not directly interact with MoodReactionComponent, demonstrating decoupling.
            if (Input.GetKeyDown(_happyKey)) _moodSystem.MakeHappy();
            else if (Input.GetKeyDown(_sadKey)) _moodSystem.MakeSad();
            else if (Input.GetKeyDown(_angryKey)) _moodSystem.MakeAngry();
            else if (Input.GetKeyDown(_neutralKey)) _moodSystem.MakeNeutral();
            else if (Input.GetKeyDown(_scaredKey)) _moodSystem.MakeScared();
            else if (Input.GetKeyDown(_excitedKey)) _moodSystem.MakeExcited();
        }
    }
}

/*
## How to Implement and Use the CharacterMoodSystem in Unity:

This guide will walk you through setting up a simple character that changes its visual,
audio, and UI based on its mood using the provided scripts.

**1. Set up your Unity Scene:**

   a.  **Create a Character GameObject:**
       -   In your scene (e.g., a new 2D scene), create an empty GameObject. Rename it to "MyCharacter".
       -   Add a `SpriteRenderer` component to "MyCharacter". You'll need a basic sprite for your character (e.g., a square, circle, or character sprite). Drag this sprite into the `Sprite` field of the `SpriteRenderer`.
       -   Add an `AudioSource` component to "MyCharacter". This will be used to play sounds for different moods.
       -   (Optional, for particle effects) Create a simple particle system (GameObject -> Effects -> Particle System). Adjust it to be a small burst or emission. Save it as a Prefab in your Project window (e.g., "HappyParticles", "AngrySmoke").

   b.  **Create a UI Text Element (for mood display):**
       -   Right-click in the Hierarchy -> UI -> Canvas.
       -   Right-click on the newly created Canvas -> UI -> Text - TextMeshPro. If prompted, import the TMP Essentials.
       -   Position this Text element somewhere visible on your screen. You might want to make it a child of "MyCharacter" or position it above it on the Canvas.

**2. Attach the Core Scripts to "MyCharacter":**

   a.  **Attach `CharacterMoodSystem.cs`:**
       -   Drag the `CharacterMoodSystem` script from your Project window onto your "MyCharacter" GameObject in the Hierarchy.
       -   In the Inspector, for the `Character Mood System` component:
           -   `Initial Mood`: Set this to your desired starting mood (e.g., `Neutral`).

   b.  **Attach `MoodReactionComponent.cs`:**
       -   Drag the `MoodReactionComponent` script onto your "MyCharacter" GameObject.
       -   In the Inspector, for the `Mood Reaction Component`:
           -   **References:**
               -   `Mood System`: Drag "MyCharacter" from the Hierarchy into this field. (It will automatically link to the `CharacterMoodSystem` component on it).
               -   `Sprite Renderer`: Drag the `SpriteRenderer` component from "MyCharacter" into this field.
               -   `Mood Text`: Drag the `TextMeshProUGUI` component you created earlier (from your Canvas) into this field.
               -   `Audio Source`: Drag the `AudioSource` component from "MyCharacter" into this field.
           -   **Mood Specific Data:**
               -   `Default Sprite`: Drag your character's default sprite here.
               -   **`Mood Definitions` array:** This is where you configure reactions for each mood.
                   -   Click the "+" button to add entries. Create one entry for each `MoodType` you want to define (e.g., Happy, Sad, Angry, Neutral, etc.).
                   -   For each entry:
                       -   `Mood Type`: Select the corresponding mood from the dropdown (e.g., `Happy`).
                       -   `Sprite`: Drag a unique sprite that visually represents this mood (e.g., a smiley face sprite for `Happy`, a frowny face for `Sad`).
                       -   `Color`: Choose a color for this mood (e.g., yellow for `Happy`, blue for `Sad`). This will change the sprite's color and the mood text color.
                       -   `Sound`: Drag an `AudioClip` (e.g., a happy jingle, a sigh) that plays when this mood is active.
                       -   `Particle Prefab`: (Optional) Drag one of your particle system Prefabs here (e.g., "HappyParticles" for `Happy`).

**3. Attach `MoodControllerInput.cs` (for testing/demonstration):**

   -   Drag the `MoodControllerInput` script onto your "MyCharacter" GameObject.
   -   In the Inspector, for the `Mood Controller Input`:
       -   `Mood System`: Drag "MyCharacter" from the Hierarchy into this field.
       -   `Key Bindings`: You can customize the keyboard keys that trigger each mood. (Default: H for Happy, S for Sad, A for Angry, N for Neutral, C for Scared, E for Excited).

**4. Run Your Scene:**

   -   Play the scene!
   -   Your character's sprite, color, UI text, and potential particles/sounds will immediately update to reflect the `Initial Mood` you set in the `CharacterMoodSystem`.
   -   Press the assigned keys (e.g., H, S, A) to see your character's mood, visuals, audio, and UI dynamically change according to your `Mood Definitions`! Check the Console for debug logs.

---

### Understanding the Character Mood System Design Pattern:

The Character Mood System, as implemented here, combines elements of two powerful design patterns: the **State Pattern** and the **Observer Pattern**.

#### 1. The CharacterMoodSystem (`Subject` / `Publisher` & `State Manager`):

*   **Role:** This is the core brain that manages the character's current emotional state. It's the "source of truth" for the character's mood.
*   **State Management:** It internally holds the `_currentMood` (which is a `MoodType` enum). This represents the character's current state. The `SetMood` method is the only way to transition between these states.
*   **Event-Driven Communication (Observer Pattern):** It exposes a `UnityEvent<MoodType> OnMoodChanged`. When `SetMood` is called and the mood actually changes, `OnMoodChanged.Invoke(newMood)` is triggered.
    *   **Decoupling:** The `CharacterMoodSystem` doesn't know or care *what* happens when the mood changes. It simply announces the change. This means you can add, remove, or change mood reactions without ever touching this core mood management script.
*   **Encapsulation:** The `_currentMood` is private, ensuring that the mood can only be changed through controlled methods (`SetMood`), which then ensures the event is always fired correctly.

#### 2. The MoodReactionComponent (`Observer` / `Subscriber`):

*   **Role:** This component's sole responsibility is to react to mood changes. You can have multiple `MoodReactionComponent`s (e.g., one for visuals, one for AI, one for UI), each handling a specific type of reaction.
*   **Subscription (Observer Pattern):**
    *   In its `OnEnable()` method, it explicitly subscribes to the `CharacterMoodSystem.OnMoodChanged` event using `AddListener()`. This makes it an 'Observer' of the mood system.
    *   In `OnDisable()`, it `RemoveListener()` to prevent memory leaks and ensure it doesn't try to react to events after being disabled or destroyed.
*   **Reaction Logic:** The `HandleMoodChanged(MoodType newMood)` method is the callback that gets executed whenever the `OnMoodChanged` event is invoked. This method contains all the specific logic for updating sprites, colors, UI text, playing sounds, and triggering particles based on the `newMood`.
*   **Data-Driven:** The use of a `Dictionary` (populated from a serialized array) makes it easy to define and manage reactions for different moods directly in the Unity Inspector, making it designer-friendly.

#### 3. The MoodControllerInput (`Client` / `Initiator`):

*   **Role:** This is an example of any other system in your game that might want to *change* the character's mood (e.g., player input, AI deciding to get angry, game event making the character happy, an item causing a temporary mood change).
*   **Interaction:** It simply calls a public method on the `CharacterMoodSystem` (like `_moodSystem.MakeHappy()`) to request a mood change.
*   **Decoupling:** Crucially, `MoodControllerInput` has no direct knowledge of `MoodReactionComponent`. It doesn't need to know how the mood change will manifest; it just tells the `CharacterMoodSystem` the new mood.

---

### Benefits of this Pattern:

*   **Modularity and Reusability:** You can easily create new reaction components (e.g., `MoodAIEngine`, `MoodDialogueModifier`, `MoodAnimationController`) without altering the `CharacterMoodSystem`. Each component focuses on a single responsibility.
*   **Decoupling:** The core mood management logic is completely separated from the specific effects of those moods. This leads to cleaner, more maintainable code and reduces dependencies between systems.
*   **Extensibility:** Adding new moods (e.g., `Anxious`, `Bored`) only requires:
    1.  Adding an entry to the `MoodType` enum.
    2.  Defining new data entries in the `MoodReactionComponent`'s `Mood Definitions` array in the Inspector.
    No need to change core `CharacterMoodSystem` code or modify existing reaction logic.
*   **Clear Communication:** The event system provides a very clear, standardized, and efficient way for different parts of your game to communicate state changes without direct references.
*   **Designer-Friendly:** With `UnityEvent` and serialized data structures, much of the configuration for moods and their reactions can be done directly in the Unity Editor, empowering designers and reducing reliance on programmers for tweaks.
*   **Scalability:** As your game grows, this pattern helps manage complexity by breaking down a large "character behavior" problem into smaller, interconnected, and manageable pieces.
*/
```