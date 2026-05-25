using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CombatLog : MonoBehaviour
{
    private const int MaxVisibleLogs = 5;
    [SerializeField] private GameObject combatLogEntryPrefab;
    [SerializeField] private Transform combatLogGrid;
    [FormerlySerializedAs("LogUI")]
    [SerializeField] private GameObject logUI;
    [SerializeField] private bool autoFindReferences = true;

    public static CombatLog Instance;

    private readonly List<LogRecord> records = new();
    private readonly List<CombatLogEntryView> liveViews = new();

    private bool IsConfigured => ResolveReferences(false) && combatLogEntryPrefab != null && combatLogGrid != null;

    private bool CanRenderImmediately =>
        IsConfigured
        && combatLogGrid.gameObject.activeInHierarchy
        && combatLogGrid.gameObject.scene.IsValid();

    private struct LogRecord
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
        ResolveReferences(true);
    }
    void Start() {

        if (logUI != null)
            logUI.SetActive(false);
    }
    private void OnEnable()
    {
        ResolveReferences(true);
        TryRenderAllRecords();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ToggleLoGUI()
    {
        ToggleLogUI();
    }

    public void ToggleLogUI()
    {
        ResolveReferences(true);

        if (logUI == null)
            return;

        logUI.SetActive(!logUI.activeSelf);

        if (logUI.activeSelf)
            TryRenderAllRecords();
    }

    public void AddAction(CardInstance cardInstance, string text)
    {
        if (cardInstance == null)
            return;

        AddAction(cardInstance, cardInstance.Data, cardInstance.Owner, text);
    }
    public void AddAnonymousAction(PlayerOwner owner, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        LogRecord incoming = new LogRecord
        {
            Owner = owner,
            Text = text,
        };

        if (ShouldAppendToLastRecord(incoming))
            AppendToLastRecord(incoming.Text);
        else
            records.Add(incoming);

        TrimToRecentLogs();

        if (CanRenderImmediately)
            TryRenderAllRecords();
    }
    public void AddAction(CardInstance cardInstance, CardData cardData, PlayerOwner owner, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        LogRecord incoming = new LogRecord
        {
            CardInstance = cardInstance,
            CardData = cardData,
            Owner = owner,
            Text = text,
        };

        if (ShouldAppendToLastRecord(incoming))
            AppendToLastRecord(incoming.Text);
        else
            records.Add(incoming);

        TrimToRecentLogs();

        if (CanRenderImmediately)
            TryRenderAllRecords();
    }

    public void Clear()
    {
        records.Clear();
        DestroyLiveViews();
    }


    private void TrimToRecentLogs()
    {
        int overflow = records.Count - MaxVisibleLogs;
        if (overflow <= 0)
            return;

        records.RemoveRange(0, overflow);
        DestroyLiveViews();
    }
    private bool ResolveReferences(bool includeInactive)
    {
        if (!autoFindReferences)
            return true;

        if (logUI == null)
            logUI = gameObject;

        if (combatLogGrid == null)
        {
            Transform[] all = GetComponentsInChildren<Transform>(includeInactive);
            foreach (Transform t in all)
            {
                if (t == transform)
                    continue;

                if (t.name.ToLower().Contains("grid") || t.GetComponent<UnityEngine.UI.GridLayoutGroup>() != null)
                {
                    combatLogGrid = t;
                    break;
                }
            }
        }

        return true;
    }

    private bool ShouldAppendToLastRecord(LogRecord incoming)
    {
        if (records.Count == 0)
            return false;

        LogRecord last = records[records.Count - 1];
        return incoming.CardInstance != null && last.CardInstance == incoming.CardInstance;
    }

    private void AppendToLastRecord(string text)
    {
        int index = records.Count - 1;
        LogRecord existing = records[index];

        if (string.IsNullOrWhiteSpace(existing.Text))
            existing.Text = text;
        else
            existing.Text += "\n" + text;

        records[index] = existing;
    }

    private void TryRenderAllRecords()
    {
        if (!CanRenderImmediately)
            return;

        if (combatLogGrid == null)
            return;

        if (liveViews.Count > records.Count)
            DestroyLiveViews();

        for (int i = liveViews.Count; i < records.Count; i++)
            CreateEntryView(records[i]);

        for (int i = 0; i < records.Count && i < liveViews.Count; i++)
            RefreshEntryView(liveViews[i], records[i]);
    }

    private void CreateEntryView(LogRecord record)
    {
        if (!IsConfigured)
            return;

        GameObject created = Instantiate(combatLogEntryPrefab, combatLogGrid);
        CombatLogEntryView view = created.GetComponent<CombatLogEntryView>();
        if (view == null)
            view = created.AddComponent<CombatLogEntryView>();

        liveViews.Add(view);
        RefreshEntryView(view, record);
    }

    private void RefreshEntryView(CombatLogEntryView view, LogRecord record)
    {
        if (view == null)
            return;

        view.Initialize(record.CardInstance, record.CardData, record.Owner, record.Text);
    }

    private void DestroyLiveViews()
    {
        for (int i = 0; i < liveViews.Count; i++)
        {
            if (liveViews[i] != null)
                Destroy(liveViews[i].gameObject);
        }

        liveViews.Clear();
    }
}
