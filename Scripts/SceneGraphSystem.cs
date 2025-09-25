// Unity Design Pattern Example: SceneGraphSystem
// This script demonstrates the SceneGraphSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Scene Graph System is a fundamental design pattern in computer graphics, used to organize and manage the logical and spatial representation of a graphical scene. In Unity, the engine itself provides a built-in scene graph (GameObjects with their Transform components forming a hierarchy). However, understanding and implementing your *own* scene graph system can be invaluable for:

*   **Procedural Content Generation:** Dynamically creating complex scenes from data, rather than manually placing GameObjects.
*   **Data-Driven Design:** Decoupling your scene's logical structure from Unity's physical GameObjects, allowing you to load scenes from custom formats.
*   **Custom Rendering Pipelines:** If you're implementing a custom renderer, you'll need your own scene graph to organize renderable objects.
*   **Performance Optimizations:** Implementing specialized culling, LOD, or batching systems that operate on a simplified or optimized scene graph structure before materializing or sending data to Unity.
*   **Architectural Clarity:** Separating the "model" of your scene (the custom graph) from its "view" (Unity GameObjects).

This example demonstrates how to create a simple, abstract scene graph in C# and then "materialize" (create) corresponding Unity GameObjects from it, keeping the internal logic of the custom graph separate from Unity's runtime representation.

---

### **SceneGraphSystemExample.cs**

To use this, create a new C# script named `SceneGraphSystemExample` in your Unity project, copy the code below into it, and follow the setup instructions in the comments.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Action delegate (though not explicitly used in this final version, good practice)

/// <summary>
/// ISceneNode Interface
/// Defines the common contract for any node in our custom scene graph.
/// This allows us to treat all nodes uniformly when traversing or manipulating the graph.
/// </summary>
public interface ISceneNode
{
    string Name { get; }
    ISceneNode Parent { get; }
    List<ISceneNode> Children { get; }

    Vector3 LocalPosition { get; set; }
    Quaternion LocalRotation { get; set; }
    Vector3 LocalScale { get; set; }

    /// <summary>
    /// Adds a child node to this node. Handles parent/child relationship management.
    /// </summary>
    void AddChild(ISceneNode child);

    /// <summary>
    /// Removes a child node from this node. Handles parent/child relationship management.
    /// </summary>
    void RemoveChild(ISceneNode child);

    /// <summary>
    /// Updates the internal state of the node.
    /// This is where custom logic for a node type would go (e.g., AI, physics, animation rules).
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    void UpdateNode(float deltaTime);

    /// <summary>
    /// Materializes (creates) the corresponding Unity GameObject hierarchy
    /// for this node and its children. This bridges our abstract graph to Unity's concrete one.
    /// </summary>
    /// <param name="parentUnityTransform">The Unity Transform that this node's GameObject
    /// should be parented under.</param>
    /// <returns>The created Unity GameObject for this node.</returns>
    GameObject Materialize(Transform parentUnityTransform);

    /// <summary>
    /// Updates the Unity GameObject's transform to match this node's internal state.
    /// This is crucial for synchronizing abstract changes with the visual representation.
    /// </summary>
    void UpdateUnityTransform();

    /// <summary>
    /// Gets the actual Unity GameObject created for this node during materialization.
    /// </summary>
    /// <returns>The Unity GameObject associated with this node.</returns>
    GameObject GetUnityGameObject();
}

/// <summary>
/// BaseSceneNode Abstract Class
/// Provides a common implementation for ISceneNode, managing hierarchy and
/// basic transform properties in our custom graph. It also handles the creation
/// and management of the corresponding Unity GameObject.
/// </summary>
public abstract class BaseSceneNode : ISceneNode
{
    public string Name { get; private set; }
    public ISceneNode Parent { get; private set; } // Set by AddChild/RemoveChild
    public List<ISceneNode> Children { get; private set; } = new List<ISceneNode>();

    // These represent the local transform of this node within our custom scene graph.
    public Vector3 LocalPosition { get; set; } = Vector3.zero;
    public Quaternion LocalRotation { get; set; } = Quaternion.identity;
    public Vector3 LocalScale { get; set; } = Vector3.one;

    // A reference to the actual Unity GameObject created for this node.
    protected GameObject _unityGameObject;
    public GameObject GetUnityGameObject() => _unityGameObject;

