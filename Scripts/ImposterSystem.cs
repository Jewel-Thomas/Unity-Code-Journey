// Unity Design Pattern Example: ImposterSystem
// This script demonstrates the ImposterSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The ImposterSystem design pattern, while not one of the "Gang of Four" patterns, is a powerful technique often used in C# and other object-oriented languages. It leverages interfaces and object composition (similar to Decorator or Proxy patterns) to dynamically inject cross-cutting concerns (like logging, performance monitoring, security, caching, etc.) into an existing object's behavior without modifying the original object's code.

The core idea is:
1.  **Interface:** Define an interface for the core functionality.
2.  **Original (Concrete) Implementation:** The actual class that performs the main logic, implementing the interface.
3.  **Imposter (Decorator/Proxy) Implementations:** Classes that also implement the *same interface*. Each imposter takes an instance of the interface (which could be the original object or another imposter) in its constructor. It then delegates calls to the wrapped instance, adding its own pre- or post-processing logic.
4.  **Imposter System/Factory:** A central point (often a factory or service locator) that is responsible for creating and assembling the chain of imposters around the original object, based on configuration or runtime conditions.

This allows you to add features like logging or performance tracking to a component *after* it's been written, purely by changing how its instance is provided to clients, and without ever touching the component's internal logic.

---

Here's a complete Unity example demonstrating the ImposterSystem pattern.

**Scenario:** We have a `PlayerCharacter` that can perform actions like `Move`, `Attack`, and `TakeDamage`. We want to add:
*   **Logging:** Record when actions occur.
*   **Performance Monitoring:** Measure how long actions take.
*   **Authorization:** Prevent actions if the character is "unauthorized".

We will implement these as Imposters, wrapping the `PlayerCharacter`'s core actions.

---

### Unity Setup:

1.  Create a new Unity project or open an existing one.
2.  Create an empty GameObject in your scene named `Player`.
3.  Attach the `PlayerCharacter.cs` script to the `Player` GameObject.
4.  Create another empty GameObject named `GameManager`.
5.  Attach the `ImposterDemonstrator.cs` script to the `GameManager` GameObject.
6.  Drag the `Player` GameObject from your Hierarchy into the `Player Character` slot on the `ImposterDemonstrator` component in the Inspector.
7.  Create a simple cube GameObject and name it `Target`. Place it somewhere in the scene. Drag this `Target` GameObject into the `Target GameObject` slot on the `ImposterDemonstrator` component.

---

### 1. `ICharacterActions.cs` (Interface Definition)

This interface defines the contract that both the original `PlayerCharacter` and all Imposters will adhere to.

```csharp
// File: Assets/Scripts/ImposterSystem/ICharacterActions.cs
using UnityEngine; // Required for Vector3 and GameObject

/// <summary>
/// Defines the core actions a character can perform.
/// This is the common interface that both the real implementation
/// and all imposter (decorator/proxy) implementations will adhere to.
/// </summary>
public interface ICharacterActions
{
    void Move(Vector3 direction);
    void Attack(GameObject target);
    void TakeDamage(float amount);
}
```

---

### 2. `PlayerCharacter.cs` (Original Implementation)

This is the concrete class that contains the actual game logic. Notice it only focuses on its core responsibilities and has no knowledge of logging, performance, or authorization.

