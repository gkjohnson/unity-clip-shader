using UnityEngine;

public class ClippedRenderer : MonoBehaviour {

    static Mesh _clipSurface;
    static Material _clipSurfaceMat;
    static MaterialPropertyBlock _matPropBlock;

    Renderer _renderer { get { return GetComponent<Renderer>(); } }
    Vector4 _planeVector = Vector4.zero;
    Vector4 planeVector
    {
        get { return _planeVector; }
        set {
            if (planeVector == value) return;

            _renderer.sharedMaterial.SetVector("_PlaneVector", planeVector);
            _planeVector = value;
        }
    }

    [SerializeField]
    Material _targetMaterial;
    void Start () {

        if (_clipSurface == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _clipSurface = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);

            _matPropBlock = new MaterialPropertyBlock();

            // TODO : Create material

        }
        // if (_targetMaterial == null) _targetMaterial = _clipSurface.sharedMaterial;
	}

    void OnRenderObject()
    {
        // TODO: Draw the clip surface plane
        var t = transform.position;
        var lookvec = new Vector3(_planeVector.x, _planeVector.y, _planeVector.z);
        var r = Quaternion.LookRotation(lookvec);
        var bounds = _renderer.bounds;
        var max = Mathf.Max(bounds.max.x, bounds.max.y, bounds.max.z);
        var s = Vector3.one * max;
        
        _matPropBlock.SetColor("_Color", _renderer.sharedMaterial.color);
        Graphics.DrawMesh(_clipSurface, Matrix4x4.TRS(t, r, s), _clipSurfaceMat, 0, Camera.current, 0, _matPropBlock);
    }
}
