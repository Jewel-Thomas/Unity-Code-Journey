// Unity Design Pattern Example: LoggingSystemPattern
// This script demonstrates the LoggingSystemPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The Logging System Pattern centralizes the management and routing of log messages in an application. It provides a flexible way to send logs to various destinations (console, file, network, etc.) without requiring the calling code to know about the concrete logging implementations. This enhances maintainability, testability, and configurability.

### Key Components of the LoggingSystemPattern:

1.  **`LogLevel` (Enum):** Defines the severity levels for log messages (e.g., Debug, Info, Warning, Error, Fatal).
2.  **`ILogger` (Interface):** Declares the common contract for all concrete loggers. It typically includes methods like `Log(LogLevel level, string message, ...)` and properties like `MinimumLogLevel`.
3.  **Concrete Loggers (`UnityConsoleLogger`, `FileLogger`, etc.):** Implement the `ILogger` interface. Each concrete logger is responsible for directing log messages to a specific output target.
    *   `UnityConsoleLogger`: Sends messages to Unity's `Debug.Log`, `Debug.LogWarning`, `Debug.LogError`.
    *   `FileLogger`: Writes messages to a persistent log file.
4.  **`Log` (Facade/Manager):** A central, static class that acts as the entry point for all logging requests. It maintains a collection of `ILogger` instances and forwards incoming log messages to all registered loggers, respecting their individual `MinimumLogLevel` settings.
5.  **`LogSystemInitializer` (MonoBehaviour):** A Unity-specific component that initializes the `Log` facade, adds concrete loggers based on inspector settings, and handles shutdown procedures.
6.  **`ExampleUsage` (MonoBehaviour):** Demonstrates how to use the `Log` facade from various parts of your application.

---

### Instructions to Use This Script in Unity:

1.  **Create a C# Script:** In your Unity project, create a new C# script named `LoggingSystem.cs`.
2.  **Copy and Paste:** Copy the entire code below and paste it into the `LoggingSystem.cs` file, replacing its default content.
3.  **Create an Initializer GameObject:** In your Unity scene, create an empty GameObject (e.g., right-click in the Hierarchy -> Create Empty) and name it `LogSystemManager`.
4.  **Attach `LogSystemInitializer`:** Drag and drop the `LoggingSystem.cs` file onto the `LogSystemManager` GameObject in the Inspector, or use the "Add Component" button and search for "Log System Initializer".
5.  **Configure in Inspector:** Select the `LogSystemManager` GameObject. In its Inspector, you can now configure the settings for the Unity Console Logger and File Logger (enable/disable, set minimum log levels, change file name).
6.  **Attach `ExampleUsage` (Optional):** To see the logging in action, create another empty GameObject (e.g., `LoggerExample`) and attach the `ExampleUsage` script component to it.
7.  **Run Your Scene:** Play the scene. You will see log messages appearing in Unity's Console window and a log file being created in your `Application.persistentDataPath` (e.g., `C:\Users\<username>\AppData\LocalLow\<company_name>\<product_name>\game_log.txt` on Windows).

---

### `LoggingSystem.cs`

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Required for LINQ extensions like FirstOrDefault
using System.Collections; // Required for Coroutines in ExampleUsage

// This entire script is provided as a single file for ease of dropping into a Unity project.
// In a larger, more organized project, each class/interface would typically reside in its own C# file.

#region LogLevel Enum
/// <summary>
/// Defines the different severity levels for log messages.
/// Higher values usually indicate more critical issues.
/// </summary>
public enum LogLevel
{
    Debug,   // Detailed information, useful for debugging.
    Info,    // General application flow, useful for tracking.
    Warning, // Potential issues, but the application can continue.
    Error,   // Significant problems, often requiring attention.
    Fatal    // Critical errors that cause application failure or instability.
}
#endregion

