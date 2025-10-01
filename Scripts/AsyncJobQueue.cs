// Unity Design Pattern Example: AsyncJobQueue
// This script demonstrates the AsyncJobQueue pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **AsyncJobQueue** design pattern in Unity. It provides a central, managed queue for executing asynchronous operations (jobs) in the background, limiting the number of concurrent jobs to prevent resource exhaustion, and allowing for cancellation.

**Key Concepts Demonstrated:**

1.  **Job Abstraction:** A `AsyncJob` class to represent a unit of work.
2.  **Concurrency Control:** Using `SemaphoreSlim` to limit how many jobs run simultaneously.
3.  **Asynchronous Processing Loop:** A background `Task` that constantly polls the queue for new jobs.
4.  **Cancellation:** Proper use of `CancellationToken` for graceful shutdown and individual job cancellation.
5.  **TaskCompletionSource:** To bridge the internal job execution with the external `await`able `Task` returned to the caller.
6.  **Error Handling:** Capturing and reporting exceptions from jobs.
7.  **Unity Integration:** A `MonoBehaviour` to demonstrate how to integrate and use the queue in a Unity project, with UI-driven examples.

---

### File 1: `AsyncJobQueue.cs`

This script defines the core `AsyncJob` and `AsyncJobQueue` classes.

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine; // For Debug.Log, even though it's a non-MonoBehaviour class. Consider a custom ILogger for pure library.

/// <summary>
/// Represents a single asynchronous job to be processed by the AsyncJobQueue.
/// </summary>
/// <remarks>
/// This class encapsulates the work to be done, along with an ID, description,
/// and a TaskCompletionSource to allow external callers to await its completion.
/// </remarks>
public class AsyncJob
{
    public string Id { get; }
    public string Description { get; }
    public Func<CancellationToken, Task<object>> WorkAction { get; }
    public TaskCompletionSource<object> Tcs { get; }

    /// <summary>
    /// Initializes a new instance of the AsyncJob class.
    /// </summary>
    /// <param name="id">A unique identifier for the job.</param>
    /// <param name="description">A human-readable description of the job.</param>
    /// <param name="workAction">
    /// The asynchronous function representing the work to be performed.
    /// It takes a CancellationToken and returns a Task<object> for its result.
    /// </param>
    public AsyncJob(string id, string description, Func<CancellationToken, Task<object>> workAction)
    {
        Id = id;
        Description = description;
        WorkAction = workAction;
        Tcs = new TaskCompletionSource<object>();
    }

    /// <summary>
    /// Gets the Task associated with this job's completion.
    /// Callers can await this Task to get the job's result or observe its status.
    /// </summary>
    public Task<object> Task => Tcs.Task;
}

/// <summary>
/// Implements the AsyncJobQueue design pattern.
/// Manages a queue of asynchronous jobs, processing them concurrently up to a specified limit.
/// </summary>
/// <remarks>
/// This queue is designed to offload potentially long-running or CPU-intensive
/// tasks from the main Unity thread, processing them in the background.
/// It uses SemaphoreSlim for concurrency control and CancellationToken for graceful shutdown.
/// </remarks>
public class AsyncJobQueue : IDisposable
{
    // A thread-safe queue to hold jobs awaiting execution.
    private readonly ConcurrentQueue<AsyncJob> _jobQueue = new ConcurrentQueue<AsyncJob>();

    // Semaphore to signal when new jobs are available in the queue.
    // It starts with 0, meaning it requires a Release() call to proceed.
    private readonly SemaphoreSlim _jobAvailableSemaphore = new SemaphoreSlim(0);

    // Semaphore to limit the number of jobs running concurrently.
    private readonly SemaphoreSlim _concurrentJobSemaphore;

    // CancellationTokenSource to manage cancellation for all jobs and the processing loop.
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    // The main Task that runs the job processing loop in the background.
    private readonly Task _processingTask;

    // The maximum number of jobs that can run simultaneously.
    private readonly int _maxConcurrentJobs;

    /// <summary>
    /// Gets the current number of jobs waiting in the queue.
    /// </summary>
    public int PendingJobCount => _jobQueue.Count;

    /// <summary>
    /// Gets the maximum number of jobs that can run concurrently.
    /// </summary>
    public int MaxConcurrentJobs => _maxConcurrentJobs;

