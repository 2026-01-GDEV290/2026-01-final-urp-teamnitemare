using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerBSK : MonoBehaviour
{
    private InputSystem_Actions playerControls;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction attackAction;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 0.8f, 0f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float rotateSpeed = 10f;
    //[SerializeField] private float maxLookAngle = 85f;

    private CharacterController characterController;
    private float verticalVelocity;
    private float cameraPitch;

    private float rotationX;
    private float rotationY;

    private InteractCollider interactCollider;

    bool lookingAtInteractable = false;
    InteractableObject currentInteractable = null;

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
    }

    void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
        lookAction = playerControls.Player.Look;
        jumpAction = playerControls.Player.Jump;
        attackAction = playerControls.Player.Attack;
        attackAction.performed += AttackAction;

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
        if (attackAction != null)
        {
            attackAction.performed -= AttackAction;
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
    }

    void Update()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        bool jumpPressedThisFrame = jumpAction.WasPressedThisFrame();

        HandleLook(lookInput);
        HandleMovement(moveInput, jumpPressedThisFrame);
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

    void HandleMovement(Vector2 moveInput, bool jumpPressedThisFrame)
    {
        float inputX = moveInput.x;
        float inputZ = moveInput.y;

        Vector3 move = (transform.right * inputX + transform.forward * inputZ).normalized;
        characterController.Move(move * moveSpeed * Time.deltaTime);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (characterController.isGrounded && jumpPressedThisFrame)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void InteractTrigger(InteractableObject interactable)
    {
        if (interactable == null)
        {
            return;
        }

        Debug.Log($"PlayerControllerFR received InteractTrigger from {interactable.gameObject.name}");
        Debug.Log($"PlayerControllerFR found InteractableObject: {interactable.gameObject.name}");
        Debug.Log($"Interactable text: {interactable.interactText}");
        interactCollider.SetInteractText(interactable.interactText);
        lookingAtInteractable = true;
        currentInteractable = interactable;

        // (on Interact button): interactable.Interact();
    }
    void InteractLeaveTrigger(InteractableObject interactable)
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
        if (lookingAtInteractable && currentInteractable != null)
        {
            Debug.Log($"Interacting with {currentInteractable.gameObject.name}");
            currentInteractable.Interact();
            return;
        }

        Debug.Log("Attack button pressed");
    }

}
