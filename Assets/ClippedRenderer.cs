using UnityEngine;

[ExecuteInEditMode]
public class ClippedRenderer : MonoBehaviour {

    const string CLIP_SURFACE_SHADER = "Hidden/Clip Plane/Surface";
    static Mesh _clipSurface;
    static Material _clipSurfaceMat;
    static MaterialPropertyBlock _matPropBlock;

    public bool useWorldSpace = false;

    public Material material = null;
    MeshFilter _meshFilter { get { return GetComponent<MeshFilter>(); } }

    void OnEnable()
    {
        if (_matPropBlock == null)
            _matPropBlock = new MaterialPropertyBlock();
        
        if (_clipSurface == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _clipSurface = go.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(go);
        }

        if (_clipSurfaceMat == null)
            _clipSurfaceMat = new Material(Shader.Find(CLIP_SURFACE_SHADER)); 
    }

    private void LateUpdate()
    {
        Draw(null);
        Draw(Camera.main);
    }
    void Draw(Camera c)
    {
        // Set shader attributes
        _matPropBlock.SetColor("_Color", material.color);
        _matPropBlock.SetFloat("_UseWorldSpace", useWorldSpace ? 1 : 0);
        Graphics.DrawMesh(_meshFilter.sharedMesh, transform.localToWorldMatrix, material, 0, c, 0, _matPropBlock);

        // Get the plane data from the material
        Vector4 planeVector = material.GetVector("_PlaneVector");
        Vector3 norm = new Vector3(planeVector.x, planeVector.y, planeVector.z).normalized;
        float dist = planeVector.w;

        // Position the clip surface
        var t = transform;
        var p = t.position + norm.normalized * dist - Vector3.Project(t.position, norm.normalized);
        var r = Quaternion.LookRotation(-new Vector3(norm.x, norm.y, norm.z));
        
        if (!useWorldSpace)
        {
            norm = transform.localToWorldMatrix * norm;
            dist *= norm.magnitude;

            r = Quaternion.LookRotation(-new Vector3(norm.x, norm.y, norm.z));
            p = t.position + norm.normalized * dist;
        }
        
        var bounds = _meshFilter.sharedMesh.bounds; // _renderer.bounds;
        var max = Mathf.Max(bounds.max.x * t.localScale.x, bounds.max.y * t.localScale.y, bounds.max.z * t.localScale.z) * 4;
        var s = Vector3.one * max;
        
        Graphics.DrawMesh(_clipSurface, Matrix4x4.TRS(p, r, s), _clipSurfaceMat, 0, c, 0, _matPropBlock);
    }
}
