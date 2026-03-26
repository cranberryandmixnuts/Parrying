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

    private readonly struct SpriteFadeEntry
    {
        public readonly SpriteRenderer SpriteRenderer;
        public readonly float Alpha;

        public SpriteFadeEntry(SpriteRenderer spriteRenderer)
        {
            SpriteRenderer = spriteRenderer;
            Alpha = spriteRenderer.color.a;
        }
    }

    private sealed class DoorFadeTarget
    {
        public GameObject Root;
        public SpriteFadeEntry[] SpriteRenderers;
    }

    [TabGroup("Boss Room", "Refs"), BoxGroup("Boss Room/Refs/Boss"), SerializeField, Required]
    private BossController bossController;

    [TabGroup("Boss Room", "Refs"), BoxGroup("Boss Room/Refs/Dialogue"), SerializeField, Required]
    private GameObject dialogueRoot;

    [TabGroup("Boss Room", "Refs"), BoxGroup("Boss Room/Refs/Dialogue"), SerializeField, Required]
    private TMP_Text dialogueText;

    [TabGroup("Boss Room", "Refs"), BoxGroup("Boss Room/Refs/Door"), SerializeField]
    private GameObject lockDoor;

    [TabGroup("Boss Room", "Timing"), SerializeField, Min(0f)]
    private float startDelaySeconds = 1f;

    [TabGroup("Boss Room", "Timing"), SerializeField, Min(0f)]
    private float betweenLinesDelaySeconds = 1.6f;

    [TabGroup("Boss Room", "Timing"), SerializeField, Min(0f)]
    private float lockDoorFadeDuration = 0.5f;

    [TabGroup("Boss Room", "Typing"), SerializeField, Min(1f)]
    private float charsPerSecond = 30f;

    [TabGroup("Boss Room", "Typing"), SerializeField, Min(0f)]
    private float minLineDurationSeconds = 0.25f;

    [TabGroup("Boss Room", "Dialogue"), SerializeField]
    private List<DialogueLine> lines = new();

    private DoorFadeTarget doorFadeTarget;
    private Coroutine routine;
    private Tween typingTween;
    private Tween doorFadeTween;

    private void Start()
    {
        CacheDoorFadeTarget();
        SetDoorInstant(false);
        dialogueRoot.SetActive(false);

        routine = StartCoroutine(RunSequence());
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

        yield return FadeDoor(true);

        dialogueRoot.SetActive(false);

        yield return new WaitForSecondsRealtime(betweenLinesDelaySeconds / 2f);

        bossController.enabled = true;
        InputManager.Instance.SetAllModes(InputMode.Manual);

        yield return new WaitUntil(() => bossController.IsDead());

        Debug.Log("Scene End");
    }

    private IEnumerator PlayLine(string text)
    {
        KillTypingTween();

        dialogueText.text = string.Empty;

        float duration = Mathf.Max(minLineDurationSeconds, text.Length / charsPerSecond);

        typingTween = dialogueText
            .DOText(text, duration, true, ScrambleMode.None, null)
            .SetEase(Ease.Linear);

        yield return typingTween.WaitForCompletion();
    }

    private void CacheDoorFadeTarget()
    {
        SpriteRenderer[] spriteRenderers = lockDoor.GetComponentsInChildren<SpriteRenderer>(true);

        SpriteFadeEntry[] spriteEntries = new SpriteFadeEntry[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteEntries[i] = new SpriteFadeEntry(spriteRenderers[i]);

        doorFadeTarget = new DoorFadeTarget
        {
            Root = lockDoor,
            SpriteRenderers = spriteEntries
        };
    }

    private void SetDoorInstant(bool active)
    {
        float factor = active ? 1f : 0f;

        doorFadeTarget.Root.SetActive(true);
        SetDoorAlpha(doorFadeTarget, factor);

        if (!active)
            doorFadeTarget.Root.SetActive(false);
    }

    private IEnumerator FadeDoor(bool active)
    {
        if (doorFadeTween != null && doorFadeTween.IsActive())
            doorFadeTween.Kill();

        Sequence sequence = DOTween.Sequence();
        float factor = active ? 1f : 0f;

        if (active)
        {
            doorFadeTarget.Root.SetActive(true);
            SetDoorAlpha(doorFadeTarget, 0f);
        }

        JoinDoorFade(sequence, doorFadeTarget, factor);

        doorFadeTween = sequence;
        yield return sequence.WaitForCompletion();

        if (!active)
            doorFadeTarget.Root.SetActive(false);

        doorFadeTween = null;
    }

    private void JoinDoorFade(Sequence sequence, DoorFadeTarget target, float factor)
    {
        for (int i = 0; i < target.SpriteRenderers.Length; i++)
        {
            SpriteFadeEntry entry = target.SpriteRenderers[i];
            sequence.Join(entry.SpriteRenderer.DOFade(entry.Alpha * factor, lockDoorFadeDuration));
        }
    }

    private void SetDoorAlpha(DoorFadeTarget target, float factor)
    {
        for (int i = 0; i < target.SpriteRenderers.Length; i++)
        {
            SpriteFadeEntry entry = target.SpriteRenderers[i];
            Color color = entry.SpriteRenderer.color;
            color.a = entry.Alpha * factor;
            entry.SpriteRenderer.color = color;
        }
    }

    private void KillTypingTween()
    {
        if (typingTween != null && typingTween.IsActive())
            typingTween.Kill(false);

        typingTween = null;
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        KillTypingTween();

        if (doorFadeTween != null && doorFadeTween.IsActive())
            doorFadeTween.Kill();
    }
}