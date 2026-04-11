using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FragmentConnection : MonoBehaviour
{
    [field: SerializeField] public FragmentConnection fragmentToConnectTo { get; private set; }
    public bool isConnected = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float maxMoveDistance = 20f;
    [SerializeField] private float snapDistance = 0.05f;

    private bool isMoving = false;
    private bool approachAlongX;
    private Vector3 originPosition;
    private Vector3 moveVelocity;
    private Collider ownCollider;
    private Collider targetCollider;

    private QuestComponent questComponent;

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();
    }
    private void Start()
    {
        questComponent = GetComponent<QuestComponent>();
        if (questComponent == null)
        {
            questComponent = gameObject.AddComponent<QuestComponent>();
        }
        questComponent.AddTaskGroup("FragmentConnectors", new UnityAction(callback), false, true);
        questComponent.AddTaskObject("FragmentConnectors", gameObject);
    }
    private static void callback()
    {
        Debug.Log("CallBACK");
    }

    private void Update()
    {
        if (!isMoving || isConnected || fragmentToConnectTo == null)
        {
            return;
        }

        if (Vector3.Distance(transform.position, originPosition) >= maxMoveDistance)
        {
            isMoving = false;
            return;
        }

        Vector3 destination = ComputeSideDestination();
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref moveVelocity, smoothTime, moveSpeed);

        if (Vector3.Distance(transform.position, destination) <= snapDistance)
        {
            SnapAndConnect();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isMoving || isConnected || fragmentToConnectTo == null)
        {
            return;
        }

        if (collision.gameObject == fragmentToConnectTo.gameObject)
        {
            SnapAndConnect();
        }
    }

    public void ConnectToFragment(FragmentConnection otherFragment)
    {
        if (otherFragment == null || isConnected)
        {
            return;
        }

        fragmentToConnectTo = otherFragment;
        targetCollider = fragmentToConnectTo.GetComponent<Collider>();
        originPosition = transform.position;
        moveVelocity = Vector3.zero;

        Bounds targetBounds = targetCollider != null
            ? targetCollider.bounds
            : new Bounds(fragmentToConnectTo.transform.position, Vector3.one);
        Vector3 myCenter = ownCollider != null ? ownCollider.bounds.center : transform.position;
        Vector3 toTarget = targetBounds.center - myCenter;
        approachAlongX = Mathf.Abs(toTarget.x) >= Mathf.Abs(toTarget.z);

        isMoving = true;
    }

    private void SnapAndConnect()
    {
        transform.position = ComputeSideDestination();
        moveVelocity = Vector3.zero;
        isMoving = false;
        isConnected = true;
        fragmentToConnectTo.isConnected = true;

        questComponent.RemoveTaskObject(gameObject);
        questComponent.RemoveTaskObject(fragmentToConnectTo.gameObject);
    }

    private Vector3 ComputeSideDestination()
    {
        Bounds targetBounds = targetCollider != null
            ? targetCollider.bounds
            : new Bounds(fragmentToConnectTo.transform.position, Vector3.one);
        Bounds myBounds = ownCollider != null
            ? ownCollider.bounds
            : new Bounds(transform.position, Vector3.one);

        Vector3 toTarget = targetBounds.center - myBounds.center;
        float destY = targetBounds.center.y;

        if (approachAlongX)
        {
            float sideX = toTarget.x > 0
                ? targetBounds.min.x - myBounds.extents.x
                : targetBounds.max.x + myBounds.extents.x;
            return new Vector3(sideX, destY, targetBounds.center.z);
        }
        else
        {
            float sideZ = toTarget.z > 0
                ? targetBounds.min.z - myBounds.extents.z
                : targetBounds.max.z + myBounds.extents.z;
            return new Vector3(targetBounds.center.x, destY, sideZ);
        }
    }
}
