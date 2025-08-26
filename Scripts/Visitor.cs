// Unity Design Pattern Example: Visitor
// This script demonstrates the Visitor pattern in Unity
// Generated automatically - ready to use in your Unity project

The Visitor design pattern allows you to separate an algorithm from the object structure on which it operates. This means you can add new operations to existing object structures without modifying those structures. It's particularly useful when you have a stable class hierarchy (the "elements") but frequently need to add new types of operations (the "visitors").

In Unity, a common real-world scenario could be applying various operations to different types of game objects, components, or in-game items. For this example, we'll demonstrate using the Visitor pattern for **item interaction and effects** in a simple RPG-like context.

You will have:
1.  **Items (Elements):** Different types of in-game items (e.g., `HealthPotionItem`, `ManaPotionItem`, `WeaponItem`).
2.  **Operations (Visitors):** Different actions that can be performed on these items (e.g., adding to inventory, using their effect, getting a description).

---

### **How to Use This Example in Unity:**

1.  **Create a new C# script** in your Unity project called `VisitorPatternDemo`.
2.  **Copy and paste the entire code** provided below into `VisitorPatternDemo.cs`.
3.  **Create an empty GameObject** in your scene (e.g., name it "VisitorPatternDemoManager").
4.  **Attach the `VisitorPatternDemo` script** to this GameObject.
5.  **Create Player Components (Optional but Recommended):**
    *   Create an empty GameObject named "PlayerInventory".
    *   Create an empty GameObject named "PlayerStats".
    *   The `VisitorPatternDemo` script will automatically create `PlayerInventory` and `PlayerStats` components if it doesn't find them in the scene. For more control, you can drag these created GameObjects to the `Player Inventory` and `Player Stats` fields in the Inspector of the "VisitorPatternDemoManager" GameObject.
6.  **Create Item Prefabs (Required for Instantiation):**
    *   **Health Potion Prefab:**
        *   Create an empty GameObject, rename it "HealthPotionPrefab".
        *   Add the `HealthPotionItem` component to it.
        *   Set its `Heal Amount` (e.g., 25) in the Inspector.
        *   Drag this GameObject from your Hierarchy into your Project window to create a prefab.
    *   **Mana Potion Prefab:**
        *   Repeat the above steps for "ManaPotionPrefab", adding the `ManaPotionItem` component.
        *   Set its `Mana Restore Amount` (e.g., 20).
    *   **Weapon Prefab:**
        *   Repeat for "WeaponPrefab", adding the `WeaponItem` component.
        *   Set its `Damage` (e.g., 15).
7.  **Assign Prefabs:** Drag these three new prefabs from your Project window to their respective fields (`Health Potion Prefab`, `Mana Potion Prefab`, `Weapon Prefab`) in the Inspector of the "VisitorPatternDemoManager" GameObject.
8.  **Run the scene!** Observe the detailed output in the Unity Console to understand how the Visitor pattern is working step-by-step.

---

### **VisitorPatternDemo.cs**

