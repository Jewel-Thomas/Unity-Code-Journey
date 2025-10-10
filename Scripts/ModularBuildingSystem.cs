// Unity Design Pattern Example: ModularBuildingSystem
// This script demonstrates the ModularBuildingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the 'Modular Building System' design pattern. This pattern involves defining individual, interchangeable building modules (like walls, floors, roofs) and using a central manager to assemble, modify, and store them to construct a larger structure.

This script is designed to be directly dropped into a Unity project.

**Key Components of the Modular Building System:**

1.  **Module Definition (`BuildingModule` component):** Each distinct building piece (e.g., a specific wall type, a floor tile) is represented by a prefab with a `BuildingModule` component attached. This component defines the module's category (Floor, Wall, Roof, etc.) and its basic dimensions (footprint), which are crucial for placement logic.
2.  **Module Prefab Association (`ModulePrefab` class):** A serializable class that links a `ModuleCategory` enum value to its corresponding Unity `GameObject` prefab. This allows the `ModularBuildingManager` to easily access and instantiate the correct module type.
3.  **Building Manager (`ModularBuildingManager`):** This is the core controller. It holds references to all available module prefabs, manages the list of instantiated modules in the scene, and provides methods for placing, removing, and clearing modules. The placement logic in this example is simplified for demonstration purposes to build a sequential structure using keyboard inputs.

---

### **ModularBuildingSystem.cs**

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<T>
using System; // Required for [Serializable]

namespace ModularBuildingSystemExample
{
    // 1. Module Definition (Component)
    // This enum defines the categories of building modules.
    // Designers can extend this with more specific types like 'Door', 'Window', 'Pillar', etc.
    public enum ModuleCategory
    {
        None, // Default or placeholder
        Floor,
        Wall,
        Roof
        // Add more categories as needed (e.g., Pillar, Stairs, Window, Door)
    }

    /// <summary>
    /// Represents a single building module. This component should be attached to module prefabs.
    /// It holds data specific to the module, such as its category and physical dimensions.
    /// </summary>
    public class BuildingModule : MonoBehaviour
    {
        [Tooltip("The category this module belongs to (e.g., Floor, Wall, Roof).")]
        public ModuleCategory category = ModuleCategory.None;

        [Tooltip("The approximate dimensions of the module. Used for placement calculations (e.g., grid snapping). " +
                 "Ensure this matches the visual size of your module for accurate placement.")]
        public Vector3 footprintSize = Vector3.one * 2f; // Default to 2x2x2 for a common grid size

        // You could add more properties here, like:
        // public List<Vector3> snapPoints; // For advanced snapping logic
        // public Material materialOverride;
        // public int cost; // For a game economy
        // public bool rotatable;
        // public bool isConnectable;
        // public ModuleCategory[] compatibleCategories; // For complex connection rules
    }

    /// <summary>
    /// Helper class to associate a ModuleCategory with its corresponding prefab in the Inspector.
    /// Used by the ModularBuildingManager to store and retrieve module prefabs.
    /// Marking it [Serializable] allows it to be displayed and edited in the Unity Inspector.
    /// </summary>
    [Serializable]
    public class ModulePrefab
    {
        public ModuleCategory category;
        public GameObject prefab;
    }

    /// <summary>
    /// The core manager for the Modular Building System.
    /// It orchestrates the placement, removal, and management of building modules.
    /// This is where the 'builder' logic resides, using the defined 'modules'.
    /// </summary>
    public class ModularBuildingManager : MonoBehaviour
    {
        [Header("Module Configuration")]
        [Tooltip("List of all available building module prefabs, categorized by their type.")]
        public List<ModulePrefab> availableModules = new List<ModulePrefab>();

        [Tooltip("The parent Transform under which all instantiated building modules will be placed. " +
                 "Keeps the Hierarchy organized.")]
        public Transform buildingParent;

        [Tooltip("The size of one grid cell in Unity units. All modules are assumed to align to this grid. " +
                 "E.g., if a floor module is 2x2 units, set this to 2.")]
        public float placementGridSize = 2f;

