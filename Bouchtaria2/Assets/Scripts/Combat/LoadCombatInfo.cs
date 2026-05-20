using UnityEngine;
using TMPro;
using System.Collections.Generic;
public class LoadCombatInfo : MonoBehaviour
{
    TextMeshProUGUI infoText;
    void Start()
    {
        infoText = GameManager.Instance.CombatInfo.GetComponentInChildren<TextMeshProUGUI>();
        UpdateText();
    }
    void UpdateText()
    {
        if (GameRunContext.IsPathOfPowerRun)
        {
            PathOfPowerRunData run = GameRunContext.PathOfPowerData;
            PathOfPowerStepData step = run?.CurrentStepData;
            string stepName = step != null ? step.stepType.ToString() : "Unknown step";
            string enemyRelicsText = BuildEnemyRelicsText(run?.activeEnemyRelicTexts);
            infoText.text = $"Path Of Power - Floor {run?.currentFloor ?? 1}, Step {run?.currentStep ?? 1}\nEncounter: {stepName}.\nRelics: {enemyRelicsText}";
        }
        else if (GameRunContext.IsDungeonRun)
        {
            if (GameRunContext.DungeonData.floor <= 15)
                infoText.text = $"Floor {GameRunContext.DungeonData.floor}\nNo particular enemy effect.";

            if (GameRunContext.DungeonData.floor > 15) 
                infoText.text = $"Floor {GameRunContext.DungeonData.floor}\nThe enemy units now gain +1/+1 at the end of the turn.";

            if (GameRunContext.DungeonData.floor > 30)
                infoText.text = $"Floor {GameRunContext.DungeonData.floor}\nThe enemy now draws 1 card the end of the turn and heals for 5.\nTheir units still gain +1/+1 at the end of the turn by the way.";
        }
        else if (GameRunContext.IsAdventureCombat)
        {
            switch (GameRunContext.AdventureFightId)
            {
                case 0:
                    infoText.text = "Unknown combat, please notify Bouchta of this bug and take a screenshot :)\nSorry if the game is still buggy";
                    break;
                case 1:
                    infoText.text = "No particular tip in this combat, give it your all !";
                    break;
                case 2:
                    infoText.text = "No particular tip in this combat, give it your all !";
                    break;
                case 3:
                    infoText.text = "No particular tip in this combat, give it your all !";
                    break;
                case 4:
                    infoText.text = "No particular tip in this combat, give it your all !";
                    break;
                case 5:
                    infoText.text = "No particular tip in this combat, give it your all !";
                    break;
                case 6:
                    infoText.text = "No particular tip in this combat, give it your all !";
                    break;
                case 7:
                    infoText.text = "No particular tip in this combat, give it your all !";
                    break;
                case 8:
                    infoText.text = "No particular tip in this combat, give it your all !";
                    break;
                case 9://Mama
                    infoText.text = "La madre plays with huge protect units and has the ability to both summon a lot and revive, silences might be a good idea.";
                    if (GameRunContext.IsAdventureHardMode)
                        infoText.text = "La madre now has access to a special spell that grants allies buffs each turn for the rest of the game.";
                    break;
                case 10://Sara
                    infoText.text = "Sara's favorite Pokemon and pets assemble for a pretty aggressive strategy. Don't forget to bring heals.";
                    if (GameRunContext.IsAdventureHardMode)
                        infoText.text = "Sara now focuses on menaces that combine units with the kill effect and big stats. You might not want to put all your eggs in the same basket.";
                    break;
                case 11://Rhita
                    infoText.text = "Rhita's healer deck is slow but can scale pretty fast if you let her finish her traits. Try an aggressive strategy !";
                    if (GameRunContext.IsAdventureHardMode)
                        infoText.text = "Rhita's focuses more on heal and huge buffs, even fatigue cannot stop her, do NOT let her complete her trait bonus!";
                    break;
                case 12://Papa
                    infoText.text = "El Padre plays around 'great rods' to summon minions from his deck. \nKeep count of how much were summonned, if you can handle his aggressive attacks, you might have a chance.";
                    if (GameRunContext.IsAdventureHardMode)
                        infoText.text = "El Padre is even more aggressive in hard mode. \nHe has more copies of his key cards and has new support cards for his play style.";
                    break;
                case 13://Bouchta
                    infoText.text = "The Bouchta is a Soul Eater, the souls of the fallen nourish him and his fierce knights stand with him. There is no tip this time give it your all !"; 
                    if (GameRunContext.IsAdventureHardMode)
                        infoText.text = "The Bouchta isn't that much stronger than before, but the Prime curse has some new legendary tools that you might not want to mess around with !\nBy the way their units gain +1/+1 at the end of the turn... yes it's free.";
                    break;
            }
        }
        else //Quickgame
        {
            infoText.text = "This is a quickgame, no particular tips for you.\nEnjoy a chill fight to test out your decks and practice core mechanics.";
        }
    }

    private string BuildEnemyRelicsText(IReadOnlyList<string> relicTexts)
    {
        if (relicTexts == null || relicTexts.Count == 0)
            return "None";

        return string.Join(", ", relicTexts);
    }
}
