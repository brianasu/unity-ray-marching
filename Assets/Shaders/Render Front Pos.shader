Shader "Hidden/Ray Marching/Render Front Position" {

	CGINCLUDE
		#pragma exclude_renderers xbox360
		#include "UnityCG.cginc"

		struct v2f {
			float4 pos : POSITION;
			float3 localPos : TEXCOORD0;
		};
		
		float4 _VolumeScale;

		v2f vert(appdata_base v) 
		{
			v2f o;
			o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
			o.localPos = v.vertex.xyz + 0.5;
			return o;
		}
		
		half4 frag(v2f i) : COLOR 
		{  
			return float4(i.localPos, 1);
		}
		
	ENDCG

	Subshader 
	{ 	
		Tags {"RenderType"="Volume"}
		Fog { Mode off }

		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}	
	}
	
	Fallback Off
}
