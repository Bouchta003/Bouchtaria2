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

    void Register();
    void Unregister();
}

public class TraitSystem : MonoBehaviour
{
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
    private int neutralPlayed;

    private readonly TraitSystem traitSystem;
    private readonly AllyCardDropArea allyBoard;
    private readonly EnemyCardDropArea enemyBoard;

    public NeutralProgression(
        PlayerOwner owner,
        int maxTier,
        TraitSystem traitSystem,
        AllyCardDropArea allyBoard,
        EnemyCardDropArea enemyBoard)
    {
        Owner = owner;
        this.maxTier = maxTier;
        this.traitSystem = traitSystem;
        this.allyBoard = allyBoard;
        this.enemyBoard = enemyBoard;
    }
    public void Register()
    {
        Debug.Log($"[NeutralProgression] Register for {Owner}");

        allyBoard.OnCardPlayed += OnCardPlayed;
        enemyBoard.OnCardPlayed += OnCardPlayed;
    }
    public void Unregister()
    {
        Debug.Log($"[NeutralProgression] Unregister for {Owner}");

        allyBoard.OnCardPlayed -= OnCardPlayed;
        enemyBoard.OnCardPlayed -= OnCardPlayed;
    }

    private void OnCardPlayed(CardInstance card)
    {
        Debug.Log($"[NeutralProgression] Card played: {card.name}, cardOwner={card.Owner}, progressionOwner={Owner}");

            if (card.Owner != Owner)
            return;

        if (!card.HasTrait("neutral"))
            return;
        neutralPlayed++;
        Debug.Log($"Neutral card played for :{Owner}, count is now {neutralPlayed}");

        if (neutralPlayed >= 5 && CurrentTier < 1 && maxTier >= 1)
        {
            UnlockTier1();
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
        var board = Object.FindFirstObjectByType<AllyCardDropArea>();
        board.OnCardPlayed += OnCardPlayed;
    }

    public void OnUnregister()
    {
        var board = Object.FindFirstObjectByType<AllyCardDropArea>();
        if (board != null)
            board.OnCardPlayed -= OnCardPlayed;
    }

    private void OnCardPlayed(CardInstance card)
    {
        if (used)
            return;

        if (card.Owner != owner)
            return;

        if (!card.HasTrait("neutral"))
            return;

        if (card.Data.cardType!="minion")
            return;

        //card.ModifyHealth(1);
        Debug.Log("Self buffing +1hp for " + card.name);
        used = true;
    }
}

#endregion
