// Unity Design Pattern Example: SignalNoiseSystem
// This script demonstrates the SignalNoiseSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete implementation of the 'SignalNoiseSystem' design pattern in Unity C#, focusing on a game event system. It demonstrates how to separate a core event (Signal) from its contextual data (Noise) and dispatch them through a central system.

## SignalNoiseSystem Design Pattern in Unity

The **SignalNoiseSystem** pattern is useful for creating a flexible and decoupled event system in games.

*   **Signal:** The core message or event that occurred (e.g., "Player Damaged," "Enemy Defeated," "Item Collected"). It tells you *what* happened.
*   **Noise:** The contextual data or metadata associated with the Signal (e.g., "amount of damage," "type of enemy," "location of death," "item ID," "who collected it"). It tells you *who, where, how much, or how* it happened.

By separating Signal and Noise, you can:
*   Keep your event definitions clean and focused.
*   Provide rich context to listeners without coupling them to specific data structures.
*   Easily extend the 'Noise' with new data without changing the core 'Signal' definitions.

---

### Setup Instructions:

1.  **Create a C# Script:** Create a new C# script in your Unity project, name it `SignalNoiseSystemExample.cs`.
2.  **Copy & Paste:** Copy the entire code block below and paste it into your `SignalNoiseSystemExample.cs` file, replacing its default content.
3.  **Create a Manager GameObject:**
    *   Create an empty GameObject in your scene (Right-click in Hierarchy -> Create Empty).
    *   Rename it to `_SignalNoiseSystem` (or anything you prefer).
    *   Attach the `SignalNoiseSystem` component from your `SignalNoiseSystemExample.cs` script to this GameObject.
    *   *(Optional but recommended):* Create another empty GameObject, name it `GameManager`, and attach the `GameManager` component from the script.
4.  **Create Player and Enemy/Collectible GameObjects:**
    *   Create a simple 3D object for your Player (e.g., a Cube).
    *   Rename it to `Player`.
    *   **Set its Tag to "Player"** (important for collision detection in examples).
    *   Add a `Rigidbody` component to the Player.
    *   Add a `Collider` (e.g., Box Collider, Capsule Collider) to the Player.
    *   Attach the `PlayerHealth` component from the script to your `Player` GameObject.
    *   Create another 3D object for an Enemy (e.g., a Sphere).
    *   Rename it to `Enemy`.
    *   Add a `Rigidbody` component to the Enemy.
    *   Add a `Collider` (e.g., Sphere Collider), mark it as **"Is Trigger"** in the Inspector.
    *   Attach the `Enemy` component from the script to your `Enemy` GameObject.
    *   Create another 3D object for a Collectible (e.g., a Cylinder).
    *   Rename it to `CollectibleCoin`.
    *   Add a `Collider` (e.g., Cylinder Collider), mark it as **"Is Trigger"**.
    *   Attach the `CollectibleItem` component from the script to your `CollectibleCoin` GameObject.
5.  **Run the Scene:**
    *   Move the `Player` into the `Enemy` to trigger a `PlayerDamaged` event.
    *   Use the `[ContextMenu]` options on the `Enemy` component in the Inspector to simulate damage. When its health drops to 0, it will dispatch `EnemyDefeated`.
    *   Move the `Player` into the `CollectibleCoin` to trigger an `ItemCollected` event.
    *   Observe the console logs to see the events being dispatched and received, including their contextual 'Noise'.
    *   You can also use the `[ContextMenu]` options on the `PlayerHealth` and `GameManager` components to test different events.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for .ToList()

namespace SignalNoisePattern
{
    // --- Core SignalNoiseSystem Design Pattern Components ---

    /// <summary>
    /// Represents the 'Signal' part of the SignalNoiseSystem.
    /// This enum defines different types of game events that can occur.
    /// </summary>
    /// <remarks>
    /// The Signal is the core message or event. It tells you WHAT happened.
    /// Add more specific event types here as your game grows.
    /// </remarks>
    public enum GameEventType
    {
        None, // Default or invalid event type
        PlayerDamaged,
        EnemyDefeated,
        ItemCollected,
        LevelCompleted,
        AbilityUsed,
        // ... extend with more specific event types as needed
    }

