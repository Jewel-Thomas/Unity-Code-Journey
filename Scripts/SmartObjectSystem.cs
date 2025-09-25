// Unity Design Pattern Example: SmartObjectSystem
// This script demonstrates the SmartObjectSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SmartObjectSystem' design pattern aims to centralize the intelligence and interactive logic of objects in a game world. Instead of scattering `if-else` logic across many individual scripts, this pattern allows you to define object behavior declaratively using conditions and actions. An object, a "SmartObject," evaluates a set of predefined "Interactions." Each interaction has "Conditions" (what must be true for it to be available) and "Actions" (what happens when it's performed). This makes objects more reusable, their logic easier to understand, and simplifies adding new behaviors without extensive refactoring.

This example demonstrates the SmartObjectSystem by creating a `SmartDoor` that can be locked/unlocked and opened/closed, potentially requiring a key. A `PlayerAgent` interacts with it.

---

**To use this example in Unity:**

1.  Create a new C# script named `SmartObjectSystemExample.cs`.
2.  Copy and paste the entire code below into this new script.
3.  **Setup Scene:**
    *   Create an empty GameObject named "SmartDoor."
    *   Add a `Box Collider` to "SmartDoor" (make it a Trigger).
    *   Add an `Animator` component to "SmartDoor."
    *   Create a simple Animation for "OpenDoor" (e.g., rotate on Y-axis) and another for "CloseDoor." Make sure the `Animator Controller` has "Open" and "Close" `Trigger` parameters.
    *   Add the `SmartDoor` component to the "SmartDoor" GameObject.
    *   In the Inspector for `SmartDoor`:
        *   **Initial States:**
            *   Add two states: `Key="IsLocked", Value=true` and `Key="IsOpened", Value=false`.
        *   **Available Interactions:**
            *   **Interaction 1: "Unlock Door"**
                *   *Conditions:*
                    *   `HasInventoryItemCondition`: `Item Name="Key"`
                    *   `ObjectStateCondition`: `State Key="IsLocked"`, `Required Value=true`
                *   *Actions:*
                    *   `RemoveInventoryItemAction`: `Item Name="Key"`
                    *   `ChangeObjectStateAction`: `State Key="IsLocked"`, `New Value=false`
                    *   `PlaySoundAction`: (Assign an Audio Clip for unlocking)
                    *   `DebugLogAction`: `Message="Door unlocked!"`
            *   **Interaction 2: "Lock Door"**
                *   *Conditions:*
                    *   `ObjectStateCondition`: `State Key="IsLocked"`, `Required Value=false`
                    *   `ObjectStateCondition`: `State Key="IsOpened"`, `Required Value=false` (Cannot lock an open door)
                *   *Actions:*
                    *   `ChangeObjectStateAction`: `State Key="IsLocked"`, `New Value=true`
                    *   `PlaySoundAction`: (Assign an Audio Clip for locking)
                    *   `DebugLogAction`: `Message="Door locked!"`
            *   **Interaction 3: "Open Door"**
                *   *Conditions:*
                    *   `ObjectStateCondition`: `State Key="IsLocked"`, `Required Value=false`
                    *   `ObjectStateCondition`: `State Key="IsOpened"`, `Required Value=false`
                *   *Actions:*
                    *   `AnimateObjectAction`: `Trigger Name="Open"`
                    *   `ChangeObjectStateAction`: `State Key="IsOpened"`, `New Value=true`
                    *   `PlaySoundAction`: (Assign an Audio Clip for opening)
                    *   `DebugLogAction`: `Message="Door opened!"`
            *   **Interaction 4: "Close Door"**
                *   *Conditions:*
                    *   `ObjectStateCondition`: `State Key="IsOpened"`, `Required Value=true`
                *   *Actions:*
                    *   `AnimateObjectAction`: `Trigger Name="Close"`
                    *   `ChangeObjectStateAction`: `State Key="IsOpened"`, `New Value=false`
                    *   `PlaySoundAction`: (Assign an Audio Clip for closing)
                    *   `DebugLogAction`: `Message="Door closed!"`

    *   Create a Cube or Capsule GameObject named "Player."
    *   Add a `Rigidbody` to "Player" (optional, but good for physics interactions).
    *   Add the `PlayerAgent` component to the "Player" GameObject.
    *   In the Inspector for `PlayerAgent`:
        *   **Inventory Items:** Add one item: `Key="Key"`.
        *   **Interaction Range:** Set to a reasonable value (e.g., 3).
        *   Assign Audio Sources for interaction sounds.

