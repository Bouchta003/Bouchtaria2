using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    private bool cardsReady;
    private bool collectionReady;

    void Start()
    {
        CardDatabase.Instance.OnCardsLoaded += OnCardsLoaded;
        UserCollectionManager.Instance.OnCollectionReady += OnCollectionReady;
    }

    private void OnCardsLoaded()
    {
        cardsReady = true;
        TryEnterCollection();
    }

    private void OnCollectionReady()
    {
        collectionReady = true;
        TryEnterCollection();
    }

    private void TryEnterCollection()
    {
        if (!cardsReady || !collectionReady)
            return;

        Debug.Log("🚀 Game ready — loading CollectionScene");
        SceneManager.LoadScene("Collection");
    }
}
