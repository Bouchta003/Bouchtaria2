using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    ProgressComplete,
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
    public bool IsAsleep { get; set; }
    public bool IsDisplay { get; set; }
    public CardView cardView { get; set; }
    public bool DeployPending { get; set; }
    //Progression
    public int ProgressionCounter { get; set; }
    public int ProgressionCap { get; set; }
    private bool progressionCompleted = false;
    public bool EffectsSuppressed { get; private set; } = false;


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
        ThornsDamage = GetThornDamage();
        CurrentMaxHealth = data.hpValue;
        
        CurrentZone = CardZone.Deck;
        Transform = transform;
        IsBleeding = false;
        IsAsleep = false;

        HasAttackedThisTurn = false;
        HasAttackedTwiceThisTurn = false;
        IsSummoningSick = true;
        IsDisplay = false;
        gameManager = FindFirstObjectByType<GameManager>();
        deckManager = FindFirstObjectByType<DeckManager>();
        cardView = GetComponent<CardView>();

        WasPlayed = true;
        InitializeProgressIfAny();
        ParseEffects();
    }
    public bool CanAttackCoreOnSummon()
    {
        return HasKeyword("charge");
    }

    public bool CanAttackUnitOnSummon()
    {
        return HasKeyword("quickstrike") || HasKeyword("charge");
    }
    public void ScrambleStats()
    {
        int atk = CurrentAttack;
        int hp = CurrentMaxHealth;

        int total = atk + hp;

        // Safety
        if (total <= 1)
            return;

        // Pick atk directly (cleaner)
        int newAtk = UnityEngine.Random.Range(0, total + 1);
        int newHp = total - newAtk;

        // Enforce minimums
        if (newHp <= 0)
        {
            newHp = 1;
            newAtk = total - 1;
        }

        CurrentAttack = newAtk;
        CurrentMaxHealth = newHp;
        CurrentHealth = Mathf.Min(CurrentHealth, CurrentMaxHealth);

        cardView.UpdateMode();
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
        if (EffectsSuppressed)
            return false;
        if (Data == null || CurrentEffect == null)
            return false;

        if (CurrentEffect.Contains(keywordString))
        {
            return true;
        }

        return false;
    }
    #region Progress
    private void InitializeProgressIfAny()
    {
        // Cleanup old subscriptions
        if (gameManager == null) return;

        CleanupProgressSubscriptions();
        ProgressionCounter = 0;
        ProgressionCap = 0;
        progressionCompleted = false;

        if (Data.cardType != "minion" || !CurrentEffect.Contains("progress"))
            return;

        if (HasKeyword("progressheal") &&
            TryParseProgress("progressheal", out int healCap))
        {
            ProgressionCap = healCap;
            gameManager.OnOwnerHeal += OnHeal;
        }
        else if (HasKeyword("progressdamage") &&
            TryParseProgress("progressdamage", out int dmgCap))
        {
            ProgressionCap = dmgCap;
            gameManager.OnOwnerDamage += OnDamage;
        }
        else if (HasKeyword("progressdraw") &&
            TryParseProgress("progressdraw", out int drawCap))
        {
            ProgressionCap = drawCap;
            deckManager.OnCardDrawn += OnCardDrawn;
        }
        else if (HasKeyword("progressattack") &&
           TryParseProgress("progressattack", out int attackCap))
        {
            ProgressionCap = attackCap;
            gameManager.OnCardAttack += OnAttack;
        }
        else if (HasKeyword("progresseot") &&
          TryParseProgress("progresseot", out int turnCap))
        {
            ProgressionCap = turnCap;
            TurnManager.Instance.OnTurnEnded += OnEndTurn;
        }
    }

    private void CheckProgressCompletion()
    {
        if (progressionCompleted)
            return;

        if (ProgressionCounter < ProgressionCap)
            return;

        progressionCompleted = true;

        TriggerEffects(EffectTrigger.ProgressComplete);
        CleanupProgressSubscriptions();
    }
    private void CleanupProgressSubscriptions()
    {
        if (gameManager != null)
            gameManager.OnOwnerHeal -= OnHeal;

        if (gameManager != null)
            gameManager.OnOwnerDamage -= OnDamage;

        if (deckManager != null)
            deckManager.OnCardDrawn -= OnCardDrawn;

        if (deckManager != null)
            gameManager.OnCardAttack -= OnAttack;

        if (deckManager != null)
            TurnManager.Instance.OnTurnEnded -= OnEndTurn;
    }
    private bool TryParseProgress(
    string keyword,
    out int cap
)
    {
        cap = 0;

        // Split effect string into tokens
        string[] tokens = CurrentEffect
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            if (!token.StartsWith(keyword))
                continue;

            int start = token.IndexOf('(');
            int end = token.IndexOf(')');

            if (start < 0 || end <= start + 1)
            {
                Debug.LogError($"Malformed {keyword} effect '{token}' on {Data.name}");
                return false;
            }

            string valueStr = token.Substring(start + 1, end - start - 1);

            if (!int.TryParse(valueStr, out cap))
            {
                Debug.LogError($"Invalid {keyword} value '{valueStr}' on {Data.name}");
                return false;
            }

            return true;
        }

        return false;
    }

    void OnHeal(PlayerOwner owner, int healamount)
    {
        if (CurrentZone != CardZone.Board)
            return;

        // 1. Must be ally
        if (owner != Owner)
            return;

        ProgressionCounter += healamount;
        cardView.ShowProgress(ProgressionCounter, ProgressionCap);

        CheckProgressCompletion();
    }
    void OnDamage(PlayerOwner owner, int damage)
    {
        if (CurrentZone != CardZone.Board)
            return;

        // 1. Must be enemy
        if (owner == Owner)
            return;

        ProgressionCounter += damage;
        cardView.ShowProgress(ProgressionCounter, ProgressionCap);

        CheckProgressCompletion();
    }
    void OnAttack(CardInstance inst)
    {
        if (CurrentZone != CardZone.Board)
            return;
        if (inst.Owner != Owner)
            return;
        ProgressionCounter++;
        cardView.ShowProgress(ProgressionCounter, ProgressionCap);

        CheckProgressCompletion();
    }
    void OnEndTurn(PlayerOwner owner)
    {
        if (CurrentZone != CardZone.Board)
            return;
        if (owner!= Owner)
            return;
        ProgressionCounter++;
        cardView.ShowProgress(ProgressionCounter, ProgressionCap);

        CheckProgressCompletion();
    }
    private void OnCardDrawn(CardInstance card)
    {
        if (CurrentZone != CardZone.Board)
            return;

        if (card.Owner != Owner)
            return;

        ProgressionCounter++;
        cardView.ShowProgress(ProgressionCounter, ProgressionCap);
        Debug.Log($"[PROGRESS] {Data.name} draw {ProgressionCounter}/{ProgressionCap}");

        CheckProgressCompletion();
    }

    #endregion
    public void SetZone(CardZone newZone)
    {
        CurrentZone = newZone;

        if (newZone == CardZone.Board)
        {
            IsSummoningSick = true; InitializeProgressIfAny();
        }
    }
    public void OnTurnStart()
    {
        HasAttackedThisTurn = false;
        if(HasKeyword("haste"))
            HasAttackedTwiceThisTurn = false;

        IsSummoningSick = false;
        TriggerEffects(EffectTrigger.StartOfTurn);
    }
    public void OnTurnEnd()
    {
        if (CurrentZone == CardZone.Board)
        {
            Bleed();
            if (IsAsleep) IsAsleep = false;
            view.UpdateMode();
            TriggerEffects(EffectTrigger.EndOfTurn);
            if (HasKeyword("regeneration"))
            {
                Heal(999);
            }
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
        //Cannot cast spells in distortion world
        if (gameManager.DistortionWorld) return;
        // Determine spell type
        if (!CurrentEffect.Contains("target"))
            spellType = CardData.SpellTargetType.None;
        else if (CurrentEffect.Contains("targetunit")) spellType = CardData.SpellTargetType.Unit;
        else if (CurrentEffect.Contains("targetcore")) spellType = CardData.SpellTargetType.Core;
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
    onTargetChosen: target =>
    {
        ResolveSpell(target);
        return true; // ✅ spell resolved → end targeting
    },
    effectTargetType: ConvertSpellTargetType(spellType)
);
        }
        else
        {
            // Enemy TARGET spell
            if (CurrentEffect.Contains("gear") ||( CurrentEffect.Contains("heal") && !CurrentEffect.Contains("autoheal")) || CurrentEffect.Contains("buff"))
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
        if (string.IsNullOrWhiteSpace(CurrentEffect))
            return;
        // 🔑 GEAR IS A CONTAINER — HANDLE IT FIRST
        if (CurrentEffect.StartsWith("gear"))
        {
            TryExecuteGear(CurrentEffect, CurrentEffectText, target);
        }

        // Same idea as minions: split by space
        string[] effects = CurrentEffect
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawEffect in effects)
        {
            string effect = rawEffect.ToLowerInvariant();
            if (CurrentEffect.StartsWith("gear") && target == null)
            {
                Debug.LogError("Gear spell resolved without target");
                return;
            }

            // Targeted spell effects
            if (effect.StartsWith("damage"))
            {
                TryExecuteDamage(effect, target);
                continue;
            }
            if (effect.StartsWith("refreshattack") && target is CardInstance refresh)
            {
                TryRefreshAttack(refresh);continue;
            }
            if (effect.StartsWith("silenceall"))
            {
                SilenceAll();
                continue;
            }
            else if (effect.StartsWith("silence"))
            {
                if (target is CardInstance inst) TryExecuteSilence(inst);
                continue;
            }

            if (effect.StartsWith("sleepall"))
            {
                SleepAll();
                continue;
            }
            else if (effect.StartsWith("sleep"))
            {
                if(target is CardInstance inst) TryExecuteSleep(inst);
                continue;
            }

            if (effect.StartsWith("healall"))
            {
                TryExecuteHealAll(effect);
                continue;
            }
            else   if (effect.StartsWith("heal"))
            {
                TryExecuteHeal(effect, target);
                continue;
            }

            if (effect.StartsWith("gear"))
            {
                TryExecuteGear(effect, CurrentEffectText, target);
                continue;
            }

            if (effect.StartsWith("buff"))
            {
                TryExecuteBuff(effect, target);
                continue;
            }

            if (effect.StartsWith("praise"))
            {
                gameManager.Praise(Owner);
                continue;
            }
            // Non-target spell effects (summon, draw, etc.)
            ExecuteEffect(effect);
        }
    }

    #endregion
    public void OnEnterBoard()
    {
        if(!gameManager.DistortionWorld)
            TriggerDeploy();
        else
        {
            EffectsSuppressed = true;
            ScrambleStats();
        }
    }
    public void TriggerDeploy()
    {
        Debug.Log($"[DEPLOY] TriggerDeploy called on {Data.name} | Effect = {CurrentEffect}");

        if (WasPlayed)
        {
            TriggerEffects(EffectTrigger.Deploy);
            WasPlayed = false;
        }
    }
    private void TriggerEffects(EffectTrigger trigger)
    {
        if (EffectsSuppressed && Data.id!=29 && Data.id!=86) return;
        if (!parsedEffects.TryGetValue(trigger, out var effects))
            return;

        foreach (string effect in effects)
            ExecuteEffect(effect);
    }
    private void ExecuteEffect(string effect)
    {
        effect = effect.ToLowerInvariant();
        Debug.Log($"[DEPLOY] Executing effect: {effect}");
        if (effect.Contains(",target") && !effect.StartsWith("gear"))
        {
            DeployPending = (CurrentZone == CardZone.Board);
            BeginTargetedEffect(effect);
            return;
        }
        //Unique Effects
        if (effect.StartsWith("extraturn"))
        {
            GainExtraTurn();
            return;
        }
        if (effect.StartsWith("limitenemyspace"))
        {
            if (TryParseIntEffect(effect, "limitenemyspace", out int limit))
            {
                gameManager.LimitEnemySpace(Owner, limit);
            }
            return;
        }
        if (effect.StartsWith("giratina"))
        {
            gameManager.DistortionWorld = true;
            return;
        }
        if (effect.StartsWith("emperorsapphire"))
        {
            gameManager.EmperorSapphire(Owner);
            return;
        }

        if (effect.StartsWith("resurrectlast"))
        {
            gameManager.ResurrectLast(Owner, Data);
            return;
        }
        else if (effect.StartsWith("resurrect"))
        {
            gameManager.ResurrectRandom(Owner, Data);
            return;
        }

        if (effect.StartsWith("sleepall"))
        {
            SleepAll();
            return;
        }
        else if (effect.StartsWith("sleep"))
        {
            BeginTargetedEffect(effect);
            return;
        }
        if (effect.StartsWith("silenceall"))
        {
            SilenceAll();
            return;
        }
        else if (effect.StartsWith("silence"))
        {
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


            deckManager.StartCoroutine(deckManager.Draw(cards, Owner));

            return;
        }

        if (effect.StartsWith("praise"))
        {
            gameManager.Praise(Owner);
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

        if (effect.StartsWith("healall"))
        {
            if (!TryParseIntEffect(effect, "healall", out int heal))
                return;

            HealAll(heal);
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
        if (effect.StartsWith("ally?"))
        {
            TryExecuteAllyConditional(effect);
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
        if (effect.StartsWith("damageaoe"))
        {
            TryExecuteDamageAoe(effect);
            return;
        }
        else if (effect.StartsWith("damage"))
        {
            TryExecuteDamage(effect, null);
            return;
        }

        if (effect.StartsWith("gear"))
        {
            TryExecuteGear(effect, CurrentEffectText, null);
            return;
        }

        if (effect.StartsWith("managain"))
        {
            TryExecuteManaGain(effect);
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
    public void TriggerStrike()
    {
        TriggerEffects(EffectTrigger.Strike);
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
    public void ParseEffects()
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

            // 🔑 STRIP PARAMETERS: progressdraw(3) → progress
            int parenIndex = triggerStr.IndexOf('(');
            if (parenIndex > 0)
            {
                triggerStr = triggerStr.Substring(0, parenIndex);
            }

            string content = block.Substring(open + 1, close - open - 1);

            if (!TryParseTrigger(triggerStr, out EffectTrigger trigger))
            {
                Debug.LogError($"Unknown trigger '{triggerStr}' on card {Data.name}");
                continue;
            }

            List<string> effects = SplitTopLevelEffects(content);

            if (!parsedEffects.ContainsKey(trigger))
                parsedEffects[trigger] = new List<string>();

            foreach (string e in effects)
            { 
                parsedEffects[trigger].Add(e.Trim()); Debug.Log($"Parsing deploy effects: {e}");
            }
        }
    }
    private bool TryParseTrigger(string str, out EffectTrigger trigger)
    {
        trigger = default;

        switch (str)
        {
            case "d": trigger = EffectTrigger.Deploy; return true;
            case "b": trigger = EffectTrigger.Berserk; return true;
            case "r": trigger = EffectTrigger.Requiem; return true;
            case "s": trigger = EffectTrigger.Strike; return true;
            case "eot": trigger = EffectTrigger.EndOfTurn; return true;
            case "sot": trigger = EffectTrigger.StartOfTurn; return true;

            // ✅ Progress triggers (parameterized)
            case "progressdraw":
            case "progressheal":
            case "progressattack":
            case "progressdamage":
                trigger = EffectTrigger.ProgressComplete;
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
       onTargetChosen: target => OnEffectTargetChosen(target),
       effectTargetType: type
         );

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
            else if (pendingTargetedEffect.StartsWith("morphto"))
            {
                ICardDropArea board =
                    Owner == PlayerOwner.Player
                        ? gameManager.allyDropArea
                        : gameManager.enemyDropArea;

                List<CardInstance> allies =
                    board.GetCards()
                         .Select(go => go.GetComponent<CardInstance>())
                         .Where(ci => ci != null && !ci.IsDead && ci != this)
                         .ToList();

                if (allies.Count == 0)
                    return;

                // Pick strongest (atk + hp)
                CardInstance best =
                    allies.OrderByDescending(ci => ci.CurrentAttack + ci.CurrentHealth)
                          .First();

                OnEffectTargetChosen(best);
            }
            else
            {
                IAttackable target =
               gameManager.ChooseEnemyEffectTarget(type, true, false);
                OnEffectTargetChosen(target);
            }
        }
    }
    public bool OnEffectTargetChosen(IAttackable target)
    {
        if (string.IsNullOrEmpty(pendingTargetedEffect))
            return false;

        if (pendingTargetedEffect.StartsWith("ally?"))
        {
            string resolved = ResolveAllyConditional(pendingTargetedEffect);
            if (string.IsNullOrEmpty(resolved))
                return false;

            // 🔑 Replace the pending effect with the resolved one
            pendingTargetedEffect = resolved;

            // 🔁 Re-run THIS SAME method with the SAME target
            return OnEffectTargetChosen(target);
        }


        bool executed = false;

        if (pendingTargetedEffect.StartsWith("gear"))
        {
            if (target is CardInstance ci)
            {
                TryExecuteGear(pendingTargetedEffect, CurrentEffectText, ci);
                executed = true;
            }
        }
        else if (pendingTargetedEffect.StartsWith("damage"))
        {
            if (target != null)
            {
                TryExecuteDamage(pendingTargetedEffect, target);
                executed = true;
            }
        }
        else if (pendingTargetedEffect.StartsWith("refreshattack") && target is CardInstance refresh)
        {
            TryRefreshAttack(refresh);
            executed = true;
        }
        else if (pendingTargetedEffect.StartsWith("heal") && target != null)
        {
            TryExecuteHeal(pendingTargetedEffect, target);
            executed = true;
        }
        else if (pendingTargetedEffect.StartsWith("buff") && target is CardInstance buffTarget)
        {
            TryExecuteBuff(pendingTargetedEffect, buffTarget);
            executed = true;
        }
        else if (pendingTargetedEffect.StartsWith("sleep") && target is CardInstance sleepTarget)
        {
            TryExecuteSleep(sleepTarget);
            executed = true;
        }
        else if (pendingTargetedEffect.StartsWith("silence") && target is CardInstance silenceTarget)
        {
            TryExecuteSilence(silenceTarget);
            executed = true;
        }
        else if (pendingTargetedEffect.StartsWith("morphto") && target is CardInstance morphTarget)
        {
            executed = TryExecuteMorphTo(morphTarget);
        }

        // 🔑 ONLY clear targeting if something actually happened
        if (executed)
        {
            pendingTargetedEffect = null;
         
            gameManager.EndEffectTargetting();
        }

        return executed;
    }

    #endregion
    #region Effects
    void GainExtraTurn()
    {
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        tm.endButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "REWIND";
        if (Owner == PlayerOwner.Player) tm.PlayerHasExtraTurn = true;
        else tm.EnemyHasExtraTurn = true;
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
    private IEnumerable<CardInstance> GetOwnerBoardCards()
    {
        ICardDropArea board =
            Owner == PlayerOwner.Player
                ? gameManager.allyDropArea
                : gameManager.enemyDropArea;

        foreach (var go in board.GetCards())
        {
            if (go == null) continue;

            var ci = go.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
                yield return ci;
        }

        // 🔑 INCLUDE SELF if deploy is pending (card not yet added)
        if (CurrentZone == CardZone.Board && !IsDead)
            yield return this;
    }
    private string ResolveAllyConditional(string effect)
    {
        // Preserve targeting suffix
        string targetSuffix = "";
        int targetIndex = effect.IndexOf(",target");
        if (targetIndex >= 0)
        {
            targetSuffix = effect.Substring(targetIndex);
            effect = effect.Substring(0, targetIndex);
        }

        int open = effect.IndexOf('(');
        int close = effect.LastIndexOf(')');

        if (open < 0 || close <= open)
            return null;

        string inner = effect.Substring(open + 1, close - open - 1);
        string[] parts = inner.Split(':');
        if (parts.Length != 2)
            return null;

        if (!int.TryParse(parts[0], out int allyId))
            return null;

        string[] outcomes = parts[1].Split(';');
        if (outcomes.Length != 2)
            return null;

        bool allyExists =
            GetOwnerBoardCards().Any(ci => ci.Data != null && ci.Data.id == allyId);

        string chosen = allyExists ? outcomes[0] : outcomes[1];
        return chosen.Trim() + targetSuffix;
    }

    private void TryExecuteAllyConditional(string effect)
    {
        // Preserve targeting suffix (e.g. ,targetunit)
        string targetSuffix = "";
        int targetIndex = effect.IndexOf(",target");
        if (targetIndex >= 0)
        {
            targetSuffix = effect.Substring(targetIndex);
            effect = effect.Substring(0, targetIndex);
        }

        // Format: ally?(19:buff(2,2);buff(1,1))

        int open = effect.IndexOf('(');
        int close = effect.LastIndexOf(')');

        if (open < 0 || close <= open)
        {
            Debug.LogError($"Malformed ally? effect '{effect}'");
            return;
        }

        string inner = effect.Substring(open + 1, close - open - 1);
        string[] parts = inner.Split(':');

        if (parts.Length != 2)
        {
            Debug.LogError($"Malformed ally? condition '{effect}'");
            return;
        }

        if (!int.TryParse(parts[0], out int allyId))
        {
            Debug.LogError($"Invalid ally id in '{effect}'");
            return;
        }

        string[] outcomes = parts[1].Split(';');
        if (outcomes.Length != 2)
        {
            Debug.LogError($"ally? requires two outcomes '{effect}'");
            return;
        }

        bool allyExists =
            GetOwnerBoardCards()
                .Any(ci => ci.Data != null && ci.Data.id == allyId);

        string chosenEffect = allyExists ? outcomes[0] : outcomes[1];

        Debug.Log($"[ALLY?] id={allyId} exists={allyExists} → {chosenEffect}");

        string finalEffect = chosenEffect.Trim() + targetSuffix;
        Debug.Log($"[ALLY?] RAW EFFECT = {effect}");
        Debug.Log($"[ALLY?] BOARD COUNT = {GetOwnerBoardCards().Count()}");
        Debug.Log($"[ALLY?] ALLY IDS = {string.Join(",", GetOwnerBoardCards().Select(c => c.Data.id))}");
        Debug.Log($"[ALLY?] ALLY EXISTS = {allyExists}");
        Debug.Log($"[ALLY?] FINAL EFFECT = {finalEffect}");

        ExecuteEffect(finalEffect);

        return;
    }
    private List<string> SplitTopLevelEffects(string content)
    {
        List<string> results = new();
        int depth = 0;
        int lastSplit = 0;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ';' && depth == 0)
            {
                results.Add(content.Substring(lastSplit, i - lastSplit).Trim());
                lastSplit = i + 1;
            }
        }

        // Add final segment
        if (lastSplit < content.Length)
            results.Add(content.Substring(lastSplit).Trim());

        return results;
    }

    private void TryExecuteManaGain(string effect)
    {
        if (!TryParseIntEffect(effect, "managain", out int mana))
            return;
        else
            gameManager.GainMana(mana, Owner);
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
        if (!effect.StartsWith("discoverownertrait"))
        {
            if ((start < 0 || end < 0 || end <= start + 1))
            {
                Debug.LogError($"Malformed {effect} effect on card {Data.name}");
                return;
            }

            string valueStr = effect.Substring(start + 1, end - start - 1);
            string[] discoversCards = valueStr.Split(',');

            //Discover specific cards
            if (int.TryParse(discoversCards[0], out int id) && int.TryParse(discoversCards[1], out int idd) && int.TryParse(discoversCards[2], out int iddd))
            {
                gameManager.Discover(id, idd, iddd, Owner);
            }
            else if (effect.StartsWith("discovertrait")) { gameManager.DiscoverTrait(valueStr, Owner); }
            else
            { gameManager.DiscoverEffect(valueStr, Owner); }
        }
        else{ gameManager.DiscoverOwnerTrait(Owner); }
    }
    private void TryExecuteAddCard(string effect)
    {
        if (!TryParseIntEffect(effect, "addcard", out int cardId))
            return;
        if (effect.StartsWith("addcardenemy")) { 
            if(Owner==PlayerOwner.Player)
                    gameManager.AddCardToHand(PlayerOwner.Enemy, cardId);
                else
                    gameManager.AddCardToHand(Owner, cardId);

            return;
        }

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
        gameManager.OnDamageWithCard(Owner);
    }
    private void TryExecuteDamageAoe(string effect)
    {
        if (!TryParseIntEffect(effect, "damageaoe", out int amount))
            return;

        // 🔑 Snapshot the targets first
        List<CardInstance> targets = new();

        if (Owner == PlayerOwner.Player)
        {
            foreach (GameObject enemyGO in gameManager.enemyDropArea.enemyPrefabCards)
            {
                if (enemyGO == null) continue;

                CardInstance ci = enemyGO.GetComponent<CardInstance>();
                if (ci != null && !ci.IsDead)
                    targets.Add(ci);
            }
        }
        else
        {
            foreach (GameObject allyGO in gameManager.allyDropArea.allyPrefabCards)
            {
                if (allyGO == null) continue;

                CardInstance ci = allyGO.GetComponent<CardInstance>();
                if (ci != null && !ci.IsDead)
                    targets.Add(ci);
            }
        }

        // 🔥 Apply damage AFTER snapshot
        foreach (CardInstance target in targets)
        {
            target.TakeDamage(amount);
        }
        gameManager.OnDamageWithCard(Owner);
    }

    private void TryExecuteSleep(CardInstance target)
    {
        if (target == null)
        {
            Debug.LogError($"Sleep effect requires a target on {Data.name}");
            return;
        }
        target.IsAsleep = true;target.view.UpdateMode();
    }
    private void TryExecuteSilence(CardInstance target)
    {
        if (target == null)
        {
            Debug.LogError($"Silence effect requires a target on {Data.name}");
            return;
        }
        target.Silence();
    }
    private void TryRefreshAttack(CardInstance target)
    {
        if (target == null)
        {
            Debug.LogError($"Refresh effect requires a target on {Data.name}");
            return;
        }

        target.HasAttackedThisTurn = false;
        target.HasAttackedTwiceThisTurn = false;

        if (target.Owner == PlayerOwner.Player)
            gameManager.CheckGlow();
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
        if (target.Owner != Owner)
        {
            if (Owner == PlayerOwner.Player && gameManager.PlayerDarkHeal) { target.TakeDamage(amount); return; }
            if (Owner != PlayerOwner.Player && gameManager.EnemyDarkHeal) { target.TakeDamage(amount); return; }
            
        }
        target.Heal(amount);
    }
    private void TryExecuteHealAll(string effect)
    {
        if (!TryParseIntEffect(effect, "heal", out int amount))
            return;

        HealAll(amount);
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
        targetInstance.ThornsDamage = targetInstance.GetThornDamage();
        targetInstance.CurrentEffectText += "\n" + effectText;
        targetInstance.cardView.UpdateMode();
        targetInstance.ParseEffects();

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
            Debug.LogWarning($"Evolve failed");
            return;
        }

        // Preserve state you want to keep

        int currentDamage = Data.hpValue - CurrentHealth;
        // Swap data
        Data = newData;

        CurrentHealth = Mathf.Max(CurrentHealth, Data.hpValue - currentDamage);
        CurrentMaxHealth = newData.hpValue;
        CurrentAttack = newData.atkValue;
        BaseManaCost = newData.manaCost;

        CurrentEffect = newData.effect;
        CurrentEffectText = newData.effectText;

        ParseEffects();

        // Notify view
        cardView.UpdateMode(); 
        StartCoroutine(DelayedProgressInit());
    }
    private bool TryExecuteMorphTo(CardInstance target)
    {
        // Must be ally
        if (target.Owner != Owner)
            return false;

        // Cannot morph into self
        if (target == this)
            return false;

        // Must be a unit
        if (target.Data.cardType != "minion")
            return false;

        Debug.Log($"[DITTO] {Data.name} morphs into {target.Data.name}");

        Data = target.Data;
        CurrentEffect = target.CurrentEffect;
        CurrentEffectText = target.CurrentEffectText;

        CurrentAttack = target.CurrentAttack;
        CurrentMaxHealth = target.CurrentMaxHealth;
        CurrentHealth = target.CurrentHealth;

        ThornsDamage = target.ThornsDamage;
        IsBleeding = target.IsBleeding;
        BleedingTurns = target.BleedingTurns;
        IsAsleep = target.IsAsleep;

        HasAttackedThisTurn = false;
        HasAttackedTwiceThisTurn = false;

        ParseEffects();
        InitializeProgressIfAny();

        cardView.Bind(this);
        cardView.UpdateMode();

        return true;
    }
    private IEnumerator DelayedProgressInit()
    {
        yield return null; // wait 1 frame
        InitializeProgressIfAny();
    }
    public void AutoHealCore(int heal)
    {
        if (Owner == PlayerOwner.Player)
            gameManager.PlayerCore.Heal(heal);
        else
            gameManager.EnemyCore.Heal(heal);
    }
    public void HealAll(int heal)
    {
        if (Owner == PlayerOwner.Player)
        {  
            gameManager.PlayerCore.Heal(heal);            
            foreach(GameObject ally in gameManager.allyDropArea.allyPrefabCards)
            {
                CardInstance inst = ally.GetComponent<CardInstance>();
                inst.Heal(heal);
            }
        }
        else
        {
            gameManager.EnemyCore.Heal(heal);
            foreach (GameObject enemy in gameManager.enemyDropArea.enemyPrefabCards)
            {
                CardInstance inst = enemy.GetComponent<CardInstance>();
                inst.Heal(heal);
            }
        }
    }
    public void SleepAll()
    {
        if (Owner == PlayerOwner.Enemy)
        {
            foreach (GameObject ally in gameManager.allyDropArea.allyPrefabCards)
            {
                CardInstance inst = ally.GetComponent<CardInstance>();
                inst.IsAsleep = true;inst.view.UpdateMode();
            }
        }
        else
        {
            Debug.Log("All enemies sleep");
            foreach (GameObject enemy in gameManager.enemyDropArea.enemyPrefabCards)
            {
                CardInstance inst = enemy.GetComponent<CardInstance>();
                inst.IsAsleep = true; inst.view.UpdateMode();
            }
        }
    }
    public void SilenceAll()
    {
        if (Owner == PlayerOwner.Enemy)
        {
            foreach (GameObject ally in gameManager.allyDropArea.allyPrefabCards)
            {
                CardInstance inst = ally.GetComponent<CardInstance>();
                inst.Silence();
            }
        }
        else
        {
            Debug.Log("All enemies silenced");
            foreach (GameObject enemy in gameManager.enemyDropArea.enemyPrefabCards)
            {
                CardInstance inst = enemy.GetComponent<CardInstance>();
                inst.Silence();
            }
        }
    }
    public void Silence()
    {
        Debug.Log($"Silenced {Data.name}");
        //Reset effect
        CurrentEffect = ""; CurrentEffectText = ""; parsedEffects.Clear();

        //Reset Stats
        CurrentAttack = Data.atkValue;CurrentHealth = Mathf.Min(CurrentHealth, Data.hpValue);
        ThornsDamage = 0;

        //Update display
        view.UpdateMode();
        TurnManager tm = FindFirstObjectByType<TurnManager>(); tm.UpdateGlow();
    }
    public void AutoDamageCore(int dmg)
    {
        if (Owner == PlayerOwner.Enemy)
            gameManager.PlayerCore.TakeDamage(dmg);
        else
            gameManager.EnemyCore.TakeDamage(dmg);
        gameManager.OnDamageWithCard(Owner);

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
        gameManager.NotifyDamage(Owner,amount);
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
        int bonus = 0;int preHeal = CurrentHealth;

        GameManager gm = FindFirstObjectByType<GameManager>();
        //ApplyBonus
        if (Owner == PlayerOwner.Player) bonus = gm.PlayerHealBonus;
        else bonus = gm.EnemyHealBonus;
        CurrentHealth = Mathf.Min(CurrentHealth += amount + bonus, CurrentMaxHealth);
        int differenceHp =  CurrentHealth- preHeal;

        view.hpTextBoard.text = CurrentHealth.ToString();
        if (CurrentHealth < CurrentMaxHealth) view.hpTextBoard.color = Color.red;
        else if (CurrentHealth > CurrentMaxHealth) view.hpTextBoard.color = Color.green;
        else if (CurrentHealth == CurrentMaxHealth) view.hpTextBoard.color = Color.white;
        if (CurrentAttack > Data.atkValue) view.atkTextBoard.color = Color.green;

        gameManager.NotifyHealed(Owner, differenceHp);
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
        if (gameManager != null)
            gameManager.OnOwnerHeal -= OnHeal;

        if (gameManager != null)
            gameManager.OnOwnerDamage -= OnDamage;

        if (gameManager != null)
            gameManager.OnCardAttack -= OnAttack;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnEndTurn;

        if (deckManager != null)
            deckManager.OnCardDrawn -= OnCardDrawn;


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
