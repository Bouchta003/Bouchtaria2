using UnityEngine;

[CreateAssetMenu(fileName = "RelicDefinition", menuName = "Bouchtaria/Path Of Power/Relic Definition")]
public class RelicDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string relicId;
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Design Hooks")]
    [SerializeField] private CardData.Trait assignedTrait = CardData.Trait.None;
    [SerializeField] private bool canAppearAsStarter = true;
    [SerializeField] private bool canAppearAsWardenReward = true;

    public string RelicId => string.IsNullOrWhiteSpace(relicId) ? name : relicId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public CardData.Trait AssignedTrait => assignedTrait;
    public bool CanAppearAsStarter => canAppearAsStarter;
    public bool CanAppearAsWardenReward => canAppearAsWardenReward;

    // TODO(Path Of Power): add typed effect hooks here once relic effects are implemented.
}
