using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class ArenaSceneDirector : Singleton<ArenaSceneDirector, SceneScope>
{
    [Serializable]
    public sealed class SpawnDefinition
    {
        [HorizontalGroup("Row", 0.55f), HideLabel]
        [ValidateInput(nameof(ValidatePrefab), "Prefab root must contain an EnemyBase in children.")]
        public GameObject PrefabRoot;

        [HorizontalGroup("Row", 0.35f), HideLabel]
        public Transform Point;

        [HorizontalGroup("Row", 0.10f), HideLabel, MinValue(1)]
        public int Count = 1;

        private bool ValidatePrefab(GameObject value)
        {
            if (value == null) return false;
            return value.GetComponentInChildren<EnemyBase>(true) != null;
        }
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true)]
        public List<SpawnDefinition> Spawns = new();
    }

    private sealed class SpawnedEnemy
    {
        public GameObject Root;
        public EnemyBase Enemy;
    }

    [TabGroup("Arena", "Timing"), SerializeField, Min(0f)]
    private float startDelaySeconds = 1f;

    [TabGroup("Arena", "Timing"), SerializeField, Min(0f)]
    private float betweenWavesDelaySeconds = 1f;

    [TabGroup("Arena", "Refs"), SerializeField]
    private Transform enemiesRoot;

    [TabGroup("Arena", "Refs"), SerializeField]
    private GameObject[] lockDoors;

    [TabGroup("Arena", "Refs"), SerializeField]
    private GameObject ExitBox;

    [TabGroup("Arena", "Waves"), SerializeField, ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true)]
    private List<WaveDefinition> waves = new();

    private readonly List<SpawnedEnemy> spawned = new(64);

    private Coroutine routine;

    private void Start()
    {
        Time.timeScale = 1f;
        InputManager.Instance.SetAllModes(InputMode.Manual);

        SetDoorsActive(false);
        ExitBox.SetActive(false);
        routine = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        if (startDelaySeconds > 0f)
            yield return new WaitForSeconds(startDelaySeconds);

        SetDoorsActive(true);

        int waveCount = waves.Count;
        for (int i = 0; i < waveCount; i++)
        {
            SpawnWave(waves[i]);

            yield return WaitForWaveCleared();

            if (i < waveCount - 1 && betweenWavesDelaySeconds > 0f)
                yield return new WaitForSeconds(betweenWavesDelaySeconds);
        }

        SetDoorsActive(false);
        ExitBox.SetActive(true);

        Debug.Log("Scene End");
    }

    private void SpawnWave(WaveDefinition wave)
    {
        spawned.Clear();

        int spawnCount = wave.Spawns.Count;
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnDefinition s = wave.Spawns[i];

            for (int c = 0; c < s.Count; c++)
            {
                GameObject root = Instantiate(s.PrefabRoot, s.Point.position, s.Point.rotation, enemiesRoot);
                SpawnedEnemy handle = new()
                {
                    Root = root,
                    Enemy = root.GetComponentInChildren<EnemyBase>(true)
                };
                spawned.Add(handle);
            }
        }
    }

    private IEnumerator WaitForWaveCleared()
    {
        yield return new WaitUntil(() => !HasAliveEnemy());
    }

    private bool HasAliveEnemy()
    {
        int count = spawned.Count;
        for (int i = 0; i < count; i++)
        {
            SpawnedEnemy h = spawned[i];

            if (h.Root == null) continue;

            if (h.Enemy == null)
                h.Enemy = h.Root.GetComponentInChildren<EnemyBase>(true);

            if (h.Enemy != null && !h.Enemy.IsDead())
                return true;
        }

        return false;
    }

    private void SetDoorsActive(bool active)
    {
        for (int i = 0; i < lockDoors.Length; i++)
            lockDoors[i].SetActive(active);
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
    }
}