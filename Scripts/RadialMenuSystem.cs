// Unity Design Pattern Example: RadialMenuSystem
// This script demonstrates the RadialMenuSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete, practical, and educational implementation of a 'RadialMenuSystem' in Unity using C#. It leverages Unity's UI system, `UnityEvents` for flexible actions, and simple animation for a polished user experience.

The core idea of the RadialMenuSystem design pattern here is to encapsulate the logic for creating, positioning, animating, and interacting with radial menu items into a reusable component. It separates the menu item's data (icon, title, action) from its UI representation and the menu's overall controller logic.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Button, Image, Text
using UnityEngine.Events; // Required for UnityEvent, allowing custom actions in the Inspector
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for List

/// <summary>
/// Represents a single item within the radial menu.
/// Marked as [System.Serializable] so it can be configured directly in the Unity Inspector.
/// </summary>
[System.Serializable]
public class RadialMenuItem
{
    [Tooltip("Optional icon to display on the menu item button.")]
    public Sprite icon;

    [Tooltip("Text label to display on the menu item button.")]
    public string title;

    [Tooltip("The action to perform when this menu item is selected (clicked).")]
    public UnityEvent OnSelected;
}

/// <summary>
/// A MonoBehaviour that controls the creation, positioning, and interaction of a radial menu.
/// This script should be attached to an empty GameObject that serves as the root/center
/// of your radial menu within a Canvas.
/// </summary>
public class RadialMenuSystem : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The prefab for a single menu item. This should be a UI Button with an Image and Text component.")]
    [SerializeField] private GameObject menuItemPrefab;

    [Tooltip("The RectTransform that will act as the parent and center point for all instantiated menu items.")]
    [SerializeField] private RectTransform centerPoint;

    [Header("Menu Layout")]
    [Tooltip("The radius from the centerPoint where menu items will be positioned.")]
    [SerializeField] private float radius = 200f;

    [Tooltip("The starting angle (in degrees) for the first menu item. 0 is right, 90 is top, etc.")]
    [SerializeField] private float startAngleOffset = 90f;

    [Tooltip("If true, items will be placed clockwise; otherwise, counter-clockwise.")]
    [SerializeField] private bool clockwise = true;

    [Header("Menu Interaction")]
    [Tooltip("The key to press to toggle the radial menu open/closed.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.E;

    [Tooltip("The duration (in seconds) for the menu's open/close animation.")]
    [SerializeField] private float animationDuration = 0.25f;

    [Header("Menu Items Data")]
    [Tooltip("List of all menu items, defining their icon, title, and the action to perform on selection.")]
    [SerializeField] private List<RadialMenuItem> menuItems = new List<RadialMenuItem>();

    // Internal state variables
    private bool isMenuOpen = false;
    private bool isAnimating = false;
    private List<GameObject> instantiatedMenuItems = new List<GameObject>();
    private List<Vector2> radialItemPositions = new List<Vector2>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the menu by instantiating items and calculating their positions.
    /// </summary>
    void Awake()
    {
        if (menuItemPrefab == null)
        {
            Debug.LogError("RadialMenuSystem: Menu Item Prefab is not assigned! Please assign a UI Button prefab.", this);
            enabled = false; // Disable the script if critical reference is missing
            return;
        }
        if (centerPoint == null)
        {
            centerPoint = GetComponent<RectTransform>();
            if (centerPoint == null)
            {
                Debug.LogError("RadialMenuSystem: Center Point (RectTransform) is not assigned and could not be found on this GameObject. Assign it in the Inspector or ensure this script is on a RectTransform.", this);
                enabled = false;
                return;
            }
        }

        InstantiateMenuItems();
        CalculateRadialPositions();
        InitializeMenuState();
    }

    /// <summary>
    /// Handles user input to toggle the menu's open/closed state.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && !isAnimating)
        {
            if (isMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    /// <summary>
    /// Instantiates the UI GameObjects for each menu item defined in the `menuItems` list.
    /// Each instantiated item is set up with its icon, title, and a click listener.
    /// </summary>
    private void InstantiateMenuItems()
    {
        // Clear any previously instantiated items
        foreach (GameObject item in instantiatedMenuItems)
        {
            Destroy(item);
        }
        instantiatedMenuItems.Clear();

        if (menuItems == null || menuItems.Count == 0)
        {
            Debug.LogWarning("RadialMenuSystem: No menu items defined. The menu will be empty.");
            return;
        }

        for (int i = 0; i < menuItems.Count; i++)
        {
            RadialMenuItem itemData = menuItems[i];
            GameObject itemGO = Instantiate(menuItemPrefab, centerPoint);
            itemGO.name = "RadialMenuItem_" + itemData.title;

            // Get UI components from the instantiated prefab
            Button button = itemGO.GetComponent<Button>();
            Image iconImage = itemGO.GetComponent<Image>(); // Assumes the prefab's root is an Image or has an Image as a direct child
            Text titleText = itemGO.GetComponentInChildren<Text>(); // Assumes Text component is a child

            if (button == null) { Debug.LogError($"RadialMenuSystem: Item prefab '{menuItemPrefab.name}' is missing a Button component!", menuItemPrefab); continue; }
            if (iconImage == null && itemData.icon != null) { Debug.LogWarning($"RadialMenuSystem: Item prefab '{menuItemPrefab.name}' is missing an Image component to display the icon for item '{itemData.title}'.", menuItemPrefab); }
            if (titleText == null && !string.IsNullOrEmpty(itemData.title)) { Debug.LogWarning($"RadialMenuSystem: Item prefab '{menuItemPrefab.name}' is missing a Text component to display the title for item '{itemData.title}'.", menuItemPrefab); }

            // Apply data to UI elements
            if (iconImage != null && itemData.icon != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.color = Color.white; // Ensure it's not tinted if using a button base image
            }
            else if (iconImage != null) // If no icon is provided, hide/clear the image
            {
                iconImage.color = Color.clear;
            }

            if (titleText != null)
            {
                titleText.text = itemData.title;
            }

            // Add listener to the button. Need to capture 'i' for correct item action.
            int itemIndex = i; // Create a local copy for the closure
            button.onClick.AddListener(() => OnMenuItemClicked(itemIndex));

            instantiatedMenuItems.Add(itemGO);
        }
    }

    /// <summary>
    /// Calculates the target positions for each menu item around the central point.
    /// These positions are stored and used for animating and displaying the menu.
    /// </summary>
    private void CalculateRadialPositions()
    {
        radialItemPositions.Clear();
        if (instantiatedMenuItems.Count == 0) return;

        // Calculate the angle step between each item
        float angleStep = 360f / instantiatedMenuItems.Count;

        for (int i = 0; i < instantiatedMenuItems.Count; i++)
        {
            // Calculate the current angle for this item
            // 'clockwise' determines if the angle increases or decreases with each item
            float currentAngle = startAngleOffset + (clockwise ? -1 : 1) * i * angleStep;

            // Convert angle to radians for trigonometric functions
            float angleInRadians = currentAngle * Mathf.Deg2Rad;

            // Calculate X and Y coordinates using trigonometry
            float x = radius * Mathf.Cos(angleInRadians);
            float y = radius * Mathf.Sin(angleInRadians);

            radialItemPositions.Add(new Vector2(x, y));
        }
    }

    /// <summary>
    /// Sets the initial state of the menu (closed, items inactive and at center).
    /// </summary>
    private void InitializeMenuState()
    {
        foreach (GameObject itemGO in instantiatedMenuItems)
        {
            RectTransform itemRect = itemGO.GetComponent<RectTransform>();
            itemRect.anchoredPosition = Vector2.zero; // Start at the center
            itemRect.localScale = Vector3.zero; // Start scaled down (invisible)
            itemGO.SetActive(false); // Make items inactive initially
        }
        isMenuOpen = false;
    }

    /// <summary>
    /// Opens the radial menu with an animation.
    /// </summary>
    public void OpenMenu()
    {
        if (isAnimating) return;
        
        Debug.Log("Opening Radial Menu");
        StopAllCoroutines(); // Stop any ongoing animations
        StartCoroutine(AnimateMenu(true));
    }

    /// <summary>
    /// Closes the radial menu with an animation.
    /// </summary>
    public void CloseMenu()
    {
        if (isAnimating) return;

        Debug.Log("Closing Radial Menu");
        StopAllCoroutines(); // Stop any ongoing animations
        StartCoroutine(AnimateMenu(false));
    }

    /// <summary>
    /// Coroutine to animate the menu items from center to radial positions (opening)
    /// or from radial positions to center (closing).
    /// </summary>
    /// <param name="opening">True if animating open, false if animating closed.</param>
    private IEnumerator AnimateMenu(bool opening)
    {
        isAnimating = true;
        
        if (opening)
        {
            foreach (GameObject itemGO in instantiatedMenuItems)
            {
                itemGO.SetActive(true); // Make items active before animating them out
            }
        }

        float timer = 0f;
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / animationDuration);
            if (!opening) progress = 1f - progress; // Reverse progress for closing animation

            for (int i = 0; i < instantiatedMenuItems.Count; i++)
            {
                RectTransform itemRect = instantiatedMenuItems[i].GetComponent<RectTransform>();
                
                // Animate position
                Vector2 startPos = opening ? Vector2.zero : radialItemPositions[i];
                Vector2 endPos = opening ? radialItemPositions[i] : Vector2.zero;
                itemRect.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);

                // Animate scale
                float scale = progress; // Scale from 0 to 1
                itemRect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, scale);
            }
            yield return null; // Wait for the next frame
        }

        // Ensure final state after animation
        for (int i = 0; i < instantiatedMenuItems.Count; i++)
        {
            RectTransform itemRect = instantiatedMenuItems[i].GetComponent<RectTransform>();
            itemRect.anchoredPosition = opening ? radialItemPositions[i] : Vector2.zero;
            itemRect.localScale = opening ? Vector3.one : Vector3.zero;
            instantiatedMenuItems[i].SetActive(opening); // Hide items if closing
        }

        isMenuOpen = opening;
        isAnimating = false;
        
        // After closing, automatically set all instantiated items to inactive
        if (!opening)
        {
            foreach (GameObject itemGO in instantiatedMenuItems)
            {
                itemGO.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called when a menu item's button is clicked.
    /// Invokes the corresponding `UnityEvent` defined for that item.
    /// </summary>
    /// <param name="index">The index of the clicked menu item.</param>
    private void OnMenuItemClicked(int index)
    {
        if (index >= 0 && index < menuItems.Count)
        {
            Debug.Log($"Radial Menu: Item '{menuItems[index].title}' clicked. Invoking action.");
            menuItems[index].OnSelected?.Invoke(); // Invoke the UnityEvent
            CloseMenu(); // Close the menu after an item is selected
        }
    }

    /// <summary>
    /// Helper method to reset the menu, useful for editor testing or dynamic changes.
    /// </summary>
    public void ResetMenu()
    {
        // Re-instantiate, re-calculate, and re-initialize
        InstantiateMenuItems();
        CalculateRadialPositions();
        InitializeMenuState();
    }

    // Optional: Draw gizmos in the editor to visualize the menu's radius and item positions
    void OnDrawGizmosSelected()
    {
        if (centerPoint == null) return;

        Gizmos.color = Color.yellow;
        // Draw the central point
        Gizmos.DrawWireSphere(centerPoint.position, 10f);
        // Draw the radius circle
        UnityEditor.Handles.DrawWireDisc(centerPoint.position, Vector3.forward, radius);

        if (menuItems == null || menuItems.Count == 0) return;

        // Draw individual item positions
        float angleStep = 360f / menuItems.Count;
        for (int i = 0; i < menuItems.Count; i++)
        {
            float currentAngle = startAngleOffset + (clockwise ? -1 : 1) * i * angleStep;
            float angleInRadians = currentAngle * Mathf.Deg2Rad;
            Vector3 itemLocalPos = new Vector3(radius * Mathf.Cos(angleInRadians), radius * Mathf.Sin(angleInRadians), 0f);
            Vector3 itemWorldPos = centerPoint.TransformPoint(itemLocalPos); // Convert local to world position

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(itemWorldPos, 5f);
            Gizmos.DrawLine(centerPoint.position, itemWorldPos);
            
            // Draw text label for clarity (only works with UnityEditor.Handles.Label)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(itemWorldPos + Vector3.up * 15, menuItems[i].title);
            #endif
        }
    }
}

/*
/// <summary>
/// EXAMPLE USAGE:
/// To use this RadialMenuSystem script in your Unity project, follow these steps:
/// </summary>
/// 
/// 1.  **Create a Canvas:**
///     -   In the Unity Hierarchy, right-click -> UI -> Canvas.
///     -   Set the Canvas 'Render Mode' to 'Screen Space - Overlay' for simplicity, or 'Screen Space - Camera' if you have a specific camera for UI.
/// 
/// 2.  **Create the Radial Menu Root GameObject:**
///     -   As a child of the Canvas, create an empty GameObject (right-click Canvas -> Create Empty).
///     -   Name it, for example, "RadialMenuRoot".
///     -   Adjust its RectTransform to position the center of your menu. A common setup is to anchor it to the center of the screen (Shift+Alt+Middle).
///     -   Attach this `RadialMenuSystem` script to "RadialMenuRoot".
/// 
/// 3.  **Create a UI Button Prefab for Menu Items:**
///     -   Create a new UI Button (right-click Canvas -> UI -> Button).
///     -   Modify this Button:
///         -   You might want to remove its default 'Text' child and add your own 'Image' (for the icon) and 'Text' (for the label) as children, or just use the Button's default Image as the icon base and ensure a Text child exists.
///         -   Adjust its size, font, colors, etc., to your liking. Make sure the `RectTransform` pivot is centered.
///         -   **Crucially:** Drag this customized Button GameObject from the Hierarchy into your Project tab to create a prefab.
///     -   Delete the Button from the Hierarchy; we only need the prefab.
/// 
/// 4.  **Configure the RadialMenuSystem in the Inspector:**
///     -   Select "RadialMenuRoot" in the Hierarchy.
///     -   In the Inspector for the `RadialMenuSystem` component:
///         -   **Menu Item Prefab**: Drag your created Button Prefab from the Project window here.
///         -   **Center Point**: Drag "RadialMenuRoot" itself from the Hierarchy here (as it will parent the menu items).
///         -   **Radius**: Set a value (e.g., 150-250) to control how far items are from the center.
///         -   **Start Angle Offset**: Adjust this (e.g., 90 for items to start at the top) to rotate the entire menu.
///         -   **Toggle Key**: Choose a key (e.g., `E`, `Q`, `Tab`) to open/close the menu.
///         -   **Animation Duration**: Set how fast the menu opens/closes (e.g., 0.25 seconds).
/// 
/// 5.  **Populate the Menu Items List:**
///     -   In the Inspector, expand the 'Menu Items' list.
///     -   Increase the `Size` of the list to add new menu items.
///     -   For each item:
///         -   **Icon**: Drag a Sprite from your Project window (e.g., a weapon icon, spell icon). This will be displayed on the button.
///         -   **Title**: Enter the text label for the item (e.g., "Sword", "Heal", "Magic Shield").
///         -   **On Selected (UnityEvent)**:
///             -   Click the `+` button to add a new action.
///             -   Drag a GameObject from your scene (e.g., your Player object, a Game Manager object) that has a script with a public method you want to call.
///             -   In the dropdown menu that appears, navigate to the script component and select the desired public method.
///             -   Example: If you have a `PlayerController` script on your `Player` object with a public method `public void EquipSword()`, drag the `Player` object and select `PlayerController.EquipSword`. You can also pass parameters if the method supports it.
/// 
/// 6.  **Run the Scene!**
///     -   Press your assigned `Toggle Key` to open the radial menu.
///     -   Click on any of the menu items to trigger its assigned `On Selected` action. The menu will automatically close after an item is selected.
///     -   Press the `Toggle Key` again to close the menu without selecting an item.
*/
```