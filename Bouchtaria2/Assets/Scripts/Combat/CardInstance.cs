using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CardZone
{
    Deck,
    Hand,
    Board,
    Graveyard
}

public enum PlayerOwner
{
    Player,
    Enemy
}
public enum EffectTrigger
{
    Deploy,    // d
    Berserk,   // b
    Requiem,   // r
    Strike,    // s
}

public class CardInstance : MonoBehaviour, IAttackable
{
    GameManager gameManager;
    DeckManager deckManager;
    public Transform Transform { get; private set; }
    // Immutable reference
    public CardData Data { get; private set; }
    CardView view;
    // Runtime state
    public int CurrentAttack { get; private set; }
    public string CurrentEffect { get; private set; }
    public string CurrentEffectText { get; private set; }
    public int BaseManaCost { get; private set; }
    public int CurrentHealth { get; private set; }
    public int CurrentManaCost => Mathf.Max(0, BaseManaCost + temporaryManaModifier);
    public bool IsDead = false;
    public PlayerOwner Owner { get; set; }
    private string pendingTargetedEffect;
    public CardData.SpellTargetType spellType { get; set; }
    public CardZone CurrentZone { get; private set; }
    private int temporaryManaModifier = 0;
    public bool HasAttackedThisTurn { get; set; }
    public bool IsSummoningSick { get; set; }
    public CardView cardView { get; set; }

    private Dictionary<EffectTrigger, List<string>> parsedEffects =    new Dictionary<EffectTrigger, List<string>>();

    public void AddTemporaryManaModifier(int amount)
    {
        temporaryManaModifier += amount;
    }
    public void ClearTemporaryManaModifiers()
    {
        temporaryManaModifier = 0;
    }
    // -------------------------
    // Initialization (called by CardFactory ONLY)
    // -------------------------
    public void Initialize(CardData data, PlayerOwner owner)
    {
        Data = data;
        Owner = owner;
        view = gameObject.GetComponent<CardView>();
        BaseManaCost = data.manaCost;
        CurrentAttack = data.atkValue;
        CurrentHealth = data.hpValue;
        CurrentEffect = data.effect;
        CurrentEffectText = data.effectText;
        CurrentZone = CardZone.Deck;
        Transform = transform;

        HasAttackedThisTurn = false;
        IsSummoningSick = true;

        gameManager = FindFirstObjectByType<GameManager>();
        deckManager = FindFirstObjectByType<DeckManager>();
        cardView = GetComponent<CardView>();

        ParseEffects();
    }
    private void Update()
    {
        if (CurrentManaCost < BaseManaCost) cardView.manaText.color = Color.green;
        if (CurrentManaCost > BaseManaCost) cardView.manaText.color = Color.red;
        if (CurrentManaCost == BaseManaCost) cardView.manaText.color = Color.white;
        cardView.manaText.text = CurrentManaCost.ToString();
    }
    public bool HasTrait(string trait)
    {
        return Data.traits != null && Data.traits.Contains(trait, StringComparer.OrdinalIgnoreCase);
    }
    public bool HasKeyword(string keywordString)
    {
        if (Data == null || CurrentEffect == null)
            return false;

        if (CurrentEffect.Contains(keywordString))
        {
            return true;
        }

        return false;
    }
    public void SetZone(CardZone newZone)
    {
        CurrentZone = newZone;

        if (newZone == CardZone.Board)
        {
            IsSummoningSick = true;
        }
    }
    public void OnTurnStart()
    {
        HasAttackedThisTurn = false;

        if (CurrentZone == CardZone.Board)
            IsSummoningSick = false;
    }
    #region EffectTriggers :
    #region Spells
    public void OnPlaySpell()
    {
        if (spellType == CardData.SpellTargetType.None)
        {
            ResolveSpell();
        }
        else
        {
            //gameManager.BeginSpellTargeting(this);
        }
    }
    public void ResolveSpell()
    {
        TriggerSpell();
        Destroy(gameObject);
    }
    public void ResolveSpell(IAttackable target)
    {
        TriggerSpell(target);
        Destroy(gameObject);
    }
    private void TriggerSpell()
    {
        Debug.Log($"Spell {name} resolved");
    }
    private void TriggerSpell(IAttackable target)
    {
        Debug.Log($"Spell {name} resolved on {target}");
    }
    #endregion
    public void OnEnterBoard()
    {
        TriggerDeploy();
    }
    private void TriggerDeploy()
    {
        TriggerEffects(EffectTrigger.Deploy);
    }
    private void TriggerBerserk()
    {
        TriggerEffects(EffectTrigger.Berserk);
    }