#region ILogger Interface
/// <summary>
/// The ILogger interface defines the contract for all concrete logging implementations.
/// This is a core component of the LoggingSystemPattern, allowing for interchangeable loggers.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Gets a unique name for this logger instance.
    /// </summary>
    string LoggerName { get; }

    /// <summary>
    /// Gets or sets the minimum log level this logger will process.
    /// Messages with a level lower than this will be ignored by this logger.
    /// </summary>
    LogLevel MinimumLogLevel { get; set; }

    /// <summary>
    /// The primary method for logging a message with a specific level.
    /// </summary>
    /// <param name="level">The severity level of the message.</param>
    /// <param name="message">The log message string.</param>
    /// <param name="context">The object associated with this log message (for Unity's console).</param>
    void Log(LogLevel level, string message, UnityEngine.Object context = null);

    // Convenience methods for specific log levels
    void LogInfo(string message, UnityEngine.Object context = null);
    void LogWarning(string message, UnityEngine.Object context = null);
    void LogError(string message, UnityEngine.Object context = null);
    void LogDebug(string message, UnityEngine.Object context = null);
    void LogFatal(string message, UnityEngine.Object context = null);

    /// <summary>
    /// Called when the logger needs to release resources (e.g., close file streams).
    /// </summary>
    void Shutdown();
}
#endregion

#region UnityConsoleLogger Class
/// <summary>
/// A concrete implementation of ILogger that outputs messages to Unity's console.
/// It uses Debug.Log, Debug.LogWarning, and Debug.LogError based on the LogLevel.
/// </summary>
public class UnityConsoleLogger : ILogger
{
    public string LoggerName => "UnityConsoleLogger";
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug; // Default to log all debug messages

    /// <summary>
    /// Logs a message to the Unity console, formatting it and using the appropriate Debug.Log method.
    /// </summary>
    /// <param name="level">The severity level of the message.</param>
    /// <param name="message">The log message string.</param>
    /// <param name="context">The object associated with this log message (clicking the log in Unity console will highlight this object).</param>
    public void Log(LogLevel level, string message, UnityEngine.Object context = null)
    {
        // Only log if the message's level is at or above this logger's minimum level
        if (level < MinimumLogLevel) return;

        // Basic formatting for the console output
        string formattedMessage = $"[{DateTime.Now:HH:mm:ss}][{level.ToString().ToUpper()}][{LoggerName}] {message}";

        switch (level)
        {
            case LogLevel.Debug:
                // Debug.Log for detailed, development-time messages
                Debug.Log($"<color=#888888>{formattedMessage}</color>", context); // Gray for debug
                break;
            case LogLevel.Info:
                // Debug.Log for general informational messages
                Debug.Log(formattedMessage, context);
                break;
            case LogLevel.Warning:
                // Debug.LogWarning for potential issues
                Debug.LogWarning(formattedMessage, context);
                break;
            case LogLevel.Error:
            case LogLevel.Fatal:
                // Debug.LogError for significant problems or critical errors
                Debug.LogError(formattedMessage, context);
                break;
        }
    }

    // Convenience methods implementing the ILogger interface by calling the main Log method.
    public void LogInfo(string message, UnityEngine.Object context = null) => Log(LogLevel.Info, message, context);
    public void LogWarning(string message, UnityEngine.Object context = null) => Log(LogLevel.Warning, message, context);
    public void LogError(string message, UnityEngine.Object context = null) => Log(LogLevel.Error, message, context);
    public void LogDebug(string message, UnityEngine.Object context = null) => Log(LogLevel.Debug, message, context);
    public void LogFatal(string message, UnityEngine.Object context = null) => Log(LogLevel.Fatal, message, context);

    /// <summary>
    /// No specific shutdown procedure is required for Unity's Debug.Log,
    /// but this method is part of the ILogger contract.
    /// </summary>
    public void Shutdown()
    {
        // Debug.Log("UnityConsoleLogger shutting down."); // Optional: log shutdown itself
    }
}
#endregion

#region FileLogger Class
/// <summary>
/// A concrete implementation of ILogger that writes messages to a text file.
/// This logger ensures that log data is persisted beyond the current application session.
/// </summary>
public class FileLogger : ILogger
{
    public string LoggerName => "FileLogger";
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Warning; // Default to Warning for file logs
    private string _logFilePath;
    private StreamWriter _streamWriter;

