// Unity Design Pattern Example: SaveCompressionSystem
// This script demonstrates the SaveCompressionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Save Compression System** design pattern by combining the **Strategy Pattern** for compression and encryption, and the **Facade Pattern** for the overall `SaveSystem`. This allows you to easily swap out different compression algorithms (e.g., GZip, Deflate, or no compression) and encryption methods (e.g., AES, simple XOR, or no encryption) without altering the core saving/loading logic.

**Key Design Pattern Concepts Applied:**

1.  **Strategy Pattern (ICompressionStrategy, IEncryptionStrategy):**
    *   Defines a family of algorithms (compression/encryption).
    *   Encapsulates each algorithm into a separate class.
    *   Makes the algorithms interchangeable at runtime.
    *   The `SaveSystem` (context) uses these strategies without knowing their concrete implementations.

2.  **Facade Pattern (SaveSystem):**
    *   Provides a unified interface to a set of interfaces in a subsystem.
    *   The `SaveSystem` simplifies the complex operations of serialization, compression, encryption, and file I/O into simple `Save()` and `Load()` methods.
    *   Clients (like `SaveSystemDemo`) interact with the facade, not the individual components.

---

### Instructions to Use in Unity:

1.  **Create a C# Script:** In your Unity project, create a new C# script named `SaveSystemExample.cs`.
2.  **Copy and Paste:** Copy the entire code block below and paste it into your `SaveSystemExample.cs` file, replacing its default content.
3.  **Attach to GameObject:** Create an empty GameObject in your scene (e.g., named "SaveManager"). Attach the `SaveSystemExample` script to this GameObject.
4.  **Run Scene:** Play the scene. You will see a simple GUI overlay.
5.  **Interact:**
    *   Click "Save Game" to save some dummy data.
    *   Click "Load Game" to load it back.
    *   Click "Delete Save File" to remove the save file.
    *   Toggle "Use GZip Compression" and "Use AES Encryption" to change the strategies on the fly and see how the file sizes and security implications change (e.g., loading a GZip compressed file with compression disabled will fail).
    *   Check your Unity Console for detailed logs of the saving/loading process, including compression and encryption steps.
    *   The save files will be located in `Application.persistentDataPath` (e.g., `C:\Users\<username>\AppData\LocalLow\<CompanyName>\<ProductName>\PlayerSaves` on Windows).

---