```csharp
// File: Assets/Scripts/ImposterSystem/PlayerCharacter.cs
using UnityEngine;
using System.Collections; // Required for IEnumerator

/// <summary>
/// The 'Original' or 'Concrete Component' in the ImposterSystem pattern.
/// This class contains the actual game logic for character actions.
/// It implements ICharacterActions and has no knowledge of any cross-cutting concerns
/// like logging, performance monitoring, or authorization checks.
/// </summary>
public class PlayerCharacter : MonoBehaviour, ICharacterActions
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth => currentHealth;

    // --- Core Action Implementations ---

    /// <summary>
    /// Implements the character's movement logic.
    /// </summary>
    /// <param name="direction">The direction to move.</param>
    public void Move(Vector3 direction)
    {
        transform.Translate(direction.normalized * moveSpeed * Time.deltaTime);
        Debug.Log($"<color=green>[PlayerCharacter]</color> Moving character in direction: {direction}. Current position: {transform.position}");
    }

    /// <summary>
    /// Implements the character's attack logic.
    /// </summary>
    /// <param name="target">The target GameObject to attack.</param>
    public void Attack(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning($"<color=green>[PlayerCharacter]</color> Tried to attack, but target is null.");
            return;
        }
        Debug.Log($"<color=green>[PlayerCharacter]</color> Attacking {target.name} for {attackDamage} damage!");
        // Simulate damage application (e.g., to a health component on the target)
        // For this example, we'll just log it.
    }

    /// <summary>
    /// Implements the character taking damage.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log($"<color=green>[PlayerCharacter]</color> Character took {amount} damage and is defeated! Health: {currentHealth}/{maxHealth}");
            // Trigger death animation, respawn, etc.
        }
        else
        {
            Debug.Log($"<color=green>[PlayerCharacter]</color> Character took {amount} damage. Current Health: {currentHealth}/{maxHealth}");
        }
    }

    // Example of an internal method not part of the interface
    private void RegenerateHealth(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"<color=green>[PlayerCharacter]</color> Regenerated {amount} health. Current Health: {currentHealth}/{maxHealth}");
    }
}
```

---

### 3. `CharacterActionImposterBase.cs` (Base Imposter Class - Optional but Recommended)

This abstract base class helps reduce boilerplate in concrete imposter classes by providing a constructor that stores the decorated object and default implementations that simply delegate to it.

```csharp
// File: Assets/Scripts/ImposterSystem/CharacterActionImposterBase.cs
using UnityEngine;
using System; // Required for ArgumentNullException

/// <summary>
/// An abstract base class for all character action imposters.
/// This class handles the common task of holding a reference to the
/// next object in the imposter chain (or the original object)
/// and provides default implementations that simply delegate the call.
/// Concrete imposters can then override specific methods to add their
/// cross-cutting concerns.
/// </summary>
public abstract class CharacterActionImposterBase : ICharacterActions
{
    // The next object in the chain, could be another imposter or the original PlayerCharacter.
    protected ICharacterActions _decoratedActions;

    /// <summary>
    /// Constructor for the base imposter.
    /// </summary>
    /// <param name="decoratedActions">The ICharacterActions instance to wrap (decorate).</param>
    /// <exception cref="ArgumentNullException">Thrown if decoratedActions is null.</exception>
    public CharacterActionImposterBase(ICharacterActions decoratedActions)
    {
        _decoratedActions = decoratedActions ?? throw new ArgumentNullException(nameof(decoratedActions));
    }

    // --- Default Delegated Implementations ---
    // These methods simply pass the call to the next decorated object.
    // Concrete imposters will override these to add their specific logic.

    public virtual void Move(Vector3 direction)
    {
        _decoratedActions.Move(direction);
    }

    public virtual void Attack(GameObject target)
    {
        _decoratedActions.Attack(target);
    }

    public virtual void TakeDamage(float amount)
    {
        _decoratedActions.TakeDamage(amount);
    }
}
```

---

### 4. Concrete Imposters

These classes add specific cross-cutting concerns without altering `PlayerCharacter.cs`.

#### `LoggingCharacterImposter.cs`

