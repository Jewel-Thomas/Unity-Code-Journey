// Unity Design Pattern Example: ShoutSystem
// This script demonstrates the ShoutSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ShoutSystem' design pattern (also known as Event Bus, Event Aggregator, or Publisher/Subscriber) is a powerful way to decouple different parts of your application. It allows components to communicate with each other without having direct references, making your code more modular, maintainable, and testable.

In Unity, this pattern is incredibly useful for:
*   **UI Updates:** A game system can "shout" an event (e.g., `OnPlayerHealthChanged`), and a UI component can "listen" to it and update the health bar, without the game system ever knowing about the UI.
*   **Game State Changes:** When a quest is completed, a boss is defeated, or a new level starts, the system can shout an event, and various other systems (e.g., audio, analytics, save system) can react.
*   **Player Actions:** When the player picks up an item, takes damage, or performs an action, the ShoutSystem can notify relevant listeners.

---

## 1. Project Setup in Unity

To get this example working:

1.  **Create a new Unity Project** (or open an existing one).
2.  **Create C# Scripts:** Create the five C# scripts listed below (`ShoutSystem.cs`, `PlayerHealth.cs`, `UIHealthDisplay.cs`, `DamageEffectHandler.cs`, `GameOverScreenController.cs`, `DamageDealerButton.cs`) in your `Assets/Scripts` folder.
3.  **UI Setup:**
    *   Go to `GameObject -> UI -> Canvas`.
    *   Right-click on the `Canvas` in the Hierarchy, then `UI -> Text - TextMeshPro`.
        *   If prompted, import the TMP Essentials.
    *   Rename this `GameObject` to `HealthText`.
    *   Adjust its Rect Transform to a suitable position (e.g., Top-Left, size 200x50).
    *   Duplicate `HealthText`, rename it to `DamageLogText`, and position it below `HealthText`. This will display recent damage.
    *   Duplicate `HealthText` again, rename it to `GameOverText`, position it in the center, set its text to "GAME OVER", increase font size significantly, and **disable it** initially.
    *   Right-click on the `Canvas` again, `UI -> Button - TextMeshPro`.
    *   Rename this `GameObject` to `DamageButton`. Position it somewhere visible (e.g., Bottom-Center). Change the button's text to "Take Damage".
4.  **Create an Empty GameObject:** In your Hierarchy, create an empty GameObject and name it `GameManager`.
5.  **Attach Scripts:**
    *   Attach `PlayerHealth.cs` to the `GameManager` GameObject.
    *   Attach `UIHealthDisplay.cs` to the `GameManager` GameObject.
        *   Drag the `HealthText` GameObject from the Hierarchy into the `Health Text` slot of the `UIHealthDisplay` script in the Inspector.
        *   Drag the `DamageLogText` GameObject from the Hierarchy into the `Damage Log Text` slot.
    *   Attach `DamageEffectHandler.cs` to the `GameManager` GameObject.
    *   Attach `GameOverScreenController.cs` to the `GameManager` GameObject.
        *   Drag the `GameOverText` GameObject from the Hierarchy into the `Game Over Panel` slot.
    *   Attach `DamageDealerButton.cs` to the `DamageButton` GameObject.
        *   Drag the `GameManager` GameObject (which has `PlayerHealth.cs`) into the `Player Health` slot of the `DamageDealerButton` script.
6.  **Run the Scene!** Observe the console and the UI as you click the "Take Damage" button.

---

## 2. The ShoutSystem Core Script (`ShoutSystem.cs`)

This is the central hub. It's a `static` class, meaning you don't need to create an instance of it. All events and methods are accessed directly via `ShoutSystem.EventName`.

