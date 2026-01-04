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
        Debug.Log($"Buffing first card {card.name}, for {owner}");
        card.ModifyStats(0,1);
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

        gameManager.OnCardKilled += OnCardKill;

        OnProgressUpdated?.Invoke(Trait, pokemonKills, GetCurrentCap(), Owner);
    }
    public void Unregister()
    {
        gameManager.OnCardKilled -= OnCardKill;
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
    public int Tier => 1;

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

        if (card.Data.cardType != "minion")
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