    /// <summary>
    /// Constructor for FileLogger. Initializes the log file path and attempts to open the stream.
    /// </summary>
    /// <param name="logFileName">The name of the log file (e.g., "game_log.txt").</param>
    public FileLogger(string logFileName = "game_log.txt")
    {
        // Application.persistentDataPath is a recommended location for persistent data
        // as it's typically user-specific and writable across different platforms.
        _logFilePath = Path.Combine(Application.persistentDataPath, logFileName);
        InitializeLogFile();
    }

    /// <summary>
    /// Initializes the StreamWriter for the log file. Handles directory creation and error cases.
    /// </summary>
    private void InitializeLogFile()
    {
        try
        {
            // Ensure the directory for the log file exists
            string logDir = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // Open the file in append mode. If it doesn't exist, it will be created.
            // Using 'true' for the second argument of StreamWriter to enable appending.
            _streamWriter = new StreamWriter(_logFilePath, true);
            _streamWriter.AutoFlush = true; // Essential to ensure logs are written to disk immediately

            // Write a session start marker to the log file
            _streamWriter.WriteLine($"--- Log Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---");
            _streamWriter.WriteLine($"Log file location: {_logFilePath}");
        }
        catch (Exception ex)
        {
            // If file logging fails, log an error to the Unity console
            // and disable the file logger to prevent further exceptions.
            Debug.LogError($"[FileLogger] Failed to initialize file logger at {_logFilePath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
            _streamWriter = null; // Mark as null to disable further writes
        }
    }

    /// <summary>
    /// Logs a message to the file.
    /// </summary>
    /// <param name="level">The severity level of the message.</param>
    /// <param name="message">The log message string.</param>
    /// <param name="context">Context is not directly used for file logging but is part of the ILogger contract.</param>
    public void Log(LogLevel level, string message, UnityEngine.Object context = null)
    {
        // Only log if the stream writer is valid and the message's level is sufficient
        if (_streamWriter == null || level < MinimumLogLevel) return;

        string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][{level.ToString().ToUpper()}][{LoggerName}] {message}";
        try
        {
            _streamWriter.WriteLine(formattedMessage);
        }
        catch (Exception ex)
        {
            // If writing fails, log to console and disable writer
            Debug.LogError($"[FileLogger] Failed to write to log file: {ex.Message}");
            _streamWriter = null;
        }
    }

    // Convenience methods implementing the ILogger interface.
    public void LogInfo(string message, UnityEngine.Object context = null) => Log(LogLevel.Info, message, context);
    public void LogWarning(string message, UnityEngine.Object context = null) => Log(LogLevel.Warning, message, context);
    public void LogError(string message, UnityEngine.Object context = null) => Log(LogLevel.Error, message, context);
    public void LogDebug(string message, UnityEngine.Object context = null) => Log(LogLevel.Debug, message, context);
    public void LogFatal(string message, UnityEngine.Object context = null) => Log(LogLevel.Fatal, message, context);

    /// <summary>
    /// Shuts down the file logger by closing the StreamWriter.
    /// This ensures all buffered data is written and the file handle is released.
    /// This is critical for file-based loggers.
    /// </summary>
    public void Shutdown()
    {
        if (_streamWriter != null)
        {
            try
            {
                _streamWriter.WriteLine($"--- Log Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---");
                _streamWriter.Close(); // Close the stream
                _streamWriter.Dispose(); // Release resources
                _streamWriter = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileLogger] Error during shutdown: {ex.Message}");
            }
        }
    }
}
#endregion

#region Log (Facade/Manager) Class
/// <summary>
/// The central static facade for the LoggingSystemPattern.
/// This class provides a unified, simple interface for logging across the entire application,
/// abstracting away the concrete logging implementations. All log requests go through this class.
/// </summary>
public static class Log
{
    // A list to hold all registered ILogger instances. This allows for multiple log outputs simultaneously.
    private static readonly List<ILogger> _loggers = new List<ILogger>();
    private static bool _isInitialized = false; // Flag to ensure initialization happens once

