using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "KeyIconDatabase", menuName = "Scriptable Objects/KeyIconDatabase")]
public sealed class KeyIconDatabase : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [HorizontalGroup, LabelWidth(110)]
        public string ControlPath;

        [HorizontalGroup, HideLabel]
        public Sprite Sprite;
    }

    [SerializeField]
    private Entry[] entries;

    private Dictionary<string, Sprite> map;

    private void OnEnable()
    {
        Build();
    }

    public bool TryGet(string controlPath, out Sprite sprite)
    {
        if (map == null) Build();
        return map.TryGetValue(controlPath, out sprite);
    }

    [Button]
    public void Rebuild()
    {
        Build();
    }

    private void Build()
    {
        map = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            Entry e = entries[i];
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.ControlPath)) continue;
            if (e.Sprite == null) continue;

            map[e.ControlPath] = e.Sprite;
        }
    }
}