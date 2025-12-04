using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    [Header("NPC Settings")]
    public float moveSpeed = 3f;
    public float ReachDistance = 0.2f;

    [Header("Pathfinding Settings")]
    public float avoidanceRange = 2.5f; // Distance to detect and avoid buildings
    public float pathUpdateRate = 0.2f; // How often to recalculate path
    public float obstacleCheckDistance = 1.5f; // Forward obstacle detection distance
    public LayerMask buildingLayer = -1; // Layer mask for buildings to avoid
    public float stuckThreshold = 0.1f; // Distance threshold to consider NPC as stuck
    public float stuckTimeLimit = 2f; // Time limit before trying alternative path

    [Header("Safety Settings")]
    public float maxMovementTime = 60f; // 최대 이동 시간 (1분) - 무한루프 방지
    public float maxDistanceFromStart = 50f; // 시작점으로부터 최대 거리

    [Header("Animation")]
    [SerializeField] private string MoveParameter;

    private IHealth m_targetController;
    private Transform m_targetTransform;
    private Animator anim;

    // Pathfinding variables
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private bool isMovingToTarget = false;
    private Vector3 currentWaypoint;
    private List<Vector3> avoidancePoints = new List<Vector3>();

    // Safety variables
    private float movementStartTime = 0f;
    private Vector3 startPosition = Vector3.zero;
    private bool isTimeoutReached = false;

    public void Init(Transform _controller = null)
    {
        if (_controller != null)
        {
            m_targetTransform = _controller;
            m_targetController = _controller.gameObject.GetComponent<IHealth>();

            if (anim == null)
                anim = GetComponent<Animator>();
            if (anim == null)
                anim = GetComponentInChildren<Animator>();

            // Initialize pathfinding
            lastPosition = transform.position;
            startPosition = transform.position; // 시작 위치 저장
            currentWaypoint = m_targetTransform.position;
            isMovingToTarget = true;
            movementStartTime = Time.time; // 이동 시작 시간 기록
            isTimeoutReached = false;

            StartCoroutine(MoveToControllerWithAvoidance());
        }
        else
        {
            Debug.LogError("NPCController: Target controller is null!");
            // 타겟이 없으면 즉시 풀에 반환
            ObjectPool.Instance.ReturnToPool(this.gameObject);
        }
    }

    IEnumerator MoveToControllerWithAvoidance()
    {
        if (m_targetController == null || m_targetTransform == null)
        {
            Debug.LogError("NPCController: Target is null, returning to pool");
            ObjectPool.Instance.ReturnToPool(this.gameObject);
            yield break;
        }

        while (isMovingToTarget && !isTimeoutReached && gameObject.activeInHierarchy)
        {
            // 시간 제한 체크 (무한 루프 방지)
            if (Time.time - movementStartTime > maxMovementTime)
            {
                Debug.LogWarning($"NPC {gameObject.name} movement timeout reached. Forcing completion.");
                isTimeoutReached = true;
                ForceReachTarget();
                yield break;
            }

            // 시작점으로부터 너무 멀어졌는지 체크
            float distanceFromStart = Vector3.Distance(transform.position, startPosition);
            if (distanceFromStart > maxDistanceFromStart)
            {
                Debug.LogWarning($"NPC {gameObject.name} moved too far from start ({distanceFromStart:F1}). Teleporting closer to target.");
                TeleportToSafePosition();
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // 타겟이 여전히 유효한지 체크
            if (m_targetTransform == null || !m_targetTransform.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"NPC {gameObject.name} target became invalid during movement");
                ObjectPool.Instance.ReturnToPool(this.gameObject);
                yield break;
            }

            float distanceToTarget = Vector3.Distance(transform.position, m_targetTransform.position);

            // Check if reached target
            if (distanceToTarget <= ReachDistance)
            {
                ReachTarget();
                yield break;
            }

            // Update animation
            UpdateAnimation(true);

            // Check for obstacles and calculate movement
            Vector3 moveDirection = CalculateMovementDirection();

            // Move the NPC
            if (moveDirection != Vector3.zero)
            {
                Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;

                // 안전한 위치인지 체크
                if (IsSafePosition(newPosition))
                {
                    transform.position = newPosition;

                    // Rotate towards movement direction
                    if (moveDirection.magnitude > 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                    }
                }
                else
                {
                    // 안전하지 않은 위치면 대체 경로 찾기
                    HandleUnsafePosition();
                }
            }

            // Check if stuck and handle accordingly
            CheckIfStuckAndHandle();

            // Update path periodically
            if (Time.fixedTime % pathUpdateRate < Time.fixedDeltaTime)
            {
                UpdateCurrentWaypoint();
            }

            yield return null;
        }

        // 루프가 끝났는데 타겟에 도달하지 못한 경우
        if (!isTimeoutReached)
        {
            Debug.LogWarning($"NPC {gameObject.name} exited movement loop without reaching target");
            ObjectPool.Instance.ReturnToPool(this.gameObject);
        }
    }

    /// <summary>
    /// 위치가 안전한지 체크 (맵 경계, 건물 충돌 등)
    /// </summary>
    bool IsSafePosition(Vector3 position)
    {
        // 맵 경계 체크 (간단한 범위 제한)
        if (Mathf.Abs(position.x) > 100f || Mathf.Abs(position.z) > 100f)
        {
            return false;
        }

        // 건물 충돌 체크
        if (GameManager.Instance != null)
        {
            return !GameManager.Instance.IsPositionCollidingWithBuilding(position, transform);
        }

        return true;
    }

    /// <summary>
    /// 안전하지 않은 위치에 도달했을 때 처리
    /// </summary>
    void HandleUnsafePosition()
    {
        // 이전 위치로 되돌리기
        if (lastPosition != Vector3.zero)
        {
            transform.position = lastPosition;
        }

        // 스턱 타이머 증가 (강제 탈출 유도)
        stuckTimer += Time.deltaTime * 2f; // 2배 빠르게 증가
    }

    /// <summary>
    /// 안전한 위치로 텔레포트
    /// </summary>
    void TeleportToSafePosition()
    {
        if (m_targetTransform == null) return;

        // 타겟 근처의 안전한 위치로 이동
        Vector3 directionToTarget = (m_targetTransform.position - transform.position).normalized;
        Vector3 safePosition = m_targetTransform.position - directionToTarget * 3f; // 타겟에서 3유닛 떨어진 곳

        // 안전한 위치인지 체크하고 이동
        if (IsSafePosition(safePosition))
        {
            transform.position = safePosition;
            lastPosition = safePosition;
            stuckTimer = 0f;
            Debug.Log($"NPC {gameObject.name} teleported to safe position near target");
        }
    }

    Vector3 CalculateMovementDirection()
    {
        if (m_targetTransform == null) return Vector3.zero;

        Vector3 directionToTarget = (currentWaypoint - transform.position).normalized;
        Vector3 finalDirection = directionToTarget;

        // Check for obstacles in front
        if (IsObstacleInDirection(directionToTarget))
        {
            // Calculate avoidance direction
            Vector3 avoidanceDirection = CalculateAvoidanceDirection();

            if (avoidanceDirection != Vector3.zero)
            {
                // Blend target direction with avoidance direction
                finalDirection = (directionToTarget * 0.3f + avoidanceDirection * 0.7f).normalized;
            }
            else
            {
                // If no clear avoidance direction, try alternative pathfinding
                finalDirection = FindAlternativePath();
            }
        }

        return finalDirection;
    }

    bool IsObstacleInDirection(Vector3 direction)
    {
        // Raycast forward to detect obstacles
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f; // Raise ray to avoid ground collision

        if (Physics.Raycast(rayStart, direction, out hit, obstacleCheckDistance, buildingLayer))
        {
            // Check if hit object is a building/obstacle
            if (IsBuilding(hit.collider.gameObject))
            {
                return true;
            }
        }

        // Also check with sphere cast for better collision detection
        if (Physics.SphereCast(rayStart, 0.3f, direction, out hit, obstacleCheckDistance, buildingLayer))
        {
            if (IsBuilding(hit.collider.gameObject))
            {
                return true;
            }
        }

        return false;
    }

    Vector3 CalculateAvoidanceDirection()
    {
        Vector3 avoidanceDirection = Vector3.zero;
        Vector3 npcPosition = transform.position;

        // Find all nearby buildings/obstacles
        Collider[] nearbyObstacles = Physics.OverlapSphere(npcPosition, avoidanceRange, buildingLayer);

        foreach (Collider obstacle in nearbyObstacles)
        {
            if (IsBuilding(obstacle.gameObject))
            {
                Vector3 obstaclePosition = obstacle.bounds.center;
                Vector3 directionFromObstacle = (npcPosition - obstaclePosition).normalized;
                float distance = Vector3.Distance(npcPosition, obstaclePosition);

                // Calculate avoidance force (stronger when closer)
                float avoidanceStrength = 1f - (distance / avoidanceRange);
                avoidanceDirection += directionFromObstacle * avoidanceStrength;
            }
        }

        // Normalize and ensure Y is 0 (ground movement only)
        if (avoidanceDirection != Vector3.zero)
        {
            avoidanceDirection.y = 0;
            avoidanceDirection = avoidanceDirection.normalized;
        }

        return avoidanceDirection;
    }

    Vector3 FindAlternativePath()
    {
        if (m_targetTransform == null) return Vector3.zero;

        // Try multiple directions to find a clear path
        Vector3[] testDirections = {
            Quaternion.Euler(0, 45, 0) * (m_targetTransform.position - transform.position).normalized,
            Quaternion.Euler(0, -45, 0) * (m_targetTransform.position - transform.position).normalized,
            Quaternion.Euler(0, 90, 0) * (m_targetTransform.position - transform.position).normalized,
            Quaternion.Euler(0, -90, 0) * (m_targetTransform.position - transform.position).normalized,
            Quaternion.Euler(0, 135, 0) * (m_targetTransform.position - transform.position).normalized,
            Quaternion.Euler(0, -135, 0) * (m_targetTransform.position - transform.position).normalized
        };

        foreach (Vector3 testDirection in testDirections)
        {
            if (!IsObstacleInDirection(testDirection))
            {
                return testDirection;
            }
        }

        // If no clear direction found, move backwards slightly
        return -transform.forward;
    }

    void UpdateCurrentWaypoint()
    {
        if (m_targetTransform == null) return;

        // Simple waypoint update - could be enhanced with more sophisticated pathfinding
        currentWaypoint = m_targetTransform.position;

        // Check if there's a direct path to target
        if (!IsObstacleInDirection((currentWaypoint - transform.position).normalized))
        {
            // Clear path to target
            currentWaypoint = m_targetTransform.position;
        }
        else
        {
            // Find intermediate waypoint to avoid obstacles
            Vector3 intermediatePoint = FindIntermediateWaypoint();
            if (intermediatePoint != Vector3.zero)
            {
                currentWaypoint = intermediatePoint;
            }
        }
    }

    Vector3 FindIntermediateWaypoint()
    {
        if (m_targetTransform == null) return Vector3.zero;

        // Create intermediate waypoint to navigate around obstacles
        Vector3 toTarget = (m_targetTransform.position - transform.position);
        Vector3 rightDirection = Vector3.Cross(toTarget.normalized, Vector3.up).normalized;

        // Try waypoints to the right and left
        Vector3[] candidatePoints = {
            transform.position + rightDirection * avoidanceRange + toTarget.normalized * 2f,
            transform.position - rightDirection * avoidanceRange + toTarget.normalized * 2f,
            transform.position + toTarget.normalized * avoidanceRange
        };

        foreach (Vector3 candidate in candidatePoints)
        {
            if (!IsObstacleInDirection((candidate - transform.position).normalized))
            {
                return candidate;
            }
        }

        return Vector3.zero;
    }

    void CheckIfStuckAndHandle()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckTimeLimit)
            {
                // NPC is stuck, try to teleport slightly or find alternative route
                HandleStuckSituation();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }
    }

    void HandleStuckSituation()
    {
        Debug.Log($"NPC {gameObject.name} is stuck, attempting to resolve...");

        // Try to move the NPC to a nearby clear position
        Vector3[] escapeDirections = {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
            (Vector3.right + Vector3.forward).normalized,
            (Vector3.right + Vector3.back).normalized,
            (Vector3.left + Vector3.forward).normalized,
            (Vector3.left + Vector3.back).normalized
        };

        foreach (Vector3 direction in escapeDirections)
        {
            Vector3 escapePosition = transform.position + direction * 2f;

            if (IsSafePosition(escapePosition) && !Physics.CheckSphere(escapePosition, 0.5f, buildingLayer))
            {
                transform.position = escapePosition;
                lastPosition = escapePosition;
                Debug.Log($"NPC {gameObject.name} escaped from stuck position");
                return;
            }
        }

        // If still can't escape, teleport closer to target (last resort)
        if (m_targetTransform != null)
        {
            Vector3 emergencyPosition = Vector3.Lerp(transform.position, m_targetTransform.position, 0.5f);
            if (IsSafePosition(emergencyPosition) && !Physics.CheckSphere(emergencyPosition, 0.5f, buildingLayer))
            {
                transform.position = emergencyPosition;
                lastPosition = emergencyPosition;
                Debug.Log($"NPC {gameObject.name} used emergency teleport");
                return;
            }
        }

        // 최후의 수단: 강제로 타겟 도달 처리
        Debug.LogWarning($"NPC {gameObject.name} could not escape stuck position, forcing completion");
        ForceReachTarget();
    }

    bool IsBuilding(GameObject obj)
    {
        // Check if object is a building that should be avoided
        return obj.GetComponent<TurretController>() != null ||
               obj.GetComponent<WallController>() != null ||
               obj.GetComponent<EnhanceController>() != null ||
               obj.CompareTag("Building") ||
               obj.CompareTag("Obstacle");
    }

    void UpdateAnimation(bool isMoving)
    {
        if (anim != null && !string.IsNullOrEmpty(MoveParameter))
        {
            anim.SetBool(MoveParameter, isMoving);
        }
    }

    void ReachTarget()
    {
        // Stop movement
        isMovingToTarget = false;
        UpdateAnimation(false);

        // Check if target is a TurretController and enhance it
        if (m_targetTransform != null)
        {
            TurretController turret = m_targetTransform.GetComponent<TurretController>();
            if (turret != null && turret.IsBuilt() && !turret.IsUpgraded())
            {
                // 터렛 업그레이드 실행
                turret.UpgradeTurret();
                Debug.Log($"NPC {gameObject.name} reached turret {turret.name} and upgraded it to multi-shot!");
            }
            else if (turret != null)
            {
                Debug.Log($"NPC {gameObject.name} reached turret {turret.name} but it's either not built or already upgraded.");
            }
            else
            {
                Debug.LogWarning($"NPC {gameObject.name} reached target but it's not a valid turret");
            }
        }

        // Return to pool
        ObjectPool.Instance.ReturnToPool(this.gameObject);
    }

    /// <summary>
    /// 강제로 타겟에 도달한 것으로 처리
    /// </summary>
    void ForceReachTarget()
    {
        Debug.Log($"NPC {gameObject.name} force-reached target due to timeout or stuck situation");
        ReachTarget();
    }

    // Public methods for external control
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0.1f, newSpeed);
    }

    public void SetAvoidanceRange(float newRange)
    {
        avoidanceRange = Mathf.Max(0.5f, newRange);
    }

    public bool IsMoving()
    {
        return isMovingToTarget;
    }

    public Transform GetTarget()
    {
        return m_targetTransform;
    }

    /// <summary>
    /// 강제 종료 (외부에서 호출 가능)
    /// </summary>
    public void ForceStop()
    {
        isMovingToTarget = false;
        isTimeoutReached = true;
        StopAllCoroutines();
        ObjectPool.Instance.ReturnToPool(this.gameObject);
        Debug.Log($"NPC {gameObject.name} was force-stopped");
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw avoidance range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidanceRange);

        // Draw obstacle check distance
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * obstacleCheckDistance);

        // Draw path to target
        if (m_targetTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, m_targetTransform.position);

            // Draw current waypoint
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(currentWaypoint, 0.5f);
        }

        // Draw start position
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(startPosition, 1f);
    }

    void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이동 중지
        isMovingToTarget = false;
    }

    void OnDisable()
    {
        // 오브젝트가 비활성화될 때 이동 중지
        isMovingToTarget = false;
    }
}