using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatDialogue : MonoBehaviour
{
    //These will be setup from editor and only one dialoguecutscene is triggered depending on choice.
    //
    [SerializeField] List<DialogueCutscene> Dialogues;
    [SerializeField] public GameObject UIDialogue;
    [SerializeField] Image AvatarImage;
    [SerializeField] TextMeshProUGUI DialogueText;
    [SerializeField] TextMeshProUGUI DialogueName;
    int currentLine = 0; DialogueCutscene currentScene = null;
    public static CombatDialogue Instance;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public void TriggerCutscene(int id)
    {
        DialogueCutscene scene = Dialogues[id];
        //Set UI Dialogue as active and hide gamemanager.ui
        currentScene = scene;
        GameManager.Instance.UIparent.SetActive(false);
        UIDialogue.SetActive(true);
        AvatarImage.sprite = scene.Lines[0].Avatar;
        DialogueText.text = scene.Lines[0].Content;
        DialogueName.text = scene.Lines[0].Name;
    }
    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if (currentScene == null) return;
            currentLine++;
            if (currentLine < currentScene.Lines.Count)
            {
                AvatarImage.sprite = currentScene.Lines[currentLine].Avatar;
                DialogueText.text = currentScene.Lines[currentLine].Content;
                DialogueName.text = currentScene.Lines[currentLine].Name;
            }
            else //No more lines end of cutscene
            {
                currentScene = null;
                currentLine = 0;
                GameManager.Instance.UIparent.SetActive(true);
                UIDialogue.SetActive(false);
            }
        }
    }
}
[System.Serializable]
public class DialogueCutscene
{
    public List<DialogueLine> Lines;
}
[System.Serializable]
public class DialogueLine
{
    public string Content;
    public string Name;
    public Sprite Avatar;
}
