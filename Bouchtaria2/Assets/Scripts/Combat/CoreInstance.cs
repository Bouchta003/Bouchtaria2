using UnityEngine;

public class CoreInstance : MonoBehaviour
{
    public PlayerOwner Owner { get; private set; }

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    public int Shield { get; private set; }
    public event System.Action OnCoreChanged;

    public bool IsDestroyed => CurrentHealth <= 0;

    public void Initialize(PlayerOwner owner, int maxHealth)
    {
        Owner = owner;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        Shield = 0;
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

}
