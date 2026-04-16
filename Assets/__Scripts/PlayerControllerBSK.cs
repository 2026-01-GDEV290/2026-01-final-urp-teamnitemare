using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerBSK : MonoBehaviour
{
    private InputSystem_Actions playerControls;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 0.8f, 0f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 3f; //1.4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Feather Boost")]
    [SerializeField] private float featherJumpHeightMultiplier = 5;
    [SerializeField] private float featherFallGravityMultiplier = 0.55f;
    [SerializeField] private float featherAirBoostStrength = 15f;
    [SerializeField] private GameObject wings;
    [SerializeField] WingAnimationControl wingAnimationControl;
    //[SerializeField] private AlphaControllerForAnimationRenderer wingsAlphaControl;

    [Header("Look")]
    [SerializeField] private float rotateSpeed = 10f;
    //[SerializeField] private float maxLookAngle = 85f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private float walkSoundDistance = 1.8f;
    [SerializeField] private float walkSoundVolume = 0.7f;
    [SerializeField] private AudioSource walkSoundSource;

    [SerializeField] TMP_Text helpText;

    [SerializeField] private GameObject audioCurvePrefab;

    List<GameObject> activeAudioCurves = new List<GameObject>();
    List<PitchBlackAttraction> activeAttractions = new List<PitchBlackAttraction>();

    private CharacterController characterController;
    private float verticalVelocity;
    private float cameraPitch;

    private float rotationX;
    private float rotationY;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float walkSoundDistanceAccumulator;
    private int walkSoundIndex;

    private InteractCollider interactCollider;

    bool lookingAtInteractable = false;
    InteractableBase currentInteractable = null;

    [SerializeField] private bool hasFeathers = false;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerControls = new InputSystem_Actions();

        rotationX = 0f;
        rotationY = transform.eulerAngles.y;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerControllerBSK could not find a Camera. Assign one in the inspector or add a child Camera.");
        }
        
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(transform);
            playerCamera.transform.localPosition = cameraLocalPosition;
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        if (walkSoundSource == null)
        {
            walkSoundSource = GetComponent<AudioSource>();
            if (walkSoundSource == null)
            {
                walkSoundSource = gameObject.AddComponent<AudioSource>();
            }
        }

        walkSoundSource.playOnAwake = false;
        walkSoundSource.loop = false;
        walkSoundSource.spatialBlend = 0f;
        
        if (wingAnimationControl == null)
        {
            wingAnimationControl = GetComponentInChildren<WingAnimationControl>();
            if (wingAnimationControl == null)
                Debug.LogWarning("PlayerControllerBSK could not find a WingAnimationControl in children. Feather boost animations will not work.");
        }
        if (wings == null)
        {
            wings = GameObject.Find("AngelWings");
            if (wings == null)
            {
                Debug.LogWarning("PlayerControllerBSK could not find a GameObject named 'AngelWings' in the scene. Assign the wings GameObject in the inspector for feather boost visuals.");
            }
        }
        if (helpText != null)
        {
            Canvas canvas = helpText.GetComponentInParent<Canvas>();
            Debug.Log("PPU:" + canvas.referencePixelsPerUnit);
        }
        activeAttractions = new List<PitchBlackAttraction>(
            FindObjectsByType<PitchBlackAttraction>(FindObjectsSortMode.None));
        // Instantiate 5 audio curves
        for (int i = 0; i < 5; i++)
        {
            GameObject curve = Instantiate(audioCurvePrefab);
            activeAudioCurves.Add(curve);
            // keep them hidden until needed
            curve.SetActive(false);
        }
    }

    private IEnumerator CheckFeatherState()
    {
        while (true)
        {
            // This is a simple way to toggle feather state for testing. Replace with actual game logic as needed.
            if (hasFeathers)
            {
                if (wings.activeInHierarchy == false)
                {
                    wings.SetActive(true);
                    wingAnimationControl.ResetToIdle();
                    Debug.Log("Feathers!");
                }
            }
            else
            {
                if (wings.activeInHierarchy == true)
                {
                    wings.SetActive(false);
                    //wingAnimationControl.ResetToIdle();
                    Debug.Log("No Feathers!");
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    public void FeathersObtained()
    {
        hasFeathers = true;
        wings.SetActive(true);
        wingAnimationControl.ResetToIdle();
        if (helpText != null)
        {
            helpText.text = "You got feathers! Press Jump while in the air to boost yourself upwards.";            
            Invoke(nameof(ClearHelpText), 5f);
        }
    }
    void ClearHelpText()
    {
        if (helpText != null)
        {
            helpText.text = "";
        }
    }

    void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
        lookAction = playerControls.Player.Look;
        jumpAction = playerControls.Player.Jump;
        attackAction = playerControls.Player.Attack;
        jumpAction.performed += JumpActionPerformed;
        attackAction.performed += AttackAction;
        interactAction = playerControls.Player.Interact;
        interactAction.performed += InteractAction;

        // get child InteractArea's InteractCollider and subscribe to its event
        InteractCollider[] interactColliders = GetComponentsInChildren<InteractCollider>();
        if (interactColliders.Length > 0)
        {
            interactCollider = interactColliders[0];
            interactCollider.OnPlayerHitInteractable += InteractTrigger;
            interactCollider.OnPlayerLeaveInteractable += InteractLeaveTrigger;
        }
        else
        {
            Debug.LogWarning("PlayerControllerBSK could not find a InteractCollider for interaction events.");
        }
    }
    void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= JumpActionPerformed;
        }

        if (attackAction != null)
        {
            attackAction.performed -= AttackAction;
        }
        if (interactAction != null)
        {
            interactAction.performed -= InteractAction;
        }

        if (playerControls != null)
        {
            playerControls.Disable();
        }

        // unsubscribe from interactCollider events
        if (interactCollider != null)
        {
            interactCollider.OnPlayerHitInteractable -= InteractTrigger;
            interactCollider.OnPlayerLeaveInteractable -= InteractLeaveTrigger;
        }
    }
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize coyote timer so player can jump even if not grounded at spawn.
        coyoteTimer = coyoteTime;
        StartCoroutine(nameof(CheckFeatherState), 1f);
    }

    void Update()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        HandleLook(lookInput);
        HandleMovement(moveInput);
    }

    void JumpActionPerformed(InputAction.CallbackContext context)
    {
        //Debug.Log($"Jump input fired. coyoteTimer={coyoteTimer:F3}, jumpBufferTimer will be set to {jumpBufferTime:F3}. CharController.isGrounded={characterController.isGrounded}");
        jumpBufferTimer = jumpBufferTime;
    }

    void HandleLook(Vector2 lookVector)
    {
        // x-axis of mouse controls pitch (looking up/down)
        rotationY += lookVector.x * rotateSpeed * Time.deltaTime;
        rotationX -= lookVector.y * rotateSpeed * Time.deltaTime;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }

    void HandleMovement(Vector2 moveInput)
    {
        float inputX = moveInput.x;
        float inputZ = moveInput.y;
        float downwardGravity = -Mathf.Abs(gravity);
        
        // Cache grounded state at frame start for consistency
        bool wasGroundedThisFrame = characterController.isGrounded;
        //Debug.Log($"HandleMovement frame start: groundedCheck={wasGroundedThisFrame}, coyote={coyoteTimer:F3}, buffer={jumpBufferTimer:F3}");

        Vector3 move = (transform.right * inputX + transform.forward * inputZ).normalized;
        Vector3 horizontalMove = move * moveSpeed * Time.deltaTime;
        characterController.Move(horizontalMove);

        UpdateWalkSound(horizontalMove, wasGroundedThisFrame);

        if (wasGroundedThisFrame && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (wasGroundedThisFrame)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        jumpBufferTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            float jumpHeightToUse = hasFeathers ? jumpHeight * featherJumpHeightMultiplier : jumpHeight;
            verticalVelocity = Mathf.Sqrt(jumpHeightToUse * -2f * downwardGravity);
            //Debug.Log($"JUMP APPLIED: jumpHeight={jumpHeightToUse:F2}, vertVel={verticalVelocity:F2}");

            if (hasFeathers && wingAnimationControl != null)
            {
                wingAnimationControl.WingFlap();
            }

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
        else if (jumpBufferTimer > 0f && !wasGroundedThisFrame && hasFeathers)
        {
            // Airborne feather boost can be triggered on every jump press while airborne.
            verticalVelocity = Mathf.Max(verticalVelocity, featherAirBoostStrength);
            //Debug.Log($"FEATHER BOOST APPLIED: vertVel={verticalVelocity:F2}");

            if (wingAnimationControl != null)
            {
                wingAnimationControl.WingFlap();
            }

            jumpBufferTimer = 0f;
        }
        else if (jumpBufferTimer > 0f)
        {
            //Debug.Log($"Jump buffer set but NOT jumping: buffer={jumpBufferTimer:F3}, coyote={coyoteTimer:F3}, wasGrounded={wasGroundedThisFrame}");
        }

        float gravityToUse = downwardGravity;
        if (hasFeathers && verticalVelocity <= 0f)
        {
            gravityToUse *= featherFallGravityMultiplier;
        }

        verticalVelocity += gravityToUse * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void UpdateWalkSound(Vector3 horizontalMove, bool wasGroundedThisFrame)
    {
        if (!wasGroundedThisFrame || walkSounds == null || walkSounds.Length == 0)
        {
            if (!wasGroundedThisFrame)
            {
                walkSoundDistanceAccumulator = 0f;
            }

            return;
        }

        float moveDistance = horizontalMove.magnitude;
        if (moveDistance <= 0.001f)
        {
            walkSoundDistanceAccumulator = 0f;
            return;
        }

        walkSoundDistanceAccumulator += moveDistance;

        float stepDistance = Mathf.Max(0.05f, walkSoundDistance);
        while (walkSoundDistanceAccumulator >= stepDistance)
        {
            walkSoundDistanceAccumulator -= stepDistance;
            AudioClip walkSound = walkSounds[walkSoundIndex % walkSounds.Length];
            walkSoundIndex++;

            if (walkSound == null)
            {
                continue;
            }

            if (walkSoundSource != null)
            {
                walkSoundSource.PlayOneShot(walkSound, walkSoundVolume);
            }
            else
            {
                AudioManager.PlayOneShot(walkSound, walkSoundVolume);
            }
        }
    }

    void InteractTrigger(InteractableBase interactable)
    {
        if (interactable == null)
        {
            return;
        }

        Debug.Log($"PlayerControllerFR received InteractTrigger from {interactable.gameObject.name}");
        Debug.Log($"PlayerControllerFR found InteractableBase: {interactable.gameObject.name}");
        Debug.Log($"Interactable text: {interactable.interactText}");
        interactCollider.SetInteractText(interactable.interactText);
        lookingAtInteractable = true;
        currentInteractable = interactable;

        // (on Interact button): interactable.Interact();
    }
    void InteractLeaveTrigger(InteractableBase interactable)
    {
        if (interactable != null)
        {
            Debug.Log($"PlayerControllerFR received InteractLeaveTrigger from {interactable.gameObject.name}");
        }
        lookingAtInteractable = false;
        currentInteractable = null;
        interactCollider.SetInteractText("");
    }

    void AttackAction(InputAction.CallbackContext context)
    {
        DoInteractIfCan();
    }

    void InteractAction(InputAction.CallbackContext context)
    {
        DoInteractIfCan();
    }

    void DoInteractIfCan()
    {
        if (lookingAtInteractable && currentInteractable != null)
        {
            Debug.Log($"Interacting with {currentInteractable.gameObject.name}");
            if (currentInteractable.CanInteract())
            {
                currentInteractable.Interact();
            }
            return;
        }
        Debug.Log("Interact/Attack button pressed but no interactable in range");
    }

    public void OnTeleported(Quaternion targetRotation, bool applyRotation)
    {
        if (applyRotation)
        {
            // Apply only yaw (Y rotation) on teleport and keep current camera pitch.
            rotationY = targetRotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        }

        // Keep a small downward velocity so grounded logic settles immediately.
        verticalVelocity = -2f;
        jumpBufferTimer = 0f;
        coyoteTimer = coyoteTime;
        walkSoundDistanceAccumulator = 0f;

        if (hasFeathers && wingAnimationControl != null)
        {
            wingAnimationControl.ResetToIdle();
        }
    }

}
