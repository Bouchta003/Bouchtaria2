using UnityEngine;
using TMPro;
public class GameManager : MonoBehaviour
{
    public int CurrentMana { get; private set; } = 10;
    public int CurrentMaxMana { get; private set; } = 10;
    [SerializeField] TextMeshProUGUI manacounter;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        manacounter.text = $"{CurrentMana}/{CurrentMaxMana}";
    }

    public void RefreshMaxMana()
    {
        CurrentMana = CurrentMaxMana;
    }
    public void UseMana(int mana)
    {
        CurrentMana -= mana;
    }

}
