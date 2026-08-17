using System;
using UnityEngine;

/// <summary>
/// Defines which wheels receive motor torque.
/// </summary>
internal enum DriveMode
{
    FrontWheelDrive,
    RearWheelDrive,
    FourWheelDrive
}

/// <summary>
/// Unit system used for displaying/capping vehicle speed.
/// </summary>
internal enum SpeedType
{
    MPH,
    KPH
}

/// <summary>
/// Core drivetrain and physics controller for the BMW car. Handles player
/// input (or delegates to CarAIControl via Move()), wheel collider physics
/// (steering, motor torque, braking, handbrake), gear/rev simulation,
/// traction control, downforce, speed capping, and wheel VFX (smoke, skid marks).
/// </summary>
public class BMWCarController : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector Configuration
    // ---------------------------------------------------------------

    [Tooltip("Which wheels receive motor torque (FWD/RWD/AWD).")]
    [SerializeField] private DriveMode carDriveType = DriveMode.FourWheelDrive;

    [Tooltip("Wheel colliders in order: [0]=Front-Left, [1]=Front-Right, [2]=Rear-Left, [3]=Rear-Right.")]
    [SerializeField] private WheelCollider[] wheelColliders = new WheelCollider[4];

    [Tooltip("ScriptableObject holding this car's stat data (mass, top speed, etc.), applied via SetCarData.")]
    [SerializeField] private CarDataSO m_CarData;

    [Tooltip("Particle systems for wheel smoke/dust, one per wheel.")]
    [SerializeField] private ParticleSystem[] wheelEffects;

    [Tooltip("Trail renderers used to draw skid marks, one per wheel.")]
    [SerializeField] private TrailRenderer[] skidMarks;

    // Tracks whether the smoke VFX is currently active, to avoid redundant Play()/Stop() calls.
    private bool smokeEffectEnabled;

    [Tooltip("Visual wheel mesh GameObjects, kept in sync with the physics wheel colliders each frame.")]
    [SerializeField] private GameObject[] wheelMeshes = new GameObject[4];

    // NOTE: [HideInInspector] has no effect on a private field (it only affects Inspector visibility
    // for fields that would otherwise be shown), and this field is never assigned elsewhere,
    // so the rigidbody's center of mass offset always ends up as Vector3.zero in Start().
    [HideInInspector] private Vector3 m_CentreOfMassOffset;//this shouldn't be touched 

    [Tooltip("Maximum steering angle (degrees) applied to the front wheels.")]
    [SerializeField] private float maximumSteerAngle;

    [Range(0, 1)]
    [Tooltip("0 = raw physics, 1 = rigidbody velocity is fully realigned to face the car's forward direction.")]
    [SerializeField] private float steeringAssists; // 0 is raw physics , 1 the car will grip in the direction it is facing

    [Range(0, 1)]
    [Tooltip("0 = no traction control intervention, 1 = full interference reducing wheel spin.")]
    [SerializeField] private float tractionControlSystem; // 0 is no traction control, 1 is full interference

    [Tooltip("Total torque available across all driven wheels before traction control reduction.")]
    [SerializeField] private float fullTorqueOverAllWheels;

    [Tooltip("Torque applied to driven wheels when reversing.")]
    [SerializeField] private float reverseTorque;

    [Tooltip("Maximum brake torque applied to the rear wheels when the handbrake is engaged.")]
    [SerializeField] private float maximumHandbrakeTorque;

    [Tooltip("Downward force applied based on speed, to improve high-speed grip/stability.")]
    [SerializeField] private float downForce = 100f;

    [Tooltip("Unit system (MPH/KPH) used when capping the car's speed against topSpeed.")]
    [SerializeField] private SpeedType speedType;

    [Tooltip("Car's maximum speed. Shared/static so all instances read the same top speed value; overwritten per-car by SetCarData.")]
    [SerializeField] public static float topSpeed = 80f;

    [Tooltip("Number of simulated gears used to drive the rev/gear-factor simulation.")]
    [SerializeField] private int noOfGears = 5;

    [Tooltip("Normalized rev-range boundary used when blending rev curve between gears.")]
    [SerializeField] private float revRangeBoundary = 1f;

    [Tooltip("Forward wheel slip threshold above which traction control starts reducing torque.")]
    [SerializeField] private float slipLimit;

    [Tooltip("Base brake torque applied to all wheels under normal (non-handbrake) braking.")]
    [SerializeField] private float brakeTorque;

    [Tooltip("Forward (rolling direction) tire friction stiffness, applied to all wheel colliders at Start.")]
    [SerializeField] private float forwardGrip = 1.5f;

    [Tooltip("Sideways (lateral) tire friction stiffness, applied to all wheel colliders at Start.")]
    [SerializeField] private float sidewaysGrip = 2.0f;

    // ---------------------------------------------------------------
    // Internal State
    // ---------------------------------------------------------------

    // Cached original local rotations of each wheel mesh (currently computed but unused elsewhere).
    private Quaternion[] wheelMeshLocalRotations;

    // Current steering angle applied to the front wheels this frame.
    private float steerAngle;

    // Current simulated gear index (0-based).
    private int gearNum;

    // Blended factor (0-1) representing position within the current gear's rev range, used for rev simulation smoothing.
    private float gearFactor;

    // Previous frame's Y rotation, used by SteeringAssists to detect turn rate.
    private float oldRotation;

    // Current available motor torque, dynamically reduced/restored by the traction control system.
    private float currentTorque;

    // Cached Rigidbody reference for this car.
    private Rigidbody m_Rigidbody;

    // ---------------------------------------------------------------
    // Public Properties (read-only outward-facing state, e.g. for UI/AI)
    // ---------------------------------------------------------------

    /// <summary>True while the handbrake is engaged and rear wheels are locked/skidding.</summary>
    public bool Skidding { get; private set; }

    /// <summary>Normalized (0-1) footbrake input from the last drive pipeline update.</summary>
    public float BrakeInput { get; private set; }

    /// <summary>Current front-wheel steering angle in degrees.</summary>
    public float CurrentSteerAngle => steerAngle;

    /// <summary>Current speed of the car in MPH, derived from rigidbody velocity.</summary>
    public float CurrentSpeed => m_Rigidbody.linearVelocity.magnitude * 2.23693629f;

    /// <summary>This car's configured top speed.</summary>
    public float MaxSpeed => topSpeed;

    /// <summary>Simulated engine revs (0-1), used for audio pitch/UI tachometer.</summary>
    public float Revs { get; private set; }

    /// <summary>Normalized (0-1) accelerator input from the last drive pipeline update.</summary>
    public float AccelInput { get; private set; }

    // ---------------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------------

    /// <summary>
    /// Initializes wheel mesh rotation cache, rigidbody center of mass,
    /// starting torque budget, handbrake torque, and applies configured
    /// tire friction stiffness to all wheel colliders.
    /// </summary>
    private void Start()
    {
        wheelMeshLocalRotations = new Quaternion[4];
        for (int i = 0; i < 4; i++)
        {
            wheelMeshLocalRotations[i] = wheelMeshes[i].transform.localRotation;
        }
        wheelColliders[0].attachedRigidbody.centerOfMass = m_CentreOfMassOffset;

        // Handbrake always applies maximum possible lock force (full wheel lock).
        maximumHandbrakeTorque = float.MaxValue;

        m_Rigidbody = GetComponent<Rigidbody>();

        // Starting torque budget is reduced by the traction control setting.
        currentTorque = fullTorqueOverAllWheels - (tractionControlSystem * fullTorqueOverAllWheels);

        // Apply configured forward/sideways friction stiffness to every wheel.
        foreach (WheelCollider wheel in wheelColliders)
        {
            WheelFrictionCurve forward = wheel.forwardFriction;
            forward.stiffness = forwardGrip;
            wheel.forwardFriction = forward;

            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.stiffness = sidewaysGrip;
            wheel.sidewaysFriction = sideways;
        }
    }

    /// <summary>
    /// Applies stat data from a CarDataSO (e.g. selected car in a garage/select screen)
    /// to this controller's mass and top speed.
    /// </summary>
    public void SetCarData(CarDataSO data)
    {
        if (data == null) return;

        m_Rigidbody.mass = data.mass;
        topSpeed = data.topSpeedSuperSpeedway;
    }

    /// <summary>
    /// Reads player keyboard input each physics step, unless an AI controller
    /// is present on this GameObject (in which case AI drives via Move() instead).
    /// </summary>
    private void FixedUpdate()
    {
        if (GetComponent<CarAIControl>() != null) return;
        ReadInputs();
    }

    // ---------------------------------------------------------------
    // Public Drive Entry Points
    // ---------------------------------------------------------------

    // Move: public entry point for CarAIControl
    // steering: -1 to 1, accel: 0 to 1, footbrake: -1 to 0, handbrake: 0 to 1
    /// <summary>
    /// External drive entry point used by AI controllers to drive this car
    /// without going through keyboard input.
    /// </summary>
    public void Move(float steering, float accel, float footbrake, float handbrake)
    {
        RunDrivePipeline(steering, accel, footbrake, handbrake);
    }

    /// <summary>
    /// Reads raw keyboard axes/keys and forwards them into the shared drive pipeline.
    /// </summary>
    private void ReadInputs()
    {
        float steering = Input.GetAxis("Horizontal");
        float accelRaw = Input.GetAxis("Vertical");
        float handbrake = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        // Split the single vertical axis into separate acceleration (positive) and footbrake (negative) values.
        float acceleration = Mathf.Clamp(accelRaw, 0f, 1f);
        float footbrake = Mathf.Clamp(accelRaw, -1f, 0f);

        RunDrivePipeline(steering, acceleration, footbrake, handbrake);
    }

    // ---------------------------------------------------------------
    // Core Drive Pipeline
    // ---------------------------------------------------------------

    //Shared pipeline used by both keyboard (CheckInput) and AI (Move)
    /// <summary>
    /// Central per-frame driving pipeline shared by both player and AI input paths:
    /// syncs wheel visuals, clamps/applies inputs (steer, accel, brake, handbrake),
    /// updates skid/smoke VFX, and drives the rev/gear/downforce/traction subsystems.
    /// </summary>
    private void RunDrivePipeline(float steering, float _acceleration, float footbrake, float handbrake)
    {
        // Update wheel mesh transforms to match collider poses
        for (int i = 0; i < 4; i++)
        {
            Quaternion quat;
            Vector3 position;
            wheelColliders[i].GetWorldPose(out position, out quat);
            wheelMeshes[i].transform.position = position;
            wheelMeshes[i].transform.rotation = quat;
        }

        // Clamp input values
        steering = Mathf.Clamp(steering, -1f, 1f);
        AccelInput = _acceleration = Mathf.Clamp(_acceleration, 0f, 1f);
        BrakeInput = footbrake = -1f * Mathf.Clamp(footbrake, -1f, 0f);
        handbrake = Mathf.Clamp(handbrake, 0f, 1f);

        // Set the steer on the front wheels.
        // Assuming that wheels 0 and 1 are the front wheels.
        steerAngle = steering * maximumSteerAngle;
        wheelColliders[0].steerAngle = steerAngle;
        wheelColliders[1].steerAngle = steerAngle;

        SteeringAssists();
        Drive(_acceleration, footbrake);
        CapSpeed();

        // Handbrake: locks rear wheels and triggers skid/smoke VFX.
        if (handbrake > 0f)
        {
            var hbTorque = handbrake * maximumHandbrakeTorque;
            wheelColliders[2].brakeTorque = hbTorque;
            wheelColliders[3].brakeTorque = hbTorque;
            EnableSkidMarks(true);
            if (!smokeEffectEnabled)
            {
                EnableSmokeEffect(true);
                smokeEffectEnabled = true;
            }
        }
        else
        {
            // Only clear if Drive's footbrake logic isn't already
            // braking these wheels this frame (footbrake handles its own brakeTorque)
            if (footbrake <= 0f)
            {
                wheelColliders[2].brakeTorque = 0f;
                wheelColliders[3].brakeTorque = 0f;
                EnableSkidMarks(false);
                if (smokeEffectEnabled)
                {
                    EnableSmokeEffect(false);
                    smokeEffectEnabled = false;
                }
            }
        }
        CalculateRevolutions();
        GearChanging();
        Downforce();
        TractionControlSystem();
    }

    // ---------------------------------------------------------------
    // Gear / Rev Simulation
    // ---------------------------------------------------------------

    /// <summary>
    /// Shifts the simulated gear up or down based on current speed relative
    /// to the current gear's speed band.
    /// </summary>
    private void GearChanging()
    {
        float f = Mathf.Abs(CurrentSpeed / MaxSpeed);
        float upgearlimit = (1 / (float)noOfGears) * (gearNum + 1);
        float downgearlimit = (1 / (float)noOfGears) * gearNum;

        if (gearNum > 0 && f < downgearlimit)
        {
            gearNum--;
        }

        if (f > upgearlimit && (gearNum < (noOfGears - 1)))
        {
            gearNum++;
        }
    }

    // Eases a linear 0-1 factor into a decelerating curve (used to shape rev-range boundaries).
    private static float CurveFactor(float factor) => 1 - (1 - factor) * (1 - factor);

    // Simple linear interpolation helper (functionally identical to Mathf.Lerp, unclamped).
    private static float ULerp(float from, float to, float value) => (1.0f - value) * from + value * to;

    /// <summary>
    /// Smoothly blends gearFactor toward the target position within the
    /// current gear's speed band, used to drive rev needle/audio pitch.
    /// </summary>
    private void CalculateGearFactor()
    {
        float f = (1 / (float)noOfGears);
        var targetGearFactor = Mathf.InverseLerp(f * gearNum, f * (gearNum + 1), Mathf.Abs(CurrentSpeed / MaxSpeed));
        gearFactor = Mathf.Lerp(gearFactor, targetGearFactor, Time.deltaTime * 5f);
    }

    /// <summary>
    /// Computes the simulated engine revs (0-1) based on the current gear
    /// and blended gear factor, using eased rev-range boundaries.
    /// </summary>
    private void CalculateRevolutions()
    {
        CalculateGearFactor();
        var gearNumFactor = gearNum / (float)noOfGears;
        var revsRangeMin = ULerp(0f, revRangeBoundary, CurveFactor(gearNumFactor));
        var revsRangeMax = ULerp(revRangeBoundary, 1f, gearNumFactor);
        Revs = ULerp(revsRangeMin, revsRangeMax, gearFactor);
    }

    // ---------------------------------------------------------------
    // Speed Limiting
    // ---------------------------------------------------------------

    /// <summary>
    /// Clamps the rigidbody's velocity so the car cannot exceed topSpeed,
    /// converting to the configured display unit (MPH/KPH) for the comparison.
    /// </summary>
    private void CapSpeed()
    {
        float speed = m_Rigidbody.linearVelocity.magnitude;
        switch (speedType)
        {
            case SpeedType.MPH:
                speed *= 2.23693629f;
                if (speed > topSpeed)
                    m_Rigidbody.linearVelocity = (topSpeed / 2.23693629f) * m_Rigidbody.linearVelocity.normalized;
                break;

            case SpeedType.KPH:
                speed *= 3.6f;
                if (speed > topSpeed)
                    m_Rigidbody.linearVelocity = (topSpeed / 3.6f) * m_Rigidbody.linearVelocity.normalized;
                break;
        }
    }

    // ---------------------------------------------------------------
    // Torque / Braking
    // ---------------------------------------------------------------

    /// <summary>
    /// Applies motor torque to the appropriate wheels based on drive type,
    /// and handles braking/reverse behavior across all wheels.
    /// </summary>
    private void Drive(float accel, float footbrake)
    {
        float thrustTorque;

        // Determine which wheel indices are actually driven this configuration
        int[] drivenWheels;
        switch (carDriveType)
        {
            case DriveMode.FrontWheelDrive:
                drivenWheels = new[] { 0, 1 };
                thrustTorque = accel * (currentTorque / 2f);
                wheelColliders[0].motorTorque = wheelColliders[1].motorTorque = thrustTorque;
                wheelColliders[2].motorTorque = 0f;
                wheelColliders[3].motorTorque = 0f;
                break;

            case DriveMode.RearWheelDrive:
                drivenWheels = new[] { 2, 3 };
                thrustTorque = accel * (currentTorque / 2f);
                wheelColliders[2].motorTorque = wheelColliders[3].motorTorque = thrustTorque;
                wheelColliders[0].motorTorque = 0f;
                wheelColliders[1].motorTorque = 0f;
                break;

            default: // FourWheelDrive
                drivenWheels = new[] { 0, 1, 2, 3 };
                thrustTorque = accel * (currentTorque / 4f);
                for (int i = 0; i < 4; i++)
                    wheelColliders[i].motorTorque = thrustTorque;
                break;
        }

        // Braking and reverse only apply to the driven wheels for that
        // drive type, matching how the motor torque is distributed above.
        // Non-driven wheels keep their motorTorque at 0 (set above) and
        // simply roll freely / receive brake torque from the braking
        // condition independently below.
        for (int i = 0; i < 4; i++)
        {
            bool isDriven = Array.IndexOf(drivenWheels, i) >= 0;

            if (CurrentSpeed > 5 && Vector3.Angle(transform.forward, m_Rigidbody.linearVelocity) < 50f)
            {
                // Regular braking applies to all wheels regardless of drive type —
                // a real car brakes on all four wheels even if only two are driven.
                wheelColliders[i].brakeTorque = brakeTorque * footbrake;
            }
            else if (footbrake > 0)
            {
                // Below the speed/angle threshold, footbrake input instead triggers reverse:
                // release brakes and apply reverse torque to the driven wheels only.
                wheelColliders[i].brakeTorque = 0f;

                if (isDriven)
                {
                    wheelColliders[i].motorTorque = -reverseTorque * footbrake;
                }
            }
        }
    }

    // ---------------------------------------------------------------
    // Handling Assists
    // ---------------------------------------------------------------

    /// <summary>
    /// Gently realigns the rigidbody's velocity vector toward the car's
    /// facing direction as it turns, to reduce unwanted drift/slide based
    /// on the steeringAssists setting. Skipped entirely if any wheel is airborne.
    /// </summary>
    private void SteeringAssists()
    {
        for (int i = 0; i < 4; i++)
        {
            WheelHit wheelhit;
            wheelColliders[i].GetGroundHit(out wheelhit);
            if (wheelhit.normal == Vector3.zero)
                return; // wheels arent on the ground so dont realign the rigidbody velocity
        }

        // Only apply the assist for small, continuous turns (guards against large
        // rotation deltas, e.g. from a spin-out, being incorrectly corrected).
        if (Mathf.Abs(oldRotation - transform.eulerAngles.y) < 10f)
        {
            var turnadjust = (transform.eulerAngles.y - oldRotation) * steeringAssists;
            Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
            m_Rigidbody.linearVelocity = velRotation * m_Rigidbody.linearVelocity;
        }
        oldRotation = transform.eulerAngles.y;
    }

    /// <summary>
    /// Applies a speed-scaled downward force to improve high-speed stability/grip.
    /// </summary>
    private void Downforce()
    {
        wheelColliders[0].attachedRigidbody.AddForce(
            -transform.up * downForce * wheelColliders[0].attachedRigidbody.linearVelocity.magnitude);
    }

    // ---------------------------------------------------------------
    // Traction Control
    // ---------------------------------------------------------------

    /// <summary>
    /// Checks forward wheel slip on the driven wheels (based on drive type)
    /// and adjusts available motor torque to curb excessive wheelspin.
    /// </summary>
    private void TractionControlSystem()
    {
        WheelHit wheelHit;
        switch (carDriveType)
        {
            case DriveMode.FourWheelDrive:
                for (int i = 0; i < 4; i++)
                {
                    wheelColliders[i].GetGroundHit(out wheelHit);
                    AdjustMotorTorque(wheelHit.forwardSlip);
                }
                break;

            case DriveMode.RearWheelDrive:
                wheelColliders[2].GetGroundHit(out wheelHit);
                AdjustMotorTorque(wheelHit.forwardSlip);

                wheelColliders[3].GetGroundHit(out wheelHit);
                AdjustMotorTorque(wheelHit.forwardSlip);
                break;

            case DriveMode.FrontWheelDrive:
                wheelColliders[0].GetGroundHit(out wheelHit);
                AdjustMotorTorque(wheelHit.forwardSlip);

                wheelColliders[1].GetGroundHit(out wheelHit);
                AdjustMotorTorque(wheelHit.forwardSlip);
                break;
        }
    }

    /// <summary>
    /// Reduces available torque when forward slip exceeds the slip limit
    /// (wheel spinning faster than traction allows), and gradually restores
    /// it back up to the full torque budget otherwise.
    /// </summary>
    private void AdjustMotorTorque(float forwardSlip)
    {
        if (forwardSlip >= slipLimit && currentTorque >= 0)
        {
            currentTorque -= 10 * tractionControlSystem;
        }
        else
        {
            currentTorque += 10 * tractionControlSystem;
            if (currentTorque > fullTorqueOverAllWheels)
            {
                currentTorque = fullTorqueOverAllWheels;
            }
        }
    }

    // ---------------------------------------------------------------
    // Wheel VFX Helpers
    // ---------------------------------------------------------------

    /// <summary>
    /// Plays or stops all wheel smoke particle effects.
    /// </summary>
    private void EnableSmokeEffect(bool enable)
    {
        foreach (ParticleSystem smokeEffect in wheelEffects)
        {
            if (enable)
            {
                smokeEffect.Play();
            }
            else
            {
                smokeEffect.Stop();
            }
        }
    }

    /// <summary>
    /// Enables or disables skid mark trail rendering on all wheels.
    /// </summary>
    private void EnableSkidMarks(bool enable)
    {
        foreach (TrailRenderer skidMark in skidMarks)
        {
            skidMark.emitting = enable;
        }
    }
}