    // Static constructor. While possible, explicit Initialize() is better in Unity for control.
    static Log()
    {
        // Static constructor can run at unpredictable times.
        // It's generally better to use an explicit MonoBehaviour-based initializer in Unity.
    }

    /// <summary>
    /// Initializes the logging system. This method should be called once at application startup
    /// (e.g., from a dedicated LogSystemInitializer MonoBehaviour). It prepares the system
    /// to accept loggers and handle messages.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            // Use Unity's Debug.LogWarning directly if our own system is already initialized,
            // as this warning is about the initializer itself.
            Debug.LogWarning("[Log] Logging system already initialized. Skipping re-initialization.");
            return;
        }

        // Clear existing loggers in case of unexpected re-initialization scenarios
        // (though ideally, it should only be called once).
        _loggers.Clear();
        _isInitialized = true;
        // The first log message uses Debug.Log directly because our system isn't fully set up yet
        // with the user-defined concrete loggers.
        Debug.Log("[Log] Core logging system initialized. Ready to accept loggers.");
    }

    /// <summary>
    /// Adds a concrete logger instance to the logging system.
    /// All subsequent log messages will be processed by this new logger (if its level allows).
    /// </summary>
    /// <param name="logger">The ILogger instance to add (e.g., new UnityConsoleLogger()).</param>
    public static void AddLogger(ILogger logger)
    {
        if (logger == null)
        {
            Debug.LogError("[Log] Attempted to add a null logger.");
            return;
        }
        if (_loggers.Contains(logger))
        {
            Debug.LogWarning($"[Log] Logger '{logger.LoggerName}' is already registered. Skipping add.");
            return;
        }
        _loggers.Add(logger);
        Info($"[Log] Added logger: {logger.LoggerName} (Min Level: {logger.MinimumLogLevel})");
    }

    /// <summary>
    /// Removes a logger from the system. The removed logger will no longer receive messages,
    /// and its Shutdown() method will be called to release resources.
    /// </summary>
    /// <param name="logger">The ILogger instance to remove.</param>
    public static void RemoveLogger(ILogger logger)
    {
        if (logger == null) return;

        if (_loggers.Remove(logger))
        {
            Info($"[Log] Removed logger: {logger.LoggerName}");
            logger.Shutdown(); // Important: Call shutdown on the removed logger
        }
        else
        {
            Debug.LogWarning($"[Log] Logger '{logger.LoggerName}' was not found in the registered list, cannot remove.");
        }
    }

    /// <summary>
    /// Removes a logger by its name. This is useful if you don't have a direct reference to the logger object.
    /// </summary>
    /// <param name="loggerName">The name of the logger to remove.</param>
    public static void RemoveLogger(string loggerName)
    {
        ILogger loggerToRemove = _loggers.FirstOrDefault(l => l.LoggerName == loggerName);
        if (loggerToRemove != null)
        {
            RemoveLogger(loggerToRemove);
        }
        else
        {
            Debug.LogWarning($"[Log] Logger with name '{loggerName}' was not found. Cannot remove.");
        }
    }

    /// <summary>
    /// Sets the minimum log level for a specific registered logger.
    /// Messages below this level will be ignored by that particular logger.
    /// </summary>
    /// <param name="loggerName">The name of the logger to configure.</param>
    /// <param name="level">The new minimum log level.</param>
    public static void SetLoggerMinimumLevel(string loggerName, LogLevel level)
    {
        ILogger logger = _loggers.FirstOrDefault(l => l.LoggerName == loggerName);
        if (logger != null)
        {
            logger.MinimumLogLevel = level;
            Info($"[Log] Set minimum log level for '{logger.LoggerName}' to {level}.");
        }
        else
        {
            Debug.LogWarning($"[Log] Logger '{loggerName}' not found. Cannot set minimum log level.");
        }
    }

    /// <summary>
    /// Shuts down all registered loggers. This should be called when the application quits
    /// to ensure all resources are released (e.g., file streams closed).
    /// </summary>
    public static void ShutdownAllLoggers()
    {
        if (!_isInitialized) return; // Only shutdown if initialized

        Debug.Log("[Log] Shutting down all loggers..."); // Use Debug.Log before our system might be off
        foreach (var logger in _loggers)
        {
            logger.Shutdown();
        }
        _loggers.Clear();
        _isInitialized = false; // Mark as uninitialized
        Debug.Log("[Log] All loggers shut down.");
    }

    // --- Core Logging Method ---
    /// <summary>
    /// The internal method that dispatches a log message to all registered loggers.
    /// Each logger decides whether to process the message based on its own minimum log level.
    /// </summary>
    private static void LogMessage(LogLevel level, string message, UnityEngine.Object context = null)
    {
        // Fallback to Unity's Debug.Log if the system isn't initialized yet.
        // This can happen if a log call is made very early in the application lifecycle.
        if (!_isInitialized)
        {
            string fallbackMessage = $"[UNINITIALIZED LOG - {level.ToString().ToUpper()}] {message}";
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(fallbackMessage, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(fallbackMessage, context);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Debug.LogError(fallbackMessage, context);
                    break;
            }
            return;
        }

        // Iterate through all registered loggers and tell them to process the message.
        // Each logger's `Log` method will internally check its `MinimumLogLevel`.
        foreach (var logger in _loggers)
        {
            logger.Log(level, message, context);
        }
    }

    // --- Public Convenience Methods for Logging ---
    // These methods provide a simple, type-safe way to log messages at specific levels
    // without needing to pass the LogLevel enum explicitly.

    /// <summary>Logs a Debug message.</summary>
    public static void Debug(string message, UnityEngine.Object context = null) => LogMessage(LogLevel.Debug, message, context);

    /// <summary>Logs an Info message.</summary>
    public static void Info(string message, UnityEngine.Object context = null) => LogMessage(LogLevel.Info, message, context);

    /// <summary>Logs a Warning message.</summary>
    public static void Warning(string message, UnityEngine.Object context = null) => LogMessage(LogLevel.Warning, message, context);

    /// <summary>Logs an Error message.</summary>
    public static void Error(string message, UnityEngine.Object context = null) => LogMessage(LogLevel.Error, message, context);

    /// <summary>Logs a Fatal message (highest severity).</summary>
    public static void Fatal(string message, UnityEngine.Object context = null) => LogMessage(LogLevel.Fatal, message, context);

    /// <summary>
    /// Logs an exception with an optional accompanying message.
    /// This method also calls Unity's Debug.LogException to ensure the full stack trace
    /// is available in the Unity console, which is often more detailed.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="message">An optional introductory message.</param>
    /// <param name="context">The object associated with this exception (for Unity's console).</param>
    public static void Exception(Exception ex, string message = null, UnityEngine.Object context = null)
    {
        string logMessage = message != null ? $"{message}\nException: {ex.GetType().Name}: {ex.Message}\nStackTrace:\n{ex.StackTrace}" : $"Exception: {ex.GetType().Name}: {ex.Message}\nStackTrace:\n{ex.StackTrace}";
        LogMessage(LogLevel.Error, logMessage, context);

        // Also call Unity's native LogException for better stack trace formatting in the console
        if (context != null)
        {
            UnityEngine.Debug.LogException(ex, context);
        }
        else
        {
            UnityEngine.Debug.LogException(ex);
        }
    }
}
#endregion

