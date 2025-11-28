using System.Collections;
using UnityEngine;

public class CombatComponent : MonoBehaviour, ICombat
{
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private bool autoTarget = true;
    [SerializeField] private float targetUpdateRate = 0.2f;
    [SerializeField] private bool turnOnRotation = false;

    [Header("WebGL Optimization")]
    [SerializeField] private bool usePhysicsDetection = false; // WebGL에서는 false로 설정
    [SerializeField] private float maxTargetSearchDistance = 12f; // 최대 타겟 검색 거리

    private IWeapon currentWeapon;
    private Transform currentTarget;
    private WeaponComponent weaponComponent;
    private bool isInitialized = false;

    // 자체적인 쿨다운 관리
    private float lastAttackTime = 0f;
    private float attackCooldown = 1f;

    void Awake()
    {
        weaponComponent = GetComponent<WeaponComponent>();
        if (weaponComponent == null)
        {
            weaponComponent = gameObject.AddComponent<WeaponComponent>();
        }
        SetWeapon(weaponComponent);
    }

    public void Initialize()
    {
        if (isInitialized) return;

        if (autoTarget)
        {
            StartCoroutine(AutoTargeting());
        }

        // 쿨다운 기반 공격 코루틴
        StartCoroutine(AttackCooldownCoroutine());

        isInitialized = true;
    }

    // 쿨다운 기반 공격 시스템
    IEnumerator AttackCooldownCoroutine()
    {
        while (true)
        {
            if (currentTarget != null && IsValidTarget(currentTarget))
            {
                Attack(currentTarget);

                // 공격 후 쿨다운만큼 대기
                yield return new WaitForSeconds(targetUpdateRate);
            }
            else
            {
                // 타겟이 없으면 짧게 대기 후 다시 체크
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    bool IsValidTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange)
            return false;

        // 안전한 IHealth 체크
        IHealth health = target.GetComponent<IHealth>();
        if (health != null && health.IsDead())
            return false;

        return true;
    }

    public void Attack(Transform target)
    {
        if (currentWeapon == null || target == null) return;

        Vector3 firePosition = weaponComponent.GetFirePoint().position;

        // 타겟을 향해 회전
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            if (turnOnRotation)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        // 무기 발사 (쿨다운 체크 없이 강제 발사)
        BulletOwner owner = GetBulletOwner();
        ForceFireWeapon(firePosition, target, owner);
    }

    // 쿨다운 무시하고 강제 발사
    void ForceFireWeapon(Vector3 startPosition, Transform target, BulletOwner owner)
    {
        string bulletType = weaponComponent.GetBulletType();
        GameObject bullet = ObjectPool.Instance.SpawnFromPool(bulletType, startPosition, weaponComponent.GetFirePoint().rotation);

        if (bullet != null)
        {
            BulletController bulletController = bullet.GetComponent<BulletController>();
            if (bulletController != null)
            {
                if (owner == BulletOwner.Turret && GetComponent<TurretController>() != null)
                {
                    bulletController.SetTarget(target, owner, GetComponent<TurretController>());
                }
                else
                {
                    bulletController.SetTarget(target, owner);
                }
                AudioManager.Instance.PlayArrowAttackSound();
            }
        }
        else
        {
            // 오브젝트 풀에서 총알을 가져오지 못한 경우 디버그
            Debug.LogWarning($"Failed to spawn bullet: {bulletType} from ObjectPool");
        }
    }

    public void SetWeapon(IWeapon weapon)
    {
        currentWeapon = weapon;
        if (weapon != null)
        {
            attackRange = weapon.GetRange();
            attackCooldown = weapon.GetFireRate(); // 쿨다운 시간 설정
        }
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }

    public bool CanAttack()
    {
        return currentTarget != null && Time.time >= lastAttackTime + attackCooldown;
    }

    public float GetAttackRange()
    {
        return attackRange;
    }

    public void SetAttackRange(float range)
    {
        attackRange = Mathf.Max(0f, range);
    }

    IEnumerator AutoTargeting()
    {
        while (true)
        {
            if (autoTarget)
            {
                FindNearestTargetWebGL();
            }
            yield return new WaitForSeconds(targetUpdateRate);
        }
    }

    /// <summary>
    /// WebGL 최적화된 타겟 찾기 (물리체 없이 거리 기반)
    /// </summary>
    void FindNearestTargetWebGL()
    {
        if (usePhysicsDetection)
        {
            // 레거시 물리 기반 방식 (WebGL에서는 사용하지 않음)
            FindNearestTargetPhysics();
            return;
        }

        // WebGL 최적화: 거리 계산 기반
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;

        // 모든 적들을 거리 기반으로 검사
        EnemyController[] allEnemies = FindObjectsOfType<EnemyController>();

        foreach (EnemyController enemy in allEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            // 최대 검색 거리로 먼저 필터링 (성능 최적화)
            float roughDistance = Vector3.Distance(transform.position, enemy.transform.position);
            if (roughDistance > maxTargetSearchDistance) continue;

            // 사거리 내에 있는지 정확히 체크
            if (roughDistance <= attackRange)
            {
                // IHealth 체크 (안전하게)
                IHealth health = enemy.GetComponent<IHealth>();
                if (health == null || !health.IsDead())
                {
                    if (roughDistance < nearestDistance)
                    {
                        nearestDistance = roughDistance;
                        nearestTarget = enemy.transform;
                    }
                }
            }
        }

        // 이전 타겟과 다른 경우에만 로그 출력
        if (currentTarget != nearestTarget)
        {
            currentTarget = nearestTarget;

            if (currentTarget != null)
            {
                Debug.Log($"{gameObject.name} found new target: {currentTarget.name} (Distance: {nearestDistance:F1})");
            }
        }
    }

    /// <summary>
    /// 레거시 물리 기반 타겟 찾기 (WebGL에서는 사용하지 않음)
    /// </summary>
    void FindNearestTargetPhysics()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRange, targetLayer);

        if (targets.Length == 0)
        {
            currentTarget = null;
            return;
        }

        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider target in targets)
        {
            // 기본 유효성 체크
            if (!target.gameObject.activeInHierarchy) continue;

            // 안전한 IHealth 컴포넌트 체크
            IHealth health = target.GetComponent<IHealth>();
            if (health != null && health.IsDead())
            {
                continue; // 죽은 적은 타겟에서 제외
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target.transform;
            }
        }

        currentTarget = nearestTarget;
    }

    private BulletOwner GetBulletOwner()
    {
        if (GetComponent<PlayerController>() != null)
            return BulletOwner.Player;
        else if (GetComponent<TurretController>() != null)
            return BulletOwner.Turret;
        else
            return BulletOwner.Player;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // WebGL 모드에서 최대 검색 거리 표시
        if (!usePhysicsDetection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxTargetSearchDistance);
        }

        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}