```csharp
// File: Assets/Scripts/ImposterSystem/LoggingCharacterImposter.cs
using UnityEngine;

/// <summary>
/// An imposter that adds logging capabilities to character actions.
/// It wraps an ICharacterActions instance and logs calls before and after
/// delegating the action to the wrapped object.
/// </summary>
public class LoggingCharacterImposter : CharacterActionImposterBase
{
    public LoggingCharacterImposter(ICharacterActions decoratedActions)
        : base(decoratedActions) { }

    public override void Move(Vector3 direction)
    {
        Debug.Log($"<color=blue>[Imposter - Logging]</color> --- BEFORE Move({direction}) ---");
        base.Move(direction); // Delegate to the next decorated object (or original)
        Debug.Log($"<color=blue>[Imposter - Logging]</color> --- AFTER Move({direction}) ---");
    }

    public override void Attack(GameObject target)
    {
        Debug.Log($"<color=blue>[Imposter - Logging]</color> --- BEFORE Attack({target?.name}) ---");
        base.Attack(target);
        Debug.Log($"<color=blue>[Imposter - Logging]</color> --- AFTER Attack({target?.name}) ---");
    }

    public override void TakeDamage(float amount)
    {
        Debug.Log($"<color=blue>[Imposter - Logging]</color> --- BEFORE TakeDamage({amount}) ---");
        base.TakeDamage(amount);
        Debug.Log($"<color=blue>[Imposter - Logging]</color> --- AFTER TakeDamage({amount}) ---");
    }
}
```

#### `PerformanceMonitoringCharacterImposter.cs`

```csharp
// File: Assets/Scripts/ImposterSystem/PerformanceMonitoringCharacterImposter.cs
using UnityEngine;
using System.Diagnostics; // Required for Stopwatch

/// <summary>
/// An imposter that monitors the performance (execution time) of character actions.
/// It uses a Stopwatch to measure how long each delegated action takes.
/// </summary>
public class PerformanceMonitoringCharacterImposter : CharacterActionImposterBase
{
    public PerformanceMonitoringCharacterImposter(ICharacterActions decoratedActions)
        : base(decoratedActions) { }

    private void MeasureAction(string actionName, System.Action action)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        action.Invoke(); // Execute the actual action (delegated or original)

        stopwatch.Stop();
        Debug.Log($"<color=magenta>[Imposter - Performance]</color> {actionName} executed in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
    }

    public override void Move(Vector3 direction)
    {
        MeasureAction($"Move({direction})", () => base.Move(direction));
    }

    public override void Attack(GameObject target)
    {
        MeasureAction($"Attack({target?.name})", () => base.Attack(target));
    }

    public override void TakeDamage(float amount)
    {
        MeasureAction($"TakeDamage({amount})", () => base.TakeDamage(amount));
    }
}
```

#### `AuthorizationCharacterImposter.cs`

```csharp
// File: Assets/Scripts/ImposterSystem/AuthorizationCharacterImposter.cs
using UnityEngine;

/// <summary>
/// An imposter that adds an authorization check before performing character actions.
/// If the character is not authorized, the action is blocked.
/// </summary>
public class AuthorizationCharacterImposter : CharacterActionImposterBase
{
    // A simple flag to simulate authorization status.
    // In a real game, this might come from a player state, permissions system, etc.
    public bool IsAuthorized { get; set; } = true; // Default to authorized

    public AuthorizationCharacterImposter(ICharacterActions decoratedActions)
        : base(decoratedActions) { }

    private bool CheckAuthorization(string actionName)
    {
        if (!IsAuthorized)
        {
            Debug.LogWarning($"<color=red>[Imposter - Authorization]</color> Action '{actionName}' blocked: Character is not authorized!");
            return false;
        }
        return true;
    }

    public override void Move(Vector3 direction)
    {
        if (CheckAuthorization("Move"))
        {
            base.Move(direction);
        }
        else
        {
            Debug.Log($"<color=red>[Imposter - Authorization]</color> Cannot move while unauthorized. Attempted direction: {direction}");
        }
    }

    public override void Attack(GameObject target)
    {
        if (CheckAuthorization("Attack"))
        {
            base.Attack(target);
        }
        else
        {
            Debug.Log($"<color=red>[Imposter - Authorization]</color> Cannot attack while unauthorized. Attempted target: {target?.name}");
        }
    }

    public override void TakeDamage(float amount)
    {
        // Damage is usually an unavoidable event, so we might not block it with authorization.
        // However, for demonstration, we can simulate an 'invulnerability' state.
        if (CheckAuthorization("TakeDamage - Invulnerability Check"))
        {
            // If authorized means 'can take damage', or if unauthorized means 'is invulnerable'
            // Let's interpret IsAuthorized as 'can perform this action normally'.
            // So if IsAuthorized is FALSE, then we are 'invulnerable' to damage.
            Debug.Log($"<color=red>[Imposter - Authorization]</color> Invulnerable! {amount} damage ignored.");
        }
        else // This path is taken if IsAuthorized is FALSE
        {
            base.TakeDamage(amount); // This would mean 'unauthorized to be invulnerable', so take damage.
                                     // Let's adjust for clarity: If IsAuthorized=true, take damage. If false, block damage.
        }
    }
}
```
**Correction for `AuthorizationCharacterImposter` on `TakeDamage`**:
The `TakeDamage` logic above is a bit inverted for a typical authorization check. If `IsAuthorized` means "can perform actions normally", then if it's `false`, we *block* the action. For `TakeDamage`, `IsAuthorized = false` could imply "invulnerable". Let's refine it:

