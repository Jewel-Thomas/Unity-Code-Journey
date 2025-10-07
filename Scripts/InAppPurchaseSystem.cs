// Unity Design Pattern Example: InAppPurchaseSystem
// This script demonstrates the InAppPurchaseSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'InAppPurchaseSystem' design pattern, while not a classical Gang of Four pattern, represents a common and highly effective architectural approach in game development. It centralizes all In-App Purchase (IAP) logic, provides a clean API for other parts of the game, and uses an event-driven mechanism to decouple the purchase process from UI and game logic.

This implementation combines:
1.  **Singleton:** Ensures only one instance of the IAP system exists, providing a global access point.
2.  **Facade:** Simplifies the complex underlying IAP operations into a clean, easy-to-use interface.
3.  **Observer/Event-driven:** Uses C# events (Actions) to notify other systems about initialization status, purchase success, or failure, promoting loose coupling.
4.  **ScriptableObject:** Allows designers to define IAP products directly in the Unity Editor without touching code.

---

### **1. IAPProductData (ScriptableObject)**

This `ScriptableObject` defines the structure for an In-App Purchase product. You'll create these assets directly in your Unity project to configure your IAP items.

**File: `IAPProductData.cs`**

```csharp
using UnityEngine;
using System;

/// <summary>
/// Defines the type of an In-App Purchase product.
/// </summary>
public enum ProductType
{
    Consumable,      // Can be purchased multiple times (e.g., coins, gems)
    NonConsumable,   // Purchased once (e.g., remove ads, unlock character)
    Subscription     // Recurring purchase (e.g., premium pass)
}

/// <summary>
/// A ScriptableObject representing a single In-App Purchase product.
/// This allows designers to define IAP items directly in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewIAPProduct", menuName = "In-App Purchase/IAP Product Data")]
public class IAPProductData : ScriptableObject
{
    [Tooltip("The unique identifier for this product, matching your store setup (e.g., com.yourcompany.game.productid).")]
    public string productID;

    [Tooltip("The type of this product (Consumable, NonConsumable, Subscription).")]
    public ProductType productType;

    [Header("Display Information (Simulated)")]
    [Tooltip("The display name of the product.")]
    public string displayName;

    [Tooltip("A short description of the product.")]
    [TextArea(3, 5)]
    public string description;

    [Tooltip("The base price for this product (simulated, real IAP would fetch localized price).")]
    public float basePrice;

    // In a real Unity IAP setup, you would have properties here to store
    // actual product metadata fetched from the store (e.g., localized price, currency code).
    // For this example, we're using basePrice as a placeholder.

    /// <summary>
    /// Validates the product data.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(productID);
    }
}
```

---

### **2. InAppPurchaseSystem (Singleton MonoBehaviour)**

This is the core of the system. It handles initialization, purchasing, and event dispatching.

**File: `InAppPurchaseSystem.cs`**

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections; // For Coroutines

/// <summary>
/// The core In-App Purchase System implemented as a Singleton.
/// This centralizes all IAP logic, provides a clean API, and uses events
/// to notify other systems about purchase outcomes, promoting loose coupling.
///
/// Pattern: Singleton, Facade, Observer/Event-driven.
/// </summary>
public class InAppPurchaseSystem : MonoBehaviour
{
    // --- Singleton Implementation ---
    /// <summary>
    /// Static instance of the InAppPurchaseSystem, ensuring only one exists.
    /// </summary>
    public static InAppPurchaseSystem Instance { get; private set; }

    // --- Public Events (Observer Pattern) ---
    // Other scripts can subscribe to these events to react to IAP changes.
    // This decouples the IAP system from specific UI or game logic.

    /// <summary>
    /// Fired when the IAP system has successfully initialized.
    /// </summary>
    public static event Action OnInitSuccess;

    /// <summary>
    /// Fired when the IAP system fails to initialize.
    /// Parameters: (errorMessage)
    /// </summary>
    public static event Action<string> OnInitFailure;

