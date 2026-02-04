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
    public bool IsBleeding { get; set; }
    public int BleedingTurns { get; set; }
    public int Shield { get; private set; }
    public event System.Action OnCoreChanged;
    [SerializeField] private Transform attackProxy;
    [SerializeField] private GameObject bleedUI;
    public string CurrentEffect { get; set; }
    public Transform AttackProxy => attackProxy;
    private void Update()
    {
        bleedUI.SetActive(IsBleeding);
    }
    public bool IsDestroyed => CurrentHealth <= 0;
    public void Initialize(PlayerOwner owner, int maxHealth)
    {
        Owner = owner;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        CurrentAttack = 0;
        Shield = 0;
        Transform = transform;
        IsBleeding = false;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        GameManager gm = FindFirstObjectByType<GameManager>();
        gm.NotifyDamage(Owner, amount);
        int remaining = amount; GetComponentInParent<DamageFeedback>()?.Play();
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if(Owner==PlayerOwner.Player)
            DamageVFXManager.Instance.PlayRandomHitOnCore(DamageVFXManager.Instance.uiVfxRootAlly);
        else
            DamageVFXManager.Instance.PlayRandomHitOnCore(DamageVFXManager.Instance.uiVfxRootEnemy);


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
    public void Bleed()
    {
        if(IsBleeding && CurrentHealth > 1)
        {
            TakeDamage(1);
            BleedingTurns++;
            if (BleedingTurns >= 3) { IsBleeding = false; BleedingTurns = 0; }
        }
    }
    public void AddShield(int amount)
    {
        Shield += amount;
        OnCoreChanged?.Invoke();
    }
    public void Heal(int amount)
    {
        int bonus = 0;int preHeal = CurrentHealth;
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (Owner == PlayerOwner.Player) bonus = gm.PlayerHealBonus;
        else bonus = gm.EnemyHealBonus;
        CurrentHealth = Mathf.Min(CurrentHealth += amount+bonus, MaxHealth);
        int differenceHp = CurrentHealth - preHeal;
        OnCoreChanged?.Invoke();
        gm.NotifyHealed(Owner,differenceHp);
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
