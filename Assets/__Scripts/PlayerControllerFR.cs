//using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerFR : MonoBehaviour
{

    InputSystem_Actions playerControls;
    InputAction moveAction, lookAction, hitAction, jumpAction;

    public float moveSpeed = 5f;
    public float rotateSpeed = 10f;
    public float jumpForce = 5f;
    public float gravity = -30f;
    public float grappleCheckDistance = 30f;
    public float grappleScreenCenterToleranceX = 0.22f;
    public float grappleScreenCenterToleranceY = 0.34f;
    public float grapplePullSpeed = 18f;
    public float grappleStopDistance = 0.6f;
    public float grappleSurfacePadding = 0.08f;
    public float grappleVerticalClearance = 0.8f;
    public bool snapToDestinationOnGrappleEnd = false;
    public bool uprightOnGrappleEnd = true;
    public bool pauseOnGrappleEnd = false;
    public float grappleLineWidth = 0.04f;
    public Vector3 grappleLineStartOffset = new Vector3(0f, 1.4f, 0.2f);
    public LayerMask grappleCheckMask = ~0;
    public Camera playerCamera;
    public LineRenderer grappleLineRenderer;

    private float rotationX;
    private float rotationY;
    private float verticalVelocity;
    private bool isGrappling;
    private Transform grappleTarget;
    private Collider grappleTargetCollider;
    private Collider grappleDestinationCollider;
    private Vector3 grappleAnchorPoint;
    private Vector3 grappleDestinationCenter;
    private float playerBottomToCenterOffset;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerControls = new InputSystem_Actions();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (grappleLineRenderer == null)
        {
            grappleLineRenderer = GetComponent<LineRenderer>();
            if (grappleLineRenderer == null)
            {
                grappleLineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        if (grappleLineRenderer != null)
        {
            ConfigureGrappleLineRenderer();
            grappleLineRenderer.positionCount = 2;
            grappleLineRenderer.enabled = false;
        }

        Vector3 currentCenter = transform.position + characterController.center;
        playerBottomToCenterOffset = currentCenter.y - GetPlayerBottomWorldY();

    }
    void OnEnable()
    {
        playerControls.Enable();
        moveAction = playerControls.Player.Move;
        lookAction = playerControls.Player.Look;
        hitAction = playerControls.Player.Attack;
        jumpAction = playerControls.Player.Jump;
        hitAction.performed += AttackAction;
        jumpAction.performed += JumpAction;
        //playerControls.Player.Jump.performed += JumpAction;
    }
    void OnDisable()
    {
        hitAction.performed -= AttackAction;
        jumpAction.performed -= JumpAction;
        //playerControls.Player.Jump.performed -= JumpAction;
        playerControls.Disable();
        StopGrapple(false);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (isGrappling)
        {
            UpdateGrapple();
            UpdateGrappleLine();
            return;
        }

        Vector2 moveVector = moveAction.ReadValue<Vector2>();
        //characterController.Move(moveVector);
        Move(moveVector);

        // Handle look input
        Vector2 lookVector = lookAction.ReadValue<Vector2>();
        //characterController.Rotate(lookVector);
        Rotate(lookVector);
    }

    void JumpAction(InputAction.CallbackContext context)
    {
        Debug.Log("Jump button pressed");
        if (characterController.isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }

    void AttackAction(InputAction.CallbackContext context)
    {
        Debug.Log("Attack button pressed");

        Camera activeCamera = playerCamera != null ? playerCamera : Camera.main;
        if (activeCamera == null)
        {
            Debug.LogWarning("No camera found for grapple targeting");
            return;
        }

        Vector3 origin = activeCamera.transform.position;
        Vector3 forward = activeCamera.transform.forward;

        // Prefer exact center hit first.
        if (Physics.Raycast(
            origin,
            forward,
            out RaycastHit centerHit,
            grappleCheckDistance,
            grappleCheckMask,
            QueryTriggerInteraction.Collide
        ))
        {
            if (centerHit.collider.CompareTag("GrapplePoint"))
            {
                Debug.Log($"Found GrapplePoint (center): {centerHit.collider.gameObject.name}");
                StartGrapple(centerHit.collider);
                return;
            }
        }

        // Fallback: choose visible GrapplePoints near the screen center, with LOS.
        Collider[] nearby = Physics.OverlapSphere(
            origin,
            grappleCheckDistance,
            grappleCheckMask,
            QueryTriggerInteraction.Collide
        );

        Collider bestTarget = null;
        float bestCenterScore = float.MaxValue;
        float bestDistance = float.MaxValue;

        foreach (Collider candidate in nearby)
        {
            if (!candidate.CompareTag("GrapplePoint"))
            {
                continue;
            }

            Vector3 targetPoint = candidate.bounds.center;
            Vector3 toTarget = targetPoint - origin;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= 0.01f)
            {
                continue;
            }

            Vector3 viewportPoint = activeCamera.WorldToViewportPoint(targetPoint);
            if (viewportPoint.z <= 0f)
            {
                continue;
            }

            if (viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
            {
                continue;
            }

            float centerOffsetX = Mathf.Abs(viewportPoint.x - 0.5f);
            float centerOffsetY = Mathf.Abs(viewportPoint.y - 0.5f);
            if (centerOffsetX > grappleScreenCenterToleranceX || centerOffsetY > grappleScreenCenterToleranceY)
            {
                continue;
            }

            float normalizedX = centerOffsetX / Mathf.Max(grappleScreenCenterToleranceX, 0.0001f);
            float normalizedY = centerOffsetY / Mathf.Max(grappleScreenCenterToleranceY, 0.0001f);
            float centerScore = (normalizedX * normalizedX) + (normalizedY * normalizedY);

            Debug.Log($"Candidate: {candidate.gameObject.name}, OffsetX: {centerOffsetX}, OffsetY: {centerOffsetY}, Distance: {distanceToTarget}");

            Vector3 directionToTarget = toTarget / distanceToTarget;

            if (!Physics.Raycast(
                origin,
                directionToTarget,
                out RaycastHit lineOfSightHit,
                distanceToTarget,
                grappleCheckMask,
                QueryTriggerInteraction.Collide
            ))
            {
                continue;
            }

            if (lineOfSightHit.collider != candidate)
            {
                continue;
            }

            if (centerScore < bestCenterScore || (Mathf.Approximately(centerScore, bestCenterScore) && distanceToTarget < bestDistance))
            {
                bestTarget = candidate;
                bestCenterScore = centerScore;
                bestDistance = distanceToTarget;
            }
        }

        if (bestTarget != null)
        {
            Debug.Log($"Found GrapplePoint (camera assist): {bestTarget.gameObject.name}");
            StartGrapple(bestTarget);
            return;
        }

        Debug.Log("No GrapplePoint found in line of sight");
    }

    void StartGrapple(Collider targetCollider)
    {
        if (targetCollider == null)
        {
            return;
        }

        grappleTargetCollider = targetCollider;
        grappleDestinationCollider = ResolveDestinationCollider(targetCollider);
        grappleTarget = targetCollider.transform;
        isGrappling = true;
        verticalVelocity = 0f;
        Vector3 playerCenter = transform.position + characterController.center;
        playerBottomToCenterOffset = playerCenter.y - GetPlayerBottomWorldY();
        grappleAnchorPoint = grappleTarget.position;
        grappleDestinationCenter = GetCurrentGrappleDestinationCenter(playerCenter);

        if (grappleLineRenderer != null)
        {
            grappleLineRenderer.enabled = true;
            grappleLineRenderer.positionCount = 2;
            UpdateGrappleLine();
        }
    }

    void UpdateGrapple()
    {
        if (grappleTarget == null || grappleTargetCollider == null)
        {
            StopGrapple(true);
            return;
        }

        Vector3 playerCenter = transform.position + characterController.center;
        Vector3 toTarget = grappleDestinationCenter - playerCenter;
        float distance = toTarget.magnitude;
        if (distance <= grappleStopDistance)
        {
            if (snapToDestinationOnGrappleEnd)
            {
                CompleteGrappleAtDestination();
            }
            StopGrapple(true);
            return;
        }

        float step = grapplePullSpeed * Time.deltaTime;
        Vector3 nextCenter = Vector3.MoveTowards(playerCenter, grappleDestinationCenter, step);
        SetPlayerCenterPosition(nextCenter);
    }

    void UpdateGrappleLine()
    {
        if (!isGrappling || grappleTarget == null || grappleLineRenderer == null)
        {
            return;
        }

        // Keep width synced with inspector value while testing/tuning.
        grappleLineRenderer.widthMultiplier = 1f;
        grappleLineRenderer.startWidth = grappleLineWidth;
        grappleLineRenderer.endWidth = grappleLineWidth;

        Vector3 lineStart = GetGrappleLineStartPoint();
        Vector3 lineEnd = grappleTarget != null ? grappleTarget.position : grappleAnchorPoint;
        grappleLineRenderer.SetPosition(0, lineStart);
        grappleLineRenderer.SetPosition(1, lineEnd);
    }

    void StopGrapple(bool shouldPause)
    {
        bool wasGrappling = isGrappling;
        isGrappling = false;
        grappleTarget = null;
        grappleTargetCollider = null;
        grappleDestinationCollider = null;

        if (wasGrappling && uprightOnGrappleEnd)
        {
            SnapPlayerUpright();
        }

        if (grappleLineRenderer != null)
        {
            grappleLineRenderer.enabled = false;
        }

        if (shouldPause && pauseOnGrappleEnd)
        {
            Debug.Break();
        }
    }

    void CompleteGrappleAtDestination()
    {
        if (characterController == null)
        {
            return;
        }

        SetPlayerCenterPosition(grappleDestinationCenter);
    }

    void SnapPlayerUpright()
    {
        rotationY = transform.eulerAngles.y;
        rotationX = 0f;
        transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
    }

    void SetPlayerCenterPosition(Vector3 worldCenterPosition)
    {
        Vector3 finalPosition = worldCenterPosition - characterController.center;
        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.position = finalPosition;
        characterController.enabled = wasEnabled;
    }

    Vector3 GetCurrentGrappleAnchor(Vector3 fromPoint)
    {
        if (grappleTargetCollider == null)
        {
            return grappleTarget != null ? grappleTarget.position : fromPoint;
        }

        return grappleTargetCollider.ClosestPoint(fromPoint);
    }

    Vector3 GetCurrentGrappleDestinationCenter(Vector3 fromPoint)
    {
        Collider destinationCollider = grappleDestinationCollider != null ? grappleDestinationCollider : grappleTargetCollider;
        if (destinationCollider == null)
        {
            return GetCurrentGrappleAnchor(fromPoint);
        }

        Vector3 referencePoint = grappleTarget != null ? grappleTarget.position : fromPoint;
        Vector3 surfacePoint = destinationCollider.ClosestPoint(referencePoint);
        Vector3 toPlayer = fromPoint - surfacePoint;
        Vector3 horizontalDirection = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (horizontalDirection.sqrMagnitude < 0.0001f)
        {
            Vector3 outward = surfacePoint - destinationCollider.bounds.center;
            horizontalDirection = new Vector3(outward.x, 0f, outward.z);
        }

        if (horizontalDirection.sqrMagnitude < 0.0001f)
        {
            horizontalDirection = -Vector3.forward;
        }

        float horizontalClearance = Mathf.Max(grappleSurfacePadding, characterController.radius + characterController.skinWidth);
        Vector3 horizontalOffset = horizontalDirection.normalized * horizontalClearance;
        Vector3 destination = surfacePoint + horizontalOffset;

        float targetBottomY = destinationCollider.bounds.min.y;
        float desiredCenterY = targetBottomY + playerBottomToCenterOffset + grappleVerticalClearance;
        destination.y = desiredCenterY;
        return destination;
    }

    float GetPlayerBottomWorldY()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float minY = float.MaxValue;
        bool foundRenderer = false;

        foreach (Renderer rendererComponent in renderers)
        {
            if (!rendererComponent.enabled || rendererComponent is LineRenderer)
            {
                continue;
            }

            foundRenderer = true;
            if (rendererComponent.bounds.min.y < minY)
            {
                minY = rendererComponent.bounds.min.y;
            }
        }

        if (foundRenderer)
        {
            return minY;
        }

        return characterController.bounds.min.y;
    }

    Collider ResolveDestinationCollider(Collider grappleHitCollider)
    {
        if (grappleHitCollider == null)
        {
            return null;
        }

        Transform current = grappleHitCollider.transform;
        while (current != null)
        {
            Collider[] colliders = current.GetComponents<Collider>();
            foreach (Collider candidate in colliders)
            {
                if (!candidate.enabled || candidate.isTrigger)
                {
                    continue;
                }

                if (candidate == grappleHitCollider)
                {
                    continue;
                }

                return candidate;
            }

            current = current.parent;
        }

        return grappleHitCollider;
    }

    Vector3 GetGrappleLineStartPoint()
    {
        Vector3 basePoint = characterController.bounds.min;
        Vector3 horizontalOffset = (transform.right * grappleLineStartOffset.x) + (transform.forward * grappleLineStartOffset.z);
        return basePoint + Vector3.up * grappleLineStartOffset.y + horizontalOffset;
    }

    void ConfigureGrappleLineRenderer()
    {
        grappleLineRenderer.useWorldSpace = true;
        grappleLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        grappleLineRenderer.receiveShadows = false;
        grappleLineRenderer.textureMode = LineTextureMode.Stretch;
        grappleLineRenderer.numCapVertices = 6;
        grappleLineRenderer.widthMultiplier = 1f;
        grappleLineRenderer.startWidth = grappleLineWidth;
        grappleLineRenderer.endWidth = grappleLineWidth;

        if (grappleLineRenderer.sharedMaterial == null)
        {
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                grappleLineRenderer.sharedMaterial = new Material(lineShader);
            }
        }

        if (grappleLineRenderer.startColor.a <= 0f && grappleLineRenderer.endColor.a <= 0f)
        {
            grappleLineRenderer.startColor = Color.white;
            grappleLineRenderer.endColor = Color.white;
        }
    }


    public void Move(Vector2 moveVector)
    {
        Vector3 move = transform.forward * moveVector.y + transform.right * moveVector.x;
        move = move * moveSpeed * Time.deltaTime;
        characterController.Move(move);

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    public void Rotate(Vector2 lookVector)
    {
        // x-axis of mouse controls pitch (looking up/down)
        rotationY += lookVector.x * rotateSpeed * Time.deltaTime;
        // make sure to clamp the x rotation to prevent flipping over
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);        
        rotationX -= lookVector.y * rotateSpeed * Time.deltaTime;
        //rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

}
