using System;
using UnityEngine;

[Serializable]
public class Engine
{
    public float idleRPM = 800f;
    public float maxRPM = 8500f;
    public float[] gearRatios = { 3.31f, 2.27f, 1.69f, 1.32f, 1.02f, 0.82f, 0.67f, 0.56f, 0.46f };
    public float finalDriveRatio = 3.73f;
    private int currentGear = 0;
    public bool automaticTransmission = true;
    private bool switchingGears = false;
    private float gearChangeTime = 0.09f;
    private float rpm = 0f;

    public void SetRPM(float avgWheelAngVel)
    {
        if (float.IsNaN(avgWheelAngVel) || float.IsInfinity(avgWheelAngVel)) avgWheelAngVel = 0f;
        float wheelRPM = (avgWheelAngVel * 60f) / (2f * Mathf.PI);
        float ratio = Math.Abs(gearRatios[currentGear] * finalDriveRatio);
        rpm = Mathf.Clamp(Mathf.Max(idleRPM, wheelRPM * ratio), idleRPM, maxRPM);
        if (float.IsNaN(rpm) || float.IsInfinity(rpm)) rpm = idleRPM;
    }

    public float GetCurrentPower(MonoBehaviour ctx)
    {
        if (switchingGears) return 0.3f;
        return Mathf.Clamp01(rpm / maxRPM);
    }

    public void UpGear(MonoBehaviour ctx)
    {
        if (currentGear < gearRatios.Length - 1 && !switchingGears)
        { currentGear++; switchingGears = true; ctx.StartCoroutine(ResetGear()); }
    }
    public void DownGear(MonoBehaviour ctx)
    {
        if (currentGear > 0 && !switchingGears)
        { currentGear--; switchingGears = true; ctx.StartCoroutine(ResetGear()); }
    }
    private System.Collections.IEnumerator ResetGear()
    { yield return new WaitForSeconds(gearChangeTime); switchingGears = false; }

    public void checkGearSwitching(MonoBehaviour ctx, float throttle = 0f)
    {
        if (switchingGears) return;
        if (rpm > maxRPM * 0.95f && currentGear < gearRatios.Length - 1) UpGear(ctx);
        else if (rpm < maxRPM * 0.6f && currentGear > 0) DownGear(ctx);
    }

    public int getCurrentGear() => currentGear + 1;
    public float getRPM() => rpm;
    public bool isSwitchingGears() => switchingGears;
}

[Serializable]
public class WheelProperties
{
    [HideInInspector] public TrailRenderer skidTrail;
    [HideInInspector] public GameObject skidTrailGameObject;

    public Vector3 localPosition;

    // Front wheels: turnAngle = 30, gripMultiplier = 1.3
    // Rear  wheels: turnAngle = 0,  gripMultiplier = 0.7
    public float turnAngle = 0f;
    public float gripMultiplier = 1.0f;

    public float suspensionLength = 0.3f;
    public float mass = 20f;
    public float size = 0.35f;
    public float engineTorque = 500f;
    public float brakeStrength = 8f;

    [HideInInspector] public float lastSuspensionLength = 0f;
    [HideInInspector] public bool slidding;
    [HideInInspector] public Vector3 worldSlipDirection;
    [HideInInspector] public Vector3 suspensionForceDirection;
    [HideInInspector] public Vector3 wheelWorldPosition;
    [HideInInspector] public float wheelCircumference;
    [HideInInspector] public float torque;
    [HideInInspector] public GameObject wheelObject;
    [HideInInspector] public Vector3 localVelocity;
    [HideInInspector] public float normalForce;
    [HideInInspector] public float angularVelocity;
    [HideInInspector] public float slip;
    [HideInInspector] public Vector2 input = Vector2.zero;
    [HideInInspector] public float brake;
    [HideInInspector] public float slipHistory;
    [HideInInspector] public float tcsReduction;
}

public class CarController : MonoBehaviour
{
    [Header("References")]
    public CarAudio audioController;
    public Engine e;
    public GameObject wheelPrefab;
    public GameObject skidMarkPrefab;
    public WheelProperties[] wheels;

    // ── Friction ─────────────────────────────────────────────────────────────
    // These are now public so you can tune in Inspector without recompiling
    [Header("Friction")]
    public float coefStaticFriction = 1.2f;
    public float coefKineticFriction = 0.85f;

    // ── Grip ──────────────────────────────────────────────────────────────────
    // wheelGripX controls turning force.
    // IMPORTANT: this value is applied as an impulse multiplier, not Newtons.
    // Start with 30 and raise if car still slides sideways.
    [Header("Grip")]
    public float wheelGripX = 30f;   // lateral  — raise this to fix sliding
    public float wheelGripZ = 8f;    // longitudinal — lower = more wheelspin

