using System;
using System.Collections.Generic;

using UnityEngine;

public sealed class ObjectPool : MonoBehaviour
{

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

        var obj = queue.Count > 0
            ? queue.Dequeue()
            : CreateNew(key);

        obj.SetActive(true);
        return obj;
    }

    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        var obj = Get(key);
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    public T Get<T>(string key) where T : Component
    {
        var obj = Get(key);
        return obj.GetComponent<T>()
               ?? throw new InvalidOperationException(
                   $"[ObjectPool] '{key}' has no component of type {typeof(T).Name}.");
    }

    public T Get<T>(string key, Vector3 position, Quaternion rotation) where T : Component
    {
        var obj = Get(key, position, rotation);
        return obj.GetComponent<T>()
               ?? throw new InvalidOperationException(
                   $"[ObjectPool] '{key}' has no component of type {typeof(T).Name}.");
    }

    public void Return(GameObject obj)
    {
        var key = obj.name.Replace("(Clone)", "").Trim();

        if (!_available.ContainsKey(key))
        {
            Debug.LogWarning($"[ObjectPool] Returning unknown key '{key}', destroying.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _available[key].Enqueue(obj);
    }

    private GameObject CreateNew(string key)
    {
        var obj = Instantiate(_prefabs[key], transform);
        obj.GetComponent<IPoolObject>()?.InitPoolObject(this);
        obj.name = key;
        obj.SetActive(false);
        return obj;
    }


    [Serializable]
    public sealed class PoolEntry
    {

        public string Key;


        public GameObject Prefab;
        [Range(0, 100)]
        public int PrewarmCount = 10;
    }
}
