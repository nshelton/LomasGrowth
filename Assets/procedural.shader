Shader "PavelKouril/Marching Cubes/Procedural Geometry"
{
	SubShader
	{
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct Vertex
			{
				float3 vPosition;
				float3 vNormal;
			};

			struct Triangle
			{
				Vertex v[3];
			};

			uniform StructuredBuffer<Triangle> triangles;
			uniform float4x4 model;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 color : COLOR;
			};

			float random(float2 p){return frac(cos(dot(p,float2(23.14069263277926,2.665144142690225)))*12345.6789);}

			v2f vert(uint id : SV_VertexID)
			{
				uint pid = id / 3;
				uint vid = id % 3;

				v2f o;
				o.vertex = UnityObjectToClipPos(mul(model, float4(triangles[pid].v[vid].vPosition, 1)));
				o.normal = mul(unity_ObjectToWorld, triangles[pid].v[vid].vNormal);
				o.color = float3(random(pid), random(pid+1), random(pid+2));
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float d = max(dot(normalize(_WorldSpaceLightPos0.xyz), i.normal), 0);
				return float4(d, d, d, 1);
				//return float4(i.color, 1);
			}
			ENDCG
		}
	}
}