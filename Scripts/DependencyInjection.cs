// Unity Design Pattern Example: DependencyInjection
// This script demonstrates the DependencyInjection pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity script demonstrates the **Dependency Injection (DI)** design pattern. It's designed to be educational, practical, and immediately usable in a Unity project.

The core idea of DI is that a class should receive its dependencies (objects it needs to function) from an external source, rather than creating them itself. This promotes loose coupling, testability, and flexibility.

---

### Key Concepts Demonstrated:

1.  **Interfaces:** Define contracts for dependencies, allowing for various implementations.
2.  **Concrete Implementations:** Actual classes that fulfill the interface contracts.
3.  **Dependent Class:** The class that *needs* the dependencies (e.g., `Player`). It depends on interfaces, not concrete types.
4.  **Composition Root (Injector):** A dedicated class responsible for creating all objects and their dependencies, and then injecting those dependencies into the objects that need them. This example uses "Method Injection" for `MonoBehaviour`s, where an `Initialize` method is called by the Composition Root to pass in dependencies.

---

### How to Use This Example in Unity:

1.  **Create a C# Script:**
    *   In your Unity project, right-click in the Project window -> Create -> C# Script.
    *   Name it `DependencyInjectionExample`.
    *   Copy and paste the entire content of the code block below into this new script, overwriting any default content.

2.  **Create the Player GameObject:**
    *   In the Unity Hierarchy window, right-click -> Create Empty.
    *   Name this new GameObject `PlayerGameObject`.
    *   Select `PlayerGameObject`. In the Inspector, click "Add Component" and search for "Player" (our `Player` script). Add it.

3.  **Create the Game Initializer GameObject (Composition Root):**
    *   In the Unity Hierarchy window, right-click -> Create Empty.
    *   Name this new GameObject `GameManager` (or `GameInitializer`).
    *   Select `GameManager`. In the Inspector, click "Add Component" and search for "GameInitializer" (our `GameInitializer` script). Add it.

4.  **Link the Player to the Game Initializer:**
    *   Select the `GameManager` GameObject in the Hierarchy.
    *   In its Inspector, you will see a field labeled "Player".
    *   Drag your `PlayerGameObject` from the Hierarchy onto this "Player" field in the `GameManager`'s Inspector.

5.  **Run the Scene:**
    *   Press the Play button in the Unity editor.
    *   Observe the Console window. You should see log messages indicating:
        *   The `GameInitializer` starting.
        *   The dependencies (`Gun` and `DebugLogger` in the default setup) being instantiated.
        *   The `Player` being initialized with these dependencies.
        *   The `Player` performing its `Start()` actions (attacking and logging).

6.  **Experiment with Flexibility:**
    *   Stop playback.
    *   Go to the `DependencyInjectionExample.cs` script.
    *   In the `GameInitializer`'s `Awake` method, change `IWeapon playerWeapon = new Gun();` to `IWeapon playerWeapon = new Sword();`.
    *   Run the scene again. Notice the Player now uses a `Sword` without any changes to the `Player` script itself! This perfectly demonstrates loose coupling and the power of DI.

---