    /// <summary>
    /// Initializes a new instance of the AsyncJobQueue.
    /// </summary>
    /// <param name="maxConcurrentJobs">
    /// The maximum number of jobs that can execute simultaneously. Must be at least 1.
    /// </param>
    public AsyncJobQueue(int maxConcurrentJobs = 1)
    {
        _maxConcurrentJobs = Math.Max(1, maxConcurrentJobs); // Ensure at least 1 concurrent job
        _concurrentJobSemaphore = new SemaphoreSlim(_maxConcurrentJobs, _maxConcurrentJobs);

        // Start the background processing task immediately.
        // It runs indefinitely until _cts is canceled.
        _processingTask = Task.Run(() => ProcessJobsAsync(_cts.Token));

        Debug.Log($"[AsyncJobQueue] Initialized with {_maxConcurrentJobs} max concurrent jobs.");
    }

    /// <summary>
    /// Enqueues a new asynchronous job into the queue.
    /// </summary>
    /// <param name="id">A unique ID for the job.</param>
    /// <param name="description">A descriptive name for the job.</param>
    /// <param name="workAction">The async function representing the job's work.</param>
    /// <returns>A Task<object> that completes when the job finishes, returning its result.</returns>
    public async Task<object> EnqueueJob(string id, string description, Func<CancellationToken, Task<object>> workAction)
    {
        var job = new AsyncJob(id, description, workAction);
        _jobQueue.Enqueue(job);
        _jobAvailableSemaphore.Release(); // Signal that a new job is available.
        Debug.Log($"[AsyncJobQueue] Enqueued job: '{id}' ({_jobQueue.Count} jobs pending).");
        return await job.Task; // Return the job's Task, allowing the caller to await its completion.
    }

    /// <summary>
    /// The main asynchronous loop that processes jobs from the queue.
    /// This runs on a separate thread managed by Task.Run.
    /// </summary>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        // Keep the processing loop alive until cancellation is requested.
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a job to become available in the queue.
                // If the queue is empty, this will block until _jobAvailableSemaphore.Release() is called.
                await _jobAvailableSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                // If cancellation was requested while waiting, throw and exit.
                cancellationToken.ThrowIfCancellationRequested();

