using System;
using UnityEngine;

public class CombatLog : MonoBehaviour
{
    [SerializeField] GameObject combatLogEntryPrefab;
    [SerializeField] GameObject combatLogGrid;
    [SerializeField] GameObject LogUI;

    public static CombatLog Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LogUI.SetActive(false);
    }
    public void ToggleLoGUI()
    {
        LogUI.SetActive(!LogUI.activeSelf);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
