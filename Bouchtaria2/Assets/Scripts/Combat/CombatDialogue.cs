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
    bool holdLastLineVisibleOnFinish = false;
    private int adventureFightId = -1;

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

    // ─────────────────────────────────────────────────────────────
    //  Public entry point
    // ─────────────────────────────────────────────────────────────

    public void TriggerCutscene(int id, bool resumeCombat = true, bool holdLastLineVisible = false)
    {
        if (Dialogues.Count < id + 1) return;

        // Store the adventure fight ID so EndDialogue knows which track to queue.
        adventureFightId = GameRunContext.IsAdventureCombat ? GameRunContext.AdventureFightId : -1;

        currentScene = Dialogues[id];
        currentLine = 0;
        resumeCombatAfterDialogue = resumeCombat;
        holdLastLineVisibleOnFinish = holdLastLineVisible;

        // ── Music: gently duck to an underscore level rather than hard-stopping.
        // This keeps the atmosphere alive while voices play front-and-centre.
        MusicManager.Instance.DuckForDialogue();

        if (GameManager.Instance != null && GameManager.Instance.UIparent != null)
            GameManager.Instance.UIparent.SetActive(false);
        StartCoroutine(FadeIn());
        DisplayLine();
    }

    // ─────────────────────────────────────────────────────────────
    //  Input
    // ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (currentScene == null) return;

        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            DialogueText.text = currentScene.Lines[currentLine].Content;
            isTyping = false;
        }
        else
        {
            NextLine();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Dialogue flow
    // ─────────────────────────────────────────────────────────────

    void NextLine()
    {
        currentLine++;

        if (currentLine < currentScene.Lines.Count)
            DisplayLine();
        else if (holdLastLineVisibleOnFinish)
        {
            OnDialogueEnded?.Invoke();
        }
        else
            StartCoroutine(EndDialogue());
    }

    public void SkipDialogue()
    {
        StartCoroutine(FadeOut());

        currentScene = null;
        currentLine = 0;

        if (GameManager.Instance != null && GameManager.Instance.UIparent != null)
            GameManager.Instance.UIparent.SetActive(true);
        OnDialogueEnded?.Invoke();

        // ── Music: handle skip the same way as a normal end.
        ResolveMusicAfterDialogue();

        if (resumeCombatAfterDialogue && GameManager.Instance != null && !GameManager.Instance.adventureBossSecondPhaseTriggered)
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

    IEnumerator EndDialogue()
    {
        yield return StartCoroutine(FadeOut());

        currentScene = null;
        currentLine = 0;

        if (GameManager.Instance != null && GameManager.Instance.UIparent != null)
            GameManager.Instance.UIparent.SetActive(true);
        OnDialogueEnded?.Invoke();

        // ── Music: restore / transition AFTER the visual fade-out completes,
        // so the new track swells in exactly as the UI returns — cinematic timing.
        ResolveMusicAfterDialogue();

        if (resumeCombatAfterDialogue && GameManager.Instance != null)
            GameManager.Instance.SetupFirstTurn();
    }

    // ─────────────────────────────────────────────────────────────
    //  Music resolution helper
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called at the end of a dialogue (or skip).
    /// If we have an adventure-specific track, crossfade into it from the
    /// currently-ducked music so the transition feels intentional and smooth.
    /// Otherwise simply un-duck the existing track back to full volume.
    /// </summary>
    private void ResolveMusicAfterDialogue()
    {
        if (MusicManager.Instance == null)
            return;

        // Final adventure boss dialogue (cutscene 15) should not restart / replay combat music
        // after phase 2 is completed.
        if (GameManager.Instance != null &&
            GameManager.Instance.adventureBossFinalDialogueTriggered &&
            !resumeCombatAfterDialogue)
        {
            return;
        }

        if (adventureFightId != -1)
        {
            // Crossfade from the ducked track directly into the battle music.
            // UnduckAndCrossfadeTo handles the case where newClip is null gracefully.
            AudioClip battleTrack = MusicManager.Instance.GetMusicForAdventure(adventureFightId);
            if (GameManager.Instance.adventureBossSecondPhaseTriggered || GameManager.Instance.adventureBossFinalDialogueTriggered)
            {
                battleTrack = MusicManager.Instance.GetMusicForAdventure(14);
            }
            MusicManager.Instance.UnduckAndCrossfadeTo(battleTrack, 2.0f);
        }
        else
        {
            // No track switch needed — just bring the existing music back up.
            MusicManager.Instance.UnduckAfterDialogue();
        }
    }


    public void CloseDialogue()
    {
        StopAllCoroutines();
        currentScene = null;
        currentLine = 0;
        isTyping = false;
        dialogueCanvasGroup.alpha = 0f;
        dialogueCanvasGroup.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  UI fades
    // ─────────────────────────────────────────────────────────────

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
}

// ─────────────────────────────────────────────────────────────────────────────
//  Data types
// ─────────────────────────────────────────────────────────────────────────────

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
