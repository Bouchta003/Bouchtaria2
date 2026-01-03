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

            if (cardInst.CurrentEffect.Contains("monsterpart*"))
            {
                assembledCards.Add(handCard);

                if (assembledCards.Count >= 2)
                {
                    string newEffect = "";
                    string newEffectText = "Gear : ";
                    int newMana = 0;

                    foreach (GameObject card in assembledCards)
                    {
                        CardInstance inst = card.GetComponent<CardInstance>();

                        newEffect += inst.CurrentEffect + " ";
                        newEffectText += inst.CurrentEffectText + " ";
                        newMana += inst.CurrentManaCost;

                        RemoveCardFromHand(card);
                        Destroy(card);

                    }

                    newEffect = CleanString(newEffect);
                    newEffectText = CleanString(newEffectText);


                    UpdateCardPositions();

                    CardInstance newGear = FindFirstObjectByType<GameManager>().AddCardToHand(Owner, 39);
                    newGear.CurrentEffect = "gear("+newEffect+"),targetunit";
                    newGear.CurrentEffectText = newEffectText;
                    Debug.Log(newGear.CurrentEffect+ "//" + newEffectText);
                    newGear.BaseManaCost = newMana;
                    newGear.GetComponent<CardView>().UpdateMode();
                    // Restart verification cleanly after modification
                    VerifyMonsterParts();
                    return; // IMPORTANT: stop current iteration
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
