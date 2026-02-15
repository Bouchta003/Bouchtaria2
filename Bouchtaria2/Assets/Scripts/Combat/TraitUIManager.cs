using System.Collections.Generic;
using UnityEngine;

public class TraitUIManager : MonoBehaviour
{
    private Dictionary<CardData.Trait, TraitsDisplay> displaysByTrait;

    public void DetectTraitBorder()
    {
        displaysByTrait = new Dictionary<CardData.Trait, TraitsDisplay>();

        TraitsDisplay[] displays = GetComponentsInChildren<TraitsDisplay>(true);

        foreach (var display in displays)
        {
            if (!displaysByTrait.ContainsKey(display.thisTrait))
            {
                displaysByTrait.Add(display.thisTrait, display);
            }
            else
            {
                Debug.LogWarning($"Duplicate TraitDisplay for {display.thisTrait}");
            }
        }
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TraitsDisplay[] allTraits = FindObjectsByType<TraitsDisplay>(sortMode:FindObjectsSortMode.None);

            bool anyOpen = false;

            // Check if any trait window is open
            foreach (TraitsDisplay trait in allTraits)
            {
                if (trait.traitEffect.activeSelf)
                {
                    anyOpen = true;
                    break;
                }
            }

            // If any is open, close them all
            if (anyOpen)
            {
                foreach (TraitsDisplay trait in allTraits)
                {
                    trait.traitEffect.SetActive(false);
                }
            }
        }
    }

    public TraitsDisplay GetTraitDisplay(CardData.Trait trait)
    {
        displaysByTrait.TryGetValue(trait, out TraitsDisplay display);
        return display;
    }
    public void UpdateTraitProgress(CardData.Trait trait, int progress, int currentCap)
    {
        TraitsDisplay display = GetTraitDisplay(trait);
        if (display == null)
            return;

        display.Progression = progress;
        display.CurrentCap = currentCap;
    }

    public void ActivateTrait(CardData.Trait trait, int tier)
    {
        if (displaysByTrait.TryGetValue(trait, out var display))
        {
            display.Activate(tier);
        }
        else
        {
            Debug.LogWarning($"No TraitDisplay found for {trait}");
        }
    }
}
