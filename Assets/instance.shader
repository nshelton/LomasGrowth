


  Shader "Custom/Instance"
{
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _ScaleFactor ("ScaleFactor", float) = 0.001
        _minScale ("_minScale", float) = 0.001
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

		struct Particle
		{
			int links[16];
			int numLinks;
			int age;
			float food;
			float curvature;
			float3 position;
			float3 delta;
			float3 normal;
		};

        sampler2D _MainTex;
		float _ScaleFactor;
		float _minScale;
		
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		uniform StructuredBuffer<Particle> particles;
#endif

        struct Input {
            float2 uv_MainTex;
        };

        void setup()
        {

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float3 pos = particles[unity_InstanceID].position;
			float s = (particles[unity_InstanceID].food * _ScaleFactor) + _minScale;

			unity_ObjectToWorld._11_21_31_41 = float4(s, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, s, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, s, 0);
            unity_ObjectToWorld._14_24_34_44 = float4(pos, 1);
			unity_WorldToObject = unity_ObjectToWorld;
            unity_WorldToObject._14_24_34 *= -1;
            unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
#endif

        }

        half _Glossiness;
        half _Metallic;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}