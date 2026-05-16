using UnityEngine;

[CreateAssetMenu(fileName = "EventDefinition", menuName = "Bouchtaria/Path Of Power/Event Definition")]
public class EventDefinition : ScriptableObject
{
    [SerializeField] private string eventId;
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private Sprite artwork;
    [SerializeField] private int minimumFloor = 1;
    [SerializeField] private bool enabledInFirstVersion = true;

    public string EventId => string.IsNullOrWhiteSpace(eventId) ? name : eventId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Artwork => artwork;
    public int MinimumFloor => Mathf.Max(1, minimumFloor);
    public bool EnabledInFirstVersion => enabledInFirstVersion;

    // TODO(Path Of Power): add event choice payloads and rewards when event content is authored.
}