    // ── Suspension ────────────────────────────────────────────────────────────
    [Header("Suspension")]
    public float suspensionForce = 200f;
    public float suspensionForceClamp = 10000f;
    public float dampAmount = 5f;

    // ── Body ──────────────────────────────────────────────────────────────────
    [Header("Body")]
    public float downforce = 0.05f;
    public Vector3 COMOffset = new Vector3(0f, -0.3f, 0.5f);
    public float Inertia = 1.2f;

    // ── Assists ───────────────────────────────────────────────────────────────
    [Header("Assists")]
    public bool throttleAssist = true;
    public bool steeringAssist = true;
    [Range(0f, 0.5f)] public float steeringAssistStrength = 0.05f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Vector2 userInput = Vector2.zero;
    [HideInInspector] public float isBraking = 0f;
    [HideInInspector] public bool forwards = true;

    private float handbrakeInput = 0f;
    private float lastResetTime = -10f;
    private const float resetCooldown = 2f;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();

        // Rigidbody settings — set these here so they're always correct
        // regardless of what the Inspector shows on the component
        rb.mass = 100f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.5f;

        rb.centerOfMass += COMOffset;
        rb.inertiaTensor *= Inertia;

        foreach (var w in wheels)
        {
            w.wheelObject = Instantiate(wheelPrefab, transform);
            w.wheelObject.transform.localPosition = w.localPosition;
            w.wheelObject.transform.eulerAngles = transform.eulerAngles;
            w.wheelObject.transform.localScale = 2f * new Vector3(w.size, w.size, w.size);
            w.wheelCircumference = 2f * Mathf.PI * w.size;

            // initialise so damping doesn't spike on frame 1
            w.lastSuspensionLength = w.size * 2f + w.suspensionLength;

            if (skidMarkPrefab != null)
            {
                w.skidTrailGameObject = Instantiate(skidMarkPrefab, w.wheelObject.transform);
                w.skidTrailGameObject.transform.localPosition = Vector3.zero;
                w.skidTrailGameObject.transform.localRotation = Quaternion.identity;
                w.skidTrailGameObject.transform.parent = null;
                w.skidTrail = w.skidTrailGameObject.GetComponent<TrailRenderer>();
                if (w.skidTrail != null) w.skidTrail.emitting = false;
            }
        }

