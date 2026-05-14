using UnityEngine;

public class PathOfPowerManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Relic SnakeRelic;
    public Sprite relicSprite;
    public string relicDescription;
    public string relicName;
    void Start()
    {
        Relic newR = new Relic(relicName, relicDescription, CardData.Trait.None, relicSprite);
        SnakeRelic = newR;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleRelic()
    {
        var panel = ScanController.Instance.panelInstance;
        if (panel == null)
            return;

        // If the panel is visible and currently showing this relic, hide it.
        if (panel.isVisible && panel.isShowingRelic)
        {
            panel.Slide(false);
            return;
        }

        // Otherwise populate with this relic and show the panel. This won't affect card hover behavior
        // since ScanController will only auto-hide when not showing a relic.
        panel.PopulateRelic(SnakeRelic);
        panel.Slide(true);
    }
}
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
}   
