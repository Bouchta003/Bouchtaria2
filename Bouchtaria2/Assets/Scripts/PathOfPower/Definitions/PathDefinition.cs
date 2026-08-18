using UnityEngine;

[CreateAssetMenu(fileName = "PathDefinition", menuName = "Bouchtaria/Path Of Power/Path Definition")]
public class PathDefinition : ScriptableObject
{
    [SerializeField] private PathOfPowerPathType pathType = PathOfPowerPathType.Normal;
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private bool allowEliteSteps;
    [SerializeField] private bool grantsPreWardenRelic;

    public PathOfPowerPathType PathType => pathType;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? pathType.ToString() : displayName;
    public string Description => description;
    public bool AllowEliteSteps => allowEliteSteps;
    public bool GrantsPreWardenRelic => grantsPreWardenRelic;
}
