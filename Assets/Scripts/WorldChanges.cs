using System;
using System.Collections.Generic;
using UnityEngine;

/** Stores changes that have been made to the world since generation */
public class WorldChanges : MonoBehaviour {
    private Dictionary<Vector3Int, BlockType> _diffs = new Dictionary<Vector3Int, BlockType>();

    public void Add(Vector3Int gridPos, BlockType type) {
        _diffs[gridPos] = type;
        Debug.Log(String.Format("Added diff: {0}: {1}", gridPos, type));
    }

    public bool TryGetValue(in Vector3Int pos, out BlockType type) {
        var found = _diffs.TryGetValue(pos, out type);
        if (found)
            Debug.Log(String.Format("Restored diff: {0}: {1}", pos, type));
        return found;
    }
}