```csharp
using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Security.Cryptography; // For AES Encryption

// ====================================================================================
// SECTION 1: Savable Data Structure
//
// This is an example of a data class that holds game-specific information.
// It must be marked with [Serializable] for JsonUtility to work.
// ====================================================================================

/// <summary>
/// Represents the actual game data that will be saved and loaded.
/// Must be marked [Serializable] for JsonUtility.ToJson/FromJson to work.
/// </summary>
[Serializable]
public class GameData
{
    public string playerName;
    public int playerScore;
    public float gameProgress;
    public Vector3 playerPosition; // Vector3 is already serializable by JsonUtility

    public GameData(string name, int score, float progress, Vector3 position)
    {
        playerName = name;
        playerScore = score;
        gameProgress = progress;
        playerPosition = position;
    }

    public override string ToString()
    {
        return $"Player: {playerName}, Score: {playerScore}, Progress: {gameProgress:F2}, Position: {playerPosition}";
    }
}

// ====================================================================================
// SECTION 2: Compression Strategy (Strategy Pattern)
//
// These interfaces and classes define different ways to compress and decompress data.
// The SaveSystem uses an ICompressionStrategy without knowing its concrete type.
// ====================================================================================

/// <summary>
/// Interface for different data compression strategies.
/// This is a key part of the 'SaveCompressionSystem' pattern, allowing interchangeable compression.
/// </summary>
public interface ICompressionStrategy
{
    byte[] Compress(byte[] data);
    byte[] Decompress(byte[] compressedData);
}

/// <summary>
/// A concrete compression strategy that does not perform any compression.
/// Data is passed through directly. Useful for debugging or when compression is not needed.
/// </summary>
public class NoCompressionStrategy : ICompressionStrategy
{
    public byte[] Compress(byte[] data)
    {
        Debug.Log("NoCompressionStrategy: Data passed through without compression.");
        return data;
    }

    public byte[] Decompress(byte[] compressedData)
    {
        Debug.Log("NoCompressionStrategy: Data passed through without decompression.");
        return compressedData;
    }
}

/// <summary>
/// A concrete compression strategy using GZip.
/// GZip provides good compression ratios for general data and is a standard algorithm.
/// </summary>
public class GZipCompressionStrategy : ICompressionStrategy
{
    public byte[] Compress(byte[] data)
    {
        if (data == null || data.Length == 0) return data;

        using (MemoryStream outputStream = new MemoryStream())
        {
            // GZipStream writes compressed data to the base stream upon disposal/closing
            using (GZipStream gzipStream = new GZipStream(outputStream, CompressionMode.Compress, true))
            {
                gzipStream.Write(data, 0, data.Length);
            } // gzipStream is closed and flushed here, ensuring all compressed data is in outputStream

            Debug.Log($"GZipCompressionStrategy: Compressed {data.Length} bytes to {outputStream.Length} bytes.");
            return outputStream.ToArray();
        }
    }

    public byte[] Decompress(byte[] compressedData)
    {
        if (compressedData == null || compressedData.Length == 0) return compressedData;

        using (MemoryStream inputStream = new MemoryStream(compressedData))
        using (GZipStream gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        using (MemoryStream outputStream = new MemoryStream())
        {
            // Copy decompressed data from GZipStream to the outputStream
            gzipStream.CopyTo(outputStream);
            Debug.Log($"GZipCompressionStrategy: Decompressed {compressedData.Length} bytes to {outputStream.Length} bytes.");
            return outputStream.ToArray();
        }
    }
}

// ====================================================================================
// SECTION 3: Encryption Strategy (Strategy Pattern)
//
// These interfaces and classes define different ways to encrypt and decrypt data.
// The SaveSystem uses an IEncryptionStrategy without knowing its concrete type.
// ====================================================================================

/// <summary>
/// Interface for different data encryption strategies.
/// This allows swapping out encryption methods without changing the core SaveSystem.
/// </summary>
public interface IEncryptionStrategy
{
    byte[] Encrypt(byte[] data, string key);
    byte[] Decrypt(byte[] encryptedData, string key);
}

/// <summary>
/// A concrete encryption strategy that does not perform any encryption.
/// Data is passed through directly. Useful for debugging or when encryption is not desired.
/// </summary>
public class NoEncryptionStrategy : IEncryptionStrategy
{
    public byte[] Encrypt(byte[] data, string key)
    {
        Debug.Log("NoEncryptionStrategy: Data passed through without encryption.");
        return data;
    }

    public byte[] Decrypt(byte[] encryptedData, string key)
    {
        Debug.Log("NoEncryptionStrategy: Data passed through without decryption.");
        return encryptedData;
    }
}

/// <summary>
/// A concrete encryption strategy using AES (Advanced Encryption Standard).
/// This provides strong, industry-standard encryption.
/// IMPORTANT: Key and IV management are crucial for real-world security.
/// For this example, a fixed salt and iteration count are used with PBKDF2
/// to derive key and IV from a passphrase. In a production environment,
/// carefully consider how to handle encryption keys.
/// </summary>
public class AesEncryptionStrategy : IEncryptionStrategy
{
    private const int KeySize = 256; // AES-256
    private const int BlockSize = 128; // 128 bits = 16 bytes for IV
    private const int Iterations = 10000; // Number of PBKDF2 iterations
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("UnitySaveSystemSaltForAES"); // A fixed salt for key derivation

    /// <summary>
    /// Derives a cryptographic key and initialization vector (IV) from a passphrase using PBKDF2.
    /// </summary>
    private (byte[] key, byte[] iv) DeriveKeyAndIV(string passphrase)
    {
        using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(passphrase, Salt, Iterations, HashAlgorithmName.SHA256))
        {
            byte[] key = rfc2898DeriveBytes.GetBytes(KeySize / 8); // 32 bytes for 256-bit key
            byte[] iv = rfc2898DeriveBytes.GetBytes(BlockSize / 8); // 16 bytes for 128-bit IV
            return (key, iv);
        }
    }

    public byte[] Encrypt(byte[] data, string key)
    {
        if (data == null || data.Length == 0) return data;
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Encryption key cannot be null or empty.", nameof(key));

        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                (byte[] derivedKey, byte[] derivedIV) = DeriveKeyAndIV(key);
                aesAlg.Key = derivedKey;
                aesAlg.IV = derivedIV;
                aesAlg.Mode = CipherMode.CBC; // Cipher Block Chaining mode
                aesAlg.Padding = PaddingMode.PKCS7; // Standard padding

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                        // csEncrypt.FlushFinalBlock(); // Not explicitly needed with 'using' and disposing the stream
                    }
                    Debug.Log($"AesEncryptionStrategy: Encrypted {data.Length} bytes.");
                    return msEncrypt.ToArray();
                }
            }
        }
        catch (CryptographicException e)
        {
            Debug.LogError($"AesEncryptionStrategy Encryption Error: {e.Message}");
            return null;
        }
    }

    public byte[] Decrypt(byte[] encryptedData, string key)
    {
        if (encryptedData == null || encryptedData.Length == 0) return encryptedData;
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Encryption key cannot be null or empty.", nameof(key));

        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                (byte[] derivedKey, byte[] derivedIV) = DeriveKeyAndIV(key);
                aesAlg.Key = derivedKey;
                aesAlg.IV = derivedIV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream msPlain = new MemoryStream())
                        {
                            csDecrypt.CopyTo(msPlain); // Read all decrypted data into msPlain
                            Debug.Log($"AesEncryptionStrategy: Decrypted {encryptedData.Length} bytes.");
                            return msPlain.ToArray();
                        }
                    }
                }
            }
        }
        catch (CryptographicException e)
        {
            // This often means the key is wrong or the data is corrupted.
            Debug.LogError($"AesEncryptionStrategy Decryption Error (likely wrong key, corrupt data, or invalid padding): {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"AesEncryptionStrategy Unexpected Decryption Error: {e.Message}");
            return null;
        }
    }
}

// ====================================================================================
// SECTION 4: SaveSystem (Facade Pattern)
//
// This is the core SaveSystem, acting as a Facade. It orchestrates all saving
// and loading steps using the chosen compression and encryption strategies.
// ====================================================================================

/// <summary>
/// The core SaveSystem, acting as a Facade to simplify saving and loading operations.
/// It orchestrates serialization, compression, encryption, and file I/O using
/// the provided strategy objects. This is the 'Save System' part of the pattern.
/// </summary>
public class SaveSystem
{
    private readonly ICompressionStrategy _compressionStrategy;
    private readonly IEncryptionStrategy _encryptionStrategy;
    private readonly string _savePath;

    /// <summary>
    /// Initializes a new instance of the SaveSystem.
    /// The client provides concrete compression and encryption strategies.
    /// </summary>
    /// <param name="compressionStrategy">The strategy to use for data compression.</param>
    /// <param name="encryptionStrategy">The strategy to use for data encryption.</param>
    /// <param name="folderName">Optional: A subfolder within Application.persistentDataPath to store save files.</param>
    public SaveSystem(ICompressionStrategy compressionStrategy, IEncryptionStrategy encryptionStrategy, string folderName = "PlayerSaves")
    {
        // Ensure strategies are provided. If not, default to NoCompression/NoEncryption for robustness.
        // Though it's better to explicitly pass NoCompressionStrategy if no compression is desired.
        _compressionStrategy = compressionStrategy ?? new NoCompressionStrategy();
        _encryptionStrategy = encryptionStrategy ?? new NoEncryptionStrategy();
        _savePath = Path.Combine(Application.persistentDataPath, folderName);

        // Ensure the save directory exists
        if (!Directory.Exists(_savePath))
        {
            Directory.CreateDirectory(_savePath);
            Debug.Log($"SaveSystem: Created save directory: {_savePath}");
        }
    }

    /// <summary>
    /// Saves game data to a file.
    /// The data is first serialized to JSON, then compressed, then encrypted, and finally written to disk.
    /// </summary>
    /// <typeparam name="T">The type of data to save (must be [Serializable]).</typeparam>
    /// <param name="data">The data object to save.</param>
    /// <param name="fileName">The name of the file to save to (e.g., "playerProgress.sav").</param>
    /// <param name="encryptionKey">The key to use for encryption. Can be null or empty if using NoEncryptionStrategy.</param>
    /// <returns>True if save was successful, false otherwise.</returns>
    public bool Save<T>(T data, string fileName, string encryptionKey)
    {
        if (data == null)
        {
            Debug.LogError("SaveSystem: Cannot save null data.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogError("SaveSystem: File name cannot be null or empty.");
            return false;
        }

        string fullPath = Path.Combine(_savePath, fileName);

        try
        {
            // 1. Serialize data to JSON string using Unity's built-in JsonUtility
            string jsonString = JsonUtility.ToJson(data);
            Debug.Log($"SaveSystem: Serialized data to JSON. Size: {Encoding.UTF8.GetByteCount(jsonString)} bytes (uncompressed, unencrypted).");

            // 2. Convert JSON string to byte array (UTF-8 encoding is standard)
            byte[] bytesToSave = Encoding.UTF8.GetBytes(jsonString);

            // 3. Apply the chosen compression strategy
            bytesToSave = _compressionStrategy.Compress(bytesToSave);

            // 4. Apply the chosen encryption strategy
            bytesToSave = _encryptionStrategy.Encrypt(bytesToSave, encryptionKey);
            if (bytesToSave == null) // Encryption failed (e.g., due to a CryptographicException)
            {
                Debug.LogError("SaveSystem: Encryption step failed during save.");
                return false;
            }

            // 5. Write the final byte array to the file system
            File.WriteAllBytes(fullPath, bytesToSave);
            Debug.Log($"SaveSystem: Successfully saved data to {fullPath}. Total file size on disk: {bytesToSave.Length} bytes.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: Failed to save data to {fullPath}. Error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads game data from a file.
    /// The data is first read from disk, then decrypted, then decompressed, and finally deserialized from JSON.
    /// </summary>
    /// <typeparam name="T">The expected type of data to load.</typeparam>
    /// <param name="fileName">The name of the file to load from.</param>
    /// <param name="encryptionKey">The key to use for decryption. Must match the key used during saving.</param>
    /// <returns>The loaded data object, or default(T) if loading failed or file not found.</returns>
    public T Load<T>(string fileName, string encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogError("SaveSystem: File name cannot be null or empty.");
            return default(T);
        }

        string fullPath = Path.Combine(_savePath, fileName);

        // Check if the file exists before attempting to read
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"SaveSystem: File not found at {fullPath}. Returning default data.");
            return default(T);
        }

        try
        {
            // 1. Read all bytes from the file
            byte[] bytesFromFile = File.ReadAllBytes(fullPath);
            Debug.Log($"SaveSystem: Read {bytesFromFile.Length} bytes from {fullPath}.");

            // 2. Apply the chosen decryption strategy
            bytesFromFile = _encryptionStrategy.Decrypt(bytesFromFile, encryptionKey);
            if (bytesFromFile == null) // Decryption failed (e.g., wrong key or corrupt data)
            {
                Debug.LogError("SaveSystem: Decryption step failed during load. Returning default data.");
                return default(T);
            }

            // 3. Apply the chosen decompression strategy
            bytesFromFile = _compressionStrategy.Decompress(bytesFromFile);

            // 4. Convert the byte array back to a JSON string
            string jsonString = Encoding.UTF8.GetString(bytesFromFile);

            // 5. Deserialize the JSON string to the data object
            T loadedData = JsonUtility.FromJson<T>(jsonString);
            Debug.Log($"SaveSystem: Successfully loaded data from {fullPath}.");
            return loadedData;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: Failed to load data from {fullPath}. Error: {e.Message}");
            return default(T);
        }
    }

    /// <summary>
    /// Deletes a saved file from disk.
    /// </summary>
    /// <param name="fileName">The name of the file to delete.</param>
    /// <returns>True if the file was deleted or did not exist, false if an error occurred.</returns>
    public bool DeleteSaveFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogError("SaveSystem: File name cannot be null or empty for deletion.");
            return false;
        }

        string fullPath = Path.Combine(_savePath, fileName);

        if (!File.Exists(fullPath))
        {
            Debug.Log($"SaveSystem: File '{fileName}' does not exist, no deletion needed.");
            return true;
        }

        try
        {
            File.Delete(fullPath);
            Debug.Log($"SaveSystem: Successfully deleted save file: {fullPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: Failed to delete save file {fullPath}. Error: {e.Message}");
            return false;
        }
    }
}


// ====================================================================================
// SECTION 5: SaveSystemDemo (Unity MonoBehaviour for Demonstration)
//
// This MonoBehaviour class acts as a client for the SaveSystem.
// It allows you to trigger save/load/delete operations from the Unity Editor or runtime.
// ====================================================================================

/// <summary>
/// A MonoBehaviour class to demonstrate the SaveCompressionSystem in a Unity scene.
/// This acts as a client for the SaveSystem, showing how to interact with it.
/// </summary>
public class SaveSystemExample : MonoBehaviour
{
    [Header("Save System Settings")]
    [SerializeField] private string _saveFileName = "gameData.sav";
    // IMPORTANT: Hardcoding encryption keys in a MonoBehaviour is NOT secure for production.
    // This is for demonstration purposes only. In a real game, keys need to be managed securely.
    [SerializeField] private string _encryptionKey = "mySuperSecretKey123";
    [SerializeField] private bool _useCompression = true;
    [SerializeField] private bool _useEncryption = true;

    private SaveSystem _saveSystem;
    private GameData _currentLocalGameData; // Holds the game data currently in memory

    void Awake()
    {
        // Initialize the SaveSystem with chosen strategies on Awake
        InitializeSaveSystem();
        // Initialize some dummy data for the first run or if no save file exists yet
        _currentLocalGameData = new GameData("PlayerOne", 0, 0f, Vector3.zero);
    }

    /// <summary>
    /// Configures and initializes the SaveSystem based on current settings.
    /// This allows changing compression/encryption strategies at runtime via UI.
    /// </summary>
    void InitializeSaveSystem()
    {
        ICompressionStrategy compression = _useCompression ? new GZipCompressionStrategy() : new NoCompressionStrategy();
        IEncryptionStrategy encryption = _useEncryption ? new AesEncryptionStrategy() : new NoEncryptionStrategy();

        // Create the SaveSystem facade with the chosen strategies
        _saveSystem = new SaveSystem(compression, encryption, "PlayerSaves"); // Saves to Application.persistentDataPath/PlayerSaves/
        Debug.Log($"SaveSystemDemo: Initialized SaveSystem with Compression: {_useCompression} ({compression.GetType().Name}), Encryption: {_useEncryption} ({encryption.GetType().Name})");
    }

    /// <summary>
    /// Example method to save the current game data.
    /// Can be triggered by a button, game event, or editor ContextMenu.
    /// </summary>
    [ContextMenu("Save Game Data")]
    public void SaveGame()
    {
        // Update some dummy data to show changes
        _currentLocalGameData.playerScore += 100;
        _currentLocalGameData.gameProgress += 0.1f;
        _currentLocalGameData.playerPosition = new Vector3(
            UnityEngine.Random.Range(-10f, 10f),
            UnityEngine.Random.Range(-5f, 5f),
            UnityEngine.Random.Range(-10f, 10f)
        );

        Debug.Log($"<color=cyan>Attempting to save:</color> {_currentLocalGameData}");
        bool success = _saveSystem.Save(_currentLocalGameData, _saveFileName, _encryptionKey);

        if (success)
        {
            Debug.Log("<color=green>Game data saved successfully!</color>");
        }
        else
        {
            Debug.LogError("<color=red>Failed to save game data.</color>");
        }
    }

    /// <summary>
    /// Example method to load game data.
    /// Can be triggered by a button, game start, or editor ContextMenu.
    /// </summary>
    [ContextMenu("Load Game Data")]
    public void LoadGame()
    {
        Debug.Log("<color=cyan>Attempting to load game data...</color>");
        GameData loadedData = _saveSystem.Load<GameData>(_saveFileName, _encryptionKey);

        if (loadedData != null)
        {
            _currentLocalGameData = loadedData;
            Debug.Log($"<color=green>Game data loaded successfully:</color> {_currentLocalGameData}");
        }
        else
        {
            Debug.LogWarning("<color=orange>Failed to load game data or file not found. Initializing new data.</color>");
            // If loading fails, reset to a default state or handle appropriately
            _currentLocalGameData = new GameData("NewPlayer", 0, 0f, Vector3.zero);
        }
    }

    /// <summary>
    /// Example method to delete the save file.
    /// Can be triggered by a button or editor ContextMenu.
    /// </summary>
    [ContextMenu("Delete Save File")]
    public void DeleteSave()
    {
        Debug.Log("<color=cyan>Attempting to delete save file...</color>");
        if (_saveSystem.DeleteSaveFile(_saveFileName))
        {
            Debug.Log("<color=green>Save file deleted successfully (if it existed)!</color>");
            _currentLocalGameData = new GameData("PlayerOne", 0, 0f, Vector3.zero); // Reset local data after deletion
        }
        else
        {
            Debug.LogError("<color=red>Failed to delete save file.</color>");
        }
    }

    /// <summary>
    /// Provides a simple in-game GUI for demonstration purposes.
    /// </summary>
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("<b>Current Game Data:</b>", new GUIStyle { richText = true, fontSize = 18 });
        if (_currentLocalGameData != null)
        {
            GUILayout.Label($"- Player: {_currentLocalGameData.playerName}");
            GUILayout.Label($"- Score: {_currentLocalGameData.playerScore}");
            GUILayout.Label($"- Progress: {_currentLocalGameData.gameProgress:F2}");
            GUILayout.Label($"- Position: {_currentLocalGameData.playerPosition}");
        }
        else
        {
            GUILayout.Label("No data loaded.");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Save Game"))
        {
            SaveGame();
        }

        if (GUILayout.Button("Load Game"))
        {
            LoadGame();
        }

        if (GUILayout.Button("Delete Save File"))
        {
            DeleteSave();
        }

        GUILayout.Space(20);
        GUILayout.Label("<b>Save System Configuration:</b>", new GUIStyle { richText = true, fontSize = 16 });

        bool newUseCompression = GUILayout.Toggle(_useCompression, "Use GZip Compression");
        if (newUseCompression != _useCompression)
        {
            _useCompression = newUseCompression;
            InitializeSaveSystem(); // Re-initialize SaveSystem with new strategy
        }

        bool newUseEncryption = GUILayout.Toggle(_useEncryption, "Use AES Encryption");
        if (newUseEncryption != _useEncryption)
        {
            _useEncryption = newUseEncryption;
            InitializeSaveSystem(); // Re-initialize SaveSystem with new strategy
        }
        GUILayout.Label($"Encryption Key: <color=yellow>{_encryptionKey}</color>", new GUIStyle { richText = true });
        GUILayout.Label("<i>(Change settings then Save/Load to see effects)</i>", new GUIStyle { richText = true, fontSize = 10 });


        GUILayout.EndArea();
    }
}
```