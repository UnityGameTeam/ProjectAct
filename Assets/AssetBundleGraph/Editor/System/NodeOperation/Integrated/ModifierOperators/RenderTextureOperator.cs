using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	/*
		paramter definitions for RenderTexture.

		このクラスの設定方法と役割
			0.TypeUtility.csのSupportedModifierOperationDefinitionに、
				サポートしたいAsset型をGetType().ToString()した文字列をkey、このクラスのTypeをvalueとして設定する。

				例：RenderTextureのModify処理をこのクラスで対応するようにしたい場合、
				{"UnityEngine.RenderTexture", typeof(ModifierOperators.RenderTextureOperator)}
				とか書く。

			1.このクラスに対象Assetの型のModifierとしての処理を記述する
				DefaultSetting, IsChanged, Modifyの三つのメソッドをoverrideして実装してもらう想定。

			2.このクラスのデータ型の定義を使ってModifierOperationData(ModifierNodeごとに作成されるjson)のデータを吐き出す
				保存したい設定データに[Serializable]とか[SerializeField]のアノテーションつけるの忘れないように。

			3.データ型の定義を持ち、Inspector表示時にそのenumを使って内容を表示することができる
				インスペクタ部分まだちゃんと作ってないですが、型に応じて作る想定。
				

		定数定義とかについて考えてあること：
		・変更できるパラメータの選別について
			オリジナルのAssetのInspector上で扱えるパラメータのみを対象に変更できるようにしようと思ってます。
			例えばRenderTextureであれば、RenderTexture型のAssetのInspectorで表示されるパラメータのみを対象に、
			調整とか変更が出来るようにしようと思っています。

		・パラメータ名はInspector準拠
			例えばRenderTextureの場合、Inspector上でColor Formatって書いてあるパラメータは、実際のAssetではformatって名前になってます。
			
			API上：renderTexture.format
			Inspector上：Color Format

			で、保存するパラメータ名をAPIとInspectorのどっちに合わせようか悩んだんですが、
			
			Inspectorで表示されてる変更可能なパラメータ数 < APIで扱える変更可能なパラメータ数 という前提がまあ自明であるので、
			より強い制約を作り出しているInspectorに準拠しようと思ってます。
			
			IsChangedやModifyメソッド内では、その辺を加味してAPIとInspector上の名前の違いを突き合せる根性が必要です。
			その代わりInspector上での値とパラメータ名のマッチングが楽です。
		
		・閾値とEnumが定義されているパラメータについて
			例えばRenderTextureのwrapModeは、Enum UnityEngine.TextureWrapModeを使っています。
			で、すでに定義されているEnumがある場合は、極力そのEnumを使うと、データ化や反映が楽なんで、
			この型でもUnityEngine.TextureWrapModeを保持して互換性を大事にしようと思ってます。

		・閾値はあるんだけどEnumのないパラメータについて
			RenderTextureのantiAliasingのように、int値かつ特定の値のみを持ち、
			EnumではなくInt32とかを使っていて、
			
			なおかつInspector上で[None]とか[2 samples]とか
			そういう表記になっているパラメータは、この型でEnumを作成して制約をつけています。

			ここで定義したEnumに関しては、データ型としてのRead/Writeはもちろん、Inspector上からも参照する想定です。

		・Inspector上の設定はあるんだけど実際にコードから変更しようとすると変更できないパラメータについて
			ImportSetting作ってる時もあったんですが、
			API上は読めて書いてできるように見えるんだけど実際には変更できない(実行時にエラーが出る)パラメータが多々あります。
			RenderTextureの場合は、widthとかheight、colorFormat, depthあたりが「実際には変更不可」でした。

			本物のInspectorからは変更できるんですが、ユーザーが扱えるレベルのMainThreadのコードでは変更不可っぽいです。
			Assetの作り直しとかを強制的にやればいけるのかもしれませんが、GUID変わりそう。

	*/
	[Serializable] public class RenderTextureOperator : ModifierBase {
		// [SerializeField] public Int32 width, height;

		// public enum AntiAliasing : int {
		// 	None = 1,
		// 	_2_Samples = 2,
		// 	_4_Samples = 4,
		// 	_8_Samples = 8
		// }
		// [SerializeField] public AntiAliasing antiAliasing;// 1, 2, 4, 8. 4type.
		
		// [SerializeField] public UnityEngine.RenderTextureFormat colorFormat;

		public enum DepthBuffer : int {
			NoDepthBuffer = 0,
			_16bitDepth = 16,
			_24bitDepth = 24
		}
		// [SerializeField] public DepthBuffer depthBuffer;// 0, 16, 24. 3type.

		[SerializeField] public UnityEngine.TextureWrapMode wrapMode;

		[SerializeField] public UnityEngine.FilterMode filterMode;

		[SerializeField] public int anisoLevel;// limit to 16.



		public RenderTextureOperator () {}

		private RenderTextureOperator (
			string operatorType,
			// Int32 width, Int32 height,
			// AntiAliasing antiAliasing,
			// UnityEngine.RenderTextureFormat colorFormat,
			// DepthBuffer depthBuffer,
			UnityEngine.TextureWrapMode wrapMode,
			UnityEngine.FilterMode filterMode,
			Int32 anisoLevel
		) {
			this.operatorType = operatorType;
			
			this.wrapMode = wrapMode;
			this.filterMode = filterMode;
			this.anisoLevel = anisoLevel;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new RenderTextureOperator(
				"UnityEngine.RenderTexture",
				UnityEngine.TextureWrapMode.Clamp,
				UnityEngine.FilterMode.Bilinear,
				0
			);
		}

		public override bool IsChanged<T> (T asset) {
			var renderTex = asset as RenderTexture;

			var changed = false;

			if (renderTex.wrapMode != this.wrapMode) changed = true; 
			if (renderTex.filterMode != this.filterMode) changed = true; 
			if (renderTex.anisoLevel != this.anisoLevel) changed = true;

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var renderTex = asset as RenderTexture;
			
			renderTex.wrapMode = this.wrapMode;
			renderTex.filterMode = this.filterMode;

			/*
				depth parameter cannot change from code.
				and anisoLevel can be change if asset's depth is 0. 
			*/
			if (renderTex.depth == (int)DepthBuffer.NoDepthBuffer) {
				renderTex.anisoLevel = this.anisoLevel;
			}
		}

		public override void DrawInspector (Action changed) {
			// wrapMode
			var newWrapMode = (UnityEngine.TextureWrapMode)EditorGUILayout.Popup("Wrap Mode", (int)this.wrapMode, Enum.GetNames(typeof(UnityEngine.TextureWrapMode)), new GUILayoutOption[0]);
			if (newWrapMode != this.wrapMode) {
				this.wrapMode = newWrapMode;
				changed();
			}
			
			// filterMode
			var newFilterMode = (UnityEngine.FilterMode)EditorGUILayout.Popup("Filter Mode", (int)this.filterMode, Enum.GetNames(typeof(UnityEngine.FilterMode)), new GUILayoutOption[0]);
			if (newFilterMode != this.filterMode) {
				this.filterMode = newFilterMode;
				changed();
			}

			// anisoLevel
			using (new GUILayout.HorizontalScope()) {
				GUILayout.Label("Aniso Level");
				
				var changedVal = (int)EditorGUILayout.Slider(this.anisoLevel, 0, 16);
				if (changedVal != this.anisoLevel) {
					this.anisoLevel = changedVal;
					changed();
				}
			}
			EditorGUILayout.HelpBox("Aniso Level can be set if target Asset(RenderTexture)'s Depth Buffer is No depth buffer. ", MessageType.Info);
		}
	}

}