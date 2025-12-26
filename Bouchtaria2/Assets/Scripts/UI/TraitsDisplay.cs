using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitsDisplay : MonoBehaviour
{
    [Header("Traits Icons")]
    [SerializeField] public Sprite pokemonIcon;//
    [SerializeField] public Sprite inazumaIcon;//
    [SerializeField] public Sprite healIcon;//
    [SerializeField] public Sprite blizzardIcon;//
    [SerializeField] public Sprite gunnerIcon;
    [SerializeField] public Sprite workoutIcon;//
    [SerializeField] public Sprite faithIcon;//
    [SerializeField] public Sprite ritualIcon;//
    [SerializeField] public Sprite memeIcon;//
    [SerializeField] public Sprite neutralIcon;//
    [SerializeField] public Sprite comboIcon;//
    [SerializeField] public Sprite haterIcon;//
    [SerializeField] public Sprite spellFocusIcon;//
    [SerializeField] public Sprite speedsterIcon;

    [Header("Prefab components")]
    [SerializeField] public Image iconSlot;
    [SerializeField] public Image gemSlot;
    [SerializeField] public Image frameRaritySlot;
    [SerializeField] public GameObject traitEffect;

    [Header("Rarity Sprites")]
    [SerializeField] public Sprite trait1Icon;
    [SerializeField] public Sprite trait2Icon;
    [SerializeField] public Sprite trait3Icon;
    //[SerializeField] public Sprite trait4Icon;
    public CardData.Trait thisTrait;
    public int tier;
    public void DisplayTraitProgression()
    {
        traitEffect.SetActive(!traitEffect.activeSelf);
        string display = thisTrait.ToString()+" :\n";
        switch (thisTrait)
        {
            case CardData.Trait.Pokemon:
                display += "Kill enemies to activate :" +
                    "\nTier 1 : The next Pokemon you play evolves instantly.";
                if(tier>1) display += 
                     "\nTier 2 : The next Pokemon you play evolves instantlyand costs (2) less."; 
                if (tier > 2) display +=
                         "\nTier 3 : Discover a LEGENDARY Pokemon.";
                break;
            default:
                display += "Need to define this trait's tier logic";
                break;
        }
        traitEffect.GetComponentInChildren<TextMeshProUGUI>().text=display;
    }
}
