using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyEncounterDefinition", menuName = "Bouchtaria/Path Of Power/Enemy Encounter Definition")]
public class EnemyEncounterDefinition : ScriptableObject
{
    [SerializeField] private string encounterId;
    [SerializeField] private string displayName;
    [SerializeField] private bool elite;
    [SerializeField] private bool warden;
    [Tooltip("If > 0 and this encounter is a warden, this encounter will always be used as that floor's warden.")]
    [SerializeField] private int specificFloorEncounter;
    [Tooltip("Relic ids held by this encounter. These are resolved by PathOfPowerManager before combat and applied by PathOfPowerRelicEffectService during combat.")]
    [SerializeField] private List<int> relicIds = new List<int>();
    [Tooltip("Card ids that can be discovered after defeating this encounter.")]
    [SerializeField] private List<int> droppablePool = new List<int>();
    [SerializeField] private Sprite displaySprite;
    [Tooltip("Deck list encoded as a string, for example: [0,2,3,4,6].")]
    [SerializeField] private string enemyDeckTemplate = "[]";

    public string EncounterId => string.IsNullOrWhiteSpace(encounterId) ? name : encounterId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public bool Elite => elite;
    public bool Warden => warden;
    public int SpecificFloorEncounter => Mathf.Max(0, specificFloorEncounter);
    public Sprite DisplaySprite => displaySprite;
    public IReadOnlyList<int> RelicIds => relicIds;
    public IReadOnlyList<int> DroppablePool => droppablePool;
    public string EnemyDeckTemplate => enemyDeckTemplate;

    public IReadOnlyList<int> GetParsedEnemyDeckTemplate()
    {
        List<int> parsedDeck = new List<int>();
        if (string.IsNullOrWhiteSpace(enemyDeckTemplate))
            return parsedDeck;

        string normalized = enemyDeckTemplate.Trim().TrimStart('[').TrimEnd(']');
        if (string.IsNullOrWhiteSpace(normalized))
            return parsedDeck;

        string[] parts = normalized.Split(',');
        foreach (string part in parts)
        {
            if (int.TryParse(part.Trim(), out int cardId))
                parsedDeck.Add(cardId);
        }

        return parsedDeck;
    }
}
