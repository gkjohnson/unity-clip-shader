using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Rendering;
#endif

[ExecuteInEditMode]
public class ClippedRenderer : MonoBehaviour {

    const string CLIP_SURFACE_SHADER = "Hidden/Clip Plane/Surface";
    static Mesh _clipSurface;
    static Material _clipSurfaceMat;

    public bool useWorldSpace = false;
    public Material material = null;
    MaterialPropertyBlock _matPropBlock;
    CommandBuffer _commandBuffer;
    MeshFilter _meshFilter { get { return GetComponent<MeshFilter>(); } }

    #region Life Cycle
    void OnEnable()
    {
        if (_clipSurfaceMat == null) _clipSurfaceMat = new Material(Shader.Find(CLIP_SURFACE_SHADER));

        if (_matPropBlock == null) _matPropBlock = new MaterialPropertyBlock();

        if (_commandBuffer == null) _commandBuffer = new CommandBuffer();

        if (_clipSurface == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _clipSurface = go.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(go);
        }
    }

    private void OnRenderObject()
    {
#if UNITY_EDITOR
        if (Camera.current.name != "Preview Scene Camera") Draw();
#else
        Draw()
#endif
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
        Destroy(material);
#endif
    }
#endregion

    void Draw()
    {
        _commandBuffer.Clear();

        // Set shader attributes
        _matPropBlock.SetColor("_Color", material.color);
        _matPropBlock.SetFloat("_UseWorldSpace", useWorldSpace ? 1 : 0);
        _commandBuffer.DrawMesh(_meshFilter.sharedMesh, transform.localToWorldMatrix, material, 0, 0, _matPropBlock);

        // Create the clip plane position here because it may have moved between lateUpdate and now
        // TODO: We could cache it between draws, though, and only update it if the vector has changed
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
        
        var bounds = _meshFilter.sharedMesh.bounds;
        var max = Mathf.Max(bounds.max.x * t.localScale.x, bounds.max.y * t.localScale.y, bounds.max.z * t.localScale.z) * 4;
        var s = Vector3.one * max;
        _commandBuffer.DrawMesh(_clipSurface, Matrix4x4.TRS(p, r, s), _clipSurfaceMat, 0, 0, _matPropBlock);
        
        Graphics.ExecuteCommandBuffer(_commandBuffer);
    }
}