                // Attempt to dequeue a job.
                if (_jobQueue.TryDequeue(out var job))
                {
                    // Acqire a slot in the concurrent jobs semaphore.
                    // This limits how many jobs can run simultaneously.
                    await _concurrentJobSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                    // If cancellation was requested while waiting for a slot, throw and exit.
                    cancellationToken.ThrowIfCancellationRequested();

                    // We now have a job and a slot. Launch the job's execution in a new Task.
                    // We don't await this Task directly in the loop to allow the loop to fetch
                    // the next job (if a slot is available) without waiting for the current one to finish.
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            Debug.Log($"[AsyncJobQueue] Starting job: '{job.Id}' - {job.Description} (Active: {_maxConcurrentJobs - _concurrentJobSemaphore.CurrentCount}/{_maxConcurrentJobs})");
                            // Execute the job's work action. Pass the queue's cancellation token.
                            object result = await job.WorkAction(cancellationToken).ConfigureAwait(false);
                            job.Tcs.SetResult(result); // Signal job completion with its result.
                            Debug.Log($"[AsyncJobQueue] Finished job: '{job.Id}'.");
                        }
                        catch (OperationCanceledException)
                        {
                            job.Tcs.SetCanceled(); // Signal that the job was canceled.
                            Debug.LogWarning($"[AsyncJobQueue] Job '{job.Id}' was canceled.");
                        }
                        catch (Exception ex)
                        {
                            job.Tcs.SetException(ex); // Signal that the job failed with an exception.
                            Debug.LogError($"[AsyncJobQueue] Job '{job.Id}' failed: {ex.Message}");
                        }
                        finally
                        {
                            _concurrentJobSemaphore.Release(); // Release the slot, allowing another job to run.
                        }
                    }, cancellationToken); // Pass cancellation token to Task.Run as well.
                }
            }
            catch (OperationCanceledException)
            {
                // The queue's CTS was canceled. Exit the processing loop gracefully.
                Debug.Log("[AsyncJobQueue] Processing loop received cancellation request. Shutting down.");
                break;
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors in the processing loop itself.
                Debug.LogError($"[AsyncJobQueue] Critical error in processing loop: {ex.Message}");
            }
        }
        Debug.Log("[AsyncJobQueue] Processing loop stopped.");
    }

    /// <summary>
    /// Disposes the AsyncJobQueue, canceling all pending and active jobs, and releasing resources.
    /// </summary>
    public void Dispose()
    {
        // Cancel all jobs and the processing loop.
        _cts.Cancel();
        // Wait for the processing task to complete its shutdown (optional, but good for clean exit).
        // Using a timeout is often a good practice here in case tasks get stuck.
        _processingTask.Wait(TimeSpan.FromSeconds(5)); 

        _cts.Dispose();
        _jobAvailableSemaphore.Dispose();
        _concurrentJobSemaphore.Dispose();
        Debug.Log("[AsyncJobQueue] Disposed and resources released.");
    }
}
```

---

### File 2: `AsyncJobQueueExample.cs`

This script is a Unity `MonoBehaviour` that demonstrates how to use the `AsyncJobQueue`. Attach this to an empty GameObject in your scene.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random; // Clarify usage of Unity's Random

/// <summary>
/// Unity MonoBehaviour to demonstrate the usage of the AsyncJobQueue.
/// </summary>
/// <remarks>
/// This script creates an AsyncJobQueue instance, configures it,
/// and provides example methods to enqueue different types of jobs
/// (heavy calculation, resource loading, game saving).
/// It shows how to await job completion and handle results/errors.
/// </remarks>
public class AsyncJobQueueExample : MonoBehaviour
{
    [Tooltip("Maximum number of jobs that can run concurrently.")]
    [SerializeField] private int _maxConcurrentJobs = 3;

    private AsyncJobQueue _jobQueue;

    void Awake()
    {
        // Initialize the AsyncJobQueue when the script awakes.
        _jobQueue = new AsyncJobQueue(_maxConcurrentJobs);
        Debug.Log($"[Example] AsyncJobQueue initialized with {_maxConcurrentJobs} concurrent jobs.");
    }

    void OnDestroy()
    {
        // Ensure the job queue is disposed when the GameObject is destroyed.
        // This cancels any ongoing jobs and releases system resources.
        _jobQueue?.Dispose();
        Debug.Log("[Example] AsyncJobQueue disposed.");
    }

    // --- Example Job Enqueue Methods ---

    /// <summary>
    /// Enqueues a job that simulates a CPU-intensive calculation.
    /// </summary>
    public async void EnqueueHeavyCalculation()
    {
        string jobId = $"Calc_{Guid.NewGuid().ToString().Substring(0, 4)}";
        Debug.Log($"[Example] Requesting heavy calculation: {jobId}");

        try
        {
            // Enqueue the job. The `await` keyword here pauses this method
            // until the job completes in the background, allowing the UI to remain responsive.
            object result = await _jobQueue.EnqueueJob(jobId, "Simulating a heavy calculation (e.g., pathfinding, procedural mesh generation)",
                async (token) => // The workAction delegate
                {
                    // Simulate CPU-bound work in chunks.
                    // In a real scenario, replace Task.Delay with actual computation.
                    for (int i = 0; i < 10; i++)
                    {
                        token.ThrowIfCancellationRequested(); // Check for cancellation regularly
                        // Simulate a small chunk of computation
                        await Task.Delay(100, token).ConfigureAwait(false); // Use ConfigureAwait(false) for non-Unity context tasks
                    }
                    float randomValue = Random.Range(100f, 1000f);
                    return $"Calculated value: {randomValue:F2}"; // Return a simulated result
                });

            // This code runs on the main thread after the background job finishes.
            Debug.Log($"[Example] [{jobId}] Heavy calculation finished. Result: {result}");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[Example] [{jobId}] Heavy calculation was canceled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Example] [{jobId}] Heavy calculation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Enqueues a job that simulates loading a resource from disk or network.
    /// </summary>
    public async void EnqueueResourceLoading()
    {
        string jobId = $"Load_{Guid.NewGuid().ToString().Substring(0, 4)}";
        string resourceName = $"Texture_{Random.Range(1, 10)}.png";
        Debug.Log($"[Example] Requesting resource loading: {jobId} for '{resourceName}'");

        try
        {
            object result = await _jobQueue.EnqueueJob(jobId, $"Loading resource: {resourceName}",
                async (token) =>
                {
                    // Simulate I/O bound work (e.g., reading from disk, downloading from network).
                    // This could involve actual File.ReadAllBytesAsync or UnityWebRequest.SendWebRequest().
                    int loadTime = Random.Range(500, 2000); // 0.5 to 2 seconds
                    await Task.Delay(loadTime, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    return $"Loaded {resourceName} successfully (simulated size: {Random.Range(1, 10)} MB)";
                });

            Debug.Log($"[Example] [{jobId}] Resource loading finished. Result: {result}");
            // Here you might process the loaded resource on the main thread, e.g., apply to a material
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[Example] [{jobId}] Resource loading for '{resourceName}' was canceled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Example] [{jobId}] Resource loading for '{resourceName}' failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Enqueues a job that simulates saving game data.
    /// </summary>
    public async void EnqueueGameSaving()
    {
        string jobId = $"Save_{Guid.NewGuid().ToString().Substring(0, 4)}";
        string saveFileName = $"PlayerSave_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
        Debug.Log($"[Example] Requesting game data save: {jobId} to '{saveFileName}'");

        try
        {
            object result = await _jobQueue.EnqueueJob(jobId, $"Saving game data to: {saveFileName}",
                async (token) =>
                {
                    // Simulate writing a large amount of game data to disk.
                    // This could be JSON serialization, binary serialization, etc.
                    int saveTime = Random.Range(300, 1500); // 0.3 to 1.5 seconds
                    await Task.Delay(saveTime, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    // Simulate a potential error during saving
                    if (Random.value < 0.1f) // 10% chance of failure
                    {
                        throw new System.IO.IOException("Simulated disk write error or data corruption!");
                    }
                    return $"Game data saved to {saveFileName} successfully.";
                });

            Debug.Log($"[Example] [{jobId}] Game data saving finished. Result: {result}");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[Example] [{jobId}] Game data saving for '{saveFileName}' was canceled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Example] [{jobId}] Game data saving for '{saveFileName}' failed: {ex.Message}");
        }
    }

    // --- UI Button Callbacks (for demonstration in Inspector) ---
    // You can hook these methods up to UI Buttons in a Canvas in your scene.

    public void OnClick_EnqueueHeavyCalculation()
    {
        EnqueueHeavyCalculation();
    }

    public void OnClick_EnqueueResourceLoading()
    {
        EnqueueResourceLoading();
    }

    public void OnClick_EnqueueGameSaving()
    {
        EnqueueGameSaving();
    }

    public void OnClick_EnqueueMultiple()
    {
        Debug.Log("[Example] Enqueuing multiple jobs rapidly...");
        for (int i = 0; i < 5; i++)
        {
            if (i % 3 == 0) EnqueueHeavyCalculation();
            else if (i % 3 == 1) EnqueueResourceLoading();
            else EnqueueGameSaving();
        }
    }
}
```