    private void TriggerRequiem()
    {
        TriggerEffects(EffectTrigger.Requiem);
    }
    private bool TryParseIntEffect(    string effect,    string effectName,    out int value)
    {
        value = default;

        if (!effect.StartsWith(effectName))
        {
            return false;
        }

        int start = effect.IndexOf('(');
        int end = effect.IndexOf(')');

        if (start < 0 || end < 0 || end <= start + 1)
        {
            Debug.LogError($"Malformed {effectName} effect '{effect}' on card {Data.name}");
            return false;
        }

        string valueStr = effect.Substring(start + 1, end - start - 1);

        if (!int.TryParse(valueStr, out value))
        {
            Debug.LogError(
                $"Invalid {effectName} parameter '{valueStr}' on card {Data.name}");
            return false;
        }

        return true;
    }
    private void ParseEffects()
    {
        parsedEffects.Clear();

        if (string.IsNullOrWhiteSpace(CurrentEffect))
            return;

        // Split triggers by space
        string[] triggerBlocks = CurrentEffect.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (string block in triggerBlocks)
        {
            int open = block.IndexOf('[');
            int close = block.LastIndexOf(']');

            if (open <= 0 || close <= open)
            {
                continue;
            }

            string triggerStr = block.Substring(0, open);
            string content = block.Substring(open + 1, close - open - 1);

            if (!TryParseTrigger(triggerStr, out EffectTrigger trigger))
            {
                Debug.LogError($"Unknown trigger '{triggerStr}' on card {Data.name}");
                continue;
            }

            string[] effects = content.Split(';', StringSplitOptions.RemoveEmptyEntries);

            if (!parsedEffects.ContainsKey(trigger))
                parsedEffects[trigger] = new List<string>();

            foreach (string e in effects)
                parsedEffects[trigger].Add(e.Trim());
        }
    }
    private bool TryParseTrigger(string str, out EffectTrigger trigger)
    {
        trigger = default;

        switch (str)
        {
            case "d":
                trigger = EffectTrigger.Deploy;
                return true;
            case "b":
                trigger = EffectTrigger.Berserk;
                return true;
            case "r":
                trigger = EffectTrigger.Requiem;
                return true;
            default:
                return false;
        }
    }
    private void TriggerEffects(EffectTrigger trigger)
    {
        if (!parsedEffects.TryGetValue(trigger, out var effects))
            return;

        foreach (string effect in effects)
            ExecuteEffect(effect);
    }
    private void ExecuteEffect(string effect)
    {
        effect = effect.ToLowerInvariant();

        if (effect.StartsWith("morphto"))
        {
            if (!TryParseIntEffect(effect, "morphto", out int id))
                return;

            MorphTo(id);
            return;
        }

        if (effect.StartsWith("draw"))
        {
            if (!TryParseIntEffect(effect, "draw", out int cards))
                return;

            deckManager.Draw(cards, Owner);
            return;
        }

        if (effect.StartsWith("autodmg"))
        {
            if (!TryParseIntEffect(effect, "autodmg", out int dmg))
                return;

            AutoDamageCore(dmg);
            return;
        }

        if (effect.StartsWith("autoheal"))
        {
            if (!TryParseIntEffect(effect, "autoheal", out int heal))
                return;

            AutoHealCore(heal);
            return;
        }

        if (effect.StartsWith("autoshield"))
        {
            if (!TryParseIntEffect(effect, "autoshield", out int shield))
                return;

            AutoShieldCore(shield);
            return;
        }

        if (effect.StartsWith("damage") && effect.Contains(",target"))
        {
            BeginTargetedEffect(effect);
            return;
        }

        Debug.LogError($"Unknown effect '{effect}' on card {Data.name}");
    }
    private void BeginTargetedEffect(string effect)
    {
        pendingTargetedEffect = effect;

        gameManager.BeginEffectTargeting(
            source: this,
            owner: Owner,
            onTargetChosen: OnEffectTargetChosen
        );
    }
    private void OnEffectTargetChosen(IAttackable target)
    {
        if (string.IsNullOrEmpty(pendingTargetedEffect))
            return;

        TryExecuteDamage(pendingTargetedEffect, target);
        pendingTargetedEffect = null;
    }
    private void TryExecuteDamage(string effect, IAttackable target)
    {
        if (!TryParseIntEffect(effect, "damage", out int amount))
            return;

        if (target == null)
        {
            Debug.LogError($"Damage effect requires a target on {Data.name}");
            return;
        }

        target.TakeDamage(amount);
    }

