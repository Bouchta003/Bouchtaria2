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
    EndOfTurn,
    StartOfTurn,
}
public enum EffectTarget
{
    None,
    Any,
    Unit, 
    Core,
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
    public bool WasPlayed { get; set; }
    public bool IsDisplay { get; set; }
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
        IsDisplay = false;
        gameManager = FindFirstObjectByType<GameManager>();
        deckManager = FindFirstObjectByType<DeckManager>();
        cardView = GetComponent<CardView>();

        WasPlayed = true;

        ParseEffects();
    }
    public void RemoveEffect(string effect)
    {
        if (CurrentEffect.Contains(effect))
        {
            CurrentEffect = string.Join(
                " ",
                CurrentEffect
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(token => token != effect)
                );
            view.UpdateMode();
            return;
        }
    }
    private void Update()
    {
        if (Data != null)
        {
            if (CurrentManaCost < BaseManaCost) cardView.manaText.color = Color.green;
            if (CurrentManaCost > BaseManaCost) cardView.manaText.color = Color.red;
            if (CurrentManaCost == BaseManaCost) cardView.manaText.color = Color.white;
            cardView.manaText.text = CurrentManaCost.ToString();
        }
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

        TriggerEffects(EffectTrigger.StartOfTurn);
    }
    public void OnTurnEnd()
    {
        if (CurrentZone == CardZone.Board)
            TriggerEffects(EffectTrigger.EndOfTurn);
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
    {if(WasPlayed)
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
            case "eot":
                trigger = EffectTrigger.EndOfTurn;
                return true;
            case "sot":
                trigger = EffectTrigger.StartOfTurn;
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

        if (effect.StartsWith("summon"))
        {
            TryExecuteSummon(effect);
            return;
        }
        if (effect.StartsWith("discover"))
        {
            TryExecuteDiscover(effect);
            return;
        }
        if (effect.StartsWith("addcard"))
        {
            TryExecuteAddCard(effect);
            return;
        }
        if (effect.StartsWith("buff"))
        {
            TryExecuteBuff(effect);
            return;
        }

        if (effect.StartsWith("damage") && effect.Contains(",target"))
        {
            BeginTargetedEffect(effect);
            return;
        }

        Debug.LogError($"Unknown effect '{effect}' on card {Data.name}");
    }
    private void TryExecuteSummon(string effect)
    {
        if (!TryParseIntEffect(effect, "summon", out int cardId))
            return;
        if(effect.StartsWith("summonforother"))
            gameManager.TrySummonForOther(Owner, cardId);
        else
            gameManager.TrySummonForOwner(Owner, cardId);
    }
    private void TryExecuteDiscover(string effect)
    {
        if (!effect.StartsWith("discover"))
        {
            Debug.LogError(
                $"Invalid discover effect on card {Data.name}");
            return;
        }

        int start = effect.IndexOf('(');
        int end = effect.IndexOf(')');

        if (start < 0 || end < 0 || end <= start + 1)
        {
            Debug.LogError($"Malformed {effect} effect on card {Data.name}");
            return;
        }

        string valueStr = effect.Substring(start + 1, end - start - 1);
        string[] discoversCards = valueStr.Split(',');int id1 = -1; int id2 = -1;int id3 = -1;
        
        if (int.TryParse(discoversCards[0], out int id))
        {
            id1 = id;
        }
        if (int.TryParse(discoversCards[1], out int idd))
        {
            id2 = idd;
        }
        if (int.TryParse(discoversCards[2], out int iddd))
        {
            id3 = iddd;
        }

        gameManager.Discover(id1,id2,id3, Owner);
    }
    private void TryExecuteAddCard(string effect)
    {
        if (!TryParseIntEffect(effect, "addcard", out int cardId))
            return;

        gameManager.AddCardToHand(Owner, cardId);
    }
    private void TryExecuteBuff(string effect)
    {
        if (!effect.StartsWith("buff"))
        {
            Debug.LogError(
                $"Invalid buff effect on card {Data.name}");
            return;
        }

        //Determining buff value
        int start = effect.IndexOf('(');
        int end = effect.IndexOf(')');

        if (start < 0 || end < 0 || end <= start + 1)
        {
            Debug.LogError($"Malformed {effect} effect on card {Data.name}");
            return;
        }

        string valueStr = effect.Substring(start + 1, end - start - 1);
        string[] stats = valueStr.Split(','); int atk = -1; int hp = -1;
        if (stats.Length < 2)
        {
            //Keyword logic
            return;
        }
        if (int.TryParse(stats[0], out int atkbuff))
        {
            atk = atkbuff;
        }
        if (int.TryParse(stats[1], out int hpbuff))
        {
            hp = hpbuff;
        }
        //Self Buff Logic :
        if (effect.StartsWith("buffself"))
        {
            CurrentAttack += atk;
            CurrentHealth += hp;
        }
        else
        {
            //Buff another card logic
        }

        //Update view
        view.hpTextBoard.text = CurrentHealth.ToString();
        if (CurrentHealth < Data.hpValue) view.hpTextBoard.color = Color.red;
        else if (CurrentHealth > Data.hpValue) view.hpTextBoard.color = Color.green;
        else if (CurrentHealth == Data.hpValue) view.hpTextBoard.color = Color.white;
        if (CurrentAttack > Data.atkValue) view.atkTextBoard.color = Color.green;
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

    private void BeginTargetedEffect(string effect)
    {
        EffectTarget type = EffectTarget.None;
        if (effect.ToLower().Contains("targetany")) type = EffectTarget.Any;
        if (effect.ToLower().Contains("targetunit")) type = EffectTarget.Unit;
        if (effect.ToLower().Contains("targetcore")) type = EffectTarget.Core;
        pendingTargetedEffect = effect;
        if (Owner == PlayerOwner.Player)
        {
            gameManager.BeginEffectTargeting(
                source: this,
                owner: Owner,
                onTargetChosen: OnEffectTargetChosen, type);
        }
        else
        {
            // 🔑 ENEMY: auto-resolve
            IAttackable target = gameManager.ChooseEnemyEffectTarget(type);
            OnEffectTargetChosen(target);
        }
    }
    private void OnEffectTargetChosen(IAttackable target)
    {
        if (string.IsNullOrEmpty(pendingTargetedEffect))
            return;

        TryExecuteDamage(pendingTargetedEffect, target);
        pendingTargetedEffect = null;
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
        RemoveEffect("blessed");
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
