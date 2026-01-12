using System.Collections.Generic;
using UnityEngine;
public interface IDeckTraitEffect
{
    CardData.Trait Trait { get; }
    int Tier { get; }

    void OnRegister();
    void OnUnregister();
}
public interface ITraitProgression
{
    CardData.Trait Trait { get; }
    PlayerOwner Owner { get; }
    int CurrentTier { get; }
    void PushInitialState();
    void ResetProgression();
    int CurrentProgress { get; }

    event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    void Register();
    void Unregister();
}

public class TraitSystem : MonoBehaviour
{
    public event System.Action<CardData.Trait, int> OnTraitTierActivated;

    public PlayerOwner Owner { get; private set; }

    private readonly List<IDeckTraitEffect> activeEffects = new();

    public void Initialize(PlayerOwner owner)
    {
        Owner = owner;
    }
    public void ActivateEffect(IDeckTraitEffect effect)
    {
        effect.OnRegister();
        activeEffects.Add(effect);

        OnTraitTierActivated?.Invoke(effect.Trait, effect.Tier);
    }
    public bool HasTraitAtTier(CardData.Trait trait, int minTier)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.Trait == trait && effect.Tier >= minTier)
                return true;
        }
        return false;
    }

    public void ClearAll()
    {
        foreach (var effect in activeEffects)
            effect.OnUnregister();

        activeEffects.Clear();
    }
}
#region Neutral Trait
public class NeutralProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Neutral;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int neutralPlayed=0;

    public int CurrentProgress => neutralPlayed;

    public event System.Action<CardData.Trait, int,int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;

    public NeutralProgression(PlayerOwner owner,int maxTier,TraitSystem traitSystem,AllyCardDropArea allyBoard,EnemyCardDropArea enemyBoard)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.allyBoard = allyBoard;
        this.enemyBoard = enemyBoard;
    }
    public void ResetProgression()
    {
        neutralPlayed = 0;
    }
    public void PushInitialState()
    {
        int cap = GetCurrentCap();
        OnProgressUpdated?.Invoke(Trait, neutralPlayed, cap, Owner);
    }

    public void Register()
    {
        Debug.Log($"[NeutralProgression] Register for {Owner}");

        allyBoard.OnCardPlayed += OnCardPlayed;
        enemyBoard.OnCardPlayed += OnCardPlayed;

        OnProgressUpdated?.Invoke(Trait, neutralPlayed, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        Debug.Log($"[NeutralProgression] Unregister for {Owner}");

        allyBoard.OnCardPlayed -= OnCardPlayed;
        enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 5,
            1 => 10,
            2 => 15,
            _ => 999
        };
    }

    private void OnCardPlayed(CardInstance card)
    {
            if (card.Owner != Owner)
            return;

        if (!card.HasTrait("neutral"))
            return;
        neutralPlayed++;
        OnProgressUpdated?.Invoke(Trait, neutralPlayed, GetCurrentCap(), Owner);

        //Debug.Log($"Neutral card played for :{Owner}, count is now {neutralPlayed}");

        if (neutralPlayed >= 5 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }

        if (neutralPlayed >= 10 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }

    }

    private void UnlockTier1()
    {
        CurrentTier = 1;

        traitSystem.ActivateEffect(
            new NeutralTier1Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Neutral Tier 1");
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        traitSystem.ActivateEffect(
            new NeutralTier2Effect(Owner, deckManager)
        );

        Debug.Log($"{Owner} unlocked Neutral Tier 2");
    }

}
public class NeutralTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Neutral;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public NeutralTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        used = false;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (used)
            return;

        if (card.Owner != owner)
            return;

        if (card.Data.cardType!="minion")
            return;
        card.ModifyStats(0,2);
        used = true;
    }
}
public class NeutralTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Neutral;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool usedThisTurn;

    private readonly DeckManager deckManager;

    public NeutralTier2Effect(PlayerOwner owner, DeckManager deckManager)
    {
        this.owner = owner;
        this.deckManager = deckManager;
    }

    public void OnRegister()
    {
        deckManager.OnCardDrawn += OnCardDrawn;
        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
        TurnManager.Instance.OnTurnEnded += OnTurnEnded;
    }

    public void OnUnregister()
    {
        deckManager.OnCardDrawn -= OnCardDrawn;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
            TurnManager.Instance.OnTurnEnded -= OnTurnEnded;
        }
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner == owner)
            usedThisTurn = false;
    }

    private void OnTurnEnded(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;

        // Clear temporary cost reductions at end of turn
        foreach (var card in GetHand(owner))
        {
            card.ClearTemporaryManaModifiers();
        }
    }

    private void OnCardDrawn(CardInstance card)
    {
        if (usedThisTurn)
            return;

        if (card.Owner != owner)
            return;

        if (card.CurrentManaCost < 2)
            return;

        card.AddTemporaryManaModifier(-1);
        usedThisTurn = true;

        Debug.Log($"[Neutral T2] Reduced cost of {card.name} for {owner}");
    }

    private IEnumerable<CardInstance> GetHand(PlayerOwner owner)
    {
        HandManager hand =
            owner == PlayerOwner.Player
                ? Object.FindFirstObjectByType<DeckManager>().handManager
                : Object.FindFirstObjectByType<DeckManager>().handManagerEnemy;

        foreach (var go in hand.handCards)
            yield return go.GetComponent<CardInstance>();
    }
}
#endregion
#region Speedster Trait
public class SpeedsterProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Speedster;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int speedsterAttacks = 0;
    public int CurrentProgress => speedsterAttacks;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly GameManager gameManager;

    public SpeedsterProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem, GameManager gameManager)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.gameManager = gameManager;
    }
    public void ResetProgression()
    {
        speedsterAttacks = 0;
    }
    public void PushInitialState()
    {
        int cap = GetCurrentCap();
        OnProgressUpdated?.Invoke(Trait, speedsterAttacks, cap, Owner);
    }
    public void Register()
    {
        Debug.Log($"[SpeedsterProgression] Register for {Owner}");

        gameManager.OnCardAttack += OnSpeedsterAttack;

        OnProgressUpdated?.Invoke(Trait, speedsterAttacks, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        Debug.Log($"[SpeedsterProgression] Unregister for {Owner}");

        gameManager.OnCardAttack -= OnSpeedsterAttack;
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 4,
            2 => 6,
            _ => 999
        };
    }

    private void OnSpeedsterAttack(CardInstance card)
    {
        if (card.Owner != Owner)
            return;

        if (!card.HasTrait("Speedster"))
            return;
        speedsterAttacks++;
        OnProgressUpdated?.Invoke(Trait, speedsterAttacks, GetCurrentCap(), Owner);

        if (speedsterAttacks >= 3 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }
        if (speedsterAttacks >= 5 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }
        if (speedsterAttacks >= 8 && CurrentTier < 3 && maxTier >= 3)
        {
            UnlockTier3();
        }
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;

        traitSystem.ActivateEffect(
            new SpeedsterTier1Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked speedster Tier 1");
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;

        traitSystem.ActivateEffect(
            new SpeedsterTier2Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked speedster Tier 2");
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;

        traitSystem.ActivateEffect(
            new SpeedsterTier3Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked speedster Tier 3");
    }
}
public class SpeedsterTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Speedster;
    public int Tier => 1;

    private readonly PlayerOwner owner;

    public SpeedsterTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion" || !card.HasTrait("Speedster"))
            return;
        Debug.Log($"Buffing first card {card.name}, for {owner}");
        card.ModifyStats(1, 0);
    }
}
public class SpeedsterTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Speedster;
    public int Tier =>2 ;

    private readonly PlayerOwner owner;

    public SpeedsterTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion" || !card.HasTrait("Speedster"))
            return;

        if (!card.HasKeyword("quickstrike")){
            card.CurrentEffect += " quickstrike";
            card.CurrentEffectText += "\nQuickstrike";
        }
    }
}
public class SpeedsterTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Speedster;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public SpeedsterTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        TurnManager.Instance.OnTurnStarted += OnTurnStarted;
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStarted -= OnTurnStarted;
    }

    private void OnTurnStarted(PlayerOwner turnOwner)
    {
        if (turnOwner != owner)
            return;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion")
            return;

        if (!card.HasKeyword("quickstrike"))
        {
            card.CurrentEffect += " quickstrike";
            card.CurrentEffectText += "\nQuickstrike";
        }
        card.ModifyStats(1, 0);
    }
}
#endregion
#region Pokemon Trait
public class PokemonProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Pokemon;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int pokemonKills = 0;

    public int CurrentProgress => pokemonKills;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;
    private readonly GameManager gameManager;

    public PokemonProgression(PlayerOwner owner, int maxTier, TraitSystem traitSystem, AllyCardDropArea allyBoard, EnemyCardDropArea enemyBoard, GameManager gameManager)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.allyBoard = allyBoard;
        this.enemyBoard = enemyBoard;
        this.gameManager = gameManager;
    }
    public void ResetProgression()
    {
        pokemonKills = 0;
    }
    public void PushInitialState()
    {
        int cap = GetCurrentCap();
        OnProgressUpdated?.Invoke(Trait, pokemonKills, cap, Owner);
    }
    public void Register()
    {
        Debug.Log($"[PokemonProgression] Register for {Owner}");

        gameManager.OnCardKiller += OnCardKill;

        OnProgressUpdated?.Invoke(Trait, pokemonKills, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        gameManager.OnCardKiller -= OnCardKill;
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 10,
            _ => 999
        };
    }
    private void OnCardKill(CardInstance card)
    {
        if (card.Owner != Owner)
            return;

        if (!card.HasTrait("Pokemon"))
            return;
        pokemonKills++;
        OnProgressUpdated?.Invoke(Trait, pokemonKills, GetCurrentCap(), Owner);

        Debug.Log($"Pokemon card kill for :{Owner}, count is now {pokemonKills}");

        if (pokemonKills >= 3 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
        }

        if (pokemonKills >= 6 && CurrentTier < 2 && maxTier >= 2)
        {
            UnlockTier2();
        }
        if (pokemonKills >= 10 && CurrentTier < 3 && maxTier >= 3)
        {
            UnlockTier3();
        }

    }
    private void UnlockTier1()
    {
        CurrentTier = 1;

        traitSystem.ActivateEffect(
            new PokemonTier1Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Pokemon Tier 1");
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(
            new PokemonTier2Effect(Owner)
        );

        Debug.Log($"{Owner} unlocked Pokemon Tier 2");
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(
            new PokemonTier3Effect(Owner, gameManager)
        );
        Debug.Log($"{Owner} unlocked Pokemon Tier 3");
    }
}
public class PokemonTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Pokemon;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public PokemonTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private void OnCardPlayed(CardInstance card)
    {
        if (used)
            return;

        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion")
            return;

        Debug.Log($"Evolving instant card {card.name}, for {owner}");
        int detectedID = GetEvolutionId(card.CurrentEffect);
        card.MorphTo(detectedID);
        used = true;
    }
    private int GetEvolutionId(string effect)
    {
        int value = -1;

        if (!effect.Contains("morphto"))
        {
            return -1;
        }

        int startEffect = effect.IndexOf("morphto");

        string morphEffect = effect.Substring(startEffect);

        int startID = morphEffect.IndexOf('(');
        int endID = morphEffect.IndexOf(')');

        if (startID < 0 || endID < 0 || endID <= startID + 1)
        {
            return -1;
        }

        string valueStr = morphEffect.Substring(startID + 1, endID - startID - 1);

        if (int.TryParse(valueStr, out value))
        {
            return value;
        }
        else return -1;
    }
}
public class PokemonTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Pokemon;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool used;

    public PokemonTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }
    public void OnRegister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;
    }
    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private void OnCardPlayed(CardInstance card)
    {
        if (used)
            return;

        if (card.Owner != owner)
            return;

        if (card.Data.cardType != "minion" || !card.HasTrait("Pokemon"))
            return;

        Debug.Log($"Evolving instant card {card.name}, for {owner}");
        card.MorphTo(GetEvolutionId(card.CurrentEffect));
        card.ModifyStats(2,2);
        used = true;
    }
    private int GetEvolutionId(string effect)
    {
        int value = -1;

        if (!effect.Contains("morphto"))
        {
            return -1;
        }

        int startEffect = effect.IndexOf("morphto");

        string morphEffect = effect.Substring(startEffect);

        int startID = morphEffect.IndexOf('(');
        int endID = morphEffect.IndexOf(')');

        if (startID < 0 || endID < 0 || endID <= startID + 1)
        {
            return -1;
        }

        string valueStr = morphEffect.Substring(startID + 1, endID - startID - 1);

        if (int.TryParse(valueStr, out value))
        {
            return value;
        }
        else return -1;
    
}
}
public class PokemonTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Pokemon;
    public int Tier => 3;

    private readonly PlayerOwner owner;
    private bool used;
    GameManager gm;


    public PokemonTier3Effect(PlayerOwner owner, GameManager gm)
    {
        this.owner = owner;
        this.gm = gm;
    }
    public void OnRegister()
    {
        DiscoverLegendary();
    }
    void DiscoverLegendary()
    {

        if (used) return;
        gm.Discover(29,28,27, owner);//FIX IDs

        used = true;
    }
    public void OnUnregister()
    {
        throw new System.NotImplementedException();
    }
}
#endregion
#region MonsterHunter
public class MonsterHunterProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.MonsterHunter;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int colossusDeaths;

    public int CurrentProgress => colossusDeaths;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly GameManager gameManager;

    public MonsterHunterProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem,
        AllyCardDropArea allyBoard,
        EnemyCardDropArea enemyBoard,
        GameManager gameManager)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.gameManager = gameManager;
    }

    public void Register()
    {
        Debug.Log($"[MonsterHunterProgression] Register for {Owner}");
        gameManager.OnCardKilled += OnCardKilled;
        PushInitialState();
    }

    public void Unregister()
    {
        gameManager.OnCardKilled -= OnCardKilled;
    }

    public void ResetProgression()
    {
        colossusDeaths = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, colossusDeaths, GetCurrentCap(), Owner);
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 9,
            3 => 999,
            _ => 9999,
        };
    }

    private void OnCardKilled(CardInstance card)
    {
        // 1. Must be ally
        if (card.Owner != Owner)
            return;

        // 2. Must be colossus
        if (card.BaseManaCost <= 1)
            return;

        // 3. Must have MonsterHunter trait
        if (!card.HasTrait("MonsterHunter"))
            return;

        colossusDeaths++;
        OnProgressUpdated?.Invoke(Trait, colossusDeaths, GetCurrentCap(), Owner);

        Debug.Log($"[MH] Colossus death for {Owner}: {colossusDeaths}");

        if (colossusDeaths >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (colossusDeaths >= 6 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2(); 
        
        if (colossusDeaths >= 9 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new MonsterHunterTier1Effect(Owner));
    }

    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new MonsterHunterTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new MonsterHunterTier3Effect(Owner));
    }
}
public class MonsterHunterTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.MonsterHunter;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public MonsterHunterTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        SummonTierMonster(1);
        used = true;
    }

    public void OnUnregister() { }

    private void SummonTierMonster(int tier)
    {
        List<CardData> options =
            CardDatabase.Instance.GetCardsByEffect($"tier{tier}monster*");

        if (options == null || options.Count == 0)
            return;

        CardData chosen = options[Random.Range(0, options.Count)];

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        gm.TrySummonForOwner(owner, chosen.id);
    }
}
public class MonsterHunterTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.MonsterHunter;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool used;

    public MonsterHunterTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        SummonTierMonster(2);
        used = true;
    }

    public void OnUnregister() { }

    private void SummonTierMonster(int tier)
    {
        List<CardData> options =
            CardDatabase.Instance.GetCardsByEffect($"tier{tier}monster*");

        if (options == null || options.Count == 0)
            return;

        CardData chosen = options[Random.Range(0, options.Count)];

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        gm.TrySummonForOwner(owner, chosen.id);
    }
}
public class MonsterHunterTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.MonsterHunter;
    public int Tier => 3;

    private readonly PlayerOwner owner;
    private bool used;

    public MonsterHunterTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        SummonTierMonster(3);
        used = true;
    }

    public void OnUnregister() { }

    private void SummonTierMonster(int tier)
    {
        Debug.Log("Summon Tier3");
        List<CardData> options =
            CardDatabase.Instance.GetCardsByEffect($"tier{tier}monster*");

        if (options == null || options.Count == 0)
            return;

        CardData chosen = options[Random.Range(0, options.Count)];

        Debug.Log($"Options count = {options.Count} and chosen card is {chosen.name}");
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        gm.TrySummonForOwner(owner, chosen.id);
    }
}
#endregion
#region Healer
public class HealerProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Healer;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int healAmount;

    public int CurrentProgress => healAmount;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly GameManager gameManager;

    public HealerProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem,
        GameManager gameManager)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.gameManager = gameManager;
    }

    public void Register()
    {
        Debug.Log($"[Healer] Register for {Owner}");
        gameManager.OnOwnerHeal += OnAllyHeal;
        PushInitialState();
    }

    public void Unregister()
    {
        gameManager.OnOwnerHeal -= OnAllyHeal;
    }

    public void ResetProgression()
    {
        healAmount = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, healAmount, GetCurrentCap(), Owner);
    }

    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 10,
            1 => 20,
            2 => 30,
            3 => 9999,
            _ => 9999,
        };
    }

    private void OnAllyHeal(PlayerOwner owner, int amount)
    {
        // 1. Must be ally
        if (owner != Owner)
            return;

        healAmount+=amount;
        OnProgressUpdated?.Invoke(Trait, healAmount, GetCurrentCap(), Owner);

        Debug.Log($"[Healer] Heal Amount for {Owner}: {healAmount}");

        if (healAmount >= 10 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (healAmount >= 20 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2(); 
        
        if (healAmount >= 30 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }

    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new HealerTier1Effect(Owner));
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new HealerTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new HealerTier3Effect(Owner));
    }
}
public class HealerTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Healer;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public HealerTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if(owner==PlayerOwner.Player)
        {
            gm.PlayerHealBonus += 2;
        }
        else
        {
            gm.EnemyHealBonus += 2;
        }
        used = true;
    }

    public void OnUnregister() { }

}
public class HealerTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Healer;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool used;

    public HealerTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (owner == PlayerOwner.Player)
        {
            gm.PlayerHealBonus += 3;
        }
        else
        {
            gm.EnemyHealBonus += 3;
        }
        used = true;
    }

    public void OnUnregister() { }

}
public class HealerTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Healer;
    public int Tier => 3;

    private readonly PlayerOwner owner;
    private bool used;

    public HealerTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (owner == PlayerOwner.Player)
        {
            gm.PlayerDarkHeal=true;
        }
        else
        {
            gm.EnemyDarkHeal = true; ;
        }
        used = true;
    }

    public void OnUnregister() { }

}
#endregion
#region Faith
public class FaithProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Faith;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int discoverCount;

    public int CurrentProgress => discoverCount;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly GameManager gameManager;

    public FaithProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem,
        GameManager gameManager)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.gameManager = gameManager;
    }

    public void Register()
    {
        Debug.Log($"[Faith] Register for {Owner}");
        gameManager.OnDiscover += OnAllyDiscover;
        PushInitialState();
    }

    public void Unregister()
    {
        gameManager.OnDiscover -= OnAllyDiscover;
    }

    public void ResetProgression()
    {
        discoverCount = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, discoverCount, GetCurrentCap(), Owner);
    }
    public void OnAllyDiscover(PlayerOwner owner)
    {
        // 1. Must be ally
        if (owner != Owner)
            return;

        discoverCount ++;
        OnProgressUpdated?.Invoke(Trait, discoverCount, GetCurrentCap(), Owner);
        if (discoverCount >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (discoverCount >= 6 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2(); 
        
        if (discoverCount >= 10 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }
    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new FaithTier1Effect(Owner));
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new FaithTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new FaithTier3Effect(Owner));
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 10,
            3 => 9999,
            _ => 9999,
        };
    }
}