```csharp
// File: Assets/Scripts/ImposterSystem/AuthorizationCharacterImposter.cs
// (Revised TakeDamage for clarity)
using UnityEngine;

/// <summary>
/// An imposter that adds an authorization check before performing character actions.
/// If the character is not authorized, the action is blocked.
/// For TakeDamage, if IsAuthorized is false, it means the character is "invulnerable".
/// </summary>
public class AuthorizationCharacterImposter : CharacterActionImposterBase
{
    public bool IsAuthorized { get; set; } = true; // Default to authorized (i.e., not invulnerable for damage)

    public AuthorizationCharacterImposter(ICharacterActions decoratedActions)
        : base(decoratedActions) { }

    private bool IsActionPermitted(string actionName)
    {
        if (!IsAuthorized)
        {
            Debug.LogWarning($"<color=red>[Imposter - Authorization]</color> Action '{actionName}' blocked: Character is not authorized!");
            return false;
        }
        return true;
    }

    public override void Move(Vector3 direction)
    {
        if (IsActionPermitted("Move"))
        {
            base.Move(direction);
        }
        else
        {
            Debug.Log($"<color=red>[Imposter - Authorization]</color> Move action denied due to unauthorized status.");
        }
    }

    public override void Attack(GameObject target)
    {
        if (IsActionPermitted("Attack"))
        {
            base.Attack(target);
        }
        else
        {
            Debug.Log($"<color=red>[Imposter - Authorization]</color> Attack action denied due to unauthorized status.");
        }
    }

    public override void TakeDamage(float amount)
    {
        // For TakeDamage, we can interpret 'IsAuthorized = false' as 'invulnerable'.
        // If the character is *not* authorized to take damage, then we block it.
        if (!IsAuthorized) // If NOT authorized (i.e., invulnerable)
        {
            Debug.Log($"<color=red>[Imposter - Authorization]</color> Character is invulnerable! {amount} damage ignored.");
        }
        else // If authorized (i.e., can take damage normally)
        {
            base.TakeDamage(amount);
        }
    }
}
```

---

### 5. `CharacterActionImposterSystem.cs` (The Imposter System / Factory)

This is the heart of the pattern. It's responsible for assembling the chain of imposters around the original `PlayerCharacter` based on configuration.

