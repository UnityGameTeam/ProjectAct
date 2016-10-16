using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetBundleGraph {
	
	/**
		IntegratedGUIImportSetting is the class for apply specific setting to already imported files.
	*/
	public class IntegratedGUIImportSetting : INodeOperationBase {
		
		public void Setup (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			// reserve importSetting type for limit asset.
			var importSettingSampleType = string.Empty;
			
			
			var outputDict = new Dictionary<string, List<Asset>>();

			var first = true;
			
			if (groupedSources.Keys.Count == 0) return;
			
			// ImportSetting merges multiple incoming groups into one, so warn this
			if (1 < groupedSources.Keys.Count) {
				Debug.LogWarning(nodeName + " ImportSetting merges incoming group into \"" + groupedSources.Keys.ToList()[0]);
			}

			var inputSources = new List<Asset>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
				
			var importedAssets = new List<Asset>();
			

			var samplingDirectoryPath = FileUtility.PathCombine(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE, nodeId);
			ValidateImportSample(samplingDirectoryPath,
				(string samplePath) => {
					// do nothing. keep importing new asset for sampling.
				},
				(string samplePath) => {
					// do nothing. keep importing new asset for sampling.
				},
				(string samplePath) => {
					importSettingSampleType = AssetImporter.GetAtPath(samplePath).GetType().ToString();
					first = false;
				},
				(string samplePath) => {
					throw new NodeException(
						String.Format("Too many sample file found for this import setting node. Delete files in {0} or use \"Clear Saved ImportSettings\" menu.", samplePath), 
						nodeId);
				}
			);

			var alreadyImported = new List<string>();
			var ignoredResource = new List<string>();
			
			
			foreach (var asset in inputSources) {
				if (string.IsNullOrEmpty(asset.absoluteAssetPath)) {
					if (!string.IsNullOrEmpty(asset.importFrom)) {
						alreadyImported.Add(asset.importFrom);
						continue;
					}

					ignoredResource.Add(asset.fileNameAndExtension);
					continue;
				}
				
				var assetType = AssetImporter.GetAtPath(asset.importFrom).GetType();
				var importerTypeStr = assetType.ToString();
				
				/*
					only texture, model and audio importer is acceptable.
				*/
				switch (importerTypeStr) {
					case "UnityEditor.TextureImporter":
					case "UnityEditor.ModelImporter":
					case "UnityEditor.AudioImporter": {
						break;
					}
					
					default: {
						throw new NodeException("unhandled importer type:" + importerTypeStr, nodeId);
					}
				}
				
				var newData = Asset.DuplicateAssetWithNewType(asset, assetType);
				importedAssets.Add(newData);

				if (first) {
					if (!Directory.Exists(samplingDirectoryPath)) Directory.CreateDirectory(samplingDirectoryPath);

					var absoluteFilePath = asset.absoluteAssetPath;
					var targetFilePath = FileUtility.PathCombine(samplingDirectoryPath, asset.fileNameAndExtension);

					EditorUtility.DisplayProgressBar("AssetBundleGraph ImportSetting generating ImporterSetting...", targetFilePath, 0);
					FileUtility.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
					first = false;
					AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
					EditorUtility.ClearProgressBar();
					
					importSettingSampleType = AssetImporter.GetAtPath(targetFilePath).GetType().ToString();
				} else {
					if (importerTypeStr != importSettingSampleType) {
						throw new NodeException("Multiple asset type is given to Importer Settings. ImporterSetting Takes only 1 asset type." + nodeName +  " is configured for " + importSettingSampleType + ", but " + importerTypeStr + " found.", nodeId);
					}
				}
			

				if (alreadyImported.Any()) {
					Debug.LogError("importSetting:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
				}
				if (ignoredResource.Any()) {
					Debug.LogError("importSetting:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");
				}

				outputDict[groupedSources.Keys.ToList()[0]] = importedAssets;
			}

			Output(nodeId, connectionIdToNextNode, outputDict, new List<string>());
		}
		
		public void Run (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			var usedCache = new List<string>();
			
			var outputDict = new Dictionary<string, List<Asset>>();


			// caution if import setting file is exists already or not.
			var samplingDirectoryPath = FileUtility.PathCombine(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE, nodeId);
			
			var sampleAssetPath = string.Empty;
			ValidateImportSample(samplingDirectoryPath,
				(string samplePath) => {
					throw new AssetBundleGraphBuildException(nodeName + ": No ImportSettings Directory found for this node:" + nodeName + " please supply assets to this node.");
				},
				(string samplePath) => {
					throw new AssetBundleGraphBuildException(nodeName + ": No saved ImportSettings found for asset:" + samplePath);
				},
				(string samplePath) => {
					sampleAssetPath = samplePath;
				},
				(string samplePath) => {
					throw new AssetBundleGraphBuildException(nodeName + ": Too many ImportSettings found. please open editor and resolve issue:" + samplePath);
				}
			);
			
			if (groupedSources.Keys.Count == 0) return;
			
			var the1stGroupKey = groupedSources.Keys.ToList()[0];
			
			
			// ImportSetting merges multiple incoming groups into one, so warn this
			if (1 < groupedSources.Keys.Count) {
				Debug.LogWarning(nodeName + " ImportSetting merges incoming group into \"" + groupedSources.Keys.ToList()[0]);
			}

			var inputSources = new List<Asset>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
			
			var assetImportSettingUpdateMap = new Dictionary<Asset, bool>();
			
			/*
				check file & setting.
				if need, apply importSetting to file.
			*/
			var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);			
			var effector = new InternalSamplingImportEffector(samplingAssetImporter);

			foreach (var asset in inputSources) {
				var importer = AssetImporter.GetAtPath(asset.importFrom);
				
				if (samplingAssetImporter.GetType() != importer.GetType()) {
					throw new NodeException("for each importerSetting should be only treat 1 import setting. current import setting type of this node is:" + 
						samplingAssetImporter.GetType().ToString() + " for file:" + asset.importFrom, nodeId);
				}
				
				assetImportSettingUpdateMap[asset] = false;
				/*
					Apply ImporterSettings' preserved settings, and record if anything changed
				*/
				switch (importer.GetType().ToString()) {
					case "UnityEditor.TextureImporter": {
						var texImporter = importer as TextureImporter;
						var same = InternalSamplingImportEffector.IsSameTextureSetting(texImporter, samplingAssetImporter as TextureImporter);
						
						if (!same) {
							effector.ForceOnPreprocessTexture(texImporter);
							assetImportSettingUpdateMap[asset] = true;
						}
						break;
					}
					case "UnityEditor.ModelImporter": {
						var modelImporter = importer as ModelImporter;
						var same = InternalSamplingImportEffector.IsSameModelSetting(modelImporter, samplingAssetImporter as ModelImporter);
						
						if (!same) {
							effector.ForceOnPreprocessModel(modelImporter);
							assetImportSettingUpdateMap[asset] = true;
						}
						break;
					}
					case "UnityEditor.AudioImporter": {
						var audioImporter = importer as AudioImporter;
						var same = InternalSamplingImportEffector.IsSameAudioSetting(audioImporter, samplingAssetImporter as AudioImporter);
						
						if (!same) {
							effector.ForceOnPreprocessAudio(audioImporter);
							assetImportSettingUpdateMap[asset] = true;
						}
						break;
					}
					
					default: {
						throw new NodeException("unhandled importer type:" + importer.GetType().ToString(), nodeId);
					}
				}
			}


			var outputSources = new List<Asset>();
			
			foreach (var asset in inputSources) {
				var updated = assetImportSettingUpdateMap[asset];
				if (!updated) {
					// already set completed.
					outputSources.Add(
						Asset.CreateNewAssetWithImportPathAndStatus(
							asset.importFrom,
							false,// isNew not changed.
							false
						)
					);
				} else {
					// updated asset.
					outputSources.Add(
						Asset.CreateNewAssetWithImportPathAndStatus(
							asset.importFrom,
							true,// isNew changed.
							false
						)
					);
				}
			}
			
			outputDict[the1stGroupKey] = outputSources;

			Output(nodeId, connectionIdToNextNode, outputDict, usedCache);
		}

		public static void ValidateImportSample (string samplePath, 
			Action<string> NoSampleFolderFound, 
			Action<string> NoSampleFound, 
			Action<string> ValidSampleFound,
			Action<string> TooManySampleFound
		) {
			if (Directory.Exists(samplePath)) {
				var filesInSampling = FileUtility.FilePathsInFolderOnly1Level(samplePath)
					.Where(path => !path.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION))
					.ToList();

				switch (filesInSampling.Count) {
					case 0: {
						NoSampleFound(samplePath);
						return;
					}
					case 1: {
						ValidSampleFound(filesInSampling[0]);
						return;
					}
					default: {
						TooManySampleFound(samplePath);
						return;
					}
				}
			}

			NoSampleFolderFound(samplePath);
		}

	}
}
