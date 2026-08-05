using System;
using UnityEngine;

internal enum DriveMode
{
    FrontWheelDrive,
    RearWheelDrive,
    FourWheelDrive
}

internal enum SpeedType
{
    MPH,
    KPH
}

public class BMWCarController : MonoBehaviour
{
    [SerializeField] private DriveMode carDriveType = DriveMode.FourWheelDrive;
    [SerializeField] private WheelCollider[] wheelColliders = new WheelCollider[4];
    [SerializeField] private CarDataSO m_CarData;
    [SerializeField] private ParticleSystem[] wheelEffects;
    [SerializeField] private TrailRenderer[] skidMarks;
    private bool smokeEffectEnabled;
    [SerializeField] private GameObject[] wheelMeshes = new GameObject[4];
    [HideInInspector] private Vector3 m_CentreOfMassOffset;//this shouldn't be touched 
    [SerializeField] private float maximumSteerAngle;
    [Range(0, 1)][SerializeField] private float steeringAssists; // 0 is raw physics , 1 the car will grip in the direction it is facing
    [Range(0, 1)][SerializeField] private float tractionControlSystem; // 0 is no traction control, 1 is full interference
    [SerializeField] private float fullTorqueOverAllWheels;
    [SerializeField] private float reverseTorque;
    [SerializeField] private float maximumHandbrakeTorque;
    [SerializeField] private float downForce = 100f;
    [SerializeField] private SpeedType speedType;
    [SerializeField] public static float topSpeed = 80f;
    [SerializeField] private int noOfGears = 5;
    [SerializeField] private float revRangeBoundary = 1f;
    [SerializeField] private float slipLimit;
    [SerializeField] private float brakeTorque;

    [SerializeField] private float forwardGrip = 1.5f;
    [SerializeField] private float sidewaysGrip = 2.0f;

    private Quaternion[] wheelMeshLocalRotations;
    private float steerAngle;
    private int gearNum;
    private float gearFactor;
    private float oldRotation;
    private float currentTorque;
    private Rigidbody m_Rigidbody;
    public bool Skidding { get; private set; }
    public float BrakeInput { get; private set; }
    public float CurrentSteerAngle => steerAngle;
    public float CurrentSpeed => m_Rigidbody.linearVelocity.magnitude * 2.23693629f;
    public float MaxSpeed => topSpeed;
    public float Revs { get; private set; }
    public float AccelInput { get; private set; }

    private void Start()
    {
        wheelMeshLocalRotations = new Quaternion[4];
        for (int i = 0; i < 4; i++)
        {
            wheelMeshLocalRotations[i] = wheelMeshes[i].transform.localRotation;
        }
        wheelColliders[0].attachedRigidbody.centerOfMass = m_CentreOfMassOffset;

        maximumHandbrakeTorque = float.MaxValue;

        m_Rigidbody = GetComponent<Rigidbody>();
        currentTorque = fullTorqueOverAllWheels - (tractionControlSystem * fullTorqueOverAllWheels);

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
    public void SetCarData(CarDataSO data)
    {
        if (data == null) return;

        m_Rigidbody.mass = data.mass;
        topSpeed = data.topSpeedSuperSpeedway;
    }
    private void FixedUpdate()
    {
        if (GetComponent<CarAIControl>() != null) return;
        ReadInputs();
    }

    // Move: public entry point for CarAIControl
    // steering: -1 to 1, accel: 0 to 1, footbrake: -1 to 0, handbrake: 0 to 1
    public void Move(float steering, float accel, float footbrake, float handbrake)
    {
        RunDrivePipeline(steering, accel, footbrake, handbrake);
    }

    private void ReadInputs()
    {
        float steering = Input.GetAxis("Horizontal");
        float accelRaw = Input.GetAxis("Vertical");
        float handbrake = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        float acceleration = Mathf.Clamp(accelRaw, 0f, 1f);
        float footbrake = Mathf.Clamp(accelRaw, -1f, 0f);

        RunDrivePipeline(steering, acceleration, footbrake, handbrake);
    }

    //Shared pipeline used by both keyboard (CheckInput) and AI (Move)
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

    private static float CurveFactor(float factor) => 1 - (1 - factor) * (1 - factor);
    private static float ULerp(float from, float to, float value) => (1.0f - value) * from + value * to;

    private void CalculateGearFactor()
    {
        float f = (1 / (float)noOfGears);
        var targetGearFactor = Mathf.InverseLerp(f * gearNum, f * (gearNum + 1), Mathf.Abs(CurrentSpeed / MaxSpeed));
        gearFactor = Mathf.Lerp(gearFactor, targetGearFactor, Time.deltaTime * 5f);
    }

    private void CalculateRevolutions()
    {
        CalculateGearFactor();
        var gearNumFactor = gearNum / (float)noOfGears;
        var revsRangeMin = ULerp(0f, revRangeBoundary, CurveFactor(gearNumFactor));
        var revsRangeMax = ULerp(revRangeBoundary, 1f, gearNumFactor);
        Revs = ULerp(revsRangeMin, revsRangeMax, gearFactor);
    }

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
                wheelColliders[i].brakeTorque = 0f;

                if (isDriven)
                {
                    wheelColliders[i].motorTorque = -reverseTorque * footbrake;
                }
            }
        }
    }

    private void SteeringAssists()
    {
        for (int i = 0; i < 4; i++)
        {
            WheelHit wheelhit;
            wheelColliders[i].GetGroundHit(out wheelhit);
            if (wheelhit.normal == Vector3.zero)
                return; // wheels arent on the ground so dont realign the rigidbody velocity
        }

        if (Mathf.Abs(oldRotation - transform.eulerAngles.y) < 10f)
        {
            var turnadjust = (transform.eulerAngles.y - oldRotation) * steeringAssists;
            Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
            m_Rigidbody.linearVelocity = velRotation * m_Rigidbody.linearVelocity;
        }
        oldRotation = transform.eulerAngles.y;
    }

    private void Downforce()
    {
        wheelColliders[0].attachedRigidbody.AddForce(
            -transform.up * downForce * wheelColliders[0].attachedRigidbody.linearVelocity.magnitude);
    }

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
    private void EnableSkidMarks(bool enable)
    {
        foreach (TrailRenderer skidMark in skidMarks)
        {
            skidMark.emitting = enable;
        }
    }
}