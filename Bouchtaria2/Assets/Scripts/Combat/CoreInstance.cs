using DG.Tweening;
using System.Collections;
using UnityEngine;

public class CoreInstance : MonoBehaviour, IAttackable
{
    public PlayerOwner Owner { get; private set; }
    public Transform Transform { get; private set; }
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public int CurrentAttack { get; private set; }
    public int Shield { get; private set; }
    public event System.Action OnCoreChanged;
    [SerializeField] private Transform attackProxy;
    public Transform AttackProxy => attackProxy;

    public bool IsDestroyed => CurrentHealth <= 0;

    public void Initialize(PlayerOwner owner, int maxHealth)
    {
        Owner = owner;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        CurrentAttack = 0;
        Shield = 0;
        Transform = transform;
    }

    public void TakeDamage(int amount)
    {
        int remaining = amount;

        if (Shield > 0)
        {
            int absorbed = Mathf.Min(Shield, remaining);
            Shield -= absorbed;
            remaining -= absorbed;
            OnCoreChanged?.Invoke();
        }

        if (remaining > 0)
        {
            CurrentHealth -= remaining;
            OnCoreChanged?.Invoke();
            Debug.Log($"Took {remaining} damage, {CurrentHealth} HP left.");
        }

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            OnCoreChanged?.Invoke();
            Debug.Log($"Ded.");
            Die();
        }
    }

    public void AddShield(int amount)
    {
        Shield += amount;
        OnCoreChanged?.Invoke();
    }
    private void Die()
    {
        FindFirstObjectByType<GameManager>()
            .OnCoreDestroyed(Owner);
    }
    public IEnumerator PlayHitReaction()
    {
        transform.DOKill();

        Vector3 originalPos = transform.position;

        transform.DOShakePosition(
            0.1f,
            0.12f,
            vibrato: 14,
            randomness: 90,
            fadeOut: true
        );

        yield return new WaitForSeconds(0.1f);

        transform.position = originalPos;
    }

}
