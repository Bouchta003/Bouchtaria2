using UnityEngine;
using System;
using UnityEngine.UI;

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
    [SerializeField] Image endButton;
    public event Action<PlayerOwner> OnTurnStarted;
    public event Action<PlayerOwner> OnTurnEnded;

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
        deckManager.Draw(3, PlayerOwner.Player);
        deckManager.Draw(3, PlayerOwner.Enemy);
    }

    public void EndTurn()
    {
        if (CurrentPhase != TurnPhase.Main)
            return;

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
        }
        CurrentPhase = TurnPhase.End;
        OnTurnEnded?.Invoke(CurrentPlayer);
        // switch player
        CurrentPlayer = CurrentPlayer == PlayerOwner.Player
            ? PlayerOwner.Enemy
            : PlayerOwner.Player;
        BeginTurn();
    }

    private void BeginTurn()
    {
        CurrentPhase = TurnPhase.Start;

        OnTurnStarted?.Invoke(CurrentPlayer);

        // Immediately enter main phase
        CurrentPhase = TurnPhase.Main;

        //Update Button color
        if (CurrentPlayer == PlayerOwner.Player)
            endButton.color = new Color(0, 0.75f, 1);
        else
            endButton.color = new Color(1, 0.5f, 0);

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
