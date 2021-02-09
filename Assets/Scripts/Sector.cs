using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class Sector : MonoBehaviour, IEnumerable<Vector3Int> {
    public Vector2Int offset;
    
    private readonly int _meshCount = 2;
    
    private ResizableArray<Vector3> vertices;
    private ResizableArray<Vector2> uvs;
    private ResizableArray<int>[] triangles;
    private BlockType[] _blocks;
    
    private static int _xSize, _zSize;
    private static int sectorSizeHeight;
    private static int sectorSize;
    
    private const float _s = 0.5f;
    private static readonly Vector3 _rub = new Vector3(_s, _s, -_s);
    private static readonly Vector3 _lub = new Vector3(-_s, _s, -_s);
    private static readonly Vector3 _luf = new Vector3(-_s, _s, _s);
    private static readonly Vector3 _ruf = new Vector3(_s, _s, _s);
    private static readonly Vector3 _rdb = new Vector3(_s, -_s, -_s);
    private static readonly Vector3 _ldb = new Vector3(-_s, -_s, -_s);
    private static readonly Vector3 _ldf = new Vector3(-_s, -_s, _s);
    private static readonly Vector3 _rdf = new Vector3(_s, -_s, _s);

    private enum Direction {
        UP, DOWN, RIGHT, LEFT, FORWARD, BACK
    }

    public static void SetSizes(int sectorSize, int sectorSizeHeight) {
        Sector.sectorSize = sectorSize;
        Sector.sectorSizeHeight = _zSize = sectorSizeHeight;
        _xSize = sectorSizeHeight * sectorSize;
    }

    public void Init() {
        _blocks = new BlockType[sectorSize * sectorSize * sectorSizeHeight];
        var predictedVertices = _xSize * sectorSize / 2;
        vertices = new ResizableArray<Vector3>(predictedVertices);
        uvs = new ResizableArray<Vector2>(predictedVertices);
        triangles = new ResizableArray<int>[_meshCount];
        for (int i = 0; i < _meshCount; i++)
            triangles[i] = new ResizableArray<int>((int)(predictedVertices * 1.5f));
    }

    public void AddBlock(in Vector3Int pos, BlockType blockType) {
        _blocks[GetId(pos)] = blockType;
    }

    private int GetId(in Vector3Int pos) {
        return pos.x * _xSize + pos.z * _zSize + pos.y;
    }

    public IEnumerator<Vector3Int> GetEnumerator() {
        for (var x = 0; x < sectorSize; x++)
            for (var z = 0; z < sectorSize; z++)
                for (var y = 0; y < sectorSizeHeight; y++)
                    yield return new Vector3Int(x, y, z);
    }

    // TODO add new type for internal pos
    public Vector3Int InternalToWorldPos(Vector3Int pos) {
        pos.x += offset.x * sectorSize;
        pos.z += offset.y * sectorSize;
        pos.y -= sectorSizeHeight / 2;
        return pos;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    private BlockType GetBlock(in Vector3Int pos) {
        return _blocks[GetId(pos)];
    }

    public bool TryGetValue(Vector3Int pos, out BlockType value) {
        // TODO remove?
        value = BlockType.Empty;
        if (
            pos.x < 0 || pos.x >= sectorSize
            || pos.y < 0 || pos.y >= sectorSizeHeight
            || pos.z < 0 || pos.z >= sectorSize
        ) {
            //Debug.Log("Requested invalid pos " + pos);
            return false;
        }
        value = _blocks[GetId(pos)];
        return true;
    }

    public void FillMesh() {
        foreach (var triangle in triangles) {
            triangle.Clear();
        }
        vertices.Clear();
        uvs.Clear();
        
        SweepMeshFaces();

        var mesh = new Mesh();
        mesh.subMeshCount = 2;
        mesh.SetVertices(vertices.GetArrayRef(), 0, vertices.Count);
        for (int i = 0; i < triangles.Length; i++)
            mesh.SetTriangles(triangles[i].GetArrayRef(), 0, triangles[i].Count, i);
        mesh.SetUVs(0, uvs.GetArrayRef(), 0, uvs.Count);
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        // Debug.Log(String.Format("Generated sector {2} with {0} vertices, {1} triangles",
        //     vertices.Count, triangles.Count / 3, offset));
    }

    private void SweepMeshFaces() {
        BlockType? ConstructFace(in Vector3Int currentPos, BlockType? previousType, in Vector3Int lastPosition,
            Direction currentDirection, Direction previousDirection) {
            var currentType = GetBlock(currentPos);
            if (previousType != null) {
                var prevGroup = Block.Groups[previousType.Value];
                var currentGroup = Block.Groups[currentType];
                if (currentType == BlockType.Empty && previousType == BlockType.Water)
                    AddFace(lastPosition, currentDirection, previousType.Value, 1);
                if (currentGroup != prevGroup) {
                    if (currentGroup == 1)
                        AddFace(lastPosition, currentDirection, previousType.Value, 0);
                    else
                        AddFace(currentPos, previousDirection, currentType, 0);
                }
            }
            previousType = currentType;
            return previousType;
        }

        BlockType? lastType = null;
        Vector3Int lastPos = Vector3Int.zero;

        // Sweep up
        for (var x = 0; x < sectorSize; x++) {
            for (var z = 0; z < sectorSize; z++) {
                for (var y = 0; y < sectorSizeHeight; y++) {
                    var currentPos = new Vector3Int(x, y, z);
                    lastType = ConstructFace(currentPos, lastType, lastPos, Direction.UP, Direction.DOWN);
                    lastPos = currentPos;
                }
                lastType = null;
            }
        }

        // Sweep forward
        for (var x = 0; x < sectorSize; x++) {
            for (var y = 0; y < sectorSizeHeight; y++) {
                for (var z = 0; z < sectorSize; z++) {
                    var currentPos = new Vector3Int(x, y, z);
                    lastType = ConstructFace(currentPos, lastType, lastPos, Direction.FORWARD, Direction.BACK);
                    lastPos = currentPos;
                }
                lastType = null;
            }
        }

        // Sweep right
        for (var y = 0; y < sectorSizeHeight; y++) {
            for (var z = 0; z < sectorSize; z++) {
                for (var x = 0; x < sectorSize; x++) {
                    var currentPos = new Vector3Int(x, y, z);
                    lastType = ConstructFace(currentPos, lastType, lastPos, Direction.RIGHT, Direction.LEFT);
                    lastPos = currentPos;
                }
                lastType = null;
            }
        }
    }

    private void AddFace(in Vector3 center, Direction dir, BlockType type, int meshId) {
        var uvPos = (int)type;
        // TODO inline method?
        switch (dir) {
            case Direction.UP:
                AddFaceInternal(_rub, _lub, _luf, _ruf, center, 0, uvPos, meshId);
                break;
            case Direction.DOWN:
                AddFaceInternal(_rdb, _rdf, _ldf, _ldb, center, 2, uvPos, meshId);
                break;
            case Direction.RIGHT:
                AddFaceInternal(_rdb, _rub, _ruf, _rdf, center, 1, uvPos, meshId);
                break;
            case Direction.LEFT:
                AddFaceInternal(_ldf, _luf, _lub, _ldb, center, 1, uvPos, meshId);
                break;
            case Direction.FORWARD:
                AddFaceInternal(_rdf, _ruf, _luf, _ldf, center, 1, uvPos, meshId);
                break;
            case Direction.BACK:
                AddFaceInternal(_ldb, _lub, _rub, _rdb, center, 1, uvPos, meshId);
                break;
        }
    }

    private const int _uvMapSize = 4;
    private const float _uvDelta = 1f / _uvMapSize;
    private void AddFaceInternal(in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 d, in Vector3 center, int uvX,
        int uvY, int meshId) {
        var i = vertices.Count;
        vertices.Add(center + a);
        vertices.Add(center + b);
        vertices.Add(center + c);
        vertices.Add(center + d);
        
        uvs.Add(new Vector2(uvX * _uvDelta, uvY * _uvDelta));
        uvs.Add(new Vector2(uvX * _uvDelta, uvY * _uvDelta + _uvDelta));
        uvs.Add(new Vector2(uvX * _uvDelta + _uvDelta, uvY * _uvDelta + _uvDelta));
        uvs.Add(new Vector2(uvX * _uvDelta + _uvDelta, uvY * _uvDelta));
        
        triangles[meshId].Add(i);
        triangles[meshId].Add(i+1);
        triangles[meshId].Add(i+3);
        
        triangles[meshId].Add(i+1);
        triangles[meshId].Add(i+2);
        triangles[meshId].Add(i+3);
    }
}
