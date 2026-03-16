using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerBSK : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 0.8f, 0f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 150f;
    [SerializeField] private float maxLookAngle = 85f;

    private CharacterController characterController;
    private float verticalVelocity;
    private float cameraPitch;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();

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

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        if (playerCamera != null)
        {
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * inputX + transform.forward * inputZ).normalized;
        characterController.Move(move * moveSpeed * Time.deltaTime);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (characterController.isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

}
