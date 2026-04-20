using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using System.Linq;
using UnityEngine.Rendering;
using System;
using System.Text.RegularExpressions;
using System.Reflection;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public interface IAttackable
{
    Transform Transform{ get; } 
    PlayerOwner Owner { get; }
    int CurrentAttack { get; }
    string CurrentEffect { get; set; }
    void TakeDamage(int amount);
    void Heal(int amount);
}
public enum GameState
{
    Playing,
    PlayerWon,
    PlayerLost
}

public class GameManager : MonoBehaviour
{
    private const int LossGoldCompensation = 20;
    private const int WinGoldReward = 100;
    private const int DungeonWinCoinReward = 30;
    private const float AdventureSecondPhaseDelaySeconds = 3f;

    public static GameManager Instance;

    [Header("Core")]
    public CoreInstance PlayerCore;
    public CoreInstance EnemyCore;
    [SerializeField] private GameObject corePrefab;
    [SerializeField] private int startingCoreHealth = 50;
    [SerializeField] private GameObject spawnPlayerCore;
    [SerializeField] private GameObject spawnEnemyCore;

    [Header("SFX")]
    [SerializeField] public AudioClip healSFX;
    [SerializeField] public AudioClip dmgSFX;
    [SerializeField] public AudioClip fahSFX;
    [SerializeField] public List<AudioClip> winSFX;
    public GameState CurrentGameState { get; private set; } = GameState.Playing;
    private int startingPlayerCoreHealth = 50;
    private int startingEnemyCoreHealth = 50;

    [Header("Deck and Board")]
    [SerializeField] public DeckManager deckManager;
    [SerializeField] public AllyCardDropArea allyDropArea;
    [SerializeField] public EnemyCardDropArea enemyDropArea;
    [SerializeField] public HandManager allyHand;
    [SerializeField] public HandManager enemyHand;

    [Header("Mana")]
    [SerializeField] private int baseManaCap = 10;
    public int AllyCurrentMana { get; private set; }
    public int AllyCurrentMaxMana { get; private set; }
    public int EnemyCurrentMana { get; private set; }
    public int EnemyCurrentMaxMana { get; private set; }
    public int AllyBonusManaCap { get; private set; }
    public int EnemyBonusManaCap { get; private set; }

    [Header("UI")]
    [SerializeField] TextMeshProUGUI manacounterAlly;
    [SerializeField] GameObject boardDesign;
    [SerializeField] GameObject fatigueDisplay;
    private CanvasGroup canvasGroup;
    [SerializeField] public GameObject UIparent;
    [SerializeField] List<Sprite> boards;
    [SerializeField] Sprite distortionBoard;
    [SerializeField] TextMeshProUGUI manacounterEnmy;
    [SerializeField] Transform playerCoreProxy;
    [SerializeField] Transform enemyCoreProxy;

    [Header("Cursor")]
    [SerializeField] Image attackCursor;
    
    [Header("EnemySpellReveal")]
    [SerializeField] GameObject enemySpellReveal;
    [SerializeField] GameObject enemySpellAnchor;

    [Header("Trait Systems")]
    [SerializeField] public TraitSystem allyTraitSystem;
    [SerializeField] public TraitSystem enemyTraitSystem;
    [SerializeField] private TraitUIManager allyTraitUI;
    [SerializeField] private TraitUIManager enemyTraitUI;
    [SerializeField] private WinLoseUI winLoseUI;
    private readonly Queue<Action> deferredActions = new();

    [Header("Tension Gauge")]
    [Range(0, 100)]
    public int fillingAlly = 0; // percentage (0–100)
    public int fillingEnemy = 0; // percentage (0–100)
    [SerializeField] public TextMeshProUGUI textComp;
    [SerializeField] public Image fillImageAlly;
    [SerializeField] public Image fillImageEnemy;
    
    [Header("Soul Counter")]
    [SerializeField] public GameObject allySoulCounter;
    [SerializeField] public GameObject enemySoulCounter;

    [Header("Camera Shake")]
    [SerializeField] private Camera mainCamera;

    //Graveyard 
    public Graveyard PlayerGraveyard { get; private set; } = new();
    public Graveyard EnemyGraveyard { get; private set; } = new();

    public int PlayerFatigue = 0;
    public int EnemyFatigue = 0;

    public int PlayerRandomCount = 0;
    public int EnemyRandomCount = 0;
    public int PlayerHealBonus = 0;
    public int EnemyHealBonus = 0;
    public bool PlayerDarkHeal = false;
    public bool EnemyDarkHeal = false;
    public bool DistortionWorld = false;
    //Animation
    public bool IsCombatAnimating { get; private set; }
    //Trait logic
    private readonly List<ITraitProgression> activeProgressions = new();
    public readonly HashSet<PlayerOwner> swordsmanBleedAppliedThisTurn = new();
    //Attack logic
    private readonly Queue<AttackRequest> attackQueue = new Queue<AttackRequest>();
    private bool isResolvingAttack = false;
    public bool isTargettingAttack;
    Card currentAttacker;

    //Trait Actions
    public event System.Action<CardInstance> OnCardKilled;
    public event System.Action<CardInstance> OnCardAttack;
    public event System.Action<PlayerOwner> OnBleedApplied;
    public event System.Action<CardInstance> OnCardKiller;
    public event System.Action<PlayerOwner, int> OnOwnerHeal;
    public event System.Action<PlayerOwner, IAttackable, int, int> OnOwnerHealResolved;
    public event System.Action<PlayerOwner, int> OnOwnerDamage;
    public event System.Action<PlayerOwner, int> OnOwnerManaGain;
    public event System.Action<PlayerOwner> OnDiscover;
    public event System.Action<PlayerOwner> OnPraise;
    public event System.Action<PlayerOwner> OnHissatsuPlayed;
    public event System.Action<PlayerOwner> OnDamageCard;
    public event System.Action<CardInstance> OnDamageCardInstance;
    public event System.Action<CardInstance> OnSpellPlayed;
    public event System.Action<CardInstance> OnCardPlayed;
    public event System.Action<CardInstance, int> OnSoulConsumed;

    private sealed class PendingHandReturn
    {
        public PlayerOwner Owner;
        public int CardId;
        public int TurnsRemaining;
        public int ManaModifier;
        public int AttackBonus;
        public int HealthBonus;
    }

    private readonly List<PendingHandReturn> pendingHandReturns = new();
    private sealed class PartnerLink
    {
        public CardInstance Source;
        public CardInstance Partner;
        public PlayerOwner Owner;
        public int BoostMode;
        public bool StatsApplied;
    }
    private readonly List<PartnerLink> activePartnerLinks = new();

    public void NotifySpellPlayed(CardInstance spell)
    {
        OnSpellPlayed?.Invoke(spell);
    }
    public void NotifyCardPlayed(CardInstance card)
    {
        OnCardPlayed?.Invoke(card);
    }
    public void NotifyBleedApplied(PlayerOwner owner)
    {
        OnBleedApplied?.Invoke(owner);
    }
    //Camera shake
    private Vector3 cameraBasePos;
    private Tween cameraShakeTween;
    //Target effects
    private bool isTargetingEffect;
    private Func<IAttackable, bool> onEffectTargetChosen;
    private CardInstance effectSource;
    private EffectTarget targetType = EffectTarget.None;
    PlayerOwner effectOwner;
    [Header("Discovery")]
    [SerializeField] public GameObject discoverDisplay;
    public bool isDiscovering;
    public int DiscoverDiscount;
    private int dungeonStartDrawBonus;
    private int dungeonStartDrawBonusEnemy;
    private bool adventureBossSecondPhaseTriggered;
    private bool adventureBossFinalDialogueTriggered;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
            mainCamera = Camera.main;

