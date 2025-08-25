// Unity Design Pattern Example: AbstractFactory
// This script demonstrates the AbstractFactory pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **Abstract Factory** design pattern. It provides a way to create families of related objects (like different types of enemies, obstacles, and collectibles) without specifying their concrete classes, based on a chosen "theme" (e.g., Fantasy or Sci-Fi).

This script is designed to be dropped directly into a Unity project.

---

**How to Use:**

1.  Create a new C# script in your Unity project named `AbstractFactoryClient`.
2.  Copy and paste the entire code below into the `AbstractFactoryClient.cs` file.
3.  Create an empty GameObject in your scene (or select an existing one like your Main Camera).
4.  Attach the `AbstractFactoryClient` script to this GameObject.
5.  In the Unity Inspector, select the desired **Game Theme** from the dropdown menu (Fantasy or SciFi).
6.  Run the scene.
7.  Observe the Unity Console for `Debug.Log` messages demonstrating the creation and interaction of the themed game elements.

---

```csharp
// ----------------------------------------------------------------------------
// AbstractFactoryClient.cs
//
// This script demonstrates the Abstract Factory design pattern in Unity.
// It allows for creating families of related game objects (enemies, obstacles,
// collectibles) without specifying their concrete classes, based on a chosen
// theme (e.g., Fantasy or Sci-Fi).
// ----------------------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic; // Commonly used in Unity, included for completeness.

// ============================================================================
// PART 1: ABSTRACT PRODUCTS
// These interfaces declare the types of products that can be created.
// Each interface represents a different type of game element.
// ============================================================================
#region Abstract Products

/// <summary>
/// Abstract Product A: Represents an enemy character in the game.
/// Defines the common interface for all enemy types.
/// </summary>
public interface IEnemy
{
    string GetName();
    void Attack();
    // In a real game, this might also include methods like GetPrefab(), TakeDamage(), etc.
}

/// <summary>
/// Abstract Product B: Represents an obstacle in the game environment.
/// Defines the common interface for all obstacle types.
/// </summary>
public interface IObstacle
{
    string GetName();
    void Interact();
    // In a real game, this might also include methods like GetPrefab(), ApplyEffect(), etc.
}

/// <summary>
/// Abstract Product C: Represents a collectible item in the game.
/// Defines the common interface for all collectible types.
/// </summary>
public interface ICollectible
{
    string GetName();
    void Collect();
    // In a real game, this might also include methods like GetPrefab(), GetValue(), etc.
}

#endregion

// ============================================================================
// PART 2: CONCRETE PRODUCTS
// These classes implement the abstract product interfaces for specific themes.
// Here we have two families of products: Fantasy and Sci-Fi.
// ============================================================================
#region Concrete Products - Fantasy Theme

/// <summary>
/// Concrete Product: A Fantasy-themed enemy.
/// </summary>
public class OrcEnemy : IEnemy
{
    public string GetName() => "Orc Grunt";
    public void Attack()
    {
        Debug.Log($"[{GetName()}] swings its crude axe, aiming for a critical hit!");
    }
}

/// <summary>
/// Concrete Product: A Fantasy-themed obstacle.
/// </summary>
public class TreeObstacle : IObstacle
{
    public string GetName() => "Ancient Tree";
    public void Interact()
    {
        Debug.Log($"[{GetName()}] blocks the path. It's too sturdy to break. You must find a detour!");
    }
}

/// <summary>
/// Concrete Product: A Fantasy-themed collectible.
/// </summary>
public class GoldCoinCollectible : ICollectible
{
    public string GetName() => "Shiny Gold Coin";
    public void Collect()
    {
        Debug.Log($"You collected a [{GetName()}]. Your purse feels heavier! (+10 Gold)");
    }
}

#endregion

#region Concrete Products - Sci-Fi Theme

/// <summary>
/// Concrete Product: A Sci-Fi-themed enemy.
/// </summary>
public class RobotEnemy : IEnemy
{
    public string GetName() => "Combat Robot Unit 7";
    public void Attack()
    {
        Debug.Log($"[{GetName()}] locks on target and fires its plasma cannon!");
    }
}

/// <summary>
/// Concrete Product: A Sci-Fi-themed obstacle.
/// </summary>
public class LaserFenceObstacle : IObstacle
{
    public string GetName() => "Active Laser Fence";
    public void Interact()
    {
        Debug.Log($"[{GetName()}] zaps you with high voltage when touched. Bypass or disable it!");
    }
}

/// <summary>
/// Concrete Product: A Sci-Fi-themed collectible.
/// </summary>
public class EnergyCellCollectible : ICollectible
{
    public string GetName() => "Plasma Energy Cell";
    public void Collect()
    {
        Debug.Log($"You collected a [{GetName()}]. Your suit's energy levels are restored! (+50 Energy)");
    }
}

#endregion

// ============================================================================
// PART 3: ABSTRACT FACTORY
// This interface declares a set of creation methods for each abstract product.
// It ensures that all concrete factories produce a full family of related products.
// ============================================================================
#region Abstract Factory

/// <summary>
/// Abstract Factory: Declares a set of methods for creating a family of related
/// abstract products (IEnemy, IObstacle, ICollectible).
/// </summary>
public interface IGameElementFactory
{
    IEnemy CreateEnemy();
    IObstacle CreateObstacle();
    ICollectible CreateCollectible();
    // Each method returns an abstract product, allowing the client to work with interfaces.
}

#endregion

// ============================================================================
// PART 4: CONCRETE FACTORIES
// These classes implement the abstract factory interface to produce concrete products
// belonging to a specific family/theme.
// ============================================================================
#region Concrete Factories

/// <summary>
/// Concrete Factory: Implements IGameElementFactory to create Fantasy-themed game elements.
/// It provides concrete implementations for each abstract product.
/// </summary>
public class FantasyGameElementFactory : IGameElementFactory
{
    public IEnemy CreateEnemy()
    {
        return new OrcEnemy(); // Returns a concrete OrcEnemy, which is an IEnemy.
    }

    public IObstacle CreateObstacle()
    {
        return new TreeObstacle(); // Returns a concrete TreeObstacle, which is an IObstacle.
    }

    public ICollectible CreateCollectible()
    {
        return new GoldCoinCollectible(); // Returns a concrete GoldCoinCollectible, which is an ICollectible.
    }
}

/// <summary>
/// Concrete Factory: Implements IGameElementFactory to create Sci-Fi-themed game elements.
/// </summary>
public class SciFiGameElementFactory : IGameElementFactory
{
    public IEnemy CreateEnemy()
    {
        return new RobotEnemy(); // Returns a concrete RobotEnemy, which is an IEnemy.
    }

    public IObstacle CreateObstacle()
    {
        return new LaserFenceObstacle(); // Returns a concrete LaserFenceObstacle, which is an IObstacle.
    }

    public ICollectible CreateCollectible()
    {
        return new EnergyCellCollectible(); // Returns a concrete EnergyCellCollectible, which is an ICollectible.
    }
}

#endregion

// ============================================================================
// PART 5: CLIENT CODE (Unity MonoBehaviour)
// The client uses the Abstract Factory and Abstract Products interfaces.
// It doesn't know the concrete classes of the products or factories being used.
// This allows the client to be decoupled from the specific implementation details
// of the product families.
// ============================================================================
#region Client

/// <summary>
/// The Client class (a MonoBehaviour in Unity) that uses the Abstract Factory
/// to create and interact with families of game elements.
/// </summary>
public class AbstractFactoryClient : MonoBehaviour
{
    /// <summary>
    /// Enum to easily switch between different game themes in the Unity Inspector.
    /// This defines the "family" of products we want to create.
    /// </summary>
    public enum GameTheme
    {
        Fantasy,
        SciFi
    }

    // [SerializeField] allows us to choose the theme directly in the Unity Editor Inspector.
    [SerializeField]
    private GameTheme _selectedTheme = GameTheme.Fantasy;

    private IGameElementFactory _factory; // Holds a reference to the abstract factory.

    // Start is called before the first frame update by Unity.
    void Start()
    {
        InitializeFactory(); // Determine which concrete factory to use.
        CreateAndInteractWithGameElements(); // Use the factory to create products.
    }

    /// <summary>
    /// Initializes the correct concrete factory based on the selected theme.
    /// This is the "factory creation" part where the specific family of products is chosen.
    /// The client's behavior changes by providing it with a different concrete factory.
    /// </summary>
    private void InitializeFactory()
    {
        switch (_selectedTheme)
        {
            case GameTheme.Fantasy:
                _factory = new FantasyGameElementFactory();
                Debug.Log("<color=cyan>--- Initializing with Fantasy Theme Factory ---</color>");
                break;
            case GameTheme.SciFi:
                _factory = new SciFiGameElementFactory();
                Debug.Log("<color=cyan>--- Initializing with Sci-Fi Theme Factory ---</color>");
                break;
            default:
                Debug.LogError("Unknown game theme selected in the Inspector!");
                break;
        }
    }

    /// <summary>
    /// Uses the initialized factory to create a family of game elements
    /// and demonstrates their interactions.
    /// The client doesn't know if it's creating Orcs or Robots,
    /// just that it's creating an IEnemy, IObstacle, and ICollectible.
    /// This method is entirely decoupled from the concrete product types.
    /// </summary>
    private void CreateAndInteractWithGameElements()
    {
        if (_factory == null)
        {
            Debug.LogError("Factory not initialized. Cannot create game elements. Check your _selectedTheme.");
            return;
        }

        Debug.Log("\n<color=yellow>--- Creating Game Elements using the factory ---</color>");

        // The client uses the abstract factory to create abstract products.
        // It does not depend on concrete product classes.
        IEnemy enemy = _factory.CreateEnemy();
        IObstacle obstacle = _factory.CreateObstacle();
        ICollectible collectible = _factory.CreateCollectible();

        Debug.Log($"Successfully created: {enemy.GetName()}, {obstacle.GetName()}, {collectible.GetName()}");

        Debug.Log("\n<color=yellow>--- Interacting with created Game Elements ---</color>");

        // The client interacts with the products through their abstract interfaces.
        enemy.Attack();
        obstacle.Interact();
        collectible.Collect();

        Debug.Log("\n<color=lime>--- Abstract Factory demonstration complete for this theme ---</color>");
    }

    // ========================================================================
    // Example Usage in Comments: How you would integrate this pattern
    // dynamically or in other parts of your game logic.
    // ========================================================================
    /*
    /// <summary>
    /// Example: A public method to dynamically change the game theme at runtime.
    /// This demonstrates the flexibility of the Abstract Factory.
    /// </summary>
    /// <param name="newTheme">The new theme to switch to.</param>
    public void SwitchGameTheme(GameTheme newTheme)
    {
        if (_selectedTheme == newTheme)
        {
            Debug.Log($"Theme is already {newTheme}. No change needed.");
            return;
        }

        _selectedTheme = newTheme;
        Debug.Log($"\n<color=orange>--- Switching Theme to: {newTheme} ---</color>");
        InitializeFactory(); // Re-initialize the factory with the new theme
        CreateAndInteractWithGameElements(); // Re-demonstrate with the new theme
        // In a real game, you would likely trigger a level reload, object pooling reset,
        // or dynamic world generation here, all using the new _factory.
    }

    /// <summary>
    /// Example: Spawning an individual enemy dynamically using the current factory.
    /// </summary>
    public void SpawnDynamicEnemy()
    {
        if (_factory != null)
        {
            IEnemy newEnemy = _factory.CreateEnemy(); // Create an enemy based on the current theme
            Debug.Log($"<color=magenta>Dynamically spawned a {newEnemy.GetName()}!</color>");
            newEnemy.Attack();
            // In a real game, you would typically instantiate a GameObject here,
            // perhaps using a prefab reference stored within the concrete product,
            // or passed to the factory during its construction.
            // e.g., GameObject enemyInstance = Instantiate(newEnemy.GetPrefab(), spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Factory not initialized. Cannot spawn enemy.");
        }
    }
    */
}

#endregion

// ============================================================================
// DETAILED EXPLANATION OF THE ABSTRACT FACTORY PATTERN IN THIS EXAMPLE:
// ============================================================================
/*
The **Abstract Factory** design pattern provides an interface for creating families
of related or dependent objects without specifying their concrete classes.
It's particularly useful when:

1.  **The system needs to be independent of how its products are created, composed, and represented.**
2.  **The system needs to be configured with one of multiple families of products.** (e.g., Fantasy vs. Sci-Fi themes)
3.  **A family of related product objects is designed to be used together, and you need to enforce this constraint.** (e.g., all Fantasy elements must be created together, not a mix of Fantasy and Sci-Fi)
4.  **You want to provide a library of products, and you only want to reveal their interfaces, not their implementations.**

**Key Components in this Example:**

1.  **Abstract Products (`IEnemy`, `IObstacle`, `ICollectible`):**
    *   These are interfaces (or abstract classes) that declare a common interface
        for a type of product. They define *what* a product can do.
    *   In our example, `IEnemy` defines an `Attack()` method, `IObstacle` defines an
        `Interact()` method, and `ICollectible` defines a `Collect()` method.
    *   The client code will primarily interact with these abstract interfaces.

2.  **Concrete Products (`OrcEnemy`, `RobotEnemy`, `TreeObstacle`, `LaserFenceObstacle`, `GoldCoinCollectible`, `EnergyCellCollectible`):**
    *   These are the actual implementations of the abstract product interfaces.
    *   They belong to a specific "family" or "theme."
    *   `OrcEnemy`, `TreeObstacle`, and `GoldCoinCollectible` form the **"Fantasy" family**.
    *   `RobotEnemy`, `LaserFenceObstacle`, and `EnergyCellCollectible` form the **"Sci-Fi" family**.
    *   Each concrete product provides its unique implementation for the methods declared
        in its corresponding abstract product interface (e.g., `OrcEnemy.Attack()` logs a different message than `RobotEnemy.Attack()`).

3.  **Abstract Factory (`IGameElementFactory`):**
    *   This is an interface (or abstract class) that declares a set of creation
        methods, one for each abstract product.
    *   `IGameElementFactory` defines methods like `CreateEnemy()`,
        `CreateObstacle()`, and `CreateCollectible()`.
    *   Crucially, this interface ensures that any concrete factory implementing it
        will be able to produce a *complete family* of related products. The return
        types are the abstract product interfaces (e.g., `IEnemy`), not concrete classes.

4.  **Concrete Factories (`FantasyGameElementFactory`, `SciFiGameElementFactory`):**
    *   These are concrete implementations of the abstract factory interface.
    *   Each concrete factory is responsible for creating a *family* of related
        concrete products.
    *   `FantasyGameElementFactory` implements `CreateEnemy()` to return an `OrcEnemy`,
        `CreateObstacle()` to return a `TreeObstacle`, etc., ensuring all products
        are from the Fantasy theme.
    *   `SciFiGameElementFactory` implements the same creation methods but returns
        `RobotEnemy`, `LaserFenceObstacle`, etc., ensuring all products are from the Sci-Fi theme.

5.  **Client (`AbstractFactoryClient` - a MonoBehaviour):**
    *   This is the part of your application that uses the factories and products.
    *   The `AbstractFactoryClient` holds a reference to an `IGameElementFactory`
        (the abstract factory).
    *   **The client only interacts with the abstract factory and abstract products.**
        It does not know about `OrcEnemy` or `RobotEnemy` directly; it only knows
        about `IEnemy`.
    *   This **decoupling** is the core benefit: you can change the entire family of
        products the client uses simply by providing a different concrete factory
        to the client (e.g., `FantasyGameElementFactory` vs. `SciFiGameElementFactory`),
        without modifying the client's code that uses the products.
    *   In our example, based on the `_selectedTheme` in the Inspector, the client
        instantiates either `FantasyGameElementFactory` or `SciFiGameElementFactory`.
        From that point on, it uses the generic `_factory` interface to create
        elements, completely unaware of their concrete types.

**Benefits for Unity Development:**

*   **Theme/Style Switching:** Easily switch entire sets of game assets and logic
    (e.g., between fantasy, sci-fi, cartoon, realistic themes) without altering core
    gameplay code. You just swap out one factory for another.
*   **Platform Specifics:** Create different UI elements, network managers, or
    input handlers for different platforms (PC, Mobile, Console) by providing
    platform-specific factories.
*   **Difficulty Levels:** Generate different types of enemies, power-ups, or
    environment challenges based on difficulty settings using corresponding factories.
*   **Maintainability and Extensibility:** When you need to add a new theme (e.g.,
    "Post-Apocalyptic"), you only need to create new concrete product classes and
    a new concrete factory. The existing client code remains untouched, adhering
    to the Open/Closed Principle.
*   **Decoupling:** The client code is independent of concrete product implementations,
    making the system more flexible, easier to test, and reducing dependencies.
*   **Ensures Consistency:** It guarantees that the client always uses products from a
    single, consistent family.
*/
```