    /// <summary>
    /// Fired when a product purchase is successful.
    /// Parameters: (purchasedProductData)
    /// </summary>
    public static event Action<IAPProductData> OnPurchaseSuccess;

    /// <summary>
    /// Fired when a product purchase fails.
    /// Parameters: (productData, errorMessage)
    /// </summary>
    public static event Action<IAPProductData, string> OnPurchaseFailed;

    /// <summary>
    /// Fired when a purchase process has started and is pending.
    /// This can be used to show a loading indicator.
    /// Parameters: (productData)
    /// </summary>
    public static event Action<IAPProductData> OnPurchasePending;

    /// <summary>
    /// Fired when non-consumable purchases are successfully restored.
    /// </summary>
    public static event Action OnRestorePurchasesSuccess;

    /// <summary>
    /// Fired when restoring purchases fails.
    /// Parameters: (errorMessage)
    /// </summary>
    public static event Action<string> OnRestorePurchasesFailure;


    // --- Internal State ---
    private bool _isInitialized = false;
    private Dictionary<string, IAPProductData> _productsById = new Dictionary<string, IAPProductData>();
    private HashSet<string> _purchasedNonConsumables = new HashSet<string>(); // Tracks non-consumables owned by the player

    [Header("Simulation Settings")]
    [Tooltip("Simulate network delays for initialization and purchases.")]
    [SerializeField] private float _simulationDelay = 1.5f;
    [Tooltip("Chance of a simulated purchase failing (0-1).")]
    [SerializeField, Range(0f, 1f)] private float _simulatedPurchaseFailureChance = 0.2f;


    // --- MonoBehaviour Lifecycle Methods ---

    private void Awake()
    {
        // Enforce Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("InAppPurchaseSystem: Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Public API (Facade Pattern) ---
    // These methods provide a clean, simple interface for other systems to interact with IAP.

    /// <summary>
    /// Initializes the In-App Purchase System with a list of products.
    /// In a real scenario, this would involve UnityPurchasing.Initialize().
    /// </summary>
    /// <param name="productsToRegister">A list of IAPProductData ScriptableObjects to register.</param>
    public void Initialize(List<IAPProductData> productsToRegister)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("InAppPurchaseSystem is already initialized.");
            OnInitSuccess?.Invoke(); // If already initialized, just report success.
            return;
        }

        Debug.Log("InAppPurchaseSystem: Starting initialization...");
        _isInitialized = false;
        _productsById.Clear();

        foreach (var product in productsToRegister)
        {
            if (product != null && product.IsValid())
            {
                _productsById[product.productID] = product;
                Debug.Log($"InAppPurchaseSystem: Registered product: {product.productID} ({product.displayName})");
            }
            else
            {
                Debug.LogWarning($"InAppPurchaseSystem: Skipping invalid product data: {product?.name ?? "NULL"}");
            }
        }

