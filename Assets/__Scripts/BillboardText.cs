using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class BillboardText : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TextMeshPro worldText;
    [SerializeField] private string message = "Interact";
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool useFixedYRotation = false;
    [SerializeField] private float fixedYRotation = 0f;

    [Header("Trigger")]
    [SerializeField] private bool onlyShowForPlayer = true;
    [SerializeField] private string playerTag = "InteractCollider"; //"Player";

    private int validTargetsInside;
    private Transform targetCameraTransform;

    private void Awake()
    {
        //"InteractCollider" has a collider set to IsTrigger so this isn't a concern:
        //WarnIfColliderNotTrigger();
        EnsureTextObject();
        ApplyTextSettings();
        SetTextVisible(false);
    }

    private void LateUpdate()
    {
        if (worldText == null || !worldText.gameObject.activeSelf)
        {
            return;
        }

        Vector3 textPosition = transform.position + worldOffset;
        worldText.transform.position = textPosition;

        if (targetCameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                targetCameraTransform = mainCam.transform;
            }
        }

        if (useFixedYRotation)
        {
            worldText.transform.rotation = Quaternion.Euler(0f, fixedYRotation, 0f);
        }
        else if (targetCameraTransform != null)
        {
            Vector3 toCamera = targetCameraTransform.position - textPosition;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                worldText.transform.rotation = Quaternion.LookRotation(-toCamera, Vector3.up);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTarget(other))
        {
            return;
        }

        validTargetsInside++;
        ResolveCameraFromCollider(other);
        SetTextVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsValidTarget(other))
        {
            return;
        }

        validTargetsInside = Mathf.Max(0, validTargetsInside - 1);
        if (validTargetsInside == 0)
        {
            SetTextVisible(false);
        }
    }

    private bool IsValidTarget(Collider other)
    {
        if (!onlyShowForPlayer)
        {
            return true;
        }

        return other.CompareTag(playerTag);
    }

    private void ResolveCameraFromCollider(Collider other)
    {
        PlayerControllerFR playerController = other.GetComponentInParent<PlayerControllerFR>();
        if (playerController != null && playerController.playerCamera != null)
        {
            targetCameraTransform = playerController.playerCamera.transform;
            return;
        }

        Camera playerCamera = other.GetComponentInParent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = other.GetComponentInChildren<Camera>(true);
        }

        if (playerCamera != null)
        {
            targetCameraTransform = playerCamera.transform;
            return;
        }

        if (Camera.main != null)
        {
            targetCameraTransform = Camera.main.transform;
        }
    }

    private void SetTextVisible(bool isVisible)
    {
        if (worldText != null)
        {
            worldText.gameObject.SetActive(isVisible);
        }
    }

    private void WarnIfColliderNotTrigger()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"BillboardText on {gameObject.name} requires a trigger collider to receive OnTrigger events. Assign a dedicated trigger collider or enable Is Trigger in the Inspector.", this);
        }
    }

    private void EnsureTextObject()
    {
        if (worldText != null)
        {
            return;
        }

        worldText = GetComponentInChildren<TextMeshPro>(true);
        if (worldText != null)
        {
            return;
        }

        GameObject textObject = new GameObject("BillboardText");
        textObject.transform.SetParent(transform, false);
        worldText = textObject.AddComponent<TextMeshPro>();
        worldText.alignment = TextAlignmentOptions.Center;
        worldText.fontSize = 2.5f;
    }

    private void ApplyTextSettings()
    {
        if (worldText == null)
        {
            return;
        }

        worldText.text = message;
        worldText.transform.position = transform.position + worldOffset;
    }

    private void OnValidate()
    {
        ApplyTextSettings();
    }
}
