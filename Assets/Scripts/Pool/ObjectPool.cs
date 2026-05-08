using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool : MonoBehaviour
{
    [Serializable]
    public sealed class PoolEntry
    {
        public string Key;
        public GameObject Prefab;
        [Range(0, 100)] public int PrewarmCount = 10;
    }

    [SerializeField] private List<PoolEntry> _entries = new();

    private readonly Dictionary<string, Queue<GameObject>> _available = new();
    private readonly Dictionary<string, GameObject> _prefabs = new();

    public void Initialize()
    {
        foreach (var entry in _entries)
        {
            _prefabs[entry.Key] = entry.Prefab;
            _available[entry.Key] = new Queue<GameObject>();

            for (int i = 0; i < entry.PrewarmCount; i++)
                _available[entry.Key].Enqueue(CreateNew(entry.Key));
        }
    }

    public GameObject Get(string key)
    {
        if (!_available.TryGetValue(key, out var queue))
            throw new InvalidOperationException($"[ObjectPool] Key '{key}' not registered.");

        var obj = queue.Count > 0 ? queue.Dequeue() : CreateNew(key);
        obj.SetActive(true);
        return obj;
    }

    public T Get<T>(string key) where T : Component
    {
        var obj = Get(key);
        return obj.GetComponent<T>() ?? throw new InvalidOperationException($"Missing {typeof(T).Name}");
    }

    public void Return(GameObject obj)
    {
        var key = obj.name.Replace("(Clone)", "").Trim();

        if (!_available.TryGetValue(key, out var queue))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        queue.Enqueue(obj);
    }

    private GameObject CreateNew(string key)
    {
        var obj = Instantiate(_prefabs[key], transform);
        obj.name = key;
        obj.SetActive(false);
        return obj;
    }
}