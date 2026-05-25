using UnityEngine;

public class CombatLog : MonoBehaviour
{
    [SerializeField] private GameObject combatLogEntryPrefab;
    [SerializeField] private Transform combatLogGrid;
    [SerializeField] private GameObject LogUI;

    public static CombatLog Instance;

    private CombatLogEntryView lastEntry;

    private bool IsConfigured
    {
        get => combatLogEntryPrefab != null && combatLogGrid != null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (LogUI != null)
            LogUI.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ToggleLoGUI()
    {
        if (LogUI != null)
            LogUI.SetActive(!LogUI.activeSelf);
    }

    public void AddAction(CardInstance cardInstance, string text)
    {
        if (cardInstance == null)
            return;

        AddAction(cardInstance, cardInstance.Data, cardInstance.Owner, text);
    }

    public void AddAction(CardInstance cardInstance, CardData cardData, PlayerOwner owner, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !IsConfigured)
            return;

        bool appendToLast = lastEntry != null && lastEntry.CardInstance == cardInstance && cardInstance != null;
        if (appendToLast)
        {
            lastEntry.AppendText(text);
            return;
        }

        if (!IsConfigured)
            return;

        GameObject created = Instantiate(combatLogEntryPrefab, combatLogGrid);
        CombatLogEntryView view = created.GetComponent<CombatLogEntryView>();
        if (view == null)
            view = created.AddComponent<CombatLogEntryView>();

        view.Initialize(cardInstance, cardData, owner, text);
        lastEntry = view;
    }
}