public class FaithTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Faith;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;

    public FaithTier1Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        gm.AddCardToHand(owner, 64);
        gm.AddCardToHand(owner, 60);
        used = true;
    }

    public void OnUnregister() { }

}
public class FaithTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Faith;
    public int Tier => 2;

    private readonly PlayerOwner owner;

    public FaithTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        //No visible effect since it discounts discoveries
    }

    public void OnUnregister() { }

}
public class FaithTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Faith;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public FaithTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        //No visible effect since it discounts discoveries
    }

    public void OnUnregister() { }

}

#endregion
#region Avatar
public class AvatarProgression : ITraitProgression
{
    public CardData.Trait Trait => CardData.Trait.Avatar;
    public PlayerOwner Owner { get; }
    public int CurrentTier { get; private set; }

    private readonly int maxTier;
    private int praiseCount;

    public int CurrentProgress => praiseCount;

    public event System.Action<CardData.Trait, int, int, PlayerOwner> OnProgressUpdated;

    private readonly TraitSystem traitSystem;
    private readonly GameManager gameManager;

    public AvatarProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem,
        GameManager gameManager)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.gameManager = gameManager;
    }

    public void Register()
    {
        Debug.Log($"[Avatar] Register for {Owner}");
        gameManager.OnPraise += OnAllyPraise;
        PushInitialState();
    }

    public void Unregister()
    {
        gameManager.OnPraise -= OnAllyPraise;
    }

    public void ResetProgression()
    {
        praiseCount = 0;
    }

    public void PushInitialState()
    {
        OnProgressUpdated?.Invoke(Trait, praiseCount, GetCurrentCap(), Owner);
    }
    public void OnAllyPraise(PlayerOwner owner)
    {
        // 1. Must be ally
        if (owner != Owner)
            return;

        praiseCount++;
        OnProgressUpdated?.Invoke(Trait, praiseCount, GetCurrentCap(), Owner);
        if (praiseCount >= 3 && CurrentTier < 1 && maxTier >= 1)
            UnlockTier1();

        if (praiseCount >= 6 && CurrentTier < 2 && maxTier >= 2)
            UnlockTier2();

        if (praiseCount >= 10 && CurrentTier < 3 && maxTier >= 3)
            UnlockTier3();
    }
    private void UnlockTier1()
    {
        CurrentTier = 1;
        traitSystem.ActivateEffect(new AvatarTier1Effect(Owner, praiseCount));
    }
    private void UnlockTier2()
    {
        CurrentTier = 2;
        traitSystem.ActivateEffect(new AvatarTier2Effect(Owner));
    }
    private void UnlockTier3()
    {
        CurrentTier = 3;
        traitSystem.ActivateEffect(new AvatarTier3Effect(Owner));
    }
    private int GetCurrentCap()
    {
        return CurrentTier switch
        {
            0 => 3,
            1 => 6,
            2 => 10,
            3 => 9999,
            _ => 9999,
        };
    }
}