4.  **Audio (Optional but Recommended):**
    *   Create an `AudioSource` component on the "SmartDoor" GameObject.
    *   Create an `AudioSource` component on the "Player" GameObject.
    *   Assign appropriate `.wav` or `.mp3` files for unlock, lock, open, close sounds in the `SmartDoor`'s interaction actions and the `PlayerAgent`'s interaction sound.

5.  Run the scene. Move the "Player" close to the "SmartDoor" and press `E`. Observe the debug logs and door behavior as you interact. The available interactions will change based on the door's state and the player's inventory.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SmartObjectSystem
{
    // =====================================================================================
    // CORE SMART OBJECT SYSTEM COMPONENTS
    // These define the fundamental structure of the SmartObjectSystem.
    // =====================================================================================

    /// <summary>
    /// Represents the context of an agent (e.g., player, NPC) interacting with a SmartObject.
    /// This context is passed to conditions and actions, allowing them to make decisions
    /// and modify the agent's state (e.g., inventory).
    /// </summary>
    [System.Serializable]
    public class AgentContext
    {
        public Transform AgentTransform;
        public List<string> InventoryItems = new List<string>();
        public AudioSource AgentAudioSource; // Agent's audio source for playing sounds

        public AgentContext(Transform agentTransform, List<string> inventoryItems, AudioSource audioSource = null)
        {
            AgentTransform = agentTransform;
            InventoryItems = inventoryItems;
            AgentAudioSource = audioSource;
        }
    }

    /// <summary>
    /// Base interface for any condition that needs to be evaluated for a SmartInteraction.
    /// </summary>
    public interface ISmartCondition
    {
        bool Evaluate(AgentContext agent, SmartObject owner);
    }

    /// <summary>
    /// Abstract base class for creating serializable conditions.
    /// Provides a description field for better editor readability.
    /// </summary>
    [System.Serializable]
    public abstract class SmartConditionBase : ISmartCondition
    {
        [Tooltip("A brief description of this condition's purpose.")]
        public string description = "New Condition";
        public abstract bool Evaluate(AgentContext agent, SmartObject owner);
    }

    /// <summary>
    /// Base interface for any action that needs to be performed as part of a SmartInteraction.
    /// </summary>
    public interface ISmartAction
    {
        void Execute(AgentContext agent, SmartObject owner);
    }

    /// <summary>
    /// Abstract base class for creating serializable actions.
    /// Provides a description field for better editor readability.
    /// </summary>
    [System.Serializable]
    public abstract class SmartActionBase : ISmartAction
    {
        [Tooltip("A brief description of this action's purpose.")]
        public string description = "New Action";
        public abstract void Execute(AgentContext agent, SmartObject owner);
    }

    /// <summary>
    /// Represents a single possible interaction with a SmartObject.
    /// It defines a set of conditions that must be met and a set of actions
    /// to be performed if the conditions are true.
    /// </summary>
    [System.Serializable]
    public class SmartInteraction
    {
        [Tooltip("The name displayed to the player for this interaction (e.g., 'Open Door', 'Unlock').")]
        public string InteractionName = "Interact";

        [Tooltip("The conditions that must ALL be true for this interaction to be available.")]
        [SerializeReference] // Allows polymorphic serialization in the Inspector
        public List<SmartConditionBase> Conditions = new List<SmartConditionBase>();

        [Tooltip("The actions to be executed when this interaction is performed.")]
        [SerializeReference] // Allows polymorphic serialization in the Inspector
        public List<SmartActionBase> Actions = new List<SmartActionBase>();

        /// <summary>
        /// Evaluates all conditions associated with this interaction.
        /// </summary>
        /// <param name="agent">The context of the interacting agent.</param>
        /// <param name="owner">The SmartObject itself.</param>
        /// <returns>True if all conditions are met, false otherwise.</returns>
        public bool EvaluateConditions(AgentContext agent, SmartObject owner)
        {
            foreach (var condition in Conditions)
            {
                if (!condition.Evaluate(agent, owner))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Executes all actions associated with this interaction.
        /// </summary>
        /// <param name="agent">The context of the interacting agent.</param>
        /// <param name="owner">The SmartObject itself.</param>
        public void ExecuteActions(AgentContext agent, SmartObject owner)
        {
            foreach (var action in Actions)
            {
                action.Execute(agent, owner);
            }
        }
    }

    /// <summary>
    /// Represents a key-value pair for defining object states in the Inspector.
    /// </summary>
    [System.Serializable]
    public class ObjectState
    {
        public string Key;
        public bool Value;
    }

    /// <summary>
    /// The base class for all interactive SmartObjects in the system.
    /// Manages its own internal state and a collection of possible interactions.
    /// </summary>
    public abstract class SmartObject : MonoBehaviour
    {
        [Header("SmartObject Settings")]
        [Tooltip("Optional AudioSource on the SmartObject for playing sounds.")]
        public AudioSource objectAudioSource;

        [Tooltip("Initial states for this object. Use ObjectStateCondition and ChangeObjectStateAction to manipulate.")]
        [SerializeField] protected List<ObjectState> initialStates = new List<ObjectState>();
        protected Dictionary<string, bool> states = new Dictionary<string, bool>();

        [Tooltip("The list of possible interactions this SmartObject can offer.")]
        [SerializeField]
        protected List<SmartInteraction> availableInteractions = new List<SmartInteraction>();

        protected virtual void Awake()
        {
            // Initialize states from the inspector list
            foreach (var state in initialStates)
            {
                if (!states.ContainsKey(state.Key))
                {
                    states.Add(state.Key, state.Value);
                }
                else
                {
                    Debug.LogWarning($"Duplicate state key '{state.Key}' found on {gameObject.name}. Skipping duplicate.", this);
                }
            }

            if (objectAudioSource == null)
            {
                objectAudioSource = GetComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Retrieves the current boolean state of the SmartObject for a given key.
        /// </summary>
        /// <param name="key">The key of the state to retrieve.</param>
        /// <returns>The boolean value of the state, or false if the key doesn't exist.</returns>
        public bool GetState(string key)
        {
            if (states.TryGetValue(key, out bool value))
            {
                return value;
            }
            Debug.LogWarning($"State '{key}' not found on {gameObject.name}. Returning false.", this);
            return false;
        }

        /// <summary>
        /// Sets the boolean state of the SmartObject for a given key.
        /// </summary>
        /// <param name="key">The key of the state to set.</param>
        /// <param name="value">The new boolean value for the state.</param>
        public void SetState(string key, bool value)
        {
            if (states.ContainsKey(key))
            {
                states[key] = value;
                Debug.Log($"Object {gameObject.name} state '{key}' set to: {value}");
            }
            else
            {
                states.Add(key, value);
                Debug.Log($"Object {gameObject.name} new state '{key}' added with value: {value}");
            }
        }

        /// <summary>
        /// Gets a list of interactions that are currently available based on the agent's context and the object's state.
        /// </summary>
        /// <param name="agent">The context of the interacting agent.</param>
        /// <returns>A list of SmartInteraction objects that can currently be performed.</returns>
        public List<SmartInteraction> GetAvailableInteractions(AgentContext agent)
        {
            List<SmartInteraction> currentInteractions = new List<SmartInteraction>();
            foreach (var interaction in availableInteractions)
            {
                if (interaction.EvaluateConditions(agent, this))
                {
                    currentInteractions.Add(interaction);
                }
            }
            return currentInteractions;
        }

        /// <summary>
        /// Performs a specific interaction identified by its name.
        /// It first re-evaluates conditions to ensure the interaction is still valid.
        /// </summary>
        /// <param name="interactionName">The name of the interaction to perform.</param>
        /// <param name="agent">The context of the interacting agent.</param>
        public void PerformInteraction(string interactionName, AgentContext agent)
        {
            SmartInteraction interactionToPerform = availableInteractions.FirstOrDefault(i => i.InteractionName == interactionName);

            if (interactionToPerform == null)
            {
                Debug.LogWarning($"Interaction '{interactionName}' not found on {gameObject.name}.", this);
                return;
            }

            // Re-evaluate conditions just before executing to prevent race conditions or stale data
            if (interactionToPerform.EvaluateConditions(agent, this))
            {
                Debug.Log($"Performing interaction: '{interactionName}' on {gameObject.name}");
                interactionToPerform.ExecuteActions(agent, this);
            }
            else
            {
                Debug.Log($"Cannot perform interaction '{interactionName}' on {gameObject.name}. Conditions are no longer met.", this);
            }
        }
    }

    // =====================================================================================
    // EXAMPLE CONCRETE CONDITIONS
    // These demonstrate how to create specific conditions for SmartObjects.
    // =====================================================================================

    /// <summary>
    /// Condition that checks if the interacting agent has a specific item in their inventory.
    /// </summary>
    [System.Serializable]
    public class HasInventoryItemCondition : SmartConditionBase
    {
        [Tooltip("The name of the item required in the agent's inventory.")]
        public string ItemName;

        public override bool Evaluate(AgentContext agent, SmartObject owner)
        {
            bool hasItem = agent.InventoryItems.Contains(ItemName);
            // Debug.Log($"Checking for item '{ItemName}' in agent inventory. Result: {hasItem}");
            return hasItem;
        }
    }

    /// <summary>
    /// Condition that checks the boolean state of the SmartObject itself.
    /// </summary>
    [System.Serializable]
    public class ObjectStateCondition : SmartConditionBase
    {
        [Tooltip("The key of the state variable to check on the SmartObject.")]
        public string StateKey;
        [Tooltip("The required boolean value for the state to satisfy this condition.")]
        public bool RequiredValue;

        public override bool Evaluate(AgentContext agent, SmartObject owner)
        {
            bool currentState = owner.GetState(StateKey);
            // Debug.Log($"Checking object state '{StateKey}'. Current: {currentState}, Required: {RequiredValue}. Result: {currentState == RequiredValue}");
            return currentState == RequiredValue;
        }
    }

    /// <summary>
    /// A simple condition that always returns true. Useful for actions that are always available.
    /// </summary>
    [System.Serializable]
    public class AlwaysTrueCondition : SmartConditionBase
    {
        public override bool Evaluate(AgentContext agent, SmartObject owner)
        {
            return true;
        }
    }

    // =====================================================================================
    // EXAMPLE CONCRETE ACTIONS
    // These demonstrate how to create specific actions for SmartObjects.
    // =====================================================================================

    /// <summary>
    /// Action to change a boolean state variable on the SmartObject.
    /// </summary>
    [System.Serializable]
    public class ChangeObjectStateAction : SmartActionBase
    {
        [Tooltip("The key of the state variable on the SmartObject to modify.")]
        public string StateKey;
        [Tooltip("The new boolean value to set for the state variable.")]
        public bool NewValue;

        public override void Execute(AgentContext agent, SmartObject owner)
        {
            owner.SetState(StateKey, NewValue);
        }
    }

    /// <summary>
    /// Action to remove a specific item from the interacting agent's inventory.
    /// </summary>
    [System.Serializable]
    public class RemoveInventoryItemAction : SmartActionBase
    {
        [Tooltip("The name of the item to remove from the agent's inventory.")]
        public string ItemName;

        public override void Execute(AgentContext agent, SmartObject owner)
        {
            if (agent.InventoryItems.Contains(ItemName))
            {
                agent.InventoryItems.Remove(ItemName);
                Debug.Log($"Agent {agent.AgentTransform.name} consumed item: {ItemName}. Inventory: {string.Join(", ", agent.InventoryItems)}");
            }
            else
            {
                Debug.LogWarning($"Agent {agent.AgentTransform.name} tried to consume {ItemName}, but doesn't have it.");
            }
        }
    }

    /// <summary>
    /// Action to trigger an Animator parameter on the SmartObject.
    /// </summary>
    [System.Serializable]
    public class AnimateObjectAction : SmartActionBase
    {
        [Tooltip("The name of the Animator Trigger parameter to set.")]
        public string TriggerName;

        public override void Execute(AgentContext agent, SmartObject owner)
        {
            Animator animator = owner.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(TriggerName);
                Debug.Log($"Triggered animator parameter '{TriggerName}' on {owner.name}.");
            }
            else
            {
                Debug.LogWarning($"SmartObject {owner.name} does not have an Animator component for action '{description}'.", owner);
            }
        }
    }

    /// <summary>
    /// Action to play an Audio Clip from the SmartObject's AudioSource.
    /// </summary>
    [System.Serializable]
    public class PlaySoundAction : SmartActionBase
    {
        [Tooltip("The audio clip to play.")]
        public AudioClip SoundClip;
        [Tooltip("The volume at which to play the sound.")]
        [Range(0f, 1f)] public float Volume = 1f;

        public override void Execute(AgentContext agent, SmartObject owner)
        {
            if (owner.objectAudioSource != null && SoundClip != null)
            {
                owner.objectAudioSource.PlayOneShot(SoundClip, Volume);
            }
            else
            {
                if (SoundClip == null) Debug.LogWarning($"PlaySoundAction on {owner.name} is missing an Audio Clip.", owner);
                if (owner.objectAudioSource == null) Debug.LogWarning($"SmartObject {owner.name} is missing an AudioSource component for action '{description}'.", owner);
            }
        }
    }

    /// <summary>
    /// Action to simply log a message to the Unity console. Useful for debugging.
    /// </summary>
    [System.Serializable]
    public class DebugLogAction : SmartActionBase
    {
        [Tooltip("The message to log to the console.")]
        public string Message;

        public override void Execute(AgentContext agent, SmartObject owner)
        {
            Debug.Log($"DebugLogAction from {owner.name}: {Message}");
        }
    }

    // =====================================================================================
    // EXAMPLE SMART OBJECT IMPLEMENTATION: SmartDoor
    // Demonstrates how to extend SmartObject for a specific interactive item.
    // =====================================================================================

    /// <summary>
    /// A concrete implementation of a SmartObject representing an interactive door.
    /// It uses the generic conditions and actions to define its unlock/lock/open/close behavior.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))] // Doors usually need a collider for interaction detection
    public class SmartDoor : SmartObject
    {
        [Header("Door Specifics")]
        [Tooltip("Optional Animator component for door opening/closing animations.")]
        public Animator doorAnimator;

        protected override void Awake()
        {
            base.Awake(); // Initialize base SmartObject properties and states

            if (doorAnimator == null)
            {
                doorAnimator = GetComponent<Animator>();
            }

            // Ensure collider is set to Trigger for easier proximity detection
            if (GetComponent<Collider>() != null)
            {
                GetComponent<Collider>().isTrigger = true;
            }
        }

        // No need for Update or fixed door-specific logic here.
        // All door behavior is defined declaratively through `availableInteractions`
        // in the Inspector using the generic SmartObjectSystem components.
    }

    // =====================================================================================
    // EXAMPLE AGENT IMPLEMENTATION: PlayerAgent
    // Demonstrates how an agent (e.g., player character) interacts with SmartObjects.
    // =====================================================================================

    /// <summary>
    /// A simple player agent that can detect and interact with SmartObjects.
    /// It gathers its own context (inventory) and initiates interactions.
    /// </summary>
    public class PlayerAgent : MonoBehaviour
    {
        [Header("Player Agent Settings")]
        [Tooltip("The range within which the player can interact with SmartObjects.")]
        public float interactionRange = 3f;
        [Tooltip("The layer(s) on which SmartObjects reside.")]
        public LayerMask interactableLayer;
        [Tooltip("The input key to trigger interaction.")]
        public KeyCode interactKey = KeyCode.E;
        [Tooltip("AudioSource for playing player-related sounds (e.g., interaction confirmation).")]
        public AudioSource playerAudioSource;
        [Tooltip("Sound played when an interaction is successfully performed.")]
        public AudioClip interactionSuccessSound;
        [Tooltip("Sound played when an interaction fails (e.g., conditions not met).")]
        public AudioClip interactionFailSound;


        [Header("Agent Inventory (for demonstration)")]
        public List<string> inventoryItems = new List<string>();

        private SmartObject currentTargetSmartObject;

        void Awake()
        {
            if (playerAudioSource == null)
            {
                playerAudioSource = GetComponent<AudioSource>();
                if (playerAudioSource == null)
                {
                    playerAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        void Update()
        {
            FindTargetSmartObject();

            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }
        }

        /// <summary>
        /// Finds the closest SmartObject within interaction range.
        /// </summary>
        void FindTargetSmartObject()
        {
            currentTargetSmartObject = null;
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);

            float closestDistance = float.MaxValue;
            SmartObject potentialTarget = null;

            foreach (var hitCollider in hitColliders)
            {
                SmartObject smartObject = hitCollider.GetComponentInParent<SmartObject>(); // Use GetCompoenentInParent for complex hierarchies
                if (smartObject != null)
                {
                    float dist = Vector3.Distance(transform.position, smartObject.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        potentialTarget = smartObject;
                    }
                }
            }
            currentTargetSmartObject = potentialTarget;

            if (currentTargetSmartObject != null)
            {
                // Optionally provide visual feedback for targeting
                Debug.DrawLine(transform.position, currentTargetSmartObject.transform.position, Color.yellow);
            }
        }


        /// <summary>
        /// Attempts to interact with the current target SmartObject.
        /// It presents available interactions and performs the first valid one.
        /// In a real game, you might present a UI menu for multiple interactions.
        /// </summary>
        void TryInteract()
        {
            if (currentTargetSmartObject == null)
            {
                Debug.Log("No SmartObject in range to interact with.");
                return;
            }

            AgentContext agentContext = new AgentContext(transform, inventoryItems, playerAudioSource);
            List<SmartInteraction> available = currentTargetSmartObject.GetAvailableInteractions(agentContext);

            if (available.Count > 0)
            {
                // For simplicity, we'll perform the first available interaction.
                // In a real game, you'd show a UI for the player to choose if multiple are available.
                SmartInteraction chosenInteraction = available[0];
                currentTargetSmartObject.PerformInteraction(chosenInteraction.InteractionName, agentContext);

                if (interactionSuccessSound != null) playerAudioSource.PlayOneShot(interactionSuccessSound);

                // Update the player's inventory reference since actions might modify it.
                inventoryItems = agentContext.InventoryItems;
            }
            else
            {
                Debug.Log($"No available interactions for {currentTargetSmartObject.name} with current agent context.");
                if (interactionFailSound != null) playerAudioSource.PlayOneShot(interactionFailSound);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
```