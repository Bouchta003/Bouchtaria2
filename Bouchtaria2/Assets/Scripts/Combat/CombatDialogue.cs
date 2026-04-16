using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatDialogue : MonoBehaviour
{
    public event System.Action OnDialogueEnded;
    [Header("Data")]
    [SerializeField] List<DialogueCutscene> Dialogues;

    [Header("UI")]
    [SerializeField] CanvasGroup dialogueCanvasGroup;
    [SerializeField] Image AvatarImage;
    [SerializeField] public GameObject UIDialogue;
    [SerializeField] TextMeshProUGUI DialogueText;
    [SerializeField] TextMeshProUGUI DialogueName;

    [Header("Settings")]
    [SerializeField] float fadeDuration = 0.3f;
    [SerializeField] float typingSpeed = 0.03f;

    int currentLine = 0;
    DialogueCutscene currentScene = null;

    bool isTyping = false;
    Coroutine typingCoroutine;
    bool resumeCombatAfterDialogue = true;
    private int adventureFightId = -1; // Store the adventure fight ID

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

    public void TriggerCutscene(int id, bool resumeCombat = true)
    {
        // Pause music when dialogue starts
        MusicManager.Instance.PauseCurrentMusic();

        // Store adventure fight ID if this is an adventure
        if (GameRunContext.IsAdventureCombat)
        {
            adventureFightId = GameRunContext.AdventureFightId;
        }
        if (Dialogues.Count<id+1) return;

        currentScene = Dialogues[id];
        currentLine = 0;
        resumeCombatAfterDialogue = resumeCombat;

        GameManager.Instance.UIparent.SetActive(false);
        StartCoroutine(FadeIn());

        DisplayLine();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (currentScene == null) return;

        if (isTyping)
        {
            // Finish instantly
            StopCoroutine(typingCoroutine);
            DialogueText.text = currentScene.Lines[currentLine].Content;
            isTyping = false;
        }
        else
        {
            NextLine();
        }
    }

    void NextLine()
    {
        currentLine++;

        if (currentLine < currentScene.Lines.Count)
        {
            DisplayLine();
        }
        else
        {
            StartCoroutine(EndDialogue());
        }
    }
    public void SkipDialogue()
    {
        StartCoroutine(FadeOut());

        currentScene = null;
        currentLine = 0;

        GameManager.Instance.UIparent.SetActive(true);
        OnDialogueEnded?.Invoke();
        if (resumeCombatAfterDialogue)
            GameManager.Instance.SetupFirstTurn();
    }
    void DisplayLine()
    {
        var line = currentScene.Lines[currentLine];

        DialogueName.text = line.Name;
        AvatarImage.sprite = line.Avatar;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.Content));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        DialogueText.text = "";

        foreach (char c in text)
        {
            DialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    IEnumerator FadeIn()
    {
        dialogueCanvasGroup.gameObject.SetActive(true);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            dialogueCanvasGroup.alpha = t / fadeDuration;
            yield return null;
        }

        dialogueCanvasGroup.alpha = 1;
    }

    IEnumerator FadeOut()
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            dialogueCanvasGroup.alpha = 1 - (t / fadeDuration);
            yield return null;
        }

        dialogueCanvasGroup.alpha = 0;
        dialogueCanvasGroup.gameObject.SetActive(false);
    }
    private AudioClip GetAdventureMusicForFight(int fightId)
    {
        // Get the music for the specific adventure fight ID
        return MusicManager.Instance.GetMusicForAdventure(fightId);
    }
    IEnumerator EndDialogue()
    {
        yield return StartCoroutine(FadeOut());

        currentScene = null;
        currentLine = 0;

        GameManager.Instance.UIparent.SetActive(true);
        OnDialogueEnded?.Invoke();
        if (resumeCombatAfterDialogue)
            GameManager.Instance.SetupFirstTurn();

        // Resume/change music after dialogue ends
        if (adventureFightId != -1)
        {
            // Play the specific adventure music for this fight
            AudioClip adventureMusic = GetAdventureMusicForFight(adventureFightId);
            if (adventureMusic != null)
            {
                MusicManager.Instance.PlayMusic(adventureMusic, 1.5f);
            }
        }
        else
        {
            // Resume normal music
            MusicManager.Instance.PlayCurrentMusic();
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
