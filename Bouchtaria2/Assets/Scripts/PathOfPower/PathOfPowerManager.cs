using UnityEngine;
using UnityEngine.Rendering;

public class PathOfPowerManager : MonoBehaviour
{
    public static PathOfPowerManager Instance;
    [SerializeField] public GameObject DiscoverDisplay;



    // Change for relic list so that all of them are written here.
    public Relic SnakeRelic;
    public Sprite relicSprite;
    public string relicDescription;
    public string relicName;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

    }
    void Start()
    {
        Relic newR = new Relic(relicName, relicDescription, CardData.Trait.None, relicSprite);
        SnakeRelic = newR;

        DiscoverDisplay.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D)) { DiscoverCardForDeck(0, 1, 2); }
    }
    /// <summary>
    /// Method assigned to button to toggle Relic info using scan panel view just like for cards. If the panel is already showing this relic, it will hide it. Otherwise it will populate the panel with this relic and show it. 
    /// This allows players to manually check the relic info without interfering with card hover behavior, since ScanController will only auto-hide when not showing a relic.
    /// </summary>
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
    public void DiscoverCardForDeck(int id1, int id2, int id3)
    {
        // Ensure CardFactory exists before using it
        if (CardFactory.Instance == null)
        {
            GameObject factoryObj = new GameObject("CardFactory");
            factoryObj.AddComponent<CardFactory>();
        }

        DiscoverDisplay.SetActive(true);

        CardData data1 = CardDatabase.Instance.GetCardById(id1);
        CardInstance dataInst1 = CardFactory.Instance.CreateCardInPosition(
            data1,
            PlayerOwner.Player,
            Vector3.zero,
            new Vector3(0.6f, 0.6f, 0.6f),
            DiscoverDisplay.transform
        );

        dataInst1.IsDisplay = true;
        dataInst1.GetComponent<SortingGroup>().sortingOrder = 201;

        CardData data2 = CardDatabase.Instance.GetCardById(id2);
        CardInstance dataInst2 = CardFactory.Instance.CreateCardInPosition(
            data2,
            PlayerOwner.Player,
            new Vector3(5, 0, 0),
            new Vector3(0.6f, 0.6f, 0.6f),
            DiscoverDisplay.transform
        );

        dataInst2.IsDisplay = true;
        dataInst2.GetComponent<SortingGroup>().sortingOrder = 201;

        CardData data3 = CardDatabase.Instance.GetCardById(id3);
        CardInstance dataInst3 = CardFactory.Instance.CreateCardInPosition(
            data3,
            PlayerOwner.Player,
            new Vector3(-5, 0, 0),
            new Vector3(0.6f, 0.6f, 0.6f),
            DiscoverDisplay.transform
        );

        dataInst3.IsDisplay = true;
        dataInst3.GetComponent<SortingGroup>().sortingOrder = 201;
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
