using UnityEngine;

public class TeleportTarget : MonoBehaviour
{
    public void TeleportPlayerHere(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("TeleportTarget->TeleportPlayerHere: Player GameObject is null!");
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasControllerEnabled = controller != null && controller.enabled;

        if (wasControllerEnabled)
        {
            controller.enabled = false;
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = transform.position;
        }
        else
        {
            player.transform.position = transform.position;
        }

        if (wasControllerEnabled)
        {
            controller.enabled = true;
        }

        Physics.SyncTransforms();
    }
    public void TeleportDumbObjectHere(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogError("TeleportTarget->TeleportDumbObjectHere: Target GameObject is null!");
            return;
        }

        Rigidbody body = obj.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = transform.position;
        }
        else
        {
            obj.transform.position = transform.position;
        }

        Physics.SyncTransforms();
    }
}