        [Header("Demo Controls")]
        [Tooltip("Key to place a Floor module.")]
        public KeyCode placeFloorKey = KeyCode.Alpha1;
        [Tooltip("Key to place a Wall module.")]
        public KeyCode placeWallKey = KeyCode.Alpha2;
        [Tooltip("Key to place a Roof module.")]
        public KeyCode placeRoofKey = KeyCode.Alpha3;
        [Tooltip("Key to remove the last placed module.")]
        public KeyCode removeLastKey = KeyCode.R;
        [Tooltip("Key to clear the entire building.")]
        public KeyCode clearBuildingKey = KeyCode.C;

        // Internal list to keep track of all currently built GameObject modules.
        private List<GameObject> currentBuildingParts = new List<GameObject>();

        // Internal cursor to guide sequential placement in this simple demo.
        // It represents the bottom-center of the *next* module to be placed.
        private Vector3 buildCursorPosition;

        // --- Unity Lifecycle Methods ---

        void Start()
        {
            // Ensure a building parent exists for organization.
            if (buildingParent == null)
            {
                buildingParent = new GameObject("BuildingParent").transform;
                // Optionally parent it to this manager for easier scene cleanup if the manager is a singleton.
                buildingParent.SetParent(this.transform);
            }
            // Initialize the build cursor at the building parent's origin.
            buildCursorPosition = buildingParent.position;
        }

        void Update()
        {
            // --- Input Handling for Demonstration ---
            // These inputs trigger the core TryPlaceModule and RemoveModule methods.
            if (Input.GetKeyDown(placeFloorKey))
            {
                TryPlaceModule(ModuleCategory.Floor);
            }
            if (Input.GetKeyDown(placeWallKey))
            {
                TryPlaceModule(ModuleCategory.Wall);
            }
            if (Input.GetKeyDown(placeRoofKey))
            {
                TryPlaceModule(ModuleCategory.Roof);
            }
            if (Input.GetKeyDown(removeLastKey))
            {
                RemoveLastModule();
            }
            if (Input.GetKeyDown(clearBuildingKey))
            {
                ClearBuilding();
            }
        }

        // --- Core Building System Methods ---

