using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyEncounterDefinition", menuName = "Bouchtaria/Path Of Power/Enemy Encounter Definition")]
public class EnemyEncounterDefinition : ScriptableObject
{
    [SerializeField] private string encounterId;
    [SerializeField] private string displayName;
    [SerializeField] private int minimumFloor = 1;
    [SerializeField] private bool elite;
    [SerializeField] private bool warden;
    [SerializeField] private List<int> enemyDeckTemplate = new List<int>();

    public string EncounterId => string.IsNullOrWhiteSpace(encounterId) ? name : encounterId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int MinimumFloor => Mathf.Max(1, minimumFloor);
    public bool Elite => elite;
    public bool Warden => warden;
    public IReadOnlyList<int> EnemyDeckTemplate => enemyDeckTemplate;
}
