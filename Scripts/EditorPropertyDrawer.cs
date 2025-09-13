// Unity Design Pattern Example: EditorPropertyDrawer
// This script demonstrates the EditorPropertyDrawer pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the `EditorPropertyDrawer` design pattern in Unity. This pattern is used to customize how a specific class or struct (which is marked `[System.Serializable]`) is drawn in the Inspector, without needing to write a full `CustomEditor` for the `MonoBehaviour` or `ScriptableObject` that contains it.

We will create:
1.  **`ProgressBarData.cs`**: A `[System.Serializable]` struct representing data for a progress bar (current value, max value, color, etc.).
2.  **`GameManager.cs`**: A `MonoBehaviour` that uses `ProgressBarData` to show how it appears in the Inspector.
3.  **`ProgressBarDrawer.cs`**: The `PropertyDrawer` script that defines the custom drawing logic for `ProgressBarData`. This script *must* be placed inside an `Editor` folder.

---

### File 1: `Assets/Scripts/ProgressBarDataExample.cs`

This file contains the `[System.Serializable]` struct that we want to customize, and an example `MonoBehaviour` that uses it.

```csharp
// File: Assets/Scripts/ProgressBarDataExample.cs
using UnityEngine;
using System; // Required for the [Serializable] attribute

/// <summary>
/// This struct holds the data for a progress bar.
/// It's marked [System.Serializable] so Unity's Inspector can display its fields,
/// and so our custom PropertyDrawer can draw it.
/// </summary>
[Serializable]
public struct ProgressBarData
{
    public string label;
    [Tooltip("The current value of the progress bar.")]
    public float currentValue;
    [Tooltip("The maximum possible value of the progress bar.")]
    public float maxValue;
    [Tooltip("The color of the filled portion of the progress bar.")]
    public Color barColor;
    [Tooltip("The color of any text displayed on the progress bar.")]
    public Color textColor;
    [Tooltip("If true, displays 'currentValue/maxValue' text on the bar.")]
    public bool showValueText;
    [Tooltip("If true, displays percentage text on the bar (e.g., '75%').")]
    public bool showPercentageText;
    [Tooltip("If true, displays the 'label' text on the bar.")]
    public bool showLabel;

    // Constructor for convenience (not strictly required for PropertyDrawer, but good practice)
    public ProgressBarData(string label, float currentValue, float maxValue, Color barColor, Color textColor, bool showValueText, bool showPercentageText, bool showLabel)
    {
        this.label = label;
        this.currentValue = currentValue;
        this.maxValue = maxValue;
        this.barColor = barColor;
        this.textColor = textColor;
        this.showValueText = showValueText;
        this.showPercentageText = showPercentageText;
        this.showLabel = showLabel;
    }

    /// <summary>
    /// Calculates the normalized progress (0 to 1) of the bar.
    /// </summary>
    public float GetNormalizedProgress()
    {
        if (maxValue <= 0) return 0; // Avoid division by zero
        return Mathf.Clamp01(currentValue / maxValue);
    }
}

/// <summary>
/// An example MonoBehaviour that uses our custom ProgressBarData struct.
/// When you attach this script to a GameObject in Unity,
/// the 'playerHealth', 'bossEncounterProgress', and 'playerStamina' fields
/// will be drawn using our custom ProgressBarDrawer.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game Progress Bars")]
    [Tooltip("Represents the player's health status.")]
    public ProgressBarData playerHealth = new ProgressBarData("Health", 100, 100, Color.red, Color.white, true, true, true);
    [Tooltip("Tracks the progress towards a boss encounter.")]
    public ProgressBarData bossEncounterProgress = new ProgressBarData("Boss Progress", 0, 100, Color.yellow, Color.black, false, true, true);
    [Tooltip("Represents the player's stamina.")]
    public ProgressBarData playerStamina = new ProgressBarData("Stamina", 50, 50, Color.green, Color.white, true, false, true);

    void Update()
    {
        // Example: Simulate player health decreasing and progress increasing over time
        // This demonstrates that the PropertyDrawer updates dynamically.
        if (Input.GetKey(KeyCode.Space))
        {
            playerHealth.currentValue -= Time.deltaTime * 5;
            playerHealth.currentValue = Mathf.Max(0, playerHealth.currentValue); // Clamp to 0
        }
        if (Input.GetKey(KeyCode.Return))
        {
            bossEncounterProgress.currentValue += Time.deltaTime * 10;
            bossEncounterProgress.currentValue = Mathf.Min(bossEncounterProgress.maxValue, bossEncounterProgress.currentValue); // Clamp to max
        }
    }
}
```