        /// <summary>
        /// Attempts to place a module of a specific category.
        /// The placement logic here is simplified for demonstration purposes (sequential building).
        /// It builds a linear structure: floors extend along X, walls/roofs stack on top of the current build height.
        /// A real-world system would involve raycasting, grid snapping, and connection point detection.
        /// </summary>
        /// <param name="category">The category of the module to place (e.g., Floor, Wall).</param>
        private void TryPlaceModule(ModuleCategory category)
        {
            // 1. Retrieve the correct prefab for the given category.
            GameObject modulePrefab = GetModulePrefab(category);
            if (modulePrefab == null)
            {
                Debug.LogError($"Failed to place module: No prefab found for category '{category}'. " +
                               "Please ensure it's assigned in the Inspector under 'Available Modules'.");
                return;
            }

            // 2. Get the BuildingModule component from the prefab to understand its properties (e.g., size).
            BuildingModule moduleData = modulePrefab.GetComponent<BuildingModule>();
            if (moduleData == null)
            {
                Debug.LogError($"Prefab '{modulePrefab.name}' for category {category} is missing a BuildingModule component!");
                return;
            }

            Vector3 moduleDimensions = moduleData.footprintSize;
            Vector3 targetPlacementPosition = Vector3.zero;
            Quaternion targetPlacementRotation = Quaternion.identity; // For simplicity, no rotation in this demo

            // 3. Determine the placement position based on the building's current state.
            // This is the 'builder' logic, defining how modules interact and where they go.
            if (currentBuildingParts.Count == 0)
            {
                // If this is the first module, place it at the initial buildCursorPosition (buildingParent's origin),
                // elevated by half its height so its base is at `buildCursorPosition`.
                targetPlacementPosition = buildCursorPosition + Vector3.up * (moduleDimensions.y / 2f);
            }
            else
            {
                // For subsequent modules, placement depends on the category and updates `buildCursorPosition`.
                if (category == ModuleCategory.Floor)
                {
                    // Floors are placed horizontally along the X-axis in this demo.
                    // The Y level of the floor is determined by the `buildCursorPosition.y`.
                    targetPlacementPosition = new Vector3(
                        buildCursorPosition.x + placementGridSize / 2f, // Place the new floor right after the previous logic's `buildCursorPosition`
                        buildCursorPosition.y + moduleDimensions.y / 2f,
                        buildCursorPosition.z
                    );
                    // After placing, advance `buildCursorPosition.x` horizontally by `placementGridSize` for the *next* floor.
                    buildCursorPosition.x += placementGridSize;
                }
                else if (category == ModuleCategory.Wall)
                {
                    // Walls are placed vertically on top of the current `buildCursorPosition`'s XZ coordinates.
                    targetPlacementPosition = new Vector3(
                        buildCursorPosition.x - (placementGridSize / 2f), // Adjust X to place wall at the start of the last floor's grid cell
                        buildCursorPosition.y + moduleDimensions.y / 2f,
                        buildCursorPosition.z
                    );
                    // After placing a wall, update `buildCursorPosition.y` to the top of this wall for stacking.
                    buildCursorPosition.y += moduleDimensions.y;
                }
                else if (category == ModuleCategory.Roof)
                {
                    // Roofs are placed on top of the current highest point (determined by `buildCursorPosition.y`).
                    targetPlacementPosition = new Vector3(
                        buildCursorPosition.x - (placementGridSize / 2f), // Centered on the general building path
                        buildCursorPosition.y + moduleDimensions.y / 2f,
                        buildCursorPosition.z
                    );
                    // After placing a roof, update `buildCursorPosition.y` to its top.
                    buildCursorPosition.y += moduleDimensions.y;
                }
            }

            // 4. Instantiate the module.
            GameObject newModuleGO = Instantiate(modulePrefab, targetPlacementPosition, targetPlacementRotation, buildingParent);
            currentBuildingParts.Add(newModuleGO); // Keep track of the new module in our list

            Debug.Log($"Placed {category} module at {newModuleGO.transform.position}. Total modules: {currentBuildingParts.Count}");
        }

        /// <summary>
        /// Removes the last placed module from the building and reverts the build cursor position.
        /// </summary>
        public void RemoveLastModule()
        {
            if (currentBuildingParts.Count > 0)
            {
                GameObject lastModule = currentBuildingParts[currentBuildingParts.Count - 1];
                BuildingModule lastModuleData = lastModule.GetComponent<BuildingModule>();

                // Revert build cursor position based on the removed module's category and size.
                if (lastModuleData != null)
                {
                    if (lastModuleData.category == ModuleCategory.Floor)
                    {
                        buildCursorPosition.x -= placementGridSize;
                    }
                    else if (lastModuleData.category == ModuleCategory.Wall || lastModuleData.category == ModuleCategory.Roof)
                    {
                        // Note: This assumes walls/roofs were purely stacked.
                        // For more complex geometry, a more sophisticated undo stack or a full grid state would be needed.
                        buildCursorPosition.y -= lastModuleData.footprintSize.y;
                    }
                }

                currentBuildingParts.RemoveAt(currentBuildingParts.Count - 1); // Remove from list
                Destroy(lastModule); // Destroy the GameObject from the scene
                Debug.Log($"Removed last module. Total modules: {currentBuildingParts.Count}");
            }
            else
            {
                Debug.Log("No modules to remove.");
            }
        }

        /// <summary>
        /// Destroys all modules, clears the internal list, and resets the build cursor.
        /// Effectively clears the entire building structure.
        /// </summary>
        public void ClearBuilding()
        {
            foreach (GameObject module in currentBuildingParts)
            {
                Destroy(module); // Destroy each module GameObject
            }
            currentBuildingParts.Clear(); // Clear the list of modules
            buildCursorPosition = buildingParent.position; // Reset build cursor to origin
            Debug.Log("Building cleared.");
        }

