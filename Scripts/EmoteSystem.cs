// Unity Design Pattern Example: EmoteSystem
// This script demonstrates the EmoteSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the "EmoteSystem" design pattern, making it practical for real-world game development. The pattern separates emote data from character-specific logic and provides a central way to manage and trigger emotes.

---

### How to Use This Example in Unity:

1.  **Create a new C# script** in your Unity project, name it `EmoteSystemExample`, and copy-paste the entire code below into it.
2.  **Create an Empty GameObject** in your scene (e.g., named "GameManagers").
3.  **Add the `EmoteSystem` component** to this "GameManagers" GameObject.
4.  **Create another Empty GameObject** in your scene (e.g., named "PlayerCharacter").
5.  **Add the `CharacterEmoteHandler` component** to the "PlayerCharacter" GameObject.
6.  **Add the `EmoteSystemDemo` component** to the "PlayerCharacter" GameObject. This script will use keyboard input to trigger emotes.
7.  **Create Emote Data Assets:**
    *   In the Unity Editor, right-click in your Project window (e.g., in a "Data" folder).
    *   Select `Create > Emote System > Emote Data`.
    *   Create several EmoteData assets (e.g., "Emote_Wave", "Emote_Laugh", "Emote_Sad").
    *   Fill in their properties in the Inspector:
        *   **Emote Name:** e.g., "Wave", "Laugh", "Sad" (This is the name you'll use to trigger them).
        *   **Emote Duration:** e.g., 2.5, 3.0, 4.0 seconds.
        *   **Animation Trigger Name:** e.g., "TriggerWave", "TriggerLaugh", "TriggerSad". (You'd set these up in your character's Animator Controller).
        *   **VFX Prefab:** (Optional) Assign a particle system or other visual effect prefab here if you have one.
8.  **Assign Emote Data to EmoteSystem:**
    *   Select your "GameManagers" GameObject.
    *   In the `EmoteSystem` component's Inspector, expand the `Available Emotes` list.
    *   Drag and drop your newly created EmoteData assets into this list.
9.  **Run the scene!**
    *   Press `1` to trigger "Wave".
    *   Press `2` to trigger "Laugh".
    *   Press `3` to trigger "Sad".
    *   Watch the Console for output demonstrating the emote system in action.
    *   Try triggering an emote while another is playing – the current one will be interrupted.

---

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// EmoteSystemExample.cs

namespace EmoteSystem
{
    // ===========================================================================================
    // 1. EmoteData (ScriptableObject) - The Emote Definition
    //    Purpose: Defines what an emote is. Separates data from logic.
    //    Why ScriptableObject: Allows creating reusable emote assets in the Unity Editor,
    //    making it easy to add new emotes without changing code.
    // ===========================================================================================
    /// <summary>
    /// Defines a single emote's properties. This is a ScriptableObject, allowing emote data
    /// to be created as assets in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEmoteData", menuName = "Emote System/Emote Data", order = 1)]
    public class EmoteData : ScriptableObject
    {
        [Tooltip("The unique name of this emote. Used to trigger it.")]
        public string EmoteName = "New Emote";

        [Tooltip("The duration in seconds this emote's animation or effect is expected to last.")]
        public float EmoteDuration = 3.0f;

        [Tooltip("The name of the Animator trigger parameter to activate this emote's animation.")]
        public string AnimationTriggerName = "TriggerEmote";

        [Tooltip("Optional: A Prefab for a visual effect (e.g., particle system) to play with the emote.")]
        public GameObject VFXPrefab;

        // You can add more properties here, e.g.:
        // public AudioClip SoundEffect;
        // public bool CanBeInterrupted;
        // public EmoteCategory Category;
    }

    // ===========================================================================================
    // 2. CharacterEmoteHandler (MonoBehaviour) - The Emote Executor/Handler
    //    Purpose: Handles the actual playing of an emote on a specific character.
    //    It receives an EmoteData object and translates it into character-specific actions
    //    like playing an animation, spawning VFX, etc.
    //    Why MonoBehaviour: Needs to be attached to a GameObject (the character) to run
    //    coroutines and interact with components like Animator.
    // ===========================================================================================
    /// <summary>
    /// Component responsible for playing emotes on a specific character.
    /// It manages the character's current emote state and interacts with the Animator.
    /// </summary>
    [RequireComponent(typeof(Animator))] // Assumes characters have an Animator for animations
    public class CharacterEmoteHandler : MonoBehaviour
    {
        [Tooltip("Reference to the character's Animator component.")]
        private Animator _animator;

        private EmoteData _currentEmote;
        private Coroutine _emoteCoroutine;

        // Events for other systems to subscribe to (e.g., UI, networking, other character systems)
        public event Action<EmoteData> OnEmoteStarted;
        public event Action<EmoteData> OnEmoteFinished;
        public event Action<EmoteData> OnEmoteInterrupted;

        public EmoteData CurrentEmote => _currentEmote;
        public bool IsEmoting => _currentEmote != null;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError($"CharacterEmoteHandler on {gameObject.name} requires an Animator component.", this);
            }
        }

        /// <summary>
        /// Attempts to play a given emote. If another emote is active, it will be interrupted.
        /// </summary>
        /// <param name="emoteToPlay">The EmoteData asset to play.</param>
        public void PlayEmote(EmoteData emoteToPlay)
        {
            if (emoteToPlay == null)
            {
                Debug.LogWarning($"Attempted to play a null emote on {gameObject.name}.", this);
                return;
            }

            // If an emote is already playing, stop it first
            if (IsEmoting)
            {
                StopCurrentEmote(true); // Interrupt current emote
            }

            _currentEmote = emoteToPlay;
            _emoteCoroutine = StartCoroutine(EmoteRoutine(emoteToPlay));

            // Raise event that an emote has started
            OnEmoteStarted?.Invoke(emoteToPlay);
            Debug.Log($"<color=cyan>{gameObject.name} started emoting: '{emoteToPlay.EmoteName}'</color>", this);
        }

        /// <summary>
        /// Stops the current active emote.
        /// </summary>
        /// <param name="interrupted">True if the emote was interrupted by another action, false if it finished naturally.</param>
        public void StopCurrentEmote(bool interrupted = false)
        {
            if (!IsEmoting) return;

            if (_emoteCoroutine != null)
            {
                StopCoroutine(_emoteCoroutine);
                _emoteCoroutine = null;
            }

            // Reset any animation triggers that might be stuck
            if (_animator != null && _currentEmote != null)
            {
                // Note: It's good practice to have exit transitions or reset logic in your Animator
                // For simplicity, we might just log here or trigger a generic "StopEmote" trigger if one existed.
                // _animator.ResetTrigger(_currentEmote.AnimationTriggerName); // This is often not needed if transitions are set up correctly
            }

            EmoteData finishedEmote = _currentEmote;
            _currentEmote = null;

            if (interrupted)
            {
                OnEmoteInterrupted?.Invoke(finishedEmote);
                Debug.Log($"<color=orange>{gameObject.name}'s emote '{finishedEmote.EmoteName}' was INTERRUPTED.</color>", this);
            }
            else
            {
                OnEmoteFinished?.Invoke(finishedEmote);
                Debug.Log($"<color=green>{gameObject.name}'s emote '{finishedEmote.EmoteName}' FINISHED.</color>", this);
            }
        }

        /// <summary>
        /// Coroutine that simulates playing the emote (e.g., triggering animation, spawning VFX).
        /// </summary>
        private IEnumerator EmoteRoutine(EmoteData emote)
        {
            // 1. Play Animation (if animator exists)
            if (_animator != null && !string.IsNullOrEmpty(emote.AnimationTriggerName))
            {
                _animator.SetTrigger(emote.AnimationTriggerName);
                Debug.Log($"Triggered animation '{emote.AnimationTriggerName}' for '{emote.EmoteName}'.", this);
            }

            // 2. Spawn Visual Effects (if prefab exists)
            if (emote.VFXPrefab != null)
            {
                GameObject vfxInstance = Instantiate(emote.VFXPrefab, transform.position, Quaternion.identity, transform);
                // Optional: Destroy VFX after duration or when particle system finishes
                Destroy(vfxInstance, emote.EmoteDuration);
                Debug.Log($"Spawned VFX '{emote.VFXPrefab.name}' for '{emote.EmoteName}'.", this);
            }

            // 3. Wait for emote duration
            yield return new WaitForSeconds(emote.EmoteDuration);

            // Emote finished naturally
            StopCurrentEmote(false);
        }

        // Example: How to integrate with Animator's OnAnimatorMove or OnAnimatorIK
        // These can be used for more advanced animation features related to emotes.
        // private void OnAnimatorMove()
        // {
        //     if (IsEmoting)
        //     {
        //         // Handle root motion, etc., specific to the emote animation
        //     }
        // }
    }

    // ===========================================================================================
    // 3. EmoteSystem (MonoBehaviour) - The Central Emote Registry/Manager
    //    Purpose: Acts as a central repository for all available EmoteData assets.
    //    Provides a way to retrieve EmoteData by name or ID.
    //    Why MonoBehaviour (or Singleton): Ensures it exists in the scene and is easily
    //    accessible by other components like CharacterEmoteHandler. Could also be a static class
    //    if not needing scene references, but MonoBehaviour is common for managers.
    // ===========================================================================================
    /// <summary>
    /// A central manager responsible for registering and providing access to all
    /// available EmoteData assets in the game.
    /// This could be a Singleton for easy global access.
    /// </summary>
    public class EmoteSystem : MonoBehaviour
    {
        public static EmoteSystem Instance { get; private set; }

        [Tooltip("List of all EmoteData assets available in the game.")]
        [SerializeField] private List<EmoteData> availableEmotes = new List<EmoteData>();

        private Dictionary<string, EmoteData> _emoteDictionary = new Dictionary<string, EmoteData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate EmoteSystem found, destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeEmoteDictionary();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Populates the internal dictionary for quick lookup of emotes by their name.
        /// </summary>
        private void InitializeEmoteDictionary()
        {
            _emoteDictionary.Clear();
            foreach (EmoteData emote in availableEmotes)
            {
                if (emote == null)
                {
                    Debug.LogWarning("An EmoteData asset in the 'Available Emotes' list is null. Please check your setup.", this);
                    continue;
                }
                if (_emoteDictionary.ContainsKey(emote.EmoteName))
                {
                    Debug.LogWarning($"Duplicate emote name '{emote.EmoteName}' found in EmoteSystem. Only the first one will be used.", this);
                    continue;
                }
                _emoteDictionary.Add(emote.EmoteName, emote);
            }
            Debug.Log($"EmoteSystem initialized with {_emoteDictionary.Count} unique emotes.", this);
        }

        /// <summary>
        /// Retrieves an EmoteData asset by its name.
        /// </summary>
        /// <param name="emoteName">The name of the emote to retrieve.</param>
        /// <returns>The EmoteData if found, otherwise null.</returns>
        public EmoteData GetEmoteData(string emoteName)
        {
            if (_emoteDictionary.TryGetValue(emoteName, out EmoteData emote))
            {
                return emote;
            }
            Debug.LogWarning($"Emote '{emoteName}' not found in EmoteSystem.", this);
            return null;
        }

        // Optional: Method to add emotes dynamically at runtime (e.g., from downloaded content)
        public void RegisterEmote(EmoteData newEmote)
        {
            if (newEmote == null)
            {
                Debug.LogWarning("Attempted to register a null EmoteData.", this);
                return;
            }
            if (!_emoteDictionary.ContainsKey(newEmote.EmoteName))
            {
                _emoteDictionary.Add(newEmote.EmoteName, newEmote);
                availableEmotes.Add(newEmote); // Also add to serialized list if you want it persistent/visible
                Debug.Log($"Emote '{newEmote.EmoteName}' registered dynamically.", this);
            }
            else
            {
                Debug.LogWarning($"Emote '{newEmote.EmoteName}' already registered.", this);
            }
        }
    }

    // ===========================================================================================
    // 4. EmoteSystemDemo (MonoBehaviour) - Example Usage
    //    Purpose: Demonstrates how a player controller or AI might use the EmoteSystem
    //    and CharacterEmoteHandler to trigger emotes.
    //    Why MonoBehaviour: To get input and interact with scene objects.
    // ===========================================================================================
    /// <summary>
    /// A simple demo script to show how a player or AI controller would interact
    /// with the EmoteSystem and CharacterEmoteHandler.
    /// </summary>
    public class EmoteSystemDemo : MonoBehaviour
    {
        private CharacterEmoteHandler _characterEmoteHandler;

        private void Awake()
        {
            _characterEmoteHandler = GetComponent<CharacterEmoteHandler>();
            if (_characterEmoteHandler == null)
            {
                Debug.LogError("EmoteSystemDemo requires a CharacterEmoteHandler component on the same GameObject.", this);
                enabled = false;
                return;
            }

            // Subscribe to events for demonstration
            _characterEmoteHandler.OnEmoteStarted += HandleEmoteStarted;
            _characterEmoteHandler.OnEmoteFinished += HandleEmoteFinished;
            _characterEmoteHandler.OnEmoteInterrupted += HandleEmoteInterrupted;
        }

        private void OnDestroy()
        {
            if (_characterEmoteHandler != null)
            {
                _characterEmoteHandler.OnEmoteStarted -= HandleEmoteStarted;
                _characterEmoteHandler.OnEmoteFinished -= HandleEmoteFinished;
                _characterEmoteHandler.OnEmoteInterrupted -= HandleEmoteInterrupted;
            }
        }

        private void Update()
        {
            // Ensure EmoteSystem is initialized before trying to use it
            if (EmoteSystem.Instance == null)
            {
                Debug.LogError("EmoteSystem.Instance is null. Make sure an EmoteSystem component exists in your scene.", this);
                return;
            }

            // Example input for triggering emotes
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TriggerEmote("Wave");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TriggerEmote("Laugh");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TriggerEmote("Sad");
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                _characterEmoteHandler.StopCurrentEmote(true); // Manually stop current emote
            }
        }

        /// <summary>
        /// Attempts to trigger an emote by its name.
        /// </summary>
        /// <param name="emoteName">The name of the emote to trigger.</param>
        private void TriggerEmote(string emoteName)
        {
            if (_characterEmoteHandler == null)
            {
                Debug.LogError("CharacterEmoteHandler is null. Cannot trigger emote.", this);
                return;
            }

            EmoteData emote = EmoteSystem.Instance.GetEmoteData(emoteName);
            if (emote != null)
            {
                _characterEmoteHandler.PlayEmote(emote);
            }
            else
            {
                Debug.LogWarning($"Could not find emote '{emoteName}'. Check if it's assigned to EmoteSystem.", this);
            }
        }

        // --- Event Handlers for Demonstration ---
        private void HandleEmoteStarted(EmoteData emote)
        {
            Debug.Log($"<color=blue>DEMO: {gameObject.name} detected emote '{emote.EmoteName}' has started!</color>");
            // E.g., Update UI, send network message, disable player movement
        }

        private void HandleEmoteFinished(EmoteData emote)
        {
            Debug.Log($"<color=blue>DEMO: {gameObject.name} detected emote '{emote.EmoteName}' has finished!</color>");
            // E.g., Re-enable player movement, update UI
        }

        private void HandleEmoteInterrupted(EmoteData emote)
        {
            Debug.Log($"<color=blue>DEMO: {gameObject.name} detected emote '{emote.EmoteName}' was interrupted!</color>");
            // E.g., Clean up specific UI elements related to the interrupted emote
        }
    }
}
```