```csharp
using System;
using UnityEngine;

/// <summary>
/// The core 'ShoutSystem' (or Event Bus / Event Aggregator) for the game.
/// This static class provides a centralized hub for events, allowing different
/// parts of the application to communicate without having direct references
/// to each other. This promotes decoupling, modularity, and easier testing.
/// </summary>
/// <remarks>
/// How it works:
/// 1.  **Define Events:** Static `event Action` (or `Action<T>`) delegates are declared
///     for each type of event. These are like "topics" that listeners can subscribe to.
/// 2.  **Shout Methods (Publishers):** Static methods (e.g., `ShoutPlayerHealthChanged`)
///     are provided to invoke these events. When a part of the game wants to announce
///     something, it calls one of these "Shout" methods.
/// 3.  **Subscribe/Unsubscribe (Listeners):** Other scripts can attach (subscribe)
///     their methods to these events using `+=` and detach (unsubscribe) using `-=`.
///     It's crucial to unsubscribe when an object is destroyed or disabled to prevent
///     memory leaks and null reference exceptions.
/// </remarks>
public static class ShoutSystem
{
    // ===============================================
    //               Player Health Events
    // ===============================================

    /// <summary>
    /// Event fired when the player's health status changes.
    /// Parameters: currentHealth (int), maxHealth (int)
    /// </summary>
    public static event Action<int, int> OnPlayerHealthChanged;
    /// <summary>
    /// Shouts that the player's health has changed.
    /// </summary>
    /// <param name="currentHealth">The player's current health.</param>
    /// <param name="maxHealth">The player's maximum health.</param>
    public static void ShoutPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        // The '?' operator ensures the event is only invoked if there are subscribers.
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"<color=cyan>[ShoutSystem]</color> Player Health Changed: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Event fired when the player takes damage.
    /// Parameters: damageAmount (int)
    /// </summary>
    public static event Action<int> OnPlayerTookDamage;
    /// <summary>
    /// Shouts that the player took a specific amount of damage.
    /// </summary>
    /// <param name="damageAmount">The amount of damage taken.</param>
    public static void ShoutPlayerTookDamage(int damageAmount)
    {
        OnPlayerTookDamage?.Invoke(damageAmount);
        Debug.Log($"<color=cyan>[ShoutSystem]</color> Player Took Damage: {damageAmount}");
    }

    /// <summary>
    /// Event fired when the player's health drops to or below zero.
    /// No parameters.
    /// </summary>
    public static event Action OnPlayerDied;
    /// <summary>
    /// Shouts that the player has died.
    /// </summary>
    public static void ShoutPlayerDied()
    {
        OnPlayerDied?.Invoke();
        Debug.Log("<color=cyan>[ShoutSystem]</color> Player Died!");
    }

    // ===============================================
    //               Example Other Events
    // ===============================================

    /// <summary>
    /// Example: Event fired when a game score is updated.
    /// Parameters: newScore (int)
    /// </summary>
    public static event Action<int> OnScoreUpdated;
    /// <summary>
    /// Example: Shouts that the game score has been updated.
    /// </summary>
    /// <param name="newScore">The new total score.</param>
    public static void ShoutScoreUpdated(int newScore)
    {
        OnScoreUpdated?.Invoke(newScore);
        Debug.Log($"<color=cyan>[ShoutSystem]</color> Score Updated: {newScore}");
    }

    // You can add many more event types here as needed for your game!
    // For example:
    // public static event Action<string, Vector3> OnItemPickedUp;
    // public static void ShoutItemPickedUp(string itemName, Vector3 position) { OnItemPickedUp?.Invoke(itemName, position); }

    // public static event Action<GameObject, float> OnEnemySpotted;
    // public static void ShoutEnemySpotted(GameObject enemy, float distance) { OnEnemySpotted?.Invoke(enemy, distance); }
}
```

---

## 3. Player Health Script (`PlayerHealth.cs`)

This script manages the player's health state and is a **publisher (shouter)** for health-related events.

