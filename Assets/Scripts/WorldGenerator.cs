using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGenerator : MonoBehaviour {
    public int viewRange;
    public int generationDepth = 1;
    public int sectorSize;
    public List<NoiseMap> noiseMaps;
    public GameObject blockTemplate;
    
    private List<Transform> _freeBlocksDeactivated = new List<Transform>();
    private List<Transform> _freeBlocksUncommited = new List<Transform>();
    private Dictionary<Vector2Int, Sector> _sectors = new Dictionary<Vector2Int, Sector>();
    private static WorldGenerator _instance;
    
    public static WorldGenerator Instance {
        get { return _instance; }
    }

    private void Awake() {
        _instance = this;
        GenerateInitialMap();
    }

    private void GenerateInitialMap() {
        for (var x = -viewRange; x <= viewRange; x++) {
            for (var y = -viewRange; y <= viewRange; y++) {
                var sector = new Sector(new Vector2Int(x, y));
                GenerateSector(sector);
                _sectors.Add(sector.offset, sector);
            }
        }
    }

    private void GenerateSector(Sector sector) {
        var sectorBase = sector.offset * sectorSize;
        for (var x = -sectorSize; x <= sectorSize; x++) {
            for (var y = -sectorSize; y <= sectorSize; y++) {
                var worldCoord = sectorBase + new Vector2Int(x, y);
                int groundZ = (int)SampleMaps(worldCoord);
                for (var z = groundZ - generationDepth + 1; z <= groundZ; z++) {
                    sector.blocks.Add(new Vector3Int(worldCoord.x, worldCoord.y, z), BlockType.Default);
                    var worldPos = new Vector3(worldCoord.x, z, worldCoord.y);
                    var go = CreateGO(worldPos);
                    sector.GameObjects.Add(go);
                }
            }
        }
    }

    private Transform CreateGO(Vector3 worldPos) {
        if (_freeBlocksUncommited.Count > 0) {
            var go = _freeBlocksUncommited[_freeBlocksUncommited.Count - 1];
            _freeBlocksUncommited.RemoveAt(_freeBlocksUncommited.Count - 1);
            go.position = worldPos;
            return go;
        }
        else if (_freeBlocksDeactivated.Count > 0) {
            var go = _freeBlocksDeactivated[_freeBlocksDeactivated.Count-1];
            _freeBlocksDeactivated.RemoveAt(_freeBlocksDeactivated.Count-1);
            go.gameObject.SetActive(true);
            go.position = worldPos;
            return go;
        }
        else
            return Instantiate(blockTemplate, worldPos, Quaternion.identity, transform).transform;
    }

    private void UnloadSector(Sector sector) {
        _freeBlocksUncommited.AddRange(sector.GameObjects);
        sector.blocks.Clear();
        sector.GameObjects.Clear();
    }

    private float SampleMaps(Vector2Int pos) {
        var res = 0.0f;
        for (var i = 0; i < noiseMaps.Count; i++)
            res += noiseMaps[i].Sample(pos);
        return res;
    }

    public void OnPlayerMoved(Vector2Int oldPos, Vector2Int newPos) {
        //Debug.Log(String.Format("Player moved from sector {0} to {1}", oldPos, newPos));
        var delta = newPos - oldPos;
        if (Mathf.Abs(delta.x) > 0) {
            var removeX = oldPos.x - (int) Mathf.Sign(delta.x) * viewRange;
            var addX = newPos.x + (int) Mathf.Sign(delta.x) * viewRange;
            for (var y = oldPos.y - viewRange; y <= oldPos.y + viewRange; y++) {
                var removePos = new Vector2Int(removeX, y);
                //Debug.Log("Removing sector " + removePos);
                var sector = _sectors[removePos];
                UnloadSector(sector);
                _sectors.Remove(removePos);
                
                var addPos = new Vector2Int(addX, y);
                sector.offset = addPos;
                //Debug.Log("Adding sector " + addPos);
                GenerateSector(sector);
                _sectors.Add(addPos, sector);
            }
        }
        if (Mathf.Abs(delta.y) > 0) {
            var removeY = oldPos.y - (int) Mathf.Sign(delta.y) * viewRange;
            var addY = newPos.y + (int) Mathf.Sign(delta.y) * viewRange;
            for (var x = oldPos.x - viewRange; x <= oldPos.x + viewRange; x++) {
                var removePos = new Vector2Int(x, removeY);
                //Debug.Log("Removing sector " + removePos);
                var sector = _sectors[removePos];
                UnloadSector(sector);
                _sectors.Remove(removePos);
                
                var addPos = new Vector2Int(x, addY);
                sector.offset = addPos;
                //Debug.Log("Adding sector " + addPos);
                GenerateSector(sector);
                _sectors.Add(addPos, sector);
            }
        }
        CommitBlockChanges();
    }

    private void CommitBlockChanges() {
        foreach (var go in _freeBlocksUncommited)
            go.gameObject.SetActive(false);
        _freeBlocksDeactivated.AddRange(_freeBlocksUncommited);
        _freeBlocksUncommited.Clear();
    }
}