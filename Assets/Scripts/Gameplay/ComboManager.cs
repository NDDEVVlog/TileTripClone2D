using System.Collections.Generic;
using System.Threading;
using TMPro;
using DG.Tweening;
using UnityEngine;
using Cysharp.Threading.Tasks;


public class ComboManager : MonoBehaviour
{   
    public TextMeshProUGUI ComboText;
    public List<string> ComboListText;

    [SerializeField] private int ComboCount = 0;

    private int _comboThreshold = 0;
    private LinkedList<string> _comboQueue = new();
    private Dictionary<string, LinkedListNode<string>> _lookup = new();

    // Enqueue
    public void Enqueue(string id)
    {
        if (_lookup.ContainsKey(id))
            return;
        if (_comboQueue.Count >= GameConstants.COMBO_MAX_SIZE)
        {
            Dequeue();
        }
            
        var node = _comboQueue.AddLast(id);
        _lookup[id] = node;
    }

    // Dequeue
    public string Dequeue()
    {
        if (_comboQueue.Count == 0)
            return null;

        var first = _comboQueue.First;

        _comboQueue.RemoveFirst();
        _lookup.Remove(first.Value);

        return first.Value;
    }

    public bool Remove(string id)
    {
        if (!_lookup.TryGetValue(id, out var node))
            return false;

        _comboQueue.Remove(node);
        _lookup.Remove(id);

        return true;
    }


    public bool Contains(string id)
    {
        return _lookup.ContainsKey(id);
    }

    public void HandleTap(string targetId)
    {
        if (!Contains(targetId))
        {   
            if(_comboThreshold > GameConstants.COMBO_MAX_SIZE)
            {
                ComboCount = 0;
                _comboThreshold = 0;
            }

            Enqueue(targetId);
            _comboThreshold++;
        }
    }

    public void HandleMatch()
    {
        ComboCount++;

        string text = ComboListText[Mathf.Min(ComboCount, ComboListText.Count) - 1];

        if (ComboCount > ComboListText.Count)
        {
            text += $" x{ComboCount}";
        }

        ComboText.text = text;


        ComboText.DOKill();

        Color color = ComboText.color;
        color.a = 0;
        ComboText.color = color;

        Sequence seq = DOTween.Sequence();

        seq.Append(ComboText.DOFade(1f, 0.15f)) // Fade in
        .AppendInterval(0.5f)               // Stay visible
        .Append(ComboText.DOFade(0f, 0.3f)); // Fade out
    }
}
