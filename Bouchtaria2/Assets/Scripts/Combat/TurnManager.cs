using UnityEngine;
using System;
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

        CurrentPhase = TurnPhase.End;
        OnTurnEnded?.Invoke(CurrentPlayer);
        // switch player
        CurrentPlayer = CurrentPlayer == PlayerOwner.Player
            ? PlayerOwner.Enemy
            : PlayerOwner.Player;

        Debug.Log($"Moving to {CurrentPlayer}'s turn");
        BeginTurn();
    }

    private void BeginTurn()
    {
        CurrentPhase = TurnPhase.Start;

        OnTurnStarted?.Invoke(CurrentPlayer);

        // Immediately enter main phase
        CurrentPhase = TurnPhase.Main;
    }

    // -------------------------
    // Permission helpers
    // -------------------------

    public bool IsPlayerTurn(PlayerOwner owner)
    {
        return CurrentPlayer == owner && CurrentPhase == TurnPhase.Main;
    }
}
