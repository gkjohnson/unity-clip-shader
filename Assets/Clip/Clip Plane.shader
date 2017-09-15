Shader "Clip Plane/Basic"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Main Texture", 2D) = "white" {}

        [MaterialToggle] _UseWorldSpace("Use World Space", Float) = 0
        _PlaneVector("Plane Vector", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Cull Off

            Stencil
            {
                Comp Always
                PassFront IncrWrap
                FailFront IncrWrap

                PassBack DecrWrap
                FailBack DecrWrap

                ZFailFront IncrWrap
                ZFailBack DecrWrap
            }

            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            uniform fixed4 _LightColor0;

            float4 _Color;
            float4 _MainTex_ST;			// For the Main Tex UV transform
            sampler2D _MainTex;			// Texture used for the line
            float4 _PlaneVector;
            float _UseWorldSpace;

            struct v2f
            {
                float4 pos		: POSITION;
                float4 col      : COLOR;
                float2 uv		: TEXCOORD0;
                float doclip    : TEXCOORD1;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                // Calculate clip value
                float4 pnorm = float4(normalize(_PlaneVector.xyz), 0);
                float dist = _PlaneVector.w;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Use World Space Lerps
                pnorm = lerp(mul(unity_ObjectToWorld, pnorm), pnorm, _UseWorldSpace);
                dist = lerp(dist * length(pnorm), dist, _UseWorldSpace);
                wp -= lerp(mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz, float3(0, 0, 0), _UseWorldSpace);
                
                o.doclip = dist - dot(wp, normalize(pnorm.xyz));
                
                // Lighting
                float4 norm = mul(unity_ObjectToWorld, v.normal);
                float3 normalDirection = normalize(norm.xyz);
                float4 AmbientLight = UNITY_LIGHTMODEL_AMBIENT;
                float4 LightDirection = normalize(_WorldSpaceLightPos0);
                float4 DiffuseLight = saturate(dot(LightDirection, -normalDirection))*_LightColor0;
                o.col = float4(AmbientLight + DiffuseLight);

                return o;
            }

            float4 frag(v2f i) : COLOR
            {
                clip(i.doclip);

                float4 col = _Color * tex2D(_MainTex, i.uv);
                return col * i.col;
            }

            ENDCG
        }
    }
}