        // Simulate initialization process (e.g., connecting to store)
        StartCoroutine(SimulateInitialization());
    }

    /// <summary>
    /// Initiates a purchase for a specific product ID.
    /// </summary>
    /// <param name="productID">The unique identifier of the product to purchase.</param>
    public void BuyProduct(string productID)
    {
        if (!_isInitialized)
        {
            Debug.LogError("InAppPurchaseSystem: Cannot buy product. System is not initialized.");
            OnPurchaseFailed?.Invoke(GetProductInfo(productID), "IAP system not initialized.");
            return;
        }

        if (!_productsById.TryGetValue(productID, out IAPProductData productData))
        {
            Debug.LogError($"InAppPurchaseSystem: Product with ID '{productID}' not found.");
            OnPurchaseFailed?.Invoke(null, $"Product '{productID}' not registered.");
            return;
        }

        Debug.Log($"InAppPurchaseSystem: Attempting to buy product: {productData.displayName} ({productID})");
        OnPurchasePending?.Invoke(productData); // Notify UI that purchase is pending

        // Simulate purchase process
        StartCoroutine(SimulatePurchase(productData));
    }

    /// <summary>
    /// Attempts to restore previously purchased non-consumable products.
    /// </summary>
    public void RestorePurchases()
    {
        if (!_isInitialized)
        {
            Debug.LogError("InAppPurchaseSystem: Cannot restore purchases. System is not initialized.");
            OnRestorePurchasesFailure?.Invoke("IAP system not initialized.");
            return;
        }

        Debug.Log("InAppPurchaseSystem: Attempting to restore purchases...");
        StartCoroutine(SimulateRestorePurchases());
    }

    /// <summary>
    /// Retrieves the IAPProductData for a given product ID.
    /// </summary>
    /// <param name="productID">The unique identifier of the product.</param>
    /// <returns>The IAPProductData if found, otherwise null.</returns>
    public IAPProductData GetProductInfo(string productID)
    {
        _productsById.TryGetValue(productID, out IAPProductData productData);
        return productData;
    }

    /// <summary>
    /// Checks if a non-consumable product has been purchased.
    /// (Consumables are not tracked as "purchased" by the system, but consumed by game logic).
    /// </summary>
    /// <param name="productID">The unique identifier of the product.</param>
    /// <returns>True if the product is a non-consumable and has been purchased, false otherwise.</returns>
    public bool IsProductPurchased(string productID)
    {
        if (_productsById.TryGetValue(productID, out IAPProductData productData))
        {
            if (productData.productType == ProductType.NonConsumable || productData.productType == ProductType.Subscription)
            {
                return _purchasedNonConsumables.Contains(productID);
            }
        }
        return false;
    }

    /// <summary>
    /// Gets a list of all registered IAP products.
    /// </summary>
    public List<IAPProductData> GetAllRegisteredProducts()
    {
        return new List<IAPProductData>(_productsById.Values);
    }

    // --- Internal Simulation Methods ---
    // These methods simulate the asynchronous nature and outcomes of real IAP operations.

    private IEnumerator SimulateInitialization()
    {
        yield return new WaitForSeconds(_simulationDelay);

        // Simulate random initialization failure
        if (UnityEngine.Random.value < 0.1f) // 10% chance of failure
        {
            Debug.LogError("InAppPurchaseSystem: Simulated initialization FAILED.");
            OnInitFailure?.Invoke("Failed to connect to store services.");
            _isInitialized = false;
        }
        else
        {
            _isInitialized = true;
            Debug.Log("InAppPurchaseSystem: Simulated initialization SUCCESS.");
            OnInitSuccess?.Invoke();

            // In a real system, you'd load previously purchased non-consumables here
            // For simulation, we'll just add a couple as if they were already owned.
            SimulateLoadExistingPurchases();
        }
    }

    private IEnumerator SimulatePurchase(IAPProductData productData)
    {
        yield return new WaitForSeconds(_simulationDelay);

        // Simulate random purchase success/failure
        if (UnityEngine.Random.value < _simulatedPurchaseFailureChance)
        {
            Debug.LogWarning($"InAppPurchaseSystem: Simulated purchase FAILED for {productData.displayName} ({productData.productID}).");
            OnPurchaseFailed?.Invoke(productData, "Simulated purchase failed (e.g., user cancelled, network error).");
        }
        else
        {
            Debug.Log($"InAppPurchaseSystem: Simulated purchase SUCCESS for {productData.displayName} ({productData.productID}).");
            
            // For non-consumables and subscriptions, mark them as purchased
            if (productData.productType == ProductType.NonConsumable || productData.productType == ProductType.Subscription)
            {
                _purchasedNonConsumables.Add(productData.productID);
                Debug.Log($"InAppPurchaseSystem: Non-consumable/Subscription '{productData.productID}' marked as purchased.");
            }
            
            OnPurchaseSuccess?.Invoke(productData);
        }
    }

    private IEnumerator SimulateRestorePurchases()
    {
        yield return new WaitForSeconds(_simulationDelay);

        // Simulate random restore failure
        if (UnityEngine.Random.value < 0.15f) // 15% chance of failure
        {
            Debug.LogError("InAppPurchaseSystem: Simulated restore purchases FAILED.");
            OnRestorePurchasesFailure?.Invoke("Failed to connect to store to restore purchases.");
        }
        else
        {
            Debug.Log("InAppPurchaseSystem: Simulated restore purchases SUCCESS.");
            // In a real system, you would get a list of product IDs from the store
            // For simulation, we'll just add some known non-consumables
            _purchasedNonConsumables.Add("com.yourcompany.game.removeads");
            _purchasedNonConsumables.Add("com.yourcompany.game.proversion");
            Debug.Log("InAppPurchaseSystem: Simulated non-consumables added to owned list: 'removeads', 'proversion'.");
            OnRestorePurchasesSuccess?.Invoke();
        }
    }

    private void SimulateLoadExistingPurchases()
    {
        // This is where you'd load actual saved purchase data (e.g., from PlayerPrefs, backend, or Unity IAP's initialization callback)
        // For this example, let's just pretend some are already owned.
        Debug.Log("InAppPurchaseSystem: Simulating loading existing purchases.");
        _purchasedNonConsumables.Add("com.yourcompany.game.proversion");
    }
}
```

---

### **3. GameManager (Example Usage)**

This script demonstrates how you would integrate and use the `InAppPurchaseSystem` in your game.

**File: `GameManager.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Example GameManager that demonstrates how to use the InAppPurchaseSystem.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("IAP Configuration")]
    [Tooltip("Drag all your IAPProductData ScriptableObjects here.")]
    public List<IAPProductData> allIAPProducts;

    private void Awake()
    {
        // 1. Subscribe to InAppPurchaseSystem events
        // This is crucial for reacting to IAP outcomes without direct dependencies.
        InAppPurchaseSystem.OnInitSuccess += HandleIAPInitSuccess;
        InAppPurchaseSystem.OnInitFailure += HandleIAPInitFailure;
        InAppPurchaseSystem.OnPurchaseSuccess += HandlePurchaseSuccess;
        InAppPurchaseSystem.OnPurchaseFailed += HandlePurchaseFailed;
        InAppPurchaseSystem.OnPurchasePending += HandlePurchasePending;
        InAppPurchaseSystem.OnRestorePurchasesSuccess += HandleRestorePurchasesSuccess;
        InAppPurchaseSystem.OnRestorePurchasesFailure += HandleRestorePurchasesFailure;
    }

    private void Start()
    {
        // 2. Initialize the InAppPurchaseSystem
        // This should typically happen early in your game's lifecycle.
        if (InAppPurchaseSystem.Instance != null)
        {
            InAppPurchaseSystem.Instance.Initialize(allIAPProducts);
        }
        else
        {
            Debug.LogError("GameManager: InAppPurchaseSystem.Instance is null. Make sure it's in your scene and set up as a Singleton.");
        }
    }

    private void OnDestroy()
    {
        // 3. Unsubscribe from events to prevent memory leaks and unexpected behavior
        InAppPurchaseSystem.OnInitSuccess -= HandleIAPInitSuccess;
        InAppPurchaseSystem.OnInitFailure -= HandleIAPInitFailure;
        InAppPurchaseSystem.OnPurchaseSuccess -= HandlePurchaseSuccess;
        InAppPurchaseSystem.OnPurchaseFailed -= HandlePurchaseFailed;
        InAppPurchaseSystem.OnPurchasePending -= HandlePurchasePending;
        InAppPurchaseSystem.OnRestorePurchasesSuccess -= HandleRestorePurchasesSuccess;
        InAppPurchaseSystem.OnRestorePurchasesFailure -= HandleRestorePurchasesFailure;
    }

    // --- Event Handlers ---

    private void HandleIAPInitSuccess()
    {
        Debug.Log("<color=green>GameManager: IAP System Initialized Successfully!</color>");
        // Now you can display product info, enable IAP buttons, etc.
        DisplayProductInformation();
    }

    private void HandleIAPInitFailure(string errorMessage)
    {
        Debug.LogError($"<color=red>GameManager: IAP System Initialization FAILED: {errorMessage}</color>");
        // Disable IAP buttons, show an error message to the user.
    }

    private void HandlePurchaseSuccess(IAPProductData productData)
    {
        Debug.Log($"<color=green>GameManager: Purchase SUCCESS for '{productData.displayName}'!</color>");
        // Grant the item/feature to the player
        GrantProductToPlayer(productData);
        // Update UI (e.g., disable 'Remove Ads' button if purchased)
        UpdateUIForProduct(productData);
    }

    private void HandlePurchaseFailed(IAPProductData productData, string errorMessage)
    {
        string productName = productData != null ? productData.displayName : "Unknown Product";
        Debug.LogWarning($"<color=red>GameManager: Purchase FAILED for '{productName}': {errorMessage}</color>");
        // Inform the user, hide loading indicators.
    }

    private void HandlePurchasePending(IAPProductData productData)
    {
        Debug.Log($"<color=yellow>GameManager: Purchase PENDING for '{productData.displayName}'...</color>");
        // Show a loading spinner or "processing" message.
    }

    private void HandleRestorePurchasesSuccess()
    {
        Debug.Log("<color=green>GameManager: Purchases Restored Successfully!</color>");
        // After restoring, re-check all non-consumables and update game state/UI
        foreach (var product in allIAPProducts)
        {
            if (product.productType == ProductType.NonConsumable || product.productType == ProductType.Subscription)
            {
                if (InAppPurchaseSystem.Instance.IsProductPurchased(product.productID))
                {
                    Debug.Log($"GameManager: Detected previously owned: {product.displayName}");
                    GrantProductToPlayer(product); // Re-grant if necessary
                    UpdateUIForProduct(product);
                }
            }
        }
    }

    private void HandleRestorePurchasesFailure(string errorMessage)
    {
        Debug.LogError($"<color=red>GameManager: Restore Purchases FAILED: {errorMessage}</color>");
        // Inform the user.
    }

    // --- Game Logic Related to IAP ---

    private void GrantProductToPlayer(IAPProductData productData)
    {
        switch (productData.productType)
        {
            case ProductType.Consumable:
                Debug.Log($"Granting {productData.displayName} to player (e.g., add {productData.basePrice * 10} coins).");
                // Example: PlayerStats.Instance.AddCoins(productData.basePrice * 10);
                break;
            case ProductType.NonConsumable:
                Debug.Log($"Unlocking permanent feature: {productData.displayName} (e.g., remove ads, new character).");
                // Example: PlayerUnlockables.Instance.UnlockFeature(productData.productID);
                break;
            case ProductType.Subscription:
                Debug.Log($"Activating subscription: {productData.displayName} (e.g., premium access for a month).");
                // Example: PlayerSubscriptions.Instance.ActivateSubscription(productData.productID, duration);
                break;
        }
    }

    private void UpdateUIForProduct(IAPProductData productData)
    {
        Debug.Log($"Updating UI for product: {productData.displayName}.");
        // Example: If 'remove ads' was purchased, hide the 'Remove Ads' button.
        if (productData.productID == "com.yourcompany.game.removeads" && InAppPurchaseSystem.Instance.IsProductPurchased(productData.productID))
        {
            Debug.Log("UI: 'Remove Ads' button should now be hidden/disabled.");
        }
        // Refresh product listings in shop UI if necessary.
    }

    private void DisplayProductInformation()
    {
        Debug.Log("\n--- Available IAP Products ---");
        foreach (var product in InAppPurchaseSystem.Instance.GetAllRegisteredProducts())
        {
            string purchaseStatus = InAppPurchaseSystem.Instance.IsProductPurchased(product.productID) ? "(OWNED)" : "";
            Debug.Log($"- {product.displayName} ({product.productID}): ${product.basePrice:F2} {purchaseStatus}\n  Description: {product.description}");
        }
        Debug.Log("----------------------------\n");
    }

    // --- Example UI Button Calls (for a UI Manager or specific button script) ---

    public void OnClickBuyCoinsPack1()
    {
        // Example of calling the purchase method from a UI button
        string productID = "com.yourcompany.game.coinpack1";
        if (InAppPurchaseSystem.Instance != null)
        {
            InAppPurchaseSystem.Instance.BuyProduct(productID);
        }
    }

    public void OnClickBuyRemoveAds()
    {
        string productID = "com.yourcompany.game.removeads";
        if (InAppPurchaseSystem.Instance != null)
        {
            if (InAppPurchaseSystem.Instance.IsProductPurchased(productID))
            {
                Debug.Log("GameManager: 'Remove Ads' is already purchased.");
                // Optionally show a message to the user
            }
            else
            {
                InAppPurchaseSystem.Instance.BuyProduct(productID);
            }
        }
    }

    public void OnClickRestorePurchases()
    {
        if (InAppPurchaseSystem.Instance != null)
        {
            InAppPurchaseSystem.Instance.RestorePurchases();
        }
    }
}
```

---

### **How to Set Up in Unity**

1.  **Create C# Scripts:**
    *   Create a new C# script named `IAPProductData.cs` and paste the first code block into it.
    *   Create a new C# script named `InAppPurchaseSystem.cs` and paste the second code block into it.
    *   Create a new C# script named `GameManager.cs` and paste the third code block into it.

2.  **Create IAP Product Assets:**
    *   In Unity, go to `Assets -> Create -> In-App Purchase -> IAP Product Data`.
    *   Create several of these assets (e.g., `CoinPack1Data`, `RemoveAdsData`, `ProVersionData`).
    *   Select each asset and fill in its details in the Inspector:
        *   **Product ID:** This is crucial. Use unique identifiers like `com.yourcompany.game.coinpack1`, `com.yourcompany.game.removeads`, `com.yourcompany.game.proversion`.
        *   **Product Type:** Set to `Consumable`, `NonConsumable`, or `Subscription`.
        *   **Display Name:** e.g., "1000 Coins", "Remove Ads", "Pro Version".
        *   **Description:** e.g., "Get 1000 coins instantly!", "Permanently remove all ads.", "Unlock exclusive features."
        *   **Base Price:** e.g., `0.99`, `4.99`, `9.99`.

3.  **Create `InAppPurchaseSystem` GameObject:**
    *   Create an empty GameObject in your first scene (e.g., `_Managers`).
    *   Rename it to `InAppPurchaseSystem`.
    *   Attach the `InAppPurchaseSystem.cs` script to this GameObject.
    *   (Optional) Adjust `Simulation Delay` and `Simulated Purchase Failure Chance` in the Inspector.

4.  **Create `GameManager` GameObject:**
    *   Create another empty GameObject in your scene.
    *   Rename it to `GameManager`.
    *   Attach the `GameManager.cs` script to this GameObject.
    *   In the Inspector for `GameManager`, drag all the `IAPProductData` assets you created into the `All IAP Products` list.

5.  **Run the Scene:**
    *   Play your Unity scene.
    *   Watch the Console window for output from `InAppPurchaseSystem` and `GameManager`.
    *   You'll see the initialization process, product listings, and simulated purchase attempts.

6.  **Simulate UI Interaction (Optional):**
    *   Create a simple UI Canvas.
    *   Add a few Buttons to it (e.g., "Buy Coins", "Remove Ads", "Restore Purchases").
    *   On each Button's `OnClick()` event, drag the `GameManager` GameObject.
    *   Select the corresponding public method from `GameManager` (e.g., `OnClickBuyCoinsPack1`, `OnClickBuyRemoveAds`, `OnClickRestorePurchases`).

Now, when you run the scene and click these buttons, you'll trigger the simulated IAP flow, and the console will log the outcomes based on the event subscriptions in `GameManager`. This provides a complete, working, and educational example of an 'InAppPurchaseSystem' design pattern in Unity.