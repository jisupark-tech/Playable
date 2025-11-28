using UnityEngine;

public class MovementComponent : MonoBehaviour, IMovable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    private Rigidbody rb;
    private Vector3 currentVelocity;
    private bool isMoving = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"MovementComponent requires Rigidbody on {gameObject.name}");
        }
    }

    public void Move(Vector3 direction)
    {
        if (rb == null || direction == Vector3.zero)
        {
            isMoving = false;
            currentVelocity = Vector3.zero;
            return;
        }

        direction = direction.normalized;
        Vector3 movement = direction * moveSpeed * Time.deltaTime;

        movement = GameManager.Instance.m_Player.GetValidPlayerPosition(transform.position, transform.position + movement);
        rb.MovePosition(/*transform.position +*/ movement);
        currentVelocity = direction * moveSpeed;
        isMoving = true;

        // 회전 처리
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
       
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    public Vector3 GetCurrentVelocity()
    {
        return currentVelocity;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    void FixedUpdate()
    {
        if (!isMoving)
        {
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
        }
    }
}