Shader "Hidden/Ray Marching/Render Back Depth" {

	CGINCLUDE
		#pragma exclude_renderers xbox360
		#include "UnityCG.cginc"

		struct v2f {
			float4 pos : POSITION;
			float depth : TEXCOORD0;
		};
		
		float4 _VolumeScale;

		v2f vert(appdata_base v) 
		{
			v2f o;
			o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
			o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z * _ProjectionParams.w;
			return o;
		}

		half4 frag(v2f i) : COLOR 
		{ 
			if(i.depth > 0.999)
			{
				return 0;
			}
		
			return float4(i.depth, 0, 0, 1);
		}
		
	ENDCG

	Subshader 
	{ 	
		Tags {"RenderType"="Volume"}
		Fog { Mode Off }
		
		Pass 
		{	
			Cull Front
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