```csharp
using UnityEngine;

/// <summary>
/// Manages the player's health and acts as a 'Shouter' for health-related events.
/// It doesn't know who is listening, it just announces its state changes via the ShoutSystem.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    private bool isDead = false;

    // Public property to allow other scripts to query health if needed,
    // though the ShoutSystem is preferred for reactive updates.
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        // Shout the initial health state so any UI elements can update immediately.
        ShoutSystem.ShoutPlayerHealthChanged(currentHealth, maxHealth);
    }

    /// <summary>
    /// Applies damage to the player.
    /// This method is called by a direct reference (e.g., from a 'DamageDealer' script),
    /// which then triggers the ShoutSystem events.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0); // Ensure health doesn't go below 0

        // 1. Shout that the player took damage (e.g., for sound effects, visual cues)
        ShoutSystem.ShoutPlayerTookDamage(amount);

        // 2. Shout that the player's overall health state has changed (e.g., for UI updates)
        ShoutSystem.ShoutPlayerHealthChanged(currentHealth, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the player for a given amount.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Ensure health doesn't exceed max

        // Shout that the player's overall health state has changed
        ShoutSystem.ShoutPlayerHealthChanged(currentHealth, maxHealth);
    }

    /// <summary>
    /// Handles the player's death sequence.
    /// </summary>
    private void Die()
    {
        isDead = true;
        // Shout that the player has died (e.g., for game over screen, respawn logic)
        ShoutSystem.ShoutPlayerDied();
        Debug.Log("Player has died!");
        // Additional death logic here (e.g., disable controls, play death animation, etc.)
    }

    // Example Usage:
    // To damage the player from another script:
    //
    // public class EnemyAttack : MonoBehaviour
    // {
    //     [SerializeField] private PlayerHealth playerHealth; // Assign in Inspector
    //     [SerializeField] private int attackDamage = 10;
    //
    //     void AttackPlayer()
    //     {
    //         if (playerHealth != null)
    //         {
    //             playerHealth.TakeDamage(attackDamage);
    //         }
    //     }
    // }
}
```

---

## 4. UI Health Display Script (`UIHealthDisplay.cs`)

This script is a **listener (subscriber)**. It subscribes to health change events and updates the UI accordingly.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshPro

/// <summary>
/// A UI component that 'listens' to player health changes via the ShoutSystem
/// and updates a TextMeshProUGUI element.
/// It has no direct reference to the PlayerHealth script, promoting decoupling.
/// </summary>
public class UIHealthDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageLogText;

    private const int MAX_LOG_MESSAGES = 5;
    private System.Collections.Generic.Queue<string> damageLog = new System.Collections.Generic.Queue<string>();

    // It's crucial to subscribe in OnEnable and unsubscribe in OnDisable
    // to prevent memory leaks and ensure the listener is active only when enabled.
    private void OnEnable()
    {
        // Subscribe to the player health changed event
        ShoutSystem.OnPlayerHealthChanged += UpdateHealthDisplay;
        // Subscribe to the player took damage event
        ShoutSystem.OnPlayerTookDamage += LogDamageTaken;
        Debug.Log("<color=green>[UIHealthDisplay]</color> Subscribed to ShoutSystem events.");
    }

    private void OnDisable()
    {
        // Unsubscribe from the player health changed event
        ShoutSystem.OnPlayerHealthChanged -= UpdateHealthDisplay;
        // Unsubscribe from the player took damage event
        ShoutSystem.OnPlayerTookDamage -= LogDamageTaken;
        Debug.Log("<color=red>[UIHealthDisplay]</color> Unsubscribed from ShoutSystem events.");
    }

    /// <summary>
    /// Callback method invoked when OnPlayerHealthChanged event is shouted.
    /// Updates the health text.
    /// </summary>
    /// <param name="currentHealth">The current health value.</param>
    /// <param name="maxHealth">The maximum health value.</param>
    private void UpdateHealthDisplay(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth}/{maxHealth}";
            Debug.Log($"<color=green>[UIHealthDisplay]</color> Updating health display to: {currentHealth}/{maxHealth}");
        }
    }

    /// <summary>
    /// Callback method invoked when OnPlayerTookDamage event is shouted.
    /// Logs recent damage.
    /// </summary>
    /// <param name="damageAmount">The amount of damage taken.</param>
    private void LogDamageTaken(int damageAmount)
    {
        if (damageLogText != null)
        {
            if (damageLog.Count >= MAX_LOG_MESSAGES)
            {
                damageLog.Dequeue(); // Remove oldest message
            }
            damageLog.Enqueue($"Took {damageAmount} damage!");

            damageLogText.text = "";
            foreach (string msg in damageLog)
            {
                damageLogText.text += msg + "\n";
            }
            Debug.Log($"<color=green>[UIHealthDisplay]</color> Logging damage: {damageAmount}");
        }
    }

    // Example Usage:
    // To subscribe to an event in another script:
    //
    // public class ScoreDisplay : MonoBehaviour
    // {
    //     [SerializeField] private TextMeshProUGUI scoreText;
    //
    //     void OnEnable()
    //     {
    //         ShoutSystem.OnScoreUpdated += UpdateScoreText;
    //     }
    //
    //     void OnDisable()
    //     {
    //         ShoutSystem.OnScoreUpdated -= UpdateScoreText;
    //     }
    //
    //     void UpdateScoreText(int newScore)
    //     {
    //         if (scoreText != null)
    //         {
    //             scoreText.text = $"Score: {newScore}";
    //         }
    //     }
    // }
}
```

---

## 5. Damage Effect Handler (`DamageEffectHandler.cs`)

Another **listener** script, demonstrating how different systems can react to the same event independently.

```csharp
using UnityEngine;

