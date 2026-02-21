using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using DG.Tweening;
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
    None,    // none
    Deploy,    // d
    Berserk,   // b
    Requiem,   // r
    Strike,    // s
    Heal,      // h
    SpellCast,      // spell
    EndOfTurn,      //eot
    StartOfTurn,    //sot
    ManaGain,       // mana
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
    public int CurrentAttack { get; set; }
    public string CurrentEffect { get; set; }
    public string CurrentEffectText { get; set; }
    public int BaseManaCost { get; set; }
    public int CurrentHealth { get; set; }
    public int CurrentMaxHealth { get; set; }
    public int CurrentManaCost
    {
        get
        {
            // If no temporary modifier, return the base cost exactly
            if (temporaryManaModifier == 0)
            {
                return BaseManaCost;
            }

            // Otherwise apply modifier and clamp to [1,10]
            int raw = BaseManaCost + temporaryManaModifier;
            return Mathf.Clamp(raw, 1, 10);
        }
    }
    public int CurrentTotalStats { get { return CurrentAttack + CurrentHealth; } }
    public bool IsDead = false;
    public PlayerOwner Owner { get; set; }
    private string pendingTargetedEffect;
    private List<string> pendingTriggeredEffects;
    private int pendingTriggeredEffectIndex;
    private EffectTrigger? pendingTriggeredEffectType;
    private bool forceRandomTargetingForCurrentDeploy;
    private EffectTrigger? currentResolvingTrigger;
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
    public bool IsDying { get; set; } = false;
    public CardView cardView { get; set; }
    public bool DeployPending { get; set; }
    public event System.Action<CardInstance> OnDeployResolved;
    public event System.Action<CardInstance> OnSpellResolved;
    //Progression
    public int ProgressionCounter { get; set; }
    public int ProgressionCap { get; set; }
    private bool progressionCompleted = false;
    public bool EffectsSuppressed { get; set; } = false;


    private Dictionary<EffectTrigger, List<string>> parsedEffects = new Dictionary<EffectTrigger, List<string>>();
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
        gameManager = GameManager.Instance;
        deckManager = FindFirstObjectByType<DeckManager>();
        cardView = GetComponent<CardView>();

        WasPlayed = true;
        ParseEffects();
        InitializeProgressIfAny();
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
        int CurrentDamage = CurrentMaxHealth - CurrentHealth;
        int total = atk + hp;

        // Safety
        if (total <= 1)
            return;

        // Pick atk directly (cleaner)
        int newHp = UnityEngine.Random.Range(1, total + 1);
        int newAtk = total - newHp;

        // Enforce minimums
        if (newHp <= 0)
        {
            newHp = 1;
            newAtk = total - 1;
        }

        CurrentAttack = newAtk;
        CurrentMaxHealth = newHp;
        CurrentHealth = CurrentMaxHealth-CurrentDamage;

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
            UpdateStatsColor();
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
    public bool HasText(string keywordString)
    {
        if (EffectsSuppressed)
            return false;
        if (Data == null || CurrentEffectText == null)
            return false;

        if (CurrentEffectText.ToLower().Contains(keywordString.ToLower()))
        {
            return true;
        }

        return false;
    }
    #region Progress
    public void InitializeProgressIfAny()
    {
        // Cleanup old subscriptions
        if (gameManager == null) return;

        CleanupProgressSubscriptions();
        ProgressionCounter = 0;
        ProgressionCap = 0;
        progressionCompleted = false;

        if (Data.cardType != "minion" || !CurrentEffect.Contains("progress"))
        {
            SyncHealSubscription();
            SyncSpellSubscription();
            SyncManaGainSubscription();
            return;
        }

        if (HasKeyword("progressheal") &&
            TryParseProgress("progressheal", out int healCap))
        {
            ProgressionCap = healCap;
        }
        if (HasKeyword("progressmana") &&
           TryParseProgress("progressmana", out int mana))
        {
            ProgressionCap = mana;
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
        else if (HasKeyword("progressplay") &&
          TryParseProgress("progressplay", out int playCap))
        {
            ProgressionCap = playCap;
            gameManager.OnCardPlayed += OnCardPlayed;
        }
        else if (HasKeyword("progresseot") &&
          TryParseProgress("progresseot", out int turnCap))
        {
            ProgressionCap = turnCap;
            TurnManager.Instance.OnTurnEnded += OnEndTurn;
        }
        else if (HasKeyword("progresskazuyacombo") &&
          TryParseProgress("progresskazuyacombo", out int kazuyaCap))
        {
            ProgressionCap = kazuyaCap;
            gameManager.OnSpellPlayed += OnSpellPlayed;
            cardView.ShowProgress(ProgressionCounter, ProgressionCap);
        }
        else if (HasKeyword("progressspel") &&
         TryParseProgress("progressspel", out int spellcap))
        {
            ProgressionCap = spellcap;
            gameManager.OnSpellPlayed += OnSpellPlayed;
            cardView.ShowProgress(ProgressionCounter, ProgressionCap);
        }
        else if (HasKeyword("progressbuff") &&
          TryParseProgress("progressbuff", out int buffCap))
        {
            ProgressionCap = buffCap;
            UpdateBuffProgressCounter();
        }

        SyncHealSubscription();
        SyncSpellSubscription();
        SyncManaGainSubscription();
    }

    private void CheckProgressCompletion()
    {
        if (progressionCompleted)
            return;

        if (ProgressionCounter < ProgressionCap)
            return;

        if (IsDead || IsDying || CurrentZone != CardZone.Board)
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

        if (gameManager != null)
            gameManager.OnCardPlayed -= OnCardPlayed;

        if (deckManager != null)
            TurnManager.Instance.OnTurnEnded -= OnEndTurn;

        if (gameManager != null)
            gameManager.OnSpellPlayed -= OnSpellPlayed;

        if (gameManager != null)
            gameManager.OnOwnerManaGain -= OnManaGained;
    }
    private void SyncHealSubscription()
    {
        if (gameManager == null)
            return;

        gameManager.OnOwnerHeal -= OnHeal;

        bool hasHealTrigger = parsedEffects.TryGetValue(EffectTrigger.Heal, out var healEffects)
                              && healEffects != null
                              && healEffects.Count > 0;

        bool hasProgressHeal = HasKeyword("progressheal") && ProgressionCap > 0;

        if (hasHealTrigger || hasProgressHeal)
            gameManager.OnOwnerHeal += OnHeal;
    }
    private void SyncSpellSubscription()
    {
        if (gameManager == null)
            return;

        gameManager.OnSpellPlayed -= OnSpellPlayed;

        bool hasSpellTrigger = parsedEffects.TryGetValue(EffectTrigger.SpellCast, out var spellEffects)
                              && spellEffects != null
                              && spellEffects.Count > 0;

        bool hasProgressSpell = (HasKeyword("progresskazuyacombo")|| HasKeyword("progressspell")) && ProgressionCap > 0;

        if (hasSpellTrigger || hasProgressSpell)
            gameManager.OnSpellPlayed += OnSpellPlayed;
    }
    private void SyncManaGainSubscription()
    {
        if (gameManager == null)
            return;

        gameManager.OnOwnerManaGain -= OnManaGained;

        bool hasManaGainTrigger = parsedEffects.TryGetValue(EffectTrigger.ManaGain, out var manaEffects)
                                  && manaEffects != null
                                  && manaEffects.Count > 0;
        bool hasProgressMana = HasKeyword("progressmana") && ProgressionCap > 0;
        if (hasManaGainTrigger ||hasProgressMana)
            gameManager.OnOwnerManaGain += OnManaGained;
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
                start = token.IndexOf('[');
                end = token.IndexOf(']');
            }

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

    private string GetProgressPlayKeywordFilter()
    {
        if (string.IsNullOrWhiteSpace(CurrentEffect))
            return string.Empty;

        string[] tokens = CurrentEffect.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            if (!token.StartsWith("progressplay"))
                continue;

            string suffix = token.Substring("progressplay".Length);
            int delimiter = suffix.IndexOfAny(new[] { '(', '[' });

            if (delimiter >= 0)
                suffix = suffix.Substring(0, delimiter);

            return suffix;
        }

        return string.Empty;
    }
    void OnHeal(PlayerOwner owner, int healamount)
    {
        if (CurrentZone != CardZone.Board)
            return;

        // 1. Must be ally
        if (owner != Owner)
            return;

        if (healamount <= 0)
            return;

        bool hasHealTrigger = parsedEffects.TryGetValue(EffectTrigger.Heal, out var healEffects)
                              && healEffects != null
                              && healEffects.Count > 0;

        if (hasHealTrigger)
            TriggerHeal();

        if (!HasKeyword("progressheal") || ProgressionCap <= 0)
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
        if (owner != Owner)
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

    private void OnCardPlayed(CardInstance card)
    {
        if (CurrentZone != CardZone.Board)
            return;

        if (card == null || card.Owner != Owner)
            return;
        
        if (ReferenceEquals(card, this))
            return;

        string progressPlayFilter = GetProgressPlayKeywordFilter();
        if (!string.IsNullOrWhiteSpace(progressPlayFilter))
        {
            string playedCardEffectText = card.CurrentEffectText ?? string.Empty;
            if (playedCardEffectText.IndexOf(progressPlayFilter, StringComparison.OrdinalIgnoreCase) < 0)
                return;
        }

        ProgressionCounter++;
        cardView.ShowProgress(ProgressionCounter, ProgressionCap);

        CheckProgressCompletion();
    }

    private void OnSpellPlayed(CardInstance spell)
    {
        if (CurrentZone != CardZone.Board)
            return;

        if (spell == null || spell.Owner != Owner)
            return;

        bool hasSpellTrigger = parsedEffects.TryGetValue(EffectTrigger.SpellCast, out var spellEffects)
                               && spellEffects != null
                               && spellEffects.Count > 0;

        if (hasSpellTrigger)
            TriggerEffects(EffectTrigger.SpellCast);

        if (HasKeyword("progressspell"))
        {
            ProgressionCounter++;
            cardView.ShowProgress(ProgressionCounter, ProgressionCap);
            CheckProgressCompletion();
            return;
        }

        if (!HasKeyword("progresskazuyacombo") || ProgressionCap <= 0)
            return;

        string effect = spell.CurrentEffect?.ToLowerInvariant() ?? string.Empty;
        bool hasBuff = effect.Contains("buff");
        bool hasDamage = effect.Contains("damage") || effect.Contains("dmg");

        bool validStep = ProgressionCounter switch
        {
            0 => hasBuff,
            1 => hasBuff,
            2 => hasDamage,
            _ => false
        };

        if (validStep)
            ProgressionCounter++;
        else
            ProgressionCounter = 0;

        cardView.ShowProgress(ProgressionCounter, ProgressionCap);
        CheckProgressCompletion();
    }

    private void OnManaGained(PlayerOwner owner, int amount)
    {
        if (CurrentZone != CardZone.Board)
            return;

        if (owner != Owner || amount <= 0)
            return;

        TriggerEffects(EffectTrigger.ManaGain);

        if (!HasKeyword("progressmana") || ProgressionCap <= 0)
            return;

        ProgressionCounter ++;
        cardView.ShowProgress(ProgressionCounter, ProgressionCap);

        CheckProgressCompletion();
    }

    private void UpdateBuffProgressCounter()
    {
        if (CurrentZone != CardZone.Board)
            return;

        if (!HasKeyword("progressbuff") || ProgressionCap <= 0)
            return;

        int atkGain = Mathf.Max(0, CurrentAttack - Data.atkValue);
        int hpGain = Mathf.Max(0, CurrentMaxHealth - Data.hpValue);

        ProgressionCounter = atkGain + hpGain;
        cardView.ShowProgress(ProgressionCounter, ProgressionCap);

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
        Bleed();
        HasAttackedThisTurn = false;
        if (HasKeyword("haste"))
            HasAttackedTwiceThisTurn = false;

        IsSummoningSick = false;
        TriggerEffects(EffectTrigger.StartOfTurn);
    }
    public void OnTurnEnd()
    {
        if (CurrentZone == CardZone.Board)
        {
            Bleed();
            if (IsAsleep) { 
                IsAsleep = false; 
                if(gameManager.BoardHasCard(OtherPlayer(Owner), 55))//If enmy owns a darkrai
                {
                    TakeDamage(3);
                }
            
            }
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
            gameManager.OnDamageWithCard(OtherPlayer(Owner));
            BleedingTurns++;
            if (BleedingTurns >= 3) { IsBleeding = false; BleedingTurns = 0; view.UpdateMode(); }
        }
    }
    public static PlayerOwner OtherPlayer(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player) return PlayerOwner.Enemy;
        else return PlayerOwner.Player;
    }
    #region EffectTriggers :
    #region Spells
    private IEnumerable<string> SplitEffectsBySpace(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        int depth = 0;
        int lastSplit = 0;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (char.IsWhiteSpace(c) && depth == 0)
            {
                if (i > lastSplit)
                    yield return content.Substring(lastSplit, i - lastSplit).Trim();
                lastSplit = i + 1;
            }
        }

        if (lastSplit < content.Length)
            yield return content.Substring(lastSplit).Trim();
    }

    public void OnPlaySpell(IAttackable forcedTarget = null)
    {
        //Cannot cast spells in distortion world
        if (gameManager.DistortionWorld) return;
        // Determine spell type
        if (string.IsNullOrWhiteSpace(CurrentEffect))
        {
            Debug.LogError($"Spell {Data.name} has no effect and cannot be cast.");
            return;
        }

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
            if (forcedTarget != null)
            {
                ResolveSpell(forcedTarget);
                return;
            }

            if (CurrentEffect.Contains("gear") || (CurrentEffect.Contains("heal") && !CurrentEffect.Contains("autoheal")) || CurrentEffect.Contains("buff") || CurrentEffect.Contains("damagenheal"))
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

    private void FinalizeSpellResolution()
    {
        OnSpellResolved?.Invoke(this);

        HandManager hand = Owner == PlayerOwner.Player
            ? gameManager.allyHand
            : gameManager.enemyHand;

        hand.handCards.Remove(gameObject);
        hand.UpdateCardPositions();

        Destroy(gameObject);
    }

    private void CancelSpellCastAndReturnToHand()
    {
        HandManager hand = Owner == PlayerOwner.Player
            ? gameManager.allyHand
            : gameManager.enemyHand;

        if (hand == null)
            return;

        if (!hand.handCards.Contains(gameObject))
            hand.AddCard(gameObject);

        hand.UpdateCardPositions();
    }

    private bool TryExecutePolymerizationSpell(bool isSuperPolymerization)
    {
        Debug.Log("poly exec");
        HandManager ownerHand = Owner == PlayerOwner.Player ? gameManager.allyHand : gameManager.enemyHand;
        HandManager enemyHand = Owner == PlayerOwner.Player ? gameManager.enemyHand : gameManager.allyHand;

        if (ownerHand == null)
            return false;

        List<CardInstance> components = new();

        if (isSuperPolymerization)
        {
            CardInstance fromOwner = ownerHand.handCards
                .Select(go => go != null ? go.GetComponent<CardInstance>() : null)
                .FirstOrDefault(card => card != null
                    && card != this
                    && card.Data != null
                    && card.Data.cardType.Equals("minion", StringComparison.OrdinalIgnoreCase));

            CardInstance fromEnemy = enemyHand?.handCards
                .Select(go => go != null ? go.GetComponent<CardInstance>() : null)
                .FirstOrDefault(card => card != null
                    && card.Data != null
                    && card.Data.cardType.Equals("minion", StringComparison.OrdinalIgnoreCase));

            if (fromOwner == null || fromEnemy == null)
                return false;

            components.Add(fromOwner);
            components.Add(fromEnemy);
        }
        else
        {
            components = ownerHand.handCards
                .Select(go => go != null ? go.GetComponent<CardInstance>() : null)
                .Where(card => card != null
                    && card != this
                    && card.Data != null
                    && card.Data.cardType.Equals("minion", StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();

            if (components.Count < 2)
                return false;
        }

        CardData cardA = components[0].Data;
        CardData cardB = components[1].Data;

        List<string> combinedTraits = new();
        string firstTrait = cardA?.traits?.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstTrait))
            combinedTraits.Add(firstTrait);

        string secondTrait = cardB?.traits?.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(secondTrait) && !combinedTraits.Contains(secondTrait, StringComparer.OrdinalIgnoreCase))
            combinedTraits.Add(secondTrait);

        if (combinedTraits.Count < 2)
        {
            IEnumerable<string> fallbackTraits = (cardA?.traits ?? new List<string>())
                .Concat(cardB?.traits ?? new List<string>());

            foreach (string trait in fallbackTraits)
            {
                if (string.IsNullOrWhiteSpace(trait))
                    continue;

                if (combinedTraits.Contains(trait, StringComparer.OrdinalIgnoreCase))
                    continue;

                combinedTraits.Add(trait);
                if (combinedTraits.Count >= 2)
                    break;
            }
        }

        List<int> relatedCards = (cardA?.relatedCards ?? new List<int>())
            .Concat(cardB?.relatedCards ?? new List<int>())
            .Distinct()
            .ToList();

        if (ownerHand.handCards.Count >= ownerHand.maxHandSize)
            return false;

        foreach (CardInstance component in components)
        {
            HandManager hand = component.Owner == PlayerOwner.Player ? gameManager.allyHand : gameManager.enemyHand;
            if (hand == null)
                continue;

            component.transform.DOKill();
            hand.RemoveCardFromHand(component.gameObject);
            Destroy(component.gameObject);
        }

        CardInstance amalgam = gameManager.AddCardToHand(Owner, 235);
        if (amalgam == null)
            return false;

        int combinedManaCost = Mathf.Clamp((cardA?.manaCost ?? 0) + (cardB?.manaCost ?? 0), 0, 10);
        int combinedAttack = (cardA?.atkValue ?? 0) + (cardB?.atkValue ?? 0);
        int combinedHealth = (cardA?.hpValue ?? 0) + (cardB?.hpValue ?? 0);

        CardData amalgamData = new CardData
        {
            id = amalgam.Data.id,
            name = amalgam.Data.name,
            cardType = "minion",
            manaCost = combinedManaCost,
            atkValue = combinedAttack,
            hpValue = combinedHealth,
            traits = combinedTraits,
            relatedCards = relatedCards,
            effect = string.Join(" ", new[] { cardA?.effect, cardB?.effect }.Where(s => !string.IsNullOrWhiteSpace(s))),
            effectText = string.Join("\n", new[] { cardA?.effectText, cardB?.effectText }.Where(s => !string.IsNullOrWhiteSpace(s))),
            artPath = amalgam.Data.artPath,
            artCompactPath = amalgam.Data.artCompactPath,
            artSprite = amalgam.Data.artSprite,
            artSpriteCompact = amalgam.Data.artSpriteCompact,
            packable = amalgam.Data.packable,
            token = amalgam.Data.token,
            signature = amalgam.Data.signature,
            spellTargetType = CardData.SpellTargetType.None,
        };

        amalgam.Initialize(amalgamData, Owner);
        amalgam.SetZone(CardZone.Hand);
        amalgam.ParseEffects();
        amalgam.CurrentCastEffect = amalgam.CurrentEffect;
        amalgam.BaseManaCost = combinedManaCost;
        amalgam.GetComponent<CardView>()?.UpdateMode();

        ownerHand.UpdateCardPositions();
        enemyHand?.UpdateCardPositions();
        return true;
    }

    public void ResolveSpell(IAttackable target)
    {
        if (CurrentEffect.IndexOf("polymerization", StringComparison.OrdinalIgnoreCase) >= 0
            && CurrentEffect.IndexOf("superpolymerization", StringComparison.OrdinalIgnoreCase) < 0
            && !TryExecutePolymerizationSpell(isSuperPolymerization: false))
        {
            CancelSpellCastAndReturnToHand();
            return;
        }

        if (CurrentEffect.IndexOf("superpolymerization", StringComparison.OrdinalIgnoreCase) >= 0
            && !TryExecutePolymerizationSpell(isSuperPolymerization: true))
        {
            CancelSpellCastAndReturnToHand();
            return;
        }

        gameManager.NotifySpellPlayed(this);
        gameManager.UseMana(CurrentManaCost, Owner);

        ExecuteSpellEffects(target);
        FinalizeSpellResolution();
    }

    private void ResolveSpell()
    {
        if (CurrentEffect.IndexOf("polymerization", StringComparison.OrdinalIgnoreCase) >= 0
            && CurrentEffect.IndexOf("superpolymerization", StringComparison.OrdinalIgnoreCase) < 0
            && !TryExecutePolymerizationSpell(isSuperPolymerization: false))
        {
            CancelSpellCastAndReturnToHand();
            return;
        }

        if (CurrentEffect.IndexOf("superpolymerization", StringComparison.OrdinalIgnoreCase) >= 0
            && !TryExecutePolymerizationSpell(isSuperPolymerization: true))
        {
            CancelSpellCastAndReturnToHand();
            return;
        }

        gameManager.NotifySpellPlayed(this);
        gameManager.UseMana(CurrentManaCost, Owner);
        ExecuteSpellEffects(null);

        if (HasText("random") && Owner == PlayerOwner.Enemy)
        {
            gameManager.EnemyRandomCount++;
        }
        FinalizeSpellResolution();
    }
    private void ExecuteSpellEffects(IAttackable target)
    {
        if (string.IsNullOrWhiteSpace(CurrentEffect))
            return;
        // Same idea as minions: split by space
        IEnumerable<string> effects = SplitEffectsBySpace(CurrentEffect);
        if (Owner == PlayerOwner.Enemy)
        {
            gameManager.enemyDropArea.CardPlayed(this);
        }

        foreach (string rawEffect in effects)
        {
            string effect = rawEffect.ToLowerInvariant();

            if (effect.StartsWith("damagerandomenemy"))
            {
                TryExecuteDamageRandomEnemy(effect);
                continue;
            }
            if (effect.StartsWith("damageaoe"))
            {
                TryExecuteDamageAoe(effect);
                gameManager.CheckGlow();
                continue;
            }if (effect.StartsWith("grantall"))
            {
                TryExecuteGrantAll(effect);
                gameManager.CheckGlow();
                continue;
            }
            // Targeted spell effects
            if (effect.StartsWith("damage"))
            {
                TryExecuteDamage(effect, target);
                continue;
            }
            if (effect.StartsWith("kill"))
            {
                TryExecuteKill(effect, target);
                continue;
            }
            if (effect.StartsWith("catch"))
            {
                TryExecuteCatch(effect, target);
                continue;
            }
            if (effect.StartsWith("refreshattack") && target is CardInstance refresh)
            {
                TryRefreshAttack(refresh); continue;
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
                if (target is CardInstance inst) TryExecuteSleep(inst);
                continue;
            }

            if (effect.StartsWith("healall"))
            {
                TryExecuteHealAll(effect);
                continue;
            }
            else if (effect.StartsWith("heal"))
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

            if (effect.StartsWith("polymerization") || effect.StartsWith("superpolymerization"))
            {
                continue;
            }
            // Non-target spell effects (summon, draw, etc.)
            ExecuteEffect(effect);
        }
    }

    #endregion
    public void OnEnterBoard()
    {
        if (!gameManager.DistortionWorld)
            TriggerDeploy();
        else
        {
            EffectsSuppressed = true;
            ScrambleStats();
        }
    }
    public void TriggerDeploy(bool forceRandomTarget = false)
    {
        Debug.Log($"[DEPLOY] TriggerDeploy called on {Data.name} | Effect = {CurrentEffect}");
        gameManager.CheckGlow();
        if (WasPlayed)
        {
            forceRandomTargetingForCurrentDeploy = forceRandomTarget;
            bool hasDeployEffects = parsedEffects.TryGetValue(EffectTrigger.Deploy, out var deployEffects)
                                   && deployEffects != null
                                   && deployEffects.Count > 0;

            TriggerEffects(EffectTrigger.Deploy);
            forceRandomTargetingForCurrentDeploy = false;

            if (!DeployPending)
                FinalizeDeployResolution();
        }
    }
    private void FinalizeDeployResolution()
    {
        if (!WasPlayed)
            return;

        WasPlayed = false;
        gameManager.ClearDeploySummonCap(this);
        OnDeployResolved?.Invoke(this);
    }

    public void CancelPendingResolution()
    {
        pendingTargetedEffect = null;
        pendingTriggeredEffects = null;
        pendingTriggeredEffectType = null;
        pendingTriggeredEffectIndex = 0;
        DeployPending = false;
    }

    private static List<string> OrderEffectsForResolution(List<string> effects)
    {
        if (effects == null || effects.Count <= 1)
            return effects;

        List<string> targeted = new();
        List<string> nonTargeted = new();

        foreach (string effect in effects)
        {
            if (effect.Contains(",target") && !effect.StartsWith("gear"))
                targeted.Add(effect);
            else
                nonTargeted.Add(effect);
        }

        targeted.AddRange(nonTargeted);
        return targeted;
    }

    private void TriggerEffects(EffectTrigger trigger)
    {
        if (EffectsSuppressed && Data.id != 29 && Data.id != 86) return;
        if (!parsedEffects.TryGetValue(trigger, out var effects))
            return;

        currentResolvingTrigger = trigger;
        List<string> orderedEffects = OrderEffectsForResolution(effects);
        for (int i = 0; i < orderedEffects.Count; i++)
        {
            if (ExecuteEffect(orderedEffects[i]))
            {
                pendingTriggeredEffects = orderedEffects;
                pendingTriggeredEffectIndex = i + 1;
                pendingTriggeredEffectType = trigger;
                currentResolvingTrigger = null;
                return;
            }
        }
        currentResolvingTrigger = null;
    }
    private bool ExecuteEffect(string effect)
    {
        effect = effect.ToLowerInvariant();
        Debug.Log($"[DEPLOY] Executing effect: {effect}");

        if (effect.Contains(",target") && !effect.StartsWith("gear"))
        {
            DeployPending = (CurrentZone == CardZone.Board);
            BeginTargetedEffect(effect, forceRandomTargetingForCurrentDeploy);
            gameManager.CheckGlow();
            return true;
        }
        //Unique Effects
        //Turn tempering
        if (effect.StartsWith("extraturn"))
        {
            GainExtraTurn();
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("lebens"))
        {
            StartCoroutine(TriggerBensEffect()); return false;
        }
        if (effect.StartsWith("skipenemydraw"))
        {
            SkipNextEnemyDraw();
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("limitenemyspace"))
        {
            if (TryParseIntEffect(effect, "limitenemyspace", out int limit))
            {
                gameManager.LimitEnemySpace(Owner, limit);
            }
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("giratina"))
        {
            if(!Data.name.Contains("Origin"))
                gameManager.DistortionWorld = true;
            else
                gameManager.DistortionWorld = false;
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("ideal"))
        {
            if (Owner == PlayerOwner.Player) {
                if (deckManager.IdealEffect != 1)deckManager.IdealEffect = 0;
                else deckManager.IdealEffect = 3;
            }
            else {
                if (deckManager.IdealEffect != 0) deckManager.IdealEffect = 1;
                else deckManager.IdealEffect = 3;
            }
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("truth"))
        {
            if (Owner == PlayerOwner.Player)
            {
                if (deckManager.TruthEffect != 1) deckManager.TruthEffect = 0;
                else deckManager.TruthEffect = 3;
            }
            else
            {
                if (deckManager.TruthEffect != 0) deckManager.TruthEffect = 1;
                else deckManager.TruthEffect = 3;
            }
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("emperorsapphire"))
        {
            gameManager.EmperorSapphire(Owner);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("tawakkul"))
        {
            gameManager.StartCoroutine(gameManager.Tawakkul(5));
            gameManager.CheckGlow();return false;
        }

        if (effect.StartsWith("buffall"))
        {
            (int atk, int hp) = GetTwoIntsFromEffect(effect);
            if (atk == -1 && hp == -1){ 
                //Scenario of random mix of stats
                TryParseIntEffect(effect, "buffall", out int stats);
                gameManager.BuffAllAllies(stats, Owner);
                gameManager.CheckGlow(); return false;
            } 
            gameManager.BuffAllAllies(atk,hp,Owner);
            gameManager.CheckGlow(); return false;
        }

        if (effect.StartsWith("wipeboard"))
        {
            gameManager.WipeBoard();
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("scrambleallstats"))
        {
            gameManager.ScrambleAllUnitsStats();
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("resurrectlast"))
        {
            gameManager.ResurrectLast(Owner, Data);
            gameManager.CheckGlow();return false;
        }
        else if (effect.StartsWith("resurrectlow"))
        {
            gameManager.ResurrectLow(Owner, Data);
            gameManager.CheckGlow(); return false;
        }
        else if (effect.StartsWith("resurrect"))
        {
            gameManager.ResurrectRandom(Owner, Data);
            gameManager.CheckGlow();return false;
        }

        if (effect.StartsWith("selfdestroy"))
        {
            SelfDestroy();
            gameManager.CheckGlow();return false;
        }

        if (effect.StartsWith("sleepall"))
        {
            SleepAll();
            gameManager.CheckGlow();return false;
        }
        else if (effect.StartsWith("sleep"))
        {
            BeginTargetedEffect(effect, forceRandomTargetingForCurrentDeploy);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("silenceall"))
        {
            SilenceAll();
            gameManager.CheckGlow();return false;
        }
        else if (effect.StartsWith("silence"))
        {
            BeginTargetedEffect(effect, forceRandomTargetingForCurrentDeploy);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("morphto"))
        {
            if (!TryParseIntEffect(effect, "morphto", out int id))
            { gameManager.CheckGlow(); return false; }

            MorphTo(id);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("absorb"))
        {
            if (!TryParseIntEffect(effect, "absorb", out int amount))
            { gameManager.CheckGlow(); return false; }

            TryExecuteAbsorb(effect, null);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("consume"))
        {
            TryExecuteConsume(effect);
            gameManager.CheckGlow(); return false;
        }

        if (effect.StartsWith("draw"))
        {
            if (effect.StartsWith("draweffect"))
            {
                int open = effect.IndexOf('(');
                int close = effect.IndexOf(')');
                if (open < 0 || close <= open)
                {
                    Debug.LogWarning($"Malformed draweffet effect '{effect}' on {Data.name}");
                    return false;
                }

                string inner = effect.Substring(open + 1, close - open - 1).Trim();
                if (string.IsNullOrEmpty(inner))
                {
                    Debug.LogWarning($"Empty selfbuff parameters '{effect}' on {Data.name}");
                    return false;
                }
                deckManager.StartCoroutine(deckManager.DrawEffect(inner, Owner));
                gameManager.CheckGlow(); return false;
            }
           
            else if(TryParseIntEffect(effect, "draw", out int cards))
            {
                gameManager.CheckGlow();
                deckManager.StartCoroutine(deckManager.Draw(cards, Owner)); return false;
            }
        }

        if (effect.StartsWith("praise"))
        {
            gameManager.Praise(Owner);
            gameManager.CheckGlow();return false;
        }

        if (effect.StartsWith("autodmg"))
        {
            if (!TryParseIntEffect(effect, "autodmg", out int dmg))
            { gameManager.CheckGlow(); return false; }

            AutoDamageCore(dmg);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("advanceprogress"))
        {
            TryExecuteAdvanceProgress();
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("autoheal"))
        {
            if (!TryParseIntEffect(effect, "autoheal", out int heal))
            { gameManager.CheckGlow(); return false; }

            AutoHealCore(heal);
            gameManager.CheckGlow();return false;
        }

        if (effect.StartsWith("healall"))
        {
            if (!TryParseIntEffect(effect, "healall", out int heal))
            { gameManager.CheckGlow(); return false; }

            HealAll(heal);
            gameManager.CheckGlow();return false;
        }

        if (effect.StartsWith("autoshield"))
        {
            if (!TryParseIntEffect(effect, "autoshield", out int shield))
            { gameManager.CheckGlow(); return false; }

            AutoShieldCore(shield);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("summonrandomcost"))
        {
            gameManager.TrySummonForOwnerManaCost(Owner, GetSingleIntFromEffect(effect));
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("summondeckmaxmana"))
        {
            if (TryParseIntEffect(effect, "summondeckmaxmana", out int maxMana))
                deckManager.TrySummonRandomMinionFromDeckByMaxMana(Owner, maxMana);

            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("summondeckeffect"))
        {
            string effectSearch = ExtractParenthesizedArgs(effect);
            deckManager.TrySummonRandomMinionFromDeckByEffect(Owner, effectSearch);
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("summondecktrait"))
        {
            string traitSearch = ExtractParenthesizedArgs(effect);
            deckManager.TrySummonRandomMinionFromDeckByTrait(Owner, traitSearch);
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("summondeck"))
        {
            deckManager.TrySummonRandomMinionFromDeck(Owner, GetStringValueFromEffect(effect, "summondeck") == "deploy");
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("summon"))
        {
            TryExecuteSummon(effect);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("discover"))
        {
            TryExecuteDiscover(effect);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("ally?"))
        {
            TryExecuteAllyConditional(effect);
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("enemy?"))
        {
            TryExecuteEnemyConditional(effect);
            gameManager.CheckGlow(); return false;
        }
        // near other effect.StartsWith checks in ExecuteEffect:
        if (effect.StartsWith("selfbuff"))
        {
            TryExecuteSelfBuff(effect);
            gameManager.CheckGlow(); // keep UI in sync
            return false;
        }
        if (effect.StartsWith("selfheal"))
        {
            TryExecuteSelfHeal(effect);
            gameManager.CheckGlow(); // keep UI in sync
            return false;
        }

        if (effect.StartsWith("addcard"))
        {
            TryExecuteAddCard(effect);
            gameManager.CheckGlow();return false;
        }
        
        if (effect.StartsWith("buff"))
        {
            TryExecuteBuff(effect, null);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("buff") && effect.Contains(",target"))
        {
            BeginTargetedEffect(effect, forceRandomTargetingForCurrentDeploy);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("damageaoe"))
        {
            TryExecuteDamageAoe(effect);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("grantall"))
        {
            TryExecuteGrantAll(effect);
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("damagerandomenemy"))
        {
            TryExecuteDamageRandomEnemy(effect);
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("killrandom") || effect.StartsWith("killlow") || effect.StartsWith("killhigh"))
        {
            TryExecuteKill(effect, null);
            gameManager.CheckGlow(); return false;
        }
        else if (effect.StartsWith("damage"))
        {
            TryExecuteDamage(effect, null);
            gameManager.CheckGlow();return false;
        }

        if (effect.StartsWith("catch") && effect.Contains(",target"))
        {
            BeginTargetedEffect(effect, forceRandomTargetingForCurrentDeploy);
            gameManager.CheckGlow(); return false;
        }

        if (effect.StartsWith("gear"))
        {
            TryExecuteGear(effect, CurrentEffectText, null);
            gameManager.CheckGlow();return false;
        }
        if (effect.StartsWith("maxmanagain"))
        {
            TryExecuteAllyMaxManaGain(effect);
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("managain"))
        {
            TryExecuteManaGain(effect);
            gameManager.CheckGlow();return false;
        }

        if (effect.StartsWith("enemymanaloss"))
        {
            TryExecuteEnemyManaLoss(effect);
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("markethandshift"))
        {
            ApplyMarketCrasherHandShift();
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("investmentspells"))
        {
            ExecuteInvestmentSpellReturn();
            gameManager.CheckGlow(); return false;
        }
        if (effect.StartsWith("investmentunits"))
        {
            ExecuteGamblersInvestmentReturn();
            gameManager.CheckGlow(); return false;
        }
        Debug.LogError($"Unknown effect '{effect}' on card {Data.name}");
        return false;
    }

    private void ResumePendingTriggeredEffects()
    {
        if (pendingTriggeredEffects == null)
            return;

        var queuedEffects = pendingTriggeredEffects;
        int start = pendingTriggeredEffectIndex;
        EffectTrigger? queuedTrigger = pendingTriggeredEffectType;

        pendingTriggeredEffects = null;
        pendingTriggeredEffectType = null;
        pendingTriggeredEffectIndex = 0;

        if (queuedTrigger.HasValue)
            currentResolvingTrigger = queuedTrigger.Value;

        for (int i = start; i < queuedEffects.Count; i++)
        {
            if (ExecuteEffect(queuedEffects[i]))
            {
                pendingTriggeredEffects = queuedEffects;
                pendingTriggeredEffectIndex = i + 1;
                pendingTriggeredEffectType = queuedTrigger;
                currentResolvingTrigger = null;
                return;
            }
        }

        currentResolvingTrigger = null;

        if (CurrentZone == CardZone.Board && !DeployPending)
            FinalizeDeployResolution();
    }
    private void TriggerBerserk()
    {
        TriggerEffects(EffectTrigger.Berserk);
    }
    private void TriggerRequiem()
    {
        TriggerEffects(EffectTrigger.Requiem);
    }
    private void TriggerHeal()
    {
        TriggerEffects(EffectTrigger.Heal);
    }
    /// <summary>
    /// Returns how many same-side board summons this card can create from its DEPLOY trigger.
    /// Used by board-drop legality checks to account for the played card + deploy summons.
    /// </summary>
    public int GetDeployOwnerSummonCount()
    {
        if (!parsedEffects.TryGetValue(EffectTrigger.Deploy, out var effects) || effects == null)
            return 0;

        int count = 0;
        foreach (string effect in effects)
        {
            if (string.IsNullOrWhiteSpace(effect))
                continue;

            // Count only summons that go to this card's owner board.
            if (IsOwnerBoardSummonEffect(effect))
                count++;
        }

        return count;
    }
    public void TriggerStrike()
    {
        TriggerEffects(EffectTrigger.Strike);
    }
    private bool IsOwnerBoardSummonEffect(string effect)
    {
        if (string.IsNullOrWhiteSpace(effect))
            return false;

        if (effect.StartsWith("summon(") && !effect.StartsWith("summonforother("))
            return true;

        return effect.StartsWith("summonrandomcost")
            || effect.StartsWith("summondeck");
    }
    private bool TryParseIntEffect(string effect, string effectName, out int value)
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
                //Debug.LogError($"Unknown trigger '{triggerStr}' on card {Data.name}");
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

        SyncHealSubscription();
        SyncSpellSubscription();
        SyncManaGainSubscription();
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
            case "h": trigger = EffectTrigger.Heal; return true;
            case "eot": trigger = EffectTrigger.EndOfTurn; return true;
            case "sot": trigger = EffectTrigger.StartOfTurn; return true;
            case "spell": trigger = EffectTrigger.SpellCast; return true;
            case "mana": trigger = EffectTrigger.ManaGain; return true;

            // ✅ Progress triggers (parameterized)
            case "progressdraw":
            case "progressheal":
            case "progressmana":
            case "progressattack":
            case "progressdamage":
            case "progresskazuyacombo":
            case "progressspell":
            case "progresseot":
            case "progressbuff":
                trigger = EffectTrigger.ProgressComplete;
                return true;

            case string progressPlayTrigger when progressPlayTrigger.StartsWith("progressplay"):
                trigger = EffectTrigger.ProgressComplete;
                return true;

            default:
            trigger = EffectTrigger.None;
                return false;
        }
    }

    private void BeginTargetedEffect(string effect, bool forceRandomTarget = false)
    {
        Debug.Log($"[TARGET] BeginTargetedEffect called for {effect} | ForceRandom = {forceRandomTarget}");

        EffectTarget type = EffectTarget.None;
        if (effect.ToLower().Contains("targetany")) type = EffectTarget.Any;
        if (effect.ToLower().Contains("targetunit")) type = EffectTarget.Unit;
        if (effect.ToLower().Contains("targetcore")) type = EffectTarget.Core;
        pendingTargetedEffect = effect;

        bool isFriendlyTarget =
            pendingTargetedEffect.StartsWith("gear")
            || pendingTargetedEffect.StartsWith("heal")
            || pendingTargetedEffect.StartsWith("buff")
            || pendingTargetedEffect.StartsWith("refreshattack");

        if (forceRandomTarget)
        {
            PlayerOwner targetOwner = isFriendlyTarget
                ? Owner
                : (Owner == PlayerOwner.Player ? PlayerOwner.Enemy : PlayerOwner.Player);

            bool excludeSleepingUnits = pendingTargetedEffect.StartsWith("sleep");

            IAttackable target = gameManager.ChooseRandomEffectTarget(
                targetOwner,
                type,
                canTargetCore: true,
                excludeSleepingUnits: excludeSleepingUnits
            );

            OnEffectTargetChosen(target);
            return;
        }

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
            IAttackable target = ChooseBestEnemyEffectTarget(type);
            OnEffectTargetChosen(target);
        }
    }

    private IAttackable ChooseBestEnemyEffectTarget(EffectTarget type)
    {
        string effect = pendingTargetedEffect?.ToLowerInvariant() ?? string.Empty;

        if (effect.StartsWith("morphto"))
            return ChooseBestDittoTarget();

        if (effect.StartsWith("sleep"))
            return ChooseSleepTarget();

        if (effect.StartsWith("silence"))
            return ChooseSilenceTarget();

        if (effect.StartsWith("damage"))
            return ChooseDamageTarget(type);

        if (effect.StartsWith("kill"))
            return ChooseKillTarget(type);

        if (effect.StartsWith("gear"))
            return ChooseBestFriendlyUnitTargetForGear();

        if (effect.StartsWith("heal") || effect.StartsWith("buff") || effect.StartsWith("refreshattack"))
            return gameManager.ChooseEnemyEffectTarget(type, false, false);

        return gameManager.ChooseEnemyEffectTarget(type, true, false);
    }

    private CardInstance ChooseBestDittoTarget()
    {
        List<CardInstance> candidates = new();

        candidates.AddRange(gameManager.allyDropArea.GetCards()
            .Select(go => go.GetComponent<CardInstance>())
            .Where(ci => ci != null && !ci.IsDead && ci != this));

        candidates.AddRange(gameManager.enemyDropArea.GetCards()
            .Select(go => go.GetComponent<CardInstance>())
            .Where(ci => ci != null && !ci.IsDead && ci != this));

        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderByDescending(ci => ci.CurrentAttack + ci.CurrentHealth)
            .ThenByDescending(ci => (ci.CurrentEffect ?? string.Empty).Length)
            .FirstOrDefault();
    }

    private CardInstance ChooseSleepTarget()
    {
        return gameManager.GetValidTargets(PlayerOwner.Player)
            .OfType<CardInstance>()
            .Where(ci => !ci.IsAsleep && !ci.IsDead)
            .OrderByDescending(ci => ci.CurrentAttack)
            .ThenByDescending(ci => ci.CurrentHealth)
            .FirstOrDefault();
    }

    private CardInstance ChooseSilenceTarget()
    {
        return gameManager.GetValidTargets(PlayerOwner.Player)
            .OfType<CardInstance>()
            .Where(ci => !ci.IsDead)
            .OrderByDescending(ci => (ci.CurrentEffect ?? string.Empty).Length)
            .ThenByDescending(ci => ci.CurrentAttack + ci.CurrentHealth)
            .FirstOrDefault();
    }

    private IAttackable ChooseKillTarget(EffectTarget type)
    {
        List<IAttackable> validTargets = gameManager.GetValidTargets(PlayerOwner.Player);
        List<CardInstance> enemyUnits = validTargets.OfType<CardInstance>().Where(ci => !ci.IsDead).ToList();

        CardInstance strongestEnemy = enemyUnits
            .OrderByDescending(ci => ci.CurrentAttack + ci.CurrentHealth)
            .ThenByDescending(ci => ci.CurrentAttack)
            .FirstOrDefault();

        if (strongestEnemy != null)
            return strongestEnemy;

        return null;
    }
    private IAttackable ChooseDamageTarget(EffectTarget type)
    {
        List<IAttackable> validTargets = gameManager.GetValidTargets(PlayerOwner.Player);
        List<CardInstance> enemyUnits = validTargets.OfType<CardInstance>().Where(ci => !ci.IsDead).ToList();

        int damageAmount = GetSingleIntFromEffect(pendingTargetedEffect);
        if (damageAmount > 0)
        {
            CardInstance perfectKill = enemyUnits
                .Where(ci => ci.CurrentHealth == damageAmount)
                .OrderByDescending(ci => ci.CurrentAttack)
                .ThenByDescending(ci => ci.CurrentHealth)
                .FirstOrDefault();

            if (perfectKill != null)
                return perfectKill;
        }

        CardInstance strongestEnemy = enemyUnits
            .OrderByDescending(ci => ci.CurrentAttack + ci.CurrentHealth)
            .ThenByDescending(ci => ci.CurrentAttack)
            .FirstOrDefault();

        if (strongestEnemy != null)
            return strongestEnemy;

        if (type == EffectTarget.Any || type == EffectTarget.Core)
            return gameManager.PlayerCore;

        return null;
    }

    private CardInstance ChooseBestFriendlyUnitTargetForGear()
    {
        List<CardInstance> friendlyUnits = gameManager.GetValidTargets(PlayerOwner.Enemy)
            .OfType<CardInstance>()
            .Where(ci => !ci.IsDead)
            .ToList();

        if (friendlyUnits.Count == 0)
            return null;

        string effect = pendingTargetedEffect?.ToLowerInvariant() ?? string.Empty;
        string[] gatedKeywords = { "protect", "quickstrike", "charge", "haste", "thorns", "blessed" };
        List<string> grantedKeywords = gatedKeywords.Where(k => effect.Contains(k)).ToList();

        IEnumerable<CardInstance> candidates = friendlyUnits;
        if (grantedKeywords.Count > 0)
        {
            candidates = friendlyUnits.Where(unit =>
                grantedKeywords.Any(keyword => !unit.HasKeyword(keyword))
            );
        }

        return candidates
            .OrderByDescending(ci => ci.CurrentAttack + ci.CurrentHealth)
            .FirstOrDefault() ?? friendlyUnits.OrderByDescending(ci => ci.CurrentAttack + ci.CurrentHealth).FirstOrDefault();
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
        else if (pendingTargetedEffect.StartsWith("kill"))
        {
            if (target != null)
            {
                TryExecuteKill(pendingTargetedEffect, target);
                executed = true;
            }
        }
        else if (pendingTargetedEffect.StartsWith("catch"))
        {
            if (target != null)
            {
                TryExecuteCatch(pendingTargetedEffect, target);
                executed = true;
            }
        }
        else if (pendingTargetedEffect.StartsWith("refreshattack") && target is CardInstance refresh)
        {
            TryRefreshAttack(refresh);
            executed = true;
        }
        else if (pendingTargetedEffect.StartsWith("absorb") && target is CardInstance absorbed)
        {
            TryExecuteAbsorb(pendingTargetedEffect, target);
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
            executed = TryExecuteDitto(morphTarget);
        }

        // 🔑 ONLY clear targeting if something actually happened
        if (executed)
        {
            pendingTargetedEffect = null;
            DeployPending = false;

            gameManager.EndEffectTargetting();
            ResumePendingTriggeredEffects();
        }

        return executed;
    }

    #endregion

    #region Effects
    void GainExtraTurn()
    {
        TurnManager.Instance.endButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "REWIND";
        if (Owner == PlayerOwner.Player) TurnManager.Instance.PlayerHasExtraTurn = true;
        else TurnManager.Instance.EnemyHasExtraTurn = true;
    }
    void SkipNextEnemyDraw()
    {
        if (Owner == PlayerOwner.Player) TurnManager.Instance.EnemySkipsNextDraw = true;
        else TurnManager.Instance.PlayerSkipsNextDraw = true;
    }

    private void ApplyMarketCrasherHandShift()
    {
        HandManager friendlyHand = Owner == PlayerOwner.Player ? gameManager.allyHand : gameManager.enemyHand;
        HandManager enemyHand = Owner == PlayerOwner.Player ? gameManager.enemyHand : gameManager.allyHand;

        foreach (GameObject go in friendlyHand.handCards)
        {
            CardInstance card = go.GetComponent<CardInstance>();
            if (card != null)
                card.AddTemporaryManaModifier(1);
        }

        foreach (GameObject go in enemyHand.handCards)
        {
            CardInstance card = go.GetComponent<CardInstance>();
            if (card != null)
                card.AddTemporaryManaModifier(1);
        }
    }

    private void ExecuteInvestmentSpellReturn()
    {
        gameManager.DiscardCardsFromHandWithDeferredReturn(
            owner: Owner,
            discardPredicate: card => card.Data != null && card.Data.cardType == "spell",
            turnsUntilReturn: 2,
            manaModifier: -3,
            attackBonus: 0,
            healthBonus: 0
        );
    }

    private void ExecuteGamblersInvestmentReturn()
    {
        gameManager.DiscardCardsFromHandWithDeferredReturn(
            owner: Owner,
            discardPredicate: card => card.Data != null && card.Data.cardType == "minion",
            turnsUntilReturn: 2,
            manaModifier: 0,
            attackBonus: 4,
            healthBonus: 4
        );
    }
    // Add this helper near other TryExecuteXxx methods
    private void TryExecuteSelfBuff(string effect)
    {
        // effect expected like: selfbuff(5) or selfbuff(2,2)
        int open = effect.IndexOf('(');
        int close = effect.IndexOf(')');
        if (open < 0 || close <= open)
        {
            Debug.LogWarning($"Malformed selfbuff effect '{effect}' on {Data.name}");
            return;
        }

        string inner = effect.Substring(open + 1, close - open - 1).Trim();
        if (string.IsNullOrEmpty(inner))
        {
            Debug.LogWarning($"Empty selfbuff parameters '{effect}' on {Data.name}");
            return;
        }

        string[] parts = inner.Split(',');
        // SELFBUFF(total) => random split total into atk/hp
        if (parts.Length == 1 && int.TryParse(parts[0].Trim(), out int totalStats))
        {
            int newAtk = UnityEngine.Random.Range(0, totalStats + 1);
            int newHp = totalStats - newAtk;

            ModifyStats(newAtk, newHp);
            Debug.Log($"[EOT] selfbuff({totalStats}) applied to {Data.name}: +{newAtk}/+{newHp}");
            return;
        }

        // SELFBUFF(atk,hp)
        if (parts.Length == 2
            && int.TryParse(parts[0].Trim(), out int atk)
            && int.TryParse(parts[1].Trim(), out int hp))
        {
            ModifyStats(atk, hp);
            Debug.Log($"[EOT] selfbuff({atk},{hp}) applied to {Data.name}: +{atk}/+{hp}");
            return;
        }

        Debug.LogWarning($"Invalid selfbuff parameters '{effect}' on {Data.name}");
    }

    private void TryExecuteSelfHeal(string effect)
    {
        int open = effect.IndexOf('(');
        int close = effect.IndexOf(')');
        if (open < 0 || close <= open)
        {
            Debug.LogWarning($"Malformed selfbuff effect '{effect}' on {Data.name}");
            return;
        }

        string inner = effect.Substring(open + 1, close - open - 1).Trim();
        if (string.IsNullOrEmpty(inner))
        {
            Debug.LogWarning($"Empty selfbuff parameters '{effect}' on {Data.name}");
            return;
        }

        string[] parts = inner.Split(',');
        // SELFBUFF(total) => random split total into atk/hp
        if (parts.Length == 1 && int.TryParse(parts[0].Trim(), out int totalStats))
        {
            Heal(totalStats);
            return;
        }
    }
    private void TryExecuteSummon(string effect)
    {
        if (!TryParseIntEffect(effect, "summon", out int cardId))
            return;
        
        if (effect.StartsWith("summoncopy("))
        { gameManager.TrySummonForOwner(Owner, Data.id, setAtk: cardId, setHp: cardId); return; }
        // DEPLOY is pre-validated at play time. If there are fewer slots than declared
        // summons, consume only the allowed summon budget and skip the rest.
        if (currentResolvingTrigger == EffectTrigger.Deploy
            && effect.StartsWith("summon(")
            && !gameManager.ConsumeDeploySummonSlot(this))
            return;

        if (effect.StartsWith("summonforother"))
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

    private void TryExecuteEnemyConditional(string effect)
    {
        // Preserve targeting suffix (e.g. ,targetunit)
        string targetSuffix = "";
        int targetIndex = effect.IndexOf(",target");
        if (targetIndex >= 0)
        {
            targetSuffix = effect.Substring(targetIndex);
            effect = effect.Substring(0, targetIndex);
        }

        // Format: enemy?(19:buff(2,2);buff(1,1))

        int open = effect.IndexOf('(');
        int close = effect.LastIndexOf(')');

        if (open < 0 || close <= open)
        {
            Debug.LogError($"Malformed enemy? effect '{effect}'");
            return;
        }

        string inner = effect.Substring(open + 1, close - open - 1);
        string[] parts = inner.Split(':');

        if (parts.Length != 2)
        {
            Debug.LogError($"Malformed enemy? condition '{effect}'");
            return;
        }

        if (!int.TryParse(parts[0], out int enemyId))
        {
            Debug.LogError($"Invalid enemy id in '{effect}'");
            return;
        }

        string[] outcomes = parts[1].Split(';');
        if (outcomes.Length != 2)
        {
            Debug.LogError($"enemy? requires two outcomes '{effect}'");
            return;
        }

        bool enemyExists = gameManager.BoardHasCard(OtherPlayer(Owner), enemyId);

        string chosenEffect = enemyExists ? outcomes[0] : outcomes[1];

        string finalEffect = chosenEffect.Trim() + targetSuffix;

        ExecuteEffect(finalEffect);

        return;
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
    private void TryExecuteEnemyManaLoss(string effect)
    {
        if (!TryParseIntEffect(effect, "enemymanaloss", out int mana))
            return;
        else
            gameManager.EnemyMaxManaLoss(mana, Owner);
    }
    private void TryExecuteAllyMaxManaGain(string effect)
    {
        if (!TryParseIntEffect(effect, "maxmanagain", out int mana))
            return;
        else
            gameManager.GainMaxMana(mana, Owner);
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

        // ✅ NEW: discovereffectdiscount(effectName,discount)
        if (effect.StartsWith("discovereffectdiscount"))
        {
            int startp = effect.IndexOf('(');
            int endp = effect.IndexOf(')');

            if (startp < 0 || endp < 0 || endp <= startp + 1)
            {
                Debug.LogError($"Malformed {effect} effect on card {Data.name}");
                return;
            }

            string valueStr = effect.Substring(startp + 1, endp - startp - 1);
            string[] parts = valueStr.Split(',');

            if (parts.Length != 2)
            {
                Debug.LogError($"Malformed {effect} effect on card {Data.name}");
                return;
            }

            string discoveryeffect = parts[0];

            if (!int.TryParse(parts[1], out int discount))
            {
                Debug.LogError($"Invalid discount in {effect} on card {Data.name}");
                return;
            }

            GameManager.Instance.DiscoverEffectDiscount(discoveryeffect, Owner, discount);
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
            {
                //Test for three different effects ? 
                (string e1, string e2, string e3) = GetThreeStringsFromEffect(effect);
                if (e1 == "" || e2 == "" || e3 == "")
                    gameManager.DiscoverEffect(valueStr, Owner); 
                else
                    gameManager.DiscoverEffect(e1,e2,e3, Owner);
            }
        }
        else { gameManager.DiscoverOwnerTrait(Owner); }
    }
    private void TryExecuteAddCard(string effect)
    {
        // handle non-parameterized variants first
        if (effect.StartsWith("addcardrandomspell"))
        {
            gameManager.AddRandomCardToHandType(Owner, "spell", Data.id);
            return;
        }

        if (effect.StartsWith("addcardrandomnonpackable"))
        {
            gameManager.AddRandomCardNonPackable(Owner, Data.id);
            return;
        }

        if (effect.StartsWith("addcardrandomunit"))
        {
            gameManager.AddRandomCardToHandType(Owner, "minion", Data.id);
            return;
        }

        if (effect.StartsWith("addcardrandomtext"))
        {
            int start = effect.IndexOf('(');
            int end = effect.IndexOf(')');

            if ((start < 0 || end < 0 || end <= start + 1))
            {
                Debug.LogError($"Malformed {effect} addcardrandomtext on card {Data.name}");
                return;
            }

            string valueStr = effect.Substring(start + 1, end - start - 1);
            Debug.Log("Added random card with the following text : " + valueStr);
            gameManager.AddRandomCardToHandText(Owner, valueStr, Data.id);
            return;
        }
        if (effect.StartsWith("addcardrandomtrait"))
        {
            int start = effect.IndexOf('(');
            int end = effect.IndexOf(')');

            if ((start < 0 || end < 0 || end <= start + 1))
            {
                Debug.LogError($"Malformed {effect} addcardrandomtrait on card {Data.name}");
                return;
            }

            string valueStr = effect.Substring(start + 1, end - start - 1);
            Debug.Log("Added random card with the following text : " + valueStr);
            gameManager.AddRandomCardToHandTrait(Owner, valueStr, Data.id);
            return;
        }
        (int id, int discount) = GetTwoIntsFromEffect(effect);
        if ( id>=0 && discount > 0 && effect.Contains("addcarddiscount"))
        {
            gameManager.AddCardToHand(Owner, id, -discount);
        }

        // now try to parse addcard(<id>)
        if (!TryParseIntEffect(effect, "addcard", out int cardId))
            return;

        if (effect.StartsWith("addcardenemy"))
        {
            if (Owner == PlayerOwner.Player)
                gameManager.AddCardToHand(PlayerOwner.Enemy, cardId);
            else
                gameManager.AddCardToHand(PlayerOwner.Player, cardId);

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
        {// SELFBUFF(x)
            if (int.TryParse(stats[0], out int totalStats))
            {
                int newAtk = UnityEngine.Random.Range(0, totalStats + 1);
                int newHp = totalStats - newAtk;

                if (target is CardInstance inst)
                    inst.ModifyStats(newAtk, newHp);
                return;
            }
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
        if (effect.StartsWith("buffdiscovertrait") && target is CardInstance cardinst)
        {
            gameManager.DiscoverTrait(cardinst.Data.traits[0], Owner);
        }
        //Update view
        view.hpTextBoard.text = CurrentHealth.ToString();
        UpdateStatsColor();
    }
    private void TryExecuteDamage(string effect, IAttackable target)
    {
        if(effect.StartsWith("damagenheal")){
            (int atk, int hp) = GetTwoIntsFromEffect(effect);

            target.TakeDamage(atk);
            gameManager.OnDamageWithCard(Owner);
            target.Heal(hp);
            return;
        }

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
    private void TryExecuteKill(string effect, IAttackable target)
    {
        if (effect.StartsWith("killrandom"))
        {
            if (!TryParseIntEffect(effect, "killrandom", out int killCount))
                return;

            if (killCount <= 0)
                return;

            List<CardInstance> enemyUnits = GetLivingUnitsOnBoard(Owner == PlayerOwner.Player ? PlayerOwner.Enemy : PlayerOwner.Player);
            killCount = Mathf.Min(killCount, enemyUnits.Count);

            for (int i = 0; i < killCount; i++)
            {
                if (enemyUnits.Count == 0)
                    break;

                int randomIndex = UnityEngine.Random.Range(0, enemyUnits.Count);
                CardInstance randomTarget = enemyUnits[randomIndex];
                enemyUnits.RemoveAt(randomIndex);

                if (randomTarget != null && !randomTarget.IsDead)
                    Kill(randomTarget);
            }
            return;
        }

        if (effect.StartsWith("killlow") || effect.StartsWith("killhigh"))
        {
            List<CardInstance> enemyUnits = GetLivingUnitsOnBoard(Owner == PlayerOwner.Player ? PlayerOwner.Enemy : PlayerOwner.Player);
            if (enemyUnits.Count == 0)
                return;

            CardInstance selectedTarget = effect.StartsWith("killlow")
                ? enemyUnits.OrderBy(ci => ci.CurrentAttack + ci.CurrentHealth).ThenBy(ci => ci.CurrentHealth).FirstOrDefault()
                : enemyUnits.OrderByDescending(ci => ci.CurrentAttack + ci.CurrentHealth).ThenByDescending(ci => ci.CurrentAttack).FirstOrDefault();

            if (selectedTarget != null && !selectedTarget.IsDead)
                Kill(selectedTarget);
            return;
        }

        if (target == null ||target is not CardInstance inst)
        {
            Debug.LogError($"Kill effect requires a target on {Data.name}");
            return;
        }

        Kill(inst);
    }
    private void TryExecuteCatch(string effect, IAttackable target)
    {
        if (!TryParseIntEffect(effect, "catch", out int maxStats))
            return;

        if (target == null)
        {
            Debug.LogError($"Damage effect requires a target on {Data.name}");
            return;
        }
        if(target is CardInstance cardInst)
        {
            if (cardInst.CurrentAttack + cardInst.CurrentHealth > maxStats)
                return;

            gameManager.AddCardToHand(Owner, cardInst.Data.id);
            Kill(cardInst);
            //gameManager.AddPokemonTraitProgress(Owner, 1);
        }

    }
    private void TryExecuteDamageRandomEnemy(string effect)
    {
        if (!TryParseIntEffect(effect, "damagerandomenemy", out int amount))
        {
            if (effect.Contains("atk"))
            {
                PlayerOwner enemyOwneratk = Owner == PlayerOwner.Player ? PlayerOwner.Enemy : PlayerOwner.Player;
                IAttackable targetatk = gameManager.ChooseRandomEffectTarget(enemyOwneratk, EffectTarget.Any, canTargetCore: true);
                if (targetatk == null)
                    return;
                targetatk.TakeDamage(CurrentAttack);
                gameManager.OnDamageWithCard(Owner);
                return;
            }
            else return;
        }
        PlayerOwner enemyOwner = Owner == PlayerOwner.Player ? PlayerOwner.Enemy : PlayerOwner.Player;
        IAttackable target = gameManager.ChooseRandomEffectTarget(enemyOwner, EffectTarget.Any, canTargetCore: true);
        if (target == null)
            return;

        target.TakeDamage(amount);
        gameManager.OnDamageWithCard(Owner);
    }
    private void TryExecuteAbsorb(string effect, IAttackable target)
    {
        if (!TryParseIntEffect(effect, "absorb", out int amount))
            return;
        if (target == null)
        {
            List<GameObject> enemies = gameManager.GetBoardForOther(Owner).GetCards();
            if (enemies.Count <= 0) return;
            CardInstance targetinst = enemies[UnityEngine.Random.Range(0, enemies.Count)].GetComponent<CardInstance>();
            int hpDiff = Mathf.Min(targetinst.CurrentHealth, amount);
            int atkDiff = Mathf.Min(targetinst.CurrentAttack, amount);
            targetinst.ModifyStats(-amount, -amount);
            ModifyStats(atkDiff, hpDiff);
            return;
        }
        if (target is CoreInstance)
        {
            Debug.LogError($"Absorb effect requires a target on {Data.name} or is core");
            return;
        }
        if (target is CardInstance targetInst)
        {
            int hpDiff = Mathf.Min(targetInst.CurrentHealth, amount);
            int atkDiff = Mathf.Min(targetInst.CurrentAttack, amount);
            targetInst.ModifyStats(-amount, -amount);
            ModifyStats(atkDiff, hpDiff);
            return;
        }
        //Buff self
    }
    private void TryExecuteConsume(string effect)
    {
        List<CardInstance> candidates;

        if (effect.StartsWith("consumeany"))
        {
            candidates = GetLivingUnitsOnBoard(null, excludeSelf: true);
        }
        else if (effect.StartsWith("consumetrait"))
        {
            string trait = GetStringValueFromEffect(effect, "consumetrait");
            if (string.IsNullOrWhiteSpace(trait))
                return;

            candidates = GetLivingUnitsOnBoard(null, excludeSelf: true)
                .Where(ci => ci.HasTrait(trait))
                .ToList();
        }
        else if (effect.StartsWith("consume"))
        {
            string requiredEffect = GetStringValueFromEffect(effect, "consume");
            if (string.IsNullOrWhiteSpace(requiredEffect))
                return;

            candidates = GetLivingUnitsOnBoard(null, excludeSelf: true)
                .Where(ci => !string.IsNullOrWhiteSpace(ci.CurrentEffect)
                             && ci.CurrentEffect.IndexOf(requiredEffect, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }
        else
        {
            return;
        }

        if (candidates.Count == 0)
            return;

        CardInstance consumed = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        if (consumed == null || consumed.IsDead)
            return;

        int gainedAtk = consumed.CurrentAttack;
        int gainedHp = consumed.CurrentHealth;
        string gainedEffect = consumed.CurrentEffect;
        string gainedEffectText = consumed.CurrentEffectText;

        Kill(consumed);

        if (!IsDead)
        {
            ModifyStats(gainedAtk, gainedHp);
            CurrentEffect = AppendToken(CurrentEffect, gainedEffect);
            CurrentEffectText = AppendToken(CurrentEffectText, gainedEffectText, separator: "\n");
            ParseEffects();
            view.UpdateMode();
        }
    }

    private List<CardInstance> GetLivingUnitsOnBoard(PlayerOwner? ownerFilter = null, bool excludeSelf = false)
    {
        List<CardInstance> result = new();

        IEnumerable<GameObject> allCards = gameManager.allyDropArea.GetCards().Concat(gameManager.enemyDropArea.GetCards());
        foreach (GameObject go in allCards)
        {
            if (go == null)
                continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead)
                continue;
            if (excludeSelf && ci == this)
                continue;
            if (ownerFilter.HasValue && ci.Owner != ownerFilter.Value)
                continue;

            result.Add(ci);
        }

        return result;
    }

    private string GetStringValueFromEffect(string effect, string effectName)
    {
        if (!effect.StartsWith(effectName))
            return string.Empty;

        int start = effect.IndexOf('(');
        int end = effect.LastIndexOf(')');
        if (start < 0 || end <= start)
            return string.Empty;

        return effect.Substring(start + 1, end - start - 1).Trim();
    }

    private static string AppendToken(string current, string addition, string separator = " ")
    {
        if (string.IsNullOrWhiteSpace(addition))
            return current ?? string.Empty;
        if (string.IsNullOrWhiteSpace(current))
            return addition.Trim();

        string trimmedCurrent = current.Trim();
        string trimmedAddition = addition.Trim();
        if (trimmedCurrent.IndexOf(trimmedAddition, StringComparison.OrdinalIgnoreCase) >= 0)
            return trimmedCurrent;

        return $"{trimmedCurrent}{separator}{trimmedAddition}";
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
    private void TryExecuteGrantAll(string completeeffect)
    {
        string args = ExtractParenthesizedArgs(completeeffect);
        if (string.IsNullOrWhiteSpace(args))
            return;

        string[] parts = args.Split(',');
        if (parts.Length < 2)
            return;

        string effect = parts[0].Trim();
        string effectText = parts[1].Trim();
        if (string.IsNullOrWhiteSpace(effect))
            return;

        List<GameObject> board = Owner != PlayerOwner.Player
            ? gameManager.enemyDropArea.enemyPrefabCards
            : gameManager.allyDropArea.allyPrefabCards;

        foreach (GameObject unitGO in board)
        {
            if (unitGO == null) continue;

            CardInstance ci = unitGO.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead) continue;

            ci.CurrentEffect = AppendToken(ci.CurrentEffect, effect);
            ci.CurrentEffectText = AppendToken(ci.CurrentEffectText, effectText, "\n");
            ci.view.UpdateMode();
        }
    }
    private string ExtractParenthesizedArgs(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        int start = input.IndexOf('(');
        if (start < 0)
            return string.Empty;

        int depth = 0;

        for (int i = start; i < input.Length; i++)
        {
            if (input[i] == '(')
                depth++;
            else if (input[i] == ')')
            {
                depth--;
                if (depth == 0)
                {
                    // Return content inside the outermost parentheses
                    return input.Substring(start + 1, i - start - 1).Trim();
                }
            }
        }

        return string.Empty; // no matching closing parenthesis
    }
    private void TryExecuteSleep(CardInstance target)
    {
        if (target == null)
        {
            Debug.LogError($"Sleep effect requires a target on {Data.name}");
            return;
        }
        target.IsAsleep = true; target.view.UpdateMode();
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
    public void TryExecuteAdvanceProgress()
    {
        if (ProgressionCap > 0)
        {
            ProgressionCounter++;
            cardView.ShowProgress(ProgressionCounter, ProgressionCap);

            CheckProgressCompletion();
        }
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
        // TRIGGER BLOCKS (eot[...], s[...], r[...], etc.)
        int bracketIndex = subEffect.IndexOf('[');
        if (bracketIndex > 0 && subEffect.EndsWith("]"))
        {
            target.CurrentEffect += " " + subEffect;
            return;
        }
        // KEYWORD (quickstrike, taunt, etc.)
        if (!subEffect.Contains("("))
        {
            target.CurrentEffect += " " + subEffect;

            // NOTE: quickstrike allows attacking UNITS while summoning sick.
            // It should never remove summoning sickness, otherwise it behaves like charge.
            // Keep IsSummoningSick unchanged here.

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

        if (subEffect.StartsWith("selfbuff"))
        {
            // Extract everything inside parentheses
            int open = subEffect.IndexOf('(');
            int close = subEffect.IndexOf(')');

            if (open == -1 || close == -1 || close <= open + 1)
            {
                Debug.LogWarning($"Invalid selfbuff format '{subEffect}' on {Data.name}");
                return;
            }

            string[] parts = subEffect
                .Substring(open + 1, close - open - 1)
                .Split(',');

            // SELFBUFF(x)
            if (parts.Length == 1 && int.TryParse(parts[0], out int totalStats))
            {
                int newAtk = UnityEngine.Random.Range(0, totalStats + 1);
                int newHp = totalStats - newAtk;

                target.ModifyStats(newAtk, newHp);
                target.CurrentEffect += " " + subEffect;
                return;
            }

            // SELFBUFF(a,b)
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int atk) &&
                int.TryParse(parts[1], out int hp))
            {
                target.ModifyStats(atk, hp);
                target.CurrentEffect += " " + subEffect;
                return;
            }

            // Anything else
            Debug.LogWarning($"Invalid selfbuff parameters '{subEffect}' on {Data.name}");
            return;
        }

        Debug.LogWarning($"Unknown gear sub-effect '{subEffect}' on {Data.name}");


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
            return (-1, -1);

        int.TryParse(parts[0], out int a);
        int.TryParse(parts[1], out int b);
        return (a, b);
    }
    private (string, string, string) GetThreeStringsFromEffect(string effect)
    {
        int start = effect.IndexOf('(');
        int end = effect.IndexOf(')');
        if (start < 0 || end <= start + 1)
            return ("", "", "");

        string[] parts = effect.Substring(start + 1, end - start - 1).Split(',');
        if (parts.Length != 3)
            return ("", "", "");

        return (parts[0], parts[1], parts[2]);
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
    public IEnumerator TriggerBensEffect()
    {
        int randCount = 0;

        if (Owner == PlayerOwner.Player)
            randCount = GameManager.Instance.PlayerRandomCount;
        else
            randCount = GameManager.Instance.EnemyRandomCount;

        if (randCount < 1)
            yield break;

        GameManager.Instance.BeginEffect();
        try
        {
            for (int i = 0; i < randCount; i++)
            {
                // ⛔ Stop if a player is already dead
                if (GameManager.Instance.CurrentGameState != GameState.Playing)
                    yield break;

                yield return StartCoroutine(
                    TurnManager.Instance.TriggerSingleChaosEvent(Owner)
                );

                // ⛔ Stop immediately if this chaos event killed someone
                if (GameManager.Instance.CurrentGameState != GameState.Playing)
                    yield break;
            }
        }
        finally
        {
            GameManager.Instance.EndEffect();
        }
    }

    public void MorphTo(int newCardId)
    {
        CardData newData = CardDatabase.Instance.GetCardById(newCardId);
        if (newData == null || newCardId == Data.id || CurrentHealth <= 0)
        {
            Debug.Log($"Evolve failed");
            return;
        }

        // Preserve runtime-only additions (ex: extra gear effects / injected rules text)
        // before changing Data.
        string runtimeAddedEffects = ExtractRuntimeAddedEffects(CurrentEffect, Data.effect);
        string runtimeAddedEffectText = ExtractRuntimeAddedEffectText(CurrentEffectText, Data.effectText);

        // Preserve temporary stat gains (buffs) relative to the original base card.
        int bonusAttack = Mathf.Max(0, CurrentAttack - Data.atkValue);
        int bonusMaxHealth = Mathf.Max(0, CurrentMaxHealth - Data.hpValue);
        int currentDamage = Mathf.Max(0, CurrentMaxHealth - CurrentHealth);

        // Swap data
        Data = newData;

        CurrentAttack = newData.atkValue + bonusAttack;
        CurrentMaxHealth = newData.hpValue + bonusMaxHealth;
        CurrentHealth = Mathf.Max(1, CurrentMaxHealth - currentDamage);
        BaseManaCost = newData.manaCost;

        CurrentEffect = string.IsNullOrWhiteSpace(runtimeAddedEffects)
            ? newData.effect
            : $"{newData.effect} {runtimeAddedEffects}";

        CurrentEffectText = string.IsNullOrWhiteSpace(runtimeAddedEffectText)
            ? newData.effectText
            : $"{newData.effectText}{runtimeAddedEffectText}";

        // Notify view
        cardView.UpdateMode();
        StartCoroutine(DelayedProgressInit());

        // 🔑 Reset deploy eligibility
        WasPlayed = true;
        EffectsSuppressed = false;

        // Re-parse effects for morphed card + preserved runtime additions
        ParseEffects();

        // Trigger deploy next frame
        StartCoroutine(DelayedDeployAfterMorph());
    }
    private string ExtractRuntimeAddedEffects(string runtimeEffect, string baseEffect)
    {
        runtimeEffect ??= string.Empty;
        baseEffect ??= string.Empty;

        List<string> runtimeTokens = runtimeEffect
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        List<string> baseTokens = baseEffect
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        Dictionary<string, int> baseCount = new();
        foreach (string token in baseTokens)
        {
            if (!baseCount.ContainsKey(token))
                baseCount[token] = 0;

            baseCount[token]++;
        }

        List<string> additions = new();

        foreach (string token in runtimeTokens)
        {
            if (baseCount.TryGetValue(token, out int count) && count > 0)
            {
                baseCount[token] = count - 1;
                continue;
            }

            additions.Add(token);
        }

        return string.Join(" ", additions).Trim();
    }

    private string ExtractRuntimeAddedEffectText(string runtimeText, string baseText)
    {
        if (string.IsNullOrEmpty(runtimeText))
            return string.Empty;

        if (string.IsNullOrEmpty(baseText))
            return "\n" + runtimeText;

        if (runtimeText.StartsWith(baseText))
            return runtimeText.Substring(baseText.Length);

        // Fallback if text was edited in another way: keep non-duplicate content.
        if (runtimeText == baseText)
            return string.Empty;

        return "\n" + runtimeText;
    }

    private IEnumerator DelayedDeployAfterMorph()
    {
        yield return null; // wait one frame
        TriggerDeploy();
    }

    private bool TryExecuteDitto(CardInstance target)
    {
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
            foreach (GameObject ally in gameManager.allyDropArea.allyPrefabCards)
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
                inst.IsAsleep = true; inst.view.UpdateMode();
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
        gameManager.CheckGlow();
    }
    public void SilenceAll()
    {
        if (Owner == PlayerOwner.Enemy)
        {
            foreach (GameObject ally in gameManager.allyDropArea.allyPrefabCards)
            {
                CardInstance inst = ally.GetComponent<CardInstance>();
                inst.Silence();inst.UpdateStatsColor();
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
        CurrentAttack = Data.atkValue; CurrentHealth = Mathf.Min(CurrentHealth, Data.hpValue);
        ThornsDamage = 0;

        //Update display
        view.UpdateMode();
        TurnManager tm = FindFirstObjectByType<TurnManager>(); tm.UpdateGlow();

        UpdateStatsColor();
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
    public void SelfDestroy()
    {
        TakeDamage(999);
    }
    #endregion
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        if (HasKeyword("blessed") && amount > 0)
        {
            RemoveEffect("blessed");
            UpdateStatsColor();
            return;
        }
        CurrentHealth -= amount;
        gameManager.NotifyDamage(Owner, amount);
        SFXManager.Instance.PlaySFXClip(gameManager.dmgSFX, transform, 1f);

        //Trigore Logic :
        CardInstance trigore = gameManager.GetBoardForOwner(Owner).BoardHasEffect("trigore");
        if (trigore!=null)
        {
            //Boost trigore
            trigore.ModifyStats(0, 2);
        }
        GetComponent<DamageFeedback>().Play();

        if (CurrentHealth <= 0 && !IsDead)
        {
            if (HasKeyword("deathless") && !HasKeyword("deathlessused"))
            {
                CurrentHealth = 1;
                CurrentEffect = CurrentEffect.Replace("deathless", "deathlessused");
                UpdateStatsColor();
                return;
            }
            IsDying = true;
            Die();
            return;
        }

        DamageVFXManager.Instance.PlayRandomHit(cardView.transform.position);

        TriggerBerserk();
        UpdateStatsColor();
        gameManager.CheckGlow();
    }
    public void Kill(CardInstance target)
    {
        target.IsDying = true;
        target.Die();
        if(HasTrait("Pokemon"))
            gameManager.AddPokemonTraitProgress(Owner, 1);
        return;
    }
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        int bonus = 0;
        int preHeal = CurrentHealth;

        SFXManager.Instance.PlaySFXClip(gameManager.healSFX, transform, 1f);
        //ApplyBonus
        if (Owner == PlayerOwner.Player) bonus = gameManager.PlayerHealBonus;
        else bonus = gameManager.EnemyHealBonus;
        int totalHeal = amount + bonus;
        CurrentHealth = Mathf.Min(CurrentHealth + totalHeal, CurrentMaxHealth);
        int differenceHp = CurrentHealth - preHeal;
        int overhealAmount = Mathf.Max(0, totalHeal - differenceHp);

        view.hpTextBoard.text = CurrentHealth.ToString();
        UpdateStatsColor();

        gameManager.NotifyHealed(Owner, differenceHp);
        gameManager.NotifyHealResolved(Owner, this, differenceHp, overhealAmount);

        if(CurrentHealth==CurrentMaxHealth && CurrentEffect.ToLower().Contains("deathlessused"))
        {
            //Refresh deathless
            CurrentEffect = CurrentEffect.Replace("deathlessused", "deathless");
        }
    }
    internal void ModifyStats(int atk, int hp)
    {
        CurrentAttack += atk;
        CurrentHealth += hp;
        CurrentMaxHealth += hp;
        if (CurrentAttack < 0) CurrentAttack = 0;
        if (CurrentHealth <= 0)
        {
            Die();
        }
        view.UpdateMode();
        UpdateStatsColor();
        UpdateBuffProgressCounter();
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

        if (gameManager != null)
            gameManager.OnCardPlayed -= OnCardPlayed;

        if (gameManager != null)
            gameManager.OnOwnerManaGain -= OnManaGained;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnded -= OnEndTurn;

        if (deckManager != null)
            deckManager.OnCardDrawn -= OnCardDrawn;

        if (CurrentZone != CardZone.Board)
            return;
        gameManager.NotifyCardKilled(this);

        if (Owner == PlayerOwner.Player)
        {
            AllyCardDropArea board = FindFirstObjectByType<AllyCardDropArea>();
            if (board != null)
                board.RemoveAllyCardFromBoard(this);
        }
        else
        {
            EnemyCardDropArea board = FindFirstObjectByType<EnemyCardDropArea>();
            if (board != null)
                board.RemoveEnemyCardFromBoard(this);
        }

        TriggerRequiem();

        Destroy(gameObject);

        IsDead = true;
        IsDying = false;
        SetZone(CardZone.Graveyard);
    }
    void UpdateStatsColor()
    {
        Color manaColor = Color.white;
        if (CurrentManaCost > BaseManaCost)
            manaColor = Color.red;
        else if (CurrentManaCost < BaseManaCost)
            manaColor = Color.green;

        view.manaText.color = manaColor;
        view.manaTextBoard.color = manaColor;
        if (CurrentManaCost>BaseManaCost) view.manaText.color = Color.red;
        if(CurrentManaCost<BaseManaCost) view.manaText.color = Color.green;
        if (CurrentHealth < CurrentMaxHealth) view.hpTextBoard.color = Color.red;
        else if (CurrentHealth > Data.hpValue) view.hpTextBoard.color = Color.green;
        else if (CurrentHealth == Data.hpValue) view.hpTextBoard.color = Color.white;
        if (CurrentAttack > Data.atkValue) view.atkTextBoard.color = Color.green;

        view.UpdateMode();
    }
}
