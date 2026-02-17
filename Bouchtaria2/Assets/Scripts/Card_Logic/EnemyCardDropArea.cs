using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class EnemyCardDropArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] GameObject GameManager;
    [SerializeField] HandManager handManager;
    [SerializeField] SplineContainer enemyBoardSpline;
    [SerializeField] Transform container;
    public Transform CardContainer { get; set; }
    private bool layoutDirty;

    public PlayerOwner Owner => PlayerOwner.Enemy;

    private GameManager gm;
    public int maxBoardSize = 6;

    public List<GameObject> enemyPrefabCards = new List<GameObject>();
    public event System.Action<CardInstance> OnCardPlayed;

    private void Start()
    {
        gm = GameManager.GetComponent<GameManager>();
        CardContainer = container;
    }

    public void OnCardDrop(Card card)
    {
        CardInstance cardInst = card.GetComponent<CardInstance>();

        if (gm.ShouldBlockRandomCardPlay(cardInst))
        {
            card.ResetCard();
            return;
        }

        // Mana / legality
        if (cardInst.CurrentManaCost > gm.EnemyCurrentMana || cardInst.Owner==PlayerOwner.Player)
        {
            card.ResetCard();
            return;
        }

        if (enemyPrefabCards.Count >= maxBoardSize)
            return;

        int boardCount = enemyPrefabCards.Count;
        int freeSlotsBeforePlay = maxBoardSize - boardCount;
        int deployOwnerSummons = cardInst.GetDeployOwnerSummonCount();
        if (deployOwnerSummons > 0)
        {
            int summonSlotsAfterPlayedCard = Mathf.Max(0, freeSlotsBeforePlay - 1);
            int allowedSummons = Mathf.Min(deployOwnerSummons, summonSlotsAfterPlayedCard);
            gm.SetDeploySummonCap(cardInst, allowedSummons);
        }

        // Remove from hand
        handManager.RemoveCardFromHand(card.gameObject);

        // Use mana
        gm.UseMana(cardInst.CurrentManaCost, PlayerOwner.Enemy);

        // Board setup
        cardInst.SetZone(CardZone.Board);
        card.transform.SetParent(CardContainer, false);
        cardInst.Owner = PlayerOwner.Enemy;

        cardInst.IsSummoningSick = true;

        cardInst.OnDeployResolved += HandleCardDeployResolved;
        cardInst.OnEnterBoard();
        if (cardInst.HasText("random"))
        {
            gm.EnemyRandomCount++;
        }
        if (!cardInst.DeployPending)
            HandleCardDeployResolved(cardInst);

        // Switch to compact board view
        card.GetComponent<CardView>().UpdateMode();

        enemyPrefabCards.Add(card.gameObject);
        UpdateEnemyCardPositions();

        if (gm != null)
            gm.NotifyUnitEnteredBoard(cardInst);
    }
    private void HandleCardDeployResolved(CardInstance cardInst)
    {
        if (cardInst == null)
            return;

        cardInst.OnDeployResolved -= HandleCardDeployResolved;
        OnCardPlayed?.Invoke(cardInst);
    }
    public void CardPlayed(CardInstance cardInst)
    {
        OnCardPlayed?.Invoke(cardInst);
    }
    public List<GameObject> GetCards()
    {
        return enemyPrefabCards;
    }

    public bool HasProtectUnits()
    {
        foreach (GameObject cardGO in enemyPrefabCards)
        {
            CardInstance instance = cardGO.GetComponent<CardInstance>(); 
            if (instance != null && !instance.IsDead && instance.HasKeyword("protect") && !instance.HasKeyword("hidden")) if (instance != null && instance.HasKeyword("protect"))
                return true;
        }
        return false;
    }

    private void OnEnable()
    {
        TurnManager.Instance.OnTurnStarted += HandleTurnStart;
        TurnManager.Instance.OnTurnEnded += HandleTurnEnd;
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= HandleTurnStart;
            TurnManager.Instance.OnTurnEnded -= HandleTurnEnd;
        }
    }
    private void HandleTurnEnd(PlayerOwner owner)
    {
        if (owner != PlayerOwner.Enemy)
            return;

        int index = 0;
        if (GameRunContext.IsDungeonRun)
        {
            if (GameRunContext.DungeonData.floor > 15)
                GameManager.GetComponent<GameManager>().BuffAllAllies(1, 1, PlayerOwner.Enemy);
            if (GameRunContext.DungeonData.floor > 30)
                GameManager.GetComponent<GameManager>().BuffAllAllies(1, 1, PlayerOwner.Enemy);
        }

        // IMPORTANT:
        // allyPrefabCards may grow during iteration (summons),
        // so we loop by index and re-evaluate Count dynamically.
        while (index < enemyPrefabCards.Count)
        {
            GameObject cardGO = enemyPrefabCards[index];

            if (cardGO == null)
            {
                index++;
                continue;
            }

            CardInstance instance = cardGO.GetComponent<CardInstance>();
            if (instance == null || instance.IsDead)
            {
                index++;
                continue;
            }

            instance.OnTurnEnd();
            index++;
        }
    }
    private void HandleTurnStart(PlayerOwner owner)
    {
        if (owner != PlayerOwner.Enemy)
            return;

        var enemyCards = new List<GameObject>(enemyPrefabCards);

        foreach (var cardGO in enemyCards)
        {
            var instance = cardGO.GetComponent<CardInstance>();
            instance.OnTurnStart();
        }
    }
    public void HandleEnemyDeath(CardInstance instance)
    {
        RemoveEnemyCardFromBoard(instance);

        // 🔑 stop animations immediately
        instance.gameObject.transform.DOKill(true);

        Destroy(instance.gameObject);
    }

    public void RemoveEnemyCardFromBoard(CardInstance instance)
    {
        GameObject cardGO = instance.gameObject;

        if (!enemyPrefabCards.Contains(cardGO))
            return;

        enemyPrefabCards.Remove(cardGO);

        if (gm != null && gm.IsResolvingAttackQueue())
            layoutDirty = true;
        else
            UpdateEnemyCardPositions();
    }

    public void MarkLayoutDirty()
    {
        layoutDirty = true;
    }

    public void AddSummonedCard(CardInstance cardInst)
    {
        if (IsFull())
            return;

        cardInst.SetZone(CardZone.Board);
        cardInst.GetComponent<CardView>().UpdateMode();
        cardInst.Owner = Owner;
        cardInst.IsSummoningSick = true;

        enemyPrefabCards.Add(cardInst.gameObject);

        if (gm != null && gm.IsResolvingAttackQueue())
        {
            layoutDirty = true;
            if (gm != null)
                gm.NotifyUnitEnteredBoard(cardInst);
            return;
        }

        UpdateEnemyCardPositions();
        if (gm != null)
            gm.NotifyUnitEnteredBoard(cardInst);
    }
    public void FlushLayoutIfDirty()
    {
        if (!layoutDirty)
            return;

        layoutDirty = false;
        UpdateEnemyCardPositions();
    }

    public bool IsFull()
    {
        if (enemyPrefabCards.Count >= maxBoardSize) return true;
        else return false;
    }
    public bool IsEmpty()
    {
        if (enemyPrefabCards.Count <= 0) return true;
        else return false;
    }
    public CardInstance BoardHasID(int id)
    {
        foreach (GameObject cardGO in enemyPrefabCards)
        {
            if (cardGO.GetComponent<CardInstance>().Data.id == id) return cardGO.GetComponent<CardInstance>();
        }
        return null;
    }
    public CardInstance BoardHasEffect(string effect)
    {
        foreach (GameObject cardGO in enemyPrefabCards)
        {
            if (cardGO.GetComponent<CardInstance>().CurrentEffect.Contains(effect)) return cardGO.GetComponent<CardInstance>();
        }
        return null;
    }
    public void UpdateEnemyCardPositions()
    {
        // ❗ Do not reflow during attack animations
        if (gm != null && gm.IsResolvingAttackQueue())
            return;

        if (enemyPrefabCards.Count == 0)
            return;

        float cardSpacing = (1f / maxBoardSize) + 0.1f / enemyPrefabCards.Count;
        float firstCardPosition = 0.5f - (enemyPrefabCards.Count - 1) * cardSpacing / 2;

        Spline spline = enemyBoardSpline.Spline;

        for (int i = 0; i < enemyPrefabCards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;

            Vector3 worldPos = enemyBoardSpline.transform.TransformPoint(
                spline.EvaluatePosition(p)
            );

            Vector3 forward = spline.EvaluateTangent(p);
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

            GameObject cardGO = enemyPrefabCards[i];
            CardView view = cardGO.GetComponent<CardView>();

            // Kill previous layout tweens
            cardGO.transform.DOKill();

            cardGO.transform.DOMove(worldPos, 0.25f)
                .OnComplete(() =>
                {
                    if (view != null)
                        view.BoardPosition = view.transform.position;
                });

            cardGO.transform.DORotate(
                new Vector3(0, 0, angle),
                0.25f
            );
        }
    }
}
