using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public sealed class CurvedSlashSplineSequence : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private int sampleCount = 32;
    [SerializeField] private string curvedSlashDownStateName = "Curved Slash Down";
    [SerializeField] private string curvedSlashUpStateName = "Curved Slash Up";
    [SerializeField] private SplineContainer Spline1;
    [SerializeField] private SplineContainer Spline2;

    private readonly List<float> distanceTable = new();
    private readonly List<float> tTable = new();

    private Coroutine playRoutine;
    public Animator Anim => anim;

    [Button]
    public void Play()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(CoPlay(Spline1, Spline2));
    }

    [Button]
    public void StopSequence()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = null;
    }

    private IEnumerator CoPlay(SplineContainer downSplineContainer, SplineContainer upSplineContainer)
    {
        yield return PlaySegment(downSplineContainer, curvedSlashDownStateName);
        yield return PlaySegment(upSplineContainer, curvedSlashUpStateName);
        playRoutine = null;
    }

    private IEnumerator PlaySegment(SplineContainer splineContainer, string stateName)
    {
        BuildArcLengthTable(splineContainer);

        Vector3 startPosition = EvaluatePosition(splineContainer, 0f);
        Vector3 endPosition = EvaluatePosition(splineContainer, 1f);

        transform.position = startPosition;

        Anim.Play(stateName, 0, 0f);
        float animLength = GetAnimLength(stateName);

        float elapsed = 0f;
        float totalDistance = distanceTable[^1];

        while (elapsed < animLength)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / animLength);
            float distance = totalDistance * normalizedTime;
            float t = GetTByDistance(distance);

            transform.position = EvaluatePosition(splineContainer, t);

            yield return null;
        }

        transform.position = endPosition;
    }

    private void BuildArcLengthTable(SplineContainer splineContainer)
    {
        distanceTable.Clear();
        tTable.Clear();

        int resolvedSampleCount = Mathf.Max(2, sampleCount);

        float accumulatedDistance = 0f;
        Vector3 previousPosition = EvaluatePosition(splineContainer, 0f);

        tTable.Add(0f);
        distanceTable.Add(0f);

        for (int i = 1; i <= resolvedSampleCount; i++)
        {
            float t = i / (float)resolvedSampleCount;
            Vector3 currentPosition = EvaluatePosition(splineContainer, t);

            accumulatedDistance += Vector3.Distance(previousPosition, currentPosition);

            tTable.Add(t);
            distanceTable.Add(accumulatedDistance);

            previousPosition = currentPosition;
        }
    }

    private float GetTByDistance(float distance)
    {
        if (distance <= 0f)
            return 0f;

        float totalDistance = distanceTable[^1];

        if (distance >= totalDistance)
            return 1f;

        for (int i = 1; i < distanceTable.Count; i++)
        {
            float previousDistance = distanceTable[i - 1];
            float currentDistance = distanceTable[i];

            if (distance > currentDistance)
                continue;

            float lerp = Mathf.InverseLerp(previousDistance, currentDistance, distance);
            return Mathf.Lerp(tTable[i - 1], tTable[i], lerp);
        }

        return 1f;
    }

    private static Vector3 EvaluatePosition(SplineContainer splineContainer, float t)
    {
        float3 position = splineContainer.EvaluatePosition(t);
        return new Vector3(position.x, position.y, position.z);
    }

    private float GetAnimLength(string stateName)
    {
        AnimatorStateInfo current = Anim.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return current.length / global;
        }

        AnimatorStateInfo next = Anim.GetNextAnimatorStateInfo(0);
        if (next.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return next.length / global;
        }

        Anim.Update(0f);

        current = Anim.GetCurrentAnimatorStateInfo(0);
        if (current.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return current.length / global;
        }

        next = Anim.GetNextAnimatorStateInfo(0);
        if (next.IsName(stateName))
        {
            float global = Anim.speed;
            if (global <= 0f) return Mathf.Infinity;
            return next.length / global;
        }

        Debug.LogError($"EnemyBase: Animator state '{stateName}' not found or not playing.");
        return 0f;
    }
}