        cameraBasePos = mainCamera.transform.position; 
        canvasGroup = fatigueDisplay.GetComponent<CanvasGroup>();

    }
    void Start()
    {
        //Combat setup that might be changed bu Dungeon mode
        startingPlayerCoreHealth = startingCoreHealth;
        startingEnemyCoreHealth = startingCoreHealth;
        dungeonStartDrawBonus = 0;
        dungeonStartDrawBonusEnemy = 0;
        adventureBossSecondPhaseTriggered = false;
        adventureBossFinalDialogueTriggered = false;
        isTargettingAttack = false;
        InitializeMana();

        boardDesign.GetComponentInChildren<SpriteRenderer>().sprite = boards[UnityEngine.Random.Range(0, boards.Count)];
        if (GameRunContext.IsDungeonRun) boardDesign.GetComponentInChildren<SpriteRenderer>().sprite = boards[0];

        //Logic for dungeon runs
        if (GameRunContext.IsDungeonRun)
        {
            SetupDungeonFight(GameRunContext.DungeonData);
            SetupFirstTurn();
        }
        else if (GameRunContext.IsAdventureCombat)
        {
            SetupAdventureFight(GameRunContext.AdventureFightId);
        }
        else SetupFirstTurn();
    }
    public void SetupFirstTurn()
    {
        if (TurnManager.Instance == null ||adventureBossSecondPhaseTriggered ||adventureBossFinalDialogueTriggered)
        {
            Debug.LogError("TurnManager missing!");
            return;
        }
        SetFill(0, PlayerOwner.Player);
        SetFill(0, PlayerOwner.Enemy);

        fillImageAlly.transform.parent.gameObject.SetActive(false);
        fillImageEnemy.transform.parent.gameObject.SetActive(false);
        //Setup cores mana and deck before the turn logic

        DiscoverDiscount = 0;
        deckManager.InitializeDecks();        // build decks
        deckManager.DetectUnlockableTraits(); // analyze decks
        SetupTraits();                        // create progressions

        SetupCores();
        PlayerCore.GetComponent<CoreView>().Bind(PlayerCore);
        EnemyCore.GetComponent<CoreView>().Bind(EnemyCore);

        //Start turn logic
        TurnManager.Instance.OnTurnStarted += HandleTurnStart;
        TurnManager.Instance.OnTurnEnded += HandleTurnEnded;
        TurnManager.Instance.StartFirstTurn();
        if (dungeonStartDrawBonus > 0)
        {
            StartCoroutine(deckManager.Draw(dungeonStartDrawBonus, PlayerOwner.Player));
        }
        if (dungeonStartDrawBonusEnemy > 0)
        {
            StartCoroutine(deckManager.Draw(dungeonStartDrawBonusEnemy, PlayerOwner.Enemy));
        }
        foreach (var progression in activeProgressions)
        {
            progression.ResetProgression();
            progression.PushInitialState();
        }

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted += HandleTurnStartedForPendingHandReturns;
    }
    private void HandleTurnStartedForPendingHandReturns(PlayerOwner owner)
    {
        if (pendingHandReturns.Count == 0)
            return;

        List<PendingHandReturn> dueReturns = new();

        for (int i = pendingHandReturns.Count - 1; i >= 0; i--)
        {
            PendingHandReturn pending = pendingHandReturns[i];
            if (pending.Owner != owner)
                continue;

            pending.TurnsRemaining--;
            if (pending.TurnsRemaining > 0)
                continue;

            dueReturns.Add(pending);
            pendingHandReturns.RemoveAt(i);
        }

        foreach (PendingHandReturn pending in dueReturns)
        {
            AddCardToHandWithBonuses(
                pending.Owner,
                pending.CardId,
                pending.ManaModifier,
                pending.AttackBonus,
                pending.HealthBonus
            );
        }
    }
    public void SetSouls(PlayerOwner owner, int counter)
    {
        GameObject soulGO = owner == PlayerOwner.Player ? allySoulCounter : enemySoulCounter;

        if (!soulGO.activeSelf)
            soulGO.SetActive(true);

        TextMeshProUGUI[] texts = soulGO.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            text.text = counter.ToString();
        }
    }
    public int GetSouls(PlayerOwner owner)
    {
        GameObject soulGO = owner == PlayerOwner.Player ? allySoulCounter : enemySoulCounter;
        if (!soulGO.activeSelf) soulGO.SetActive(true);

        return Convert.ToInt32(soulGO.GetComponentInChildren<TextMeshProUGUI>().text);
    }
    public int ConsumeSoul(CardInstance consumer, int amount)
    {
        if (consumer == null || amount <= 0)
            return 0;

        PlayerOwner owner = consumer.Owner;
        int availableSouls = GetSouls(owner);
        if (availableSouls <= 0)
            return 0;

        int consumed = Mathf.Min(amount, availableSouls);
        SetSouls(owner, availableSouls - consumed);

        OnSoulConsumed?.Invoke(consumer, consumed);

        if (OwnerHasTrait(owner, CardData.Trait.SoulForce, 2) &&
            consumer.CurrentZone == CardZone.Board &&
            !consumer.IsDead)
        {
            consumer.ModifyStats(consumed, consumed);
        }

        return consumed;
    }
    public int DiscardCardsFromHandWithDeferredReturn(
        PlayerOwner owner,
        Func<CardInstance, bool> discardPredicate,
        int turnsUntilReturn,
        int manaModifier,
        int attackBonus,
        int healthBonus
    )
    {
        HandManager hand = owner == PlayerOwner.Player ? allyHand : enemyHand;
        if (hand == null)
            return 0;

        int discardedCount = 0;
        List<GameObject> handSnapshot = new(hand.handCards);

        foreach (GameObject go in handSnapshot)
        {
            if (go == null)
                continue;

            CardInstance card = go.GetComponent<CardInstance>();
            if (card == null || !discardPredicate(card))
                continue;

            pendingHandReturns.Add(new PendingHandReturn
            {
                Owner = owner,
                CardId = card.Data.id,
                TurnsRemaining = turnsUntilReturn,
                ManaModifier = manaModifier,
                AttackBonus = attackBonus,
                HealthBonus = healthBonus
            });

            hand.RemoveCardFromHand(go);
            Destroy(go);
            discardedCount++;
        }

        hand.UpdateCardPositions();
        return discardedCount;
    }
    public void DisplayFatigue(PlayerOwner owner)
    {
        fatigueDisplay.SetActive(true);

        int fatigueVal = owner == PlayerOwner.Player ? PlayerFatigue : EnemyFatigue;

        TextMeshProUGUI fatigueTxt = fatigueDisplay.GetComponentInChildren<TextMeshProUGUI>();
        fatigueTxt.text = $"No more cards in deck.\nRefilling deck.\n{owner} now takes damage equal to the mana of the drawn cards * {fatigueVal}";

        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }
    private IEnumerator FadeRoutine()
    {
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, 0.5f));

        // Stay visible for 3 seconds
        yield return new WaitForSeconds(3f);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, 0.5f));

        fatigueDisplay.SetActive(false);
    }
    private IEnumerator Fade(float start, float end, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, time / duration);
            yield return null;
        }

        canvasGroup.alpha = end;
    }
    public int DiscardRandomCards(PlayerOwner sourceOwner, bool targetEnemy, int count)
    {
        if (count <= 0)
            return 0;

        HandManager targetHand;

        if (targetEnemy)
            targetHand = sourceOwner == PlayerOwner.Player ? enemyHand : allyHand;
        else
            targetHand = sourceOwner == PlayerOwner.Player ? allyHand : enemyHand;

        if (targetHand == null || targetHand.handCards.Count == 0)
            return 0;

        int discardedCount = 0;
        int toDiscard = Mathf.Min(count, targetHand.handCards.Count);

        for (int i = 0; i < toDiscard; i++)
        {
            if (targetHand.handCards.Count == 0)
                break;

            int randomIndex = UnityEngine.Random.Range(0, targetHand.handCards.Count);
            GameObject toRemove = targetHand.handCards[randomIndex];

            if (toRemove == null)
            {
                targetHand.handCards.RemoveAt(randomIndex);
                continue;
            }

            targetHand.RemoveCardFromHand(toRemove);
            Destroy(toRemove);
            discardedCount++;
        }

        targetHand.UpdateCardPositions();
        return discardedCount;
    }
    public CardInstance AddCardToHandWithBonuses(
        PlayerOwner owner,
        int id,
        int manaModifier,
        int attackBonus,
        int healthBonus
    )
    {
        HandManager hand = owner == PlayerOwner.Player ? allyHand : enemyHand;
        if (hand == null || hand.handCards.Count >= hand.maxHandSize)
            return null;

        CardData data = CardDatabase.Instance.GetCardById(id);
        if (data == null)
            return null;

        CardInstance card = CardFactory.Instance.CreateCard(data, owner);
        card.SetZone(CardZone.Hand);

        if (manaModifier != 0)
            card.AddTemporaryManaModifier(manaModifier);

        if (attackBonus != 0 || healthBonus != 0)
            card.ModifyStats(attackBonus, healthBonus);

        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();
        return card;
    }
    public void BuffAllAllies(int atk, int hp, PlayerOwner owner)
    {
        List<GameObject> allies;
        if (owner == PlayerOwner.Player) allies = allyDropArea.allyPrefabCards;
        else allies = enemyDropArea.enemyPrefabCards;

        foreach(GameObject go in allies)
        {
            go.GetComponent<CardInstance>().ModifyStats(atk, hp);
        }
    }
    public void BuffAllAllies(int totalStats, PlayerOwner owner)
    {
        List<GameObject> allies;
        if (owner == PlayerOwner.Player) allies = allyDropArea.allyPrefabCards;
        else allies = enemyDropArea.enemyPrefabCards;

        foreach (GameObject go in allies)
        {
            int newAtk = UnityEngine.Random.Range(0, totalStats + 1);
            int newHp = totalStats - newAtk;

            go.GetComponent<CardInstance>().ModifyStats(newAtk, newHp);
        }
    }
    void SetupDungeonFight(DungeonRunData runData)
    {
        startingEnemyCoreHealth = 5 * (runData.floor+4);
        startingPlayerCoreHealth = 30;

        if (startingEnemyCoreHealth > 100) startingEnemyCoreHealth = 100;

        int bonusenemyMana = Math.Min(runData.floor/6, 9);
        int bonusenemyDraw = Math.Min(runData.floor/5, 5);
        EnemyCurrentMana += bonusenemyMana;
        EnemyCurrentMaxMana += bonusenemyMana;
        dungeonStartDrawBonusEnemy += bonusenemyDraw;
        string myaugments = "Current augments = ";
        foreach(DungeonShop.Augment augment in runData.augments)
        {
            myaugments += augment.ToString() + "/";
            //Max HP Augment
            if(augment == DungeonShop.Augment.MaxHP)
            {
                startingPlayerCoreHealth += 10;
            }

            //Starting MANA Augment
            if (augment == DungeonShop.Augment.StartMana)
            {
                AllyCurrentMaxMana++;
                AllyCurrentMana++;
            }

            if (augment == DungeonShop.Augment.StartDraw)
            {
                dungeonStartDrawBonus++;
            }
        }
    }

    void SetupAdventureFight(int battleId)
    {
        if(battleId<9)
            startingEnemyCoreHealth = 30;
        else if(battleId<13)
            startingEnemyCoreHealth = 50;
        else
            startingEnemyCoreHealth = 80;

        startingPlayerCoreHealth = 30;
        CombatDialogue.Instance.TriggerCutscene(battleId);
    }
    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= HandleTurnStart;
            TurnManager.Instance.OnTurnStarted -= HandleTurnStartedForPendingHandReturns;
            TurnManager.Instance.OnTurnEnded -= HandleTurnEnded;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) EnemyCore.TakeDamage(999) ;
        manacounterAlly.text = $"{AllyCurrentMana}/{AllyCurrentMaxMana}";
        manacounterEnmy.text = $"{EnemyCurrentMana}/{EnemyCurrentMaxMana}";
        attackCursor.transform.position = Input.mousePosition;
        if (DistortionWorld && boardDesign.GetComponentInChildren<SpriteRenderer>().sprite != distortionBoard)
        {
            boardDesign.GetComponentInChildren<SpriteRenderer>().sprite = distortionBoard;
        }
        if (!DistortionWorld && boardDesign.GetComponentInChildren<SpriteRenderer>().sprite == distortionBoard)
        {
            boardDesign.GetComponentInChildren<SpriteRenderer>().sprite = boards[UnityEngine.Random.Range(0, boards.Count)];
        }
    }
    #region Turn Logic

    public void SetFill(int value, PlayerOwner owner)
    {
        if(owner==PlayerOwner.Player)
            fillingAlly = Mathf.Clamp(value, 0, 100);
        else
            fillingEnemy = Mathf.Clamp(value, 0, 100);
        UpdateFill();
    }
    public void IncreaseFill(int value, PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            fillingAlly = Mathf.Clamp(fillingAlly + value, 0, 100);
        else
            fillingEnemy = Mathf.Clamp(fillingEnemy + value, 0, 100);
        UpdateFill();
    }
    public void ReduceFill(int value, PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            fillingAlly = Mathf.Clamp(fillingAlly - value, 0, 100);
        else
            fillingEnemy = Mathf.Clamp(fillingEnemy - value, 0, 100);
        UpdateFill();
    }

    public bool IsTensionBarVisible(PlayerOwner owner)
    {
        Image fillImage = owner == PlayerOwner.Player ? fillImageAlly : fillImageEnemy;
        if (fillImage == null || fillImage.transform.parent == null)
            return false;

        return fillImage.transform.parent.gameObject.activeSelf;
    }

    public void UnlockTensionBar(PlayerOwner owner)
    {
        Image fillImage = owner == PlayerOwner.Player ? fillImageAlly : fillImageEnemy;
        if (fillImage == null || fillImage.transform.parent == null)
            return;

        fillImage.transform.parent.gameObject.SetActive(true);
        SetFill(0, owner);
    }

    public int GetTension(PlayerOwner owner)
    {
        int filling = owner == PlayerOwner.Player ? fillingAlly : fillingEnemy;
        return Mathf.Clamp(filling / 10, 0, 10);
    }

    public bool TryConsumeTension(int tensionCost, PlayerOwner owner)
    {
        if (tensionCost <= 0 || !IsTensionBarVisible(owner))
            return false;

        int requiredFill = tensionCost * 10;
        int currentFill = owner == PlayerOwner.Player ? fillingAlly : fillingEnemy;
        if (currentFill < requiredFill)
            return false;

        ReduceFill(requiredFill, owner);
        return true;
    }

    public int GetHissatsuTensionCost(CardInstance card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.CurrentEffect))
            return 0;

        Match match = Regex.Match(card.CurrentEffect, @"hissatsu\*\((\d+)\)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return 0;

        return int.TryParse(match.Groups[1].Value, out int cost) ? Mathf.Max(0, cost) : 0;
    }
    public void ShowChainHissatsu(int value)
    {
        if (textComp == null) return;
        StartCoroutine(QuickNotifyRoutine(textComp, value));
    }
    private IEnumerator QuickNotifyRoutine(TextMeshProUGUI textComp, int value)
    {
        if (textComp == null) yield break;

        GameObject root = textComp.transform.parent.gameObject;

        // Ensure CanvasGroup exists
        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = root.AddComponent<CanvasGroup>();

        // Set text
        textComp.text = value.ToString();

        // Random Z rotation
        float zRot = UnityEngine.Random.Range(-5f, 5f);
        root.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        root.SetActive(true);


        // ✨ scale punch
        StartCoroutine(ScalePunch(root.transform));
        float fadeInTime = 0.15f;
        float visibleTime = 0.35f;
        float fadeOutTime = 0.2f;

        cg.alpha = 0f;

        // ----- Fade In -----
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.SmoothStep(0f, 1f, t / fadeInTime);
            yield return null;
        }
        cg.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(visibleTime);

        // ----- Fade Out -----
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.SmoothStep(1f, 0f, t / fadeOutTime);
            yield return null;
        }
        cg.alpha = 0f;

        root.SetActive(false);
    }
    private IEnumerator ScalePunch(Transform t)
    {
        float d = 0.12f;
        float time = 0f;
        Vector3 start = t.localScale;
        Vector3 end = Vector3.one;

        while (time < d)
        {
            time += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, end, time / d);
            yield return null;
        }
        t.localScale = end;
    }
    public bool CanAffordCardCost(CardInstance card)
    {
        if (card == null)
            return false;

        bool isHissatsu = card.HasKeyword("hissatsu*");
        int tensionCost = GetHissatsuTensionCost(card);

        if (isHissatsu && IsTensionBarVisible(card.Owner) && tensionCost > 0)
        {
            int currentFill = card.Owner == PlayerOwner.Player ? fillingAlly : fillingEnemy;
            if (currentFill >= tensionCost * 10)
                return true;
        }

        return card.Owner == PlayerOwner.Player
            ? card.CurrentManaCost <= AllyCurrentMana
            : card.CurrentManaCost <= EnemyCurrentMana;
    }

    public void SpendCardCost(CardInstance card)
    {
        if (card == null)
            return;

        bool isHissatsu = card.HasKeyword("hissatsu*");
        int tensionCost = GetHissatsuTensionCost(card);

        if (isHissatsu && tensionCost > 0 && TryConsumeTension(tensionCost, card.Owner))
            return;

        UseMana(card.CurrentManaCost, card.Owner);
    }
    private void UpdateFill()
    {
        if (fillImageAlly == null && fillImageEnemy == null) return;

        // Image.fillAmount expects 0–1
        fillImageAlly.fillAmount = fillingAlly / 100f;
        fillImageEnemy.fillAmount = fillingEnemy / 100f;
    }
    private void HandleTurnStart(PlayerOwner owner)
    {
        swordsmanBleedAppliedThisTurn.Remove(owner);
        IncreaseMaxMana(owner);
        RefillMana(owner);
    }
    private void HandleTurnEnded(PlayerOwner owner)
    {
        UpdatePartnerLinks();
    }
    private bool IsPartnerLinkAlive(PartnerLink link)
    {
        if (link == null || link.Source == null || link.Partner == null)
            return false;

        return !link.Source.IsDead
            && !link.Partner.IsDead
            && link.Source.CurrentZone == CardZone.Board
            && link.Partner.CurrentZone == CardZone.Board;
    }
    private void UpdatePartnerLinks()
    {
        for (int i = activePartnerLinks.Count - 1; i >= 0; i--)
        {
            PartnerLink link = activePartnerLinks[i];
            bool alive = IsPartnerLinkAlive(link);

            if (!alive)
            {
                if (link != null && link.StatsApplied)
                {
                    if (link.Source != null && !link.Source.IsDead)
                        link.Source.ModifyStats(-1, -1);
                    if (link.Partner != null && !link.Partner.IsDead)
                        link.Partner.ModifyStats(-1, -1);
                }

                activePartnerLinks.RemoveAt(i);
                continue;
            }

            if (link.BoostMode == 0 && !link.StatsApplied)
            {
                link.Source.ModifyStats(1, 1);
                link.Partner.ModifyStats(1, 1);
                link.StatsApplied = true;
            }
        }
    }
    private void HandlePartnerAttackTriggers(CardInstance attacker)
    {
        if (attacker == null || attacker.IsDead)
            return;

        UpdatePartnerLinks();

        foreach (PartnerLink link in activePartnerLinks)
        {
            if (link == null || link.BoostMode != 2)
                continue;

            if (!IsPartnerLinkAlive(link))
                continue;

            CardInstance buffTarget = null;
            if (ReferenceEquals(link.Source, attacker))
                buffTarget = link.Partner;
            else if (ReferenceEquals(link.Partner, attacker))
                buffTarget = link.Source;

            if (buffTarget == null || buffTarget.IsDead)
                continue;

            int totalStats = 2;
            int atk = UnityEngine.Random.Range(0, totalStats + 1);
            int hp = totalStats - atk;
            buffTarget.ModifyStats(atk, hp);
        }
    }
    private void HandlePartnerDeathTriggers(CardInstance deadCard)
    {
        if (deadCard == null)
            return;

        for (int i = 0; i < activePartnerLinks.Count; i++)
        {
            PartnerLink link = activePartnerLinks[i];
            if (link == null || link.BoostMode != 1)
                continue;

            bool sourceDied = ReferenceEquals(link.Source, deadCard);
            bool partnerDied = ReferenceEquals(link.Partner, deadCard);
            if (!sourceDied && !partnerDied)
                continue;

            CardInstance survivor = sourceDied ? link.Partner : link.Source;
            if (survivor == null || survivor.IsDead || survivor.CurrentZone != CardZone.Board)
                continue;

            survivor.ModifyStats(3, 3);
        }
    }
    public void SummonPartner(CardInstance source, int cardId, int boost)
    {
        if (source == null || source.IsDead || source.CurrentZone != CardZone.Board)
            return;

        CardInstance summoned = TrySummonForOwnerAndGet(source.Owner, cardId);
        if (summoned == null || summoned.IsDead)
            return;

        PartnerLink link = new PartnerLink
        {
            Source = source,
            Partner = summoned,
            Owner = source.Owner,
            BoostMode = boost,
            StatsApplied = false
        };

        activePartnerLinks.Add(link);
        UpdatePartnerLinks();
    }
    private void EndGame()
    {
        isTargettingAttack = false;
        attackCursor.gameObject.SetActive(false);

        if (TurnManager.Instance != null)
            TurnManager.Instance.enabled = false;

        if (winLoseUI != null)
        {
            winLoseUI.gameObject.SetActive(true);
            if (CurrentGameState == GameState.PlayerWon)
                winLoseUI.ShowWin();
            else if (CurrentGameState == GameState.PlayerLost)
                winLoseUI.ShowLose();
        }

        Debug.Log($"Game ended with state: {CurrentGameState}");
    }
    #endregion

    #region Trait Management
    private void SetupTraits()
    {
        allyTraitSystem.Initialize(PlayerOwner.Player);
        enemyTraitSystem.Initialize(PlayerOwner.Enemy);
        allyTraitUI.DetectTraitBorder();enemyTraitUI.DetectTraitBorder();
        allyTraitSystem.OnTraitTierActivated += OnAllyTraitActivated;
        enemyTraitSystem.OnTraitTierActivated += OnEnemyTraitActivated;

        SetupPlayerTraits(PlayerOwner.Player, deckManager.AllyTraitsUnlockable, allyTraitSystem);
        SetupPlayerTraits(PlayerOwner.Enemy, deckManager.EnemyTraitsUnlockable, enemyTraitSystem);
    }
    private void SetupPlayerTraits(PlayerOwner owner, Dictionary<CardData.Trait, int> unlockables, TraitSystem traitSystem)
    {
        if (unlockables == null)
        {
            Debug.LogError($"Unlockables dictionary is NULL for {owner}");
            return;
        }
        foreach (var pair in unlockables)
        {
            CardData.Trait trait = pair.Key;
            int maxTier = pair.Value;

            ITraitProgression progression = trait switch
            {
                CardData.Trait.Neutral => new NeutralProgression(owner, maxTier, traitSystem, allyDropArea, enemyDropArea),
                CardData.Trait.Pokemon => new PokemonProgression(owner, maxTier, traitSystem, allyDropArea, enemyDropArea),
                CardData.Trait.MonsterHunter => new MonsterHunterProgression(owner, maxTier, traitSystem, allyDropArea, enemyDropArea),
                CardData.Trait.Gunner => new GunnerProgression(owner, maxTier, traitSystem),
                CardData.Trait.Speedster => new SpeedsterProgression(owner, maxTier, traitSystem),
                CardData.Trait.Faith => new FaithProgression(owner, maxTier, traitSystem),
                CardData.Trait.Avatar => new AvatarProgression(owner, maxTier, traitSystem),
                CardData.Trait.Healer => new HealerProgression(owner, maxTier, traitSystem),
                CardData.Trait.Chaos => new ChaosProgression(owner, maxTier, traitSystem, allyDropArea, enemyDropArea),
                CardData.Trait.Fighter => new FighterProgression(owner, maxTier, traitSystem, allyDropArea, enemyDropArea),
                CardData.Trait.Inazuma => new InazumaProgression(owner, maxTier, traitSystem),
                CardData.Trait.SpellFocus => throw new System.NotImplementedException(),
                CardData.Trait.Cozy => new CozyProgression(owner, maxTier, traitSystem),
                CardData.Trait.Swordsman => new SwordsmanProgression(owner, maxTier, traitSystem),
                CardData.Trait.Combo => new ComboProgression(owner, maxTier, traitSystem),
                CardData.Trait.SoulForce => new SoulForceProgression(owner, maxTier, traitSystem, deckManager),
                _ => throw new System.NotImplementedException(),
            };
            if (progression != null)
            {
                progression.Register(); progression.OnProgressUpdated += HandleTraitProgressUpdated;

                activeProgressions.Add(progression);
            }
        }
    }
    private void HandleTraitProgressUpdated(    CardData.Trait trait,    int progress, int currentCap,    PlayerOwner owner)
    {
        TraitUIManager ui =
            owner == PlayerOwner.Player
                ? allyTraitUI
                : enemyTraitUI;
        TraitsDisplay display = ui.GetTraitDisplay(trait);
        if (display == null)
            return;

        ui.UpdateTraitProgress(trait, progress, currentCap);

        display.Progression = progress;
        display.ShowProgress(progress, currentCap);
    }
    public bool OwnerHasTrait(PlayerOwner owner, CardData.Trait trait, int minTier = 0)
    {
        TraitSystem system =
            owner == PlayerOwner.Player
                ? allyTraitSystem
                : enemyTraitSystem;

        return system.HasTraitAtTier(trait, minTier);
    }

    public void AddPokemonTraitProgress(PlayerOwner owner, int amount)
    {
        if (amount <= 0)
            return;

        foreach (var progression in activeProgressions)
        {
            if (progression is PokemonProgression pokemonProgression && pokemonProgression.Owner == owner)
            {
                pokemonProgression.AddCatchProgress(amount);
                return;
            }
        }
    }

    private void OnAllyTraitActivated(CardData.Trait trait, int tier)
    {
        allyTraitUI.ActivateTrait(trait, tier);
    }
    private void OnEnemyTraitActivated(CardData.Trait trait, int tier)
    {
        enemyTraitUI.ActivateTrait(trait, tier);
    }
    #endregion

    #region Core Management
    private void SetupCores()
    {
        //PlayerCore = Instantiate(corePrefab, spawnPlayerCore.transform).GetComponent<CoreInstance>();
        PlayerCore.Initialize(PlayerOwner.Player, startingPlayerCoreHealth);

        //EnemyCore = Instantiate(corePrefab, spawnEnemyCore.transform).GetComponent<CoreInstance>();
        EnemyCore.Initialize(PlayerOwner.Enemy, startingEnemyCoreHealth);
    }
    public void OnCoreDestroyed(PlayerOwner owner)
    {
        if (CurrentGameState != GameState.Playing)
            return; // prevent double fire

        if (owner == PlayerOwner.Player)
        {
            CurrentGameState = GameState.PlayerLost;
            Debug.Log("PLAYER LOSES");
            ModifyUserGold(LossGoldCompensation);
            if (GameRunContext.IsDungeonRun)
                DungeonManager.SetDungeonCombatActive(false);
            if (GameRunContext.IsAdventureCombat)
            {
                AdventureProgressionService.SetAdventureCombatActive(false);
                AdventureProgressionService.RecordFightResult(GameRunContext.AdventureFightId, false);
            }
        }
        else
        {
            if (GameRunContext.IsAdventureCombat && GameRunContext.AdventureFightId == 13 && !adventureBossSecondPhaseTriggered)
            {
                adventureBossSecondPhaseTriggered = true;
                StartCoroutine(HandleAdventureBossSecondPhaseTransition());
                return;
            }
            if (GameRunContext.IsAdventureCombat && GameRunContext.AdventureFightId == 13 && adventureBossSecondPhaseTriggered && !adventureBossFinalDialogueTriggered)
            {
                adventureBossFinalDialogueTriggered = true;
                CurrentGameState = GameState.PlayerWon;
                Debug.Log("PLAYER WINS");
                ApplyPlayerWinRewardsAndProgression();
                StartCoroutine(HandleAdventureBossFinalDialogueThenWinUI());
                return;
            }
            CurrentGameState = GameState.PlayerWon;
            Debug.Log("PLAYER WINS");
            ApplyPlayerWinRewardsAndProgression();
        }

        EndGame();
    }
    private void ApplyPlayerWinRewardsAndProgression()
    {
        ModifyUserGold(WinGoldReward);
        if (GameRunContext.IsDungeonRun)
        {
            DungeonManager.SetDungeonCombatActive(false);
            ApplyDungeonCoinReward(DungeonWinCoinReward);
        }
        if (GameRunContext.IsAdventureCombat)
        {
            AdventureProgressionService.SetAdventureCombatActive(false);
            AdventureProgressionService.RecordFightResult(GameRunContext.AdventureFightId, true);
        }
    }
    private IEnumerator HandleAdventureBossSecondPhaseTransition()
    {
        if (winLoseUI != null)
        {
            winLoseUI.gameObject.SetActive(true);
            winLoseUI.ShowWin();
            winLoseUI.SetInteractionEnabled(false);
        }

        yield return new WaitForSeconds(AdventureSecondPhaseDelaySeconds);

        if (winLoseUI != null)
        {
            winLoseUI.SetInteractionEnabled(true);
            winLoseUI.gameObject.SetActive(false);
        }

        EnemyCore.Initialize(PlayerOwner.Enemy, 100);
        EnemyCore.FullHeal();
        ResetEnemyForAdventureSecondPhase(14);
        CombatDialogue.Instance.TriggerCutscene(14);
    }
    private IEnumerator HandleAdventureBossFinalDialogueThenWinUI()
    {
        Debug.Log("Adventure boss second phase completed. Triggering final dialogue.");

        if (TurnManager.Instance != null)
            TurnManager.Instance.enabled = false;

        if (winLoseUI != null)
            winLoseUI.gameObject.SetActive(false);

        bool dialogueFinished = false;
        CombatDialogue dialogue = CombatDialogue.Instance;

        if (dialogue != null)
        {
            Action markDialogueFinished = () => dialogueFinished = true;
            dialogue.OnDialogueEnded += markDialogueFinished;
            dialogue.TriggerCutscene(15, resumeCombat: false);
            yield return new WaitUntil(() => dialogueFinished);
            dialogue.OnDialogueEnded -= markDialogueFinished;
        }
        else
        {
            Debug.LogWarning("CombatDialogue.Instance is missing; skipping combat 15 dialogue.");
        }

        EndGame();
    }
    private void ResetEnemyForAdventureSecondPhase(int adventureDeckId)
    {
        Dictionary<CardData.Trait, int> savedEnemyProgress = SnapshotTraitProgress(PlayerOwner.Enemy);
        RemoveTraitProgressionsForOwner(PlayerOwner.Enemy);
        ClearEnemyHand();
        DiscardEnemyDeck();
        EnemyGraveyard = new Graveyard();
        ReplaceEnemyDeckFromAdventureDeck(adventureDeckId);
        deckManager.RefreshUnlockableTraitsForOwner(PlayerOwner.Enemy);
        enemyTraitUI.DetectTraitBorder();
        SetupPlayerTraits(PlayerOwner.Enemy, deckManager.EnemyTraitsUnlockable, enemyTraitSystem);
        RestoreTraitProgress(PlayerOwner.Enemy, savedEnemyProgress);
        StartCoroutine(deckManager.Draw(5, PlayerOwner.Enemy));
    }
    private void ClearEnemyHand()
    {
        if (enemyHand == null)
            return;

        foreach (GameObject handCard in new List<GameObject>(enemyHand.handCards))
        {
            enemyHand.RemoveCardFromHand(handCard);
            Destroy(handCard);
        }

        enemyHand.handCards.Clear();
        enemyHand.UpdateCardPositions();
    }
    private void ReplaceEnemyDeckFromAdventureDeck(int adventureDeckId)
    {
        List<int> enemyDeckIds = EnemyDecks.GetAdventureDeck(adventureDeckId);
        Queue<CardData> enemyDeck = new Queue<CardData>();

        foreach (int cardId in enemyDeckIds)
        {
            CardData cardData = CardDatabase.Instance.GetCardById(cardId);
            if (cardData != null)
                enemyDeck.Enqueue(cardData);
        }

        deckManager.Shuffle(enemyDeck);
        deckManager.decks[PlayerOwner.Enemy] = enemyDeck;
    }
    private void DiscardEnemyDeck()
    {
        if (deckManager == null || deckManager.decks == null)
            return;

        if (deckManager.decks.TryGetValue(PlayerOwner.Enemy, out Queue<CardData> existingEnemyDeck))
            existingEnemyDeck.Clear();
    }
    private Dictionary<CardData.Trait, int> SnapshotTraitProgress(PlayerOwner owner)
    {
        Dictionary<CardData.Trait, int> snapshot = new();

        foreach (ITraitProgression progression in activeProgressions)
        {
            if (progression.Owner != owner)
                continue;

            snapshot[progression.Trait] = Mathf.Max(progression.CurrentProgress, 0);
        }

        return snapshot;
    }
    private void RemoveTraitProgressionsForOwner(PlayerOwner owner)
    {
        for (int i = activeProgressions.Count - 1; i >= 0; i--)
        {
            ITraitProgression progression = activeProgressions[i];
            if (progression.Owner != owner)
                continue;

            progression.OnProgressUpdated -= HandleTraitProgressUpdated;
            progression.Unregister();
            activeProgressions.RemoveAt(i);
        }

        TraitSystem traitSystem = owner == PlayerOwner.Player ? allyTraitSystem : enemyTraitSystem;
        traitSystem.ClearAll();
    }
    private void RestoreTraitProgress(PlayerOwner owner, Dictionary<CardData.Trait, int> savedProgress)
    {
        foreach (ITraitProgression progression in activeProgressions)
        {
            if (progression.Owner != owner)
                continue;

            if (!savedProgress.TryGetValue(progression.Trait, out int progress) || progress <= 0)
            {
                progression.PushInitialState();
                continue;
            }

            TryRestoreProgressOnProgression(progression, progress);
            progression.PushInitialState();
        }
    }
    private void TryRestoreProgressOnProgression(ITraitProgression progression, int progress)
    {
        switch (progression.Trait)
        {
            case CardData.Trait.Neutral:
                SetIntField(progression, "neutralPlayed", progress);
                break;
            case CardData.Trait.SoulForce:
                SetIntField(progression, "soulsCollected", progress);
                break;
            case CardData.Trait.Fighter:
                SetIntField(progression, "FighterPlayed", progress);
                break;
            case CardData.Trait.Chaos:
                SetIntField(progression, "randomPlayed", progress);
                break;
            case CardData.Trait.Speedster:
                SetIntField(progression, "speedsterAttacks", progress);
                break;
            case CardData.Trait.Pokemon:
                SetIntField(progression, "pokemonKills", progress);
                break;
            case CardData.Trait.MonsterHunter:
                SetIntField(progression, "colossusDeaths", progress);
                break;
            case CardData.Trait.Healer:
                SetIntField(progression, "healAmount", progress);
                break;
            case CardData.Trait.Faith:
                SetIntField(progression, "discoverCount", progress);
                break;
            case CardData.Trait.Avatar:
                SetIntField(progression, "praiseCount", progress);
                break;
            case CardData.Trait.Gunner:
                SetIntField(progression, "damageCount", progress);
                break;
            case CardData.Trait.Inazuma:
                SetIntField(progression, "hissatsuCount", progress);
                break;
            default:
                Debug.Log($"No trait-progress restoration mapping for {progression.Trait}.");
                break;
        }
    }
    private void SetIntField(ITraitProgression progression, string fieldName, int value)
    {
        if (progression == null || string.IsNullOrWhiteSpace(fieldName))
            return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        FieldInfo field = progression.GetType().GetField(fieldName, flags);

        if (field == null || field.FieldType != typeof(int))
            return;

        field.SetValue(progression, value);
    }
    private void ApplyDungeonCoinReward(int reward)
    {
        if (reward <= 0)
            return;
        if (GameRunContext.DungeonData.floor % 10 == 0) reward += 50;
        if (DungeonManager.Instance?.CurrentRun != null)
        {
            DungeonManager.Instance.CurrentRun.coins += reward;
        }
        else
        {
            Debug.LogWarning("Dungeon run data missing while applying coin reward. Falling back to direct user coin update.");
            ModifyUserCoin(reward);
        }

    }
    private void ModifyUserCoin(int delta)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore.DefaultInstance
            .Collection("users")
            .Document(user.UserId)
            .UpdateAsync("coin", FieldValue.Increment(delta))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Failed to modify coin reward after match.");
            });
    }
    private void ModifyUserGold(int delta)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("No authenticated user.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("users")
          .Document(user.UserId)
          .UpdateAsync("gold", FieldValue.Increment(delta))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to modify gold.");
                    return;
                }
            });

    }
    #endregion

    #region Mana Management
    private void InitializeMana()
    {
        AllyCurrentMana = 0;
        AllyCurrentMaxMana = 0;
        AllyBonusManaCap = 0;

        EnemyCurrentMana = 0;
        EnemyCurrentMaxMana = 0;
        EnemyBonusManaCap = 0;
    }
    public void RefreshMaxMana(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
        {
            int gained = Mathf.Max(0, AllyCurrentMaxMana - AllyCurrentMana);
            AllyCurrentMana = AllyCurrentMaxMana;
            NotifyManaGained(owner, gained);
        }
        else
        {
            int gained = Mathf.Max(0, EnemyCurrentMaxMana - EnemyCurrentMana);
            EnemyCurrentMana = EnemyCurrentMaxMana;
            NotifyManaGained(owner, gained);
        }
    }
    public void UseMana(int mana, PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player)
            AllyCurrentMana -= mana;
        else
            EnemyCurrentMana -= mana;
    }
    public void GainMana(int mana, PlayerOwner owner)
    {
        if (mana <= 0)
            return;

        if (owner == PlayerOwner.Player)
            AllyCurrentMana += mana;
        else
            EnemyCurrentMana += mana;

        NotifyManaGained(owner, mana);
    }
    public void EnemyMaxManaLoss(int mana, PlayerOwner owner)
    {
        if (owner == PlayerOwner.Enemy) 
        { AllyCurrentMaxMana -= mana; AllyCurrentMana -= mana; }

        else { EnemyCurrentMaxMana -= mana; EnemyCurrentMana -= mana; }
    }
    public void GainMaxMana(int mana, PlayerOwner owner)
    {
        if (mana <= 0)
            return;

        if (owner == PlayerOwner.Player) { AllyCurrentMaxMana += mana; AllyCurrentMana += mana; }

        else { EnemyCurrentMaxMana += mana; EnemyCurrentMana += mana; }

        NotifyManaGained(owner, mana);
    }
    public int GainMaxManaCapped(int mana, PlayerOwner owner)
    {
        int effectiveCap = GetEffectiveManaCap(owner);
        int currentMax = owner == PlayerOwner.Player ? AllyCurrentMaxMana : EnemyCurrentMaxMana;
        int room = Mathf.Max(0, effectiveCap - currentMax);
        int applied = Mathf.Clamp(mana, 0, room);

        if (applied <= 0)
            return 0;

        GainMaxMana(applied, owner);
        return applied;
    }
    private int GetEffectiveManaCap(PlayerOwner owner)
    {
        return baseManaCap + GetBonusManaCap(owner);
    }

    private int GetBonusManaCap(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? AllyBonusManaCap
            : EnemyBonusManaCap;
    }
    private void IncreaseMaxMana(PlayerOwner owner)
    {
        int effectiveCap = GetEffectiveManaCap(owner);

        if (owner == PlayerOwner.Player)
        {
            if (AllyCurrentMaxMana < effectiveCap)
                AllyCurrentMaxMana++;
        }
        else
        {
            if (EnemyCurrentMaxMana < effectiveCap)
                EnemyCurrentMaxMana++;
        }
    }
    private void RefillMana(PlayerOwner owner)
    {
        int gained;

        if (owner == PlayerOwner.Player)
        {
            gained = Mathf.Max(0, AllyCurrentMaxMana - AllyCurrentMana);
            AllyCurrentMana = AllyCurrentMaxMana;
        }
        else
        {
            gained = Mathf.Max(0, EnemyCurrentMaxMana - EnemyCurrentMana);
            EnemyCurrentMana = EnemyCurrentMaxMana;
        }

        NotifyManaGained(owner, gained);
    }
    #endregion

    #region Effects
    public int ActiveEffectCount { get; private set; } = 0;
    public void EnqueueDeferredAction(Action action)
    {
        if (IsResolvingEffects)
        {
            deferredActions.Enqueue(action);
        }
        else
        {
            action.Invoke();
        }
    }

    public bool IsResolvingEffects => ActiveEffectCount > 0;

    public event Action OnAllEffectsResolved;

    public void BeginEffect()
    {
        ActiveEffectCount++;
    }
    public void EndEffect()
    {
        ActiveEffectCount = Mathf.Max(0, ActiveEffectCount - 1);

        if (ActiveEffectCount == 0)
        {
            OnAllEffectsResolved?.Invoke();
            ResolveDeferredActions();
        }
    }

    private void ResolveDeferredActions()
    {
        while (deferredActions.Count > 0)
        {
            deferredActions.Dequeue().Invoke();
        }
    }
    public IEnumerator DamageRandomEnemy(bool andCore, int ticsDmg, PlayerOwner owner)
    {
        BeginEffect();
        try
        {
            for (int i = 0; i < ticsDmg; i++)
            {
                if (andCore) GetCoreForEnemy(owner).TakeDamage(1);
                if (GetBoardForOther(owner).GetCards().Count > 0)
                {
                    int rndTarget = UnityEngine.Random.Range(0, GetBoardForOther(owner).GetCards().Count);
                    GetBoardForOther(owner).GetCards()[rndTarget].GetComponent<CardInstance>().TakeDamage(1);
                    Debug.Log("Dealt damage to enemy GUN");
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
        finally
        {
            EndEffect();
        }
    }
    public IEnumerator DamageRandomEnemyChaos(bool andCore, int ticsDmg, PlayerOwner owner)
    {
        BeginEffect();
        try
        {
            for (int i = 0; i < ticsDmg; i++)
            {
                var enemyBoard = GetBoardForOther(owner);
                var enemyCards = enemyBoard.GetCards();
                int unitCount = enemyCards.Count;

                // Total possible targets
                int totalTargets = unitCount + (andCore ? 1 : 0);

                if (totalTargets > 0)
                {
                    int rndTarget = UnityEngine.Random.Range(0, totalTargets);

                    if (andCore && rndTarget == unitCount)
                    {
                        // Last index represents the core
                        GetCoreForEnemy(owner).TakeDamage(1);
                    }
                    else
                    {
                        enemyCards[rndTarget]
                            .GetComponent<CardInstance>()
                            .TakeDamage(1);
                    }

                    Debug.Log("Dealt damage to enemy chaos");
                }

                yield return new WaitForSeconds(0.5f);
            }
        }
        finally
        {
            EndEffect();
        }
    }
    // reservation counters to avoid concurrent over-summon
    private int allyPendingSummons = 0;
    private int enemyPendingSummons = 0;
    private readonly Dictionary<CardInstance, int> deploySummonCaps = new();

    private int GetPendingSummons(PlayerOwner owner) =>
        owner == PlayerOwner.Player ? allyPendingSummons : enemyPendingSummons;

    private void IncPendingSummons(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player) allyPendingSummons++;
        else enemyPendingSummons++;
    }

    private void DecPendingSummons(PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player) allyPendingSummons = Mathf.Max(0, allyPendingSummons - 1);
        else enemyPendingSummons = Mathf.Max(0, enemyPendingSummons - 1);
    }

    public void SetDeploySummonCap(CardInstance source, int allowedSummons)
    {
        if (source == null)
            return;

        deploySummonCaps[source] = Mathf.Max(0, allowedSummons);
    }

    public bool ConsumeDeploySummonSlot(CardInstance source)
    {
        if (source == null)
            return true;

        if (!deploySummonCaps.TryGetValue(source, out int remaining))
            return true;

        if (remaining <= 0)
            return false;

        deploySummonCaps[source] = remaining - 1;
        return true;
    }

    public void ClearDeploySummonCap(CardInstance source)
    {
        if (source == null)
            return;

        deploySummonCaps.Remove(source);
    }

    public bool TrySummonForOwnerSafe(PlayerOwner owner, int cardId, bool isTrait = false)
    {
        var board = GetBoardForOwner(owner);

        if (board.IsFull())
            return false;

        TrySummonForOwner(owner, cardId, isTrait);
        return true;
    }
    public void TrySummonForOwner(PlayerOwner owner, int cardId, bool isTrait = false, bool islow = false, int setAtk = -1, int setHp = -1)
    {
        TrySummonForOwnerAndGet(owner, cardId, isTrait, islow, setAtk, setHp);
    }
    public CardInstance TrySummonForOwnerAndGet(PlayerOwner owner, int cardId, bool isTrait = false, bool islow = false, int setAtk = -1, int setHp = -1)
    {
        var board = GetBoardForOwner(owner);
        if (board == null) return null;

        // compute effective occupied slots including pending reservations
        int effectiveCount = board.GetCards().Count + GetPendingSummons(owner);
        if (effectiveCount >= (owner == PlayerOwner.Player ? allyDropArea.maxBoardSize : enemyDropArea.maxBoardSize))
            return null;

        // Reserve a slot immediately to prevent other concurrent summons from oversubscribing.
        IncPendingSummons(owner);

        CardData data = CardDatabase.Instance.GetCardById(cardId);
        if (data == null)
        {
            DecPendingSummons(owner);
            return null;
        }

        ICardDropArea parent = owner == PlayerOwner.Player ? allyDropArea : enemyDropArea;

        // create the card (this instantiates a GameObject)
        CardInstance cardInst = CardFactory.Instance.CreateCard(data, owner, parent.CardContainer);

        if (cardInst == null)
        {
            DecPendingSummons(owner);
            return null;
        }

        // attempt to add to board
        if (owner == PlayerOwner.Player)
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            cardInst.IsSummoningSick = true;
            Card card = cardInst.GetComponent<Card>();
            card.gameObject.GetComponent<CardView>().UpdateMode();
            cardInst.Owner = PlayerOwner.Player;
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
            if (islow)
            {
                cardInst.CurrentHealth = 1;
            }
            if (setAtk > -1)
            {
                cardInst.CurrentAttack = setAtk;
            }
            if (setHp > -1)
            {
                cardInst.CurrentMaxHealth = setHp;
                cardInst.CurrentHealth = setHp;
            }
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            cardInst.IsSummoningSick = true;
            cardInst.Owner = PlayerOwner.Enemy;
            Card card = cardInst.GetComponent<Card>();
            card.gameObject.GetComponent<CardView>().UpdateMode();
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
            if (islow)
            {
                cardInst.CurrentHealth = 1;
            }
        }

        // Verify the card was actually added to board list (AddSummonedCard may early-return when full)
        bool actuallyAdded = parent.GetCards().Contains(cardInst.gameObject);

        if (!actuallyAdded)
        {
            // board might have filled up in-between, destroy created GameObject and free reservation
            Destroy(cardInst.gameObject);
            DecPendingSummons(owner);
            return null;
        }

        // successful add -> release reservation
        DecPendingSummons(owner);

        if (isTrait)
        {
            StartCoroutine(DelayedDeploy(cardInst, forceRandomTarget: true));
        }
        return cardInst;
    }
    public void TrySummonForOwnerNergi(PlayerOwner owner)
    {
        var board = GetBoardForOwner(owner);
        if (board == null) return;

        // compute effective occupied slots including pending reservations
        int effectiveCount = board.GetCards().Count + GetPendingSummons(owner);
        if (effectiveCount >= (owner == PlayerOwner.Player ? allyDropArea.maxBoardSize : enemyDropArea.maxBoardSize))
            return;

        // Reserve a slot immediately to prevent other concurrent summons from oversubscribing.
        IncPendingSummons(owner);

        CardData data = CardDatabase.Instance.GetCardById(228);
        if (data == null)
        {
            DecPendingSummons(owner);
            return;
        }

        ICardDropArea parent = owner == PlayerOwner.Player ? allyDropArea : enemyDropArea;

        // create the card (this instantiates a GameObject)
        CardInstance cardInst = CardFactory.Instance.CreateCard(data, owner, parent.CardContainer);

        if (cardInst == null)
        {
            DecPendingSummons(owner);
            return;
        }

        // attempt to add to board
        if (owner == PlayerOwner.Player)
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            cardInst.IsSummoningSick = true;
            Card card = cardInst.GetComponent<Card>();
            card.gameObject.GetComponent<CardView>().UpdateMode();
            cardInst.Owner = PlayerOwner.Player;
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
            cardInst.ModifyStats(1,1);
            cardInst.CurrentEffect += " protect quickstrike regeneration";
            cardInst.CurrentEffectText += "\nProtect Quickstrike and Regeneration";
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            cardInst.IsSummoningSick = true;
            cardInst.Owner = PlayerOwner.Enemy;
            Card card = cardInst.GetComponent<Card>();
            card.gameObject.GetComponent<CardView>().UpdateMode();
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
            cardInst.ModifyStats(2, 2);
            cardInst.CurrentEffect += " protect quickstrike regeneration";
            cardInst.CurrentEffectText += "\nProtect Quickstrike and Regeneration";
        }

        // Verify the card was actually added to board list (AddSummonedCard may early-return when full)
        bool actuallyAdded = parent.GetCards().Contains(cardInst.gameObject);

        if (!actuallyAdded)
        {
            // board might have filled up in-between, destroy created GameObject and free reservation
            Destroy(cardInst.gameObject);
            DecPendingSummons(owner);
            return;
        }

        // successful add -> release reservation
        DecPendingSummons(owner);
    }

    public void TrySummonForOwnerEffect(PlayerOwner owner, string effect, bool isTrait = false)
    {
        var board = GetBoardForOwner(owner);
        if (board == null) return;

        // compute effective occupied slots including pending reservations
        int effectiveCount = board.GetCards().Count + GetPendingSummons(owner);
        if (effectiveCount >= (owner == PlayerOwner.Player ? allyDropArea.maxBoardSize : enemyDropArea.maxBoardSize))
            return;

        // Reserve a slot immediately to prevent other concurrent summons from oversubscribing.
        IncPendingSummons(owner);

        List<CardData> options = CardDatabase.Instance.GetCardsByEffect(effect);
        options = options.FindAll(card => card.cardType == "minion");
        int range = options.Count;
        if (range <= 0) return;
        CardData data = options[UnityEngine.Random.Range(0, range)];

        if (data == null)
        {
            DecPendingSummons(owner);
            return;
        }

        ICardDropArea parent = owner == PlayerOwner.Player ? allyDropArea : enemyDropArea;

        // create the card (this instantiates a GameObject)
        CardInstance cardInst = CardFactory.Instance.CreateCard(data, owner, parent.CardContainer);

        if (cardInst == null)
        {
            DecPendingSummons(owner);
            return;
        }

        // attempt to add to board
        if (owner == PlayerOwner.Player)
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
        }

        // Verify the card was actually added to board list (AddSummonedCard may early-return when full)
        bool actuallyAdded = parent.GetCards().Contains(cardInst.gameObject);

        if (!actuallyAdded)
        {
            // board might have filled up in-between, destroy created GameObject and free reservation
            Destroy(cardInst.gameObject);
            DecPendingSummons(owner);
            return;
        }

        // successful add -> release reservation
        DecPendingSummons(owner);


        if (isTrait)
        {
            StartCoroutine(DelayedDeploy(cardInst, forceRandomTarget: true));
        }
    }
    public void TrySummonForOwnerTrait(PlayerOwner owner, string trait, bool isTrait = false)
    {
        var board = GetBoardForOwner(owner);
        if (board == null) return;

        // compute effective occupied slots including pending reservations
        int effectiveCount = board.GetCards().Count + GetPendingSummons(owner);
        if (effectiveCount >= (owner == PlayerOwner.Player ? allyDropArea.maxBoardSize : enemyDropArea.maxBoardSize))
            return;

        // Reserve a slot immediately to prevent other concurrent summons from oversubscribing.
        IncPendingSummons(owner);

        List<CardData> options = CardDatabase.Instance.GetCardsByTraitPackable(trait);
        options = options.FindAll(card => card.cardType == "minion");
        int range = options.Count;
        if (range <= 0) return;
        CardData data = options[UnityEngine.Random.Range(0, range)];

        if (data == null)
        {
            DecPendingSummons(owner);
            return;
        }

        ICardDropArea parent = owner == PlayerOwner.Player ? allyDropArea : enemyDropArea;

        // create the card (this instantiates a GameObject)
        CardInstance cardInst = CardFactory.Instance.CreateCard(data, owner, parent.CardContainer);

        if (cardInst == null)
        {
            DecPendingSummons(owner);
            return;
        }

        // attempt to add to board
        if (owner == PlayerOwner.Player)
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
        }

        // Verify the card was actually added to board list (AddSummonedCard may early-return when full)
        bool actuallyAdded = parent.GetCards().Contains(cardInst.gameObject);

        if (!actuallyAdded)
        {
            // board might have filled up in-between, destroy created GameObject and free reservation
            Destroy(cardInst.gameObject);
            DecPendingSummons(owner);
            return;
        }

        // successful add -> release reservation
        DecPendingSummons(owner);


        if (isTrait)
        {
            StartCoroutine(DelayedDeploy(cardInst, forceRandomTarget: true));
        }
    }
    public void TrySummonForOwnerManaCost(PlayerOwner owner, int manaCost, bool isTrait = false)
    {
        var board = GetBoardForOwner(owner);
        if (board == null) return;

        // compute effective occupied slots including pending reservations
        int effectiveCount = board.GetCards().Count + GetPendingSummons(owner);
        if (effectiveCount >= (owner == PlayerOwner.Player ? allyDropArea.maxBoardSize : enemyDropArea.maxBoardSize))
            return;

        // Reserve a slot immediately to prevent other concurrent summons from oversubscribing.
        IncPendingSummons(owner);

        List<CardData> options = CardDatabase.Instance.GetCardsByManaCost(manaCost);
        options = options.FindAll(card => card.cardType=="minion");
        int range = options.Count;
        if (range <= 0) return;
        CardData data = options[UnityEngine.Random.Range(0, range)];

        if (data == null)
        {
            DecPendingSummons(owner);
            return;
        }

        ICardDropArea parent = owner == PlayerOwner.Player ? allyDropArea : enemyDropArea;

        // create the card (this instantiates a GameObject)
        CardInstance cardInst = CardFactory.Instance.CreateCard(data, owner, parent.CardContainer);

        if (cardInst == null)
        {
            DecPendingSummons(owner);
            return;
        }

        // attempt to add to board
        if (owner == PlayerOwner.Player)
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
        }

        // Verify the card was actually added to board list (AddSummonedCard may early-return when full)
        bool actuallyAdded = parent.GetCards().Contains(cardInst.gameObject);

        if (!actuallyAdded)
        {
            // board might have filled up in-between, destroy created GameObject and free reservation
            Destroy(cardInst.gameObject);
            DecPendingSummons(owner);
            return;
        }

        // successful add -> release reservation
        DecPendingSummons(owner);


        if (isTrait)
        {
            StartCoroutine(DelayedDeploy(cardInst, forceRandomTarget: true));
        }
    }
    public IEnumerator DelayedDeploy(CardInstance card, bool forceRandomTarget = false)
    {
        yield return null; // wait one frame
        if (card != null)
            card.TriggerDeploy(forceRandomTarget);
    }
    public void TrySummonForOther(PlayerOwner owner, int cardId)
    {
        // This means: summon on the opponent's board
        var board = GetBoardForOther(owner);
        if (board == null)
            return;

        PlayerOwner targetOwner =
            owner == PlayerOwner.Player
                ? PlayerOwner.Enemy
                : PlayerOwner.Player;

        int effectiveCount =
            board.GetCards().Count + GetPendingSummons(targetOwner);

        int maxSize =
    targetOwner == PlayerOwner.Player
        ? allyDropArea.maxBoardSize
        : enemyDropArea.maxBoardSize;

        if (effectiveCount >= maxSize)
            return;

        // 🔒 Reserve slot
        IncPendingSummons(targetOwner);

        CardData data = CardDatabase.Instance.GetCardById(cardId);
        if (data == null)
        {
            DecPendingSummons(targetOwner);
            return;
        }

        ICardDropArea parent =
            targetOwner == PlayerOwner.Player
                ? allyDropArea
                : enemyDropArea;

        CardInstance cardInst =
            CardFactory.Instance.CreateCard(data, targetOwner, parent.CardContainer);

        if (cardInst == null)
        {
            DecPendingSummons(targetOwner);
            return;
        }

        // Add to board
        if (targetOwner == PlayerOwner.Player)
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 1;
            cardInst.SetZone(CardZone.Board);
            allyDropArea.AddSummonedCard(cardInst);
            allyDropArea.UpdateAllyCardPositions();
        }
        else
        {
            cardInst.GetComponent<SortingGroup>().sortingOrder = 3;
            cardInst.SetZone(CardZone.Board);
            enemyDropArea.AddSummonedCard(cardInst);
            enemyDropArea.UpdateEnemyCardPositions();
        }

        // Verify success
        bool actuallyAdded =
            parent.GetCards().Contains(cardInst.gameObject);

        if (!actuallyAdded)
        {
            Destroy(cardInst.gameObject);
            DecPendingSummons(targetOwner);
            return;
        }

        // ✅ Success
        DecPendingSummons(targetOwner);

    }

    public void NotifyUnitEnteredBoard(CardInstance summonedCard)
    {
        if (summonedCard == null)
            return;

        StartCoroutine(TriggerAutoHitAfterSummonDelay(summonedCard));
    }

    private IEnumerator TriggerAutoHitAfterSummonDelay(CardInstance summonedCard)
    {
        // Let board placement / layout settle for a few frames before triggering autohit.
        for (int i = 0; i < 3; i++)
            yield return new WaitForEndOfFrame();

        if (summonedCard == null || summonedCard.IsDead || summonedCard.CurrentZone != CardZone.Board)
            yield break;

        PlayerOwner opposingOwner =
            summonedCard.Owner == PlayerOwner.Player
                ? PlayerOwner.Enemy
                : PlayerOwner.Player;

        var opposingBoard = GetBoardForOwner(opposingOwner);
        foreach (GameObject cardObj in opposingBoard.GetCards())
        {
            if (cardObj == null)
                continue;

            CardInstance autoHitter = cardObj.GetComponent<CardInstance>();
            if (autoHitter == null || autoHitter.IsDead || autoHitter.CurrentZone != CardZone.Board)
                continue;

            if (!autoHitter.HasKeyword("autohit"))
                continue;

            QueueAttack(
                autoHitter,
                summonedCard,
                consumeAttackForTurn: false,
                bypassSelectionRules: true,
                allowRetarget: false);
        }
    }

    public bool IsAntiRandomActive()
    {
        return BoardHasKeyword(allyDropArea.GetCards(), "antirandom") ||
               BoardHasKeyword(enemyDropArea.GetCards(), "antirandom");
    }

    public bool ShouldBlockRandomCardPlay(CardInstance cardInst)
    {
        if (cardInst == null)
            return false;

        return IsAntiRandomActive() && cardInst.HasText("random");
    }

    private bool BoardHasKeyword(List<GameObject> cards, string keyword)
    {
        if (cards == null)
            return false;

        foreach (GameObject cardObj in cards)
        {
            if (cardObj == null)
                continue;

            CardInstance boardCard = cardObj.GetComponent<CardInstance>();
            if (boardCard == null || boardCard.IsDead || boardCard.CurrentZone != CardZone.Board)
                continue;

            if (boardCard.HasKeyword(keyword))
                return true;
        }

        return false;
    }

    public void Praise(PlayerOwner owner)
    {
        OnPraise?.Invoke(owner);
    }
    public void Hissatsu(PlayerOwner owner)
    {
        OnHissatsuPlayed?.Invoke(owner);
    }
    public void DamageRandomEnemyAmount(int amount, PlayerOwner owner)
    {
        List<IAttackable> possibleTargets = new();

        // Add enemy minions
        var enemyBoard = GetBoardForOther(owner).GetCards();
        foreach (var go in enemyBoard)
        {
            if (go == null) continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead) continue;

            possibleTargets.Add(ci);
        }

        CoreInstance enemyCore = GetCoreForEnemy(owner);

        // If no minions, core MUST be hit
        if (possibleTargets.Count == 0)
        {
            enemyCore.TakeDamage(amount);
            Debug.Log("Random damage hit enemy CORE (no units)");
            return;
        }

        // If minions exist, core is also a valid random target
        possibleTargets.Add(enemyCore);

        // Pick random target
        IAttackable target =
            possibleTargets[UnityEngine.Random.Range(0, possibleTargets.Count)];

        target.TakeDamage(amount);

        Debug.Log($"Random damage hit: {target.GetType().Name}");
    }
    public void EmperorSapphire(PlayerOwner owner)
    {
        // 🔑 Snapshot the board to avoid collection modification
        List<GameObject> allOwnerAllies =
            new List<GameObject>(GetBoardForOwner(owner).GetCards());

        foreach (GameObject ownerAlly in allOwnerAllies)
        {
            if (ownerAlly == null) continue;

            CardInstance cardInst = ownerAlly.GetComponent<CardInstance>();
            if (cardInst == null || cardInst.IsDead) continue;

            if (cardInst.Data.id == 15)
            {
                StartCoroutine(KillCrystalWithDelay(cardInst, owner));
            }
        }
    }
    private IEnumerator KillCrystalWithDelay(CardInstance cardInst, PlayerOwner owner)
    {
        yield return new WaitForSeconds(0.1f);

        if (cardInst == null || cardInst.IsDead)
            yield break;

        cardInst.TakeDamage(999);
        DamageRandomEnemyAmount(7, owner);
    }

    public void OnDamageWithCard(PlayerOwner owner)
    {
        OnDamageCard?.Invoke(owner);
    }
    public void OnDamageWithCardInstance(CardInstance inst)
    {
        OnDamageCardInstance?.Invoke(inst);
    }
    public void LimitEnemySpace(PlayerOwner effectOwner, int limit)
    {
        ICardDropArea board =
            effectOwner == PlayerOwner.Player
                ? enemyDropArea
                : allyDropArea;

        if (board is EnemyCardDropArea enemyBoard)
        {
            ApplyBoardLimit(enemyBoard.enemyPrefabCards, limit, enemyBoard.HandleEnemyDeath, enemyBoard.UpdateEnemyCardPositions);
            enemyBoard.maxBoardSize = limit;
        }
        else if (board is AllyCardDropArea allyBoard)
        {
            ApplyBoardLimit(allyBoard.allyPrefabCards, limit, allyBoard.HandleAllyDeath, allyBoard.UpdateAllyCardPositions);
            allyBoard.maxBoardSize = limit;
        }
    }
    private void ApplyBoardLimit(   List<GameObject> cards,   int limit,
        System.Action<CardInstance> deathHandler,   System.Action updateLayout)
    {
        if (cards == null)
            return;

        Debug.Log("Applying Board limit");

        // Destroy newest cards first
        while (cards.Count > limit)
        {
            GameObject last = cards[^1];

            if (last == null)
            {
                cards.RemoveAt(cards.Count - 1);
                continue;
            }

            CardInstance ci = last.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
            {
                // 🔑 death handler will remove + destroy + relayout
                deathHandler.Invoke(ci);
            }
            else
            {
                cards.RemoveAt(cards.Count - 1);
                Destroy(last);
            }
        }

        updateLayout?.Invoke();
    }

    public void Discover(int id1, int id2, int id3, PlayerOwner owner)
    {
        if(owner == PlayerOwner.Enemy)
        {
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                int randintDisc = UnityEngine.Random.Range(0, 3);
                if (randintDisc == 0) AddCardToHand(PlayerOwner.Enemy, id1,-1);
                if (randintDisc == 1) AddCardToHand(PlayerOwner.Enemy, id2,-1);
                if (randintDisc == 2) AddCardToHand(PlayerOwner.Enemy, id3,-1);
                if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
                return;
            }
            int randint = UnityEngine.Random.Range(0, 3);
            if (randint == 0) AddCardToHand(PlayerOwner.Enemy, id1);
            if (randint == 1) AddCardToHand(PlayerOwner.Enemy, id2);
            if (randint == 2) AddCardToHand(PlayerOwner.Enemy, id3);
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = CardDatabase.Instance.GetCardById(id1);
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f,0.6f,0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        CardData data2 = CardDatabase.Instance.GetCardById(id2);
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5,0,0), new Vector3(0.6f,0.6f,0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        CardData data3 = CardDatabase.Instance.GetCardById(id3);
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f,0.6f,0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;

        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverEffect(string effect1, string effect2, string effect3, PlayerOwner owner)
    {
        List<CardData> options1 = CardDatabase.Instance.GetCardsByEffect(effect1 + "*");
        List<CardData> options2 = CardDatabase.Instance.GetCardsByEffect(effect2 + "*");
        List<CardData> options3 = CardDatabase.Instance.GetCardsByEffect(effect3 + "*");

        if (effect1.Contains("gear", StringComparison.OrdinalIgnoreCase))
            options1 = FilterCardsStartingWithEffectKeyword(options1, "gear");
        if (effect2.Contains("gear", StringComparison.OrdinalIgnoreCase))
            options2 = FilterCardsStartingWithEffectKeyword(options2, "gear");
        if (effect3.Contains("gear", StringComparison.OrdinalIgnoreCase))
            options3 = FilterCardsStartingWithEffectKeyword(options3, "gear");

        if (options1.Count <= 0) return;
        if (owner == PlayerOwner.Enemy)
        {
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                int numb = UnityEngine.Random.Range(0, 3);
                switch (numb)
                {
                    case 0:
                        AddCardToHand(PlayerOwner.Enemy, options1[UnityEngine.Random.Range(0, options1.Count)].id, -1);
                        if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner); return;
                    case 1:
                        AddCardToHand(PlayerOwner.Enemy, options2[UnityEngine.Random.Range(0, options2.Count)].id, -1);
                        if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner); return;
                    case 2:
                        AddCardToHand(PlayerOwner.Enemy, options3[UnityEngine.Random.Range(0, options3.Count)].id, -1);
                        if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner); return;

                }
            }
            else
            {
                int numb = UnityEngine.Random.Range(0, 3);
                switch (numb)
                {
                    case 0:
                        AddCardToHand(PlayerOwner.Enemy, options1[UnityEngine.Random.Range(0, options1.Count)].id, -1);
                        if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner); return;
                    case 1:
                        AddCardToHand(PlayerOwner.Enemy, options2[UnityEngine.Random.Range(0, options2.Count)].id, -1);
                        if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner); return;
                    case 2:
                        AddCardToHand(PlayerOwner.Enemy, options3[UnityEngine.Random.Range(0, options3.Count)].id, -1);
                        if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner); return;

                }
            }
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = options1[UnityEngine.Random.Range(0, options1.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        CardData data2 = options2[UnityEngine.Random.Range(0, options2.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        CardData data3 = options3[UnityEngine.Random.Range(0, options3.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverEffect(string effect, PlayerOwner owner)
    {
        List<CardData> options = CardDatabase.Instance.GetCardsByEffect(effect+"*");
        if (effect.Contains("gear", StringComparison.OrdinalIgnoreCase))
            options = FilterCardsStartingWithEffectKeyword(options, "gear");

        Debug.Log(effect + "*");
        string res = "options :";
        foreach(CardData option in options)
        {
            res += option.name + " ";
        }
        if (options.Count <= 0) return;
        if (owner == PlayerOwner.Enemy)
        {
            if(OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id,-1);
                if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
                return;
            }
            AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id);
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data1);
        CardData data2 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data2);
        CardData data3 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data3);
        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverEffectSelective(string effect,string banned, PlayerOwner owner)
    {
        List<CardData> options = CardDatabase.Instance.GetCardsByEffect(effect + "*");
        options = options.Where(t => !t.effect.ToLower().Contains(banned.ToLower())).ToList();
        if (effect.Contains("gear", StringComparison.OrdinalIgnoreCase))
            options = FilterCardsStartingWithEffectKeyword(options, "gear");

        Debug.Log(effect + "*");
        string res = "options :";
        foreach (CardData option in options)
        {
            res += option.name + " ";
        }
        if (options.Count <= 0) return;
        if (owner == PlayerOwner.Enemy)
        {
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id, -1);
                if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
                return;
            }
            AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id);
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data1);
        CardData data2 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data2);
        CardData data3 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data3);
        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverAura(PlayerOwner owner)
    {
        List<CardData> options = CardDatabase.Instance.GetCardsByTextPackable("aura*");
        if (options == null || options.Count < 3)
            return;

        if (owner == PlayerOwner.Enemy)
        {
            int randomIndex = UnityEngine.Random.Range(0, options.Count);
            int discount = OwnerHasTrait(owner, CardData.Trait.Faith, 2) ? -1 : 0;
            AddCardToHand(PlayerOwner.Enemy, options[randomIndex].id, discount);

            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3))
                GainMana(1, owner);

            return;
        }

        isDiscovering = true;
        discoverDisplay.SetActive(true);

        CardData data1 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data1);

        CardData data2 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data2);

        CardData data3 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;

        OnDiscover?.Invoke(owner);
    }
    public void DiscoverEffectDiscount(string effect, PlayerOwner owner, int manaDiscount)
    {
        DiscoverDiscount = manaDiscount;
        List<CardData> options = CardDatabase.Instance.GetCardsByEffect(effect + "*");
        if (effect.Contains("gear", StringComparison.OrdinalIgnoreCase))
            options = FilterCardsStartingWithEffectKeyword(options, "gear");

        Debug.Log(effect + "*");
        string res = "options :";
        foreach (CardData option in options)
        {
            res += option.name + " ";
        }
        if (options.Count <= 0) return;
        if (owner == PlayerOwner.Enemy)
        {
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id, -(manaDiscount)-1);
                if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
                return;
            }
            AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id,-manaDiscount);
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        DiscoverDiscount = manaDiscount;
        CardData data1 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data1);
        CardData data2 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data2);
        CardData data3 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data3);
        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverTrait(string trait, PlayerOwner owner)
    {
        List<CardData> options =
       new List<CardData>(CardDatabase.Instance.GetCardsByTraitPackable(trait));

        string res = "options :";
        foreach (CardData option in options)
        {
            res += option.name + " ";
        }
        if (options.Count <= 0) return;
        if (owner == PlayerOwner.Enemy)
        {
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
            {
                AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id, -1);
                if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
                return;
            }
            AddCardToHand(PlayerOwner.Enemy, options[UnityEngine.Random.Range(0, options.Count)].id);
            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3)) GainMana(1, owner);
            return;
        }
        isDiscovering = true;
        discoverDisplay.SetActive(true);
        CardData data1 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(data1, PlayerOwner.Player, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data1);
        CardData data2 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(data2, PlayerOwner.Player, new Vector3(5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data2);
        CardData data3 = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(data3, PlayerOwner.Player, new Vector3(-5, 0, 0), new Vector3(0.6f, 0.6f, 0.6f), discoverDisplay.transform);
        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
        options.Remove(data3);
        //Call Discover
        OnDiscover?.Invoke(owner);
    }
    public void DiscoverOwnerTrait(PlayerOwner owner)
    {
        // 1️⃣ Get trait pool from DECK (not active effects)
        Dictionary<CardData.Trait, int> traitPool =
            owner == PlayerOwner.Player
                ? deckManager.AllyTraitsUnlockable
                : deckManager.EnemyTraitsUnlockable;

        if (traitPool == null || traitPool.Count == 0)
            return;

        // 2️⃣ Pick up to 3 distinct traits
        // 2️⃣ Build exactly 3 traits based on owner's pool
        List<CardData.Trait> availableTraits =
            traitPool.Keys
                     .Where(t => t != CardData.Trait.Neutral)
                     .ToList();

        List<CardData.Trait> traits = new();

        if (availableTraits.Count == 0)
        {
            // No traits → all Neutral
            traits.Add(CardData.Trait.Neutral);
            traits.Add(CardData.Trait.Neutral);
            traits.Add(CardData.Trait.Neutral);
        }
        else if (availableTraits.Count == 1)
        {
            // One trait → all same
            traits.Add(availableTraits[0]);
            traits.Add(availableTraits[0]);
            traits.Add(availableTraits[0]);
        }
        else if (availableTraits.Count == 2)
        {
            // Two traits → one duplicated, one single
            bool firstIsDouble = UnityEngine.Random.value < 0.5f;

            CardData.Trait a = availableTraits[0];
            CardData.Trait b = availableTraits[1];

            if (firstIsDouble)
            {
                traits.Add(a);
                traits.Add(a);
                traits.Add(b);
            }
            else
            {
                traits.Add(b);
                traits.Add(b);
                traits.Add(a);
            }
        }
        else
        {
            // 3+ traits → pick 3 distinct
            traits = availableTraits
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(3)
                .ToList();
        }


        // 3️⃣ Pick one card per trait
        List<CardData> options = new();

        foreach (var trait in traits)
        {
            List<CardData> cards =
                CardDatabase.Instance.GetCardsByTraitPackable(trait.ToString());

            if (cards.Count > 0)
                options.Add(cards[UnityEngine.Random.Range(0, cards.Count)]);
        }

        if (options.Count == 0)
            return;

        // 4️⃣ Enemy: auto-pick
        if (owner == PlayerOwner.Enemy)
        {
            CardData choice = options[UnityEngine.Random.Range(0, options.Count)];

            if (OwnerHasTrait(owner, CardData.Trait.Faith, 2))
                AddCardToHand(owner, choice.id, -1);
            else
                AddCardToHand(owner, choice.id);

            if (OwnerHasTrait(owner, CardData.Trait.Faith, 3))
                GainMana(1, owner);

            return;
        }

        // 5️⃣ Player: Discover UI
        isDiscovering = true;
        discoverDisplay.SetActive(true);

        float[] xPos = { 0f, 5f, -5f };

        for (int i = 0; i < options.Count && i < 3; i++)
        {
            CardInstance preview =
                CardFactory.Instance.CreateCardInPosition(
                    options[i],
                    PlayerOwner.Player,
                    new Vector3(xPos[i], 0, 0),
                    new Vector3(0.6f, 0.6f, 0.6f),
                    discoverDisplay.transform
                );

            preview.IsDisplay = true;
            preview.GetComponent<SortingGroup>().sortingOrder = 201;
        }

        OnDiscover?.Invoke(owner);
    }
    private List<CardData> FilterCardsStartingWithEffectKeyword(List<CardData> cards, string keyword)
    {
        if (cards == null || string.IsNullOrWhiteSpace(keyword))
            return cards ?? new List<CardData>();

        return cards
            .Where(card => !string.IsNullOrWhiteSpace(card.effect)
                && card.effect.TrimStart().StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void ShuffleInDeck(int id, PlayerOwner owner)
    {
        CardData shuffledCard = CardDatabase.Instance.GetCardById(id);
        deckManager.decks[owner].Enqueue(shuffledCard);
        deckManager.Shuffle(deckManager.decks[owner]);
    }
    public CardInstance AddCardToHand(PlayerOwner owner, int id)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        CardData data = CardDatabase.Instance.GetCardById(id);

        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddRandomCardToHandType(PlayerOwner owner, string type, int prohibitedId)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        List<CardData> options = CardDatabase.Instance.GetCardsByTypePackable(type);
        options = options.FindAll(card => card.id != prohibitedId);
        if (options.Count == 0)
            return null;
        CardData data = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddRandomCardNonPackable(PlayerOwner owner, int prohibitedId)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        List<CardData> options = CardDatabase.Instance.GetNonPackableCards();
        options = options.FindAll(card => card.id != prohibitedId);
        options = options.FindAll(card => card.id != 39);

        if (options.Count == 0)
            return null;
        CardData data = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddRandomCardToHandEffectSelective(PlayerOwner owner, string text, string banned, int prohibitedId)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        List<CardData> options = CardDatabase.Instance.GetCardsByEffect(text);
        options = options.FindAll(card => card.id != prohibitedId && card.packable && !card.effect.ToLower().Contains(banned.ToLower()));
        if (text.Contains("gear", StringComparison.OrdinalIgnoreCase))
            options = FilterCardsStartingWithEffectKeyword(options, "gear");
        if (options.Count == 0)
            return null;
        CardData data = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddRandomCardToHandEffect(PlayerOwner owner, string text, int prohibitedId)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        List<CardData> options = CardDatabase.Instance.GetCardsByEffect(text);
        options = options.FindAll(card => card.id != prohibitedId && card.packable);
        if (text.Contains("gear", StringComparison.OrdinalIgnoreCase))
            options = FilterCardsStartingWithEffectKeyword(options, "gear");
        if (options.Count == 0)
            return null;
        CardData data = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddRandomCardToHandText(PlayerOwner owner, string text, int prohibitedId)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        List<CardData> options = CardDatabase.Instance.GetCardsByTextPackable(text);
        options = options.FindAll(card => card.id != prohibitedId);

        if (options.Count == 0)
            return null;
        CardData data = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddRandomCardToHandTrait(PlayerOwner owner, string trait, int prohibitedId)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }
        List<CardData> options =
       new List<CardData>(CardDatabase.Instance.GetCardsByTraitPackable(trait));
        options = options.FindAll(card => card.id != prohibitedId);

        if (options.Count == 0)
            return null;
        CardData data = options[UnityEngine.Random.Range(0, options.Count)];
        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public CardInstance AddCardToHand(PlayerOwner owner, int id, int discount)
    {
        HandManager hand = owner == PlayerOwner.Player
            ? allyHand
            : enemyHand;

        if (hand.handCards.Count >= hand.maxHandSize)
        {
            Debug.Log($"{owner} hand is full.");
            return null;
        }

        CardData data = CardDatabase.Instance.GetCardById(id);

        CardInstance card =
            CardFactory.Instance.CreateCard(data, owner);

        card.SetZone(CardZone.Hand);
        card.AddTemporaryManaModifier(discount);
        hand.AddCard(card.gameObject);
        hand.UpdateCardPositions();

        return card;
    }
    public void WipeBoard()
    {
        // Copy lists first to avoid modification during iteration
        var allyCards = new List<GameObject>(allyDropArea.allyPrefabCards);
        var enemyCards = new List<GameObject>(enemyDropArea.enemyPrefabCards);

        foreach (GameObject ally in allyCards)
        {
            if (ally == null) continue;

            var card = ally.GetComponentInChildren<CardInstance>();
            if (card != null)
                Kill(card);
        }

        foreach (GameObject enemy in enemyCards)
        {
            if (enemy == null) continue;

            var card = enemy.GetComponentInChildren<CardInstance>();
            if (card != null)
                Kill(card);
        }
    }
    public void ScrambleAllUnitsStats()
    {
        // Copy lists first to avoid modification during iteration
        var allyCards = new List<GameObject>(allyDropArea.allyPrefabCards);
        var enemyCards = new List<GameObject>(enemyDropArea.enemyPrefabCards);

        foreach (GameObject ally in allyCards)
        {
            if (ally == null) continue;

            var card = ally.GetComponentInChildren<CardInstance>();
            card.ScrambleStats();
        }

        foreach (GameObject enemy in enemyCards)
        {
            if (enemy == null) continue;

            var card = enemy.GetComponentInChildren<CardInstance>();
            card.ScrambleStats();
        }
    }
    public void Kill(CardInstance target)
    {
        target.IsDying = true;
        target.Die();
        return;
    }
    public int GetFootballCount(PlayerOwner owner)
    {
        Graveyard graveyard =
            owner == PlayerOwner.Player
                ? PlayerGraveyard
                : EnemyGraveyard;
        int count = 0;
        foreach(CardData card in graveyard.Cards)
        {
            if (card.effect.Contains("football")) count++;
        }
        return count;
    }
    public void ResurrectLast(PlayerOwner owner, CardData excluded)
    {
        Graveyard graveyard =
            owner == PlayerOwner.Player
                ? PlayerGraveyard
                : EnemyGraveyard;

        CardData data = graveyard.PopLastExcluding(excluded);
        if (data == null)
            return;

        TrySummonForOwner(owner, data.id);
    }
    public void ResurrectRandom(PlayerOwner owner, CardData excluded)
    {
        Graveyard graveyard =
            owner == PlayerOwner.Player
                ? PlayerGraveyard
                : EnemyGraveyard;

        CardData data = graveyard.PopRandomExcluding(excluded);
        if (data == null)
            return;

        TrySummonForOwner(owner, data.id);
    }
    public void ResurrectLow(PlayerOwner owner, CardData excluded)
    {
        Graveyard graveyard =
            owner == PlayerOwner.Player
                ? PlayerGraveyard
                : EnemyGraveyard;

        CardData data = graveyard.PopRandomExcluding(excluded);
        if (data == null)
            return;

        TrySummonForOwner(owner, data.id, islow:true);
    }
    #endregion

    #region Combat Manager
    public void NotifyCardKilled(CardInstance deadCard)
    {
        OnCardKilled?.Invoke(deadCard);
        HandlePartnerDeathTriggers(deadCard);
        UpdatePartnerLinks();

        // 🔑 Add to graveyard
        if (deadCard.Owner == PlayerOwner.Player)
            PlayerGraveyard.Add(deadCard.Data);
        else
            EnemyGraveyard.Add(deadCard.Data);

        if (deadCard.Data.id == 86)
            DistortionWorld = false;
    }

    public void NotifyHealed(PlayerOwner owner, int amount)
    {
        OnOwnerHeal?.Invoke(owner, amount);
    }
    public void NotifyHealResolved(PlayerOwner owner, IAttackable target, int healedAmount, int overhealAmount)
    {
        OnOwnerHealResolved?.Invoke(owner, target, healedAmount, overhealAmount);
    }
    public void NotifyDamage(PlayerOwner owner, int amount)
    {
        OnOwnerDamage?.Invoke(owner, amount);
    }
    public void NotifyManaGained(PlayerOwner owner, int amount)
    {
        if (amount <= 0)
            return;

        OnOwnerManaGain?.Invoke(owner, amount);
    }
    public void CancelCurrentTargeting()
    {
        // Cancel attack targeting
        if (isTargettingAttack)
        {
            CancelAttackTargeting();
            return;
        }

        // Cancel effect / spell targeting
        if (isTargetingEffect)
        {
            CancelEffectTargeting();
            return;
        }
    }
    private void CancelEffectTargeting()
    {
        if (effectSource == null)
            return;
        Debug.Log("[CANCEL] Effect targeting cancelled");
        if (effectSource is not CardInstance card)
            return;

        card.SetZone(CardZone.Hand);
        card.GetComponent<CardView>().UpdateMode();
        card.ClearTemporaryManaModifiers();
        card.GetComponent<Card>().ResetCard();
        card.DeployPending = false;
        card.CurrentCastEffect = null;
        card.CancelPendingResolution();
        if(card.Data.cardType=="minion")GainMana(card.CurrentManaCost, PlayerOwner.Player);
        // Restore card so it behaves like a fresh hand card
        card.WasPlayed = true;
        card.EffectsSuppressed = false;
        card.DeployPending = false;
        card.IsSummoningSick = false;

        // Rebuild effect parsing & progress hooks
        card.InitializeProgressIfAny();
        card.ParseEffects();

        allyHand.AddCard(card.gameObject);
        allyHand.UpdateCardPositions();
        allyDropArea.allyPrefabCards.Remove(card.gameObject);

        isTargetingEffect = false;
        attackCursor.gameObject.SetActive(false);

        card = null;
        onEffectTargetChosen = null;
        targetType = EffectTarget.None;

        CheckGlow();
    }

    private void CancelAttackTargeting()
    {
        isTargettingAttack = false;
        attackCursor.gameObject.SetActive(false);

        // IMPORTANT: do NOT mark attacker as having attacked
        currentAttacker = null;

        // Reset glows
        CheckGlow();
    }
    public bool BoardHasCard(PlayerOwner owner, int id)
    {
        if(owner == PlayerOwner.Player)
        {
            foreach(GameObject cardGO in allyDropArea.allyPrefabCards)
            {
                CardInstance inst = cardGO.GetComponent<CardInstance>();

                if (inst.Data.id == id) return true;
            }
        }
        else
        {
            foreach (GameObject cardGO in enemyDropArea.enemyPrefabCards)
            {
                CardInstance inst = cardGO.GetComponent<CardInstance>();

                if (inst.Data.id == id) return true;
            }
        }

        return false;
    }
    public void ShakeCameraForDamage(int damage)
    {
        float strength = 0f;
        float duration = 0.1f;

        if (damage <= 3)
            return; // no shake
        else if (damage <= 5)
            strength = 0.08f;
        else if (damage <= 9)
            strength = 0.14f;
        else
            strength = Mathf.Clamp(0.18f + (damage - 10) * 0.03f, 0.18f, 0.35f);

        cameraShakeTween?.Kill();

        cameraShakeTween = mainCamera.transform.DOShakePosition(
            duration,
            strength,
            vibrato: 18,
            randomness: 90,
            fadeOut: true
        ).OnComplete(() =>
        {
            mainCamera.transform.position = cameraBasePos;
        });
    }
    public void BeginAttack(Card attacker)
    {
        if (CurrentGameState != GameState.Playing)
            return;

        CardInstance attackerInst = attacker.GetComponent<CardInstance>();
        if (attackerInst == null)
            return;

        if (attackerInst.Data.cardType.ToLower() == "spell")//Add Spell Logic later
            return;

        if (attackerInst.CurrentZone != CardZone.Board)
            return;

        if (!TurnManager.Instance.IsPlayerTurn(attackerInst.Owner))
            return;

        if (!CanSelectAttacker(attackerInst))
            return;


        // 🔑 Cancel previous selection safely
        currentAttacker = attacker;
        isTargettingAttack = true;
        attackCursor.gameObject.SetActive(true);
    }
    public IEnumerator Tawakkul(int damage)
    {
        BeginEffect(); // 🔒 LOCK TURN SYSTEM

        try
        {
            while (true)
            {
                List<IAttackable> targets = GetAllDamageableTargets();

                // Stop when no minions remain
                if (!targets.Any(t => t is CardInstance))
                    break;

                IAttackable target =
                    targets[UnityEngine.Random.Range(0, targets.Count)];

                if (target is CoreInstance core)
                    core.TakeDamage(2);
                else
                    target.TakeDamage(damage);

                yield return new WaitForSeconds(0.5f);
            }
        }
        finally
        {
            EndEffect(); // 🔓 ALWAYS UNLOCK
        }
    }

    private List<IAttackable> GetAllDamageableTargets()
    {
        List<IAttackable> targets = new List<IAttackable>();

        // Ally minions
        foreach (GameObject go in allyDropArea.allyPrefabCards)
        {
            if (go == null) continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
                targets.Add(ci);
        }

        // Enemy minions
        foreach (GameObject go in enemyDropArea.enemyPrefabCards)
        {
            if (go == null) continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci != null && !ci.IsDead)
                targets.Add(ci);
        }

        // Always include both cores
        targets.Add(PlayerCore);
        targets.Add(EnemyCore);

        return targets;
    }
    public bool CanSelectAttacker(CardInstance attacker)
    {
        if (!TurnManager.Instance.IsPlayerTurn(attacker.Owner))
            return false;
        if (isTargetingEffect) return false;
        if ((attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste")) ||
            (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn) ||
            attacker.CurrentAttack <= 0 || attacker.IsAsleep)
            return false;

        // If not summoning sick, all other checks above are sufficient.
        if (!attacker.IsSummoningSick)
            return true;

        // Summoning sick: can only attack if keywords allow it.
        // If neither keyword allows attacking on summon, disallow.
        bool canAttackUnitOnSummon = attacker.CanAttackUnitOnSummon();
        bool canAttackCoreOnSummon = attacker.CanAttackCoreOnSummon();
        if (!canAttackUnitOnSummon && !canAttackCoreOnSummon)
            return false;

        // --- NEW: ensure there is at least one *allowed* target available ---
        // If a unit-only-on-summon attacker (e.g. quickstrike) exists but there are
        // no enemy units to target, it should NOT be selectable (no green glow).
        var validTargets = GetValidTargets(attacker); // uses existing board logic

        if (canAttackUnitOnSummon && !canAttackCoreOnSummon)
        {
            // needs at least one unit target
            return validTargets.Any(t => t is CardInstance);
        }

        if (canAttackCoreOnSummon && !canAttackUnitOnSummon)
        {
            // needs at least one core target (usually always true unless core unavailable)
            return validTargets.Any(t => t is CoreInstance);
        }

        // If both are allowed, any valid target is fine
        return validTargets.Count > 0;
    }

    public ICardDropArea GetBoardForOwner(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? allyDropArea
            : enemyDropArea;
    }
    public ICardDropArea GetBoardForOther(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? enemyDropArea
            : allyDropArea;
    }
    private CoreInstance GetCoreForOwner(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? PlayerCore
            : EnemyCore;
    }
    private CoreInstance GetCoreForEnemy(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? EnemyCore
            : PlayerCore;
    }
    public List<IAttackable> GetValidTargets(CardInstance attacker)
    {
        List<IAttackable> targets = new();

        var defendingBoard = GetBoardForOwner(
            attacker.Owner == PlayerOwner.Player
                ? PlayerOwner.Enemy
                : PlayerOwner.Player
        );

        // Only consider alive units
        List<CardInstance> aliveNonHiddenUnits = new();

        foreach (var go in defendingBoard.GetCards())
        {
            if (go == null || !go.activeSelf)
                continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead || ci.HasKeyword("hidden"))
                continue;

            aliveNonHiddenUnits.Add(ci);
        }

        bool hasProtect = aliveNonHiddenUnits.Exists(ci => ci.HasKeyword("protect") && !ci.HasKeyword("hidden"));

        foreach (var ci in aliveNonHiddenUnits)
        {
            if (hasProtect && !ci.HasKeyword("protect"))
                continue;

            targets.Add(ci);
        }

        if (!hasProtect)
        {
            if (!attacker.IsSummoningSick || attacker.CanAttackCoreOnSummon())
            {
                CoreInstance core = GetCoreForOwner(defendingBoard.Owner);
                targets.Add(core);
            }
        }

        return targets;
    }
    public List<IAttackable> GetValidTargets(PlayerOwner owner)
    {
        List<IAttackable> targets = new();

        var defendingBoard = GetBoardForOwner(owner
        );

        // Only consider alive units
        List<CardInstance> aliveNonHiddenUnits = new();

        foreach (var go in defendingBoard.GetCards())
        {
            if (go == null || !go.activeSelf)
                continue;

            CardInstance ci = go.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead || ci.HasKeyword("hidden"))
                continue;

            aliveNonHiddenUnits.Add(ci);
        }

        bool hasProtect = aliveNonHiddenUnits.Exists(ci => ci.HasKeyword("protect"));

        foreach (var ci in aliveNonHiddenUnits)
        {
            if (hasProtect && !ci.HasKeyword("protect"))
                continue;

            targets.Add(ci);
        }

        if (!hasProtect)
        {
            CoreInstance core = GetCoreForOwner(defendingBoard.Owner);
            targets.Add(core);
        }

        return targets;
    }
    public void CheckGlow()
    {
            foreach (GameObject cardGO in allyDropArea.allyPrefabCards)
            {
                CardInstance ci = cardGO.GetComponent<CardInstance>();
                CardView view = ci.GetComponent<CardView>();

                if (CanSelectAttacker(ci))
                    view.SetGlow(CardView.CardGlowState.CanAttack);
                else
                    view.SetGlow(CardView.CardGlowState.None);
            }
            foreach (IAttackable targets in GetValidTargets(PlayerOwner.Enemy))
            {
                if (targets is CardInstance ci)
                {
                    ci.GetComponent<CardView>()
                        .SetGlow(CardView.CardGlowState.CanBeTargeted);
                }
            }
        
    }
    public void ResolveAttack(
        CardInstance attacker,
        IAttackable target,
        bool consumeAttackForTurn = true,
        bool bypassSelectionRules = false)
    {
        if (attacker == null || target == null)
            return;
        // 🔒 FINAL SUMMON-TURN VALIDATION (authoritative)
        if (attacker.IsSummoningSick)
        {
            if (target is CoreInstance && !attacker.CanAttackCoreOnSummon())
                return;

            if (target is CardInstance && !attacker.CanAttackUnitOnSummon())
                return;
        }

        if (!bypassSelectionRules && !CanSelectAttacker(attacker))
            return;

        if (attacker.Owner == target.Owner)
            return;
        if (consumeAttackForTurn)
        {
            //Handle Haste Scenario
            if (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste"))
                attacker.HasAttackedTwiceThisTurn = true;

            attacker.HasAttackedThisTurn = true;
            attacker.RemoveEffect("hidden");
        }

        if (attacker.Owner == PlayerOwner.Player)
        {
            CheckGlow();
        }

        OnCardAttack?.Invoke(attacker);
        HandlePartnerAttackTriggers(attacker);

        // UNIT vs UNIT
        if (target is CardInstance targetUnit)
        {
            bool isKill = false;
            int attackerDmg = attacker.CurrentAttack;
            int defenderDmg = targetUnit.CurrentAttack;
            bool targetWasBleeding = targetUnit.IsBleeding;
            List<CardInstance> cleaveTargets = attacker.HasKeyword("cleave")
                ? GetAdjacentUnits(targetUnit)
                : null;

            if (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 2) && targetUnit.IsBleeding)
                defenderDmg = Mathf.Max(0, defenderDmg - 2);

            if (OwnerHasTrait(targetUnit.Owner, CardData.Trait.Swordsman, 2) && attacker.IsBleeding)
                attackerDmg = Mathf.Max(0, attackerDmg - 2);

            if (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 3) && targetWasBleeding && attacker.HasTrait("Swordsman"))
            {
                attackerDmg *= 2;
                targetUnit.IsBleeding = false;
                targetUnit.BleedingTurns = 0;
                targetUnit.cardView.UpdateMode();
            }

            if (attackerDmg >= targetUnit.CurrentHealth && !targetUnit.HasKeyword("blessed")) isKill = true;
            int thornDamage = 0;
            attacker.TriggerStrike();
            //Taking thorns damage
            if (targetUnit.HasKeyword("thorns"))
            {
                thornDamage = targetUnit.ThornsDamage;
            }
            //Apply Bleeding to target
            bool shouldApplyBleed =
                attacker.HasKeyword("strikebleed") ||
                (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 1)
                 && attacker.HasTrait("Swordsman")
                 && !swordsmanBleedAppliedThisTurn.Contains(attacker.Owner)
                 && !targetWasBleeding);

            if (shouldApplyBleed)
            {
                targetUnit.IsBleeding = true;
                if (targetUnit.CurrentEffectText == null)
                    targetUnit.CurrentEffectText = "Is Bleeding";
                else if (!targetUnit.CurrentEffectText.Contains("Is Bleeding"))
                    targetUnit.CurrentEffectText += "\nIs Bleeding";
                targetUnit.GetComponent<CardView>().UpdateMode();

                if (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 1) && attacker.HasTrait("Swordsman"))
                    swordsmanBleedAppliedThisTurn.Add(attacker.Owner);
                OnBleedApplied?.Invoke(attacker.Owner);
            }
            //Apply lifesteal Heal before damage if enemy not blessed
            if (attacker.HasKeyword("lifesteal") && !targetUnit.HasKeyword("blessed"))
            {
                attacker.AutoHealCore(attackerDmg);
            }
            if (targetUnit.HasKeyword("lifesteal") && !attacker.HasKeyword("blessed"))
            {
                targetUnit.AutoHealCore(defenderDmg);
            }
            if (isKill)
            {
                OnCardKiller?.Invoke(attacker);
            }
            attacker.TakeDamage(defenderDmg + thornDamage);
            targetUnit.TakeDamage(attackerDmg);
            ApplyCleaveDamage(attacker, cleaveTargets, attackerDmg);

            return;
        }

        // UNIT vs CORE
        if (target is CoreInstance core)
        {
            attacker.TriggerStrike();
            bool targetWasBleeding = core.IsBleeding;
            int attackerDmg = attacker.CurrentAttack;

            if (OwnerHasTrait(target.Owner, CardData.Trait.Swordsman, 2) && attacker.IsBleeding)
                attackerDmg = Mathf.Max(0, attackerDmg - 2);
            if (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 3) && targetWasBleeding && attacker.HasTrait("Swordsman"))
            {
                attackerDmg *= 2;
                core.IsBleeding = false;
                core.BleedingTurns = 0;
            }

            bool shouldApplyBleed =
                attacker.HasKeyword("strikebleed") ||
                (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 1)
                 && attacker.HasTrait("Swordsman")
                 && !swordsmanBleedAppliedThisTurn.Contains(attacker.Owner)
                 && !targetWasBleeding);

            if (shouldApplyBleed)
            {
                core.IsBleeding = true;
                if (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 1) && attacker.HasTrait("Swordsman"))
                    swordsmanBleedAppliedThisTurn.Add(attacker.Owner);
                OnBleedApplied?.Invoke(attacker.Owner);
            }

            if (attacker.HasKeyword("lifesteal"))
            {
                attacker.AutoHealCore(attackerDmg);
            }
            core.TakeDamage(attackerDmg);
            return;
        }
    }
    private List<CardInstance> GetAdjacentUnits(CardInstance centerUnit)
    {
        List<CardInstance> adjacent = new();
        if (centerUnit == null)
            return adjacent;

        ICardDropArea board = GetBoardForOwner(centerUnit.Owner);
        if (board == null)
            return adjacent;

        List<CardInstance> livingUnitsInOrder = new();
        foreach (GameObject cardGO in board.GetCards())
        {
            if (cardGO == null || !cardGO.activeSelf)
                continue;

            CardInstance ci = cardGO.GetComponent<CardInstance>();
            if (ci == null || ci.IsDead || ci.CurrentZone != CardZone.Board)
                continue;

            livingUnitsInOrder.Add(ci);
        }

        int centerIndex = livingUnitsInOrder.IndexOf(centerUnit);
        if (centerIndex < 0)
            return adjacent;

        if (centerIndex - 1 >= 0)
            adjacent.Add(livingUnitsInOrder[centerIndex - 1]);

        if (centerIndex + 1 < livingUnitsInOrder.Count)
            adjacent.Add(livingUnitsInOrder[centerIndex + 1]);

        return adjacent;
    }
    private void ApplyCleaveDamage(CardInstance attacker, List<CardInstance> cleaveTargets, int damage)
    {
        if (attacker == null || cleaveTargets == null || cleaveTargets.Count == 0 || damage <= 0)
            return;

        foreach (CardInstance cleaveTarget in cleaveTargets)
        {
            if (cleaveTarget == null ||
                cleaveTarget.IsDead ||
                cleaveTarget.CurrentZone != CardZone.Board)
                continue;

            bool isKill = false;

            int attackerDmg = damage;
            bool targetWasBleeding = cleaveTarget.IsBleeding;

            //----------------------------------
            // Attacker-side trait modifiers
            //----------------------------------

            if (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 3)
                && targetWasBleeding
                && attacker.HasTrait("Swordsman"))
            {
                attackerDmg *= 2;

                cleaveTarget.IsBleeding = false;
                cleaveTarget.BleedingTurns = 0;
                cleaveTarget.cardView.UpdateMode();
            }

            //----------------------------------
            // Kill check
            //----------------------------------

            if (attackerDmg >= cleaveTarget.CurrentHealth &&
                !cleaveTarget.HasKeyword("blessed"))
            {
                isKill = true;
            }

            //----------------------------------
            // Bleed
            //----------------------------------

            bool shouldApplyBleed =
                attacker.HasKeyword("strikebleed") ||
                (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 1)
                 && attacker.HasTrait("Swordsman")
                 && !swordsmanBleedAppliedThisTurn.Contains(attacker.Owner)
                 && !targetWasBleeding);

            if (shouldApplyBleed)
            {
                cleaveTarget.IsBleeding = true;

                if (cleaveTarget.CurrentEffectText == null)
                    cleaveTarget.CurrentEffectText = "Is Bleeding";
                else if (!cleaveTarget.CurrentEffectText.Contains("Is Bleeding"))
                    cleaveTarget.CurrentEffectText += "\nIs Bleeding";

                cleaveTarget.cardView.UpdateMode();

                if (OwnerHasTrait(attacker.Owner, CardData.Trait.Swordsman, 1)
                    && attacker.HasTrait("Swordsman"))
                {
                    swordsmanBleedAppliedThisTurn.Add(attacker.Owner);
                }

                OnBleedApplied?.Invoke(attacker.Owner);
            }

            //----------------------------------
            // Attacker lifesteal only
            //----------------------------------

            if (attacker.HasKeyword("lifesteal") &&
                !cleaveTarget.HasKeyword("blessed"))
            {
                attacker.AutoHealCore(attackerDmg);
            }

            //----------------------------------
            // On kill
            //----------------------------------

            if (isKill)
            {
                OnCardKiller?.Invoke(attacker);
            }

            //----------------------------------
            // Damage only
            //----------------------------------

            cleaveTarget.TakeDamage(attackerDmg);
        }
    }
    public void HandleBoardCardClick(Card card)
    {
        if (CurrentGameState != GameState.Playing)
            return;

        CardInstance clickedInst = card.GetComponent<CardInstance>();
        if (clickedInst == null)
            return;

        // CASE 1: Not targeting → try to select attacker
        if (!isTargettingAttack && !isTargetingEffect && !clickedInst.IsAsleep)
        {
            BeginAttack(card);
            return;
        }

        // CASE 2: Targeting but attacker was cleared (race condition)
        if (currentAttacker == null)
        {
            isTargettingAttack = false;
            BeginAttack(card);
            return;
        }

        // CASE 3: Valid attack attempt
        CardInstance attackerInst = currentAttacker.GetComponent<CardInstance>();
        if (attackerInst == null)
        {
            isTargettingAttack = false;
            currentAttacker = null;
            return;
        }

        if (GetValidTargets(attackerInst).Contains(clickedInst) && !attackerInst.IsAsleep)
        {
            QueueAttack(attackerInst, clickedInst);
        }
    }
    public bool CanAttackUnit(CardInstance target)
    {
        if (target.Owner == PlayerOwner.Player)
        {
            if (!target.HasKeyword("protect") && allyDropArea.HasProtectUnits())
                return false;
            else return true;
        }
        else
        {
            if (!target.HasKeyword("protect") && enemyDropArea.HasProtectUnits())
                return false;
            else return true;
        }
    }
    public void TryAttackCore(CoreInstance targetCore)
    {
        if (currentAttacker == null)
            return;
        CardInstance cardInst = currentAttacker.GetComponent<CardInstance>();

        if (cardInst.IsSummoningSick && !cardInst.CanAttackCoreOnSummon())
            return;

        if (CurrentGameState != GameState.Playing)
            return;


        if (cardInst.Owner == targetCore.Owner)
            return;

        if (cardInst.Owner == PlayerOwner.Player && enemyDropArea.HasProtectUnits())
        {
            return;
        }
        else if (cardInst.Owner == PlayerOwner.Enemy && allyDropArea.HasProtectUnits())
        {
            return;
        }
        QueueAttack(cardInst, targetCore);
        attackCursor.gameObject.SetActive(false);
        currentAttacker = null; isTargettingAttack = false;
    }
    public void QueueAttack(
        CardInstance attacker,
        IAttackable target,
        bool consumeAttackForTurn = true,
        bool bypassSelectionRules = false,
        bool allowRetarget = true)
    {
        // Safety checks
        if (attacker == null || target == null)
            return; 
        if (attacker.IsSummoningSick)
        {
            if (target is CoreInstance && !attacker.CanAttackCoreOnSummon())
                return;

            if (target is CardInstance && !attacker.CanAttackUnitOnSummon())
                return;
        }

        if ((attacker.HasAttackedThisTurn && !attacker.HasKeyword("haste")) ||
                (attacker.HasAttackedThisTurn && attacker.HasKeyword("haste") && attacker.HasAttackedTwiceThisTurn) || 
                    attacker.CurrentAttack<=0 ||attacker.IsAsleep)
            return;


        if (!bypassSelectionRules && !CanSelectAttacker(attacker))
            return;

        attackQueue.Enqueue(new AttackRequest(attacker, target, consumeAttackForTurn, bypassSelectionRules, allowRetarget));

        // Start processing if idle
        if (!isResolvingAttack)
            StartCoroutine(ProcessAttackQueue());
    }
    public bool IsResolvingAttackQueue()
    {
        return isResolvingAttack || attackQueue.Count > 0;
    }
    private Transform GetCoreProxy(PlayerOwner owner)
    {
        return owner == PlayerOwner.Player
            ? enemyCoreProxy
            : playerCoreProxy;
    }
    private IEnumerator ProcessAttackQueue()
    {
        isResolvingAttack = true;

        while (attackQueue.Count > 0)
        {
            AttackRequest req = attackQueue.Dequeue();

            // Attacker validation
            if (req.Attacker == null ||
                req.Attacker.CurrentZone != CardZone.Board ||
                req.Attacker.IsDead ||
                req.Attacker.IsAsleep ||
                req.Attacker.CurrentAttack <= 0 ||
                (req.Attacker.HasAttackedThisTurn && !req.Attacker.HasKeyword("haste")) || 
                (req.Attacker.HasAttackedThisTurn && req.Attacker.HasKeyword("haste") && req.Attacker.HasAttackedTwiceThisTurn))
            {
                continue;
            }

            // Determine actual target (with retargeting)
            IAttackable target = req.Target;


            bool targetInvalid = false;

            if (target == null)
            {
                targetInvalid = true;
            }
            else if (target is CardInstance ci)
            {
                if (ci.IsDead || !ci.gameObject.activeSelf || ci.CurrentZone != CardZone.Board)
                    targetInvalid = true;
            }

            if (targetInvalid)
            {
                if (!req.AllowRetarget)
                    continue;

                List<IAttackable> newTargets = GetValidTargets(req.Attacker);
                if (newTargets.Count == 0)
                    continue;

                target = newTargets[0];
            }


            // Stop cursor & targeting immediately
            isTargettingAttack = false;
            attackCursor.gameObject.SetActive(false);
            currentAttacker = null;

            // Animate
            CardView attackerView = req.Attacker.GetComponent<CardView>();

            if (target is CoreInstance core)
            {
                // Prefer board-level proxy anchors so each side always gets hit on the
                // correct visual lane, even if a core prefab proxy is misconfigured.
                Transform proxy = GetCoreProxy(core.Owner) ?? core.AttackProxy;

                if (attackerView != null && proxy != null)
                    yield return attackerView.PlayAttackAnimation(proxy);

                yield return core.GetComponent<CoreView>()
                    .PlayHitReaction(req.Attacker.CurrentAttack);
            }
            else
            {
                CardView targetView = target.Transform.GetComponent<CardView>();

                if (attackerView != null)
                    yield return attackerView.PlayAttackAnimation(target.Transform);

                if (targetView != null)
                    yield return targetView.PlayHitReaction(req.Attacker.CurrentAttack);
            }

            // Apply combat logic AFTER visual impact
            ResolveAttack(
                req.Attacker,
                target,
                consumeAttackForTurn: req.ConsumeAttackForTurn,
                bypassSelectionRules: req.BypassSelectionRules);


            // Small pacing delay (FEELS GOOD)
            yield return new WaitForSeconds(0.05f);
        }

        isResolvingAttack = false;
        enemyDropArea.FlushLayoutIfDirty();

    }
    public bool IsAwaitingEffectTarget(CardInstance source)
    {
        return isTargetingEffect && effectSource == source;
    }

    public void EndEffectTargetting()
    {
        // Exit effect targeting mode
        isTargetingEffect = false;

        // Hide cursor
        attackCursor.gameObject.SetActive(false);

        // Clear callbacks & state
        onEffectTargetChosen = null;
        effectSource = null;
        targetType = EffectTarget.None;

        // Refresh glows (attack / playable cards)
        CheckGlow();
    }
    public void BeginEffectTargeting(
    CardInstance source,
    PlayerOwner owner,
    Func<IAttackable, bool> onTargetChosen,
    EffectTarget effectTargetType
)
    {
        if (isTargetingEffect)
            return;

        isTargetingEffect = true;
        effectSource = source;
        effectOwner = owner;
        onEffectTargetChosen = onTargetChosen;
        targetType = effectTargetType;

        attackCursor.gameObject.SetActive(true);
    }
    private bool IsValidEffectTarget(PlayerOwner owner, IAttackable target, EffectTarget effectTargetType)
    {
        if (target is CardInstance targetCard && targetCard.HasKeyword("untargettable"))
            return false;

        // Enforce type rules
        if ((target is CoreInstance) && effectTargetType == EffectTarget.Unit)
            return false;

        if ((target is CardInstance) && effectTargetType == EffectTarget.Core)
            return false;

        // 🔑 Allow allies by default (gear, buffs, heals)
        return true;
    }

    public IAttackable ChooseEnemyEffectTarget(EffectTarget type, bool targetPlayer, bool canTargetCore)
    {
        List<IAttackable> targets = GetValidTargets(PlayerOwner.Player);
        if (!targetPlayer)
        {
            targets = GetValidTargets(PlayerOwner.Enemy);
            if(canTargetCore)
            targets.Add(EnemyCore);
        }
        else if(canTargetCore)
        targets.Add(PlayerCore);

        if (targets.Count == 0)
            return null;

        // Filter by effect target type
        targets = targets.Where(t =>
        {
            if (t is CardInstance untargettableCandidate && untargettableCandidate.HasKeyword("untargettable"))
                return false;

            if (effectSource != null &&
                effectSource.CurrentEffect.Contains("sleep") &&
                t is CardInstance sleepingCandidate &&
                sleepingCandidate.IsAsleep)
            {
                return false;
            }

            if (type == EffectTarget.Unit)
                return t is CardInstance;

            if (type == EffectTarget.Core)
                return t is CoreInstance;

            return true; // Any
        }).ToList();
        
        if (targets.Count == 0)
            return null;

        while (targets.Count > 0)
        {
            IAttackable choice = targets[UnityEngine.Random.Range(0, targets.Count)];
            if (IsTargetUsableForRandomEffects(choice))
            {
                Debug.Log($"Enemy triggered effect on " + choice.ToString() + " ");
                return choice;
            }

            targets.Remove(choice);
        }

        return null;
    }

    public IAttackable ChooseRandomEffectTarget(PlayerOwner targetOwner, EffectTarget type, bool canTargetCore = true, bool excludeSleepingUnits = false)
    {
        List<IAttackable> targets = GetValidTargets(targetOwner);

        targets = targets.Where(t =>
        {
            if (t is CardInstance untargettableCandidate && untargettableCandidate.HasKeyword("untargettable"))
                return false;

            if (!canTargetCore && t is CoreInstance)
                return false;

            if (excludeSleepingUnits && t is CardInstance ci && ci.IsAsleep)
                return false;

            if (type == EffectTarget.Unit)
                return t is CardInstance;

            if (type == EffectTarget.Core)
                return t is CoreInstance;

            return true;
        }).ToList();

        while (targets.Count > 0)
        {
            IAttackable choice = targets[UnityEngine.Random.Range(0, targets.Count)];
            if (IsTargetUsableForRandomEffects(choice))
                return choice;

            targets.Remove(choice);
        }

        return null;
    }

    private bool IsTargetUsableForRandomEffects(IAttackable target)
    {
        if (target == null)
            return false;

        if (target is CardInstance ci)
            return !ci.IsDead && ci.gameObject != null && ci.gameObject.activeInHierarchy;

        return true;
    }
    public void HandleTargetClick(IAttackable target)
    {
        if (!isTargetingEffect || effectSource == null)
            return;

        if (!IsValidEffectTarget(effectSource.Owner, target, targetType))
            return;

        isTargetingEffect = false;
        attackCursor.gameObject.SetActive(false);

        onEffectTargetChosen?.Invoke(target);

        onEffectTargetChosen = null;
        effectSource = null;
    }

    public IEnumerator ShowEnemySpell(CardData data)
    {
        enemySpellReveal.SetActive(true);

        // Create preview card
        CardInstance preview =
            CardFactory.Instance.CreateCardInPosition(
                data,
                PlayerOwner.Enemy,Vector3.zero,Vector3.one,
                enemySpellAnchor.transform
            );
        preview.GetComponent<SortingGroup>().sortingOrder = 500;
        preview.SetZone(CardZone.Board);
        preview.GetComponent<CardView>().UpdateMode();
        preview.IsDisplay = true; preview.GetComponent<Collider2D>().enabled = false;

        yield return new WaitForSeconds(1.0f);

        Destroy(preview.gameObject);
        enemySpellReveal.SetActive(false);
    }

    #endregion
}
public class AttackRequest
{
    public CardInstance Attacker;
    public IAttackable Target;
    public Transform TargetTransform;
    public bool ConsumeAttackForTurn;
    public bool BypassSelectionRules;
    public bool AllowRetarget;

    public AttackRequest(
        CardInstance attacker,
        IAttackable target,
        bool consumeAttackForTurn,
        bool bypassSelectionRules,
        bool allowRetarget)
    {
        Attacker = attacker;
        Target = target;
        TargetTransform = target != null ? target.Transform : null;
        ConsumeAttackForTurn = consumeAttackForTurn;
        BypassSelectionRules = bypassSelectionRules;
        AllowRetarget = allowRetarget;
    }
}