```csharp
// File: Assets/Scripts/ImposterSystem/CharacterActionImposterSystem.cs
using UnityEngine;
using System.Collections.Generic; // Required for List

/// <summary>
/// The central 'Imposter System' or 'Imposter Factory'.
/// This class is responsible for creating and assembling the chain of imposters
/// around an original ICharacterActions instance.
/// It determines which cross-cutting concerns (Imposters) should be applied.
/// </summary>
public static class CharacterActionImposterSystem
{
    // --- Configuration Flags ---
    // These static flags control which imposters are enabled.
    // In a real project, these might be loaded from a config file,
    // determined by build settings (e.g., #if UNITY_EDITOR), or runtime conditions.
    public static bool EnableLoggingImposter = true;
    public static bool EnablePerformanceMonitoringImposter = true;
    public static bool EnableAuthorizationImposter = true;

    /// <summary>
    /// Creates a decorated (imposter-wrapped) version of the original character actions.
    /// This is where the ImposterSystem decides which imposters to apply and in what order.
    /// </summary>
    /// <param name="originalCharacterActions">The raw, undecorated ICharacterActions instance (e.g., a PlayerCharacter MonoBehaviour).</param>
    /// <returns>An ICharacterActions instance that might be the original, or an imposter wrapping the original,
    /// or a chain of imposters.</returns>
    public static ICharacterActions GetDecoratedCharacterActions(ICharacterActions originalCharacterActions)
    {
        // Start with the original object as the base of our decoration chain.
        ICharacterActions decoratedActions = originalCharacterActions;

        // Apply imposters in the desired order.
        // The order matters for how concerns are processed.
        // Example: Logging -> Performance -> Authorization -> Original
        // This means: Log (start) -> Measure (start) -> Check Auth -> (if authorized) -> Original Action -> Measure (end) -> Log (end)

        if (EnableLoggingImposter)
        {
            // Wrap the current 'decoratedActions' with the LoggingImposter.
            // The output of this becomes the new 'decoratedActions' for the next imposter.
            decoratedActions = new LoggingCharacterImposter(decoratedActions);
            Debug.Log("<color=grey>[ImposterSystem]</color> LoggingImposter enabled.");
        }

        if (EnablePerformanceMonitoringImposter)
        {
            decoratedActions = new PerformanceMonitoringCharacterImposter(decoratedActions);
            Debug.Log("<color=grey>[ImposterSystem]</color> PerformanceMonitoringImposter enabled.");
        }

        if (EnableAuthorizationImposter)
        {
            // When AuthorizationImposter is enabled, we might want to get a reference to it
            // if we need to modify its state (e.g., toggle authorization) later.
            var authImposter = new AuthorizationCharacterImposter(decoratedActions);
            decoratedActions = authImposter;
            Debug.Log("<color=grey>[ImposterSystem]</color> AuthorizationImposter enabled.");

            // Store a reference to the authorization imposter if it's created,
            // so we can manipulate its state (e.g., call ToggleAuthorization).
            // This is a common pattern when imposters expose mutable state.
            // For simplicity, we'll expose a static setter for it below.
            SetCurrentAuthorizationImposter(authImposter);
        }

        Debug.Log($"<color=grey>[ImposterSystem]</color> Final decorated object type: {decoratedActions.GetType().Name}");

        return decoratedActions;
    }

    // --- Static Reference for State Manipulation (Optional) ---
    // If an imposter has mutable state that needs to be controlled externally,
    // the system can expose a way to get or set that specific imposter.
    private static AuthorizationCharacterImposter _currentAuthorizationImposter;

    private static void SetCurrentAuthorizationImposter(AuthorizationCharacterImposter imposter)
    {
        _currentAuthorizationImposter = imposter;
    }

    /// <summary>
    /// Toggles the authorization status of the AuthorizationImposter, if it's active.
    /// </summary>
    /// <param name="isAuthorized">The new authorization status.</param>
    public static void SetAuthorizationStatus(bool isAuthorized)
    {
        if (_currentAuthorizationImposter != null)
        {
            _currentAuthorizationImposter.IsAuthorized = isAuthorized;
            Debug.Log($"<color=grey>[ImposterSystem]</color> Authorization status set to: {isAuthorized}");
        }
        else
        {
            Debug.LogWarning($"<color=grey>[ImposterSystem]</color> Cannot set authorization status: AuthorizationImposter is not active.");
        }
    }
}
```

