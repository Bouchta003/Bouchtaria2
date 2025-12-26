using UnityEngine;
using UnityEngine.UI;

public class TraitsDisplay : MonoBehaviour
{
    [Header("Traits Icons")]
    [SerializeField] public Sprite pokemonIcon;
    [SerializeField] public Sprite inazumaIcon;
    [SerializeField] public Sprite healIcon;
    [SerializeField] public Sprite neutralIcon;

    [Header("Prefab components")]
    [SerializeField] public Image iconSlot;
    [SerializeField] public Image gemSlot;
    [SerializeField] public Image frameRaritySlot;

    [Header("Rarity Sprites")]
    [SerializeField] public Sprite trait1Icon;
    [SerializeField] public Sprite trait2Icon;
    [SerializeField] public Sprite trait3Icon;
    //[SerializeField] public Sprite trait4Icon;

    public CardData.Trait thisTrait;
    public int tier;
    public void DisplayTraitPregression()
    {
        Debug.Log($"{thisTrait} and max tier = {tier}");
    }
}
