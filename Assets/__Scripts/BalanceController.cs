using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BalanceController : MonoBehaviour
{
    [SerializeField] Animator anim;
    private InputSystem_Actions playerControls;
    private InputAction moveAction;
    CharacterController characterController;
    float lean;           // current balance
    float leanVelocity;   // optional inertia
    float gustForce;
    float gustTimer;
    [SerializeField] float driftStrength = 0.35f;  // overall drift magnitude
    [SerializeField] float driftChangeInterval = 1.25f;
    [SerializeField] float driftSmoothing = 1.5f;
    [SerializeField] float driftMinTargetAbs = 0.45f;
    [SerializeField] float counterStrength = 2f;   // how strong the player counterbalance is
    [SerializeField] float inputDeadzone = 0.15f;
    [SerializeField] float holdRampTime = 0.35f;
    [SerializeField] float maxHoldCounterMultiplier = 1.8f;
    [SerializeField] float damping = 4f;           // damping rate per second
    [SerializeField] float leanAcceleration = 20; // how quickly lean reacts to force
    [SerializeField] float maxLeanVelocity = 30f;  // cap lean speed for stability
    [SerializeField] float failThreshold = 40f;    // how far you have to lean before you fail

    [SerializeField] Camera playerCam;
    [SerializeField] float maxRollAngle = 10f;
    [SerializeField] float rollSmoothing = 8f;
    [SerializeField] bool invertInputDirection = true;
    [SerializeField] bool invertCameraRoll = true;

    [SerializeField] float struggleBobAmount = 0.05f;
    [SerializeField] float struggleBobSpeed = 8f;

    [SerializeField] bool enableDirectionDebugLogs = true;
    [SerializeField] float debugLogInterval = 0.35f;

    [SerializeField] float forwardMoveInterval = 3f;
    [SerializeField] float forwardMoveDistance = 1.25f;
    [SerializeField] float centerThresholdForForwardMove = 2.5f;

    [SerializeField] RectTransform needle;
    [SerializeField] float needleMaxOffset = 200f;

    Vector3 cameraBaseLocalPosition;
    Quaternion cameraBaseLocalRotation;
    float driftCurrent;
    float driftTarget;
    float driftChangeTimer;
    float debugLogTimer;
    int lastDriftSign = 1;
    int holdDirection;
    float holdTimer;
    float forwardMoveTimer;
    bool waitingForCenteredForwardMove;

    bool stopMovement = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerControls = new InputSystem_Actions();
        characterController = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
    }

    void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Disable();
        }
    }

    void Start()
    {
        if (playerCam == null)
        {
            playerCam = GetComponentInChildren<Camera>();
        }

        if (playerCam != null)
        {
            cameraBaseLocalPosition = playerCam.transform.localPosition;
            cameraBaseLocalRotation = playerCam.transform.localRotation;
        }

        driftTarget = Random.Range(0f, 1f);
        driftCurrent = driftTarget;
        lastDriftSign = 1;
        driftChangeTimer = driftChangeInterval;
        debugLogTimer = 0f;
        holdDirection = 0;
        holdTimer = 0f;
        forwardMoveTimer = Mathf.Max(0.1f, forwardMoveInterval);
        waitingForCenteredForwardMove = false;

        StopMovement();
        Invoke(nameof(ResumeMovement), 2f);

        //StartCoroutine(GustRoutine());

    }

    void Update()
    {
        if (stopMovement)
        {
            ResetLeanAndCamera();
            return;
        }
        float dt = Time.deltaTime;

        UpdateForwardStep(dt);

        // 1. Smoothed random drift (changes slowly so it is reactable)
        float drift = UpdateDrift(dt);

        // 2. Player counterbalance
        float horizontalRaw = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;
        float horizontal = ApplyDeadzone(horizontalRaw, inputDeadzone);
        float inputSign = invertInputDirection ? -1f : 1f;
        float holdMultiplier = UpdateHoldMultiplier(horizontal, dt);
        float counter = horizontal * counterStrength * holdMultiplier * inputSign;

        // 3. Wind gusts
        //UpdateGust(dt);

        // 4. Combine forces
        float totalForce = drift + counter + gustForce;

        // 5. Apply inertia
        leanVelocity += totalForce * leanAcceleration * dt;
        leanVelocity = Mathf.Clamp(leanVelocity, -maxLeanVelocity, maxLeanVelocity);
        leanVelocity *= Mathf.Exp(-damping * dt);

        lean += leanVelocity * dt;
        lean = Mathf.Clamp(lean, -failThreshold, failThreshold);

        // 6. UI + Animation
        //UpdateUI();
        //UpdateAnimation();
        UpdateCameraTilt();
        UpdateStruggleBob();
        LogDirectionDebug(dt, horizontal, drift, counter, totalForce);

        // 7. Failure check
        if (Mathf.Abs(lean) >= failThreshold)
            Fail();
    }

    public void StopMovement()
    {
        stopMovement = true;
        ResetLeanAndCamera();
    }

    public void ResumeMovement()
    {
        stopMovement = false;
        forwardMoveTimer = Mathf.Max(0.1f, forwardMoveInterval);
        waitingForCenteredForwardMove = false;
    }

    void UpdateForwardStep(float dt)
    {
        if (!waitingForCenteredForwardMove)
        {
            forwardMoveTimer -= dt;
            if (forwardMoveTimer > 0f)
            {
                return;
            }

            waitingForCenteredForwardMove = true;
        }

        if (Mathf.Abs(lean) > centerThresholdForForwardMove)
        {
            return;
        }

        waitingForCenteredForwardMove = false;
        forwardMoveTimer = Mathf.Max(0.1f, forwardMoveInterval);
        Vector3 forwardStep = transform.forward * forwardMoveDistance;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(forwardStep);
        }
        else
        {
            transform.position += forwardStep;
        }
    }

    void Fail()
    {
        Debug.Log("Failed! Lean was: " + lean);
        // You can add failure logic here, like restarting the level or showing a game over screen.
        // For now, we'll just reset the lean for testing purposes.
        lean = 0f;
        leanVelocity = 0f;
    }

    void ResetLeanAndCamera()
    {
        lean = 0f;
        leanVelocity = 0f;
        gustForce = 0f;
        gustTimer = 0f;
        holdDirection = 0;
        holdTimer = 0f;

        if (playerCam != null)
        {
            playerCam.transform.localRotation = cameraBaseLocalRotation;
            playerCam.transform.localPosition = cameraBaseLocalPosition;
        }
    }



    void UpdateStruggleBob()
    {
        if (playerCam == null)
        {
            return;
        }

        float danger = Mathf.InverseLerp(0.6f * failThreshold, failThreshold, Mathf.Abs(lean));
        if (danger <= 0f)
        {
            playerCam.transform.localPosition = Vector3.Lerp(
                playerCam.transform.localPosition,
                cameraBaseLocalPosition,
                Time.deltaTime * rollSmoothing);
            return;
        }

        float bob = Mathf.Sin(Time.time * struggleBobSpeed) * struggleBobAmount * danger;

        Vector3 pos = cameraBaseLocalPosition;
        pos.y += bob;
        playerCam.transform.localPosition = pos;
    }



    void UpdateCameraTilt()
    {
        if (playerCam == null)
        {
            return;
        }

        float normalized = Mathf.Clamp(lean / failThreshold, -1f, 1f);
        float rollSign = invertCameraRoll ? -1f : 1f;
        float targetRoll = normalized * maxRollAngle * rollSign;

        Quaternion targetRot = Quaternion.Euler(
            cameraBaseLocalRotation.eulerAngles.x,
            cameraBaseLocalRotation.eulerAngles.y,
            targetRoll
        );

        playerCam.transform.localRotation =
            Quaternion.Slerp(playerCam.transform.localRotation, targetRot, Time.deltaTime * rollSmoothing);
    }

    void UpdateUI()
    {
        Debug.Log("Updating UI. Lean: " + lean);
        float normalized = Mathf.Clamp(lean / failThreshold, -1f, 1f);
        needle.anchoredPosition = new Vector2(normalized * needleMaxOffset, 0f);
    }

    float UpdateDrift(float dt)
    {
        driftChangeTimer -= dt;
        if (driftChangeTimer <= 0f)
        {
            lastDriftSign *= -1;
            float magnitude = Random.Range(driftMinTargetAbs, 1f);
            driftTarget = magnitude * lastDriftSign;
            driftChangeTimer = driftChangeInterval;
        }

        driftCurrent = Mathf.MoveTowards(driftCurrent, driftTarget, driftSmoothing * dt);
        return driftCurrent * driftStrength;
    }

    float ApplyDeadzone(float value, float deadzone)
    {
        if (Mathf.Abs(value) <= deadzone)
        {
            return 0f;
        }

        float sign = Mathf.Sign(value);
        float normalized = (Mathf.Abs(value) - deadzone) / (1f - deadzone);
        return sign * Mathf.Clamp01(normalized);
    }

    float UpdateHoldMultiplier(float horizontal, float dt)
    {
        int direction = horizontal > 0f ? 1 : horizontal < 0f ? -1 : 0;

        if (direction == 0)
        {
            holdDirection = 0;
            holdTimer = 0f;
            return 1f;
        }

        if (direction != holdDirection)
        {
            holdDirection = direction;
            holdTimer = 0f;
        }
        else
        {
            holdTimer += dt;
        }

        if (holdRampTime <= 0f)
        {
            return maxHoldCounterMultiplier;
        }

        float t = Mathf.Clamp01(holdTimer / holdRampTime);
        return Mathf.Lerp(1f, maxHoldCounterMultiplier, t);
    }

    void LogDirectionDebug(float dt, float horizontal, float drift, float counter, float totalForce)
    {
        if (!enableDirectionDebugLogs)
        {
            return;
        }

        debugLogTimer -= dt;
        if (debugLogTimer > 0f)
        {
            return;
        }

        debugLogTimer = Mathf.Max(0.05f, debugLogInterval);

        float inputSign = invertInputDirection ? -1f : 1f;
        float rollSign = invertCameraRoll ? -1f : 1f;
        float targetRoll = Mathf.Clamp(lean / failThreshold, -1f, 1f) * maxRollAngle * rollSign;

        string leanDirection = lean > 0.05f ? "RIGHT (+lean)" : lean < -0.05f ? "LEFT (-lean)" : "CENTER";
        string rollDirection = targetRoll > 0.1f ? "RIGHT ROLL" : targetRoll < -0.1f ? "LEFT ROLL" : "NEUTRAL ROLL";

        string suggestedInput = "NONE";
        if (Mathf.Abs(lean) > 0.05f)
        {
            float desiredCounterForceSign = -Mathf.Sign(lean);
            float requiredHorizontalSign = desiredCounterForceSign / inputSign;
            suggestedInput = requiredHorizontalSign >= 0f ? "RIGHT (D / Stick Right)" : "LEFT (A / Stick Left)";
        }

        Debug.Log(
            $"[Balance] Lean={lean:F2} [{leanDirection}] | CamTargetRoll={targetRoll:F2} [{rollDirection}] | InputX={horizontal:F2} SuggestedInput={suggestedInput} | Drift={drift:F2} Counter={counter:F2} Gust={gustForce:F2} Total={totalForce:F2}");
    }


    public void TriggerGust(float strength, float duration)
    {
        gustForce = strength;
        gustTimer = duration;
    }

    void UpdateGust(float dt)
    {
        if (gustTimer > 0f)
        {
            gustTimer -= dt;
            leanVelocity += gustForce * dt;
        }
    }


    IEnumerator GustRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3f, 8f));
            if (stopMovement)
            {
                continue;
            }
            float strength = Random.Range(-3f, 3f);
            float duration = Random.Range(0.5f, 1.5f);
            TriggerGust(strength, duration);
        }
    }


    void UpdateAnimation()
    {
        float normalized = Mathf.Clamp(lean / failThreshold, -1f, 1f);
        anim.SetFloat("LeanAmount", normalized);
    }


}