```csharp
// DependencyInjectionExample.cs
// This script provides a comprehensive, practical, and educational example
// of the Dependency Injection (DI) design pattern in Unity.
// It demonstrates how to achieve loose coupling, enhance testability, and improve
// maintainability by providing dependencies to an object rather than letting the
// object create them directly.

// The core idea of DI is that a class should receive its dependencies from an
// external source (the "Composition Root"). This allows the dependent class
// to focus on its own logic without worrying about how its dependencies are created or managed.

using UnityEngine;
using System; // Required for core C# types, potentially Action/Func if used

// --- 1. Define Interfaces for Dependencies ---
// Interfaces are fundamental to Dependency Injection. They define a contract that
// concrete implementations must adhere to, which is crucial for achieving loose coupling.
// The 'Player' class will depend on these interfaces, not on specific concrete types
// (e.g., it needs an 'IWeapon', not specifically a 'Sword' or a 'Gun').

/// <summary>
/// Interface for a weapon. Defines the contract for any object that can be used as a weapon.
/// Any class that implements IWeapon must provide an 'Attack' method.
/// </summary>
public interface IWeapon
{
    void Attack();
}

/// <summary>
/// Interface for a logger. Defines the contract for any object that can log messages.
/// Any class that implements ILogger must provide a 'Log' method.
/// </summary>
public interface ILogger
{
    void Log(string message);
}

// --- 2. Create Concrete Implementations for Dependencies ---
// These are the actual classes that provide the functionality defined by the interfaces.
// The dependent class (Player) does not need to know about these specific types,
// only that they fulfill the 'IWeapon' or 'ILogger' contract.

/// <summary>
/// A concrete implementation of IWeapon: A Sword.
/// </summary>
public class Sword : IWeapon
{
    public void Attack()
    {
        Debug.Log("<color=cyan>Player swings a mighty Sword!</color>");
    }
}

/// <summary>
/// A concrete implementation of IWeapon: A Gun.
/// </summary>
public class Gun : IWeapon
{
    public void Attack()
    {
        Debug.Log("<color=green>Player fires a powerful Gun!</color>");
    }
}

/// <summary>
/// A concrete implementation of ILogger that uses Unity's Debug.Log.
/// This could easily be swapped for a FileLogger, NetworkLogger, etc.,
/// without changing the Player class.
/// </summary>
public class DebugLogger : ILogger
{
    public void Log(string message)
    {
        Debug.Log($"<color=yellow>[DebugLogger]: {message}</color>");
    }
}

// --- 3. The Dependent Class (Player) ---
// This is the class that requires (depends on) other services (an IWeapon and an ILogger).
// Crucially, the Player class does NOT create instances of Sword, Gun, or DebugLogger itself.
// Instead, it receives them through an injection method. This is where loose coupling shines.

/// <summary>
/// Represents a Player character that needs a weapon and a logger to perform actions.
/// This class demonstrates 'Method Injection' for MonoBehaviour components, which is
/// a common and practical way to do DI in Unity without complex frameworks.
/// </summary>
public class Player : MonoBehaviour
{
    // Private fields to hold the injected dependencies.
    // They are of interface types (IWeapon, ILogger), not concrete types (Sword, Gun, DebugLogger).
    // This design choice ensures loose coupling: Player only knows it needs *a* weapon and *a* logger.
    private IWeapon _weapon;
    private ILogger _logger;

    // A flag to ensure dependencies are initialized before any methods like Start() or Update()
    // attempt to use them. This is a good practice for MonoBehaviour DI.
    private bool _isInitialized = false;

    /// <summary>
    /// This is the PRIMARY DEPENDENCY INJECTION POINT for the Player class.
    /// An external class (our 'GameInitializer' in this example) will call this method
    /// to provide the Player with its required dependencies (an IWeapon and an ILogger).
    /// This pattern is known as "Method Injection" or "Setter Injection".
    /// </summary>
    /// <param name="weapon">The weapon implementation to use (e.g., Sword, Gun).</param>
    /// <param name="logger">The logger implementation to use (e.g., DebugLogger).</param>
    public void Initialize(IWeapon weapon, ILogger logger)
    {
        // Basic validation to ensure dependencies are not null.
        // If they were, it indicates a misconfiguration in the Composition Root.
        if (weapon == null)
        {
            Debug.LogError("Player.Initialize: Weapon dependency cannot be null.");
            return;
        }
        if (logger == null)
        {
            Debug.LogError("Player.Initialize: Logger dependency cannot be null.");
            return;
        }

        _weapon = weapon;
        _logger = logger;
        _isInitialized = true; // Mark as initialized after successful injection

        _logger.Log($"Player initialized successfully with '{_weapon.GetType().Name}' and '{_logger.GetType().Name}'.");
    }

    // Start is called before the first frame update.
    // We demonstrate using the injected dependencies here.
    void Start()
    {
        // Crucial check: Ensure dependencies have been injected before attempting to use them.
        if (!_isInitialized)
        {
            Debug.LogError("Player not initialized! Dependencies were not injected. " +
                           "Make sure GameInitializer calls Player.Initialize().");
            // In a real game, you might want a fallback (e.g., default weapon/logger)
            // or to disable the component if critical dependencies are missing.
            enabled = false; // Disable the component to prevent further errors
            return;
        }

        _logger.Log("Player is ready to act!");
        _weapon.Attack(); // Use the injected weapon's functionality
        _logger.Log("Player performed an action with its weapon."); // Use the injected logger
    }

    // Example of another method that uses the injected dependencies.
    public void PerformSpecialMove()
    {
        if (!_isInitialized)
        {
            _logger?.Log("Player cannot perform special move: Not initialized!");
            return;
        }
        _logger.Log("Player charges up a special move!");
        _weapon.Attack(); // Special move might also involve the weapon
    }
}


// --- 4. The Composition Root (GameInitializer) ---
// This is a MonoBehaviour that acts as the "Composition Root" for our example.
// The Composition Root is responsible for creating ALL objects and their dependencies,
// and then injecting those dependencies into the objects that need them.
// It's the "glue" that brings all the pieces together at the application's entry point.

/// <summary>
/// This class serves as the "Composition Root" for our Dependency Injection example.
/// Its primary role is to:
/// 1. Create instances of concrete dependency implementations (e.g., Sword, DebugLogger).
/// 2. Locate or instantiate the dependent class (Player).
/// 3. Inject the created dependencies into the dependent class via its Initialize method.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    // Public field to allow linking the Player GameObject from the Unity Inspector.
    // This shows how to inject into an already existing GameObject in the scene.
    [Tooltip("Drag the Player GameObject (which has the Player script attached) here from the Hierarchy.")]
    public Player player;

    // Awake is called when the script instance is being loaded,
    // ensuring initialization happens before any Start methods (including Player's Start).
    void Awake()
    {
        Debug.Log("GameInitializer: Starting dependency resolution and injection process...");

        // --- Step 1: Validate Setup ---
        if (player == null)
        {
            Debug.LogError("GameInitializer: Player GameObject not assigned! " +
                           "Please drag your 'PlayerGameObject' from the Hierarchy onto the 'Player' field " +
                           "of the 'GameManager' in the Inspector.");
            enabled = false; // Disable this component to prevent further errors
            return;
        }

        // --- Step 2: Dependency Resolution (Creating concrete instances) ---
        // Here, we decide WHICH concrete implementations to use. This is the crucial
        // part for flexibility. We are "resolving" the interfaces to concrete types.
        // If we want to change the player's weapon, we only change this line, not the Player class!

        // Example 1: Player gets a Gun and uses the DebugLogger
        IWeapon playerWeapon = new Gun();
        ILogger gameLogger = new DebugLogger();

        // Example 2 (Uncomment and comment out Example 1 to swap dependencies):
        // IWeapon playerWeapon = new Sword(); // Swap to Sword
        // ILogger gameLogger = new DebugLogger(); // Keep DebugLogger

        // Example 3 (Instantiate a new Player if it wasn't pre-placed in the scene)
        // This is if you wanted to create the Player dynamically.
        // GameObject playerGO = new GameObject("DynamicPlayer");
        // Player playerInstance = playerGO.AddComponent<Player>();
        // playerInstance.Initialize(playerWeapon, gameLogger);


        Debug.Log($"GameInitializer: Resolved dependencies: '{playerWeapon.GetType().Name}' and '{gameLogger.GetType().Name}'.");

        // --- Step 3: Dependency Injection ---
        // Now, we take the concrete instances we just created and inject them
        // into the 'Player' component via its public Initialize method.
        player.Initialize(playerWeapon, gameLogger);

        Debug.Log("GameInitializer: Dependencies successfully injected into Player.");
        _logger?.Log("GameInitializer: Completed setup."); // Using an injected logger for GameInitializer itself (optional)
    }

    // Note: The GameInitializer itself could also have dependencies injected into it,
    // though for a simple composition root, it often creates them.
    // For this example, we don't inject into GameInitializer for simplicity.
}


/*
// --- BENEFITS OF DEPENDENCY INJECTION ---

1.  <color=lime>Loose Coupling:</color> Classes depend on abstractions (interfaces) rather than concrete implementations. This means changing an implementation (e.g., from Sword to Gun) doesn't require modifying the dependent class (Player). The Player class doesn't care *how* it attacks, only that it *can* attack.

2.  <color=lime>Enhanced Testability:</color> Because dependencies can be easily swapped, you can provide "mock" or "stub" implementations during unit testing. For example, you could inject a `MockWeapon` that just logs "Mock Attack" (without actual game logic) to test the Player's internal logic in isolation.

3.  <color=lime>Improved Maintainability:</color> Code becomes easier to understand, manage, and refactor. Changes in one part of the system (e.g., how a Sword attacks) are less likely to break others because the contracts (interfaces) remain stable.

4.  <color=lime>Greater Flexibility and Extensibility:</color> It's easy to introduce new implementations (e.g., a `LaserGun`, a `Bow`, or a `FileLogger`) without altering existing code. Just create a new class implementing the interface and change the injection point in the Composition Root.

5.  <color=lime>Clearer Separation of Concerns:</color> The Player class focuses solely on *what* a player does, not *how* it obtains its tools. The GameInitializer focuses on *how* to set up the game world and its objects.

// --- ADVANCED CONSIDERATIONS (Beyond this example) ---

For larger, more complex Unity projects, manually managing all dependencies in a single `GameInitializer` can become cumbersome. This is where dedicated **Dependency Injection Frameworks (or "IoC Containers")** come in handy. Popular choices for Unity include:

-   <color=orange>**Zenject (Extenject):**</color> A powerful, feature-rich, and widely adopted DI framework for Unity. It handles object creation, automatic dependency resolution, and injection based on configurations you provide.
-   <color=orange>**VContainer:**</color> A more lightweight, modern, and performance-oriented DI framework for Unity, gaining popularity.

These frameworks provide mechanisms for:
-   Registering types and their implementations.
-   Automatic dependency resolution (the container figures out what needs to be injected based on your registrations).
-   Lifetime management (e.g., singletons, transient objects, per-scene objects).

This example uses "Manual DI" or "Pure DI," which is an excellent way to learn the pattern's fundamentals. It clearly illustrates the pattern's principles without the "magic" that a framework might introduce, making it ideal for educational purposes and perfectly suitable for many small to medium-sized Unity projects.
*/
```