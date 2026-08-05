using UnityEngine;

/// <summary>
/// Controls the vehicle's audio by updating engine and skid sounds
/// based on the current driving state of the BMWCarController.
/// </summary>
[RequireComponent(typeof(BMWCarController))]
public class BMWCarAudio : MonoBehaviour
{
    [Header("Audio Sources")]

    // Audio source responsible for engine sounds.
    [SerializeField] private AudioSource engineSource;

    // Audio source responsible for tire skid sounds.
    [SerializeField] private AudioSource skidSource;

    // Audio source reserved for gear shift sounds.
    [SerializeField] private AudioSource gearSource;

    [Header("Engine Sounds")]

    // Engine idle audio clip.
    [SerializeField] private AudioClip engineIdleClip;

    // Optional high RPM engine audio clip.
    [SerializeField] private AudioClip engineHighClip;   // Optional - you can leave empty

    [Header("Skid Sound")]

    // Tire skidding audio clip.
    [SerializeField] private AudioClip tireSkidClip;

    [Header("Settings")]

    // Minimum engine pitch.
    [Range(0.6f, 3f)] public float minPitch = 0.85f;

    // Maximum engine pitch.
    [Range(0.6f, 3f)] public float maxPitch = 2.4f;

    // Controls how quickly the engine pitch changes.
    [SerializeField] private float pitchSmooth = 7f;

    // Reference to the vehicle controller.
    private BMWCarController carController;

    // Current interpolated engine pitch.
    private float currentPitch = 1f;

    /// <summary>
    /// Initializes component references and configures
    /// the required audio sources.
    /// </summary>
    private void Awake()
    {
        // Cache the vehicle controller.
        carController = GetComponent<BMWCarController>();

        // Configure all audio sources.
        SetupAudioSources();
    }

    /// <summary>
    /// Automatically locates and configures the audio sources
    /// used by the vehicle.
    /// </summary>
    private void SetupAudioSources()
    {
        // Auto find if not assigned
        if (engineSource == null) engineSource = transform.Find("Engine Audio")?.GetComponent<AudioSource>();
        if (skidSource == null) skidSource = transform.Find("Skid Audio")?.GetComponent<AudioSource>();
        if (gearSource == null) gearSource = transform.Find("Gear Shift Audio")?.GetComponent<AudioSource>();

        // === ENGINE SOUND SETUP ===
        if (engineSource != null)
        {
            engineSource.loop = true;
            engineSource.playOnAwake = false;
            engineSource.spatialBlend = 1f;
            engineSource.minDistance = 3f;
            engineSource.maxDistance = 50f;
            engineSource.volume = 0.85f;

            // Assign the idle engine clip.
            if (engineIdleClip != null)
                engineSource.clip = engineIdleClip;

            // Begin playing the engine sound.
            engineSource.Play();
        }

        // === SKID SOUND SETUP ===
        if (skidSource != null)
        {
            skidSource.loop = true;
            skidSource.playOnAwake = false;
            skidSource.spatialBlend = 1f;
            skidSource.volume = 0f;
        }
    }

    /// <summary>
    /// Updates the vehicle audio every frame.
    /// </summary>
    private void Update()
    {
        // Exit if the controller reference is missing.
        if (carController == null) return;

        // Update engine audio.
        UpdateEngineSound();

        // Update tire skid audio.
        UpdateSkidSound();
    }

    /// <summary>
    /// Adjusts engine pitch and volume according
    /// to the current engine RPM and throttle input.
    /// </summary>
    private void UpdateEngineSound()
    {
        // Ensure the required references exist.
        if (engineSource == null || engineIdleClip == null) return;

        // Calculate the desired engine pitch.
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, carController.Revs);

        // Boost pitch when accelerating hard.
        if (carController.AccelInput > 0.7f)
            targetPitch += 0.15f;

        // Smoothly interpolate the engine pitch.
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * pitchSmooth);
        engineSource.pitch = currentPitch;

        // Increase engine volume as RPM rises.
        engineSource.volume = Mathf.Lerp(0.6f, 1.1f, carController.Revs);
    }

    /// <summary>
    /// Plays and controls the tire skid sound
    /// based on braking, skidding, and vehicle speed.
    /// </summary>
    private void UpdateSkidSound()
    {
        // Ensure the required references exist.
        if (skidSource == null || tireSkidClip == null) return;

        // Determine whether the tires should currently be skidding.
        bool shouldSkid = carController.Skidding ||
                         (carController.BrakeInput > 0.6f && carController.CurrentSpeed > 8f);

        if (shouldSkid)
        {
            // Start the skid sound if it is not already playing.
            if (!skidSource.isPlaying)
            {
                skidSource.clip = tireSkidClip;
                skidSource.Play();
            }

            // Increase skid volume based on vehicle speed.
            float targetVolume = Mathf.Clamp01(carController.CurrentSpeed / carController.MaxSpeed * 1.1f);
            skidSource.volume = Mathf.Lerp(skidSource.volume, targetVolume, Time.deltaTime * 8f);
        }
        else
        {
            // Smoothly fade out the skid sound.
            skidSource.volume = Mathf.Lerp(skidSource.volume, 0f, Time.deltaTime * 6f);

            // Stop the audio once it becomes nearly inaudible.
            if (skidSource.volume < 0.05f && skidSource.isPlaying)
                skidSource.Stop();
        }
    }
}