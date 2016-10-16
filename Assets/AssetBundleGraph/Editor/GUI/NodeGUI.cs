using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;


namespace AssetBundleGraph {
	[Serializable] 
	public class NodeGUI {

		public static float scaleFactor = 1.0f;// 1.0f. 0.7f, 0.4f, 0.3f
		public const float SCALE_MIN = 0.3f;
		public const float SCALE_MAX = 1.0f;
		public const int SCALE_WIDTH = 30;
		public const float SCALE_RATIO = 0.3f;

		[SerializeField] private List<ConnectionPoint> inputConnectionPoints = new List<ConnectionPoint>();
		[SerializeField] private List<ConnectionPoint> outputConnectionPoints = new List<ConnectionPoint>();

		[SerializeField] private int nodeWindowId;
		[SerializeField] private Rect baseRect;

		[SerializeField] public string name;
		[SerializeField] public string nodeId;
		[SerializeField] public AssetBundleGraphSettings.NodeKind kind;

		[SerializeField] public string scriptAttrNameOrClassName;
		
		[SerializeField] public SerializablePseudoDictionary loadPath;
		[SerializeField] public SerializablePseudoDictionary exportTo;
		[SerializeField] public List<string> filterContainsKeywords;
		[SerializeField] public List<string> filterContainsKeytypes;
		[SerializeField] public SerializablePseudoDictionary importerPackages;
		[SerializeField] public SerializablePseudoDictionary groupingKeyword;
		[SerializeField] public SerializablePseudoDictionary bundleNameTemplate;
		[SerializeField] public SerializablePseudoDictionary variants;
		[SerializeField] public SerializablePseudoDictionary2 enabledBundleOptions;

		// for platform-package specified parameter.
		[SerializeField] public string currentPlatform = AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME;

		[SerializeField] private string nodeInterfaceTypeStr;
		
		[SerializeField] private NodeGUIInspectorHelper nodeInsp;

		/*
			show error on node functions.
		*/
		private bool hasErrors = false;

		public void RenewErrorSource () {
			hasErrors = false;
			this.nodeInsp.UpdateNode(this);
			this.nodeInsp.UpdateErrors(new List<string>());
		}
		public void AppendErrorSources (List<string> errors) {
			this.hasErrors = true;
			this.nodeInsp.UpdateNode(this);
			this.nodeInsp.UpdateErrors(errors);
		}

		public int WindowId {
			get {
				return nodeWindowId;
			}

			set {
				nodeWindowId = value;
			}
		}

		/*
			show progress on node functions(unused. due to mainthread synchronization problem.)
			can not update any visual on Editor while building AssetBundles through AssetBundleGraph.
		*/
		private float progress;
		private bool running;

