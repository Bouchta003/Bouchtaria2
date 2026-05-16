using UnityEngine;

[System.Serializable]
public class Relic
{
    public string Name { get; set; }
    public string Description { get; set; }
    public CardData.Trait assignedTrait { get; set; }
    public Sprite sprite { get; set; }

    public Relic(string name, string description, CardData.Trait trait, Sprite sprite)
    {
        Name = name;
        Description = description;
        assignedTrait = trait;
        this.sprite = sprite;
    }

    public Relic(RelicDefinition definition)
    {
        Name = definition != null ? definition.DisplayName : string.Empty;
        Description = definition != null ? definition.Description : string.Empty;
        assignedTrait = definition != null ? definition.AssignedTrait : CardData.Trait.None;
        sprite = definition != null ? definition.Icon : null;
    }
}
