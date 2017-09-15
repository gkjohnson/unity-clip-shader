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
        
        Camera.onPreCull -= Draw;
        Camera.onPreCull += Draw;
    }

    private void OnDisable()
    {
        Camera.onPreCull -= Draw;
    }

    void Draw(Camera c)
    {
        _matPropBlock.SetColor("_Color", material.color);
        _matPropBlock.SetFloat("_UseWorldSpace", useWorldSpace ? 1 : 0);
        Graphics.DrawMesh(_meshFilter.sharedMesh, transform.localToWorldMatrix, material, 0, c, 0, _matPropBlock);

        Vector4 _planeVector = material.GetVector("_PlaneVector");

        Vector3 norm = new Vector3(_planeVector.x, _planeVector.y, _planeVector.z).normalized;
        float dist = _planeVector.w;

        // TODO : This should be done in the shader so we
        // can operate in the "clip space" and not share
        // a normal across many materials
        // Or use "DrawMeshNow"
        if (!useWorldSpace)
        {
            norm = transform.localToWorldMatrix * norm;
            dist *= norm.magnitude;
        }

        var t = transform;
        var p = t.position + norm.normalized * dist - Vector3.Project(t.position, norm.normalized);
        var lookvec = -new Vector3(norm.x, norm.y, norm.z);
        var r = Quaternion.LookRotation(lookvec);
        var bounds = _meshFilter.sharedMesh.bounds; // _renderer.bounds;
        var max = Mathf.Max(bounds.max.x * t.localScale.x, bounds.max.y * t.localScale.y, bounds.max.z * t.localScale.z) * 4;
        var s = Vector3.one * max;
        
        Graphics.DrawMesh(_clipSurface, Matrix4x4.TRS(p, r, s), _clipSurfaceMat, 0, c, 0, _matPropBlock);
    }
}