    #endregion
    #region Effects
    public void MorphTo(int newCardId)
    {
        CardData newData = CardDatabase.Instance.GetCardById(newCardId);
        if (newData == null || newCardId == Data.id)
        {
            Debug.LogError($"Evolve failed");
            return;
        }

        // Preserve state you want to keep

        int currentDamage = Data.hpValue - CurrentHealth;
        // Swap data
        Data = newData;

        CurrentHealth = Mathf.Max(CurrentHealth, Data.hpValue - currentDamage);
        CurrentAttack = newData.atkValue;
        BaseManaCost = newData.manaCost;

        CurrentEffect = newData.effect;
        CurrentEffectText = newData.effectText;
        ParseEffects();

        // Notify view
        cardView.UpdateMode();
    }
    public void AutoHealCore(int heal)
    {
        if (Owner == PlayerOwner.Player)
            gameManager.PlayerCore.Heal(heal);
        else
            gameManager.EnemyCore.Heal(heal);
    }
    public void AutoDamageCore(int dmg)
    {
        if (Owner == PlayerOwner.Enemy)
            gameManager.PlayerCore.TakeDamage(dmg);
        else
            gameManager.EnemyCore.TakeDamage(dmg);
    }
    public void AutoShieldCore(int shield)
    {
        if (Owner == PlayerOwner.Player)
            gameManager.PlayerCore.AddShield(shield);
        else
            gameManager.EnemyCore.AddShield(shield);
    }
    #endregion
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        CurrentHealth -= amount;

        if (CurrentHealth <= 0)
        {
            Die();
        }
        
        TriggerBerserk();

        view.hpTextBoard.text = CurrentHealth.ToString();
        if (CurrentHealth < Data.hpValue) view.hpTextBoard.color = Color.red;
        else if (CurrentHealth > Data.hpValue) view.hpTextBoard.color = Color.green;
        else if (CurrentHealth == Data.hpValue) view.hpTextBoard.color = Color.white;
        if (CurrentAttack > Data.atkValue) view.atkTextBoard.color = Color.green;

    }
    internal void ModifyStats(int atk, int hp)
    {
        CurrentAttack += atk;
        CurrentHealth+=hp;
        if (CurrentHealth < Data.hpValue) view.hpTextBoard.color = Color.red;
        if (CurrentHealth > Data.hpValue) view.hpTextBoard.color = Color.green;
        if (CurrentAttack > Data.atkValue) view.hpTextBoard.color = Color.green;
    }
    public void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        if (CurrentZone != CardZone.Board)
            return;

        TriggerRequiem();

        if (Owner == PlayerOwner.Player)
        {
            AllyCardDropArea board = FindFirstObjectByType<AllyCardDropArea>();
            if (board != null)
                board.HandleAllyDeath(this);
        }
        else
        {
            EnemyCardDropArea board = FindFirstObjectByType<EnemyCardDropArea>();
            if (board != null)
                board.HandleEnemyDeath(this);
        }
    }

}
