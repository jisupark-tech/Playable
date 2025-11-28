using UnityEngine;

public interface IHealth
{
    /// <summary>
    /// 현재 체력 반환
    /// </summary>
    int GetCurrentHealth();

    /// <summary>
    /// 최대 체력 반환
    /// </summary>
    int GetMaxHealth();

    /// <summary>
    /// 데미지 받기
    /// </summary>
    /// <param name="damage">받을 데미지 양</param>
    void TakeDamage(int damage);

    /// <summary>
    /// 체력 회복
    /// </summary>
    /// <param name="amount">회복할 체력 양</param>
    void Heal(int amount);

    /// <summary>
    /// 죽었는지 확인
    /// </summary>
    bool IsDead();

    /// <summary>
    /// 체력 변화 시 호출되는 이벤트
    /// </summary>
    /// <param name="currentHealth">현재 체력</param>
    /// <param name="maxHealth">최대 체력</param>
    void OnHealthChanged(int currentHealth, int maxHealth);

    /// <summary>
    /// 죽을 때 호출되는 이벤트
    /// </summary>
    void OnDeath();

    /// <summary>
    /// 체력 비율 반환 (0.0 ~ 1.0)
    /// </summary>
    float GetHealthRatio();
}