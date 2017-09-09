using UnityEngine;

public class ClippedRenderer : MonoBehaviour {

    static Renderer _clipSurface;
    static Material _clipSurfaceMat;

    Vector4 _planeVector = Vector4.zero;
    Vector4 planeVector
    {
        get { return _planeVector; }
        set {
            if (planeVector == value) return;
            _planeVector = value;
        }
    }

    [SerializeField]
    Material _targetMaterial;
    void Start () {
        _clipSurface = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Renderer>();
        _clipSurface.enabled = false;
        Destroy(_clipSurface.GetComponent<Collider>());

        if (_targetMaterial == null) _targetMaterial = _clipSurface.sharedMaterial;
	}

    void OnRenderObject()
    {
        // TODO: Draw the clip surface plane
    }
}