#region LogSystemInitializer MonoBehaviour
/// <summary>
/// This MonoBehaviour is responsible for initializing and configuring the central Log system.
/// It acts as the entry point for setting up which loggers are active and their respective settings
/// based on values configured in the Unity Inspector.
/// </summary>
public class LogSystemInitializer : MonoBehaviour
{
    // [SerializeField] allows these private fields to be exposed and configured in the Unity Inspector.
    // This provides a user-friendly way to manage logging settings without modifying code.

    [Header("Unity Console Logger Settings")]
    [Tooltip("Enable or disable the Unity Console Logger.")]
    [SerializeField] private bool _enableUnityConsoleLogger = true;
    [Tooltip("Minimum log level for the Unity Console Logger. Messages below this level will be ignored by this logger.")]
    [SerializeField] private LogLevel _unityConsoleMinLevel = LogLevel.Debug;

    [Header("File Logger Settings")]
    [Tooltip("Enable or disable the File Logger. Log files are saved in Application.persistentDataPath.")]
    [SerializeField] private bool _enableFileLogger = true;
    [Tooltip("Minimum log level for the File Logger. Messages below this level will be ignored by this logger.")]
    [SerializeField] private LogLevel _fileLoggerMinLevel = LogLevel.Warning;
    [Tooltip("Name of the log file (e.g., 'game_log.txt').")]
    [SerializeField] private string _logFileName = "game_log.txt";