---

### 6. `ImposterDemonstrator.cs` (Client Usage)

This `MonoBehaviour` acts as a client to the `ImposterSystem`. It requests the `ICharacterActions` interface from the system and uses it, without knowing if it's interacting with the raw `PlayerCharacter` or a chain of imposters.

```csharp
// File: Assets/Scripts/ImposterSystem/ImposterDemonstrator.cs
using UnityEngine;

/// <summary>
/// A MonoBehaviour to demonstrate the usage of the ImposterSystem.
/// This acts as a client that requests an ICharacterActions instance from the system.
/// It interacts solely with the ICharacterActions interface, unaware if it's dealing
/// with the raw PlayerCharacter or an imposter-decorated version.
/// </summary>
public class ImposterDemonstrator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerCharacter playerCharacter; // Assign the Player GameObject here in Inspector
    [SerializeField] private GameObject targetGameObject; // Assign a Target GameObject here in Inspector
    [SerializeField] private float damageAmount = 20f;
    [SerializeField] private float moveDistance = 1f;

    // The interface reference through which we will interact with the character.
    private ICharacterActions _characterActions;

    // --- Example Usage Setup ---

    void Awake()
    {
        if (playerCharacter == null)
        {
            Debug.LogError("PlayerCharacter not assigned to ImposterDemonstrator!", this);
            enabled = false;
            return;
        }

        // --- Core of the ImposterSystem Usage ---
        // Request the decorated ICharacterActions instance from our ImposterSystem.
        // The ImposterSystem decides which imposters (if any) to apply.
        _characterActions = CharacterActionImposterSystem.GetDecoratedCharacterActions(playerCharacter);

        Debug.Log($"<color=lime>[ImposterDemonstrator]</color> Initialized with CharacterActions of type: {_characterActions.GetType().Name}");

        // Example: If we want to change imposter settings dynamically,
        // we can set up initial states or provide UI to toggle them.
        CharacterActionImposterSystem.SetAuthorizationStatus(true); // Start authorized
    }

    // --- Demonstration Controls ---

    void Update()
    {
        // Movement Input
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("<color=lime>[ImposterDemonstrator]</color> W key pressed: Attempting to Move.");
            _characterActions.Move(Vector3.forward * moveDistance);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("<color=lime>[ImposterDemonstrator]</color> S key pressed: Attempting to Move.");
            _characterActions.Move(Vector3.back * moveDistance);
        }

        // Attack Input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("<color=lime>[ImposterDemonstrator]</color> Space key pressed: Attempting to Attack.");
            _characterActions.Attack(targetGameObject);
        }

        // Take Damage Input
        if (Input.GetKeyDown(KeyCode.H)) // 'H' for Hurt
        {
            Debug.Log("<color=lime>[ImposterDemonstrator]</color> H key pressed: Attempting to Take Damage.");
            _characterActions.TakeDamage(damageAmount);
        }

        // Toggle Authorization Status
        if (Input.GetKeyDown(KeyCode.T)) // 'T' for Toggle Authorization
        {
            bool currentStatus = (CharacterActionImposterSystem._currentAuthorizationImposter != null) ?
                                  CharacterActionImposterSystem._currentAuthorizationImposter.IsAuthorized :
                                  false;
            CharacterActionImposterSystem.SetAuthorizationStatus(!currentStatus);
        }

        // Toggle All Imposters (for demonstration purposes, would usually be static config)
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Toggle Logging
        {
            CharacterActionImposterSystem.EnableLoggingImposter = !CharacterActionImposterSystem.EnableLoggingImposter;
            Debug.Log($"<color=yellow>[ImposterDemonstrator]</color> Logging Imposter Toggled: {CharacterActionImposterSystem.EnableLoggingImposter}");
            ReinitializeImposters();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) // Toggle Performance
        {
            CharacterActionImposterSystem.EnablePerformanceMonitoringImposter = !CharacterActionImposterSystem.EnablePerformanceMonitoringImposter;
            Debug.Log($"<color=yellow>[ImposterDemonstrator]</color> Performance Imposter Toggled: {CharacterActionImposterSystem.EnablePerformanceMonitoringImposter}");
            ReinitializeImposters();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) // Toggle Authorization
        {
            CharacterActionImposterSystem.EnableAuthorizationImposter = !CharacterActionImposterSystem.EnableAuthorizationImposter;
            Debug.Log($"<color=yellow>[ImposterDemonstrator]</color> Authorization Imposter Toggled: {CharacterActionImposterSystem.EnableAuthorizationImposter}");
            ReinitializeImposters();
        }
    }

    // Helper to re-create the imposter chain after toggling flags
    private void ReinitializeImposters()
    {
        Debug.Log("<color=yellow>[ImposterDemonstrator]</color> Re-initializing Imposter chain...");
        _characterActions = CharacterActionImposterSystem.GetDecoratedCharacterActions(playerCharacter);
        Debug.Log($"<color=yellow>[ImposterDemonstrator]</color> New CharacterActions type: {_characterActions.GetType().Name}");
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Logging Imposter: {CharacterActionImposterSystem.EnableLoggingImposter} (Press 1)");
        GUI.Label(new Rect(10, 30, 300, 20), $"Performance Imposter: {CharacterActionImposterSystem.EnablePerformanceMonitoringImposter} (Press 2)");
        GUI.Label(new Rect(10, 50, 300, 20), $"Authorization Imposter: {CharacterActionImposterSystem.EnableAuthorizationImposter} (Press 3)");
        
        bool authStatus = (CharacterActionImposterSystem._currentAuthorizationImposter != null) ?
                          CharacterActionImposterSystem._currentAuthorizationImposter.IsAuthorized :
                          false; // Default if no imposter
        GUI.Label(new Rect(10, 70, 300, 20), $"Current Auth Status: {authStatus} (Press T to Toggle)");

        GUI.Label(new Rect(10, 100, 300, 20), "Press W/S to Move");
        GUI.Label(new Rect(10, 120, 300, 20), "Press Space to Attack");
        GUI.Label(new Rect(10, 140, 300, 20), "Press H to Take Damage");
    }
}
```

