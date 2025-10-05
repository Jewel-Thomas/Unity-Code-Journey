// Unity Design Pattern Example: FlockingBehavior
// This script demonstrates the FlockingBehavior pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive example demonstrates the **FlockingBehavior design pattern** in Unity using C#. It consists of two main scripts: `FlockingManager` (the controller for the entire flock) and `Boid` (the individual agent that follows the flocking rules).

**FlockingBehavior Design Pattern Explained:**

The Flocking Behavior pattern, often credited to Craig Reynolds' "Boids" algorithm, is an example of **emergent behavior**. It achieves complex, group-level behavior (like a flock of birds or school of fish) by having many individual agents follow a few simple, local rules.

The core rules, implemented in the `Boid` script, are:

1.  **Separation:** Steer to avoid crowding local flockmates. (Don't bump into your neighbors).
2.  **Alignment:** Steer towards the average heading of local flockmates. (Match the direction of your neighbors).
3.  **Cohesion:** Steer to move towards the average position (center of mass) of local flockmates. (Move towards the center of your neighbors).

In addition to these core rules, this practical example includes:

4.  **Goal Seeking:** Steer towards a global target (e.g., a point of interest).
5.  **Boundary Avoidance:** Stay within a defined area.
6.  **Obstacle Avoidance:** Avoid collisions with static objects in the environment.

These combined rules create a dynamic, lifelike flocking simulation.

---

### 1. `FlockingManager.cs`

This script manages the overall flock. It's responsible for:
*   Instantiating the Boid agents.
*   Holding global parameters for flocking behavior (weights, radii, speeds).
*   Providing a list of all Boids for individual Boids to find their neighbors.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<T>

/// <summary>
/// Manages the flock of Boids.
/// This script is responsible for creating Boids and holding global flocking parameters.
/// It acts as the central configuration point for the flocking behavior.
/// </summary>
public class FlockingManager : MonoBehaviour
{
    [Header("Boid Prefab and Instantiation")]
    [Tooltip("The Boid GameObject prefab to instantiate. It must have a 'Boid' component attached.")]
    public GameObject boidPrefab;
    [Tooltip("The number of Boids to create in the flock.")]
    [Range(1, 500)]
    public int numBoids = 50;
    [Tooltip("The radius within which Boids will be randomly spawned around the manager's position.")]
    public float spawnRadius = 20.0f;

    [Header("Flocking Rules Weights")]
    [Tooltip("The weight applied to the Separation rule. Higher value means more avoidance of close neighbors.")]
    [Range(0.1f, 10.0f)]
    public float separationWeight = 1.5f;
    [Tooltip("The weight applied to the Alignment rule. Higher value means stronger tendency to match neighbor headings.")]
    [Range(0.1f, 10.0f)]
    public float alignmentWeight = 1.0f;
    [Tooltip("The weight applied to the Cohesion rule. Higher value means stronger tendency to move towards the center of neighbors.")]
    [Range(0.1f, 10.0f)]
    public float cohesionWeight = 1.0f;

    [Header("Global Boid Parameters")]
    [Tooltip("The maximum speed a Boid can achieve.")]
    public float maxSpeed = 5.0f;
    [Tooltip("The speed at which Boids can turn and adjust their heading.")]
    public float rotationSpeed = 4.0f;
    [Tooltip("The radius within which a Boid considers other Boids as neighbors for flocking calculations.")]
    public float neighborRadius = 3.0f;
    [Tooltip("The minimum distance a Boid tries to maintain from other Boids to avoid crowding.")]
    public float separationDistance = 1.0f;

    [Header("Goal Seeking")]
    [Tooltip("The global Transform target Boids will try to move towards.")]
    public Transform flockGoal;
    [Tooltip("The weight applied to the Goal Seeking rule. Higher value means stronger pull towards the goal.")]
    [Range(0.0f, 5.0f)]
    public float goalWeight = 1.0f;

    [Header("Boundary Avoidance")]
    [Tooltip("Enable or disable boundary avoidance.")]
    public bool useBounds = true;
    [Tooltip("The center of the flocking boundaries (a box).")]
    public Vector3 flockBoundsCenter = Vector3.zero;
    [Tooltip("The size of the flocking boundaries (a box).")]
    public Vector3 flockBoundsSize = new Vector3(50, 50, 50);
    [Tooltip("The distance from the boundary at which Boids start to turn back.")]
    public float boundaryAvoidanceDistance = 10.0f;
    [Tooltip("The weight applied to the Boundary Avoidance rule. Higher value means stronger tendency to stay within bounds.")]
    [Range(0.0f, 5.0f)]
    public float boundaryWeight = 2.0f;

    [Header("Obstacle Avoidance")]
    [Tooltip("Enable or disable obstacle avoidance.")]
    public bool useObstacleAvoidance = true;
    [Tooltip("The distance ahead a Boid checks for obstacles.")]
    public float obstacleAvoidanceDistance = 5.0f;
    [Tooltip("The weight applied to the Obstacle Avoidance rule. Higher value means stronger avoidance of obstacles.")]
    [Range(0.0f, 5.0f)]
    public float obstacleAvoidanceWeight = 3.0f;
    [Tooltip("The layer mask that defines which layers are considered obstacles.")]
    public LayerMask obstacleLayerMask;

    // A list to keep track of all Boids in the flock.
    // This allows individual Boids to easily find their neighbors by iterating this list.
    [HideInInspector] // Hidden in inspector, but public for Boids to access
    public List<Boid> allBoids;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the flock by instantiating Boids and setting up the flock goal.
    /// </summary>
    void Awake()
    {
        allBoids = new List<Boid>();

        // Create a parent GameObject for cleaner hierarchy in the scene
        GameObject boidParent = new GameObject("Boids");
        boidParent.transform.SetParent(this.transform);

        for (int i = 0; i < numBoids; i++)
        {
            // Spawn Boids randomly within a sphere around the manager's position.
            Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
            GameObject boidGO = Instantiate(boidPrefab, spawnPosition, Random.rotation, boidParent.transform);
            Boid boid = boidGO.GetComponent<Boid>();

            if (boid == null)
            {
                Debug.LogError("Boid prefab does not have a Boid component attached! Please add one.", boidPrefab);
                Destroy(boidGO);
                continue;
            }

            boid.manager = this; // Assign this manager instance to the Boid
            allBoids.Add(boid); // Add the new Boid to the global list
        }

        // If no flock goal is explicitly set in the inspector, create a default one.
        if (flockGoal == null)
        {
            GameObject goalGO = new GameObject("FlockGoal");
            goalGO.transform.position = transform.position + Vector3.forward * 20f; // Place it ahead of the manager
            flockGoal = goalGO.transform;
            Debug.LogWarning("No 'Flock Goal' Transform assigned. A default one has been created at " + goalGO.transform.position + ". You can move it in the scene view to guide the flock.", goalGO);
        }
    }

    /// <summary>
    /// Draws gizmos in the editor to visualize spawn radius and flock boundaries.
    /// These are visible when the FlockingManager GameObject is selected in the editor.
    /// </summary>
    void OnDrawGizmos()
    {
        // Visualize spawn radius
        Gizmos.color = new Color(0, 1, 0, 0.2f); // Green transparent
        Gizmos.DrawSphere(transform.position, spawnRadius);

        // Visualize flock boundaries
        if (useBounds)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange transparent
            Gizmos.DrawWireCube(flockBoundsCenter, flockBoundsSize);
        }

        // Visualize flock goal
        if (flockGoal != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(flockGoal.position, 1f);
            Gizmos.DrawWireSphere(flockGoal.position, 1.5f);
        }
    }
}
```

---

### 2. `Boid.cs`

This script defines the behavior of an individual Boid. Each Boid calculates its movement based on the global rules provided by the `FlockingManager` and the positions/velocities of its local neighbors.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<T>

/// <summary>
/// Represents an individual Boid (flock member).
/// This script calculates and applies movement based on the core flocking rules
/// (Separation, Alignment, Cohesion), plus Goal Seeking, Boundary Avoidance,
/// and Obstacle Avoidance.
/// </summary>
public class Boid : MonoBehaviour
{
    // Reference to the FlockingManager to access global settings and the list of all Boids.
    // This is assigned by the FlockingManager when the Boid is created.
    [HideInInspector]
    public FlockingManager manager;

    // The current movement vector of this Boid.
    // [SerializeField] makes it visible in the inspector for debugging purposes.
    [SerializeField]
    private Vector3 currentVelocity;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the Boid's starting velocity randomly.
    /// </summary>
    void Start()
    {
        // Give an initial random velocity to prevent all Boids from starting static
        // and to encourage initial dispersion.
        currentVelocity = Random.insideUnitSphere * manager.maxSpeed;
    }

    /// <summary>
    /// Called once per frame.
    /// Calculates the Boid's new velocity based on all applicable flocking rules and applies it.
    /// </summary>
    void Update()
    {
        // Initialize forces for each rule. These will be calculated and then combined.
        Vector3 separationForce = Vector3.zero;
        Vector3 alignmentForce = Vector3.zero;
        Vector3 cohesionForce = Vector3.zero;
        Vector3 goalForce = Vector3.zero;
        Vector3 boundaryForce = Vector3.zero;
        Vector3 obstacleAvoidanceForce = Vector3.zero;

        int numNeighbors = 0;
        Vector3 centerOfNeighbors = Vector3.zero; // For Cohesion
        Vector3 averageHeading = Vector3.zero;    // For Alignment

        // --- Core Flocking Rules: Separation, Alignment, Cohesion ---
        // Iterate through all other Boids in the flock to find neighbors and calculate forces.
        // NOTE: This O(N^2) approach (N = number of boids) is simple for demonstration.
        // For very large flocks (hundreds or thousands), consider spatial partitioning
        // (e.g., Unity Physics.OverlapSphere, a custom grid/octree) for better performance.
        foreach (Boid otherBoid in manager.allBoids)
        {
            if (otherBoid == this) continue; // A Boid cannot be its own neighbor

            float distance = Vector3.Distance(transform.position, otherBoid.transform.position);

            // Check if 'otherBoid' is within the neighbor radius defined by the manager.
            if (distance < manager.neighborRadius)
            {
                numNeighbors++;
                centerOfNeighbors += otherBoid.transform.position; // Accumulate positions for Cohesion
                averageHeading += otherBoid.currentVelocity;       // Accumulate velocities for Alignment

                // 1. Separation Rule: Steer to avoid crowding local flockmates.
                // If another boid is too close (within separationDistance), calculate a repulsion vector.
                if (distance < manager.separationDistance)
                {
                    Vector3 awayFromBoid = transform.position - otherBoid.transform.position;
                    // Normalize and scale inversely with distance: closer boids repel more strongly.
                    separationForce += awayFromBoid.normalized / distance;
                }
            }
        }

        // If neighbors were found, calculate the average for Cohesion and Alignment.
        if (numNeighbors > 0)
        {
            // 2. Cohesion Rule: Steer to move towards the average position (center of mass) of local flockmates.
            centerOfNeighbors /= numNeighbors;
            cohesionForce = (centerOfNeighbors - transform.position).normalized; // Vector towards center

            // 3. Alignment Rule: Steer towards the average heading of local flockmates.
            averageHeading /= numNeighbors;
            alignmentForce = averageHeading.normalized; // Vector towards average direction
        }

        // --- Additional Practical Rules ---

        // 4. Goal Seeking Rule: Steer towards the global flock goal.
        if (manager.flockGoal != null)
        {
            goalForce = (manager.flockGoal.position - transform.position).normalized;
        }

        // 5. Boundary Avoidance Rule: Stay within defined boundaries.
        if (manager.useBounds)
        {
            boundaryForce = CalculateBoundaryAvoidanceForce();
        }

        // 6. Obstacle Avoidance Rule: Avoid collisions with static obstacles.
        if (manager.useObstacleAvoidance)
        {
            obstacleAvoidanceForce = CalculateObstacleAvoidanceForce();
        }

        // --- Combine All Forces ---
        Vector3 totalForce = Vector3.zero;
        totalForce += separationForce * manager.separationWeight;
        totalForce += alignmentForce * manager.alignmentWeight;
        totalForce += cohesionForce * manager.cohesionWeight;
        totalForce += goalForce * manager.goalWeight;
        totalForce += boundaryForce * manager.boundaryWeight;
        totalForce += obstacleAvoidanceForce * manager.obstacleAvoidanceWeight;

        // --- Apply Movement ---
        // Update velocity: Smoothly interpolate current velocity towards the new desired direction
        // (the direction of the total combined force).
        currentVelocity = Vector3.Lerp(currentVelocity, totalForce.normalized * manager.maxSpeed, manager.rotationSpeed * Time.deltaTime);

        // Ensure velocity doesn't exceed the maximum speed.
        currentVelocity = Vector3.ClampMagnitude(currentVelocity, manager.maxSpeed);

        // Apply the calculated velocity to the Boid's position.
        transform.position += currentVelocity * Time.deltaTime;

        // Orient the Boid to face its direction of movement for visual realism.
        if (currentVelocity != Vector3.zero) // Avoid looking at (0,0,0) if velocity is zero
        {
            transform.forward = Vector3.Slerp(transform.forward, currentVelocity.normalized, manager.rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Calculates a force to steer the Boid back towards the defined flock boundaries.
    /// The force pushes the Boid inwards if it gets too close to the boundary edge.
    /// </summary>
    /// <returns>A normalized vector force pushing the Boid away from the boundary.</returns>
    private Vector3 CalculateBoundaryAvoidanceForce()
    {
        Vector3 avoidanceVector = Vector3.zero;
        // Calculate the min and max corners of the bounding box.
        Vector3 boundsMin = manager.flockBoundsCenter - manager.flockBoundsSize / 2f;
        Vector3 boundsMax = manager.flockBoundsCenter + manager.flockBoundsSize / 2f;

        // Check distance to each boundary plane. If too close, apply a force to turn inwards.
        if (transform.position.x < boundsMin.x + manager.boundaryAvoidanceDistance)
            avoidanceVector.x = 1; // Turn right
        else if (transform.position.x > boundsMax.x - manager.boundaryAvoidanceDistance)
            avoidanceVector.x = -1; // Turn left

        if (transform.position.y < boundsMin.y + manager.boundaryAvoidanceDistance)
            avoidanceVector.y = 1; // Turn up
        else if (transform.position.y > boundsMax.y - manager.boundaryAvoidanceDistance)
            avoidanceVector.y = -1; // Turn down

        if (transform.position.z < boundsMin.z + manager.boundaryAvoidanceDistance)
            avoidanceVector.z = 1; // Turn forward
        else if (transform.position.z > boundsMax.z - manager.boundaryAvoidanceDistance)
            avoidanceVector.z = -1; // Turn backward

        return avoidanceVector.normalized; // Return normalized direction
    }

    /// <summary>
    /// Calculates a force to steer the Boid away from obstacles using raycasting.
    /// Fires rays ahead of the Boid to detect obstacles and generates a repulsion force.
    /// </summary>
    /// <returns>A normalized vector force pushing the Boid away from detected obstacles.</returns>
    private Vector3 CalculateObstacleAvoidanceForce()
    {
        Vector3 avoidanceForce = Vector3.zero;
        RaycastHit hit;

        // Define multiple ray directions for more robust obstacle detection:
        // 1. Straight forward
        // 2. Slightly to the right
        // 3. Slightly to the left
        // This helps the Boid anticipate and steer around obstacles more smoothly.
        Vector3[] rayDirections = new Vector3[]
        {
            transform.forward,
            (transform.forward + transform.right * 0.5f).normalized,
            (transform.forward - transform.right * 0.5f).normalized
        };

        foreach (Vector3 dir in rayDirections)
        {
            // Cast a ray in the specified direction up to obstacleAvoidanceDistance.
            // Only hit objects on the manager's obstacleLayerMask.
            if (Physics.Raycast(transform.position, dir, out hit, manager.obstacleAvoidanceDistance, manager.obstacleLayerMask))
            {
                // If an obstacle is detected, generate a force to steer away.
                // Vector3.Reflect turns the current forward vector away from the hit surface normal,
                // providing a simple but effective avoidance direction.
                avoidanceForce += Vector3.Reflect(transform.forward, hit.normal);
            }
        }
        return avoidanceForce.normalized; // Return normalized combined avoidance direction
    }

    /// <summary>
    /// Draws gizmos in the editor to visualize Boid-specific parameters.
    /// These are visible when the individual Boid GameObject is selected in the editor.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (manager == null) return; // Ensure manager is assigned before drawing gizmos

        // Draw neighbor radius (blue wire sphere)
        Gizmos.color = new Color(0, 0, 1, 0.1f);
        Gizmos.DrawWireSphere(transform.position, manager.neighborRadius);

        // Draw separation distance (red wire sphere)
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, manager.separationDistance);

        // Draw obstacle avoidance rays (magenta rays)
        if (manager.useObstacleAvoidance)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, transform.forward * manager.obstacleAvoidanceDistance);
            Gizmos.DrawRay(transform.position, (transform.forward + transform.right * 0.5f).normalized * manager.obstacleAvoidanceDistance);
            Gizmos.DrawRay(transform.position, (transform.forward - transform.right * 0.5f).normalized * manager.obstacleAvoidanceDistance);
        }
    }
}
```

