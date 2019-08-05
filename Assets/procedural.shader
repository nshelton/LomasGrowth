Shader "PavelKouril/Marching Cubes/Procedural Geometry"
{
	SubShader
	{
		Cull Off
//		 Blend One One
	  //ztest Off

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			 #pragma multi_compile_fog    
			#include "UnityCG.cginc"

			struct Vertex
			{
				float3 vPosition;
				float3 vNormal;
				float food;
				float curvature;
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
				     UNITY_FOG_COORDS(1)
			};

			float random(float2 p){return frac(cos(dot(p,float2(23.14069263277926,2.665144142690225)))*12345.6789);}

			v2f vert(uint id : SV_VertexID)
			{
				uint pid = id / 3;
				uint vid = id % 3;

				v2f o;
				o.vertex = UnityObjectToClipPos(mul(model, float4(triangles[pid].v[vid].vPosition, 1)));
				o.normal = mul(unity_ObjectToWorld, triangles[pid].v[vid].vNormal);
				o.color = float3(vid == 0 ? 0 : 1,vid == 1 ? 0 : 1, vid == 2 ? 0 : 1 );

				o.normal.x = triangles[pid].v[vid].food;
				 UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float d = abs(dot(normalize(float3(1,1,1)), i.normal));
				// return   float4(d, d, d, 1);

				float3 c = (float3) 0.0;

				float thresh = 0.98;
			
				float dist = max(max(i.color.x, i.color.y), i.color.z);

				c.g = smoothstep(0.9, 1, dist); 
				c.b = 0.01 * i.normal.x * c.g;

				UNITY_APPLY_FOG(i.fogCoord, c);

			    return float4(c, 1);
			}
			ENDCG
		}
	}
}