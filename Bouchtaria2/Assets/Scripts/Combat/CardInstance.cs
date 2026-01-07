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
    public string CurrentEffect { get; set; }
    public string CurrentEffectText { get; set; }
    public int BaseManaCost { get; set; }
    public int CurrentHealth { get; private set; }
    public int CurrentMaxHealth { get; private set; }
    public int CurrentManaCost => Mathf.Max(0, BaseManaCost + temporaryManaModifier);
    public bool IsDead = false;
    public PlayerOwner Owner { get; set; }
    private string pendingTargetedEffect;
    public CardData.SpellTargetType spellType { get; set; }
    public CardZone CurrentZone { get; private set; }
    private int temporaryManaModifier = 0;
    public bool HasAttackedThisTurn { get; set; }
    public int ThornsDamage { get; set; }
    public bool IsBleeding { get; set; }
    public int BleedingTurns { get; set; }
    public bool HasAttackedTwiceThisTurn { get; set; }
    public bool IsSummoningSick { get; set; }
    public bool WasPlayed { get; set; }
    public bool IsDisplay { get; set; }
    public CardView cardView { get; set; }
    public bool DeployPending { get; set; }

    private Dictionary<EffectTrigger, List<string>> parsedEffects =    new Dictionary<EffectTrigger, List<string>>();
    public string CurrentCastEffect;
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
        ThornsDamage = GetThornDamage();
        IsBleeding = false;
        CurrentMaxHealth = data.hpValue;

        HasAttackedThisTurn = false;
        HasAttackedTwiceThisTurn = false;
        IsSummoningSick = true;
        IsDisplay = false;
        gameManager = FindFirstObjectByType<GameManager>();
        deckManager = FindFirstObjectByType<DeckManager>();
        cardView = GetComponent<CardView>();

        WasPlayed = true;

        ParseEffects();
    }
    private int GetThornDamage()
    {
        int value = -1;

        if (!CurrentEffect.Contains("thorns"))
        {
            return -1;
        }

        int startEffect = CurrentEffect.IndexOf("thorns");

        string thornsEffect = CurrentEffect.Substring(startEffect);

        int startDmg = thornsEffect.IndexOf('(');
        int endDmg = thornsEffect.IndexOf(')');

        if (startDmg < 0 || endDmg < 0 || endDmg <= startDmg + 1)
        {
            return -1;
        }

        string valueStr = thornsEffect.Substring(startDmg + 1, endDmg - startDmg - 1);

        if (int.TryParse(valueStr, out value))
        {
            return value;
        }
        else return -1;

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
        if(HasKeyword("haste"))
            HasAttackedTwiceThisTurn = false;

        if (CurrentZone == CardZone.Board)
            IsSummoningSick = false;

        TriggerEffects(EffectTrigger.StartOfTurn);
    }
    public void OnTurnEnd()
    {
        if (CurrentZone == CardZone.Board)
        {
            Bleed();
            TriggerEffects(EffectTrigger.EndOfTurn);
        }
    }
    public void Bleed()
    {
        if (IsBleeding)
        {
            TakeDamage(1);
            BleedingTurns++;
            if (BleedingTurns >= 3) { IsBleeding = false; BleedingTurns = 0; view.UpdateMode(); }
        }
    }
    #region EffectTriggers :
    #region Spells
    public void OnPlaySpell()
    {
        // Determine spell type
        if (!CurrentEffect.Contains("target"))
            spellType = CardData.SpellTargetType.None;
        else
            spellType = CardData.SpellTargetType.Any;

        // NON-TARGET SPELL
        if (spellType == CardData.SpellTargetType.None)
        {
            ResolveSpell();
            return;
        }

        // TARGET SPELL
        if (Owner == PlayerOwner.Player)
        {
            gameManager.BeginEffectTargeting(
                source: this,
                owner: Owner,
                onTargetChosen: ResolveSpell,
                effectTargetType: ConvertSpellTargetType(spellType)
            );
        }
        else
        {
            // Enemy TARGET spell
            if (CurrentEffect.Contains("gear") || CurrentEffect.Contains("heal") || CurrentEffect.Contains("buff"))
            {
                IAttackable target =
                    gameManager.ChooseEnemyEffectTarget(
                        EffectTarget.Unit, false, false);

                if (target == null)
                {
                    Debug.LogWarning($"Enemy tried to play gear spell '{Data.name}' but no valid target.");
                    return;
                }

                ResolveSpell(target);
                return;
            }

            // Enemy NON-GEAR spell
            IAttackable defaultTarget =
                gameManager.ChooseEnemyEffectTarget(
                    ConvertSpellTargetType(spellType), true, false);

            ResolveSpell(defaultTarget);
        }

    }

    private EffectTarget ConvertSpellTargetType(CardData.SpellTargetType type)
    {
        return type switch
        {
            CardData.SpellTargetType.Unit => EffectTarget.Unit,
            CardData.SpellTargetType.Core => EffectTarget.Core,
            CardData.SpellTargetType.Any => EffectTarget.Any,
            _ => EffectTarget.None
        };
    }
    public void ResolveSpell(IAttackable target)
    {
        gameManager.UseMana(CurrentManaCost, Owner);

        ExecuteSpellEffects(target);

        // NOW remove from hand
        HandManager hand = Owner == PlayerOwner.Player
            ? gameManager.allyHand
            : gameManager.enemyHand;

        hand.handCards.Remove(gameObject);
        hand.UpdateCardPositions();

        Destroy(gameObject);
    }

    private void ResolveSpell()
    {
        gameManager.UseMana(CurrentManaCost, Owner);
        ExecuteSpellEffects(null);

        HandManager hand = Owner == PlayerOwner.Player
            ? gameManager.allyHand
            : gameManager.enemyHand;

        hand.handCards.Remove(gameObject);
        hand.UpdateCardPositions();

        Destroy(gameObject);
    }

    private void ExecuteSpellEffects(IAttackable target)
    {
        if (string.IsNullOrEmpty(CurrentEffect))
            return;

        // Directly resolve targeted effects
        if (CurrentEffect.StartsWith("damage"))
        {
            TryExecuteDamage(CurrentEffect, target);
            return;
        }

        if (CurrentEffect.StartsWith("heal"))
        {
            TryExecuteHeal(CurrentEffect, target);
            return;
        }

        if (CurrentEffect.StartsWith("gear"))
        {
            TryExecuteGear(CurrentEffect, CurrentEffectText, target);
            return;
        }

        if (CurrentEffect.StartsWith("buff"))
        {
            TryExecuteBuff(CurrentEffect, target);
            return;
        }

        // Non-target effects (draw, summon, etc.)
        ExecuteEffect(CurrentEffect);
    }

    #endregion
    public void OnEnterBoard()
    {
        TriggerDeploy();
    }
    private void TriggerDeploy()
    {
        Debug.Log($"[DEPLOY] TriggerDeploy called on {Data.name} | Effect = {CurrentEffect}");

        if (WasPlayed)
        {
            TriggerEffects(EffectTrigger.Deploy);
            WasPlayed = true;
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
        Debug.Log($"[DEPLOY] Executing effect: {effect}");
        if (effect.Contains(",target"))
        {
            DeployPending = (CurrentZone == CardZone.Board);
            BeginTargetedEffect(effect);
            return;
        }


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
            TryExecuteBuff(effect, null);
            return;
        }
        if (effect.StartsWith("buff") && effect.Contains(",target"))
        {
            BeginTargetedEffect(effect);
            return;
        }
        if (effect.StartsWith("damage"))
        {
            TryExecuteDamage(effect, null);
            return;
        }

        if (effect.StartsWith("gear"))
        {
            TryExecuteGear(effect, CurrentEffectText, null);
            return;
        }

        Debug.LogError($"Unknown effect '{effect}' on card {Data.name}");
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
            { 
                parsedEffects[trigger].Add(e.Trim()); Debug.Log($"[DEPLOY] Parsing deploy effects: {e}");
            }
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
    private void BeginTargetedEffect(string effect)
    {
        Debug.Log($"[TARGET] BeginTargetedEffect called for {effect}");

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
            // Enemy auto-target
            if (CurrentEffect.Contains("gear") || CurrentEffect.Contains("heal") || CurrentEffect.Contains("buff"))
            {
                IAttackable target =
                  gameManager.ChooseEnemyEffectTarget(type, false, false);
                OnEffectTargetChosen(target);
            }
            else
            {
                IAttackable target =
               gameManager.ChooseEnemyEffectTarget(type, true, false);
                OnEffectTargetChosen(target);
            }
        }
    }
    private void OnEffectTargetChosen(IAttackable target)
    {
        if (string.IsNullOrEmpty(pendingTargetedEffect))
            return;

        if (pendingTargetedEffect.StartsWith("gear"))
        {
            TryExecuteGear(pendingTargetedEffect, "no", target);
        }
        else if (pendingTargetedEffect.StartsWith("damage"))
        {
            TryExecuteDamage(pendingTargetedEffect, target);
        }
        else if (pendingTargetedEffect.StartsWith("heal"))
        {
            TryExecuteHeal(pendingTargetedEffect, target);
        }
        else if (pendingTargetedEffect.StartsWith("buff"))
        {
            TryExecuteBuff(pendingTargetedEffect, target);
        }

        pendingTargetedEffect = null;

    }

    #endregion
    #region Effects
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
        string[] discoversCards = valueStr.Split(',');

        if (int.TryParse(discoversCards[0], out int id) && int.TryParse(discoversCards[1], out int idd) && int.TryParse(discoversCards[2], out int iddd))
        {
            gameManager.Discover(id, idd, iddd, Owner);
        }
        else gameManager.DiscoverEffect(valueStr, Owner);
    }
    private void TryExecuteAddCard(string effect)
    {
        if (!TryParseIntEffect(effect, "addcard", out int cardId))
            return;

        gameManager.AddCardToHand(Owner, cardId);
    }
    private void TryExecuteBuff(string effect, IAttackable target)
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
            ModifyStats(atk, hp);
        }
        else
        {
            if (target is CardInstance inst)
                inst.ModifyStats(atk, hp);
        }

        //Update view
        view.hpTextBoard.text = CurrentHealth.ToString();
        if (CurrentHealth < CurrentMaxHealth) view.hpTextBoard.color = Color.red;
        if (CurrentHealth > CurrentMaxHealth) view.hpTextBoard.color = Color.green;
        if (CurrentHealth == CurrentMaxHealth) view.hpTextBoard.color = Color.white;
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
    private void TryExecuteHeal(string effect, IAttackable target)
    {
        if (!TryParseIntEffect(effect, "heal", out int amount))
            return;

        if (target == null)
        {
            Debug.LogError($"Heal effect requires a target on {Data.name}");
            return;
        }

        target.Heal(amount);
    }
    private void TryExecuteGear(string effect, string effectText, IAttackable target)
    {
        if (target is not CardInstance targetInstance)
        {
            Debug.LogError($"Gear effect requires a unit target on {Data.name}");
            return;
        }

        // Extract content inside gear(...)
        int start = effect.IndexOf('(');
        int end = effect.LastIndexOf(')');

        if (start < 0 || end <= start + 1)
        {
            Debug.LogError($"Malformed gear effect '{effect}' on {Data.name}");
            return;
        }

        string inner = effect.Substring(start + 1, end - start - 1).Trim();
        if (targetInstance.CurrentEffect.Contains("hunter")) inner += " selfbuff(1,1)";
        // Split multiple inner effects by space
        string[] subEffects = inner.Split(' ');

        foreach (string subEffect in subEffects)
        {
            ApplySingleGearEffect(subEffect.Trim(), targetInstance);
        }

        // Final visuals / text
        targetInstance.CurrentEffectText += "\n" + effectText;
        targetInstance.cardView.UpdateMode();

        if (targetInstance.Owner == PlayerOwner.Player)
            gameManager.CheckGlow();
    }
    private void ApplySingleGearEffect(string subEffect, CardInstance target)
    {
        // KEYWORD (quickstrike, taunt, etc.)
        if (!subEffect.Contains("("))
        {
            target.CurrentEffect += " " + subEffect;

            if (subEffect == "quickstrike" && target.IsSummoningSick)
                target.IsSummoningSick = false;

            return;
        }

        // THORNS(x)
        if (subEffect.StartsWith("thorns"))
        {
            int addedValue = GetSingleIntFromEffect(subEffect);
            if (addedValue <= 0)
                return;

            // Check if target already has thorns
            int currentThorns = target.ThornsDamage;
            int newThornsValue = currentThorns + addedValue;

            // Update runtime value
            target.ThornsDamage = newThornsValue;

            // Update effect string:
            // 1. Remove existing thorns(x) if present
            target.CurrentEffect = RemoveEffectByPrefix(target.CurrentEffect, "thorns");

            // 2. Add updated thorns value
            target.CurrentEffect += $" thorns({newThornsValue})";

            return;
        }

        // SELFBUFF(a,b)
        if (subEffect.StartsWith("selfbuff"))
        {
            (int atk, int hp) = GetTwoIntsFromEffect(subEffect);
            target.ModifyStats(atk, hp);
            target.CurrentEffect += " " + subEffect;
            return;
        }

        Debug.LogWarning($"Unknown gear sub-effect '{subEffect}' on {Data.name}");
    }
    private int GetSingleIntFromEffect(string effect)
    {
        int start = effect.IndexOf('(');
        int end = effect.IndexOf(')');
        if (start < 0 || end <= start + 1)
            return -1;

        return int.TryParse(effect.Substring(start + 1, end - start - 1), out int v)
            ? v
            : -1;
    }
    private (int, int) GetTwoIntsFromEffect(string effect)
    {
        int start = effect.IndexOf('(');
        int end = effect.IndexOf(')');
        if (start < 0 || end <= start + 1)
            return (0, 0);

        string[] parts = effect.Substring(start + 1, end - start - 1).Split(',');
        if (parts.Length != 2)
            return (0, 0);

        int.TryParse(parts[0], out int a);
        int.TryParse(parts[1], out int b);
        return (a, b);
    }
    private string RemoveEffectByPrefix(string effects, string prefix)
    {
        if (string.IsNullOrEmpty(effects))
            return effects;

        string[] parts = effects.Split(' ');
        List<string> kept = new();

        foreach (string part in parts)
        {
            if (!part.StartsWith(prefix))
                kept.Add(part);
        }

        return string.Join(" ", kept).Trim();
    }
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
        if (CurrentHealth < CurrentMaxHealth) view.hpTextBoard.color = Color.red;
        else if (CurrentHealth > CurrentMaxHealth) view.hpTextBoard.color = Color.green;
        else if (CurrentHealth == CurrentMaxHealth) view.hpTextBoard.color = Color.white;
        if (CurrentAttack > Data.atkValue) view.atkTextBoard.color = Color.green;

    }
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHealth =Mathf.Min(CurrentHealth+amount,CurrentMaxHealth);

        view.hpTextBoard.text = CurrentHealth.ToString();
        if (CurrentHealth < CurrentMaxHealth) view.hpTextBoard.color = Color.red;
        else if (CurrentHealth > CurrentMaxHealth) view.hpTextBoard.color = Color.green;
        else if (CurrentHealth == CurrentMaxHealth) view.hpTextBoard.color = Color.white;
        if (CurrentAttack > Data.atkValue) view.atkTextBoard.color = Color.green;

    }
    internal void ModifyStats(int atk, int hp)
    {
        CurrentAttack += atk;
        CurrentHealth+=hp;
        CurrentMaxHealth += hp;
        if (CurrentHealth < CurrentMaxHealth) view.hpTextBoard.color = Color.red;
        if (CurrentHealth > CurrentMaxHealth) view.hpTextBoard.color = Color.green;
        if (CurrentAttack > Data.atkValue) view.atkTextBoard.color = Color.green;
        view.UpdateMode();
    }
    public void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        if (CurrentZone != CardZone.Board)
            return;
        gameManager.NotifyCardKilled(this);

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
