using UnityEngine;
using UnityEngine.InputSystem;

public class SausageMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float laneChangeSpeed = 6f;

    [Header("X Limits")]
    [SerializeField] private float minX = -6f;
    [SerializeField] private float maxX = 6f;

    [Header("Lane Positions")]
    [SerializeField] private Transform[] lanePoints;
    [SerializeField] private int startLaneIndex = 1;

    [Header("Window Resistance")]
    [SerializeField] private AnimationCurve forwardResistanceCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
    [SerializeField] private float chainBonus = 0f;
    [SerializeField] private float maxChainBonus = 0.8f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    private InputAction moveAction;
    private Vector2 moveInput;
    private int currentLaneIndex;
    private float targetLaneZ;
    private bool laneInputLocked;

    private void Awake()
    {
        currentLaneIndex = GetValidLaneIndex(startLaneIndex);
        targetLaneZ = GetLanePosition(currentLaneIndex);
    }

    private void OnEnable()
    {
        moveAction = InputSystem.actions.FindAction("Move");

        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateLaneTarget();
        MoveHorizontally();
        MoveToLane();
    }

    private void ReadInput()
    {
        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private void UpdateLaneTarget()
    {
        if (lanePoints == null || lanePoints.Length == 0)
        {
            return;
        }

        if (Mathf.Abs(moveInput.y) < 0.5f)
        {
            laneInputLocked = false;
            return;
        }

        if (laneInputLocked)
        {
            return;
        }

        laneInputLocked = true;

        if (moveInput.y > 0.5f)
        {
            SetLane(currentLaneIndex + 1);
        }
        else if (moveInput.y < -0.5f)
        {
            SetLane(currentLaneIndex - 1);
        }
    }

    private void SetLane(int newLaneIndex)
    {
        int clampedLaneIndex = GetValidLaneIndex(newLaneIndex);

        if (clampedLaneIndex == currentLaneIndex)
        {
            return;
        }

        currentLaneIndex = clampedLaneIndex;
        targetLaneZ = GetLanePosition(currentLaneIndex);
    }

    private void MoveHorizontally()
    {
        float resistance = GetForwardMovementMultiplier();
        float xDelta = moveInput.x * moveSpeed * resistance * Time.deltaTime;
        float targetX = Mathf.Clamp(transform.position.x + xDelta, minX, maxX);

        Vector3 position = transform.position;
        position.x = targetX;
        transform.position = position;
    }

    private void MoveToLane()
    {
        Vector3 position = transform.position;
        position.z = Mathf.MoveTowards(position.z, targetLaneZ, laneChangeSpeed * Time.deltaTime);
        transform.position = position;
    }

    private float GetForwardMovementMultiplier()
    {
        float normalizedX = Mathf.InverseLerp(minX, maxX, transform.position.x);
        float baseResistance = forwardResistanceCurve.Evaluate(normalizedX);
        float bonus = Mathf.Clamp(chainBonus, 0f, maxChainBonus);

        return Mathf.Clamp(baseResistance + bonus, 0.05f, 1f);
    }

    private int GetValidLaneIndex(int laneIndex)
    {
        if (lanePoints == null || lanePoints.Length == 0)
        {
            return 0;
        }

        return Mathf.Clamp(laneIndex, 0, lanePoints.Length - 1);
    }

    private float GetLanePosition(int laneIndex)
    {
        if (lanePoints == null || lanePoints.Length == 0)
        {
            return transform.position.z;
        }

        Transform lanePoint = lanePoints[GetValidLaneIndex(laneIndex)];
        return lanePoint != null ? lanePoint.position.z : transform.position.z;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Vector3 from = new Vector3(minX, transform.position.y, transform.position.z);
        Vector3 to = new Vector3(maxX, transform.position.y, transform.position.z);
        Gizmos.DrawLine(from, to);

        if (lanePoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        foreach (Transform lanePoint in lanePoints)
        {
            if (lanePoint == null)
            {
                continue;
            }

            float laneZ = lanePoint.position.z;
            Vector3 laneStart = new Vector3(minX, transform.position.y, laneZ);
            Vector3 laneEnd = new Vector3(maxX, transform.position.y, laneZ);
            Gizmos.DrawLine(laneStart, laneEnd);
        }
    }

    public void SetChainBonus(float bonus)
    {
        chainBonus = Mathf.Clamp(bonus, 0f, maxChainBonus);
    }
}
