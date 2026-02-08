using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public sealed class BossRoomSceneDirector : Singleton<BossRoomSceneDirector, SceneScope>
{
    [Serializable]
    public sealed class DialogueLine
    {
        [TextArea(2, 6)]
        public string Text;
    }

    [TabGroup("Boss Room", "Refs"), BoxGroup("Boss Room/Refs/Boss"), SerializeField, Required]
    private BossController bossController;

    [TabGroup("Boss Room", "Refs"), BoxGroup("Boss Room/Refs/Dialogue"), SerializeField, Required]
    private GameObject dialogueRoot;

    [TabGroup("Boss Room", "Refs"), BoxGroup("Boss Room/Refs/Dialogue"), SerializeField, Required]
    private TMP_Text dialogueText;

    [TabGroup("Boss Room", "Refs"), BoxGroup("Boss Room/Refs/Door"), SerializeField]
    private GameObject[] lockDoors;

    [TabGroup("Boss Room", "Timing"), SerializeField, Min(0f)]
    private float startDelaySeconds = 1f;

    [TabGroup("Boss Room", "Timing"), SerializeField, Min(0f)]
    private float betweenLinesDelaySeconds = 1.6f;

    [TabGroup("Boss Room", "Typing"), SerializeField, Min(1f)]
    private float charsPerSecond = 30f;

    [TabGroup("Boss Room", "Typing"), SerializeField, Min(0f)]
    private float minLineDurationSeconds = 0.25f;

    [TabGroup("Boss Room", "Dialogue"), SerializeField]
    private List<DialogueLine> lines = new();

    private Tween typingTween;

    private void Start()
    {
        SetDoorsActive(false);
        dialogueRoot.SetActive(false);

        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        yield return null;

        bossController.enabled = false;

        yield return new WaitForSecondsRealtime(startDelaySeconds);

        dialogueRoot.SetActive(true);

        int count = lines.Count;
        for (int i = 0; i < count; i++)
        {
            yield return PlayLine(lines[i].Text);

            yield return new WaitForSecondsRealtime(betweenLinesDelaySeconds);
        }

        SetDoorsActive(true);
        dialogueRoot.SetActive(false);

        yield return new WaitForSecondsRealtime(betweenLinesDelaySeconds);

        bossController.enabled = true;

        InputManager.Instance.SetAllModes(InputMode.Manual);

        Debug.Log("Scene End");
    }

    private IEnumerator PlayLine(string text)
    {
        KillTypingTween();

        dialogueText.text = string.Empty;

        float duration = Mathf.Max(minLineDurationSeconds, text.Length / charsPerSecond);

        typingTween = dialogueText
            .DOText(text, duration, true, ScrambleMode.None, null)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        yield return typingTween.WaitForCompletion();
    }

    private void SetDoorsActive(bool active)
    {
        int count = lockDoors.Length;
        for (int i = 0; i < count; i++)
            lockDoors[i].SetActive(active);
    }

    private void KillTypingTween()
    {
        if (typingTween != null && typingTween.IsActive())
            typingTween.Kill(false);

        typingTween = null;
    }

    private void OnDisable()
    {
        KillTypingTween();
    }
}