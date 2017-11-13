﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System;

[RequireComponent(typeof(Renderer))]
[ExecuteInEditMode]
public class ClippedRenderer : MonoBehaviour, ISerializationCallbackReceiver {

    // Static variables
    const string CLIP_SURFACE_SHADER = "Hidden/Clip Plane/Surface";
    const string USE_WORLD_SPACE_PROPERTY = "_UseWorldSpace";
    const string PLANE_VECTOR_PROPERTY = "_PlaneVector";
    static Mesh _clipSurface;
    static Material _clipSurfaceMat;

    // Private variables
    [SerializeField]
    bool _shareMaterialProperties = true;

    // For Drawing
    MaterialPropertyBlock _matPropBlock;
    CommandBuffer _commandBuffer;

    // Getters
    Mesh clipSurface {
        get {
            if (_clipSurface == null) {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _clipSurface = go.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(go);
            }
            return _clipSurface;
        }
    }

    MaterialPropertyBlock matPropBlock {
        get { return _matPropBlock = _matPropBlock == null ? new MaterialPropertyBlock() : _matPropBlock; }
    }

    MeshRenderer renderer {
        get { return GetComponent<MeshRenderer>(); }
    }

    public Material material {
        get { return renderer.sharedMaterial; }
        set { renderer.sharedMaterial = value; }
    }

    public bool shareMaterialProperties {
        get { return _shareMaterialProperties; } 
        set {
            Vector4 pv = GetPlaneVector();
            bool uws = GetUseWorldSpace();

            _shareMaterialProperties = value;
            
            matPropBlock.Clear();

            planeVector = pv;
            useWorldSpace = uws;
        }
    }

    public bool useWorldSpace {
        get { return GetUseWorldSpace(); }
        set { SetUseWorldSpace(value); }
    }

    public Vector3 planeNormal {
        get {
            Vector4 pv = GetPlaneVector();
            return new Vector3(pv.x, pv.y, pv.z);
        }
        set {
            Vector3 pp = planePoint;
            SetPlaneVector(normal: value);
            planePoint = pp;
        }
    }

    public Vector3 planePoint {
        get { return planeNormal * planeVector.w; }
        set { SetPlaneVector(dist: Vector3.Dot(planeNormal, value)); }
    }

    public Vector4 planeVector {
        get { return GetPlaneVector(); }
        set { SetPlaneVector(new Vector3(value.x, value.y, value.z), value.w); }
    }

    Mesh mesh {
        get {
            var mf = GetComponent<MeshFilter>();
            if (mf) return mf.sharedMesh;
            else return null;
        }
    }

    #region Life Cycle

    void OnEnable() {
        ApplySerializedValues();

        if (_clipSurfaceMat == null) _clipSurfaceMat = new Material(Shader.Find(CLIP_SURFACE_SHADER));
        if (_commandBuffer == null) _commandBuffer = new CommandBuffer();

    }

    // clear shadows on disable
    void OnRenderObject() {
        UpdateCommandBuffers();
        
#if UNITY_EDITOR
        if (Camera.current.name != "Preview Scene Camera") Draw();
#else
        Draw()
#endif
    }
    #endregion

    #region Material Helpers
    // Helpers for setting the material properties depending
    // on whether or not we're using the material property block or not
    void SetPlaneVector(Vector3? normal = null, float? dist = null) {
        Vector4 currVec = GetPlaneVector();
        normal = (normal ?? new Vector3(currVec.x, currVec.y, currVec.z)).normalized;
        dist = dist ?? currVec.w;

        renderer.GetPropertyBlock(matPropBlock);

        Vector4 newVec = new Vector4(normal.Value.x, normal.Value.y, normal.Value.z, dist.Value);
        if (_shareMaterialProperties) material.SetVector(PLANE_VECTOR_PROPERTY, newVec);
        else matPropBlock.SetVector(PLANE_VECTOR_PROPERTY, newVec);

        renderer.SetPropertyBlock(matPropBlock);
    }

    Vector4 GetPlaneVector() {
        return _shareMaterialProperties ? material.GetVector(PLANE_VECTOR_PROPERTY) : matPropBlock.GetVector(PLANE_VECTOR_PROPERTY);
    }
    
    void SetUseWorldSpace(bool ws) {
        renderer.GetPropertyBlock(matPropBlock);

        if (_shareMaterialProperties) material.SetFloat(USE_WORLD_SPACE_PROPERTY, ws ? 1 : 0);
        else matPropBlock.SetFloat(USE_WORLD_SPACE_PROPERTY, ws ? 1 : 0);

        renderer.SetPropertyBlock(matPropBlock);
    }

    bool GetUseWorldSpace() {
        return (_shareMaterialProperties ? material.GetFloat(USE_WORLD_SPACE_PROPERTY) : matPropBlock.GetFloat(USE_WORLD_SPACE_PROPERTY)) == 1;
    }
    #endregion

    #region Visuals
    // Update the command buffers if something has changed
    void UpdateCommandBuffers() {
        // Update Main CommandBuffer
        _commandBuffer.Clear();

        // Set shader attributes
        matPropBlock.SetColor("_Color", material.color);
        _commandBuffer.DrawRenderer(renderer, material, 0, 0);

        Vector3 norm = planeNormal;
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
        _commandBuffer.DrawMesh(clipSurface, Matrix4x4.TRS(p, r, s), _clipSurfaceMat, 0, 0, matPropBlock);
    }

    // Drwa the current target
    void Draw() {
        if (mesh == null) return;
        Graphics.ExecuteCommandBuffer(_commandBuffer);
    }

    // Visualize the clip plane normal and position
    void OnDrawGizmosSelected() {
        if (mesh == null) return;
        
        Vector3 norm = planeNormal;
        Vector3 point = planePoint;

        if (useWorldSpace) {
            // adjust the plane position so it's centered around
            // the mesh in world space
            float projDist = Vector3.Dot(norm, transform.position);
            Vector3 delta = transform.position - norm * projDist;
            point += delta;
            
            Gizmos.matrix = Matrix4x4.TRS(point, Quaternion.LookRotation(new Vector3(norm.x, norm.y, norm.z)), Vector3.one);
        } else {
            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(point, Quaternion.LookRotation(new Vector3(norm.x, norm.y, norm.z)), Vector3.one);
        }

        float planeSize = mesh.bounds.extents.magnitude * 2;

        // Draw box plane and normal
        Color c = Gizmos.color;
        c.a = 0.25f;
        Gizmos.color = c;
        Gizmos.DrawCube(Vector3.forward * 0.0001f, new Vector3(1, 1, 0) * planeSize);

        c.a = 1;
        Gizmos.color = c;
        Gizmos.DrawRay(Vector3.zero, Vector3.forward);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 1, 0) * planeSize);
    }
    #endregion

    #region Serialization
    [SerializeField] Vector4 serializable_planeVector;
    [SerializeField] bool serializable_useWorldSpace;
    void ISerializationCallbackReceiver.OnBeforeSerialize() {
        serializable_planeVector = planeVector;
        serializable_useWorldSpace = useWorldSpace;
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize() {
        // We can't create a materialPropertyBlock here, so set the values in "OnEnable" instead
    }
    
    void ApplySerializedValues() {
        if (!_shareMaterialProperties)
        {
            planeVector = serializable_planeVector;
            useWorldSpace = serializable_useWorldSpace;
        }
    }
    #endregion
}
