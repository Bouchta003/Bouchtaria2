using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum TurnPhase
{
    Start,
    Main,
    End
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public PlayerOwner CurrentPlayer { get; private set; }
    public TurnPhase CurrentPhase { get; private set; }
    
    [SerializeField] DeckManager deckManager;
    [SerializeField] GameManager gameManager;
    [SerializeField] public Image endButton;
    [SerializeField] public ChaosEffectDisplay chaosEffectDisplay;
    [SerializeField] List<Sprite> ChaosSprites;

    public event Action<PlayerOwner> OnTurnStarted;
    public event Action<PlayerOwner> OnTurnEnded;
    public bool PlayerHasExtraTurn;
    public bool EnemyHasExtraTurn;
    public bool PlayerSkipsNextDraw;
    public bool EnemySkipsNextDraw;
    public int PlayerChaosEventCount = 0;
    public int EnemyChaosEventCount = 0;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public void StartFirstTurn()
    {
        CurrentPlayer = PlayerOwner.Player;
        BeginTurn();
        StartCoroutine(deckManager.Draw(3, PlayerOwner.Player));
        StartCoroutine(deckManager.Draw(4, PlayerOwner.Enemy));
    }

    public void EndTurn()
    {
        if (CurrentPhase != TurnPhase.Main)
            return;
        // inside TurnManager.EndTurn()
        if (gameManager.IsResolvingEffects)
        {
            // Defer the end-turn request until all effects have finished.
            // GameManager will run this action when ActiveEffectCount reaches 0.
            Debug.Log("[TURN] EndTurn deferred because effects are resolving.");
            gameManager.EnqueueDeferredAction(() =>
            {
                // re-run EndTurn on the main Unity thread when effects are done
                // (defensive: check phase again so we don't double-run incorrectly)
                if (CurrentPhase == TurnPhase.Main)
                    EndTurn();
            });
            return;
        }

        if (CurrentPlayer == PlayerOwner.Player)
        {
            AllyCardDropArea allydrop = FindFirstObjectByType<AllyCardDropArea>();
            foreach (GameObject cardGO in allydrop.allyPrefabCards)
            {
                CardInstance ci = cardGO.GetComponent<CardInstance>();
                CardView view = ci.GetComponent<CardView>();

                view.SetGlow(CardView.CardGlowState.None);
            }
            EnemyCardDropArea enemyDrop = FindFirstObjectByType<EnemyCardDropArea>();
            foreach (GameObject cardGO in enemyDrop.enemyPrefabCards)
            {
                CardInstance ci = cardGO.GetComponent<CardInstance>();
                CardView view = ci.GetComponent<CardView>();

                view.SetGlow(CardView.CardGlowState.None);
            }
            //Ally EOT Core
        gameManager.PlayerCore.Bleed();
        }
        else
        {
            //Enemy EOT Core
            gameManager.EnemyCore.Bleed();
        }
        CurrentPhase = TurnPhase.End;
        OnTurnEnded?.Invoke(CurrentPlayer);
        // switch player
        if ((CurrentPlayer == PlayerOwner.Player && !PlayerHasExtraTurn) || (CurrentPlayer == PlayerOwner.Enemy && !EnemyHasExtraTurn))
        {
            CurrentPlayer = CurrentPlayer == PlayerOwner.Player
              ? PlayerOwner.Enemy
              : PlayerOwner.Player;
        }
        else
        {
            //Has ended turn and has extra turn
            PlayerHasExtraTurn = false;
            EnemyHasExtraTurn = false;
            endButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "End";
        }
        BeginTurn();
    }

    private void BeginTurn()
    {
        StartCoroutine(BeginTurnRoutine());

    }
    private IEnumerator BeginTurnRoutine()
    {
        CurrentPhase = TurnPhase.Start;

        if (gameManager.OwnerHasTrait(CurrentPlayer, CardData.Trait.Chaos, 2))
        {
            bool chaosFinished = false;
            int randomChaosIndex = UnityEngine.Random.Range(0, ChaosSprites.Count);
            // Trigger chaos effect FIRST
            chaosEffectDisplay.ShowChaosEffect(
                rune: ChaosSprites[randomChaosIndex],
                description: GetChaosDescription(randomChaosIndex, CurrentPlayer),
                onComplete: () => chaosFinished = true
            );

            // Wait until the animation finishes
            while (!chaosFinished)
                yield return null;
            yield return (0.5f);
            TriggerChaosEffect(randomChaosIndex, CurrentPlayer);
            yield return (0.5f);
            if (CurrentPlayer == PlayerOwner.Player) PlayerChaosEventCount++;
            else EnemyChaosEventCount++;
        }
        // NOW the turn officially starts
        OnTurnStarted?.Invoke(CurrentPlayer);

        // Immediately enter main phase
        CurrentPhase = TurnPhase.Main;

        // Update button color
        if (CurrentPlayer == PlayerOwner.Player)
            endButton.color = new Color(0, 0.75f, 1);
        else
            endButton.color = new Color(1, 0.5f, 0);

        UpdateGlow();
    }
    public IEnumerator TriggerSingleChaosEvent()
    {
        bool chaosFinished = false;
        int randomChaosIndex = UnityEngine.Random.Range(0, ChaosSprites.Count);
        // Trigger chaos effect FIRST
        chaosEffectDisplay.ShowChaosEffect(
            rune: ChaosSprites[randomChaosIndex],
            description: GetChaosDescription(randomChaosIndex, CurrentPlayer),
            onComplete: () => chaosFinished = true
        );

        // Wait until the animation finishes
        while (!chaosFinished)
            yield return null;
        yield return (0.5f);
        TriggerChaosEffect(randomChaosIndex, CurrentPlayer);
        yield return (0.5f);
        if (CurrentPlayer == PlayerOwner.Player) PlayerChaosEventCount++;
        else EnemyChaosEventCount++;
    }

    string GetChaosDescription(int index, PlayerOwner owner)
    {
        switch (index)
        {
            case 0:
                //Trigger effect here
                return "Chaos Event : Draw 1";
            case 1:
                return "Chaos Event : Summon a random 2 mana card";
            case 2:
                return "Chaos Event : Deal 5 damage split amongst all enemies";
            case 3:
                return "Chaos Event : Add two coins to your hand";
            case 4:
                return "Chaos Event : Heal your core for 5 HP";
            case 5:
                return "Chaos Event : Gain 1 max mana";
            default:
                return "Unkown Chaos event /!'\'";
        }
    }
    void TriggerChaosEffect(int index, PlayerOwner owner)
    {
        switch (index)
        {
            case 0:
                //Trigger effect here
                deckManager.Draw(1, owner);break;
            case 1:
                gameManager.TrySummonForOwnerManaCost(owner, 2); break;
            case 2:
                StartCoroutine(gameManager.DamageRandomEnemy(true, 5, owner)); break;
            case 3:
                gameManager.AddCardToHand(owner, 62); gameManager.AddCardToHand(owner, 62); break;
            case 4:
                gameManager.PlayerCore.Heal(5); break;
            case 5:
                gameManager.GainMaxMana(1, owner); break;
        }
    }
    public void UpdateGlow()
    {
        //Update card can attack or attackable visuals
        if (CurrentPlayer == PlayerOwner.Player)
        {
            AllyCardDropArea allydrop = FindFirstObjectByType<AllyCardDropArea>();
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            foreach (GameObject cardGO in allydrop.allyPrefabCards)
            {
                CardInstance ci = cardGO.GetComponent<CardInstance>();
                CardView view = ci.GetComponent<CardView>();

                if (gameManager.CanSelectAttacker(ci))
                    view.SetGlow(CardView.CardGlowState.CanAttack);
                else
                    view.SetGlow(CardView.CardGlowState.None);
            }
            foreach (IAttackable targets in gameManager.GetValidTargets(PlayerOwner.Enemy))
            {
                if (targets is CardInstance ci)
                {
                    ci.GetComponent<CardView>()
                        .SetGlow(CardView.CardGlowState.CanBeTargeted);
                }
            }
        }
    }
    // -------------------------
    // Permission helpers
    // -------------------------

    public bool IsPlayerTurn(PlayerOwner owner)
    {
        return CurrentPlayer == owner && CurrentPhase == TurnPhase.Main;
    }
}
