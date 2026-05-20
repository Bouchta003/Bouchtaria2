using UnityEngine;

[CreateAssetMenu(fileName = "RelicDefinition", menuName = "Bouchtaria/Path Of Power/Relic Definition")]
public class RelicDefinition : ScriptableObject
{
    public enum RelicType
    {
        CombatRelic = 0,
        DeckRelic = 1
    }
    [Header("Identity")]
    [SerializeField] private string relicId;
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string description;
    [TextArea]
    [SerializeField] private string enemyRelicText;
    [SerializeField] private Sprite icon;

    [Header("Design Hooks")]
    [SerializeField] private CardData.Trait assignedTrait = CardData.Trait.None;
    [SerializeField] private bool canAppearAsStarter = true;
    [SerializeField] private bool canAppearAsWardenReward = true;
    [SerializeField] private bool discoverable = true;
    [SerializeField] private RelicType relicType = RelicType.CombatRelic;

    public string RelicId => string.IsNullOrWhiteSpace(relicId) ? name : relicId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public string EnemyRelicText => enemyRelicText;
    public Sprite Icon => icon;
    public CardData.Trait AssignedTrait => assignedTrait;
    public bool CanAppearAsStarter => canAppearAsStarter;
    public bool CanAppearAsWardenReward => canAppearAsWardenReward;
    public bool Discoverable => discoverable;
    public RelicType Type => relicType;

    // TODO(Path Of Power): add typed effect hooks here once relic effects are implemented.
}