    // Cache the transform component of the Unity GameObject for performance.
    protected Transform _unityTransform;

    public BaseSceneNode(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Adds a child node to this node and manages the parent-child relationship.
    /// If the child already has a parent, it's removed from its previous parent first.
    /// </summary>
    /// <param name="child">The child node to add.</param>
    public void AddChild(ISceneNode child)
    {
        if (child == null || Children.Contains(child)) return;
        
        // If the child already has a parent, remove it from that parent's children list.
        if (child.Parent != null)
        {
            child.Parent.RemoveChild(child);
        }

        Children.Add(child);
        // Explicitly set the parent of the child, requiring a cast as Parent is protected set.
        ((BaseSceneNode)child).Parent = this; 
    }

    /// <summary>
    /// Removes a child node from this node and clears its parent reference.
    /// </summary>
    /// <param name="child">The child node to remove.</param>
    public void RemoveChild(ISceneNode child)
    {
        if (child == null || !Children.Contains(child)) return;

        Children.Remove(child);
        // Clear the parent reference of the removed child.
        ((BaseSceneNode)child).Parent = null; 
    }

    /// <summary>
    /// Default UpdateNode implementation. It recursively calls UpdateNode on all children.
    /// Derived classes can override this to add their specific update logic.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    public virtual void UpdateNode(float deltaTime)
    {
        // Recursively update children
        foreach (var child in Children)
        {
            child.UpdateNode(deltaTime);
        }
    }

    /// <summary>
    /// Materializes this node's Unity GameObject and then recursively materializes its children.
    /// The specific GameObject creation is handled by the abstract CreateUnityGameObject method.
    /// </summary>
    /// <param name="parentUnityTransform">The Unity Transform to parent the new GameObject under.</param>
    /// <returns>The created Unity GameObject.</returns>
    public GameObject Materialize(Transform parentUnityTransform)
    {
        // Step 1: Create the specific Unity GameObject for this node type using a factory method.
        _unityGameObject = CreateUnityGameObject();
        _unityGameObject.name = Name; // Set the Unity GameObject's name
        _unityTransform = _unityGameObject.transform; // Cache its transform

        // Step 2: Apply initial transform from our custom node data.
        _unityTransform.localPosition = LocalPosition;
        _unityTransform.localRotation = LocalRotation;
        _unityTransform.localScale = LocalScale;

        // Step 3: Parent the newly created Unity GameObject under the provided Unity parent transform.
        _unityTransform.SetParent(parentUnityTransform);

        // Step 4: Recursively materialize children under this node's newly created Unity GameObject.
        foreach (var child in Children)
        {
            child.Materialize(_unityTransform);
        }

        return _unityGameObject;
    }

    /// <summary>
    /// Abstract method: Derived classes must implement this to define what kind
    /// of Unity GameObject they represent (e.g., an empty, a mesh, a light).
    /// </summary>
    /// <returns>A newly created Unity GameObject.</returns>
    protected abstract GameObject CreateUnityGameObject();

    /// <summary>
    /// Updates the Unity GameObject's transform to match this node's internal state.
    /// This method is called after our custom nodes' LocalPosition/Rotation/Scale have been modified.
    /// It then recursively calls this on children to ensure the entire Unity hierarchy is synchronized.
    /// </summary>
    public virtual void UpdateUnityTransform()
    {
        if (_unityTransform == null) return; // Only update if materialized

        // Synchronize our internal custom transform data to the Unity GameObject's transform.
        // We do a check to avoid unnecessary assignments, though Unity is optimized for this.
        if (_unityTransform.localPosition != LocalPosition) _unityTransform.localPosition = LocalPosition;
        if (_unityTransform.localRotation != LocalRotation) _unityTransform.localRotation = LocalRotation;
        if (_unityTransform.localScale != LocalScale) _unityTransform.localScale = LocalScale;

        // Recursively update children's Unity transforms to reflect changes in their custom nodes.
        foreach (var child in Children)
        {
            child.UpdateUnityTransform();
        }
    }
}

// --- Concrete SceneNode Implementations ---

/// <summary>
/// GenericSceneNode: Represents a simple transform node in our custom scene graph.
/// It maps to an empty GameObject in Unity, useful for grouping or empty spatial points.
/// </summary>
public class GenericSceneNode : BaseSceneNode
{
    public GenericSceneNode(string name) : base(name) { }

