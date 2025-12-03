using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string uniqueId = "";
    public string UniqueId => uniqueId;

#if UNITY_EDITOR
    private static readonly HashSet<string> usedIds = new HashSet<string>();

    private void OnEnable()
    {
        if (!Application.isPlaying) RegisterOrRefresh();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) RegisterOrRefresh();
    }

    private void RegisterOrRefresh()
    {
        if (string.IsNullOrEmpty(uniqueId) || usedIds.Contains(uniqueId))
        {
            uniqueId = Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        usedIds.Add(uniqueId);
    }
#endif
}