    /// <summary>
    /// Represents the 'Noise' part of the SignalNoiseSystem.
    /// This struct carries contextual data relevant to a specific GameEventType (Signal).
    /// </summary>
    /// <remarks>
    /// The Noise provides the details or context about the Signal. It tells you WHO, WHERE, HOW MUCH, etc.
    /// Using a struct for value-type semantics; it's passed by value and generally efficient for small data.
    /// Provides flexible fields and static factory methods for common data types.
    /// </remarks>
    public struct EventNoise
    {
        public float floatValue;
        public int intValue;
        public string stringValue;
        public Vector3 vector3Value;
        public GameObject gameObjectRef; // Reference to a GameObject, e.g., the source of damage, or the collector
        public object customData; // For any other complex or custom data types not covered by specific fields

        // --- Static Factory Methods for convenient EventNoise creation ---

        /// <summary>
        /// Creates an empty EventNoise instance. Useful when a signal has no relevant noise.
        /// </summary>
        public static EventNoise CreateEmpty() => new EventNoise();

        /// <summary>
        /// Creates EventNoise with a float value.
        /// </summary>
        public static EventNoise Create(float value) => new EventNoise { floatValue = value };

        /// <summary>
        /// Creates EventNoise with an integer value.
        /// </summary>
        public static EventNoise Create(int value) => new EventNoise { intValue = value };

        /// <summary>
        /// Creates EventNoise with a string value.
        /// </summary>
        public static EventNoise Create(string value) => new EventNoise { stringValue = value };

        /// <summary>
        /// Creates EventNoise with a Vector3 value.
        /// </summary>
        public static EventNoise Create(Vector3 value) => new EventNoise { vector3Value = value };

        /// <summary>
        /// Creates EventNoise with a GameObject reference.
        /// </summary>
        public static EventNoise Create(GameObject value) => new EventNoise { gameObjectRef = value };

        /// <summary>
        /// Creates EventNoise with a custom object.
        /// </summary>
        public static EventNoise Create(object data) => new EventNoise { customData = data };

        /// <summary>
        /// Creates EventNoise with a combination of common data types using named parameters.
        /// This method allows for specifying only the relevant noise data for an event.
        /// </summary>
        /// <example>
        /// `EventNoise.Create(intValue: 10, stringValue: "Coin", gameObjectRef: playerObject)`
        /// </example>
        public static EventNoise Create(
            float floatValue = 0f,
            int intValue = 0,
            string stringValue = null,
            Vector3 vector3Value = default,
            GameObject gameObjectRef = null,
            object customData = null)
        {
            return new EventNoise
            {
                floatValue = floatValue,
                intValue = intValue,
                stringValue = stringValue,
                vector3Value = vector3Value,
                gameObjectRef = gameObjectRef,
                customData = customData
            };
        }
    }

    /// <summary>
    /// Represents the combined 'Signal' and 'Noise' for a complete event message.
    /// </summary>
    /// <remarks>
    /// This struct bundles the specific GameEventType (the 'Signal') with its contextual data (the 'Noise').
    /// This is the primary data type passed through the SignalNoiseSystem.
    /// </remarks>
    public struct GameSignalNoise
    {
        public GameEventType Signal;
        public EventNoise Noise;

        public GameSignalNoise(GameEventType signal, EventNoise noise)
        {
            Signal = signal;
            Noise = noise;
        }
    }

    /// <summary>
    /// The central dispatcher for the SignalNoiseSystem pattern.
    /// This is a singleton MonoBehaviour responsible for managing event listeners and dispatching events.
    /// </summary>
    /// <remarks>
    /// This class acts as the hub where different parts of your game can
    /// subscribe to specific signals (events) and receive the accompanying noise (contextual data).
    /// It decouples the event producer from the event consumer, promoting modular design.
    /// </remarks>
    public class SignalNoiseSystem : MonoBehaviour
    {
        // Singleton instance for easy global access
        public static SignalNoiseSystem Instance { get; private set; }