    protected override GameObject CreateUnityGameObject()
    {
        return new GameObject(Name + " (Generic)");
    }
}

/// <summary>
/// MeshSceneNode: Represents a mesh-based object in our custom scene graph.
/// It maps to a Unity GameObject with a MeshFilter and MeshRenderer components.
/// </summary>
public class MeshSceneNode : BaseSceneNode
{
    public Mesh MeshData { get; set; }
    public Material MaterialData { get; set; }

    public MeshSceneNode(string name, Mesh mesh, Material material) : base(name)
    {
        MeshData = mesh;
        MaterialData = material;
    }

    protected override GameObject CreateUnityGameObject()
    {
        GameObject go = new GameObject(Name + " (Mesh)");
        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

        meshFilter.mesh = MeshData;
        meshRenderer.material = MaterialData;

        return go;
    }
}

/// <summary>
/// LightSceneNode: Represents a light source in our custom scene graph.
/// It maps to a Unity GameObject with a Light component.
/// </summary>
public class LightSceneNode : BaseSceneNode
{
    public LightType LightTypeData { get; set; } = LightType.Point;
    public Color ColorData { get; set; } = Color.white;
    public float IntensityData { get; set; } = 1f;

    public LightSceneNode(string name, LightType type, Color color, float intensity) : base(name)
    {
        LightTypeData = type;
        ColorData = color;
        IntensityData = intensity;
    }

    protected override GameObject CreateUnityGameObject()
    {
        GameObject go = new GameObject(Name + " (Light)");
        Light light = go.AddComponent<Light>();
        light.type = LightTypeData;
        light.color = ColorData;
        light.intensity = IntensityData;
        return go;
    }
}


/// <summary>
/// SceneGraphSystemExample (MonoBehaviour)
/// This is the "System" that manages our custom scene graph. It handles:
/// 1. Building the abstract scene graph hierarchy.
/// 2. Materializing the abstract graph into concrete Unity GameObjects.
/// 3. Updating the internal state of the abstract nodes.
/// 4. Synchronizing these changes back to the Unity GameObjects.
/// </summary>
public class SceneGraphSystemExample : MonoBehaviour
{
    // Serialized fields to assign assets in the Unity Editor for our example nodes.
    // These allow the example to use real Unity assets.
    [Header("Node Assets (for example)")]
    [SerializeField] private Mesh _exampleCubeMesh;
    [SerializeField] private Material _exampleRedMaterial;
    [SerializeField] private Material _exampleGreenMaterial;

    // The root of our custom, abstract scene graph.
    private ISceneNode _rootNode;

