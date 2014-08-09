Shader "Hidden/Ray Marching/Composite" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
		_BlendTex ("Blend (RGB)", 2D) = "" {}	
	}
	
	// Shader code pasted into all further CGPROGRAM blocks	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	#pragma target 3.0
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv[2] : TEXCOORD0;
	};
	
	sampler2D _MainTex;
	sampler2D _BlendTex;
	
	float4 _MainTex_TexelSize;
	
	v2f vert( appdata_img v ) {
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		
		o.uv[0] = v.texcoord.xy;
		o.uv[1] = v.texcoord.xy;
		#if SHADER_API_D3D9
		if (_MainTex_TexelSize.y < 0)
			o.uv[0].y = 1-o.uv[0].y;
		#endif			
		return o;
	}
	
	half4 frag(v2f i) : COLOR 
	{		
		half4 src = tex2D(_MainTex, i.uv[1]);
		half4 dst = tex2D(_BlendTex, i.uv[0]);
		return (1.0f - dst.a) * src + dst;
	}

	ENDCG
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }
	  
      CGPROGRAM
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
  }
}

Fallback off
	
} // shader