---

### How to Use This Flocking Behavior Example in Unity:

Follow these steps to set up and run the flocking simulation in your Unity project:

1.  **Create an Empty GameObject for the Flocking Manager:**
    *   In your Unity scene, create an empty GameObject (`GameObject -> Create Empty`).
    *   Rename it to something descriptive, like "FlockingManager".
    *   Attach the `FlockingManager.cs` script to this GameObject.

2.  **Create a Boid Prefab:**
    *   Create a simple 3D object to represent a single Boid. For example: `GameObject -> 3D Object -> Capsule`.
    *   Scale it down to a small, appropriate size (e.g., X:0.2, Y:0.2, Z:0.5).
    *   **Crucial:** Ensure the Boid prefab's **forward axis (Z-axis)** points in the direction you want it to move. For a default Capsule, its height is along Y. You might need to rotate it by 90 degrees on the X-axis (`Rotation X: 90`) so its 'nose' points along Z.
    *   Add a `Rigidbody` component to the Boid prefab.
        *   Set its `Is Kinematic` property to `true` (we're manually controlling movement via script).
        *   Disable gravity by unchecking `Use Gravity`.
    *   Add a `Collider` component (e.g., `Capsule Collider`) to the Boid. Ensure it's not marked as a trigger.
    *   Attach the `Boid.cs` script to this Boid GameObject.
    *   Drag this configured Boid GameObject from the Hierarchy into your Project panel to create a Prefab (e.g., rename it "BoidPrefab" in the Project panel).
    *   Delete the original Boid GameObject from your scene hierarchy (you only need the prefab).

3.  **Configure the FlockingManager:**
    *   Select the "FlockingManager" GameObject in your Hierarchy.
    *   In its Inspector panel, drag your "BoidPrefab" from the Project panel into the `Boid Prefab` slot on the `FlockingManager` component.
    *   Adjust the `Num Boids` property to control how many Boids are in the flock (start with 50-100 for a good visual).
    *   **Flock Goal:**
        *   Create another Empty GameObject (`GameObject -> Create Empty`), name it "FlockGoal", and position it somewhere in your scene. This is where the flock will try to move towards.
        *   Drag this "FlockGoal" GameObject from the Hierarchy into the `Flock Goal` slot on the `FlockingManager` component.
        *   **Tip:** While in Play Mode, you can move the "FlockGoal" in the scene view to see the flock actively follow it!
    *   **Obstacle Avoidance Setup:**
        *   Ensure your "BoidPrefab" (and thus all instantiated Boids) has a Layer assigned to it that is *not* in the `Obstacle Layer Mask` (e.g., create a new layer named "Boid" and assign it).
        *   Create some static obstacles in your scene (e.g., `GameObject -> 3D Object -> Cube`).
        *   Assign a specific Layer to these obstacle objects (e.g., `Layers -> Add Layer...`, create a new layer named "Obstacle", then assign it to your cubes).
        *   In the `FlockingManager` component's `Obstacle Layer Mask` dropdown, select the "Obstacle" layer you just created. This tells Boids which objects to avoid.

4.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   You should now see a flock of Boids moving around, demonstrating the emergent behavior defined by the flocking rules.
    *   **Experiment:** While the scene is running, select the "FlockingManager" GameObject and adjust the various weights and parameters in its Inspector. Observe how changes to `Separation Weight`, `Alignment Weight`, `Cohesion Weight`, `Max Speed`, `Neighbor Radius`, etc., dynamically affect the flock's behavior in real-time. Move the "FlockGoal" around to guide the flock.

Enjoy building your intelligent swarms and learning about design patterns!