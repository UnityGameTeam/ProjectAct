Shader "UI/TextEmoji" {
	Properties
	{
		_MainTex ("Sprtie Texture", 2D) = "white" {}
		_EmojiTex ("Emoji Texture", 2D) = "white" {}
		_Color ("Tint Color", Color) = (1,1,1,1)

		_StencilComp ("Stencil Comparison", float) = 8
		_Stencil ("Stencil ID", float) = 0
		_StencilOp ("Stencil Operation", float) = 0
		_StencilWriteMask ("Stencil Write Mask", float) = 255
		_StencilReadMask ("Stencil Read Mask", float) = 255

		_ColorMask("Color Mask", float) = 15
	}

	SubShader 
	{
		Tags
		{ 
			"Queue" = "Transparent" 
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Fog
		{ 
			Mode Off 
		}

		Lighting Off
		Cull Off
		ZTest[unity_GUIZTestMode]
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord1 : TEXCOORD1;
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord1 : TEXCOORD1;
			};

			sampler2D _EmojiTex;
			uniform float4 _MainTex_ST;
			uniform fixed4 _Color;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color * _Color;
				o.texcoord1 = v.texcoord1;
#ifdef UNITY_HALF_TEXEL_OFFSET
				o.vertex.xy += (_ScreenParams.zw - 1.0)*float2(-1, 1);
#endif
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				half4 color = tex2D(_EmojiTex, i.texcoord1) * i.color;
				clip(color.a - 0.01);
				return color;
			}

			ENDCG
		}
	} 
}