        foreach (var w in wheels) { w.tcsReduction = 0f; w.slipHistory = 0f; }
    }

    private bool IsFlipped() => Vector3.Dot(transform.up, Vector3.up) < 0.1f;

    private void Update()
    {
        // Reset
        if (Input.GetKeyDown(KeyCode.R) && Time.time - lastResetTime > resetCooldown)
        {
            lastResetTime = Time.time;
            float yRot = transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yRot, 0f);
            if (Physics.Raycast(transform.position, Vector3.down, 3f) || IsFlipped())
                transform.position += Vector3.up * 2f;
            rb.linearVelocity = transform.forward * 2f;
            rb.angularVelocity = Vector3.zero;
        }

        if (GetComponent<CarAIControl>() != null) return;

        float rawH = Input.GetAxisRaw("Horizontal");
        float rawV = Input.GetAxisRaw("Vertical");

        float speedFactor = 1f / (1f + rb.linearVelocity.magnitude / 28f);
        userInput.x = Mathf.Lerp(userInput.x, rawH * speedFactor, Time.deltaTime * 10f);
        userInput.y = Mathf.Lerp(userInput.y, rawV, Time.deltaTime * 10f);

        bool movingFwd = rb.linearVelocity.magnitude > 0.5f && forwards;
        isBraking = (rawV < -0.1f && movingFwd) ? 1f : 0f;

        handbrakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];
            if (!IsValid(w.slip)) w.slip = 0f;

            if (throttleAssist)
            {
                const float ts = 0.85f, tol = 0.05f;
                if (w.slip > ts + tol)
                    w.tcsReduction = Mathf.Lerp(w.tcsReduction, 1f,
                        Mathf.Clamp01((w.slip - ts) * 2f) / 5f);
                else if (w.slip < ts - tol)
                    w.tcsReduction = Mathf.Lerp(w.tcsReduction, 0f, 0.6f * Time.deltaTime);
                w.tcsReduction = Mathf.Clamp01(w.tcsReduction);
            }

            w.brake = isBraking * (1f - w.tcsReduction);

            w.input.x = Mathf.Lerp(w.input.x, userInput.x, Time.deltaTime * 10f);
            float s = Mathf.Clamp01(w.slip);
            if (s > 0.3f && s < 1.5f && steeringAssist)
                w.input.x = Mathf.Lerp(w.input.x, 0f, s * Time.deltaTime * steeringAssistStrength * 10f);

            float finalThrottle = userInput.y * (1f - w.tcsReduction);
            if (!IsValid(finalThrottle)) finalThrottle = 0f;
            w.input.y = Mathf.Lerp(w.input.y, finalThrottle, 0.95f * Time.deltaTime * 60f);
            if (!IsValid(w.input.y)) w.input.y = 0f;
        }

        if (Input.GetKeyDown(KeyCode.E)) e.UpGear(this);
        else if (Input.GetKeyDown(KeyCode.Q)) e.DownGear(this);
        e.checkGearSwitching(this, Mathf.Max(0f, userInput.y));

        if (audioController != null)
        {
            float avg = 0f;
            foreach (var w in wheels) avg += w.slip;
            avg /= wheels.Length;
            audioController.UpdateAudioValues(e.getRPM(), Mathf.Max(0f, userInput.y),
                rb.linearVelocity.magnitude, avg, e.isSwitchingGears());
        }
    }

    private void FixedUpdate()
    {
        rb.AddForce(-transform.up * rb.linearVelocity.magnitude * downforce);

        float avgAngVel = 0f;

        foreach (var w in wheels)
        {
            float rayLen = w.size * 2f + w.suspensionLength;
            Transform wheelObj = w.wheelObject.transform;
            Transform wheelVisual = wheelObj.GetChild(0);

            // ── 1. Steer the wheel ───────────────────────────────────────────
            wheelObj.localRotation = Quaternion.Euler(0f, w.turnAngle * w.input.x, 0f);

            // ── 2. Measure velocity at this wheel in wheel-local space ────────
            // MUST happen AFTER rotation so local axes are correct
            w.wheelWorldPosition = transform.TransformPoint(w.localPosition);
            Vector3 velAtWheel = rb.GetPointVelocity(w.wheelWorldPosition);
            w.localVelocity = wheelObj.InverseTransformDirection(velAtWheel);
            forwards = w.localVelocity.z > 0.1f;

            // ── 3. Engine torque ─────────────────────────────────────────────
            w.torque = w.engineTorque * w.input.y * e.GetCurrentPower(this);
            if (!IsValid(w.torque)) w.torque = 0f;

            float inertia = w.mass * w.size * w.size / 2f;

            // ── 4. Raycast ───────────────────────────────────────────────────
            RaycastHit hit;
            bool grounded = Physics.Raycast(w.wheelWorldPosition, -transform.up, out hit, rayLen);

            // ── 5. Lateral friction ──────────────────────────────────────────
            // This is the KEY fix for sliding.
            // We apply lateral friction as a direct velocity correction impulse
            // rather than a scaled force — this removes the normalForce dependency
            // that was making lateral grip near-zero at low speed.
            float lateralVel = w.localVelocity.x;
            if (!IsValid(lateralVel)) lateralVel = 0f;

            // Additional hit-point lateral damping (only when grounded)
            float lateralHitVel = 0f;
            if (grounded)
            {
                lateralHitVel = wheelObj.InverseTransformDirection(
                    rb.GetPointVelocity(hit.point)).x;
                if (!IsValid(lateralHitVel)) lateralHitVel = 0f;
            }

            // ── 6. Longitudinal friction ─────────────────────────────────────
            float longitudinalFriction = -wheelGripZ *
                (w.localVelocity.z - w.angularVelocity * w.size);
            if (!IsValid(longitudinalFriction)) longitudinalFriction = 0f;

            // ── 7. Angular velocity ──────────────────────────────────────────
            w.angularVelocity += (w.torque - longitudinalFriction * w.size)
                                / inertia * Time.fixedDeltaTime;
            w.angularVelocity *= 1f - w.brake * w.brakeStrength * Time.fixedDeltaTime;
            if (!IsValid(w.angularVelocity)) w.angularVelocity = 0f;
            if (handbrakeInput > 0.5f) w.angularVelocity = 0f;

            if (grounded)
            {
                // ── 8. Suspension spring ─────────────────────────────────────
                float compression = rayLen - hit.distance;
                float damping = (w.lastSuspensionLength - hit.distance) * dampAmount;
                w.normalForce = Mathf.Clamp(
                    (compression + damping) * suspensionForce, 0f, suspensionForceClamp);
                if (!IsValid(w.normalForce)) w.normalForce = 0f;
                w.lastSuspensionLength = hit.distance;

                Vector3 springForce = hit.normal * w.normalForce;
                w.suspensionForceDirection = springForce;

                // ── 9. LATERAL GRIP — applied directly as impulse ────────────
                // Instead of multiplying by normalForce (which breaks at low speed),
                // we cancel the sideways velocity directly each physics step.
                // gripMultiplier makes front wheels grip harder than rear.
                float lateralFriction = -wheelGripX * w.gripMultiplier
                    * lateralVel - 2f * lateralHitVel;
                if (!IsValid(lateralFriction)) lateralFriction = 0f;

                // Clamp lateral friction so it can't exceed what's physically possible
                float maxLateral = w.normalForce * coefStaticFriction;
                lateralFriction = Mathf.Clamp(lateralFriction, -maxLateral, maxLateral);

                // Longitudinal force also clamped by normal force
                float longForce = Mathf.Clamp(longitudinalFriction,
                    -w.normalForce * coefStaticFriction,
                     w.normalForce * coefStaticFriction);

                Vector3 totalLocalForce = new Vector3(lateralFriction, 0f, longForce);
                Vector3 totalWorldForce = wheelObj.TransformDirection(totalLocalForce);
                w.worldSlipDirection = totalWorldForce;

                w.slip = maxLateral > 0.001f
                    ? Mathf.Abs(lateralFriction) / maxLateral : 0f;
                w.slidding = w.slip > 1f;

                // Apply spring + friction at contact point
                Vector3 totalForce = springForce + totalWorldForce;
                if (IsValid(totalForce.x) && IsValid(totalForce.y) && IsValid(totalForce.z))
                    rb.AddForceAtPosition(totalForce, hit.point);

                // Wheel visual sits one radius above contact
                wheelObj.position = hit.point + transform.up * w.size;

                // ── 10. Skid marks ───────────────────────────────────────────
                HandleSkidMark(w, hit, wheelObj);
            }
            else
            {
                // Airborne: hang wheel at full droop
                wheelObj.position = w.wheelWorldPosition - transform.up * (rayLen - w.size);
                StopSkidMark(w);
            }

            avgAngVel += w.angularVelocity;
            wheelVisual.Rotate(Vector3.right,
                w.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime, Space.Self);
        }

        avgAngVel /= wheels.Length;
        e.SetRPM(avgAngVel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsValid(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    private static bool IsValid(Vector3 v) =>
        !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
        !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);

    private void HandleSkidMark(WheelProperties w, RaycastHit hit, Transform wheelObj)
    {
        if (!w.slidding) { StopSkidMark(w); return; }

        if (w.skidTrail == null && skidMarkPrefab != null)
        {
            GameObject obj = Instantiate(skidMarkPrefab, transform);
            obj.transform.SetParent(w.wheelObject.transform);
            obj.transform.localPosition = Vector3.zero;
            w.skidTrail = obj.GetComponent<TrailRenderer>();
            w.skidTrail.time = 30f;
            w.skidTrail.autodestruct = true;
            w.skidTrail.emitting = false;
            w.skidTrail.transform.position = hit.point;
            Vector3 sd = Vector3.ProjectOnPlane(w.worldSlipDirection.normalized, hit.normal);
            if (sd.sqrMagnitude < 0.001f)
                sd = Vector3.ProjectOnPlane(wheelObj.forward, hit.normal).normalized;
            w.skidTrail.transform.rotation =
                Quaternion.LookRotation(sd, hit.normal) * Quaternion.Euler(90f, 0f, 0f);
        }
        else if (w.skidTrail != null)
        {
            if (!w.skidTrail.emitting) w.skidTrail.emitting = true;
            w.skidTrail.transform.position = hit.point;
            Vector3 sd = Vector3.ProjectOnPlane(w.worldSlipDirection.normalized, hit.normal);
            if (sd.sqrMagnitude < 0.001f)
                sd = Vector3.ProjectOnPlane(wheelObj.forward, hit.normal).normalized;
            w.skidTrail.transform.rotation =
                Quaternion.LookRotation(sd, hit.normal) * Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void StopSkidMark(WheelProperties w)
    {
        if (w.skidTrail == null) return;
        if (w.skidTrail.emitting)
        {
            w.skidTrail.emitting = false;
            w.skidTrail.transform.parent = null;
            Destroy(w.skidTrail.gameObject, w.skidTrail.time);
        }
        else Destroy(w.skidTrail.gameObject);
        w.skidTrail = null;
    }
}