		public static NodeGUI CreateLoaderNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> loadPath, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				loadPath: loadPath
			);
		}

		public static NodeGUI CreateExporterNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> exportTo, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				exportTo: exportTo
			);
		}

		public static NodeGUI CreateScriptNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, string scriptClassName, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				scriptAttrNameOrClassName: scriptClassName
			);
		}

		public static NodeGUI CreateGUIFilterNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, List<string> filterContainsKeywords, List<string> filterContainsKeytypes, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				filterContainsKeywords: filterContainsKeywords,
				filterContainsKeytypes: filterContainsKeytypes
			);
		}

		public static NodeGUI CreateGUIImportNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> importerPackages, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				importerPackages: importerPackages
			);
		}

		public static NodeGUI CreateGUIModifierNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, float x, float y, string scriptClassName) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				scriptAttrNameOrClassName: scriptClassName
			);
		}

		public static NodeGUI CreateGUIGroupingNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> groupingKeyword, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				groupingKeyword: groupingKeyword
			);
		}

		public static NodeGUI CreatePrefabricatorNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, string scriptAttrNameOrClassName, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				scriptAttrNameOrClassName: scriptAttrNameOrClassName
			);
		}

		public static NodeGUI CreateBundlizerNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> bundleNameTemplate, Dictionary<string, string> variants, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				bundleNameTemplate: bundleNameTemplate,
				variants: variants
			);
		}

		public static NodeGUI CreateBundleBuilderNode (string name, string nodeId, AssetBundleGraphSettings.NodeKind kind, Dictionary<string, List<string>> enabledBundleOptions, float x, float y) {
			return new NodeGUI(
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				enabledBundleOptions: enabledBundleOptions
			);
		}

		public void AddFilterOutputPoint (int addedIndex, string keyword) {
			outputConnectionPoints.Insert(addedIndex, ConnectionPoint.OutputPoint(Guid.NewGuid().ToString(), keyword));
			Save();
			UpdateNodeRect();
		}

		public void DeleteFilterOutputPoint (int deletedIndex) {
			var deletedConnectionPoint = outputConnectionPoints[deletedIndex];
			NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, this, Vector2.zero, deletedConnectionPoint.pointId));
			outputConnectionPoints.Remove(deletedConnectionPoint);
			Save();
			UpdateNodeRect();
		}

		public void AddInputPoint (string guid, string label) {
			inputConnectionPoints.Add(ConnectionPoint.InputPoint(guid, label));
			Save();
			UpdateNodeRect();
		}

		public void DeleteInputPoint (string guid) {

			int deletedIndex = inputConnectionPoints.FindIndex( c => c.pointId == guid );

			if(deletedIndex >= 0) {
				NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, this, Vector2.zero, guid));
				inputConnectionPoints.RemoveAt(deletedIndex);
				Save();
				UpdateNodeRect();
			}
		}

		public void RenameInputPoint (string guid, string label) {
			inputConnectionPoints.ForEach( c => { if( c.pointId == guid ) {
					c.label = label; 
					NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, this, Vector2.zero, c.pointId));
				}
			});
			Save();
			UpdateNodeRect();
		}

		public void RenameFilterOutputPointLabel (int changedIndex, string latestLabel) {
			outputConnectionPoints[changedIndex].label = latestLabel;
			NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, this, Vector2.zero, outputConnectionPoints[changedIndex].pointId));
			Save();
			UpdateNodeRect();
		}

		public void BeforeSave () {
			NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_BEFORESAVE, this, Vector2.zero, null));
		}

		/**
			node's setting is changed from Inspector.
		*/
		public void Save () {
			/*
				update as no errors.
			*/
			RenewErrorSource();

			NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_SAVE, this, Vector2.zero, null));
		}

		public NodeGUI () {}

		private NodeGUI (
			string name, 
			string nodeId, 
			AssetBundleGraphSettings.NodeKind kind, 
			float x, 
			float y,
			string scriptAttrNameOrClassName = null, 
			Dictionary<string, string> loadPath = null, 
			Dictionary<string, string> exportTo = null, 
			List<string> filterContainsKeywords = null, 
			List<string> filterContainsKeytypes = null, 
			Dictionary<string, string> importerPackages = null,
			Dictionary<string, string> groupingKeyword = null,
			Dictionary<string, string> bundleNameTemplate = null,
			Dictionary<string, string> variants = null,
			Dictionary<string, List<string>> enabledBundleOptions = null
		) {
			this.nodeInsp = ScriptableObject.CreateInstance<NodeGUIInspectorHelper>();
			this.nodeInsp.hideFlags = HideFlags.DontSave;

			this.nodeWindowId = 0;
			this.name = name;
			this.nodeId = nodeId;
			this.kind = kind;
			this.scriptAttrNameOrClassName = scriptAttrNameOrClassName;
			
			if (loadPath != null) this.loadPath = new SerializablePseudoDictionary(loadPath);
			if (exportTo != null) this.exportTo = new SerializablePseudoDictionary(exportTo);
			this.filterContainsKeywords = filterContainsKeywords;
			this.filterContainsKeytypes = filterContainsKeytypes;
			if (importerPackages != null) this.importerPackages = new SerializablePseudoDictionary(importerPackages);
			if (groupingKeyword != null) this.groupingKeyword = new SerializablePseudoDictionary(groupingKeyword);
			if (bundleNameTemplate != null) this.bundleNameTemplate = new SerializablePseudoDictionary(bundleNameTemplate);
			if (variants != null) this.variants = new SerializablePseudoDictionary(variants);
			if (enabledBundleOptions != null) this.enabledBundleOptions = new SerializablePseudoDictionary2(enabledBundleOptions);

			this.baseRect = new Rect(x, y, AssetBundleGraphGUISettings.NODE_BASE_WIDTH, AssetBundleGraphGUISettings.NODE_BASE_HEIGHT);

			switch (this.kind) {
			case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
			case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}
			case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:
				{
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

			default: {
					Debug.LogError(this.name + " is defined as unknown kind of node. value:" + this.kind);
					break;
				}
			}
		}

		public NodeGUI DuplicatedNode (float newX, float newY) {

			// variants are set of <connectionId : label>
			// therefore connectionId must be regenerated.
			Dictionary<string, string> duplicatedVariants = null;
			if( this.variants != null ) {
				duplicatedVariants = new Dictionary<string, string>();
				for(int i=0; i < this.variants.Values.Count; ++i) {
					duplicatedVariants.Add(Guid.NewGuid().ToString(), this.variants.Values[i]);
				}
			}

			var duplicatedNode = new NodeGUI(
				this.name,
				Guid.NewGuid().ToString(),
				this.kind, 
				newX,
				newY,
				this.scriptAttrNameOrClassName,
				(this.loadPath != null) ? loadPath.ReadonlyDict() : null,
				(this.exportTo != null) ? this.exportTo.ReadonlyDict() : null,
				this.filterContainsKeywords,
				this.filterContainsKeytypes,
				(this.importerPackages != null) ? this.importerPackages.ReadonlyDict() : null,
				(this.groupingKeyword != null) ? this.groupingKeyword.ReadonlyDict() : null,
				(this.bundleNameTemplate != null) ? this.bundleNameTemplate.ReadonlyDict() : null,
				(duplicatedVariants != null) ? duplicatedVariants : null,
				(this.enabledBundleOptions != null) ? this.enabledBundleOptions.ReadonlyDict() : null
			);
			return duplicatedNode;
		}

		public void DeleteCurrentPackagePlatformKey (string platformPackageKey) {
			switch (this.kind) {
			case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
					loadPath.Remove(platformPackageKey);
					break;
				}

			case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					importerPackages.Remove(platformPackageKey);
					break;
				}

			case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
					IntegratedGUIModifier.DeletePlatformData(nodeId, platformPackageKey);
					break;
				}

			case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					groupingKeyword.Remove(platformPackageKey);
					break;
				}

			case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					bundleNameTemplate.Remove(platformPackageKey);
					break;
				}

			case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					enabledBundleOptions.Remove(platformPackageKey);
					break;
				}

			case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					exportTo.Remove(platformPackageKey);
					break;
				}

			default: {
					Debug.LogError(this.name + " is defined as unknown kind of node. value:" + this.kind);
					break;
				}
			}
		}

		public void SetActive () {
			nodeInsp.UpdateNode(this);
			Selection.activeObject = nodeInsp;

			switch (this.kind) {
			case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
			case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1 on";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2 on";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6 on";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3 on";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:
				{
					this.nodeInterfaceTypeStr = "flow node 4 on";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5 on";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6 on";
					break;
				}

			default: {
					Debug.LogError(this.name + " is defined as unknown kind of node. value:" + this.kind);
					break;
				}
			}
		}

		public void SetInactive () {
			switch (this.kind) {
			case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
			case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:
				{
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

			case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

			default: {
					Debug.LogError(this.name + " is defined as unknown kind of node. value:" + this.kind);
					break;
				}
			}
		}

		public void AddConnectionPoint (ConnectionPoint adding) {
			if(adding.isInput) {
				inputConnectionPoints.Insert(0, adding);
			} else {
				outputConnectionPoints.Add(adding);
			}
			UpdateNodeRect();
		}

		private void RefreshConnectionPos () {
			var inputPoints = inputConnectionPoints;
			var outputPoints = outputConnectionPoints;

			for (int i = 0; i < inputPoints.Count; i++) {
				var point = inputPoints[i];
				point.UpdatePos(i, inputPoints.Count, baseRect.width, baseRect.height);
			}

			for (int i = 0; i < outputPoints.Count; i++) {
				var point = outputPoints[i];
				point.UpdatePos(i, outputPoints.Count, baseRect.width, baseRect.height);
			}
		}

		public List<string> OutputPointLabels () {
			return outputConnectionPoints
				.Select(p => p.label)
				.ToList();
		}

		public List<string> OutputPointIds () {
			return outputConnectionPoints
				.Select(p => p.pointId)
				.ToList();
		}

		public ConnectionPoint ConnectionPointFromConPointId (string pointId) {
			var wholeConnectionPoints = WholeConnectionPoints();

			var targetPoints = wholeConnectionPoints.Where(con => con.pointId == pointId).ToList();
			if (targetPoints.Count == 0) throw new Exception("no connection point found. which should be contained pointId:" + pointId);
			return targetPoints[0];
		}

		private List<ConnectionPoint> WholeConnectionPoints () {
			var wholeConnectionPoints = new List<ConnectionPoint>();
			wholeConnectionPoints.AddRange(inputConnectionPoints);
			wholeConnectionPoints.AddRange(outputConnectionPoints);
			
			return wholeConnectionPoints;
		}

		public void DrawNode () {
			var scaledBaseRect = ScaleEffect(baseRect);

			var movedRect = GUI.Window(nodeWindowId, scaledBaseRect, DrawThisNode, string.Empty, nodeInterfaceTypeStr);

			baseRect.position = baseRect.position + (movedRect.position - scaledBaseRect.position);
		}

		public static Rect ScaleEffect (Rect nonScaledRect) {
			var scaledRect = new Rect(nonScaledRect);
			scaledRect.x = scaledRect.x * scaleFactor;
			scaledRect.y = scaledRect.y * scaleFactor;
			scaledRect.width = scaledRect.width * scaleFactor;
			scaledRect.height = scaledRect.height * scaleFactor;
			return scaledRect;
		}

		public static Vector2 ScaleEffect (Vector2 nonScaledVector2) {
			var scaledVector2 = new Vector2(nonScaledVector2.x, nonScaledVector2.y);
			scaledVector2.x = scaledVector2.x * scaleFactor;
			scaledVector2.y = scaledVector2.y * scaleFactor;
			return scaledVector2;
		}

		private void DrawThisNode(int id) {
			HandleNodeEvent ();
			DrawNodeContents();
			GUI.DragWindow();
		}

		/**
			retrieve mouse events for this node in this AssetGraoh window.
		*/
		private void HandleNodeEvent () {
			switch (Event.current.type) {
				/*
					handling release of mouse drag from this node to another node.
					this node doesn't know about where the other node is. the master only knows.
					only emit event.
				*/
			case EventType.Ignore: {
					NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_OVERED, this, Event.current.mousePosition, null));
					break;
				}

				/*
					handling drag.
				*/
			case EventType.MouseDrag: {
					NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_MOVING, this, Event.current.mousePosition, null));
					break;
				}

				/*
					check if the mouse-down point is over one of the connectionPoint in this node.
					then emit event.
				*/
			case EventType.MouseDown: {
					var connectionPoints = WholeConnectionPoints();
					var result = IsOverConnectionPoint(connectionPoints, Event.current.mousePosition);

					if (!string.IsNullOrEmpty(result)) {
						if (scaleFactor == SCALE_MAX) {
							NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, Event.current.mousePosition, result));
						}
						break;
					}
					break;
				}
			}

			/*
				retrieve mouse events for this node in|out of this AssetGraoh window.
			*/
			switch (Event.current.rawType) {
			case EventType.MouseUp: {
					var connectionPoints = WholeConnectionPoints();
					// if mouse position is on the connection point, emit mouse raised event.
					foreach (var connectionPoint in connectionPoints) {
						var globalConnectonPointRect = new Rect(connectionPoint.buttonRect.x, connectionPoint.buttonRect.y, connectionPoint.buttonRect.width, connectionPoint.buttonRect.height);
						if (globalConnectonPointRect.Contains(Event.current.mousePosition)) {
							NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED, this, Event.current.mousePosition, connectionPoint.pointId));
							return;
						}
					}

					NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_TOUCHED, this, Event.current.mousePosition, null));
					break;
				}
			}

			/*
				right click to open Context menu
			*/
			if (scaleFactor == SCALE_MAX) {
				if (Event.current.type == EventType.ContextClick || (Event.current.type == EventType.MouseUp && Event.current.button == 1)) 
				{
					var menu = new GenericMenu();
					menu.AddItem(
						new GUIContent("Delete"),
						false, 
						() => {
							NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CLOSE_TAPPED, this, Vector2.zero, null));
						}
					);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}
		}

		public void DrawConnectionInputPointMark (OnNodeEvent eventSource, bool justConnecting) {
			if (scaleFactor != SCALE_MAX) return;

			var defaultPointTex = NodeGUIUtility.inputPointMarkTex;

			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					var connectionPoint = eventSource.eventSourceNode.ConnectionPointFromConPointId(eventSource.conPointId);
					if (connectionPoint.isOutput) {
						defaultPointTex = NodeGUIUtility.enablePointMarkTex;
					}
				}
			}
			var connectionPoints = WholeConnectionPoints();

			foreach (var point in connectionPoints) {
				if (point.isInput) {
					var inputPointRect = GetInputRectForPoint(point);

					GUI.DrawTexture(
						inputPointRect, 
						defaultPointTex
					);
				}
			}
		}

		public void DrawConnectionOutputPointMark (OnNodeEvent eventSource, bool justConnecting, Event current) {
			if (scaleFactor != SCALE_MAX) return;

			var defaultPointTex = NodeGUIUtility.outputPointMarkConnectedTex;

			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					var connectionPoint = eventSource.eventSourceNode.ConnectionPointFromConPointId(eventSource.conPointId);
					if (connectionPoint.isInput) {
						defaultPointTex = NodeGUIUtility.enablePointMarkTex;
					}
				}
			}

			var globalMousePosition = current.mousePosition;

			var connectionPoints = WholeConnectionPoints();
			
			foreach (var point in connectionPoints) {
				if (point.isOutput) {
					var outputPointRect = GetOutputRectForPoint(point);

					GUI.DrawTexture(
						outputPointRect, 
						defaultPointTex
					);

					// eventPosition is contained by outputPointRect.
					if (outputPointRect.Contains(globalMousePosition)) {
						if (current.type == EventType.MouseDown) {
							NodeGUIUtility.FireNodeEvent(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, current.mousePosition, point.pointId));
						}
					}
				}
			}
		}

		private Rect GetOutputRectForPoint (ConnectionPoint outputPoint) {
			return new Rect(
				baseRect.x + baseRect.width - 8f, 
				baseRect.y + outputPoint.buttonRect.y + 1f, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE
			);
		}

		private Rect GetInputRectForPoint (ConnectionPoint inputPoint) {

			return new Rect(
				baseRect.x - 2f, 
				baseRect.y + inputPoint.buttonRect.y + 3f, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE
			);
		}

		private void DrawNodeContents () {
			var style = new GUIStyle(EditorStyles.label);
			style.alignment = TextAnchor.MiddleCenter;

			var connectionNodeStyleOutput = new GUIStyle(EditorStyles.label);
			connectionNodeStyleOutput.alignment = TextAnchor.MiddleRight;

			var connectionNodeStyleInput = new GUIStyle(EditorStyles.label);
			connectionNodeStyleInput.alignment = TextAnchor.MiddleLeft;

			var nodeTitleRect = new Rect(0, 0, baseRect.width * scaleFactor, baseRect.height * scaleFactor);
			GUI.Label(nodeTitleRect, name, style);

			if (running) {
				EditorGUI.ProgressBar(new Rect(10f, baseRect.height - 20f, baseRect.width - 20f, 10f), progress, string.Empty);
			}
			if (hasErrors) {
				GUIStyle errorStyle = new GUIStyle("CN EntryError");
				errorStyle.alignment = TextAnchor.MiddleCenter;
				var labelSize = GUI.skin.label.CalcSize(new GUIContent(name));
				EditorGUI.LabelField(new Rect((nodeTitleRect.width - labelSize.x )/2.0f - 28f, (nodeTitleRect.height-labelSize.y)/2.0f - 7f, 20f, 20f), string.Empty, errorStyle);
			}

			// draw & update connectionPoint button interface.
			if (scaleFactor == SCALE_MAX) {
				var connectionPoints = WholeConnectionPoints();
				
				foreach (var point in connectionPoints) {
					switch (this.kind) {
					case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
					case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: 
						{
							var label = point.label;
							if( label != AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL ) {
								// if point is output node, then label position offset is minus. otherwise plus.
								var xOffset = (point.isOutput) ? - baseRect.width : AssetBundleGraphGUISettings.INPUT_POINT_WIDTH;
								var labelStyle = (point.isOutput) ? connectionNodeStyleOutput : connectionNodeStyleInput;
								var labelRect = new Rect(point.buttonRect.x + xOffset, point.buttonRect.y - (point.buttonRect.height/2), baseRect.width, point.buttonRect.height*2);

								GUI.Label(labelRect, label, labelStyle);
							}
							break;
						}
					}

					if (point.isInput) {
						GUI.backgroundColor = Color.clear;
						GUI.Button(point.buttonRect, NodeGUIUtility.inputPointTex, "AnimationKeyframeBackground");
					}

					if (point.isOutput) {
						GUI.backgroundColor = Color.clear;
						GUI.Button(point.buttonRect, NodeGUIUtility.outputPointTex, "AnimationKeyframeBackground");
					}
				}
			}
		}

		public void UpdateNodeRect () {
			// UpdateNodeRect will be called outside OnGUI(), so it use inacurate but simple way to calcurate label width
			// instead of CalcSize()
			var contentLabelWordsLength = this.name.Length;
			switch (this.kind) {
			case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
			case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					var connectionPoints = WholeConnectionPoints();
					var inputLabels = connectionPoints.FindAll(con => con.isInput).OrderByDescending(con => con.label.Length).Select(con => con.label.Length).ToList();
					if (inputLabels.Any()) {
						contentLabelWordsLength = contentLabelWordsLength + 1 + inputLabels[0];
					}

					var outputLabels = connectionPoints.FindAll(con => con.isOutput).OrderByDescending(con => con.label.Length).Select(con => con.label.Length).ToList();
					if (outputLabels.Any()) {
						contentLabelWordsLength = contentLabelWordsLength + 1 + outputLabels[0];
					}

					// update node height by number of output connectionPoint.
					var outputPointCount = connectionPoints.Where(connectionPoint => connectionPoint.isOutput).ToList().Count;
					var inputPointCount  = connectionPoints.Where(connectionPoint => connectionPoint.isInput).ToList().Count;
					var larger = Mathf.Max(outputPointCount, inputPointCount);
					this.baseRect = new Rect(baseRect.x, baseRect.y, 
						baseRect.width, 
						AssetBundleGraphGUISettings.NODE_BASE_HEIGHT + (AssetBundleGraphGUISettings.FILTER_OUTPUT_SPAN * Mathf.Max(0, (larger - 1)))
					);

					break;
				}
			}

			var newWidth = Mathf.Max(AssetBundleGraphGUISettings.NODE_BASE_WIDTH, contentLabelWordsLength * 10.0f);
			baseRect = new Rect(baseRect.x, baseRect.y, newWidth, baseRect.height);

			RefreshConnectionPos();
		}

		private string IsOverConnectionPoint (List<ConnectionPoint> points, Vector2 touchedPoint) {
			foreach (var p in points) {
				if (p.buttonRect.x <= touchedPoint.x && 
					touchedPoint.x <= p.buttonRect.x + p.buttonRect.width && 
					p.buttonRect.y <= touchedPoint.y && 
					touchedPoint.y <= p.buttonRect.y + p.buttonRect.height
				) {
					return p.pointId;
				}
			}

			return string.Empty;
		}

		public Rect GetRect () {
			return baseRect;
		}

		public Vector2 GetPos () {
			return baseRect.position;
		}

		public int GetX () {
			return (int)baseRect.x;
		}

		public int GetY () {
			return (int)baseRect.y;
		}

		public int GetRightPos () {
			return (int)(baseRect.x + baseRect.width);
		}

		public int GetBottomPos () {
			return (int)(baseRect.y + baseRect.height);
		}

		public void SetPos (Vector2 position) {
			baseRect.position = position;
		}

		public void SetProgress (float val) {
			progress = val;
		}

		public void MoveRelative (Vector2 diff) {
			baseRect.position = baseRect.position - diff;
		}

		public void ShowProgress () {
			running = true;
		}

		public void HideProgress () {
			running = false;
		}

		public bool ConitainsGlobalPos (Vector2 globalPos) {
			if (baseRect.Contains(globalPos)) {
				return true;
			}
			var connectionPoints = WholeConnectionPoints();
			
			foreach (var connectionPoint in connectionPoints) {
				if (connectionPoint.isOutput) {
					var outputRect = GetOutputRectForPoint(connectionPoint);
					if (outputRect.Contains(globalPos)) {
						return true;
					}
				}
			}

			return false;
		}

		public Vector2 GlobalConnectionPointPosition(string pointId) {
			var point = ConnectionPointFromConPointId(pointId);

			var x = 0f;
			var y = 0f;

			if (point.isInput) {
				x = baseRect.x;
				y = baseRect.y + point.buttonRect.y + (point.buttonRect.height / 2f) - 1f;
			}

			if (point.isOutput) {
				x = baseRect.x + baseRect.width;
				y = baseRect.y + point.buttonRect.y + (point.buttonRect.height / 2f) - 1f;
			}

			return new Vector2(x, y);
		}

		public List<ConnectionPoint> ConnectionPointUnderGlobalPos (Vector2 globalPos) {
			var containedPoints = new List<ConnectionPoint>();
			var connectionPoints = WholeConnectionPoints();
			
			foreach (var connectionPoint in connectionPoints) {
				var grobalConnectionPointRect = new Rect(
					baseRect.x + connectionPoint.buttonRect.x,
					baseRect.y + connectionPoint.buttonRect.y,
					connectionPoint.buttonRect.width,
					connectionPoint.buttonRect.height
				);

				if (grobalConnectionPointRect.Contains(globalPos)) containedPoints.Add(connectionPoint);
				if (connectionPoint.isOutput) {
					var outputRect = GetOutputRectForPoint(connectionPoint);
					if (outputRect.Contains(globalPos)) containedPoints.Add(connectionPoint);
				}
			}

			return containedPoints;
		}

		public static void ShowTypeNamesMenu (string current, List<string> contents, Action<string> ExistSelected) {
			var menu = new GenericMenu();

			for (var i = 0; i < contents.Count; i++) {
				var type = contents[i];
				var selected = false;
				if (type == current) selected = true;

				menu.AddItem(
					new GUIContent(type),
					selected,
					() => {
						ExistSelected(type);
					}
				);
			}
			menu.ShowAsContext();
		}

		public static void ShowFilterKeyTypeMenu (string current, Action<string> ExistSelected) {
			var menu = new GenericMenu();

			for (var i = 0; i < TypeUtility.KeyTypes.Count; i++) {
				var type = TypeUtility.KeyTypes[i];
				var selected = false;
				if (type == current) selected = true;

				menu.AddItem(
					new GUIContent(type),
					selected,
					() => {
						ExistSelected(type);
					}
				);
			}
			menu.ShowAsContext();
		}
	}
}
