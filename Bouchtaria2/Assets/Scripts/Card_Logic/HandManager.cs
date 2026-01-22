using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Rendering;
using System.Text.RegularExpressions;

public class HandManager : MonoBehaviour
{
    [SerializeField] public int maxHandSize;
    public PlayerOwner Owner;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] SplineContainer splineContainer;
    [SerializeField] Transform spawnPoint;

    [SerializeField] int baseSortingOrder = 100;
    public List<GameObject> handCards = new();

    public void RemoveCardFromHand(GameObject cardToRemove)
    {
        if (handCards.Remove(cardToRemove))
        {
            UpdateCardPositions();
        }
    }
    public void AddCard(GameObject card)
    {
        CardInstance instance = card.GetComponent<CardInstance>();
        if (instance == null)
            return;

        if (instance.Owner != Owner)
            return;

        // 🔑 THIS LINE FIXES EVERYTHING
        card.transform.SetParent(transform, false);

        handCards.Add(card);
        UpdateCardPositions();
    }
    string CleanString(string input)
    {
        // Remove whole-word "monsterpart"
        input = Regex.Replace(input, @"\bmonsterpart\b", "");

        // Collapse multiple spaces into one
        input = Regex.Replace(input, @"\s+", " ");

        // Trim leading/trailing spaces
        return input.Trim();
    }
    public void VerifyMonsterParts()
    {
        List<GameObject> assembledCards = new List<GameObject>();

        // Iterate over a COPY so handCards can be modified safely
        foreach (GameObject handCard in new List<GameObject>(handCards))
        {
            CardInstance cardInst = handCard.GetComponent<CardInstance>();

            if (cardInst.CurrentEffect.Contains("monsterpart*") &&
                !cardInst.CurrentEffect.StartsWith("gear"))
            {
                assembledCards.Add(handCard);

                if (assembledCards.Count >= 2)
                {
                    List<string> effectParts = new();
                    List<string> effectTextParts = new();
                    int newMana = 0;

                    foreach (GameObject card in assembledCards)
                    {
                        CardInstance inst = card.GetComponent<CardInstance>();
                        string cleaned = inst.CurrentEffect
                                    .Replace("*", "")
                                    .Replace("monsterpart", "")
                                    .Trim();

                        effectParts.Add(cleaned);

                        effectTextParts.Add(inst.CurrentEffectText);
                        newMana += inst.CurrentManaCost;

                        RemoveCardFromHand(card);
                        Destroy(card);
                    }

                    string combinedEffects = string.Join(",", effectParts);
                    string combinedEffectText = string.Join(" ", effectTextParts);

                    // 🔑 Create the gear card AFTER consuming parts
                    CardInstance newGear =
                        FindFirstObjectByType<GameManager>().AddCardToHand(Owner, 39);

                    newGear.CurrentEffect = $"gear({combinedEffects},targetunit)";
                    newGear.CurrentEffectText = $"Gear: {combinedEffectText}";
                    newGear.BaseManaCost = newMana;
                    newGear.ParseEffects(); // safety
                    newGear.CurrentCastEffect = newGear.CurrentEffect;

                    newGear.GetComponent<CardView>().UpdateMode();

                    Debug.Log(newGear.CurrentEffect + " // " + newGear.CurrentEffectText);

                    // Restart verification cleanly
                    VerifyMonsterParts();
                    return;
                }
            }
        }
    }

    public void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;

        float cardSpacing = 1f / maxHandSize;
        float firstCardPosition = 0.5f - (handCards.Count - 1) * cardSpacing / 2;
        Spline spline = splineContainer.Spline;

        for (int i = 0; i < handCards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;

            Vector3 splinePosition = spline.EvaluatePosition(p);
            Vector3 forward = spline.EvaluateTangent(p);
            Vector3 up = spline.EvaluateUpVector(p);

            // 🔒 ORIGINAL ROTATION (unchanged)
            Quaternion rotation = Quaternion.LookRotation(
                up,
                Vector3.Cross(up, forward).normalized
            );

            GameObject card = handCards[i];

            card.transform.DOLocalMove(splinePosition, 0.25f);

            card.transform.DOLocalRotateQuaternion(rotation, 0.25f);

            // 🎨 Sorting polish
            SortingGroup group = card.GetComponent<SortingGroup>();
            if (group != null)
            {
                group.sortingOrder = baseSortingOrder + i;
            }
        }
        VerifyMonsterParts();
    }

    // =========================
    // POLISH HOOKS (CALLED BY CARD)
    // =========================

    public void RaiseCard(GameObject card, int bonus = 100)
    {
        SortingGroup group = card.GetComponent<SortingGroup>();
        if (group != null)
        {
            group.sortingOrder = baseSortingOrder + maxHandSize + bonus;
        }
    }

    public void RestoreCardOrder()
    {
        UpdateCardPositions();
    }


}
