// Unity Design Pattern Example: DamageSystem
// This script demonstrates the DamageSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The DamageSystem pattern centralizes health management, damage application, and death logic into a single, reusable component. This promotes modularity, maintainability, and extensibility in your Unity projects.

**Key Benefits of the DamageSystem Pattern:**

1.  **Single Responsibility:** All health-related logic (taking damage, healing, dying, invulnerability) is contained in one place.
2.  **Reusability:** The `HealthSystem` component can be attached to any game object that needs health (player, enemies, destructible objects).
3.  **Loose Coupling:** Attacking entities don't need to know *how* damage is processed; they simply call `TakeDamage()`. Health-related UI or other game systems subscribe to events without needing direct references to the `HealthSystem`'s internal state.
4.  **Extensibility:** Easily add features like damage types, status effects, critical hits, or different death animations by modifying or extending this single component, or by adding subscribers to its events.
5.  **Maintainability:** Changes to the damage model or health rules only need to be made in one script.

---

## Complete Unity C# Script: `HealthSystem.cs`

This script provides a robust and event-driven `HealthSystem` that can be attached to any game object needing health.

```csharp
using UnityEngine;
using System; // Required for Action delegate
using System.Collections; // Required for Coroutines

/// <summary>
/// Implements the core logic for the DamageSystem design pattern.
/// This component manages an entity's health, handles taking damage, healing,
/// and death. It uses events to communicate state changes to other systems
/// (e.g., UI, visual effects, audio).
/// </summary>
[DisallowMultipleComponent] // Ensures only one HealthSystem can be on a GameObject
public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("The maximum amount of health this entity can have.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("The current health of this entity.")]
    private float currentHealth;

    [Tooltip("Is this entity currently dead?")]
    private bool isDead = false;

    [Header("Invulnerability Settings")]
    [Tooltip("Can this entity become temporarily invulnerable after taking damage?")]
    [SerializeField] private bool canBeInvulnerable = true;

    [Tooltip("Duration in seconds for which the entity remains invulnerable after taking damage.")]
    [SerializeField] private float invulnerabilityDuration = 1f;

    [Tooltip("Is this entity currently invulnerable?")]
    private bool isInvulnerable = false;

    // --- Public Properties ---
    // These properties provide read-only access to important health states from other scripts.
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    // --- Events ---
    // Events are a core part of the DamageSystem pattern, enabling loose coupling.
    // Other scripts can subscribe to these events to react to health changes
    // without needing direct references to the HealthSystem's internal state.

    /// <summary>
    /// Event fired whenever the current health changes (damaged or healed).
    /// Parameters: (float currentHealth, float maxHealth)
    /// Useful for updating UI health bars, etc.
    /// </summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>
    /// Event fired when the entity takes damage.
    /// Parameter: (float damageAmount)
    /// Useful for triggering hit reactions, sound effects, particle effects.
    /// </summary>
    public event Action<float> OnDamaged;

    /// <summary>
    /// Event fired when the entity is healed.
    /// Parameter: (float healAmount)
    /// Useful for triggering healing effects or sounds.
    /// </summary>
    public event Action<float> OnHealed;

    /// <summary>
    /// Event fired when the entity's health drops to 0 or below, causing death.
    /// Useful for triggering death animations, game over screens, dropping loot, etc.
    /// </summary>
    public event Action OnDied;

    /// <summary>
    /// Event fired when the entity is revived or reset.
    /// Useful for re-enabling functionality, resetting visuals.
    /// </summary>
    public event Action OnRespawned;

    /// <summary>
    /// Initializes the health system when the component awakes.
    /// </summary>
    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        // Invoke the health changed event to initialize UI if listeners are already present.
        OnHealthChanged?.Invoke(currentHealth, maxHealth); 
    }

    /// <summary>
    /// Applies damage to the entity.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to apply.</param>
    public void TakeDamage(float damageAmount)
    {
        // Guard clauses: Do nothing if already dead or invulnerable
        if (isDead || isInvulnerable)
        {
            return;
        }

        // Ensure damage is not negative
        damageAmount = Mathf.Max(0, damageAmount);

        // Reduce current health
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth); // Clamp health to a minimum of 0

        // Notify subscribers that damage was taken
        OnDamaged?.Invoke(damageAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
        else if (canBeInvulnerable && !isInvulnerable)
        {
            // Start invulnerability period after taking damage
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }

    /// <summary>
    /// Heals the entity, restoring health.
    /// </summary>
    /// <param name="healAmount">The amount of health to restore.</param>
    public void Heal(float healAmount)
    {
        // Guard clause: Cannot heal if dead
        if (isDead)
        {
            return;
        }

        // Ensure heal amount is not negative
        healAmount = Mathf.Max(0, healAmount);

        // Increase current health
        currentHealth += healAmount;
        currentHealth = Mathf.Min(maxHealth, currentHealth); // Clamp health to maxHealth

        // Notify subscribers that healing occurred
        OnHealed?.Invoke(healAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Handles the death logic for the entity.
    /// </summary>
    private void Die()
    {
        isDead = true;
        isInvulnerable = false; // Ensure invulnerability is off when dead
        // Stop any active invulnerability coroutine
        StopAllCoroutines(); 

        // Notify subscribers that the entity has died
        OnDied?.Invoke();

        // Optional: Disable the GameObject or its renderer/collider
        // For example, if this is an enemy, you might want to disable its AI and physics.
        // gameObject.SetActive(false); 
        // GetComponent<Collider>()?.enabled = false;
        // GetComponent<Renderer>()?.enabled = false;
        Debug.Log($"{gameObject.name} has died!");
    }

    /// <summary>
    /// Resets the entity's health and state, effectively respawning it.
    /// </summary>
    public void Respawn()
    {
        if (!isDead) return; // Only respawn if dead

        isDead = false;
        currentHealth = maxHealth;
        isInvulnerable = false; // Reset invulnerability
        StopAllCoroutines(); // Clear any lingering coroutines

        // Re-enable GameObject/components if they were disabled on death
        // gameObject.SetActive(true);
        // GetComponent<Collider>()?.enabled = true;
        // GetComponent<Renderer>()?.enabled = true;

        // Notify subscribers of respawn
        OnRespawned?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth); // Update UI
        Debug.Log($"{gameObject.name} has respawned!");
    }

    /// <summary>
    /// Coroutine to manage the invulnerability period after taking damage.
    /// </summary>
    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        Debug.Log($"{gameObject.name} is now invulnerable for {invulnerabilityDuration} seconds.");
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
        Debug.Log($"{gameObject.name} is no longer invulnerable.");
    }
}
```

