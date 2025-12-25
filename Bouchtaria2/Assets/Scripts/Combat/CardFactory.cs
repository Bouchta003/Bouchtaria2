using UnityEngine;

public class CardFactory : MonoBehaviour
{
    public static CardFactory Instance { get; private set; }

    [SerializeField] private GameObject cardPrefab;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public CardInstance CreateCard(CardData data, PlayerOwner owner)
    {
        GameObject cardGO = Instantiate(cardPrefab);

        CardInstance instance = cardGO.GetComponent<CardInstance>();
        if (instance == null)
        {
            Debug.LogError("Card prefab is missing CardInstance component!");
            Destroy(cardGO);
            return null;
        }

        instance.Initialize(data, owner);

        // Visual setup (sprite, text, etc.)
        cardGO.GetComponent<CardView>().Init(data);

        return instance;
    }
}