public class AvatarTier1Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Avatar;
    public int Tier => 1;

    private readonly PlayerOwner owner;
    private bool used;
    int praiseCount;
    public AvatarTier1Effect(PlayerOwner owner, int praiseCount)
    {
        this.owner = owner;
        this.praiseCount = praiseCount;
    }

    public void OnRegister()
    {
        if (used) return;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        gm.ShuffleInDeck(74, owner);
        gm.ShuffleInDeck(78, owner);

        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        used = true;
    }

    public void OnUnregister() {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.HasKeyword("avatar2"))
            card.ModifyStats(2*praiseCount,2*praiseCount);
        else if (card.HasKeyword("avatar"))
            card.ModifyStats(praiseCount, praiseCount);
    }

}
public class AvatarTier2Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Avatar;
    public int Tier => 2;

    private readonly PlayerOwner owner;
    private bool used;
    public AvatarTier2Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        if (used) return;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        gm.ShuffleInDeck(74, owner);
        gm.ShuffleInDeck(78, owner);

        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed += OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed += OnCardPlayed;

        used = true;
    }

    public void OnUnregister()
    {
        var allyBoard = Object.FindFirstObjectByType<AllyCardDropArea>();
        var enemyBoard = Object.FindFirstObjectByType<EnemyCardDropArea>();

        if (allyBoard != null)
            allyBoard.OnCardPlayed -= OnCardPlayed;

        if (enemyBoard != null)
            enemyBoard.OnCardPlayed -= OnCardPlayed;
    }
    private void OnCardPlayed(CardInstance card)
    {
        if (card.Owner != owner)
            return;

        if (card.Data.name == "Aang") {
            card.CurrentEffect = "avatar d[draw(1)] d[autoheal(5)] d[autodmg(3)] d[summon(76)] d[summon(76)]";
            card.CurrentEffectText = "Avatar.\nDraw 1, Heal 5 HP to your core, Deal 3 damage to enemy core.\nSummon two 1/1 golemites.";
            card.ParseEffects(); card.TriggerDeploy();
        }
        if (card.Data.name == "Korra")
        {
            card.CurrentEffect = "avatar quickstrike lifesteal protect blessed";
            card.CurrentEffectText = "Avatar.\nQuickstrike Blessed \nProtect Lifesteal.";
        }
    }

}
public class AvatarTier3Effect : IDeckTraitEffect
{
    public CardData.Trait Trait => CardData.Trait.Avatar;
    public int Tier => 3;

    private readonly PlayerOwner owner;

    public AvatarTier3Effect(PlayerOwner owner)
    {
        this.owner = owner;
    }

    public void OnRegister()
    {
        var deckManager = Object.FindFirstObjectByType<DeckManager>();

        deckManager.ReplaceCardsEverywhere(
            owner,
            new Dictionary<int, int>
            {
            { 74, 75 },
            { 78, 79 }
            }
        );
    }

    public void OnUnregister() { }

}

#endregion