---

## How to Use the `HealthSystem` (Example Usage)

Here are example scripts demonstrating how other parts of your game would interact with the `HealthSystem`.

### 1. Attacking Script (e.g., `Attacker.cs` for a player or enemy)

This script would be attached to an entity that deals damage (e.g., a weapon, a projectile, an enemy AI).

```csharp
using UnityEngine;

public class Attacker : MonoBehaviour
{
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private string targetTag = "Player"; // Or "Enemy"

    // Example: Deal damage on collision (e.g., a projectile hitting something)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            // Try to get the HealthSystem component from the collided object
            HealthSystem targetHealth = collision.gameObject.GetComponent<HealthSystem>();

            if (targetHealth != null)
            {
                // Call the TakeDamage method on the target's HealthSystem
                targetHealth.TakeDamage(damageAmount);
                Debug.Log($"{gameObject.name} dealt {damageAmount} damage to {collision.gameObject.name}. " +
                          $"{collision.gameObject.name} health: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}");

                // Optional: Destroy self if this is a projectile
                // Destroy(gameObject); 
            }
        }
    }

    // Example: Deal damage to a target's HealthSystem directly (e.g., a melee attack)
    public void AttackTarget(GameObject target)
    {
        HealthSystem targetHealth = target.GetComponent<HealthSystem>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damageAmount);
            Debug.Log($"{gameObject.name} directly attacked {target.name} for {damageAmount} damage. " +
                      $"{target.name} health: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}");
        }
    }
}
```

### 2. Health Bar UI Script (e.g., `HealthBarUI.cs`)

This script would be attached to a UI element (like a Slider or Image) to visualize the health.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Slider or Image

