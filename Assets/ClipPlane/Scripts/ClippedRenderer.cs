using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class ClippedRenderer : MonoBehaviour {

    // Static variables
    const string CLIP_SURFACE_SHADER = "Hidden/Clip Plane/Surface";
    static Mesh _clipSurface;
    static Material _clipSurfaceMat;

    // Getters
    public Vector3 planeNormal {
        get { return new Vector3(planeVector.x, planeVector.y, planeVector.z); }
        set {
            Vector3 pp = planePoint;
            planeVector.x = value.x;
            planeVector.y = value.y;
            planeVector.z = value.z;
            planePoint = pp;
        }
    }

    public Vector3 planePoint {
        get { return planeNormal * planeVector.w; }
        set { planeVector.w = Vector3.Dot(planeNormal, value); }
    }

    // For Drawing
    public bool useWorldSpace = false;          // whether or not the clip plane variable should be used in world space or local space

    // TODO: the API should enforce that this is normalized
    public Vector4 planeVector = Vector4.zero;  // xyz is the normal, w is the distance from 0,0,0
    public Material material = null;            // the material to render with
    MaterialPropertyBlock _matPropBlock;
    CommandBuffer _commandBuffer;
    CommandBuffer _lightingCommandBuffer;
    Mesh mesh {
        get {
            var mf = GetComponent<MeshFilter>();
            if (mf) return mf.sharedMesh;
            else return null;
        }
    }

    // For Shadows
    public Light[] shadowCastingLights;     // the lights to render shadows to
    public bool castShadows = true;         // whether or not this object should render shadows
    List<Light> _prevLights = new List<Light>();

    #region Life Cycle
    void OnEnable()
    {
        if (_clipSurfaceMat == null) _clipSurfaceMat = new Material(Shader.Find(CLIP_SURFACE_SHADER));

        if (_matPropBlock == null) _matPropBlock = new MaterialPropertyBlock();

        if (_commandBuffer == null) _commandBuffer = new CommandBuffer();

        if (_lightingCommandBuffer == null) _lightingCommandBuffer = new CommandBuffer();

        if (_clipSurface == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _clipSurface = go.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(go);
        }
    }

    // Here we update every command buffer every frame to update the shadows
    // for lighting
    // TODO: This shouldn't happen every frame. Maybe it only needs to happen once at the beginning with
    // a public function to force an update? Or it updates every frame in editor while not playing?
    void LateUpdate()
    {
        // clear all the previous shadow draw bufers
        for (int i = 0; i < _prevLights.Count; i++) _prevLights[i].RemoveCommandBuffer(LightEvent.AfterShadowMapPass, _lightingCommandBuffer);
        _prevLights.Clear();
        
        // If we're going to cash shadows
        if (castShadows && shadowCastingLights != null)
        {
            // regenerate the command buffer
            _matPropBlock.SetColor("_Color", material.color);
            _matPropBlock.SetFloat("_UseWorldSpace", useWorldSpace ? 1 : 0);
            _matPropBlock.SetVector("_PlaneVector", planeVector);

            _lightingCommandBuffer.Clear();
            _lightingCommandBuffer.DrawMesh(mesh, transform.localToWorldMatrix, material, 0, 0, _matPropBlock);

            // add the shadow drawing
            for (int i = 0; i < shadowCastingLights.Length; i++)
            {
                shadowCastingLights[i].AddCommandBuffer(LightEvent.AfterShadowMapPass, _lightingCommandBuffer);
                _prevLights.Add(shadowCastingLights[i]);
            }
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
#endregion

    void Draw()
    {
        if (mesh == null) return;

        // TODO: We should only have to clear the commandbuffer
        // if something breaking has changed! Same with the shadow commandbuffer
        _commandBuffer.Clear();

        // Set shader attributes
        _matPropBlock.SetColor("_Color", material.color);
        _matPropBlock.SetFloat("_UseWorldSpace", useWorldSpace ? 1 : 0);
        _matPropBlock.SetVector("_PlaneVector", planeVector);
        _commandBuffer.DrawMesh(mesh, transform.localToWorldMatrix, material, 0, 0, _matPropBlock);

        // Create the clip plane position here because it may have moved between lateUpdate and now
        // TODO: We could cache it between draws, though, and only update it if the vector has changed
        // Get the plane data from the material
        Vector3 norm = planeNormal.normalized;
        float dist = planeVector.w;

        // Position the clip surface
        var t = transform;
        var p = t.position + norm * dist - Vector3.Project(t.position, norm);
        var r = Quaternion.LookRotation(-new Vector3(norm.x, norm.y, norm.z));
        
        if (!useWorldSpace)
        {
            norm = transform.localToWorldMatrix * norm;
            dist *= norm.magnitude;

            r = Quaternion.LookRotation(-new Vector3(norm.x, norm.y, norm.z));
            p = t.position + norm.normalized * dist;
        }
        
        var bounds = mesh.bounds;
        var max = Mathf.Max(bounds.max.x * t.localScale.x, bounds.max.y * t.localScale.y, bounds.max.z * t.localScale.z) * 4;
        var s = Vector3.one * max;
        _commandBuffer.DrawMesh(_clipSurface, Matrix4x4.TRS(p, r, s), _clipSurfaceMat, 0, 0, _matPropBlock);
        
        Graphics.ExecuteCommandBuffer(_commandBuffer);
    }

    void OnDrawGizmosSelected()
    {
        if (mesh == null) return;

        Vector3 norm = planeNormal;
        Vector3 point = planePoint;

        if (!useWorldSpace)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
        }

        float planeSize = mesh.bounds.extents.magnitude * 2;

        Color c = Gizmos.color;
        c.a = 0.25f;
        Gizmos.color = c;
        Gizmos.DrawCube(point + planeNormal * 0.0001f, new Vector3(1, 0, 1) * planeSize);

        c.a = 1;
        Gizmos.color = c;
        Gizmos.DrawRay(point, norm);
        Gizmos.DrawWireCube(point, new Vector3(1, 0, 1) * planeSize);


    }
}
