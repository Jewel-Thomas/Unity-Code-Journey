// Unity Design Pattern Example: Strategy
// This script demonstrates the Strategy pattern in Unity
// Generated automatically - ready to use in your Unity project

Here is an example of the Strategy pattern implemented in C# for Unity:

```csharp
using System.Collections;
using UnityEngine;

// The strategy interface defines the abstract operations that 
// are common to all concrete strategies.
public interface IEnemyBehavior
{
    void Execute();
}

// Concrete strategy A
public class NormalAttack : IEnemyBehavior
{
    public void Execute()
    {
        Debug.Log("Normal attack");
    }
}

// Concrete strategy B
public class SpecialAttack : IEnemyBehavior
{
    public void Execute()
    {
        Debug.Log("Special attack");
    }
}

// The context interface declares common interface for 
// an operation and defines a method for executing the operation.
public interface IEnemy
{
    void SetBehavior(IEnemyBehavior behavior);
    void Attack();
}

// A concrete strategy implements one of the strategies.
public class Enemy : MonoBehaviour, IEnemy
{
    private IEnemyBehavior behavior;

    public void Start()
    {
        // Initialize with a default strategy
        this.behavior = new NormalAttack();
    }

    public void SetBehavior(IEnemyBehavior behavior)
    {
        this.behavior = behavior;
    }

    public void Attack()
    {
        behavior.Execute();
    }
}

// The client code does not know which concrete 
// strategy to use. It should be able to work with all of them.
public class EnemyController : MonoBehaviour
{
    private IEnemy enemy;

    public void Start()
    {
        // Create an instance of the context and set a default strategy.
        this.enemy = new Enemy();
        enemy.SetBehavior(new NormalAttack());

        // Demonstrate how to switch between strategies.
        enemy.Attack(); // Outputs: Normal attack

        // Switch to special attack
        enemy.SetBehavior(new SpecialAttack());
        enemy.Attack(); // Outputs: Special attack
    }
}
```

This script demonstrates the Strategy pattern in a simple Unity game where enemies can have different behaviors. You can easily switch between these behaviors by setting a new strategy for an enemy.

1.  **The Strategy Interface (`IEnemyBehavior`):** This interface defines the abstract operations that are common to all concrete strategies.
2.  **Concrete Strategies (`NormalAttack`, `SpecialAttack`):** These classes implement one of the strategies. They must provide an implementation for the `Execute()` method defined in the strategy interface.
3.  **The Context Interface (`IEnemy`):** This interface declares a common interface for an operation and defines a method for executing the operation. It also has a method to set the behavior.
4.  **Concrete Context (`Enemy`):** This class implements the context. It sets the default strategy when it starts, and then allows you to change the strategy using the `SetBehavior()` method.
5.  **The Client Code (`EnemyController`):** The client code does not know which concrete strategy to use. It can work with all of them. It creates an instance of the context, sets a default strategy, and demonstrates how to switch between strategies.

In this example, the `Enemy` class is the context that uses different strategies (normal attack and special attack) depending on the situation. The `EnemyController` class demonstrates how to change the strategy by calling the `SetBehavior()` method on the `Enemy` object.