---

### File 2: `Assets/Editor/ProgressBarDrawer.cs`

This file contains the actual `PropertyDrawer` logic. **IMPORTANT**: This file must be placed in a folder named `Editor` (or a subfolder of `Editor`) for Unity to recognize it as an editor script.

```csharp
// File: Assets/Editor/ProgressBarDrawer.cs
// This file MUST be placed in an 'Editor' folder (e.g., Assets/Editor/)
// for Unity to recognize it as an editor script.

using UnityEngine;
using UnityEditor; // Essential for PropertyDrawer, EditorGUI, SerializedProperty, etc.

/// <summary>
/// Custom PropertyDrawer for the ProgressBarData struct.
/// This class tells Unity how to draw our ProgressBarData in the Inspector.
/// </summary>
[CustomPropertyDrawer(typeof(ProgressBarData))]
public class ProgressBarDrawer : PropertyDrawer
{
    // Define standard heights and spacing for consistent UI layout
    private const float LineHeight = EditorGUIUtility.singleLineHeight;
    private const float VerticalSpacing = EditorGUIUtility.standardVerticalSpacing;
    private const float BarHeight = LineHeight * 1.5f; // Make the visual bar slightly taller

    /// <summary>
    /// This method calculates the total height required by our custom drawer.
    /// It's crucial for preventing UI elements from overlapping, especially when
    /// the PropertyDrawer spans multiple lines.
    /// </summary>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Start with the height for the overall foldout label of the struct itself.
        // This is always present, whether the struct is expanded or not.
        float height = LineHeight + VerticalSpacing; 

        // If the struct is expanded, calculate the height for its contents.
        if (property.isExpanded)
        {
            // Add height for each distinct line/element we draw:
            // 1. "Bar Name" label field (string label)
            height += LineHeight + VerticalSpacing;
            // 2. "Current Value" slider field
            height += LineHeight + VerticalSpacing;
            // 3. "Max Value" slider field
            height += LineHeight + VerticalSpacing;
            // 4. "Bar Color" field
            height += LineHeight + VerticalSpacing;
            // 5. "Text Color" field
            height += LineHeight + VerticalSpacing;
            // 6. Display options (Show Label, Show Value, Show Percentage) - assumed to fit on one line
            height += LineHeight + VerticalSpacing;
            // 7. The actual visual progress bar itself
            height += BarHeight + VerticalSpacing;
        }
        
        return height;
    }

    /// <summary>
    /// This method is called by Unity to draw the custom UI for ProgressBarData in the Inspector.
    /// </summary>
    /// <param name="position">The rectangle on the screen where the PropertyDrawer should draw itself.</param>
    /// <param name="property">The SerializedProperty representing the ProgressBarData instance.</param>
    /// <param name="label">The GUIContent label for the property (e.g., "Player Health").</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty ensures that the property is correctly handled
        // by the Unity Editor's undo system and prefab overrides.
        EditorGUI.BeginProperty(position, label, property);

        // Store and reset indent level for the main struct label to ensure it's flush left.
        int originalIndentLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // --- 1. Draw the overall foldout label for the struct (e.g., "Player Health") ---
        // This allows the entire ProgressBarData section in the Inspector to be collapsed/expanded.
        position.height = LineHeight; // Set height for the foldout label
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
        position.y += LineHeight + VerticalSpacing; // Move to the next line for drawing content

        // If the struct is folded, we don't draw its internal contents.
        if (!property.isExpanded)
        {
            EditorGUI.indentLevel = originalIndentLevel; // Restore original indent level
            EditorGUI.EndProperty();
            return;
        }

        // Increase indentation for the fields within the expanded struct for better visual hierarchy.
        EditorGUI.indentLevel = originalIndentLevel + 1;

        // --- Find the serialized properties for each field within our ProgressBarData struct ---
        // We use FindPropertyRelative because 'property' is the parent ProgressBarData struct,
        // and we need to access its children fields.
        SerializedProperty labelProp = property.FindPropertyRelative("label");
        SerializedProperty currentValueProp = property.FindPropertyRelative("currentValue");
        SerializedProperty maxValueProp = property.FindPropertyRelative("maxValue");
        SerializedProperty barColorProp = property.FindPropertyRelative("barColor");
        SerializedProperty textColorProp = property.FindPropertyRelative("textColor");
        SerializedProperty showValueTextProp = property.FindPropertyRelative("showValueText");
        SerializedProperty showPercentageTextProp = property.FindPropertyRelative("showPercentageText");
        SerializedProperty showLabelProp = property.FindPropertyRelative("showLabel");

        // --- 2. Draw standard Unity Editor GUI controls for each field ---

        // Draw the "Bar Name" string field
        position.height = LineHeight;
        EditorGUI.PropertyField(position, labelProp, new GUIContent("Bar Name"));
        position.y += LineHeight + VerticalSpacing;

        // Draw the "Current Value" slider.
        // We clamp the max value of this slider to the actual maxValueProp to ensure it's logical.
        // We also ensure maxValueProp is at least 0.1 to avoid issues.
        position.height = LineHeight;
        currentValueProp.floatValue = EditorGUI.Slider(
            position, 
            new GUIContent("Current Value"), 
            currentValueProp.floatValue, 
            0f, 
            Mathf.Max(0.1f, maxValueProp.floatValue) // Max slider value cannot be less than 0.1
        );
        position.y += LineHeight + VerticalSpacing;

        // Draw the "Max Value" slider.
        // Min value is 0.1 to prevent division by zero for progress calculation.
        position.height = LineHeight;
        maxValueProp.floatValue = EditorGUI.Slider(position, new GUIContent("Max Value"), maxValueProp.floatValue, 0.1f, 1000f);
        position.y += LineHeight + VerticalSpacing;

        // Ensure current value does not exceed max value after user input.
        if (currentValueProp.floatValue > maxValueProp.floatValue)
        {
            currentValueProp.floatValue = maxValueProp.floatValue;
        }

        // Draw the "Bar Color" field
        position.height = LineHeight;
        EditorGUI.PropertyField(position, barColorProp, new GUIContent("Bar Color"));
        position.y += LineHeight + VerticalSpacing;

        // Draw the "Text Color" field
        position.height = LineHeight;
        EditorGUI.PropertyField(position, textColorProp, new GUIContent("Text Color"));
        position.y += LineHeight + VerticalSpacing;

        // Draw display options as toggle switches, laid out horizontally.
        position.height = LineHeight;
        Rect toggleRect = position;
        // Adjust toggleRect width to evenly distribute 3 toggles across the available content width.
        toggleRect.width = (position.width - EditorGUI.indentLevel * 15) / 3f; // Divide by 3 for 3 toggles

        // Each toggle takes up a portion of the line.
        showLabelProp.boolValue = EditorGUI.ToggleLeft(toggleRect, new GUIContent("Show Label"), showLabelProp.boolValue);
        toggleRect.x += toggleRect.width; // Move X position for the next toggle
        showValueTextProp.boolValue = EditorGUI.ToggleLeft(toggleRect, new GUIContent("Show Value"), showValueTextProp.boolValue);
        toggleRect.x += toggleRect.width; // Move X position for the next toggle
        showPercentageTextProp.boolValue = EditorGUI.ToggleLeft(toggleRect, new GUIContent("Show %"), showPercentageTextProp.boolValue);
        position.y += LineHeight + VerticalSpacing; // Move to the next line for the bar visual

        // --- 3. Draw the actual visual progress bar ---
        Rect barBackgroundRect = position;
        barBackgroundRect.height = BarHeight;
        // Adjust the bar's X position and width to respect the indentation and fill available space.
        barBackgroundRect.xMin += (EditorGUI.indentLevel - originalIndentLevel) * 15; // Shift right by current indent
        barBackgroundRect.width = position.width - (EditorGUI.indentLevel - originalIndentLevel) * 15; // Reduce width

        // Draw the background of the bar (e.g., a dark grey)
        EditorGUI.DrawRect(barBackgroundRect, Color.gray * 0.7f);

        // Calculate the fill amount (0-1)
        float normalizedProgress = 0f;
        if (maxValueProp.floatValue > 0)
        {
            normalizedProgress = Mathf.Clamp01(currentValueProp.floatValue / maxValueProp.floatValue);
        }

        // Draw the filled portion of the bar using the specified barColor
        Rect barFillRect = barBackgroundRect;
        barFillRect.width *= normalizedProgress; // Scale width by progress
        EditorGUI.DrawRect(barFillRect, barColorProp.color);

        // --- 4. Draw text overlay on the bar ---
        // Only draw text if at least one text option is enabled.
        if (showLabelProp.boolValue || showValueTextProp.boolValue || showPercentageTextProp.boolValue)
        {
            // Center the text vertically within the bar
            Rect textRect = barBackgroundRect;
            textRect.y += (BarHeight - LineHeight) * 0.5f; // Adjust to center line height within bar height
            textRect.height = LineHeight;

            // Define text style
            GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel); // Use a bold label style
            textStyle.normal.textColor = textColorProp.color; // Apply specified text color
            textStyle.alignment = TextAnchor.MiddleCenter; // Center text horizontally and vertically
            textStyle.clipping = TextClipping.Overflow; // Allow text to overflow if it's too long

            // Construct the display string based on enabled options
            string displayText = "";
            if (showLabelProp.boolValue && !string.IsNullOrEmpty(labelProp.stringValue))
            {
                displayText += labelProp.stringValue;
            }
            if (showValueTextProp.boolValue)
            {
                if (!string.IsNullOrEmpty(displayText)) displayText += " - ";
                displayText += $"{currentValueProp.floatValue:F0}/{maxValueProp.floatValue:F0}"; // Format to 0 decimal places
            }
            if (showPercentageTextProp.boolValue)
            {
                if (!string.IsNullOrEmpty(displayText)) displayText += " (";
                else displayText += "(";
                displayText += $"{normalizedProgress * 100:F0}%)"; // Format to 0 decimal places
            }

            // Draw the combined text string
            EditorGUI.LabelField(textRect, displayText, textStyle);
        }

        // Restore original indent level to avoid affecting subsequent properties.
        EditorGUI.indentLevel = originalIndentLevel;

        // End the property block. This saves any changes made through PropertyDrawer.
        EditorGUI.EndProperty();
    }
}
```