    void Start()
    {
        // --- Pre-checks for Editor setup ---
        if (_exampleCubeMesh == null)
        {
            Debug.LogError("Assign a Cube Mesh to SceneGraphSystemExample script in the Inspector. Disabling example.");
            enabled = false;
            return;
        }
        if (_exampleRedMaterial == null || _exampleGreenMaterial == null)
        {
            Debug.LogError("Assign Red and Green Materials to SceneGraphSystemExample script in the Inspector. Disabling example.");
            enabled = false;
            return;
        }

        Debug.Log("SceneGraphSystemExample: Building custom scene graph...");

        // --- Step 1: Define the Root Node of our Custom Scene Graph ---
        // This node acts as the entry point for our abstract hierarchy.
        // Its corresponding Unity GameObject will be parented under the GameObject
        // this MonoBehaviour is attached to (e.g., "SceneGraphManager").
        _rootNode = new GenericSceneNode("SceneRoot");
        _rootNode.LocalPosition = new Vector3(0, 0, 0); // Position relative to its Unity parent

        // --- Step 2: Build the Scene Graph Hierarchy (Abstract Level) ---
        // We create child nodes and establish parent-child relationships within our
        // custom graph. At this stage, no Unity GameObjects are created yet.
        // This hierarchy defines the logical structure and spatial relationships.

        // A. Main Group Node (a generic grouping node)
        GenericSceneNode mainGroup = new GenericSceneNode("MainGroup");
        mainGroup.LocalPosition = new Vector3(0, 2, 0); // Local position relative to SceneRoot
        _rootNode.AddChild(mainGroup);

        // B. Animated Object Group (another generic group, will be animated)
        GenericSceneNode animatedGroup = new GenericSceneNode("AnimatedGroup");
        animatedGroup.LocalPosition = new Vector3(-2, 0, 0); // Local position relative to MainGroup
        mainGroup.AddChild(animatedGroup);

        //    B1. Animated Cube 1 (a mesh node)
        MeshSceneNode animatedCube1 = new MeshSceneNode("AnimatedCube1", _exampleCubeMesh, _exampleRedMaterial);
        animatedCube1.LocalPosition = new Vector3(-1, 0, 0); // Local position relative to AnimatedGroup
        animatedCube1.LocalScale = Vector3.one * 0.5f;
        animatedGroup.AddChild(animatedCube1);

        //    B2. Animated Cube 2 (a mesh node, child of Animated Cube 1)
        MeshSceneNode animatedCube2 = new MeshSceneNode("AnimatedCube2", _exampleCubeMesh, _exampleGreenMaterial);
        animatedCube2.LocalPosition = new Vector3(1, 0, 0); // Local position relative to AnimatedCube1
        animatedCube2.LocalScale = Vector3.one * 0.7f;
        animatedCube1.AddChild(animatedCube2); // animatedCube2 is a child of animatedCube1

        // C. Static Object Group (another generic group)
        GenericSceneNode staticGroup = new GenericSceneNode("StaticGroup");
        staticGroup.LocalPosition = new Vector3(2, 0, 0); // Local position relative to MainGroup
        mainGroup.AddChild(staticGroup);

        //    C1. Static Cube (a mesh node)
        MeshSceneNode staticCube = new MeshSceneNode("StaticCube", _exampleCubeMesh, _exampleRedMaterial);
        staticCube.LocalPosition = new Vector3(0, 0, 0); // Local position relative to StaticGroup
        staticCube.LocalScale = Vector3.one * 0.8f;
        staticGroup.AddChild(staticCube);

        // D. Light Node (a light source node)
        LightSceneNode sceneLight = new LightSceneNode("SceneLight", LightType.Point, Color.yellow, 1.5f);
        sceneLight.LocalPosition = new Vector3(0, 3, -2); // Local position relative to SceneRoot
        _rootNode.AddChild(sceneLight);


        Debug.Log("SceneGraphSystemExample: Materializing custom scene graph into Unity GameObjects...");

        // --- Step 3: Materialize the Custom Scene Graph into Unity GameObjects ---
        // This is the crucial step where our abstract graph structure is traversed,
        // and corresponding Unity GameObjects (MeshFilter, MeshRenderer, Light components)
        // are created and parented according to our custom graph's hierarchy.
        // The root of our custom graph will be parented under this MonoBehaviour's GameObject.
        _rootNode.Materialize(this.transform);

        Debug.Log("SceneGraphSystemExample: Scene graph materialized successfully.");

        // Example of accessing a node and its created Unity GameObject for verification.
        // This shows how you can still interact with the Unity representation through your custom nodes.
        Debug.Log($"Accessing 'AnimatedCube1' Unity GameObject: {animatedCube1.GetUnityGameObject().name}");
    }

