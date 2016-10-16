﻿using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleGraph {
	/**
		GUI Inspector to NodeGUI (Through NodeGUIInspectorHelper)
	*/
	[CustomEditor(typeof(NodeGUIInspectorHelper))]
	public class NodeGUIEditor : Editor {

		private List<Action> messageActions;

		private bool packageEditMode = false;

		public override bool RequiresConstantRepaint() {
			return true;
		}

		private void DoInspectorLoaderGUI (NodeGUI node) {
			if (node.loadPath == null) return;

			EditorGUILayout.HelpBox("Loader: Load assets in given directory path.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			/*
				platform & package
			*/
			if (packageEditMode) EditorGUI.BeginDisabledGroup(true);

			// update platform & package.
			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField("Load Path:");
				var newLoadPath = EditorGUILayout.TextField(
					SystemDataUtility.GetProjectName() + AssetBundleGraphSettings.ASSETS_PATH, 
					SystemDataUtility.GetPlatformValue(
						node.loadPath.ReadonlyDict(), 
						node.currentPlatform
					).ToString()
				);
				var loaderNodePath = FileUtility.GetPathWithAssetsPath(newLoadPath);
				IntegratedGUILoader.ValidateLoadPath(
					newLoadPath,
					loaderNodePath,
					() => {
						//EditorGUILayout.HelpBox("load path is empty.", MessageType.Error);
					},
					() => {
						//EditorGUILayout.HelpBox("Directory not found:" + loaderNodePath, MessageType.Error);
					}
				);

				if (newLoadPath !=	SystemDataUtility.GetPlatformValue(
					node.loadPath.ReadonlyDict(),
					node.currentPlatform
				).ToString()
				) {
					node.BeforeSave();
					node.loadPath.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), newLoadPath);
					node.Save();
				}
			}

			if (packageEditMode) {
				EditorGUI.EndDisabledGroup();
			}
			UpdateDeleteSetting(node);
		}

		private void DoInspectorFilterGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Filter: Filter incoming assets by keywords and types. You can use regular expressions for keyword field.", MessageType.Info);
			UpdateNodeName(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				GUILayout.Label("Filter Settings:");
				for (int i = 0; i < node.filterContainsKeywords.Count; i++) {

					Action messageAction = null;

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("-", GUILayout.Width(30))) {
							node.BeforeSave();
							node.filterContainsKeywords.RemoveAt(i);
							node.filterContainsKeytypes.RemoveAt(i);

							node.DeleteFilterOutputPoint(i);
						}
						else {
							var newContainsKeyword = node.filterContainsKeywords[i];

							/*
								generate keyword + keytype string for compare exists setting vs new modifying setting at once.
							*/
							var currentKeywordsSource = new List<string>(node.filterContainsKeywords);
							var currentKeytypesSource = new List<string>(node.filterContainsKeytypes);

							var currentKeytype = currentKeytypesSource[i];

							for (var j = 0; j < currentKeywordsSource.Count; j++) {
								currentKeywordsSource[j] = currentKeywordsSource[j] + currentKeytypesSource[j];
							}

							// remove current choosing one from compare target.
							currentKeywordsSource.RemoveAt(i);
							var currentKeywords = new List<string>(currentKeywordsSource);

							GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");

							IntegratedGUIFilter.ValidateFilter(
								newContainsKeyword + currentKeytype,
								currentKeywords,
								() => {
									s.fontStyle = FontStyle.Bold;
									s.fontSize = 12;
								},
								() => {
									s.fontStyle = FontStyle.Bold;
									s.fontSize = 12;
								}
							);

							using (new EditorGUILayout.HorizontalScope()) {
								newContainsKeyword = EditorGUILayout.TextField(node.filterContainsKeywords[i], s, GUILayout.Width(120));
								var currentIndex = i;
								if (GUILayout.Button(node.filterContainsKeytypes[i], "Popup")) {
									NodeGUI.ShowFilterKeyTypeMenu(
										node.filterContainsKeytypes[currentIndex],
										(string selectedTypeStr) => {
											node.BeforeSave();
											node.filterContainsKeytypes[currentIndex] = selectedTypeStr;
											node.Save();
										} 
									);
								}
							}

							if (newContainsKeyword != node.filterContainsKeywords[i]) {
								node.BeforeSave();
								node.filterContainsKeywords[i] = newContainsKeyword;
								node.RenameFilterOutputPointLabel(i, node.filterContainsKeywords[i]);
							}
						}
					}

					if(messageAction != null) {
						using (new GUILayout.HorizontalScope()) {
							messageAction.Invoke();
						}
					}
				}

				// add contains keyword interface.
				if (GUILayout.Button("+")) {
					node.BeforeSave();
					var addingIndex = node.filterContainsKeywords.Count;
					var newKeyword = AssetBundleGraphSettings.DEFAULT_FILTER_KEYWORD;

					node.filterContainsKeywords.Add(newKeyword);
					node.filterContainsKeytypes.Add(AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE);

					node.AddFilterOutputPoint(addingIndex, AssetBundleGraphSettings.DEFAULT_FILTER_KEYWORD);
				}
			}
		}

		private void DoInspectorImportSettingGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("ImportSetting: Force apply import settings to given assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			if (packageEditMode) {
				EditorGUI.BeginDisabledGroup(true);
			}
			
			/*
				importer node has no platform key. 
				platform key is contained by Unity's importer inspector itself.
			*/
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var nodeId = node.nodeId;

				var samplingPath = FileUtility.PathCombine(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE, nodeId);

				IntegratedGUIImportSetting.ValidateImportSample(samplingPath,
					(string noFolderFound) => {
						EditorGUILayout.LabelField("Sampling Asset", "No sample asset found. please Reload first.");
					},
					(string noFilesFound) => {
						EditorGUILayout.LabelField("Sampling Asset", "No sample asset found. please Reload first.");
					},
					(string samplingAssetPath) => {
						EditorGUILayout.LabelField("Sampling Asset Path", samplingAssetPath);
						if (GUILayout.Button("Setup Import Setting")) {
							var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(samplingAssetPath);
							Selection.activeObject = obj;
						}
						if (GUILayout.Button("Reset Import Setting")) {
							// delete all import setting files.
							FileUtility.RemakeDirectory(samplingPath);
							node.Save();
						}
					},
					(string tooManyFilesFoundMessage) => {
						if (GUILayout.Button("Reset Import Setting")) {
							// delete all import setting files.
							FileUtility.RemakeDirectory(samplingPath);
							node.Save();
						}
					}
				);
			}

			if (packageEditMode) {
				EditorGUI.EndDisabledGroup();
			}
			UpdateDeleteSetting(node);
		}

		private void DoInspectorModifierGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Modifier: Force apply asset settings to given assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			var currentModifierTargetType = IntegratedGUIModifier.ModifierOperationTargetTypeName(node.nodeId);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				// show incoming type of Assets and reset interface.
				{
					var isOperationDataExist = false;
					IntegratedGUIModifier.ValidateModifiyOperationData(
						node.nodeId,
						node.currentPlatform,
						() => {
							GUILayout.Label("No modifier data found, please Reload first.");
						},
						() => {
							isOperationDataExist = true;
						}
					);
					
					if (!isOperationDataExist) {
						return;
					}

					using (new EditorGUILayout.HorizontalScope()) {
						GUILayout.Label("Target Type:");
						GUILayout.Label(currentModifierTargetType);
					}

					/*
						reset whole platform's data for this modifier.
					*/
					if (GUILayout.Button("Reset Modifier")) {
						var modifierFolderPath = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, node.nodeId);
						FileUtility.RemakeDirectory(modifierFolderPath);
						node.Save();
						modifierOperatorInstance = null;
						return;
					}
				}

				GUILayout.Space(10f);

				var usingScriptMode = !string.IsNullOrEmpty(node.scriptAttrNameOrClassName);

				// use modifier script manually.
				{
					GUIStyle s = new GUIStyle("TextFieldDropDownText");
					/*
						check prefabricator script-type string.
					*/
					if (string.IsNullOrEmpty(node.scriptAttrNameOrClassName)) {
						s.fontStyle = FontStyle.Bold;
						s.fontSize  = 12;
					} else {
						var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(node.scriptAttrNameOrClassName);

						if (loadedType == null) {
							s.fontStyle = FontStyle.Bold;
							s.fontSize  = 12;
						}
					}
					
					var before = !string.IsNullOrEmpty(node.scriptAttrNameOrClassName);
					usingScriptMode = EditorGUILayout.ToggleLeft("Use ModifierOperator Script", !string.IsNullOrEmpty(node.scriptAttrNameOrClassName));
					
					// detect mode changed.
					if (before != usingScriptMode) {
						// checked. initialize value of scriptClassName.
						if (usingScriptMode) {
							node.BeforeSave();
							node.scriptAttrNameOrClassName = "MyModifier";
							node.Save();
						}

						// unchecked.
						if (!usingScriptMode) {
							node.BeforeSave();
							node.scriptAttrNameOrClassName = string.Empty;
							node.Save();
						}
					}
					
					if (!usingScriptMode) {
						EditorGUI.BeginDisabledGroup(true);	
					}
					GUILayout.Label("ここをドロップダウンにする。2");
					var newScriptClass = EditorGUILayout.TextField("Classname", node.scriptAttrNameOrClassName, s);
					if (newScriptClass != node.scriptAttrNameOrClassName) {
						node.BeforeSave();
						node.scriptAttrNameOrClassName = newScriptClass;
						node.Save();
					}
					if (!usingScriptMode) {
						EditorGUI.EndDisabledGroup();	
					}
				}

				GUILayout.Space(10f);

				if (usingScriptMode) {
					EditorGUI.BeginDisabledGroup(true);
				}

				// show for each platform tab. 

				var currentPlatform = node.currentPlatform;
				node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

				/*
					if platform tab is changed, renew modifierOperatorInstance for that tab.
				*/
				if (currentPlatform != node.currentPlatform) {
					modifierOperatorInstance = null;
				}

				/*
					reload modifierOperator instance from saved modifierOperator data.
				*/
				if (modifierOperatorInstance == null) {
					var modifierOperatorDataPath = IntegratedGUIModifier.ModifierDataPathForeachPlatform(node.nodeId, node.currentPlatform);

					// choose default modifierOperatorData if platform specified file is not exist.
					if (!File.Exists(modifierOperatorDataPath)) {
						modifierOperatorDataPath = IntegratedGUIModifier.ModifierDataPathForDefaultPlatform(node.nodeId);
					}
					
					var loadedModifierOperatorDataStr = string.Empty;
					using (var sr = new StreamReader(modifierOperatorDataPath)) {
						loadedModifierOperatorDataStr = sr.ReadToEnd();
					} 

					var modifierOperatorType = TypeUtility.SupportedModifierOperatorDefinition[currentModifierTargetType];

					/*
						create instance from saved modifierOperator data.
					*/
					modifierOperatorInstance = typeof(NodeGUIEditor)
						.GetMethod("FromJson")
						.MakeGenericMethod(modifierOperatorType)// set desired generic type here.
						.Invoke(this, new object[] { loadedModifierOperatorDataStr }) as ModifierBase;
				}

				/*
					Show ModifierOperator Inspector.
				*/
				if (modifierOperatorInstance != null) {
					Action changed = () => {
						var data = JsonUtility.ToJson(modifierOperatorInstance);
						var prettified = AssetBundleGraphEditorWindow.PrettifyJson(data);

						var modifierOperatorDataPath = IntegratedGUIModifier.ModifierDataPathForeachPlatform(node.nodeId, node.currentPlatform);

						using (var sw = new StreamWriter(modifierOperatorDataPath)) {
							sw.Write(prettified);
						}

						// reflect change of data.
						AssetDatabase.Refresh();
						
						modifierOperatorInstance = null;
					};

					GUILayout.Space(10f);

					modifierOperatorInstance.DrawInspector(changed);
				}

				var deleted = UpdateDeleteSetting(node);
				if (deleted) {
					// source platform depended data is deleted. reload instance for reloading instance from data.
					modifierOperatorInstance = null;
				}

				if (usingScriptMode) {
					EditorGUI.EndDisabledGroup();
				}
			}
		}

		public T FromJson<T> (string source) {
			return JsonUtility.FromJson<T>(source);
		}  

		/*
			・NonSerializedをセットしないと、ModifierOperators.OperatorBase型に戻ってしまう。
			・SerializeFieldにする or なにもつけないと、もれなくModifierOperators.OperatorBase型にもどる
			・Undo/Redoを行うためには、ModifierOperators.OperatorBaseを拡張した型のメンバーをUndo/Redo対象にしなければいけない
			・ModifierOperators.OperatorBase意外に晒していい型がない

			という無茶苦茶な難題があります。
			Undo/Redo時にオリジナルの型に戻ってしまう、という仕様と、追加を楽にするために型定義をModifierOperators.OperatorBase型にする、
			っていうのが相反するようです。うーんどうしよう。
		*/
		[NonSerialized] private ModifierBase modifierOperatorInstance;

		private void DoInspectorGroupingGUI (NodeGUI node) {
			if (node.groupingKeyword == null) return;

			EditorGUILayout.HelpBox("Grouping: Create group of assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var newGroupingKeyword = EditorGUILayout.TextField(
					"Grouping Keyword",
					SystemDataUtility.GetPlatformValue(
						node.groupingKeyword.ReadonlyDict(), 
						node.currentPlatform
					).ToString()
				);
				EditorGUILayout.HelpBox("Grouping Keyword requires \"*\" in itself. It assumes there is a pattern such as \"ID_0\" in incoming paths when configured as \"ID_*\" ", MessageType.Info);


				IntegratedGUIGrouping.ValidateGroupingKeyword(
					newGroupingKeyword,
					() => {
//						EditorGUILayout.HelpBox("groupingKeyword is empty.", MessageType.Error);
					},
					() => {
//						EditorGUILayout.HelpBox("grouping keyword does not contain " + AssetBundleGraphSettings.KEYWORD_WILDCARD + " groupingKeyword:" + newGroupingKeyword, MessageType.Error);
					}
				);

				if (newGroupingKeyword != SystemDataUtility.GetPlatformValue(
					node.groupingKeyword.ReadonlyDict(), 
					node.currentPlatform
				).ToString()
				) {
					node.BeforeSave();
					node.groupingKeyword.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), newGroupingKeyword);
					node.Save();
				}
			}

			UpdateDeleteSetting(node);
		}

		private void DoInspectorPrefabricatorGUI (NodeGUI node) {
			EditorGUILayout.HelpBox("Prefabricator: Create prefab with given assets and script.", MessageType.Info);
			UpdateNodeName(node);

			using (new EditorGUILayout.HorizontalScope(GUI.skin.box)) {
				GUILayout.Label("Prefabricator Class");
				if (GUILayout.Button(node.scriptAttrNameOrClassName, "Popup")) {
					/*
						collect type name or "Name" attribute parameter from extended-PrefabricatorBase class.
					*/
					var prefabricatorCandidateTypeNameOrAttrName = PrefabricatorBase.GetPrefabricatorAttrName_ClassNameDict();
					
					// prepare for no class found.
					if (!prefabricatorCandidateTypeNameOrAttrName.Any()) {
						var menu = new GenericMenu();

						menu.AddItem(
							new GUIContent("Generate Example Prefabricator Script"),
							false,
							() => {
								// generate sample.
								AssetBundleGraphEditorWindow.GenerateScript(AssetBundleGraphEditorWindow.ScriptType.SCRIPT_PREFABRICATOR);
							}
						);

						menu.ShowAsContext();
						return;
					}

					/*
						displays type name or attribute if exist.
					*/
					NodeGUI.ShowTypeNamesMenu(
						node.scriptAttrNameOrClassName,
						prefabricatorCandidateTypeNameOrAttrName.Keys.ToList(),
						(string selectedClassNameOrAttrName) => {
							node.BeforeSave();
							node.scriptAttrNameOrClassName = selectedClassNameOrAttrName;
							node.Save();
						}
					);
				}
			}
		}
		
		private void DoInspectorBundlizerGUI (NodeGUI node) {
			if (node.bundleNameTemplate == null) return;

			EditorGUILayout.HelpBox("Bundlizer: Create asset bundle settings with given group of assets.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var bundleNameTemplate = EditorGUILayout.TextField(
					"Bundle Name Template", 
					SystemDataUtility.GetPlatformValue(
						node.bundleNameTemplate.ReadonlyDict(), 
						node.currentPlatform
					).ToString()
				).ToLower();

				IntegratedGUIBundlizer.ValidateBundleNameTemplate(
					bundleNameTemplate,
					() => {
//						EditorGUILayout.HelpBox("No Bundle Name Template set.", MessageType.Error);
					}
				);

				if (bundleNameTemplate != SystemDataUtility.GetPlatformValue(
					node.bundleNameTemplate.ReadonlyDict(), 
					node.currentPlatform
				).ToString()
				) {
					node.BeforeSave();
					node.bundleNameTemplate.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), bundleNameTemplate);
					node.Save();
				}

				GUILayout.Label("Variants:");
				for (int i = 0; i < node.variants.Keys.Count; ++i) {

					var inputConnectionId = node.variants.Keys[i];

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("-", GUILayout.Width(30))) {
							node.BeforeSave();
							node.variants.Remove(inputConnectionId);
							node.DeleteInputPoint(inputConnectionId);
						}
						else {
							var variantName = node.variants.Values[i];

							GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");
							Action makeStyleBold = () => {
								s.fontStyle = FontStyle.Bold;
								s.fontSize = 12;
							};

							IntegratedGUIBundlizer.ValidateVariantName(variantName, node.variants.Values, 
								makeStyleBold,
								makeStyleBold,
								makeStyleBold);

							variantName = EditorGUILayout.TextField(variantName, s);

							if (variantName != node.variants.Values[i]) {
								node.BeforeSave();
								node.variants.Values[i] = variantName;
								node.RenameInputPoint(inputConnectionId, variantName);
							}
						}
					}
				}

				if (GUILayout.Button("+")) {
					node.BeforeSave();
					var newid = Guid.NewGuid().ToString();
					node.variants.Add(newid, AssetBundleGraphSettings.BUNDLIZER_VARIANTNAME_DEFAULT);
					node.AddInputPoint(newid, AssetBundleGraphSettings.BUNDLIZER_VARIANTNAME_DEFAULT);
				}

			}

			UpdateDeleteSetting(node);
		}

		private void DoInspectorBundleBuilderGUI (NodeGUI node) {
			if (node.enabledBundleOptions == null) return;

			EditorGUILayout.HelpBox("BundleBuilder: Build asset bundles with given asset bundle settings.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var bundleOptions = SystemDataUtility.GetPlatformValue(
					node.enabledBundleOptions.ReadonlyDict(),
					node.currentPlatform
				);

				var plartform_pakcage_key = SystemDataUtility.CreateKeyNameFromString(node.currentPlatform);

				for (var i = 0; i < AssetBundleGraphSettings.DefaultBundleOptionSettings.Count; i++) {
					var enablablekey = AssetBundleGraphSettings.DefaultBundleOptionSettings[i];

					// contains keyword == enabled. if not, disabled.
					var isEnabled = bundleOptions.Contains(enablablekey);

					var result = EditorGUILayout.ToggleLeft(enablablekey, isEnabled);
					if (result != isEnabled) {
						node.BeforeSave();

						var resultsDict = node.enabledBundleOptions.ReadonlyDict();
						var resultList = new List<string>();
						if (resultsDict.ContainsKey(plartform_pakcage_key)) resultList = resultsDict[plartform_pakcage_key];

						if (result) {
							if (!resultList.Contains(enablablekey)) {
								var currentEnableds = new List<string>();
								if (resultsDict.ContainsKey(plartform_pakcage_key)) currentEnableds = resultsDict[plartform_pakcage_key];
								currentEnableds.Add(enablablekey);

								node.enabledBundleOptions.Add(
									SystemDataUtility.CreateKeyNameFromString(node.currentPlatform),
									currentEnableds
								);
							}
						}

						if (!result) {
							if (resultList.Contains(enablablekey)) {
								var currentEnableds = new List<string>();
								if (resultsDict.ContainsKey(plartform_pakcage_key)) currentEnableds = resultsDict[plartform_pakcage_key];
								currentEnableds.Remove(enablablekey);

								node.enabledBundleOptions.Add(
									SystemDataUtility.CreateKeyNameFromString(node.currentPlatform),
									currentEnableds
								);
							}
						}

						/*
										Cannot use options DisableWriteTypeTree and IgnoreTypeTreeChanges at the same time.
									*/
						if (enablablekey == "Disable Write TypeTree" && result &&
							node.enabledBundleOptions.ReadonlyDict()[SystemDataUtility.CreateKeyNameFromString(node.currentPlatform)].Contains("Ignore TypeTree Changes")) {

							var newEnableds = node.enabledBundleOptions.ReadonlyDict()[SystemDataUtility.CreateKeyNameFromString(node.currentPlatform)];
							newEnableds.Remove("Ignore TypeTree Changes");

							node.enabledBundleOptions.Add(
								SystemDataUtility.CreateKeyNameFromString(node.currentPlatform),
								newEnableds
							);
						}

						if (enablablekey == "Ignore TypeTree Changes" && result &&
							node.enabledBundleOptions.ReadonlyDict()[SystemDataUtility.CreateKeyNameFromString(node.currentPlatform)].Contains("Disable Write TypeTree")) {

							var newEnableds = node.enabledBundleOptions.ReadonlyDict()[SystemDataUtility.CreateKeyNameFromString(node.currentPlatform)];
							newEnableds.Remove("Disable Write TypeTree");

							node.enabledBundleOptions.Add(
								SystemDataUtility.CreateKeyNameFromString(node.currentPlatform),
								newEnableds
							);
						}

						node.Save();
						return;
					}
				}
			}

			UpdateDeleteSetting(node);

		}


		private void DoInspectorExporterGUI (NodeGUI node) {
			if (node.exportTo == null) return;

			EditorGUILayout.HelpBox("Exporter: Export given files to output directory.", MessageType.Info);
			UpdateNodeName(node);

			GUILayout.Space(10f);

			node.currentPlatform = UpdateCurrentPlatform(node.currentPlatform);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField("Export Path:");
				var newExportPath = EditorGUILayout.TextField(
					SystemDataUtility.GetProjectName(), 
					SystemDataUtility.GetPlatformValue(
						node.exportTo.ReadonlyDict(), 
						node.currentPlatform
					).ToString()
				);

				var exporterrNodePath = FileUtility.GetPathWithProjectPath(newExportPath);
				if(IntegratedGUIExporter.ValidateExportPath(
					newExportPath,
					exporterrNodePath,
					() => {
						// TODO Make text field bold
					},
					() => {
						using (new EditorGUILayout.HorizontalScope()) {
							EditorGUILayout.LabelField(exporterrNodePath + " does not exist.");
							if(GUILayout.Button("Create directory")) {
								Directory.CreateDirectory(exporterrNodePath);
								node.Save();
							}
						}
						EditorGUILayout.Space();

						EditorGUILayout.LabelField("Available Directories:");
						string[] dirs = Directory.GetDirectories(Path.GetDirectoryName(exporterrNodePath));
						foreach(string s in dirs) {
							EditorGUILayout.LabelField(s);
						}
					}
				)) {
					using (new EditorGUILayout.HorizontalScope()) {
						GUILayout.FlexibleSpace();
						#if UNITY_EDITOR_OSX
						string buttonName = "Reveal in Finder";
						#else
						string buttonName = "Show in Explorer";
						#endif 
						if(GUILayout.Button(buttonName)) {
							EditorUtility.RevealInFinder(exporterrNodePath);
						}
					}
				}


				if (newExportPath != SystemDataUtility.GetPlatformValue(
					node.exportTo.ReadonlyDict(),
					node.currentPlatform
				).ToString()
				) {
					node.BeforeSave();
					node.exportTo.Add(SystemDataUtility.CreateKeyNameFromString(node.currentPlatform), newExportPath);
					node.Save();
				}
			}

			UpdateDeleteSetting(node);
		}


		public override void OnInspectorGUI () {
			var currentTarget = (NodeGUIInspectorHelper)target;
			var node = currentTarget.node;
			if (node == null) return;

			if(messageActions == null) {
				messageActions = new List<Action>();
			}

			messageActions.Clear();

			switch (node.kind) {
			case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
				DoInspectorLoaderGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
				DoInspectorFilterGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI :
				DoInspectorImportSettingGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI :
				DoInspectorModifierGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.GROUPING_GUI:
				DoInspectorGroupingGUI(node);
				break;
			
			case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:{
				DoInspectorPrefabricatorGUI(node);
				break;
			}
			case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI:
				DoInspectorBundlizerGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI:
				DoInspectorBundleBuilderGUI(node);
				break;
			case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: 
				DoInspectorExporterGUI(node);
				break;
			default: 
				Debug.LogError(node.name + " is defined as unknown kind of node. value:" + node.kind);
				break;
			}

			var errors = currentTarget.errors;
			if (errors != null && errors.Any()) {
				foreach (var error in errors) {
					EditorGUILayout.HelpBox(error, MessageType.Error);
				}
			}
			using (new EditorGUILayout.VerticalScope()) {
				foreach(Action a in messageActions) {
					a.Invoke();
				}
			}
		}

		private void ShowFilterKeyTypeMenu (string current, Action<string> ExistSelected) {
			var menu = new GenericMenu();
			
			menu.AddDisabledItem(new GUIContent(current));
			
			menu.AddSeparator(string.Empty);
			
			for (var i = 0; i < TypeUtility.KeyTypes.Count; i++) {
				var type = TypeUtility.KeyTypes[i];
				if (type == current) continue;
				
				menu.AddItem(
					new GUIContent(type),
					false,
					() => {
						ExistSelected(type);
					}
				);
			}
			menu.ShowAsContext();
		}

		private void UpdateNodeName (NodeGUI node) {
			var newName = EditorGUILayout.TextField("Node Name", node.name);

			if( NodeGUIUtility.allNodeNames != null ) {
				var overlapping = NodeGUIUtility.allNodeNames.GroupBy(x => x)
					.Where(group => group.Count() > 1)
					.Select(group => group.Key);
				if (overlapping.Any() && overlapping.Contains(newName)) {
					EditorGUILayout.HelpBox("This node name already exist. Please put other name:" + newName, MessageType.Error);
					AssetBundleGraphEditorWindow.AddNodeException(new NodeException("Node name " + newName + " already exist.", node.nodeId ));
				}
			}

			if (newName != node.name) {
				node.BeforeSave();
				node.name = newName;
				node.UpdateNodeRect();
				node.Save();
			}
		}

		private string UpdateCurrentPlatform (string basePlatfrom) {
			var newPlatform = basePlatfrom;

			EditorGUI.BeginChangeCheck();
			using (new EditorGUILayout.HorizontalScope()) {
				var choosenIndex = -1;
				for (var i = 0; i < NodeGUIUtility.platformButtonTextures.Length; i++) {
					var onOffBefore = NodeGUIUtility.platformStrings[i] == basePlatfrom;
					var onOffAfter = onOffBefore;

					// index 0 is Default.
					switch (i) {
					case 0: {
							onOffAfter = GUILayout.Toggle(onOffBefore, "Default", "toolbarbutton");
							break;
						}
					default: {
							// for each platform texture.
							var platformButtonTexture = NodeGUIUtility.platformButtonTextures[i];
							onOffAfter = GUILayout.Toggle(onOffBefore, platformButtonTexture, "toolbarbutton");
							break;
						}
					}

					if (onOffBefore != onOffAfter) {
						choosenIndex = i;
						break;
					}
				}

				if (EditorGUI.EndChangeCheck()) {
					newPlatform = NodeGUIUtility.platformStrings[choosenIndex];
				}
			}

			if (newPlatform != basePlatfrom) GUI.FocusControl(string.Empty);
			return newPlatform;
		}


		private bool UpdateDeleteSetting (NodeGUI currentNode) {
			var currentNodePlatformPackageKey = SystemDataUtility.CreateKeyNameFromString(currentNode.currentPlatform);

			if (currentNodePlatformPackageKey == AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME) return false;

			var deleted = false;
			using (new EditorGUILayout.HorizontalScope()) {
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Use Default Setting", GUILayout.Width(150))) {
					currentNode.BeforeSave();
					currentNode.DeleteCurrentPackagePlatformKey(currentNodePlatformPackageKey);
					GUI.FocusControl(string.Empty);
					currentNode.Save();
					deleted = true;
				}
			}
			return deleted;
		}
	}
}