        /// <summary>
        /// Helper method to find a module prefab by its category from the `availableModules` list.
        /// </summary>
        /// <param name="category">The category of the prefab to search for.</param>
        /// <returns>The GameObject prefab associated with the category, or null if not found.</returns>
        private GameObject GetModulePrefab(ModuleCategory category)
        {
            foreach (var mod in availableModules)
            {
                if (mod.category == category)
                {
                    return mod.prefab;
                }
            }
            return null; // Prefab not found for this category
        }
    }
}
```

---

### **How to Implement in Unity:**

1.  **Create the Script:**
    *   Create a new C# script in your Unity project, name it `ModularBuildingSystem`.
    *   Copy and paste the entire code above into this new script.

2.  **Create Module Prefabs:**
    *   In your Unity scene, create three basic 3D objects (e.g., `Cube` from `GameObject -> 3D Object -> Cube`).
    *   **For Floor Module:**
        *   Rename it `FloorModulePrefab`.
        *   Scale it to `X: 2, Y: 0.1, Z: 2` (or your desired `placementGridSize`).
        *   Add a `BuildingModule` component to it (`Add Component -> Building Module`).
        *   In the `BuildingModule` component, set `Category` to `Floor`.
        *   Set `Footprint Size` to `(2, 0.1, 2)` (matching its scaled size).
        *   Drag this `FloorModulePrefab` from the Hierarchy into your Project window to create a prefab. Delete it from the Hierarchy.
    *   **For Wall Module:**
        *   Rename it `WallModulePrefab`.
        *   Scale it to `X: 0.1, Y: 2, Z: 2` (or similar, where Y is height and Z is width of the wall).
        *   Add a `BuildingModule` component.
        *   Set `Category` to `Wall`.
        *   Set `Footprint Size` to `(0.1, 2, 2)`.
        *   Create a prefab and delete from Hierarchy.
    *   **For Roof Module:**
        *   Rename it `RoofModulePrefab`.
        *   Scale it to `X: 2, Y: 0.5, Z: 2` (e.g., a flat roof).
        *   Add a `BuildingModule` component.
        *   Set `Category` to `Roof`.
        *   Set `Footprint Size` to `(2, 0.5, 2)`.
        *   Create a prefab and delete from Hierarchy.
    *   *(Optional: You can add different materials or colors to these prefabs for better visual distinction).*

3.  **Set up the ModularBuildingManager:**
    *   In your Unity scene, create an empty GameObject (`GameObject -> Create Empty`).
    *   Rename it `ModularBuildingManager`.
    *   Add the `ModularBuildingManager` script to this GameObject.
    *   In the Inspector for `ModularBuildingManager`:
        *   Set `Placement Grid Size` to `2` (to match our example prefab dimensions).
        *   Expand the `Available Modules` list.
        *   Add 3 new elements.
        *   For each element:
            *   Set `Category` (e.g., `Floor`, `Wall`, `Roof`).
            *   Drag the corresponding prefab (`FloorModulePrefab`, `WallModulePrefab`, `RoofModulePrefab`) from your Project window into the `Prefab` slot.
        *   Leave `Building Parent` as `None` if you want the manager to create it automatically, or create an empty GameObject named `Building` and drag it there.

4.  **Run the Scene:**
    *   Play the scene.
    *   Press `1` (Alpha1) to place Floor modules. Observe them appearing sequentially along the X-axis.
    *   Press `2` (Alpha2) to place Wall modules. Observe them stacking on top of the last floor.
    *   Press `3` (Alpha3) to place Roof modules. Observe them capping the structure.
    *   Press `R` to remove the last placed module.
    *   Press `C` to clear the entire building.

This example provides a clear foundation for building more complex modular systems in Unity. You can extend it by adding more `ModuleCategory` types, implementing advanced snapping logic (e.g., using connection points on modules), saving/loading building structures, and integrating with a UI for user interaction.