---

### How to Use This Example:

1.  **Create Folders**: In your Unity project's `Assets` folder, create two new folders: `Scripts` and `Editor`.
2.  **Save Scripts**:
    *   Save the content of `ProgressBarDataExample.cs` into `Assets/Scripts/ProgressBarDataExample.cs`.
    *   Save the content of `ProgressBarDrawer.cs` into `Assets/Editor/ProgressBarDrawer.cs`.
3.  **Create GameObject**: In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty). Name it "GameManager".
4.  **Attach Script**: Drag the `GameManager.cs` script from your `Assets/Scripts` folder onto the "GameManager" GameObject in the Hierarchy, or click "Add Component" and search for "GameManager".
5.  **Observe in Inspector**: Select the "GameManager" GameObject. You will now see the `playerHealth`, `bossEncounterProgress`, and `playerStamina` fields drawn using the custom progress bar UI defined in `ProgressBarDrawer.cs`, rather than Unity's default drawing for structs.
6.  **Interact**:
    *   You can expand/collapse each progress bar using the foldout arrow.
    *   Adjust the "Current Value" and "Max Value" sliders, change colors, and toggle the text options.
    *   Run the scene and press **Space** to decrease player health, or **Return** to increase boss progress. Observe the progress bars updating in the Inspector in real-time.

This example provides a practical and educational demonstration of the `EditorPropertyDrawer` pattern, allowing you to create custom, intuitive Inspector UIs for your data structures in Unity.