    void Update()
    {
        // --- Step 4: Update and Synchronize the Scene Graph ---
        // In a real system, you might have more complex traversal for culling,
        // physics updates, or specific animations driven by game logic.

        // First, update the internal state of our custom scene graph nodes.
        // For this demonstration, we'll animate 'AnimatedGroup' and 'AnimatedCube2'
        // by modifying their LocalRotation properties in our custom nodes.
        
        // Find the 'AnimatedGroup' node within our custom graph
        ISceneNode animatedGroup = _rootNode.Children.Find(n => n.Name == "MainGroup")?
                                          .Children.Find(n => n.Name == "AnimatedGroup");
        
        if (animatedGroup != null)
        {
            // Rotate the entire 'animatedGroup' node around its local Y axis.
            animatedGroup.LocalRotation *= Quaternion.Euler(0, 30 * Time.deltaTime, 0);

            // Find 'AnimatedCube1' (child of animatedGroup)
            ISceneNode animatedCube1 = animatedGroup.Children.Find(n => n.Name == "AnimatedCube1");
            if (animatedCube1 != null)
            {
                // Find 'AnimatedCube2' (child of AnimatedCube1)
                ISceneNode animatedCube2 = animatedCube1.Children.Find(n => n.Name == "AnimatedCube2");
                if (animatedCube2 != null)
                {
                    // Rotate 'AnimatedCube2' around its local Z axis relative to 'AnimatedCube1'.
                    animatedCube2.LocalRotation *= Quaternion.Euler(0, 0, 60 * Time.deltaTime);
                }
            }
        }

        // After updating the internal state (LocalPosition, LocalRotation, LocalScale)
        // of our custom nodes, we need to synchronize these changes to the
        // corresponding Unity GameObjects. This involves traversing our *custom* graph
        // and updating the *Unity* transforms.
        _rootNode.UpdateUnityTransform();
    }
}


/*
--- How to Use This Example in Unity ---

1.  Create a new Unity project or open an existing one.
2.  Create a new C# script named "SceneGraphSystemExample" (exactly this name).
3.  Copy and paste the entire code above into this new script, overwriting its contents.
4.  In your Unity project's Assets folder, ensure you have:
    a.  **A Cube Mesh:** You can use Unity's default `Cube` mesh (usually found at `Packages/Unity Default Resources/Cube`) or create a new 3D Object (e.g., `GameObject -> 3D Object -> Cube` in a new scene, then drag its Mesh Filter component's `Mesh` property into your Assets folder to save it as an asset).
    b.  **Two Material Assets:** Right-click in the Project window -> `Create -> Material`.
        -   Name one "RedMaterial" and set its Albedo color to red.
        -   Name the other "GreenMaterial" and set its Albedo color to green.
5.  Create an empty GameObject in your scene: Right-click in Hierarchy -> `Create Empty`. Name it "SceneGraphManager".
6.  Attach the `SceneGraphSystemExample` script to the "SceneGraphManager" GameObject.
7.  In the Inspector window for "SceneGraphManager", drag and drop your created assets:
    -   Drag your `Cube` Mesh asset to the `Example Cube Mesh` slot.
    -   Drag your `RedMaterial` asset to the `Example Red Material` slot.
    -   Drag your `GreenMaterial` asset to the `Example Green Material` slot.
8.  Run the scene.

You should observe the following in the Unity Editor:

-   Under the "SceneGraphManager" GameObject in your Hierarchy, a new hierarchy of GameObjects will be created dynamically at runtime.
-   These GameObjects will have names like "SceneRoot (Generic)", "MainGroup (Generic)", "AnimatedCube1 (Mesh)", "AnimatedCube2 (Mesh)", "StaticCube (Mesh)", and "SceneLight (Light)".
-   The "AnimatedGroup" (a generic empty parent) will be rotating around its own axis.
-   "AnimatedCube2" (which is a child of "AnimatedCube1", which is in turn a child of "AnimatedGroup") will also be rotating around its own local Z-axis, demonstrating inherited transformations from its parents.
-   A yellow point light will be present, casting light on the scene.

This example clearly demonstrates the SceneGraphSystem pattern by:

-   **Abstraction:** `ISceneNode` and `BaseSceneNode` define a custom, abstract scene graph structure, separate from Unity's `GameObject` and `Transform`.
-   **Data-Driven:** The scene graph's structure, node types, and initial properties are defined in C# code (or could be loaded from external data), not manually in the Unity Editor.
-   **Separation of Concerns:** The custom graph manages its own hierarchy and transform data. The `Materialize` method acts as a "view creator," bridging this abstract data to Unity's visual representation. `UpdateUnityTransform` keeps the view synchronized with the model.
-   **Flexibility & Extensibility:** You can easily create new `BaseSceneNode` derived classes (e.g., `CameraSceneNode`, `AudioSourceSceneNode`, `ParticleSystemSceneNode`) to represent any Unity component or custom game object behavior within your abstract graph.
-   **Runtime Control:** The `Update` method shows how you can manipulate your abstract nodes' properties (like `LocalRotation`) and then have those changes reflected in the live Unity scene, fully demonstrating the "system" aspect of managing and synchronizing the graph.
*/
```