/// <summary>
/// Handles visual and audio effects when the player takes damage.
/// This script 'listens' to the OnPlayerTookDamage event via the ShoutSystem.
/// It's completely decoupled from PlayerHealth or UI, focusing solely on effects.
/// </summary>
public class DamageEffectHandler : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private GameObject hitEffectPrefab; // e.g., a blood splatter, particle effect
    [SerializeField] private float effectDuration = 0.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        // Subscribe to the player took damage event
        ShoutSystem.OnPlayerTookDamage += HandleDamageEffects;
        Debug.Log("<color=green>[DamageEffectHandler]</color> Subscribed to ShoutSystem events.");
    }

    private void OnDisable()
    {
        // Unsubscribe from the player took damage event
        ShoutSystem.OnPlayerTookDamage -= HandleDamageEffects;
        Debug.Log("<color=red>[DamageEffectHandler]</color> Unsubscribed from ShoutSystem events.");
    }

    /// <summary>
    /// Callback method invoked when OnPlayerTookDamage event is shouted.
    /// Plays sound and instantiates a visual effect.
    /// </summary>
    /// <param name="damageAmount">The amount of damage taken (can be used to scale effects).</param>
    private void HandleDamageEffects(int damageAmount)
    {
        Debug.Log($"<color=green>[DamageEffectHandler]</color> Handling damage effect for {damageAmount} damage.");

        // Play hit sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Instantiate visual effect (e.g., blood splatter, hit spark)
        if (hitEffectPrefab != null)
        {
            // For a simple example, we instantiate at the GameManager's position.
            // In a real game, you might pass the hit position as an event parameter.
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }

        // Additional logic here, e.g., screen shake, camera effects
    }
}
```

---

## 6. Game Over Screen Controller (`GameOverScreenController.cs`)

Another **listener** script, reacting to the player death event.

```csharp
using UnityEngine;

