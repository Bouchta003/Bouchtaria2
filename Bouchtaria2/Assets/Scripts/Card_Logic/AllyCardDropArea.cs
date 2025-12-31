using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using DG.Tweening;
using System.Linq;

public class AllyCardDropArea : MonoBehaviour, ICardDropArea
{
    [SerializeField] GameObject GameManager;
    [SerializeField] HandManager handManager;
    [SerializeField] EnemyCardDropArea enemyCardDropArea;
    [SerializeField] SplineContainer allyBoardSpline;
    public PlayerOwner Owner => PlayerOwner.Player;
    GameManager gm;
    public int maxBoardSize = 6;
    public event System.Action<CardInstance> OnCardPlayed;

    public List<GameObject> allyPrefabCards = new List<GameObject>();
    private void Start()
    {
        gm = GameManager.GetComponent<GameManager>();
    }
    public void OnCardDrop(Card card)
    {
        //Verify Mana Legality 
        if (card.gameObject.GetComponent<CardInstance>().CurrentManaCost > gm.AllyCurrentMana ||             
            !TurnManager.Instance.IsPlayerTurn(PlayerOwner.Player))
        {
            card.ResetCard();
            return;
        }
        CardInstance cardInst = card.gameObject.GetComponent<CardInstance>();
        //SpellCast
        if (cardInst.Data.cardType == "spell")
        {
            cardInst.OnEnterBoard();
            Destroy(cardInst.gameObject);
            handManager.RemoveCardFromHand(card.gameObject);
            gm.UseMana(card.gameObject.GetComponent<CardInstance>().CurrentManaCost, PlayerOwner.Player);
            return;
        }

        //Verify board space Legality
        if (allyPrefabCards.Count >= maxBoardSize) return;

        // ----- Card is legal -----

        //Remove card from hand
        handManager.RemoveCardFromHand(card.gameObject);

        //Use Mana
        gm.UseMana(card.gameObject.GetComponent<CardInstance>().CurrentManaCost, PlayerOwner.Player);

        //Instantiate card compact instead on board
        cardInst.SetZone(CardZone.Board);
        cardInst.Owner = PlayerOwner.Player;
        if (cardInst.HasKeyword("quickstrike") || cardInst.HasKeyword("charge"))
            cardInst.IsSummoningSick = false;
        else
            cardInst.IsSummoningSick = true;
        cardInst.OnEnterBoard(); 
        
        //Call for  update
        OnCardPlayed?.Invoke(cardInst);
        
        //UpdateView to board mode
        card.gameObject.GetComponent<CardView>().UpdateMode();

        //Add to list of ally cards
        allyPrefabCards.Add(card.gameObject);
        UpdateAllyCardPositions();
    }
    public void AddSummonedCard(CardInstance cardInst)
    {
        // Safety
        if (IsFull())
            return;

        // Force board state
        cardInst.SetZone(CardZone.Board);
        cardInst.Owner = Owner;
        cardInst.IsSummoningSick = true;

        // Switch to compact view
        CardView view = cardInst.GetComponent<CardView>();
        if (view != null)
            view.UpdateMode();

        // Add to board list
        if (Owner == PlayerOwner.Player)
            allyPrefabCards.Add(cardInst.gameObject);

        // Position immediately
        if (Owner == PlayerOwner.Player)
            UpdateAllyCardPositions();
    }

    public bool HasProtectUnits()
    {
        foreach (GameObject cardGO in allyPrefabCards)
        {
            CardInstance instance = cardGO.GetComponent<CardInstance>();
            if (instance != null && instance.HasKeyword("protect"))
                return true;
        }
        return false;
    }
    public bool IsFull()
    {
        if (allyPrefabCards.Count >= maxBoardSize) return true;
        else return false;
    }
    public List<GameObject> GetCards()
    {
        return allyPrefabCards;
    }
    public void HandleAllyDeath(CardInstance instance)
    {
        GameObject cardGO = instance.gameObject;

        if (!allyPrefabCards.Contains(cardGO))
            return;

        allyPrefabCards.Remove(cardGO);

        //cardGO.SetActive(false);
        Destroy(cardGO);
        UpdateAllyCardPositions();
    }
    public void UpdateAllyCardPositions()
    {
        if (gm != null && gm.IsCombatAnimating)
            return;

        if (allyPrefabCards.Count == 0) return;

        float cardSpacing = (1f / maxBoardSize) + 0.1f / allyPrefabCards.Count;
        float firstCardPosition = 0.5f - (allyPrefabCards.Count - 1) * cardSpacing / 2;

        Spline spline = allyBoardSpline.Spline;

        for (int i = 0; i < allyPrefabCards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;

            // Convert spline local position → world position
            Vector3 worldPos = allyBoardSpline.transform.TransformPoint(
                spline.EvaluatePosition(p)
            );

            // Get tangent (direction along spline)
            Vector3 forward = spline.EvaluateTangent(p);

            // 2D rotation angle
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

            CardView view = allyPrefabCards[i].GetComponent<CardView>();

            allyPrefabCards[i].transform.DOMove(worldPos, 0.25f)
                .OnComplete(() =>
                {
                    if (view != null)
                        view.BoardPosition = view.transform.position;
                });

            allyPrefabCards[i].transform.DORotate(
                new Vector3(0, 0, angle),
                0.25f
            );
        }
        foreach (GameObject cardGO in allyPrefabCards)
        {
            CardInstance ci = cardGO.GetComponent<CardInstance>();
            CardView view = ci.GetComponent<CardView>();

            if (GameManager.GetComponent<GameManager>().CanSelectAttacker(ci))
                view.SetGlow(CardView.CardGlowState.CanAttack);
            else
                view.SetGlow(CardView.CardGlowState.None);
        }
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
    #region Effect Triggers
    private void HandleTurnEnd(PlayerOwner owner)
    {
        // Only trigger for the owner of THIS board
        if (owner != PlayerOwner.Player)
            return;

        foreach (var cardGO in allyPrefabCards)
        {
            var instance = cardGO.GetComponent<CardInstance>();
            instance.OnTurnEnd();
        }
    }
    private void HandleTurnStart(PlayerOwner owner)
    {
        if (owner != PlayerOwner.Player)
            return;

        foreach (var cardGO in allyPrefabCards)
        {
            var instance = cardGO.GetComponent<CardInstance>();
            instance.OnTurnStart();
        }
    }
    private void TriggerGunners(int traitLevel)
    {
        int damage = 1;
        int gunnerEffectCounter = 0;
        if (traitLevel > 2) damage = 2;
        // Collect enemy units once
        List<CardInstance> enemies = enemyCardDropArea.enemyPrefabCards
            .Select(go => go.GetComponent<CardInstance>())
            .Where(ci => ci != null)
            .ToList();

        if (enemies.Count == 0)
            return;

        foreach (GameObject cardGO in allyPrefabCards)
        {
            if (gunnerEffectCounter > 0 && traitLevel==1) return;
            if (gunnerEffectCounter > 1 && traitLevel>1) return;
            CardInstance instance = cardGO.GetComponent<CardInstance>();

            if (instance == null)
                continue;

            if (!instance.HasTrait("Gunner"))
                continue;

            CardInstance target = enemies[Random.Range(0, enemies.Count)];

            target.TakeDamage(damage);
            gunnerEffectCounter++;
        }
    }
    #endregion
}
