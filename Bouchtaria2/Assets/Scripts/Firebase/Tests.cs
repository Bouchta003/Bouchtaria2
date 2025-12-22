using UnityEngine;

public class Tests : MonoBehaviour
{
    public static void ConductTest()
    {
        UserCollectionManager.Instance.UnlockCard(1);
        UserCollectionManager.Instance.UnlockCard(2);
        UserCollectionManager.Instance.UnlockCard(6);
    }
}
