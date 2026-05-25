using System.Collections.Generic;
using UnityEngine;

public class CombatLog : MonoBehaviour
{
    [SerializeField] private GameObject combatLogEntryPrefab;
    [SerializeField] private Transform combatLogGrid;
    [SerializeField] private GameObject LogUI;

    public static CombatLog Instance;

    private CombatLogEntryView lastEntry;
    private readonly Queue<PendingLogEntry> pendingEntries = new();

    private bool IsConfigured
    {
        get => combatLogEntryPrefab != null && combatLogGrid != null;
    }

    private bool CanRenderImmediately
    {
        get => IsConfigured && combatLogGrid.gameObject.activeInHierarchy;
    }

    private struct PendingLogEntry
    {
        public CardInstance CardInstance;
        public CardData CardData;
        public PlayerOwner Owner;
        public string Text;
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
        if (LogUI == null)
            return;

        LogUI.SetActive(!LogUI.activeSelf);

        if (LogUI.activeSelf)
            FlushPendingEntries();
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

        if (!CanRenderImmediately)
        {
            pendingEntries.Enqueue(new PendingLogEntry
            {
                CardInstance = cardInstance,
                CardData = cardData,
                Owner = owner,
                Text = text,
            });
            return;
        }

        AddEntry(cardInstance, cardData, owner, text);
    }

    private void FlushPendingEntries()
    {
        if (!CanRenderImmediately)
            return;

        while (pendingEntries.Count > 0)
        {
            PendingLogEntry entry = pendingEntries.Dequeue();
            AddEntry(entry.CardInstance, entry.CardData, entry.Owner, entry.Text);
        }
    }

    private void AddEntry(CardInstance cardInstance, CardData cardData, PlayerOwner owner, string text)
    {
        bool appendToLast = lastEntry != null && lastEntry.CardInstance == cardInstance && cardInstance != null;
        if (appendToLast)
        {
            lastEntry.AppendText(text);
            return;
        }

        GameObject created = Instantiate(combatLogEntryPrefab, combatLogGrid);
        CombatLogEntryView view = created.GetComponent<CombatLogEntryView>();
        if (view == null)
            view = created.AddComponent<CombatLogEntryView>();

        view.Initialize(cardInstance, cardData, owner, text);
        lastEntry = view;
    }
}
