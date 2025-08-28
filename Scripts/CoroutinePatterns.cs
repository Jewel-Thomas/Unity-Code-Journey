// Unity Design Pattern Example: CoroutinePatterns
// This script demonstrates the CoroutinePatterns pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates various "Coroutine Patterns" in Unity, which are not formal design patterns like Singleton or Factory, but rather a collection of common and effective techniques for using Unity's Coroutines to manage asynchronous, time-dependent, or sequential operations.

These patterns are crucial for:
*   **Sequential Logic:** Executing tasks one after another.
*   **Parallel Logic:** Running multiple tasks concurrently and optionally waiting for all to complete.
*   **Asynchronous Operations:** Performing long-running tasks without freezing the main thread.
*   **State Management:** Handling progress, results, or cancellation of ongoing operations.

---

### **How to Use This Script in Unity:**

1.  Create a new C# script named `CoroutinePatternsDemo`.
2.  Copy and paste the entire code below into the script.
3.  Attach this script to any GameObject in your Unity scene (e.g., your Main Camera).
4.  Run the scene.
5.  Observe the console output for the demonstration of each pattern.
6.  During the "Cooperative Cancellation Demo," press the **SPACEBAR** to trigger cancellation.

---

```csharp
using UnityEngine;
using System.Collections; // Required for IEnumerator and Coroutine
using System;             // Required for Action (delegates)

/// <summary>
/// Demonstrates various "Coroutine Patterns" in Unity.
/// These patterns are common techniques for managing asynchronous and sequential tasks
/// using Unity's Coroutines effectively.
/// </summary>
public class CoroutinePatternsDemo : MonoBehaviour
{
    // --- Public Properties (for Inspector) ---
    [Tooltip("Default duration for tasks in seconds.")]
    [SerializeField] private float defaultTaskDuration = 1.0f;

    // --- Internal State for Cooperative Cancellation ---
    // This flag is checked by cancellable coroutines to determine if they should stop.
    private bool _cancellationRequested = false;

    // --- Main Entry Point: Start all demos ---
    void Start()
    {
        Debug.Log("--- Coroutine Patterns Demo Started ---");
        // Start a master coroutine to orchestrate all the individual pattern demonstrations.
        // This ensures each demo runs in sequence and its output is clear in the console.
        StartCoroutine(RunAllDemos());
    }

    /// <summary>
    /// Orchestrates the execution of all individual coroutine pattern demonstrations.
    /// Each `yield return StartCoroutine(...)` ensures that the current demo
    /// fully completes before the next one begins, making the console output clear.
    /// </summary>
    private IEnumerator RunAllDemos()
    {
        Debug.Log("\n--- 1. Sequential Coroutines Pattern ---");
        yield return StartCoroutine(DemoSequentialCoroutines());

        Debug.Log("\n--- 2. Coroutine with Parameter Pattern ---");
        yield return StartCoroutine(DemoCoroutineWithParameter("A custom message passed to the coroutine", 2.0f));

        Debug.Log("\n--- 3. Parallel Coroutines Pattern (Wait for All) ---");
        yield return StartCoroutine(DemoParallelCoroutines());

        Debug.Log("\n--- 4. Coroutine with Progress Reporting Pattern ---");
        // We pass a longer duration here to clearly see the progress updates.
        yield return StartCoroutine(DemoCoroutineWithProgress(3.5f));

        Debug.Log("\n--- 5. Coroutine with Result (Callback) Pattern ---");
        yield return StartCoroutine(DemoCoroutineWithResult(2.5f));

        Debug.Log("\n--- 6. Reusable Coroutine Logic Pattern ---");
        yield return StartCoroutine(DemoReusableLogic());

        Debug.Log("\n--- 7. Cooperative Cancellation Pattern ---");
        // Give it a longer duration so there's time to press SPACE and cancel.
        yield return StartCoroutine(DemoCooperativeCancellation(5.0f));

        Debug.Log("\n--- Coroutine Patterns Demo Finished ---");
    }

    // --- Pattern 1: Sequential Coroutines ---
    // Description: Executes multiple coroutines one after another.
    // How it works: Use `yield return StartCoroutine(yourCoroutine);`
    //               This waits for `yourCoroutine` to complete before proceeding
    //               to the next line in the current coroutine.
    private IEnumerator DemoSequentialCoroutines()
    {
        Debug.Log("[Sequential] Starting Task A...");
        // This line will pause DemoSequentialCoroutines until SimulateSimpleTask("Task A") finishes.
        yield return StartCoroutine(SimulateSimpleTask("Task A", defaultTaskDuration));
        
        Debug.Log("[Sequential] Starting Task B...");
        // After Task A, this line starts and waits for SimulateSimpleTask("Task B").
        yield return StartCoroutine(SimulateSimpleTask("Task B", defaultTaskDuration));
        
        Debug.Log("[Sequential] All sequential tasks completed.");
    }

    // --- Pattern 2: Coroutine with Parameter ---
    // Description: Pass data into a coroutine when starting it.
    // How it works: Define your coroutine method with parameters like any other method.
    //               When calling `StartCoroutine`, pass the arguments.
    private IEnumerator DemoCoroutineWithParameter(string message, float duration)
    {
        Debug.Log($"[Parameter] Starting parameterized task: '{message}' for {duration:F1} seconds.");
        // The parameters (`message`, `duration`) are used within the coroutine.
        yield return StartCoroutine(SimulateSimpleTask(message, duration));
        Debug.Log("[Parameter] Parameterized task completed.");
    }

    // --- Pattern 3: Parallel Coroutines (Wait for All) ---
    // Description: Start multiple coroutines at the same time and wait until all of them have finished.
    // How it works:
    // 1. Call `StartCoroutine` for each task and store the returned `Coroutine` object.
    // 2. Then, `yield return` each of those `Coroutine` objects.
    //    The `yield return` waits for a *specific* coroutine instance to finish.
    //    The tasks themselves run in parallel after being started.
    private IEnumerator DemoParallelCoroutines()
    {
        Debug.Log("[Parallel] Starting Parallel Tasks (Task X and Task Y)...");
        
        // Start both coroutines. They begin executing immediately (in parallel).
        // We store the Coroutine objects to yield them later.
        Coroutine taskX = StartCoroutine(SimulateSimpleTask("Parallel Task X", defaultTaskDuration));
        Coroutine taskY = StartCoroutine(SimulateSimpleTask("Parallel Task Y", defaultTaskDuration * 1.5f)); // Make Y a bit longer

        Debug.Log("[Parallel] Both parallel tasks started. Waiting for them to complete...");

        // Yield each Coroutine object. The current coroutine (DemoParallelCoroutines)
        // will pause until `taskX` finishes, then it will pause until `taskY` finishes.
        // Because X and Y were started in parallel, if X finishes before Y, the `yield return taskX`
        // will resolve immediately when its turn comes (as X is already done).
        // This effectively ensures *both* have completed before proceeding.
        yield return taskX;
        yield return taskY;

        Debug.Log("[Parallel] All parallel tasks completed.");
    }

    // --- Pattern 4: Coroutine with Progress Reporting ---
    // Description: Provide real-time updates on the progress of a long-running operation.
    // How it works: Pass an `Action<float>` (or similar callback) to the coroutine.
    //               The coroutine periodically invokes this callback with the current progress.
    private IEnumerator DemoCoroutineWithProgress(float duration)
    {
        Debug.Log($"[Progress] Starting task with progress reporting for {duration:F1} seconds.");
        float currentProgress = 0f; // A local variable to hold the latest reported progress.

        // Start the long task, passing a lambda expression as the progress callback.
        yield return StartCoroutine(SimulateLongTaskWithProgress("Progress Task", duration, (progress) =>
        {
            currentProgress = progress; // Update the local variable
            // Log the progress. This callback is invoked frequently by SimulateLongTaskWithProgress.
            Debug.Log($"[Progress] {currentProgress:P0} complete."); // e.g., "25% complete."
        }));

        Debug.Log($"[Progress] Progress task finished. Final progress: {currentProgress:P0}");
    }

    // --- Pattern 5: Coroutine with Result (using a callback) ---
    // Description: Obtain a result from a coroutine that performs an asynchronous calculation.
    // How it works: Since `IEnumerator` cannot directly return a value (except `yield return null`, `yield return new WaitForSeconds`, etc.),
    //               a common pattern is to pass a callback (`Action<T>`) to the coroutine.
    //               The coroutine invokes this callback when its result is ready.
    private IEnumerator DemoCoroutineWithResult(float duration)
    {
        Debug.Log($"[Result] Starting task to calculate a result for {duration:F1} seconds.");
        int calculatedResult = 0; // A local variable to store the result when it becomes available.

        // Start the calculation coroutine, passing a lambda expression as the completion callback.
        yield return StartCoroutine(SimulateCalculationAsync(duration, (result) =>
        {
            calculatedResult = result; // The callback assigns the result here.
            Debug.Log($"[Result] Received result via callback: {calculatedResult}");
        }));

        Debug.Log($"[Result] Task with result completed. Final result: {calculatedResult}");
    }

    // --- Pattern 6: Reusable Coroutine Logic ---
    // Description: Create helper methods that return `IEnumerator` to encapsulate common coroutine logic.
    // How it works: Define a `private IEnumerator` method. This method can then be `yield return`ed
    //               from other coroutines, promoting code reuse.
    private IEnumerator DemoReusableLogic()
    {
        Debug.Log("[Reusable] Starting demo of reusable coroutine logic.");

        // We can directly yield the IEnumerator returned by ReusableStep.
        // This pauses DemoReusableLogic until ReusableStep completes.
        yield return ReusableStep("First reusable step", 1.0f);
        yield return ReusableStep("Second reusable step", 0.8f);

        Debug.Log("[Reusable] Reusable coroutine logic demo completed.");
    }

    // --- Pattern 7: Cooperative Cancellation ---
    // Description: Allows a long-running coroutine to be gracefully stopped by an external request.
    // How it works:
    // 1. Maintain a shared flag (e.g., `_cancellationRequested`).
    // 2. The cancellable coroutine periodically checks this flag.
    // 3. If the flag is set, the coroutine performs any necessary cleanup and exits using `yield break;`.
    // 4. An external source (e.g., user input, another coroutine) sets the flag to request cancellation.
    private IEnumerator DemoCooperativeCancellation(float duration)
    {
        Debug.Log($"[Cancellation] Starting cancellable task for {duration:F1} seconds. Press SPACEBAR to cancel.");
        _cancellationRequested = false; // Reset the flag for this demo.

        // Start the cancellable task.
        Coroutine cancellableTask = StartCoroutine(CooperativeCancellableTask(duration));

        // Wait for some time. We will try to cancel it halfway through.
        yield return new WaitForSeconds(duration / 2f);

        // If the task hasn't already completed or been cancelled by some other means,
        // then request its cancellation.
        if (!_cancellationRequested)
        {
            Debug.Log("[Cancellation] Requesting cancellation via flag...");
            _cancellationRequested = true;
        }

        // Wait for the cancellable task to actually finish (either by completing naturally
        // or by exiting due to cancellation check).
        yield return cancellableTask;

        Debug.Log("[Cancellation] Cooperative cancellation demo finished.");
    }

    // --- Helper Coroutines (These are the actual implementations of the tasks) ---

    /// <summary>
    /// A simple coroutine that waits for a specified duration and then logs a message.
    /// Used in Sequential, Parameter, and Parallel demos.
    /// </summary>
    private IEnumerator SimulateSimpleTask(string taskName, float duration)
    {
        Debug.Log($"[Helper] '{taskName}' started. Waiting for {duration:F1} seconds...");
        yield return new WaitForSeconds(duration);
        Debug.Log($"[Helper] '{taskName}' finished.");
    }

    /// <summary>
    /// Simulates a long-running task that reports its progress periodically via a callback.
    /// </summary>
    /// <param name="taskName">Name of the task.</param>
    /// <param name="duration">Total duration of the task.</param>
    /// <param name="onProgress">Callback to invoke with current progress (0.0 to 1.0).</param>
    private IEnumerator SimulateLongTaskWithProgress(string taskName, float duration, Action<float> onProgress)
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            onProgress?.Invoke(progress); // Report progress
            yield return null; // Wait for the next frame
        }
        onProgress?.Invoke(1f); // Ensure 100% is always reported at completion
        Debug.Log($"[Helper] '{taskName}' completed.");
    }

    /// <summary>
    /// Simulates an asynchronous calculation that returns a result via a callback.
    /// </summary>
    /// <param name="duration">Duration of the simulated calculation.</param>
    /// <param name="onComplete">Callback to invoke with the integer result when done.</param>
    private IEnumerator SimulateCalculationAsync(float duration, Action<int> onComplete)
    {
        Debug.Log($"[Helper] Calculating result asynchronously for {duration:F1} seconds...");
        yield return new WaitForSeconds(duration); // Simulate work
        int result = UnityEngine.Random.Range(100, 1000); // Generate a random result
        onComplete?.Invoke(result); // Pass the result back via the callback
        Debug.Log($"[Helper] Calculation finished. Result: {result}");
    }

    /// <summary>
    /// A reusable coroutine step that waits and logs.
    /// </summary>
    /// <param name="stepName">Name of this reusable step.</param>
    /// <param name="duration">Duration for this step.</param>
    private IEnumerator ReusableStep(string stepName, float duration)
    {
        Debug.Log($"[Reusable Helper] Starting step: '{stepName}'. Waiting {duration:F1}s.");
        yield return new WaitForSeconds(duration);
        Debug.Log($"[Reusable Helper] Finished step: '{stepName}'.");
    }

    /// <summary>
    /// A coroutine that continuously checks a cancellation flag and stops gracefully if requested.
    /// </summary>
    /// <param name="duration">Total planned duration if not cancelled.</param>
    private IEnumerator CooperativeCancellableTask(float duration)
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // --- Cooperative Cancellation Check ---
            // This is the core of the cooperative cancellation pattern.
            // The coroutine regularly checks an external flag.
            if (_cancellationRequested)
            {
                Debug.Log("[Cancellable Task] Cancellation requested! Performing cleanup and stopping gracefully.");
                // Perform any cleanup here if necessary (e.g., release resources, save partial state).
                yield break; // Exits the coroutine immediately.
            }

            elapsedTime = Time.time - startTime;
            Debug.Log($"[Cancellable Task] Working... ({elapsedTime:F1}s / {duration:F1}s)");
            yield return null; // Wait for the next frame before checking again.
        }

        Debug.Log("[Cancellable Task] Task completed without cancellation.");
    }

    /// <summary>
    /// Listens for user input to trigger cancellation during the Cooperative Cancellation Demo.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Only request cancellation if the demo is actually running and hasn't been cancelled yet.
            if (!_cancellationRequested && IsRunningCooperativeCancellationDemo())
            {
                Debug.Log("[Input] User pressed SPACEBAR. Setting cancellation flag.");
                _cancellationRequested = true;
            }
        }
    }

    /// <summary>
    /// Helper to determine if the cooperative cancellation demo is active.
    /// (A more robust solution might track the state of the `RunAllDemos` coroutine more precisely.)
    /// </summary>
    private bool IsRunningCooperativeCancellationDemo()
    {
        // This is a simple heuristic. In a real project, you might have a state machine
        // or a dedicated manager for long-running operations.
        // For this demo, we assume if `_cancellationRequested` is false, it means
        // the task hasn't been cancelled yet and might be running.
        return true; // We'll just always allow cancellation for demo purposes.
    }
}
```