---

### How to Use and Observe:

1.  **Run the Scene:** Hit Play in the Unity Editor.
2.  **Observe Console:** Watch the Unity Console output.
    *   Initially, you'll see messages from all Imposters (Logging, Performance, Authorization) and the `PlayerCharacter` itself.
    *   `Move`, `Attack`, `TakeDamage` actions triggered by `W`, `S`, `Space`, `H` keys.
3.  **Toggle Imposters:**
    *   Press `1` to toggle the `LoggingImposter`. You'll notice the blue `[Imposter - Logging]` messages disappear/reappear.
    *   Press `2` to toggle the `PerformanceMonitoringImposter`. You'll see the magenta `[Imposter - Performance]` messages.
    *   Press `3` to toggle the `AuthorizationImposter`.
4.  **Toggle Authorization Status:**
    *   If the `AuthorizationImposter` is enabled (default), press `T`. This will toggle the player's authorization status (e.g., between authorized and unauthorized/invulnerable).
    *   When unauthorized, `Move` and `Attack` actions will be blocked, and `TakeDamage` will be ignored.
    *   Notice how the original `PlayerCharacter`'s `Debug.Log` messages for `Move`, `Attack`, `TakeDamage` only appear when the action actually reaches it (i.e., when not blocked by an imposter).

This example fully demonstrates how the ImposterSystem allows you to dynamically add, remove, and configure cross-cutting concerns around your core game logic without ever modifying the `PlayerCharacter` class itself, making your code more modular, testable, and maintainable.