using UnityEngine;

[ExecuteInEditMode]
public class ClippedRenderer : MonoBehaviour {

    const string CLIP_SURFACE_SHADER = "Clip Plane/Surface";
    static Mesh _clipSurface;
    static Material _clipSurfaceMat;
    static MaterialPropertyBlock _matPropBlock;

    public bool useWorldSpace = false;
    public Vector4 _planeVector = Vector4.zero;

    public Material material = null;
    MeshFilter _meshFilter { get { return GetComponent<MeshFilter>(); } }

    void Awake () {
    }

    void OnEnable()
    {
        if (_matPropBlock == null)
            _matPropBlock = new MaterialPropertyBlock();


        if (_clipSurface == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _clipSurface = go.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(go);
             
            // TODO : Create material
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

        Vector4 newPlane = new Vector4(norm.x, norm.y, norm.z, dist);

        material.SetVector("_PlaneVector", newPlane);
        Graphics.DrawMesh(_meshFilter.sharedMesh, transform.localToWorldMatrix, material, 0, c);

        var t = transform;
        var p = t.position + norm.normalized * dist;
        var lookvec = -new Vector3(newPlane.x, newPlane.y, newPlane.z);
        var r = Quaternion.LookRotation(lookvec);
        var bounds = _meshFilter.sharedMesh.bounds; // _renderer.bounds;
        var max = Mathf.Max(bounds.max.x * t.localScale.x, bounds.max.y * t.localScale.y, bounds.max.z * t.localScale.z) * 2;
        var s = Vector3.one * max;
        
        _matPropBlock.SetColor("_Color", material.color);
        Graphics.DrawMesh(_clipSurface, Matrix4x4.TRS(p, r, s), _clipSurfaceMat, 0, c, 0, _matPropBlock);
    }
}
