// Unity Design Pattern Example: ImpactEffectSystem
// This script demonstrates the ImpactEffectSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `ImpactEffectSystem` is a powerful design pattern in Unity for handling visual and auditory feedback when something interacts with another object. It decouples the *event* (e.g., a bullet hitting a wall) from the *effects* it produces (e.g., particle effects, audio, decals, camera shake).

**Core Idea:**
1.  **Impact Data:** A struct containing all relevant information about the impact (position, normal, hit object, velocity, etc.).
2.  **Impact Effects (ScriptableObjects):** Individual effects (like playing particles, sounds, or spawning decals) are defined as ScriptableObjects. They inherit from a common base `ImpactEffectSO` and implement a `PlayEffect` method.
3.  **Impact Types (ScriptableObjects):** These define a collection of `ImpactEffectSO`s. For example, a "WoodImpactType" might contain a "WoodParticleEffectSO" and a "WoodAudioEffectSO".
4.  **Impact Effect Manager (Singleton):** A central service that receives an `ImpactTypeSO` and `ImpactData`. It then iterates through all the effects defined in that `ImpactTypeSO` and tells them to play.
5.  **Surface Type (MonoBehaviour):** Components placed on game objects (e.g., walls, floors) to define what `ImpactTypeSO` they represent when hit.
6.  **Impact Producer (MonoBehaviour):** An object that causes an impact (e.g., a projectile, a player landing). It gathers `ImpactData` and the relevant `ImpactTypeSO` (usually from the hit object's `SurfaceType`) and calls the `ImpactEffectManager` to play the effects.

This structure allows for easy expansion with new effect types, flexible configuration of impact behaviors, and reusability across different game systems.

---

To use this script:

1.  **Create a C# Script:** Create a new C# script named `ImpactSystemExample.cs` in your Unity project and copy-paste the entire content below into it.
2.  **Create Manager GameObject:** In your scene, create an empty GameObject (e.g., `_Managers`). Add the `ImpactEffectManager` component to it.
3.  **Create Effect ScriptableObjects:**
    *   Right-click in your Project window -> Create -> Impact System -> Particle Effect (create a few, e.g., "BulletSpark", "WoodSplinter")
    *   Right-click in your Project window -> Create -> Impact System -> Audio Effect (create a few, e.g., "BulletHitMetal", "BulletHitWood")
    *   Right-click in your Project window -> Create -> Impact System -> Decal Effect (create a few, e.g., "BulletHole", "ScuffMark")
    *   Configure their respective fields (Particle Prefab, Audio Clips, Decal Prefab).
        *   **Particle Prefab:** You'll need some pre-made particle systems. Unity's Standard Assets (or other free assets from the Asset Store) often contain examples.
        *   **Audio Clips:** Import some `.wav` or `.mp3` files.
        *   **Decal Prefab:** A simple quad mesh with a transparent material (using a shader like "Unlit/Transparent" or "Standard" with rendering mode set to "Fade" or "Transparent") and a texture (e.g., a bullet hole alpha texture).
4.  **Create Impact Type ScriptableObjects:**
    *   Right-click in your Project window -> Create -> Impact System -> Impact Type (create a few, e.g., "MetalImpact", "WoodImpact", "DefaultImpact").
    *   Select an `ImpactType` asset (e.g., "MetalImpact"). In the Inspector, drag and drop the relevant `ParticleImpactEffectSO`, `AudioImpactEffectSO`, and `DecalImpactEffectSO` assets into its `Effects` list.
5.  **Assign Default Impact Type:** In the `ImpactEffectManager` component in your scene, assign a `DefaultImpactType` (e.g., your "DefaultImpact" SO). This will be used if a hit object doesn't specify a `SurfaceType`.
6.  **Setup Interactable Objects:**
    *   Create some 3D objects in your scene (e.g., a Cube, a Sphere). Add a `Box Collider` or `Sphere Collider` to them.
    *   Add the `SurfaceType` component to these objects.
    *   Assign the appropriate `ImpactTypeSO` to their `ImpactType` field (e.g., "MetalImpact" to a metallic-looking cube, "WoodImpact" to a wooden-looking sphere).
7.  **Setup Impact Producer:**
    *   Create an empty GameObject (e.g., `PlayerCamera`) and position it where your camera would be. Add a `Camera` component to it.
    *   Add the `ExampleImpactProducer` component to this `PlayerCamera` GameObject.
    *   Set its `ImpactLayerMask` to include the layers of your interactable objects.
    *   Assign a `DefaultProjectileImpactType` (e.g., "BulletImpact").
8.  **Run the Scene:** Click play. When you left-click, a raycast will be fired from the mouse position. If it hits an object with a `SurfaceType`, the corresponding effects will play. If it hits something without a `SurfaceType`, the `DefaultProjectileImpactType` will be used. Watch the Console for debug messages and observe the particle, audio, and decal effects!

---

```csharp
using UnityEngine;
using System.Collections.Generic;

// ====================================================================================================
// SECTION 1: Impact Data Structure
// This struct holds all the relevant information about a specific impact event.
// It's passed around to all the impact effects, allowing them to customize their behavior.
// ====================================================================================================

/// <summary>
/// Represents the data associated with a specific impact event.
/// This struct allows different impact effects to react based on the details of the impact.
/// </summary>
public struct ImpactData
{
    /// <summary> The world position where the impact occurred. </summary>
    public Vector3 Position;
    /// <summary> The normal vector of the surface at the impact point. Useful for orienting particles/decals. </summary>
    public Vector3 Normal;
    /// <summary> The GameObject that was hit. </summary>
    public GameObject HitObject;
    /// <summary> The velocity of the object that caused the impact. Can be used for scaling effects. </summary>
    public Vector3 HitVelocity;
    /// <summary> A general strength or magnitude of the impact. Can be used for scaling effects (e.g., damage, sound volume). </summary>
    public float ImpactStrength;

    // You can add more fields as needed, e.g.:
    // public Collider HitCollider;
    // public Rigidbody HitRigidbody;
    // public Material HitMaterial;
    // public float DamageAmount;
    // public GameObject Instigator; // The object that caused the impact (e.g., the bullet)
}

// ====================================================================================================
// SECTION 2: Base Impact Effect ScriptableObject
// This abstract class serves as the base for all specific impact effects (particles, audio, decals).
// Using ScriptableObjects allows us to define effect configurations as assets in the project.
// ====================================================================================================

/// <summary>
/// Abstract base class for all impact effects.
/// Concrete effects will inherit from this and implement the PlayEffect method.
/// These are ScriptableObjects, allowing them to be created as assets and configured in the Inspector.
/// </summary>
public abstract class ImpactEffectSO : ScriptableObject
{
    /// <summary>
    /// This method is called by the ImpactEffectManager to play this specific effect.
    /// Concrete implementations will define what happens when this effect is played.
    /// </summary>
    /// <param name="data">The ImpactData containing information about the impact event.</param>
    public abstract void PlayEffect(ImpactData data);
}

// ====================================================================================================
// SECTION 3: Concrete Impact Effect Implementations (ScriptableObjects)
// These classes define specific types of effects that can be played.
// You can create more of these (e.g., CameraShakeImpactEffectSO, ForceFeedbackImpactEffectSO).
// ====================================================================================================

/// <summary>
/// An ImpactEffectSO that plays a particle system at the impact point.
/// </summary>
[CreateAssetMenu(fileName = "NewParticleImpactEffect", menuName = "Impact System/Effects/Particle Effect")]
public class ParticleImpactEffectSO : ImpactEffectSO
{
    [Tooltip("The particle system prefab to instantiate.")]
    public GameObject ParticlePrefab;
    [Tooltip("How long the instantiated particle system will live before being destroyed. " +
             "In a real game, consider using object pooling instead of destroying.")]
    public float Lifetime = 5f;
    [Tooltip("If true, the particle system will be parented to the hit object. " +
             "Useful for effects that should stick to moving objects.")]
    public bool ParentToHitObject = false;

    public override void PlayEffect(ImpactData data)
    {
        if (ParticlePrefab == null)
        {
            Debug.LogWarning($"ParticleImpactEffectSO '{name}' has no ParticlePrefab assigned.", this);
            return;
        }

        // Instantiate the particle system at the impact position, rotated to face away from the surface.
        GameObject particles = Instantiate(ParticlePrefab, data.Position, Quaternion.LookRotation(data.Normal));

        if (ParentToHitObject && data.HitObject != null)
        {
            particles.transform.SetParent(data.HitObject.transform);
        }

        // Destroy the particles after their lifetime. For performance in real games, use object pooling.
        Destroy(particles, Lifetime);

        Debug.Log($"Playing Particle Effect '{ParticlePrefab.name}' at {data.Position} on {data.HitObject.name}", particles);
    }
}

/// <summary>
/// An ImpactEffectSO that plays an audio clip at the impact point.
/// </summary>
[CreateAssetMenu(fileName = "NewAudioImpactEffect", menuName = "Impact System/Effects/Audio Effect")]
public class AudioImpactEffectSO : ImpactEffectSO
{
    [Tooltip("An array of audio clips to choose from. One will be randomly selected.")]
    public AudioClip[] AudioClips;
    [Tooltip("Volume of the audio clip.")]
    [Range(0, 1)] public float Volume = 1f;
    [Tooltip("Random pitch variation to make sounds less repetitive.")]
    [Range(0, 0.5f)] public float PitchRandomness = 0.1f;
    [Tooltip("Maximum distance for the audio source to be heard.")]
    public float MaxDistance = 50f;

    public override void PlayEffect(ImpactData data)
    {
        if (AudioClips == null || AudioClips.Length == 0)
        {
            Debug.LogWarning($"AudioImpactEffectSO '{name}' has no AudioClips assigned.", this);
            return;
        }

        AudioClip clipToPlay = AudioClips[Random.Range(0, AudioClips.Length)];
        if (clipToPlay == null)
        {
            Debug.LogWarning($"AudioImpactEffectSO '{name}' has a null AudioClip in its array.", this);
            return;
        }

        // Create a temporary GameObject for the AudioSource
        // In a real game, consider pooling AudioSources.
        GameObject audioGO = new GameObject($"TempAudio_{clipToPlay.name}");
        audioGO.transform.position = data.Position;
        AudioSource source = audioGO.AddComponent<AudioSource>();

        source.clip = clipToPlay;
        source.volume = Volume;
        source.pitch = Random.Range(1f - PitchRandomness, 1f + PitchRandomness);
        source.spatialBlend = 1f; // Make it a 3D sound
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.maxDistance = MaxDistance;
        source.Play();

        // Destroy the temporary GameObject after the clip finishes playing
        Destroy(audioGO, clipToPlay.length + 0.1f); // Add a small buffer

        Debug.Log($"Playing Audio Effect '{clipToPlay.name}' at {data.Position} on {data.HitObject.name}", audioGO);
    }
}

/// <summary>
/// An ImpactEffectSO that spawns a decal (e.g., a bullet hole, scorch mark) at the impact point.
/// The decal prefab should be a simple quad with a transparent material and an appropriate texture.
/// </summary>
[CreateAssetMenu(fileName = "NewDecalImpactEffect", menuName = "Impact System/Effects/Decal Effect")]
public class DecalImpactEffectSO : ImpactEffectSO
{
    [Tooltip("The decal prefab to instantiate. Should be a quad with a transparent material.")]
    public GameObject DecalPrefab;
    [Tooltip("The size multiplier for the decal.")]
    public float DecalSize = 0.5f;
    [Tooltip("How long the decal will persist before being destroyed. " +
             "Consider a DecalManager for proper decal pooling/management in a real game.")]
    public float Lifetime = 10f;
    [Tooltip("Offset the decal slightly along the normal to prevent Z-fighting with the surface.")]
    public float NormalOffset = 0.01f;
    [Tooltip("If true, the decal will be parented to the hit object. " +
             "Useful for decals that should stick to moving objects.")]
    public bool ParentToHitObject = true;

    public override void PlayEffect(ImpactData data)
    {
        if (DecalPrefab == null)
        {
            Debug.LogWarning($"DecalImpactEffectSO '{name}' has no DecalPrefab assigned.", this);
            return;
        }

        // Instantiate the decal at the impact position, rotated to align with the surface normal.
        GameObject decal = Instantiate(DecalPrefab, data.Position + data.Normal * NormalOffset, Quaternion.LookRotation(data.Normal));
        decal.transform.localScale = Vector3.one * DecalSize;

        if (ParentToHitObject && data.HitObject != null)
        {
            decal.transform.SetParent(data.HitObject.transform);
        }

        // Destroy the decal after its lifetime. For complex decal systems, a dedicated DecalManager is better.
        Destroy(decal, Lifetime);

        Debug.Log($"Playing Decal Effect '{DecalPrefab.name}' at {data.Position} on {data.HitObject.name}", decal);
    }
}


// ====================================================================================================
// SECTION 4: Impact Type ScriptableObject
// This acts as a container, grouping multiple ImpactEffectSOs together under a single "ImpactType".
// For example, "WoodImpactType" would contain particle, audio, and decal effects specific to wood.
// ====================================================================================================

/// <summary>
/// Defines a collection of ImpactEffectSOs for a specific type of impact (e.g., "Metal", "Wood", "Flesh").
/// These are ScriptableObjects and serve as data assets configured in the project.
/// </summary>
[CreateAssetMenu(fileName = "NewImpactType", menuName = "Impact System/Impact Type")]
public class ImpactTypeSO : ScriptableObject
{
    [Tooltip("A descriptive name for this impact type (e.g., 'Wood', 'Metal', 'Flesh').")]
    public string ImpactTypeName = "Default";
    [Tooltip("A list of individual effects to be played when this impact type is triggered.")]
    public List<ImpactEffectSO> Effects = new List<ImpactEffectSO>();
}

// ====================================================================================================
// SECTION 5: Impact Effect Manager (Singleton)
// The central hub of the system. It's a MonoBehaviour that resides in the scene,
// provides a static access point, and orchestrates the playing of effects.
// ====================================================================================================

/// <summary>
/// The central manager for playing impact effects.
/// It is a singleton, allowing easy access from anywhere in the game.
/// It receives an ImpactTypeSO and ImpactData, then triggers all associated effects.
/// </summary>
public class ImpactEffectManager : MonoBehaviour
{
    public static ImpactEffectManager Instance { get; private set; }

    [Tooltip("A fallback impact type to use if no specific ImpactTypeSO is provided during an impact.")]
    public ImpactTypeSO DefaultImpactType;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ImpactEffectManager instances found. Destroying duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make the manager persistent across scene loads
            // DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Plays all effects associated with the given impactType and data.
    /// If impactType is null, it falls back to the DefaultImpactType.
    /// </summary>
    /// <param name="impactType">The ImpactTypeSO defining which effects to play.</param>
    /// <param name="data">The ImpactData struct containing details about the impact event.</param>
    public void PlayImpact(ImpactTypeSO impactType, ImpactData data)
    {
        // Use the provided impactType, or fall back to the default if it's null
        ImpactTypeSO activeImpactType = impactType ?? DefaultImpactType;

        if (activeImpactType == null)
        {
            Debug.LogWarning("No ImpactType provided and no DefaultImpactType set for ImpactEffectManager. Cannot play impact effects.", this);
            return;
        }

        Debug.Log($"<color=cyan>ImpactEffectManager: Playing impact for type: '{activeImpactType.ImpactTypeName}' at {data.Position} on {data.HitObject.name}</color>", data.HitObject);

        // Iterate through all effects defined in the active ImpactTypeSO and play them
        foreach (ImpactEffectSO effect in activeImpactType.Effects)
        {
            if (effect != null)
            {
                effect.PlayEffect(data);
            }
            else
            {
                Debug.LogWarning($"ImpactEffectManager: An effect in '{activeImpactType.ImpactTypeName}' is null and will be skipped.", activeImpactType);
            }
        }
    }
}

// ====================================================================================================
// SECTION 6: Surface Type Component (MonoBehaviour)
// This component is attached to game objects in the scene to define their surface material.
// When an impact occurs on this object, this component determines the ImpactTypeSO to use.
// ====================================================================================================

/// <summary>
/// Attachable to GameObjects to define their surface impact type.
/// When an object with this component is hit, the ImpactProducer can query its ImpactTypeSO.
/// </summary>
public class SurfaceType : MonoBehaviour
{
    [Tooltip("The ImpactTypeSO associated with this surface (e.g., 'WoodImpact', 'MetalImpact').")]
    public ImpactTypeSO ImpactType;
}

// ====================================================================================================
// SECTION 7: Example Impact Producer (MonoBehaviour)
// This simulates an object that causes impacts (e.g., a bullet, a player's footstep).
// It's responsible for detecting an impact, gathering ImpactData, and calling the Manager.
// ====================================================================================================

/// <summary>
/// An example component that simulates an entity producing impacts (e.g., a bullet, a player).
/// It performs a raycast to detect hits and then uses the ImpactEffectManager to play effects.
/// Attach this to your player camera or a testing GameObject.
/// </summary>
public class ExampleImpactProducer : MonoBehaviour
{
    [Tooltip("The maximum distance for the impact raycast.")]
    public float ImpactRaycastDistance = 100f;
    [Tooltip("The layers that the impact raycast will interact with.")]
    public LayerMask ImpactLayerMask;
    [Tooltip("The ImpactTypeSO to use if the hit object does not have a SurfaceType component.")]
    public ImpactTypeSO DefaultProjectileImpactType;
    [Tooltip("Visual debug sphere size for impact point.")]
    public float DebugSphereRadius = 0.1f;
    [Tooltip("Duration for which the debug sphere remains visible.")]
    public float DebugSphereDuration = 1f;

    void Update()
    {
        // Simulate an impact event when the left mouse button is pressed down.
        if (Input.GetMouseButtonDown(0))
        {
            SimulateImpact();
        }
    }

    private void SimulateImpact()
    {
        // Cast a ray from the camera's view through the mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Perform the raycast
        if (Physics.Raycast(ray, out hit, ImpactRaycastDistance, ImpactLayerMask))
        {
            // --- Step 1: Determine the ImpactType for this collision ---
            // Try to get the ImpactType from the hit object's SurfaceType component.
            SurfaceType surface = hit.collider.GetComponent<SurfaceType>();
            ImpactTypeSO impactType = null;

            if (surface != null && surface.ImpactType != null)
            {
                impactType = surface.ImpactType;
                Debug.Log($"<color=green>Hit object '{hit.collider.name}' has SurfaceType: {impactType.ImpactTypeName}</color>");
            }
            else
            {
                // If no SurfaceType, use the producer's default impact type
                impactType = DefaultProjectileImpactType;
                Debug.Log($"<color=orange>Hit object '{hit.collider.name}' has no SurfaceType. Using DefaultProjectileImpactType: {DefaultProjectileImpactType?.ImpactTypeName}</color>");
            }

            // --- Step 2: Create the ImpactData struct ---
            ImpactData data = new ImpactData
            {
                Position = hit.point,
                Normal = hit.normal,
                HitObject = hit.collider.gameObject,
                HitVelocity = ray.direction * 50f, // Example: Assume projectile travels at 50 m/s in ray direction
                ImpactStrength = 1.0f // Example: Default strength, could be velocity-based
            };

            // --- Step 3: Trigger the ImpactEffectManager ---
            if (ImpactEffectManager.Instance != null)
            {
                ImpactEffectManager.Instance.PlayImpact(impactType, data);
            }
            else
            {
                Debug.LogError("ImpactEffectManager.Instance is null. Make sure an ImpactEffectManager GameObject is in your scene!");
            }

            // --- Optional: Visual Debugging ---
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, DebugSphereDuration);
            GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphere.transform.position = hit.point;
            debugSphere.transform.localScale = Vector3.one * DebugSphereRadius;
            debugSphere.GetComponent<Renderer>().material.color = Color.red;
            Destroy(debugSphere.GetComponent<Collider>()); // Remove collider to not interfere
            Destroy(debugSphere, DebugSphereDuration);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * ImpactRaycastDistance, Color.blue, DebugSphereDuration);
            Debug.Log("Raycast hit nothing.");
        }
    }
}
```