    // You can extend this with more serialized fields for other logger types
    // (e.g., for a remote logger, a database logger, etc.).

    private void Awake()
    {
        // Make this GameObject persist across scene loads. This is crucial for a logging system
        // to ensure it remains active throughout the application's entire lifecycle.
        DontDestroyOnLoad(gameObject);

        // --- Step 1: Initialize the core Log facade ---
        // This prepares the static Log class to manage loggers.
        Log.Initialize();

        // --- Step 2: Add and configure concrete loggers based on Inspector settings ---
        // This demonstrates how to dynamically assemble the logging pipeline.

        if (_enableUnityConsoleLogger)
        {
            var unityLogger = new UnityConsoleLogger
            {
                MinimumLogLevel = _unityConsoleMinLevel
            };
            Log.AddLogger(unityLogger);
        }

        if (_enableFileLogger)
        {
            // Ensure the log file name is not empty
            if (string.IsNullOrWhiteSpace(_logFileName))
            {
                Debug.LogWarning("[LogSystemInitializer] Log file name cannot be empty. Using default 'game_log.txt'.");
                _logFileName = "game_log.txt";
            }
            var fileLogger = new FileLogger(_logFileName)
            {
                MinimumLogLevel = _fileLoggerMinLevel
            };
            Log.AddLogger(fileLogger);
        }

        // Example: Dynamically adding another logger at runtime if needed (not configured via Inspector)
        // Log.AddLogger(new CustomNetworkLogger("http://my-log-server.com/api/logs"));

        // Log a confirmation message using our newly configured system
        Log.Info($"[{nameof(LogSystemInitializer)}] Logging system fully configured and ready.");
        Log.Debug($"[{nameof(LogSystemInitializer)}] Application persistent data path: {Application.persistentDataPath}");
    }

    private void OnApplicationQuit()
    {
        // --- Step 3: Shut down all loggers gracefully when the application quits ---
        // This is extremely important for loggers that hold open resources (like FileLogger's file stream)
        // to ensure all pending messages are written to disk and resources are properly closed and released.
        Log.ShutdownAllLoggers();
    }
}
#endregion

#region ExampleUsage MonoBehaviour
/// <summary>
/// This script provides practical examples of how to use the LogSystemPattern facade
/// from various parts of your Unity application.
/// Attach this to any GameObject in your scene to see it in action.
/// Ensure that a `LogSystemInitializer` GameObject is present and configured in your scene.
/// </summary>
public class ExampleUsage : MonoBehaviour
{
    private int _updateCount = 0;