public class HealthBarUI : MonoBehaviour
{
    [Tooltip("Reference to the HealthSystem component to display.")]
    [SerializeField] private HealthSystem targetHealthSystem;

    [Tooltip("Reference to the UI Slider component for the health bar.")]
    [SerializeField] private Slider healthSlider;

    // Start is called before the first frame update
    void Start()
    {
        if (targetHealthSystem == null)
        {
            Debug.LogError("HealthBarUI: targetHealthSystem is not assigned!", this);
            return;
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
            if (healthSlider == null)
            {
                Debug.LogError("HealthBarUI: No Slider component found!", this);
                return;
            }
        }

        // Subscribe to the OnHealthChanged event
        targetHealthSystem.OnHealthChanged += UpdateHealthUI;
        targetHealthSystem.OnDied += OnTargetDied;
        targetHealthSystem.OnRespawned += OnTargetRespawned;

        // Initialize the UI with current health
        UpdateHealthUI(targetHealthSystem.CurrentHealth, targetHealthSystem.MaxHealth);
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks and errors if targetHealthSystem is destroyed
        if (targetHealthSystem != null)
        {
            targetHealthSystem.OnHealthChanged -= UpdateHealthUI;
            targetHealthSystem.OnDied -= OnTargetDied;
            targetHealthSystem.OnRespawned -= OnTargetRespawned;
        }
    }

    /// <summary>
    /// Callback function for the OnHealthChanged event.
    /// Updates the UI slider value.
    /// </summary>
    /// <param name="currentHealth">The new current health.</param>
    /// <param name="maxHealth">The maximum health.</param>
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        // You could also update text displaying health values here
        // For example: GetComponentInChildren<Text>().text = $"{currentHealth}/{maxHealth}";
    }

    /// <summary>
    /// Callback function for the OnDied event.
    /// Hides the health bar when the target dies.
    /// </summary>
    private void OnTargetDied()
    {
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(false);
            Debug.Log($"Health bar for {targetHealthSystem.gameObject.name} hidden because it died.");
        }
    }

    /// <summary>
    /// Callback function for the OnRespawned event.
    /// Shows the health bar when the target respawns.
    /// </summary>
    private void OnTargetRespawned()
    {
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(true);
            Debug.Log($"Health bar for {targetHealthSystem.gameObject.name} shown because it respawned.");
        }
    }
}
```

### 3. Visual/Audio Feedback Script (e.g., `DamageFeedback.cs`)

This script reacts to damage/heal events to play visual effects or sounds.

```csharp
using UnityEngine;

public class DamageFeedback : MonoBehaviour
{
    [Tooltip("Reference to the HealthSystem component to listen to.")]
    [SerializeField] private HealthSystem targetHealthSystem;

    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffectPrefab; // e.g., particle system
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    private Material originalMaterial;
    private Renderer targetRenderer;

    [Header("Audio Effects")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip deathSound;
    private AudioSource audioSource;

    void Awake()
    {
        if (targetHealthSystem == null)
        {
            targetHealthSystem = GetComponent<HealthSystem>();
            if (targetHealthSystem == null)
            {
                Debug.LogError("DamageFeedback: No HealthSystem found on this GameObject or assigned!", this);
                enabled = false; // Disable this script if no HealthSystem is found
                return;
            }
        }

        targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.material; // Store original material
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        // Subscribe to relevant HealthSystem events
        targetHealthSystem.OnDamaged += OnDamagedEffect;
        targetHealthSystem.OnHealed += OnHealedEffect;
        targetHealthSystem.OnDied += OnDiedEffect;
    }

    void OnDisable()
    {
        // Unsubscribe from events to prevent errors
        if (targetHealthSystem != null)
        {
            targetHealthSystem.OnDamaged -= OnDamagedEffect;
            targetHealthSystem.OnHealed -= OnHealedEffect;
            targetHealthSystem.OnDied -= OnDiedEffect;
        }
    }

    /// <summary>
    /// Called when the target HealthSystem takes damage.
    /// </summary>
    /// <param name="damageAmount">The amount of damage taken.</param>
    private void OnDamagedEffect(float damageAmount)
    {
        Debug.Log($"{targetHealthSystem.gameObject.name} took {damageAmount} damage. Playing hit effects.");

        // Play hit sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Instantiate hit effect particles
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Visual flash effect
        if (targetRenderer != null && flashColor != null)
        {
            StopAllCoroutines(); // Stop any ongoing flash
            StartCoroutine(FlashEffect());
        }
    }

    /// <summary>
    /// Called when the target HealthSystem is healed.
    /// </summary>
    /// <param name="healAmount">The amount of health healed.</param>
    private void OnHealedEffect(float healAmount)
    {
        Debug.Log($"{targetHealthSystem.gameObject.name} healed for {healAmount}. Playing heal effects.");
        if (healSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healSound);
        }
        // Could add a green flash or healing particle effect here
    }

