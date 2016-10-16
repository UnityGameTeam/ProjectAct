using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	[Serializable] 
	public class ConnectionGUI {
		[SerializeField] public string label;
		[SerializeField] public string connectionId;

		[SerializeField] public string outputNodeId;
		[SerializeField] public ConnectionPoint outputPoint;

		[SerializeField] public string inputNodeId;
		[SerializeField] public ConnectionPoint inputPoint;

		[SerializeField] public ConnectionGUIInspectorHelper conInsp;

		[SerializeField] private string connectionButtonStyle;

		private Rect buttonRect;

		public static ConnectionGUI LoadConnection (string label, string connectionId, string startNodeId, ConnectionPoint output, string endNodeId, ConnectionPoint input) {
			return new ConnectionGUI(
				label,
				connectionId,
				startNodeId,
				output,
				endNodeId,
				input
			);
		}

		public static ConnectionGUI NewConnection (string label, string startNodeId, ConnectionPoint output, string endNodeId, ConnectionPoint input) {
			return new ConnectionGUI(
				label,
				Guid.NewGuid().ToString(),
				startNodeId,
				output,
				endNodeId,
				input
			);
		}

		private ConnectionGUI (string label, string connectionId, string startNodeId, ConnectionPoint output, string endNodeId, ConnectionPoint input) {
			conInsp = ScriptableObject.CreateInstance<ConnectionGUIInspectorHelper>();
			conInsp.hideFlags = HideFlags.DontSave;

			this.label = label;
			this.connectionId = connectionId;

			this.outputNodeId = startNodeId;
			this.outputPoint = output;
			this.inputNodeId = endNodeId;
			this.inputPoint = input;

			connectionButtonStyle = "sv_label_0";
		}

		/**
			Inspector GUI for this connection.
		*/
		[CustomEditor(typeof(ConnectionGUIInspectorHelper))]
		public class ConnectionObj : Editor {

			public override bool RequiresConstantRepaint() {
				return true;
			}

			public override void OnInspectorGUI () {
				var con = ((ConnectionGUIInspectorHelper)target).con;
				if (con == null) return;
				

				var foldouts = ((ConnectionGUIInspectorHelper)target).foldouts;
				

				var count = 0;
				var throughputListDict = ((ConnectionGUIInspectorHelper)target).throughputListDict;
				if (throughputListDict == null)  return;

				foreach (var throughputList in throughputListDict.Values) {
					count += throughputList.Count;
				}

				EditorGUILayout.LabelField("Total", count.ToString());
				
				var redColor = new GUIStyle(EditorStyles.label);
				redColor.normal.textColor = Color.gray;
		 
				var index = 0;
				foreach (var groupKey in throughputListDict.Keys) {
					var throughputList = throughputListDict[groupKey];

					var foldout = foldouts[index];
					
					foldout = EditorGUILayout.Foldout(foldout, "Group Key:" + groupKey);
					if (foldout) {
						EditorGUI.indentLevel = 1;
						for (var i = 0; i < throughputList.Count; i++) {
							var sourceStr = throughputList[i].path;
							var isBundled = throughputList[i].isBundled;
							
							if (isBundled) EditorGUILayout.LabelField(sourceStr, redColor); 
							else EditorGUILayout.LabelField(sourceStr);
						}
						EditorGUI.indentLevel = 0;
					}
					foldouts[index] = foldout;

					index++;
				}
			}
		}

		public Rect GetRect () {
			return buttonRect;
		}
		
		/**
			throughputListDict contains:
				group/
					throughput assets
		*/
		public void DrawConnection (List<NodeGUI> nodes, Dictionary<string, List<DepreacatedThroughputAsset>> throughputListDict) {
			var startNodes = nodes.Where(node => node.nodeId == outputNodeId).ToList();
			if (!startNodes.Any()) return;
			
			var startPoint = startNodes[0].GlobalConnectionPointPosition(outputPoint.pointId);
			startPoint = NodeGUI.ScaleEffect(startPoint);
			var startV3 = new Vector3(startPoint.x, startPoint.y, 0f);

			var endNodes = nodes.Where(node => node.nodeId == inputNodeId).ToList();
			if (!endNodes.Any()) return;

			var endPoint = endNodes[0].GlobalConnectionPointPosition(inputPoint.pointId);
			endPoint = NodeGUI.ScaleEffect(endPoint);
			var endV3 = new Vector3(endPoint.x, endPoint.y + 1f, 0f);
			
			var centerPoint = startPoint + ((endPoint - startPoint) / 2);
			var centerPointV3 = new Vector3(centerPoint.x, centerPoint.y, 0f);

			var pointDistance = (endPoint.x - startPoint.x) / 3f;
			if (pointDistance < AssetBundleGraphGUISettings.CONNECTION_CURVE_LENGTH) pointDistance = AssetBundleGraphGUISettings.CONNECTION_CURVE_LENGTH;

			var startTan = new Vector3(startPoint.x + pointDistance, startPoint.y, 0f);
			var endTan = new Vector3(endPoint.x - pointDistance, endPoint.y, 0f);

			Handles.DrawBezier(startV3, endV3, startTan, endTan, Color.gray, null, 4f);

			// draw connection label if connection's label is not normal.
			if (NodeGUI.scaleFactor == NodeGUI.SCALE_MAX) {
				switch (label){
					case AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL: {
						// show nothing
						break;
					}
					
					case AssetBundleGraphSettings.BUNDLIZER_BUNDLE_OUTPUTPOINT_LABEL: {
						var labelPointV3 = new Vector3(centerPointV3.x - ((AssetBundleGraphSettings.BUNDLIZER_BUNDLE_OUTPUTPOINT_LABEL.Length * 6f) / 2), centerPointV3.y - 24f, 0f) ;
						Handles.Label(labelPointV3, AssetBundleGraphSettings.BUNDLIZER_BUNDLE_OUTPUTPOINT_LABEL);
						break;
					}

					default: {
						var labelPointV3 = new Vector3(centerPointV3.x - ((label.Length * 7f) / 2), centerPointV3.y - 24f, 0f) ;
						Handles.Label(labelPointV3, label);
						break;
					}
				}
			}

			// draw connection arrow.
			if (NodeGUI.scaleFactor == NodeGUI.SCALE_MAX) {
				GUI.DrawTexture(
					new Rect(
						endV3.x - AssetBundleGraphGUISettings.CONNECTION_ARROW_WIDTH + 4f, 
						endV3.y - (AssetBundleGraphGUISettings.CONNECTION_ARROW_HEIGHT / 2f) - 1f, 
						AssetBundleGraphGUISettings.CONNECTION_ARROW_WIDTH, 
						AssetBundleGraphGUISettings.CONNECTION_ARROW_HEIGHT
					), 
					ConnectionGUIUtility.connectionArrowTex
				);
			}

			/*
				draw throughtput badge.
			*/
			var throughputCount = 0;
			foreach (var list in throughputListDict.Values) {
				throughputCount += list.Count;
			}
			
			var offsetSize = throughputCount.ToString().Length * 20f;
			
			buttonRect = new Rect(centerPointV3.x - offsetSize/2f, centerPointV3.y - 7f, offsetSize, 20f);

			if (
				Event.current.type == EventType.ContextClick
				|| (Event.current.type == EventType.MouseUp && Event.current.button == 1)
			) {
				var rightClickPos = Event.current.mousePosition;
				if (buttonRect.Contains(rightClickPos)) {
					var menu = new GenericMenu();
					menu.AddItem(
						new GUIContent("Delete"),
						false, 
						() => {
							Delete();
						}
					);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}

			if (GUI.Button(buttonRect, throughputCount.ToString(), connectionButtonStyle)) {
				conInsp.UpdateCon(this, throughputListDict);
				ConnectionGUIUtility.FireNodeEvent(new OnConnectionEvent(OnConnectionEvent.EventType.EVENT_CONNECTION_TAPPED, this));
			}
		}

		public bool IsStartAtConnectionPoint (ConnectionPoint p) {
			return outputPoint == p;
		}

		public bool IsEndAtConnectionPoint (ConnectionPoint p) {
			return inputPoint == p;
		}

		public bool IsSameDetail (NodeGUI start, ConnectionPoint output, NodeGUI end, ConnectionPoint input) {
			if (
				outputNodeId == start.nodeId &&
				outputPoint == output && 
				inputNodeId == end.nodeId &&
				inputPoint == input
			) {
				return true;
			}
			return false;
		}
		
		public void SetActive () {
			Selection.activeObject = conInsp;
			connectionButtonStyle = "sv_label_1";
		}

		public void SetInactive () {
			connectionButtonStyle = "sv_label_0";
		}

		public void Delete () {
			ConnectionGUIUtility.FireNodeEvent(new OnConnectionEvent(OnConnectionEvent.EventType.EVENT_CONNECTION_DELETED, this));
		}
	}

	public static class NodeEditor_ConnectionListExtension {
		public static bool ContainsConnection(this List<ConnectionGUI> connections, NodeGUI start, ConnectionPoint output, NodeGUI end, ConnectionPoint input) {
			foreach (var con in connections) {
				if (con.IsSameDetail(start, output, end, input)) return true;
			}
			return false;
		}
	}
}