```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionary and List

// --- 1. The IElement Interface ---
// This interface defines the 'Accept' method. All concrete elements (our items)
// must implement this method. The 'Accept' method takes an IItemVisitor as an argument,
// allowing the visitor to interact with the element.
public interface IItem
{
    string ItemName { get; } // A common property for all items
    void Accept(IItemVisitor visitor);
}

// --- 2. The IVisitor Interface ---
// This interface declares a 'Visit' method for each concrete type of element
// (each specific item type) that it can operate on.
// When a new type of item is introduced, this interface (and consequently, all
// concrete visitor classes) must be updated to include a new 'Visit' method for that item.
public interface IItemVisitor
{
    void Visit(HealthPotionItem healthPotion);
    void Visit(ManaPotionItem manaPotion);
    void Visit(WeaponItem weapon);
    // If you add a new item type (e.g., ArmorItem), you MUST add:
    // void Visit(ArmorItem armor);
    // to this interface and implement it in all concrete visitors.
}

// --- 3. Concrete Element Classes (Items) ---
// These classes represent the different types of items in our game.
// They implement the IItem interface and contain data specific to that item.
// Critically, their `Accept` method simply calls the appropriate `Visit` method
// on the passed-in visitor, effectively "dispatching" the operation back to the visitor.

// Health Potion Item: Heals the player.
public class HealthPotionItem : MonoBehaviour, IItem
{
    public string ItemName => "Health Potion";
    // [field: SerializeField] allows for serialization in the Inspector while keeping the setter private.
    [field: SerializeField] public int HealAmount { get; private set; } = 25;

    // The core of the Visitor pattern on the element side.
    // When a visitor 'visits' this item, this method ensures that the
    // correct 'Visit' overload (for HealthPotionItem) is called on the visitor.
    public void Accept(IItemVisitor visitor)
    {
        visitor.Visit(this);
    }
}

// Mana Potion Item: Restores player mana.
public class ManaPotionItem : MonoBehaviour, IItem
{
    public string ItemName => "Mana Potion";
    [field: SerializeField] public int ManaRestoreAmount { get; private set; } = 20;

    public void Accept(IItemVisitor visitor)
    {
        visitor.Visit(this);
    }
}

// Weapon Item: Can be equipped by the player.
public class WeaponItem : MonoBehaviour, IItem
{
    public string ItemName => "Sword of Power";
    [field: SerializeField] public int Damage { get; private set; } = 15;

    public void Accept(IItemVisitor visitor)
    {
        visitor.Visit(this);
    }
}

// --- 4. Context/Helper Classes (Player-specific components) ---
// These classes are not part of the Visitor pattern itself, but they provide the
// necessary environment and state for our visitor operations to make sense in Unity.

// Player Inventory Component: Manages items the player possesses.
public class PlayerInventory : MonoBehaviour
{
    private Dictionary<string, IItem> _items = new Dictionary<string, IItem>();

    public void AddItem(string itemName, IItem item)
    {
        if (!_items.ContainsKey(itemName))
        {
            _items.Add(itemName, item);
            Debug.Log($"[PlayerInventory] Added '{itemName}' to inventory.");
        }
        else
        {
            Debug.LogWarning($"[PlayerInventory] Item '{itemName}' already exists. Overwriting. (This demo simplifies inventory management)");
            _items[itemName] = item;
        }
    }

    public IItem GetItem(string itemName)
    {
        _items.TryGetValue(itemName, out IItem item);
        return item;
    }

    public void RemoveItem(string itemName)
    {
        if (_items.Remove(itemName))
        {
            Debug.Log($"[PlayerInventory] Removed '{itemName}'.");
        }
        else
        {
            Debug.LogWarning($"[PlayerInventory] Could not remove '{itemName}': not found.");
        }
    }

    public void ListItems()
    {
        Debug.Log("[PlayerInventory] Current Contents:");
        if (_items.Count == 0)
        {
            Debug.Log("  (Empty)");
            return;
        }
        foreach (var item in _items)
        {
            Debug.Log($"  - {item.Key}");
        }
    }
}

// Player Stats Component: Manages player's health, mana, and equipped gear.
public class PlayerStats : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _currentHealth = 70;
    [SerializeField] private int _maxMana = 50;
    [SerializeField] private int _currentMana = 30;
    private WeaponItem _equippedWeapon;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public int MaxMana => _maxMana;
    public int CurrentMana => _currentMana;
    public int CurrentWeaponDamage => _equippedWeapon != null ? _equippedWeapon.Damage : 0;

    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        Debug.Log($"[PlayerStats] Player healed by {amount}. HP: {_currentHealth}/{_maxHealth}");
    }

    public void RestoreMana(int amount)
    {
        _currentMana = Mathf.Min(_currentMana + amount, _maxMana);
        Debug.Log($"[PlayerStats] Player mana restored by {amount}. MP: {_currentMana}/{_maxMana}");
    }

    public void EquipWeapon(WeaponItem weapon)
    {
        _equippedWeapon = weapon;
        Debug.Log($"[PlayerStats] Player equipped {weapon.ItemName}. Current Damage: {weapon.Damage}");
    }

    public void TakeDamage(int amount)
    {
        _currentHealth = Mathf.Max(_currentHealth - amount, 0);
        Debug.Log($"[PlayerStats] Player took {amount} damage. HP: {_currentHealth}/{_maxHealth}");
    }

    void Start()
    {
        Debug.Log($"[PlayerStats] Initialized: HP {_currentHealth}/{_maxHealth}, MP {_currentMana}/{_maxMana}");
    }
}

// --- 5. Concrete Visitor Classes ---
// Each concrete visitor implements the IItemVisitor interface and provides specific
// logic for each type of item it can visit. These classes encapsulate operations
// that would otherwise be spread across item classes or require cumbersome type checks.

// Visitor for adding items to the player's inventory.
public class InventoryAdderVisitor : IItemVisitor
{
    private PlayerInventory _inventory;

    public InventoryAdderVisitor(PlayerInventory inventory)
    {
        _inventory = inventory;
    }

    public void Visit(HealthPotionItem healthPotion)
    {
        _inventory.AddItem(healthPotion.ItemName, healthPotion);
        Debug.Log($"[InventoryAdderVisitor] Processed {healthPotion.ItemName} for inventory.");
    }

    public void Visit(ManaPotionItem manaPotion)
    {
        _inventory.AddItem(manaPotion.ItemName, manaPotion);
        Debug.Log($"[InventoryAdderVisitor] Processed {manaPotion.ItemName} for inventory.");
    }

    public void Visit(WeaponItem weapon)
    {
        _inventory.AddItem(weapon.ItemName, weapon);
        Debug.Log($"[InventoryAdderVisitor] Processed {weapon.ItemName} for inventory.");
    }
}

// Visitor for applying an item's effect (e.g., healing, mana restoration, equipping).
public class ItemUseVisitor : IItemVisitor
{
    private PlayerStats _playerStats;

    public ItemUseVisitor(PlayerStats playerStats)
    {
        _playerStats = playerStats;
    }

    public void Visit(HealthPotionItem healthPotion)
    {
        _playerStats.Heal(healthPotion.HealAmount);
        Debug.Log($"[ItemUseVisitor] Used {healthPotion.ItemName}.");
    }

    public void Visit(ManaPotionItem manaPotion)
    {
        _playerStats.RestoreMana(manaPotion.ManaRestoreAmount);
        Debug.Log($"[ItemUseVisitor] Used {manaPotion.ItemName}.");
    }

    public void Visit(WeaponItem weapon)
    {
        _playerStats.EquipWeapon(weapon);
        Debug.Log($"[ItemUseVisitor] Equipped {weapon.ItemName}.");
    }
}

// Visitor for generating a descriptive string for an item.
public class ItemDescriberVisitor : IItemVisitor
{
    public string Description { get; private set; } = "No description available.";

    public void Visit(HealthPotionItem healthPotion)
    {
        Description = $"A potent {healthPotion.ItemName} that restores {healthPotion.HealAmount} health.";
    }

    public void Visit(ManaPotionItem manaPotion)
    {
        Description = $"A shimmering {manaPotion.ItemName} that restores {manaPotion.ManaRestoreAmount} mana.";
    }

    public void Visit(WeaponItem weapon)
    {
        Description = $"A mighty {weapon.ItemName} capable of dealing {weapon.Damage} damage.";
    }
}

// --- 6. Demo Manager (Client) ---
// This MonoBehaviour orchestrates the demonstration. It acts as the 'Client' in the
// Visitor pattern, creating elements (items) and visitors, then initiating the
// 'visitation' process.
public class VisitorDemoManager : MonoBehaviour
{
    [Header("Player Components (Assign in Inspector or they will be created)")]
    public PlayerInventory playerInventory;
    public PlayerStats playerStats;

    [Header("Item Prefabs (Assign in Inspector for easy spawning)")]
    public HealthPotionItem healthPotionPrefab;
    public ManaPotionItem manaPotionPrefab;
    public WeaponItem weaponPrefab;

    void Start()
    {
        Debug.Log("--- Visitor Pattern Demo Started ---");

        // Ensure player components exist. If not assigned in Inspector, try to find or create.
        if (playerInventory == null) playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();

        if (playerInventory == null)
        {
            playerInventory = new GameObject("PlayerInventory").AddComponent<PlayerInventory>();
            Debug.LogWarning("PlayerInventory not found in scene, created a new one dynamically.");
        }
        if (playerStats == null)
        {
            playerStats = new GameObject("PlayerStats").AddComponent<PlayerStats>();
            Debug.LogWarning("PlayerStats not found in scene, created a new one dynamically.");
        }

        // --- Instantiate Items ---
        // In a real game, these items might be found in the world, dropped by enemies, etc.
        // For this demo, we instantiate them from prefabs and customize some properties.
        Debug.Log("\n--- Instantiating Sample Items ---");
        HealthPotionItem bigHealthPotion = Instantiate(healthPotionPrefab);
        bigHealthPotion.HealAmount = 50; // Customize properties for this specific instance
        bigHealthPotion.gameObject.name = "Big Health Potion (Instance)";

        ManaPotionItem smallManaPotion = Instantiate(manaPotionPrefab);
        smallManaPotion.ManaRestoreAmount = 15;
        smallManaPotion.gameObject.name = "Small Mana Potion (Instance)";

        WeaponItem magicSword = Instantiate(weaponPrefab);
        magicSword.Damage = 25;
        magicSword.gameObject.name = "Magic Sword (Instance)";

        // A list to hold all our instanced items, treating them generically as IItem.
        List<IItem> droppedItems = new List<IItem> { bigHealthPotion, smallManaPotion, magicSword };

        Debug.Log("\n--- PHASE 1: Collecting Items using InventoryAdderVisitor ---");
        // Create an InventoryAdderVisitor, giving it a reference to the player's inventory.
        IItemVisitor inventoryVisitor = new InventoryAdderVisitor(playerInventory);

        // Iterate through each 'dropped' item. The magic happens here:
        // Each item calls its own Accept method, passing the visitor.
        // The item then calls the *correct* Visit method on the visitor based on its own type.
        foreach (var item in droppedItems)
        {
            item.Accept(inventoryVisitor); // item.Accept(visitor) internally calls visitor.Visit(this)
        }
        playerInventory.ListItems(); // Show what's now in the inventory.

        Debug.Log("\n--- PHASE 2: Describing Items using ItemDescriberVisitor ---");
        // Create an ItemDescriberVisitor. This visitor will store the generated description.
        ItemDescriberVisitor describerVisitor = new ItemDescriberVisitor();

        // Describe each item. Note how the same 'Accept' method is used, but a different
        // operation (description) is performed because a different visitor is provided.
        foreach (var item in droppedItems)
        {
            item.Accept(describerVisitor);
            Debug.Log($"Description for {item.ItemName}: \"{describerVisitor.Description}\"");
        }

        Debug.Log("\n--- PHASE 3: Using Items using ItemUseVisitor ---");
        // Simulate player taking damage to make healing relevant.
        playerStats.TakeDamage(40);

        // Create an ItemUseVisitor, giving it a reference to the player's stats.
        IItemVisitor useVisitor = new ItemUseVisitor(playerStats);

        // "Use" some items. Again, the same 'Accept' method performs different actions
        // based on the visitor and the item type.
        smallManaPotion.Accept(useVisitor); // Restores mana
        bigHealthPotion.Accept(useVisitor); // Heals HP
        magicSword.Accept(useVisitor);      // Equips the weapon

        Debug.Log($"\nFinal Player Stats: HP {playerStats.CurrentHealth}/{playerStats.MaxHealth}, MP {playerStats.CurrentMana}/{playerStats.MaxMana}, Equipped Weapon DMG: {playerStats.CurrentWeaponDamage}");


        Debug.Log("\n--- Key Takeaway & Extensibility of the Visitor Pattern ---");
        Debug.Log("   - **Benefit: To add a NEW OPERATION (Visitor):**");
        Debug.Log("     Simply create a new class (e.g., 'TraderPriceVisitor') that implements `IItemVisitor`.");
        Debug.Log("     You DO NOT need to modify the `IItem` interface or any of the existing `HealthPotionItem`, `ManaPotionItem`, or `WeaponItem` classes.");
        Debug.Log("     This is the primary advantage: new functionality without touching existing object structures.");
        Debug.Log("   - **Drawback: To add a NEW ELEMENT (Item Type):**");
        Debug.Log("     1. Create the new item class (e.g., `ArmorItem : MonoBehaviour, IItem`).");
        Debug.Log("     2. You MUST add a new `void Visit(ArmorItem armor);` method to the `IItemVisitor` interface.");
        Debug.Log("     3. Consequently, you MUST implement this new `Visit(ArmorItem)` method in ALL existing concrete visitors (e.g., `InventoryAdderVisitor`, `ItemUseVisitor`, `ItemDescriberVisitor`).");
        Debug.Log("     This is the main 'drawback': Adding new element types requires modifying the visitor interface and all concrete visitors.");
        Debug.Log("     Therefore, the Visitor pattern is most suitable when your object structure (the items) is relatively stable, but you frequently need to add new types of operations (visitors).");


        Debug.Log("\n--- Visitor Pattern Demo Finished ---");
    }
}
```