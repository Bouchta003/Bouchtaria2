using UnityEngine;
using UnityEngine.Rendering;

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
    public CardInstance CreateCardInPosition(CardData data, PlayerOwner owner, Vector3 pos, Vector3 scale, Transform parent)
    {
        GameObject cardGO = Instantiate(cardPrefab, parent);
        cardGO.transform.localPosition = pos;cardGO.transform.localScale = scale;
        //cardGO.GetComponent<CardInstance>().SetZone(CardZone.Board);
        //cardGO.GetComponent<CardView>().UpdateMode();
        //cardGO.GetComponent<BoxCollider2D>().enabled=false;

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