        // Dictionary to store listeners for each GameEventType.
        // Key: GameEventType (Signal)
        // Value: List of Action<GameSignalNoise> (delegates to be invoked with the combined event)
        private Dictionary<GameEventType, List<Action<GameSignalNoise>>> _listeners;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the singleton and event listener dictionary.
        /// </summary>
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple SignalNoiseSystem instances found. Destroying duplicate.");
                Destroy(gameObject); // Destroy duplicate instances
            }
            else
            {
                Instance = this;
                _listeners = new Dictionary<GameEventType, List<Action<GameSignalNoise>>>();
                // Optional: Make this object persistent across scene loads.
                // Remove if you want a new SignalNoiseSystem for each scene.
                DontDestroyOnLoad(gameObject);
                Debug.Log("<color=cyan>SignalNoiseSystem initialized.</color>");
            }
        }

        /// <summary>
        /// Adds a listener (subscriber) to a specific GameEventType.
        /// When an event of `eventType` is dispatched, the `listener` action will be invoked
        /// with the `GameSignalNoise` data.
        /// </summary>
        /// <param name="eventType">The type of signal to listen for.</param>
        /// <param name="listener">The action (method) to call when the signal is dispatched.</param>
        public void AddListener(GameEventType eventType, Action<GameSignalNoise> listener)
        {
            if (!_listeners.ContainsKey(eventType))
            {
                _listeners[eventType] = new List<Action<GameSignalNoise>>();
            }
            if (!_listeners[eventType].Contains(listener)) // Prevent duplicate subscriptions
            {
                _listeners[eventType].Add(listener);
                // Debug.Log($"Listener added for {eventType}"); // Uncomment for detailed debug
            }
        }

        /// <summary>
        /// Removes a listener (subscriber) from a specific GameEventType.
        /// It's crucial to remove listeners when objects are destroyed or disabled
        /// to prevent NullReferenceExceptions and memory leaks.
        /// </summary>
        /// <param name="eventType">The type of signal the listener was subscribed to.</param>
        /// <param name="listener">The action (method) to remove.</param>
        public void RemoveListener(GameEventType eventType, Action<GameSignalNoise> listener)
        {
            if (_listeners.ContainsKey(eventType))
            {
                _listeners[eventType].Remove(listener);
                if (_listeners[eventType].Count == 0)
                {
                    _listeners.Remove(eventType); // Clean up empty lists
                }
                // Debug.Log($"Listener removed for {eventType}"); // Uncomment for detailed debug
            }
        }

        /// <summary>
        /// Dispatches a `GameSignalNoise` event to all registered listeners for its `Signal` type.
        /// </summary>
        /// <param name="signalNoise">The combined signal and noise event to dispatch.</param>
        public void Dispatch(GameSignalNoise signalNoise)
        {
            if (signalNoise.Signal == GameEventType.None)
            {
                Debug.LogWarning("Attempted to dispatch GameEventType.None. This event will not be processed.");
                return;
            }

            if (_listeners.TryGetValue(signalNoise.Signal, out var eventListeners))
            {
                // Iterate over a copy of the list to prevent issues if listeners modify
                // the subscription list (e.g., unsubscribe themselves) during iteration.
                // The .ToList() extension method creates a shallow copy.
                foreach (var listener in eventListeners.ToList())
                {
                    try
                    {
                        listener?.Invoke(signalNoise);
                    }
                    catch (Exception e)
                    {
                        // Log any exceptions that occur within a listener to prevent
                        // one bad listener from breaking the entire dispatch chain.
                        Debug.LogError($"<color=red>Error in event listener for {signalNoise.Signal}:</color> {e.Message}\n{e.StackTrace}");
                    }
                }
            }
            // else { Debug.Log($"No listeners for {signalNoise.Signal}"); } // Uncomment for debugging
        }

        /// <summary>
        /// Convenience overload for dispatching a signal with no specific noise.
        /// </summary>
        /// <param name="signal">The type of signal to dispatch.</param>
        public void Dispatch(GameEventType signal)
        {
            Dispatch(new GameSignalNoise(signal, EventNoise.CreateEmpty()));
        }

        /// <summary>
        /// Convenience overload for dispatching a signal with pre-packaged noise.
        /// </summary>
        /// <param name="signal">The type of signal to dispatch.</param>
        /// <param name="noise">The contextual data (noise) accompanying the signal.</param>
        public void Dispatch(GameEventType signal, EventNoise noise)
        {
            Dispatch(new GameSignalNoise(signal, noise));
        }

        /// <summary>
        /// Called when the GameObject is destroyed.
        /// Cleans up the singleton instance and clears listeners.
        /// </summary>
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                _listeners?.Clear(); // Clear all listeners to prevent memory leaks
                Debug.Log("<color=cyan>SignalNoiseSystem destroyed and listeners cleared.</color>");
            }
        }
    }

    // --- Example Usage Classes (Demonstrators) ---
    // These classes show how different parts of your game would interact with the SignalNoiseSystem.
    // Attach these scripts to different GameObjects in your scene as described in the setup.

    /// <summary>
    /// Example: Represents a Player character's health system.
    /// Subscribes to 'PlayerDamaged' signals and reacts to them.
    /// </summary>
    /// <remarks>
    /// Attach this to your Player GameObject (ensure it has the "Player" tag).
    /// </remarks>
    public class PlayerHealth : MonoBehaviour
    {
        public int currentHealth = 100;
        public int maxHealth = 100;

        void OnEnable()
        {
            // Subscribe to the PlayerDamaged signal when this component is enabled.
            // It's good practice to subscribe in OnEnable and unsubscribe in OnDisable/OnDestroy.
            if (SignalNoiseSystem.Instance != null)
            {
                SignalNoiseSystem.Instance.AddListener(GameEventType.PlayerDamaged, OnPlayerDamaged);
                Debug.Log($"<color=green>{gameObject.name} (PlayerHealth) subscribed to PlayerDamaged events.</color>");
            }
            else
            {
                Debug.LogError("PlayerHealth: SignalNoiseSystem.Instance is null. Is it in the scene and initialized?");
            }
        }

        void OnDisable()
        {
            // Unsubscribe when disabled to prevent trying to call methods on a destroyed object
            // and to avoid memory leaks (the dispatcher holding a reference to this object).
            if (SignalNoiseSystem.Instance != null)
            {
                SignalNoiseSystem.Instance.RemoveListener(GameEventType.PlayerDamaged, OnPlayerDamaged);
                Debug.Log($"<color=green>{gameObject.name} (PlayerHealth) unsubscribed from PlayerDamaged events.</color>");
            }
        }

        /// <summary>
        /// Callback method for when a PlayerDamaged signal is received.
        /// This method processes the 'noise' (damage amount, source) associated with the signal.
        /// </summary>
        /// <param name="signalNoise">The combined signal and its contextual noise.</param>
        private void OnPlayerDamaged(GameSignalNoise signalNoise)
        {
            // Even though we only subscribed to PlayerDamaged, it's good practice to check if
            // this listener might be used for multiple events or if logic relies on the specific signal.
            if (signalNoise.Signal == GameEventType.PlayerDamaged)
            {
                int damageAmount = signalNoise.Noise.intValue; // Assuming damage is passed as intValue
                GameObject damageSource = signalNoise.Noise.gameObjectRef; // Assuming damage source is passed as GameObjectRef

                currentHealth -= damageAmount;
                currentHealth = Mathf.Max(0, currentHealth); // Ensure health doesn't go below zero

                string sourceName = (damageSource != null) ? damageSource.name : "Unknown Source";
                Debug.Log($"<color=red>{gameObject.name} took {damageAmount} damage from {sourceName}.</color> Current Health: {currentHealth}");

                if (currentHealth <= 0)
                {
                    Debug.Log("<color=red>Player Defeated!</color>");
                    // Optionally, dispatch a "PlayerDefeated" or "LevelCompleted" signal with context.
                    // Here, we dispatch LevelCompleted to indicate Game Over.
                    SignalNoiseSystem.Instance.Dispatch(
                        GameEventType.LevelCompleted,
                        EventNoise.Create(stringValue: "Game Over", gameObjectRef: this.gameObject)
                    );
                    // You might want to disable player input, show game over screen, etc.
                    // gameObject.SetActive(false); // For example, disable the player object
                    // Destroy(gameObject); // Or destroy it
                }
            }
        }

        /// <summary>
        /// Allows testing damage from the Inspector via a context menu.
        /// </summary>
        [ContextMenu("Test: Take 20 Damage")]
        void TestTakeDamage()
        {
            if (SignalNoiseSystem.Instance != null)
            {
                // Simulate an event where the player takes damage from a generic "TestSource" object.
                // In a real game, this would be an actual enemy or hazard GameObject.
                SignalNoiseSystem.Instance.Dispatch(
                    GameEventType.PlayerDamaged,
                    EventNoise.Create(intValue: 20, gameObjectRef: new GameObject("TestSource"))
                );
            }
        }
    }

    /// <summary>
    /// Example: Represents an Enemy character that can damage the player and be defeated.
    /// Dispatches 'PlayerDamaged' and 'EnemyDefeated' signals.
    /// </summary>
    /// <remarks>
    /// Attach this to an Enemy GameObject.
    /// Ensure it has a Collider (e.g., Box Collider) marked as **"Is Trigger"**,
    /// and a Rigidbody (optional, but good for physics triggers).
    /// The Player GameObject should have a Collider and Rigidbody too, with tag "Player".
    /// </remarks>
    public class Enemy : MonoBehaviour
    {
        public int attackDamage = 20;
        public int health = 50;
        public string enemyName = "Goblin";
        public int scoreValue = 100;

        /// <summary>
        /// Called when another collider enters this trigger collider.
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"<color=magenta>{gameObject.name} ({enemyName}) collided with Player. Dispatching PlayerDamaged signal.</color>");
                // Dispatch the PlayerDamaged signal, including the damage amount and this enemy as the source (noise).
                SignalNoiseSystem.Instance.Dispatch(
                    GameEventType.PlayerDamaged,
                    EventNoise.Create(intValue: attackDamage, gameObjectRef: this.gameObject)
                );
            }
        }

        /// <summary>
        /// Simulates the enemy taking damage.
        /// </summary>
        /// <param name="amount">The amount of damage taken.</param>
        public void TakeDamage(int amount)
        {
            health -= amount;
            Debug.Log($"<color=magenta>{gameObject.name} ({enemyName}) took {amount} damage.</color> Health: {health}");

            if (health <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Handles the enemy's death. Dispatches an 'EnemyDefeated' signal.
        /// </summary>
        void Die()
        {
            Debug.Log($"<color=magenta>{gameObject.name} ({enemyName}) defeated!</color> Dispatching EnemyDefeated signal.");
            // Dispatch the EnemyDefeated signal with relevant noise:
            // - The enemy's name (string)
            // - Its position (Vector3)
            // - Score value (int)
            // - Reference to the enemy GameObject itself
            SignalNoiseSystem.Instance.Dispatch(
                GameEventType.EnemyDefeated,
                EventNoise.Create(
                    stringValue: enemyName,
                    vector3Value: transform.position,
                    intValue: scoreValue,
                    gameObjectRef: this.gameObject
                )
            );
            Destroy(gameObject); // Remove the enemy GameObject from the scene
        }

        /// <summary>
        /// Allows testing damage from the Inspector via a context menu.
        /// </summary>
        [ContextMenu("Test: Take 25 Damage")]
        void TestTakeDamage()
        {
            TakeDamage(25);
        }
    }

    /// <summary>
    /// Example: Represents a Collectible Item in the game.
    /// Dispatches 'ItemCollected' signals.
    /// </summary>
    /// <remarks>
    /// Attach this to a Collectible GameObject (e.g., a coin, a potion).
    /// Ensure it has a Collider (e.g., Sphere Collider) marked as **"Is Trigger"**.
    /// The Player GameObject should have a Collider and Rigidbody, with tag "Player".
    /// </remarks>
    public class CollectibleItem : MonoBehaviour
    {
        public string itemName = "Coin";
        public int quantity = 1;
        public bool destroyOnCollect = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"<color=cyan>{gameObject.name} ({itemName}) collected by Player. Dispatching ItemCollected signal.</color>");
                // Dispatch the ItemCollected signal with contextual noise:
                // - The item's name (string)
                // - The quantity collected (int)
                // - The collector (player GameObject) (gameObjectRef)
                SignalNoiseSystem.Instance.Dispatch(
                    GameEventType.ItemCollected,
                    EventNoise.Create(
                        stringValue: itemName,
                        intValue: quantity,
                        gameObjectRef: other.gameObject // The GameObject that collected it
                    )
                );

                if (destroyOnCollect)
                {
                    Destroy(gameObject); // Remove the item from the scene
                }
            }
        }
    }

    /// <summary>
    /// Example: Represents a central Game Manager that listens to various game events
    /// and performs game-wide actions (e.g., updating score, checking level completion).
    /// </summary>
    /// <remarks>
    /// Create an empty GameObject in your scene, name it "GameManager", and attach this script.
    /// </remarks>
    public class GameManager : MonoBehaviour
    {
        private int _score = 0;
        public int Score
        {
            get { return _score; }
            private set
            {
                _score = value;
                Debug.Log($"<color=yellow>GameManager: Score: {_score}</color>");
            }
        }

        void OnEnable()
        {
            // Subscribe to various signals that the GameManager needs to be aware of.
            if (SignalNoiseSystem.Instance != null)
            {
                SignalNoiseSystem.Instance.AddListener(GameEventType.EnemyDefeated, OnEnemyDefeated);
                SignalNoiseSystem.Instance.AddListener(GameEventType.ItemCollected, OnItemCollected);
                SignalNoiseSystem.Instance.AddListener(GameEventType.LevelCompleted, OnLevelCompleted);
                Debug.Log($"<color=yellow>{gameObject.name} (GameManager) subscribed to EnemyDefeated, ItemCollected, and LevelCompleted events.</color>");
            }
            else
            {
                Debug.LogError("GameManager: SignalNoiseSystem.Instance is null. Is it in the scene and initialized?");
            }
        }

        void OnDisable()
        {
            if (SignalNoiseSystem.Instance != null)
            {
                SignalNoiseSystem.Instance.RemoveListener(GameEventType.EnemyDefeated, OnEnemyDefeated);
                SignalNoiseSystem.Instance.RemoveListener(GameEventType.ItemCollected, OnItemCollected);
                SignalNoiseSystem.Instance.RemoveListener(GameEventType.LevelCompleted, OnLevelCompleted);
                Debug.Log($"<color=yellow>{gameObject.name} (GameManager) unsubscribed from events.</color>");
            }
        }

        /// <summary>
        /// Handles 'EnemyDefeated' signals. Updates score and logs details from the noise.
        /// </summary>
        private void OnEnemyDefeated(GameSignalNoise signalNoise)
        {
            string enemyName = signalNoise.Noise.stringValue;
            Vector3 position = signalNoise.Noise.vector3Value;
            int points = signalNoise.Noise.intValue;
            GameObject defeatedEnemy = signalNoise.Noise.gameObjectRef;

            Debug.Log($"<color=yellow>GameManager received EnemyDefeated: '{enemyName}' defeated at {position}! Awarded {points} points.</color>");
            Score += points; // Update the game score
            // Potentially spawn a pickup or particle effect at 'position' using 'defeatedEnemy' for context
        }

        /// <summary>
        /// Handles 'ItemCollected' signals. Updates inventory/score based on the item noise.
        /// </summary>
        private void OnItemCollected(GameSignalNoise signalNoise)
        {
            string itemName = signalNoise.Noise.stringValue;
            int quantity = signalNoise.Noise.intValue;
            GameObject collector = signalNoise.Noise.gameObjectRef;

            Debug.Log($"<color=yellow>GameManager received ItemCollected: {collector?.name ?? "An object"} collected {quantity}x {itemName}.</color>");
            if (itemName == "Coin")
            {
                Score += quantity; // Example: coins add to score
            }
            // Add logic here to add item to player's inventory, apply effects, etc.
        }

        /// <summary>
        /// Handles 'LevelCompleted' signals. Processes level transition or game over.
        /// </summary>
        private void OnLevelCompleted(GameSignalNoise signalNoise)
        {
            string message = signalNoise.Noise.stringValue;
            GameObject triggerSource = signalNoise.Noise.gameObjectRef;

            Debug.Log($"<color=yellow>GameManager received LevelCompleted: Message: '{message}' triggered by {triggerSource?.name ?? "unknown"}.</color>");
            // For example, load next scene, show end-game UI, reset game, etc.
            // UnityEngine.SceneManagement.SceneManager.LoadScene("GameOverScene");
        }

        // --- Editor Testing Methods ---

        [ContextMenu("Test: Simulate Enemy Defeat")]
        void TestEnemyDefeat()
        {
            if (SignalNoiseSystem.Instance != null)
            {
                SignalNoiseSystem.Instance.Dispatch(
                    GameEventType.EnemyDefeated,
                    EventNoise.Create(stringValue: "TestZombie", vector3Value: new Vector3(10, 0, 5), intValue: 50, gameObjectRef: null)
                );
            }
        }

        [ContextMenu("Test: Simulate Item Collection")]
        void TestItemCollection()
        {
            if (SignalNoiseSystem.Instance != null)
            {
                // Find a GameObject tagged "Player" to use as the collector reference for testing.
                GameObject playerObject = GameObject.FindWithTag("Player");
                SignalNoiseSystem.Instance.Dispatch(
                    GameEventType.ItemCollected,
                    EventNoise.Create(stringValue: "Potion", intValue: 1, gameObjectRef: playerObject)
                );
            }
        }

        [ContextMenu("Test: Simulate Level Completed (Win)")]
        void TestLevelCompletedWin()
        {
            if (SignalNoiseSystem.Instance != null)
            {
                SignalNoiseSystem.Instance.Dispatch(
                    GameEventType.LevelCompleted,
                    EventNoise.Create(stringValue: "Victory!", gameObjectRef: null)
                );
            }
        }
    }
}
```