/// <summary>
/// Controls the Game Over screen activation.
/// It 'listens' to the OnPlayerDied event via the ShoutSystem.
/// This script only cares about player death, regardless of how it happened.
/// </summary>
public class GameOverScreenController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel; // Assign your Game Over UI Panel here

    private void Awake()
    {
        // Ensure the game over panel is initially inactive
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the player died event
        ShoutSystem.OnPlayerDied += ActivateGameOverScreen;
        Debug.Log("<color=green>[GameOverScreenController]</color> Subscribed to ShoutSystem events.");
    }

    private void OnDisable()
    {
        // Unsubscribe from the player died event
        ShoutSystem.OnPlayerDied -= ActivateGameOverScreen;
        Debug.Log("<color=red>[GameOverScreenController]</color> Unsubscribed from ShoutSystem events.");
    }

    /// <summary>
    /// Callback method invoked when OnPlayerDied event is shouted.
    /// Activates the game over UI panel.
    /// </summary>
    private void ActivateGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("<color=green>[GameOverScreenController]</color> Game Over! Activating panel.");
            // Optionally pause the game or show other end-game options
            Time.timeScale = 0f; // Pause game
        }
    }
}
```

---

## 7. Damage Dealer Button (`DamageDealerButton.cs`)

This is a simple script to simulate an external source of damage (e.g., an enemy attack, a trap). It directly calls a method on `PlayerHealth`, which then initiates the chain of shouts.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Button component

/// <summary>
/// A simple script that simulates an external damage source by calling
/// the PlayerHealth's TakeDamage method when a UI button is clicked.
/// This demonstrates how an action (button press) can trigger the ShoutSystem flow.
/// </summary>
[RequireComponent(typeof(Button))]
public class DamageDealerButton : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private PlayerHealth playerHealth; // Drag the GameObject with PlayerHealth here

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("DamageDealerButton requires a Button component on the same GameObject.", this);
            enabled = false;
        }

        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth reference is not set on DamageDealerButton.", this);
            // Attempt to find it if not set, for convenience in simple scenes.
            // In a larger project, explicit assignment is better.
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogError("No PlayerHealth found in the scene for DamageDealerButton.", this);
                enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(DealDamageToPlayer);
            Debug.Log("<color=magenta>[DamageDealerButton]</color> Button listener added.");
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(DealDamageToPlayer);
            Debug.Log("<color=magenta>[DamageDealerButton]</color> Button listener removed.");
        }
    }

    /// <summary>
    /// This method is called when the UI button is clicked.
    /// It directly interacts with the PlayerHealth script, which then
    /// uses the ShoutSystem to notify other parts of the game.
    /// </summary>
    private void DealDamageToPlayer()
    {
        if (playerHealth != null && playerHealth.CurrentHealth > 0)
        {
            playerHealth.TakeDamage(damageAmount);
            Debug.Log($"<color=magenta>[DamageDealerButton]</color> Instructing PlayerHealth to take {damageAmount} damage.");
        }
        else if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            Debug.Log("<color=magenta>[DamageDealerButton]</color> Player is already dead, no more damage can be dealt.");
        }
    }
}
```

---

## Explanation and Benefits of the ShoutSystem

1.  **Decoupling:**
    *   `PlayerHealth` doesn't know about `UIHealthDisplay`, `DamageEffectHandler`, or `GameOverScreenController`. It just shouts its state changes.
    *   `UIHealthDisplay` doesn't know about `PlayerHealth`. It just listens for `OnPlayerHealthChanged`.
    *   `DamageDealerButton` directly interacts with `PlayerHealth` to *initiate* the process, but `PlayerHealth` then takes over and distributes the information via the `ShoutSystem`.

2.  **Modularity and Maintainability:**
    *   You can add or remove listeners (e.g., a new "Analytics Tracker" for damage taken) without modifying `PlayerHealth`.
    *   You can change the UI (e.g., from Text to a Health Bar Slider) without touching the `PlayerHealth` logic.
    *   Each component focuses on its own responsibility.

3.  **Testability:**
    *   You could write unit tests for `PlayerHealth` that only check if `ShoutSystem.ShoutPlayerHealthChanged` was called with the correct parameters, without needing to mock UI or effect systems.
    *   You can easily simulate events being shouted for testing specific listener behaviors.

4.  **Extensibility:**
    *   Adding new event types is as simple as declaring a new `static event Action` and a corresponding `Shout` method in `ShoutSystem.cs`.
    *   New features can easily hook into existing events.

5.  **Global Access:**
    *   Being a `static` class, the `ShoutSystem` is globally accessible from anywhere in your code, providing a convenient central point for event management without singletons or `FindObjectOfType`.

This example provides a robust foundation for implementing a highly decoupled and scalable event system in your Unity projects using the ShoutSystem pattern.