    void Start()
    {
        // Log an informational message. This will go to all active loggers
        // (Unity Console and File Logger, if enabled and level permits).
        Log.Info($"[{nameof(ExampleUsage)}] Application started. Log system is active!");

        // Log different levels of messages:
        Log.Debug($"[{nameof(ExampleUsage)}] This is a debug message. It's very verbose and often filtered out in production builds.");
        Log.Info($"[{nameof(ExampleUsage)}] Player '{SystemInfo.deviceName}' has started a new game session.");
        Log.Warning($"[{nameof(ExampleUsage)}] Low disk space detected (less than 1GB free). This might affect save game operations.", this);

        // Log an error message with a 'context' object.
        // In the Unity Console, clicking this log will highlight this 'ExampleUsage' GameObject.
        Log.Error($"[{nameof(ExampleUsage)}] Failed to load configuration file 'game_settings.json'. Using default settings.", this);

        // --- Demonstrating Exception Logging ---
        // It's good practice to log exceptions with the dedicated Log.Exception method.
        // This ensures full stack trace information is captured.
        try
        {
            // Simulate a method that throws an exception
            SimulateInvalidOperation();
        }
        catch (InvalidOperationException ex)
        {
            // Catch the specific exception and log it using our system.
            Log.Exception(ex, $"[{nameof(ExampleUsage)}] Caught an expected exception during startup configuration.", this);
        }

        // Start a coroutine to log messages at regular intervals
        StartCoroutine(TimedLogRoutine());

        // You can dynamically change logger settings at runtime too
        Log.SetLoggerMinimumLevel("FileLogger", LogLevel.Info); // Now file logger will also log Info messages
        Log.Info($"[{nameof(ExampleUsage)}] FileLogger minimum level changed to INFO dynamically.");
    }

    void Update()
    {
        _updateCount++;

        // Log a debug message every 100 frames. This is useful for monitoring performance/state.
        if (_updateCount % 100 == 0)
        {
            Log.Debug($"[{nameof(ExampleUsage)}] Update loop running. Frame count: {Time.frameCount}. Mouse X: {Input.mousePosition.x}");
        }

        // --- Respond to user input to trigger various log messages ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Log.Info($"[{nameof(ExampleUsage)}] Space key pressed! Current Game Time: {Time.time:F2} seconds.", this);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Simulate an error condition
            Log.Error($"[{nameof(ExampleUsage)}] Critical game state error triggered by 'E' key press!", this);
            try
            {
                // Simulate an exception that would typically halt a process
                SimulateDivideByZero();
            }
            catch (DivideByZeroException ex)
            {
                // Log this runtime exception.
                Log.Exception(ex, $"[{nameof(ExampleUsage)}] A simulated runtime exception was caught after 'E' press.", this);
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            // Simulate a warning condition
            Log.Warning($"[{nameof(ExampleUsage)}] Player tried to access a locked area. Access denied.", this);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            // Simulate a fatal error - something that should not happen
            Log.Fatal($"[{nameof(ExampleUsage)}] Unrecoverable engine error! Forcing application shutdown now (simulated).", this);
            // In a real scenario, you might quit the application here: Application.Quit();
        }
    }

    /// <summary>
    /// A coroutine that logs an info message every 5 seconds.
    /// Useful for background monitoring or periodic status updates.
    /// </summary>
    private IEnumerator TimedLogRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // Wait for 5 seconds
            Log.Info($"[{nameof(ExampleUsage)}] This message is logged periodically every 5 seconds. Current Time: {Time.timeSinceLevelLoad:F2}s.");
            Log.Debug($"[{nameof(ExampleUsage)}] Current memory usage (approx): {GC.GetTotalMemory(false) / (1024 * 1024)} MB.");
        }
    }

    /// <summary>
    /// Helper method to simulate an exception.
    /// </summary>
    private void SimulateInvalidOperation()
    {
        throw new InvalidOperationException("This is a simulated invalid operation specific to our example!");
    }

    /// <summary>
    /// Helper method to simulate a divide by zero exception.
    /// </summary>
    private void SimulateDivideByZero()
    {
        int numerator = 10;
        int denominator = 0;
        int result = numerator / denominator; // This line will throw the exception
    }

    void OnDestroy()
    {
        // Log a message when this GameObject is destroyed.
        // This demonstrates logging during object lifecycle events.
        Log.Info($"[{nameof(ExampleUsage)}] GameObject '{gameObject.name}' destroyed. Logging from OnDestroy.");
    }
}
#endregion
```