// Unity Design Pattern Example: VendingMachineSystem
// This script demonstrates the VendingMachineSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'VendingMachineSystem' design pattern using the **State Design Pattern**, which is highly suitable for systems whose behavior changes based on their internal state.

In this context:
*   **VendingMachine (Context):** The main MonoBehaviour that holds the current state and delegates user actions to it.
*   **IVendingMachineState (State Interface):** Defines the common operations for all possible states of the vending machine.
*   **Concrete States (IdleState, HasMoneyState, DispensingState):** Implement the `IVendingMachineState` interface, each encapsulating the behavior for a specific state. They also handle transitions to other states based on input.

---

```csharp
using UnityEngine;
using System.Collections.Generic; // For List
using System.Linq; // For LINQ operations like FirstOrDefault

// --- VendingMachineSystem Design Pattern Explanation (using State Pattern) ---
// The 'VendingMachineSystem' design pattern, as implemented here, leverages the
// STATE DESIGN PATTERN. This pattern allows an object (the VendingMachine) to
// alter its behavior when its internal state changes. The object will appear to
// change its class, as the responsibility for handling requests is delegated
// to the current state object.
//
// Why is the State Pattern ideal for a Vending Machine?
// A vending machine's operational flow is inherently state-driven:
// 1. Idle (Waiting for Money/Input): It only accepts money or returns 0 change.
// 2. Has Money (Waiting for Selection): It accepts more money, allows product selection, or returns change.
// 3. Dispensing: It processes the selected item, deducts money, updates stock, and calculates change.
// 4. (Potentially) Out of Stock, Maintenance, etc.
//
// The behavior for actions like "Insert Money" or "Select Product" changes
// dramatically depending on what state the machine is currently in. The State Pattern
// provides a clean and extensible way to manage this complexity, preventing large
// conditional statements in the VendingMachine class itself.
//
// Key Components of the State Pattern:
// 1. Context (VendingMachine): The main class that maintains an instance of a
//    ConcreteState subclass that defines the current state. It delegates state-specific
//    behavior to the current state object. It can change its state by setting
//    its current state object to a new one.
// 2. State (IVendingMachineState): An interface or abstract class that defines the
//    interface for encapsulating the behavior associated with a particular state of the Context.
// 3. Concrete States (IdleState, HasMoneyState, DispensingState): Each subclass
//    implements a behavior associated with a state of the Context. Each state handles
//    requests differently and may transition the Context to another state.

// --- 1. Product Data Structure ---
// Represents an item that can be sold by the vending machine.
[System.Serializable] // Makes this class visible and editable in the Unity Inspector
public class VendingMachineItem
{
    public string name;
    public float price;
    public int stock;

    public VendingMachineItem(string name, float price, int stock)
    {
        this.name = name;
        this.price = price;
        this.stock = stock;
    }

    public void DecreaseStock(int amount = 1)
    {
        stock = Mathf.Max(0, stock - amount); // Ensure stock doesn't go below zero
    }
}

// --- 2. State Interface ---
// Defines the common interface for all concrete state classes.
// Each method represents an action that can be performed on the vending machine.
public interface IVendingMachineState
{
    // Called when the vending machine enters this state. Useful for setup/UI updates.
    void EnterState(VendingMachine vendingMachine);

    // Called when the vending machine exits this state. Useful for cleanup.
    void ExitState(VendingMachine vendingMachine);

    // Handles inserting money into the machine.
    void InsertMoney(VendingMachine vendingMachine, float amount);

    // Handles selecting a product from the machine.
    void SelectProduct(VendingMachine vendingMachine, string productName);

    // Handles requesting to dispense the selected product.
    // (Note: In this implementation, dispensing is triggered by SelectProduct
    // when conditions are met, rather than a separate user action).
    void DispenseProduct(VendingMachine vendingMachine);

    // Handles requesting to return any inserted money (change).
    void ReturnChange(VendingMachine vendingMachine);

    // Provides a descriptive name for the current state (for debugging/UI).
    string GetStateName();
}

// --- 3. Concrete State Implementations ---

// Represents the state where the machine is waiting for initial money insertion
// or has just completed a transaction/returned change.
public class IdleState : IVendingMachineState
{
    public void EnterState(VendingMachine vendingMachine)
    {
        Debug.Log($"[VendingMachine] Entered {GetStateName()}. Current Balance: {vendingMachine.CurrentBalance:C2}");
        vendingMachine.ClearSelectedItem(); // No item should be selected in Idle state
        // Potentially update UI to show "Insert Money" or "Welcome"
    }

    public void ExitState(VendingMachine vendingMachine)
    {
        Debug.Log($"[VendingMachine] Exited {GetStateName()}.");
    }

    public void InsertMoney(VendingMachine vendingMachine, float amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[VendingMachine] Cannot insert zero or negative money.");
            return;
        }
        vendingMachine.AddBalance(amount);
        Debug.Log($"[VendingMachine] Inserted {amount:C2}. Current Balance: {vendingMachine.CurrentBalance:C2}");
        // Transition to HasMoneyState as soon as money is inserted
        vendingMachine.ChangeState(vendingMachine.hasMoneyState);
    }

    public void SelectProduct(VendingMachine vendingMachine, string productName)
    {
        Debug.LogWarning($"[VendingMachine] Please insert money first before selecting '{productName}'. Current Balance: {vendingMachine.CurrentBalance:C2}");
        // No state change, remains in IdleState
    }

    public void DispenseProduct(VendingMachine vendingMachine)
    {
        Debug.LogWarning("[VendingMachine] Cannot dispense. No product selected and no money inserted.");
        // No state change, remains in IdleState
    }

    public void ReturnChange(VendingMachine vendingMachine)
    {
        if (vendingMachine.CurrentBalance > 0)
        {
            Debug.Log($"[VendingMachine] Returning change: {vendingMachine.CurrentBalance:C2}.");
            vendingMachine.ClearBalance();
        }
        else
        {
            Debug.Log("[VendingMachine] No change to return. Balance is 0.");
        }
        // Always remain in IdleState or transition back to it if balance was cleared.
        // In this case, if currentBalance was 0, it means no actual change was returned.
        // If > 0, it became 0, so it's effectively back to an idle state.
        vendingMachine.ChangeState(vendingMachine.idleState);
    }

    public string GetStateName() => "IdleState (Waiting for Money/Input)";
}

// Represents the state where money has been inserted, and the machine is
// waiting for the user to select a product.
public class HasMoneyState : IVendingMachineState
{
    public void EnterState(VendingMachine vendingMachine)
    {
        Debug.Log($"[VendingMachine] Entered {GetStateName()}. Current Balance: {vendingMachine.CurrentBalance:C2}");
        // Potentially update UI to show "Select Product" and available balance
    }

    public void ExitState(VendingMachine vendingMachine)
    {
        Debug.Log($"[VendingMachine] Exited {GetStateName()}.");
    }

    public void InsertMoney(VendingMachine vendingMachine, float amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[VendingMachine] Cannot insert zero or negative money.");
            return;
        }
        vendingMachine.AddBalance(amount);
        Debug.Log($"[VendingMachine] Inserted {amount:C2}. Current Balance: {vendingMachine.CurrentBalance:C2}");
        // Remains in HasMoneyState as more money might be needed or user is just adding more.
    }

    public void SelectProduct(VendingMachine vendingMachine, string productName)
    {
        VendingMachineItem selectedItem = vendingMachine.GetItem(productName);

        if (selectedItem == null)
        {
            Debug.LogWarning($"[VendingMachine] Product '{productName}' not found. Current Balance: {vendingMachine.CurrentBalance:C2}");
            vendingMachine.ClearSelectedItem(); // Clear any previous selection
            return; // Stays in HasMoneyState
        }

        if (selectedItem.stock <= 0)
        {
            Debug.LogWarning($"[VendingMachine] Product '{productName}' is out of stock. Current Balance: {vendingMachine.CurrentBalance:C2}");
            vendingMachine.ClearSelectedItem();
            return; // Stays in HasMoneyState
        }

        if (vendingMachine.CurrentBalance >= selectedItem.price)
        {
            vendingMachine.SetSelectedItem(selectedItem);
            Debug.Log($"[VendingMachine] Selected '{selectedItem.name}' (Price: {selectedItem.price:C2}). Current Balance: {vendingMachine.CurrentBalance:C2}");
            // Sufficient funds, transition to DispensingState.
            vendingMachine.ChangeState(vendingMachine.dispensingState);
        }
        else
        {
            Debug.LogWarning($"[VendingMachine] Insufficient funds for '{selectedItem.name}'. Price: {selectedItem.price:C2}, Current Balance: {vendingMachine.CurrentBalance:C2}. Please insert {selectedItem.price - vendingMachine.CurrentBalance:C2} more.");
            vendingMachine.ClearSelectedItem();
            // Stays in HasMoneyState, waiting for more money or a different selection.
        }
    }

    public void DispenseProduct(VendingMachine vendingMachine)
    {
        Debug.LogWarning("[VendingMachine] Cannot dispense without a product being selected and paid for first.");
        // No state change, remains in HasMoneyState
    }

    public void ReturnChange(VendingMachine vendingMachine)
    {
        Debug.Log($"[VendingMachine] Returning change: {vendingMachine.CurrentBalance:C2}.");
        vendingMachine.ClearBalance();
        // Transition to IdleState after returning change.
        vendingMachine.ChangeState(vendingMachine.idleState);
    }

    public string GetStateName() => "HasMoneyState (Waiting for Selection)";
}

// Represents the state where a product has been selected and paid for,
// and the machine is currently processing the order (dispensing).
public class DispensingState : IVendingMachineState
{
    public void EnterState(VendingMachine vendingMachine)
    {
        Debug.Log($"[VendingMachine] Entered {GetStateName()}. Dispensing '{vendingMachine.SelectedItem.name}'.");
        // This state immediately handles dispensing logic and transitions.
        vendingMachine.DispenseSelectedProductInternal(); // Call internal method on Context
        
        // After dispensing, calculate and return any remaining change.
        this.ReturnChange(vendingMachine); // Calling ReturnChange from this state handles the final transition
    }

    public void ExitState(VendingMachine vendingMachine)
    {
        Debug.Log($"[VendingMachine] Exited {GetStateName()}.");
    }

    public void InsertMoney(VendingMachine vendingMachine, float amount)
    {
        Debug.LogWarning($"[VendingMachine] Cannot insert money while dispensing. Current Balance: {vendingMachine.CurrentBalance:C2}");
        // No state change, remains in DispensingState until dispensing is complete.
    }

    public void SelectProduct(VendingMachine vendingMachine, string productName)
    {
        Debug.LogWarning($"[VendingMachine] Cannot select product '{productName}' while dispensing.");
        // No state change, remains in DispensingState.
    }

    public void DispenseProduct(VendingMachine vendingMachine)
    {
        Debug.LogWarning("[VendingMachine] Product is already being dispensed or processed.");
        // No state change, remains in DispensingState.
    }

    public void ReturnChange(VendingMachine vendingMachine)
    {
        if (vendingMachine.CurrentBalance > 0)
        {
            Debug.Log($"[VendingMachine] Returning remaining change: {vendingMachine.CurrentBalance:C2}.");
            vendingMachine.ClearBalance();
        }
        else
        {
            Debug.Log("[VendingMachine] No remaining change to return.");
        }
        // Always transition to IdleState after dispensing and returning change.
        vendingMachine.ChangeState(vendingMachine.idleState);
    }

    public string GetStateName() => "DispensingState (Processing Order)";
}


// --- 4. Context (VendingMachine) Class ---
// This is the main MonoBehaviour class for the vending machine.
// It acts as the "Context" in the State Pattern, maintaining a reference to
// the current state object and delegating all state-dependent requests to it.
public class VendingMachine : MonoBehaviour
{
    [Header("Vending Machine Settings")]
    [Tooltip("List of products available in the vending machine.")]
    [SerializeField] private List<VendingMachineItem> availableItems = new List<VendingMachineItem>();

    [Header("Current Status (Read Only)")]
    [Tooltip("Current amount of money inserted by the user.")]
    [SerializeField] private float currentBalance = 0.0f;
    [Tooltip("The product currently selected by the user.")]
    [SerializeField] private VendingMachineItem selectedItem = null;
    [Tooltip("The name of the vending machine's current operational state.")]
    [SerializeField] private string currentStateName = "N/A"; // For Inspector visibility

    // Public properties to access current status safely from other scripts/UI.
    public float CurrentBalance => currentBalance;
    public VendingMachineItem SelectedItem => selectedItem;
    public string CurrentStateName => currentStateName;
    public List<VendingMachineItem> AvailableItems => availableItems; // Allow external scripts to see items

    // References to all concrete state instances.
    // These are initialized once and reused throughout the machine's lifetime.
    public IVendingMachineState idleState { get; private set; }
    public IVendingMachineState hasMoneyState { get; private set; }
    public IVendingMachineState dispensingState { get; private set; }

    private IVendingMachineState _currentState; // The actual current state object

    void Awake()
    {
        // Initialize all state objects.
        // They take the VendingMachine (this) as context when their methods are called.
        idleState = new IdleState();
        hasMoneyState = new HasMoneyState();
        dispensingState = new DispensingState();

        // Set the initial state of the vending machine.
        _currentState = idleState;
        _currentState.EnterState(this); // Call EnterState for initial setup
        UpdateCurrentStateName();       // Update for Inspector display
    }

    // --- Public API for User/UI Interaction ---
    // These methods are called by user input (e.g., UI buttons).
    // They simply delegate the action to the current state object, which knows
    // how to handle it based on the machine's current operational mode.

    public void InsertMoney(float amount)
    {
        _currentState.InsertMoney(this, amount);
        UpdateCurrentStateName(); // Update Inspector display after potential state change
    }

    public void SelectProduct(string productName)
    {
        _currentState.SelectProduct(this, productName);
        UpdateCurrentStateName(); // Update Inspector display
    }

    public void ReturnChange()
    {
        _currentState.ReturnChange(this);
        UpdateCurrentStateName(); // Update Inspector display
    }

    // --- Internal Vending Machine Logic (Called by States) ---
    // These methods provide the core functionality that different states might need to access
    // to modify the vending machine's internal data or perform actions.

    // Changes the current state of the vending machine.
    public void ChangeState(IVendingMachineState newState)
    {
        if (_currentState != null)
        {
            _currentState.ExitState(this); // Notify the old state it's being exited
        }
        _currentState = newState;
        _currentState.EnterState(this);    // Notify the new state it's being entered
        UpdateCurrentStateName();          // Keep Inspector updated
    }

    // Adds money to the current balance.
    public void AddBalance(float amount)
    {
        currentBalance += amount;
        Debug.Log($"[VendingMachine] Balance increased. Current balance: {currentBalance:C2}");
    }

    // Clears the current balance, typically after dispensing or returning change.
    public void ClearBalance()
    {
        currentBalance = 0.0f;
        Debug.Log("[VendingMachine] Balance cleared.");
    }

    // Sets the currently selected product.
    public void SetSelectedItem(VendingMachineItem item)
    {
        selectedItem = item;
    }

    // Clears the currently selected product.
    public void ClearSelectedItem()
    {
        selectedItem = null;
    }

    // Retrieves an item by its name from the available items list.
    public VendingMachineItem GetItem(string name)
    {
        // Using LINQ's FirstOrDefault for a concise search.
        // It returns the first item that matches, or null if no item is found.
        return availableItems.FirstOrDefault(item => item.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    // Internal method to handle the actual dispensing of the product.
    // This is explicitly called by the DispensingState when an item is ready to be delivered.
    public void DispenseSelectedProductInternal()
    {
        if (selectedItem != null)
        {
            currentBalance -= selectedItem.price; // Deduct the price of the item
            selectedItem.DecreaseStock();        // Decrease the stock count
            Debug.Log($"[VendingMachine] Dispensed '{selectedItem.name}'. Remaining stock: {selectedItem.stock}.");

            // No need to clear selected item here, as the DispensingState will ensure
            // the machine transitions back to IdleState where selectedItem is cleared.
        }
        else
        {
            Debug.LogError("[VendingMachine] Attempted to dispense without a valid selected item!");
        }
    }

    // Helper method to update the state name displayed in the Inspector.
    private void UpdateCurrentStateName()
    {
        currentStateName = _currentState?.GetStateName() ?? "Unknown";
    }


    // --- Example Usage in Unity ---
    // You can attach this script to an empty GameObject in your scene.
    // Configure 'Available Items' in the Inspector.
    // The Start() method below demonstrates various scenarios.
    void Start()
    {
        // Optional: Initialize some default items if the list is empty in the Inspector.
        if (availableItems.Count == 0)
        {
            availableItems.Add(new VendingMachineItem("Soda", 1.50f, 5));
            availableItems.Add(new VendingMachineItem("Chips", 1.00f, 10));
            availableItems.Add(new VendingMachineItem("Candy", 0.75f, 3));
            availableItems.Add(new VendingMachineItem("Water", 2.00f, 0)); // Out of stock example
            Debug.Log("[VendingMachine] Default items added for demonstration (if list was empty).");
        }

        Debug.Log("\n--- Vending Machine Demonstration ---");

        // --- Scenario 1: Successful Purchase ---
        Debug.Log("\n--- Scenario 1: Successful Purchase (Soda for $1.50, inserted $2.00) ---");
        InsertMoney(2.00f); // Machine goes to HasMoneyState
        SelectProduct("Soda"); // Machine goes to DispensingState, then back to IdleState, returns $0.50 change.

        // --- Scenario 2: Insufficient Funds, then top-up and purchase ---
        Debug.Log("\n--- Scenario 2: Insufficient Funds (Chips for $1.00, inserted $0.50 then $1.00) ---");
        InsertMoney(0.50f); // HasMoneyState ($0.50)
        SelectProduct("Chips"); // Stays in HasMoneyState (warns: need $0.50 more)
        InsertMoney(1.00f); // HasMoneyState (total $1.50)
        SelectProduct("Chips"); // DispensingState, then IdleState, returns $0.50 change.

        // --- Scenario 3: Product Out of Stock ---
        Debug.Log("\n--- Scenario 3: Product Out of Stock (Water) ---");
        InsertMoney(5.00f); // HasMoneyState ($5.00)
        SelectProduct("Water"); // Stays in HasMoneyState (warns: out of stock)
        ReturnChange(); // Returns $5.00, goes to IdleState

        // --- Scenario 4: Return Change Early ---
        Debug.Log("\n--- Scenario 4: Return Change Early (Inserted $3.00, then requested change) ---");
        InsertMoney(3.00f); // HasMoneyState ($3.00)
        ReturnChange(); // Returns $3.00, goes to IdleState

        // --- Scenario 5: Invalid Product Selection ---
        Debug.Log("\n--- Scenario 5: Invalid Product Selection (Coffee) ---");
        InsertMoney(1.00f); // HasMoneyState ($1.00)
        SelectProduct("Coffee"); // Stays in HasMoneyState (warns: not found)
        ReturnChange(); // Returns $1.00, goes to IdleState

        // --- Scenario 6: Attempts in wrong state (demonstrates state-dependent behavior) ---
        Debug.Log("\n--- Scenario 6: Attempts in wrong state (e.g., selecting product while idle) ---");
        SelectProduct("Candy"); // IdleState, warns "Please insert money first"
        // (DispenseProduct is not a public user action, it's internal to the state logic)
        InsertMoney(0.10f); // HasMoneyState
        SelectProduct("Candy"); // DispensingState, then IdleState (returns $0.10 - $0.75 = -$0.65? No, the logic handles this correctly: only dispenses if enough money).
                                // Correction: Candy is $0.75, Inserted $0.10, so it will warn about insufficient funds.
        InsertMoney(1.00f); // HasMoneyState (total $1.10)
        SelectProduct("Candy"); // DispensingState, then IdleState (cost $0.75, returns $0.35 change)
    }

    // --- Example of how UI buttons might call these methods ---
    // You would connect these methods to Unity UI Button's OnClick() events.

    // Example for a coin button (e.g., a "Insert $1.00" button)
    public void OnInsertCoinClicked(float coinValue)
    {
        InsertMoney(coinValue);
        // You would typically update UI elements here, like a balance display.
    }

    // Example for a product button (e.g., a "Buy Soda" button)
    public void OnProductButtonClicked(string productName)
    {
        SelectProduct(productName);
        // Update UI elements like messages ("Dispensing...") or item list.
    }

    // Example for a "Return Change" button
    public void OnReturnChangeButtonClicked()
    {
        ReturnChange();
        // Update UI elements like displaying change returned message.
    }
}
```