---

### How to Use in Unity:

1.  **Create C# Scripts:**
    *   Create a new C# script named `AsyncJobQueue.cs` and paste the content of `File 1` into it.
    *   Create another C# script named `AsyncJobQueueExample.cs` and paste the content of `File 2` into it.
2.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., `JobQueueManager`).
3.  **Attach Script:** Drag and drop the `AsyncJobQueueExample.cs` script onto the `JobQueueManager` GameObject in the Hierarchy.
4.  **Configure in Inspector:**
    *   Select `JobQueueManager`.
    *   In the Inspector, you'll see a field `Max Concurrent Jobs`. You can change this value (e.g., to 1, 3, or 5) to observe how it affects job execution.
5.  **Add UI Buttons (Optional but Recommended):**
    *   Create a Canvas (`GameObject -> UI -> Canvas`).
    *   Inside the Canvas, create a few UI Buttons (`GameObject -> UI -> Button`).
    *   For each button:
        *   Change its text to something descriptive (e.g., "Heavy Calc", "Load Resource", "Save Game", "Enqueue 5 Random").
        *   In the Inspector, find the "On Click ()" section.
        *   Click the `+` button to add a new event.
        *   Drag the `JobQueueManager` GameObject from the Hierarchy into the "None (Object)" slot.
        *   From the dropdown, select `AsyncJobQueueExample` and then choose the corresponding method:
            *   "Heavy Calc" -> `OnClick_EnqueueHeavyCalculation()`
            *   "Load Resource" -> `OnClick_EnqueueResourceLoading()`
            *   "Save Game" -> `OnClick_EnqueueGameSaving()`
            *   "Enqueue 5 Random" -> `OnClick_EnqueueMultiple()`
6.  **Run the Scene:**
    *   Press Play in Unity.
    *   Click your UI buttons. Observe the `Debug.Log` messages in the Unity Console. You'll see jobs being enqueued, starting, finishing, or failing, and how the concurrency limit is respected.
    *   Try changing `_maxConcurrentJobs` to `1` and re-run to see strictly sequential execution.

This setup provides a complete, working example that you can immediately drop into a Unity project to understand and utilize the AsyncJobQueue pattern for managing background tasks efficiently.