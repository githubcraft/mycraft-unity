using UnityEngine;

public class Builder : MonoBehaviour {
    public float maxDistance = 5f;
    public new Transform camera;
    public GameObject constructionBlock;
    
    private int _layerMask;

    private void Awake() {
        _layerMask = LayerMask.GetMask("World");
        var startPosition = WorldGenerator.Instance.GetHeightAt(new Vector2Int(0, 0));
        transform.position = startPosition + new Vector3(0, 10, 0);
    }

    void Update() {
        RaycastHit hit;
        var hasHit = Physics.Raycast(new Ray(transform.position, camera.forward), out hit, maxDistance, _layerMask);
        if (!hasHit)
            return;
        CheckConstruct(hit);
        CheckDestroy(hit);
        // TODO bug: when changing blocks on the edge the sweep only updates faces in one sector instead of both
    }

    private void CheckDestroy(RaycastHit hit) {
        var reboundPoint = hit.point + hit.normal * -0.5f;
        var target = new Vector3Int(
            Mathf.RoundToInt(reboundPoint.x),
            Mathf.RoundToInt(reboundPoint.y),
            Mathf.RoundToInt(reboundPoint.z)
        );
        if (!Input.GetKeyDown(KeyCode.Mouse1)) return;
        var sectorPos = new Vector2Int(
            Mathf.FloorToInt(target.x / (float) Sector.sectorSize),
            Mathf.FloorToInt(target.z / (float) Sector.sectorSize));
        var sector = WorldGenerator.Instance.GetSector(sectorPos);
        var gridPos = sector.WorldToInternalPos(target);
        // Debug.Log(String.Format("Building at ({0}): {1}", sectorPos, gridPos));
        sector.AddBlock(gridPos, BlockType.Empty);
        // TODO should only add new meshes instead of redrawing the whole sector
        sector.FinishGeneratingGrid();
        sector.GenerateMesh();
    }

    private void CheckConstruct(RaycastHit hit) {
        var richochet = camera.forward * (-0.1f);
        var reboundPoint = hit.point + richochet;
        var target = new Vector3Int(
            Mathf.RoundToInt(reboundPoint.x),
            Mathf.RoundToInt(reboundPoint.y),
            Mathf.RoundToInt(reboundPoint.z)
        );
        constructionBlock.transform.position = target;

        if (!Input.GetKeyDown(KeyCode.Mouse0)) return;
        var sectorPos = new Vector2Int(
            Mathf.FloorToInt(target.x / (float) Sector.sectorSize),
            Mathf.FloorToInt(target.z / (float) Sector.sectorSize));
        var sector = WorldGenerator.Instance.GetSector(sectorPos);
        var gridPos = sector.WorldToInternalPos(target);
        // Debug.Log(String.Format("Building at ({0}): {1}", sectorPos, gridPos));
        sector.AddBlock(gridPos, BlockType.Grass);
        // TODO should only add new meshes instead of redrawing the whole sector
        sector.FinishGeneratingGrid();
        sector.GenerateMesh();
    }
}