    /// <summary>
    /// Called when the target HealthSystem dies.
    /// </summary>
    private void OnDiedEffect()
    {
        Debug.Log($"{targetHealthSystem.gameObject.name} died. Playing death effects.");
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        // Could trigger a death explosion or dismemberment effect
    }

    /// <summary>
    /// Coroutine for a quick visual flash effect.
    /// </summary>
    private System.Collections.IEnumerator FlashEffect()
    {
        Material flashMaterial = new Material(Shader.Find("Standard")); // Or any shader that supports color
        if (flashMaterial != null)
        {
            flashMaterial.color = flashColor;
            targetRenderer.material = flashMaterial;

            yield return new WaitForSeconds(flashDuration);

            // Restore original material
            targetRenderer.material = originalMaterial;
            Destroy(flashMaterial); // Clean up temporary material
        }
    }
}
```

---

## Setting Up in Unity

1.  **Create a new C# Script** named `HealthSystem` and copy the first code block into it.
2.  **Create a new C# Script** named `Attacker` and copy the second code block into it.
3.  **Create a new C# Script** named `HealthBarUI` and copy the third code block into it.
4.  **Create a new C# Script** named `DamageFeedback` and copy the fourth code block into it.

**Example Scene Setup:**

1.  **Player/Enemy GameObject:**
    *   Create a 3D Object (e.g., a Cube). Name it "Player" (or "Enemy").
    *   Add a `HealthSystem` component to it. Adjust `Max Health`, `Can Be Invulnerable`, and `Invulnerability Duration` in the Inspector.
    *   Add a `DamageFeedback` component to it. Drag an `AudioSource` to this GameObject, then drag `hitSound`, `healSound`, `deathSound` (you'll need to import some sounds or use placeholders) into the `DamageFeedback` component's slots.
    *   Ensure the Player/Enemy has a `Collider` (e.g., Box Collider, Capsule Collider) and a `Rigidbody` (if it's intended to move or be affected by physics).
    *   Set its `Tag` to "Player" or "Enemy" so the `Attacker` script can identify it.

2.  **Attacker GameObject:**
    *   Create another 3D Object (e.g., a Sphere for a projectile).
    *   Add an `Attacker` component to it. Adjust `Damage Amount` and `Target Tag` as needed.
    *   If it's a projectile, ensure it has a `Collider` (set to `Is Trigger` if you want it to pass through) and a `Rigidbody`. Add a script to make it move forward (e.g., `GetComponent<Rigidbody>().velocity = transform.forward * speed;` in `Start()`).

3.  **UI Canvas and Health Bar:**
    *   Create a UI -> Canvas.
    *   Inside the Canvas, create a UI -> Slider. Name it "PlayerHealthBar".
    *   Add a `HealthBarUI` component to the "PlayerHealthBar" Slider.
    *   Drag your "Player" GameObject from the Hierarchy into the `Target Health System` slot of the `HealthBarUI` component.
    *   The `HealthBarUI` should automatically find the `Slider` component if it's on the same GameObject, or you can drag the Slider itself into the `Health Slider` slot.

Now, when your `Attacker` hits the "Player" (or whatever object has the `HealthSystem`), the player will take damage, the UI health bar will update, and visual/audio feedback will occur. The `HealthSystem` handles all the complex logic, keeping your attacking and UI scripts clean and focused.