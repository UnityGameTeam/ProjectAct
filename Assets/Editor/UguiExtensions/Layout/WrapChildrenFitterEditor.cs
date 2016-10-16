//******************************
//
// 模块名   : WrapChildrenFitterEditor
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : WrapChildrenFitter的编辑器扩展
//
//******************************

using UnityEditor;
using UnityEngine;

namespace UguiExtensions
{
    [CustomEditor(typeof (WrapChildrenFitter), true)]
    [CanEditMultipleObjects]
    public class WrapChildrenFitterEditor : Editor
    {
        private SerializedProperty m_Padding;
        private SerializedProperty m_HorizontalFit;
        private SerializedProperty m_VerticalFit;

        private SerializedProperty m_SelfSizeWidthRatio;
        private SerializedProperty m_SelfSizeHeightRatio;
        private SerializedProperty m_FixedSize;
        private SerializedProperty m_MaxWidthRatio;
        private SerializedProperty m_MinWidthRatio;
        private SerializedProperty m_MaxHeightRatio;
        private SerializedProperty m_MinHeightRatio;
        private SerializedProperty m_LayoutGroupComplete;

        private SerializedProperty m_LimitPadding;
        private SerializedProperty m_NeedRecalculate;

        protected virtual void OnEnable()
        {
            m_Padding = serializedObject.FindProperty("m_Padding");
            m_HorizontalFit = serializedObject.FindProperty("m_HorizontalFit");
            m_VerticalFit = serializedObject.FindProperty("m_VerticalFit");

            m_SelfSizeWidthRatio = serializedObject.FindProperty("m_SelfSizeWidthRatio");
            m_SelfSizeHeightRatio = serializedObject.FindProperty("m_SelfSizeHeightRatio");
            m_FixedSize = serializedObject.FindProperty("m_FixedSize");
            m_MaxWidthRatio = serializedObject.FindProperty("m_MaxWidthRatio");
            m_MinWidthRatio = serializedObject.FindProperty("m_MinWidthRatio");
            m_MaxHeightRatio = serializedObject.FindProperty("m_MaxHeightRatio");
            m_MinHeightRatio = serializedObject.FindProperty("m_MinHeightRatio");

            m_LayoutGroupComplete = serializedObject.FindProperty("m_LayoutGroupComplete");
            m_LimitPadding = serializedObject.FindProperty("m_LimitPadding");
            m_NeedRecalculate = serializedObject.FindProperty("m_NeedRecalculate");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var targetObject = serializedObject.targetObject as WrapChildrenFitter;

            EditorGUILayout.PropertyField(m_Padding, true);
            EditorGUILayout.PropertyField(m_LimitPadding, true);
            EditorGUILayout.PropertyField(m_HorizontalFit, true);
            EditorGUILayout.PropertyField(m_VerticalFit, true);

            EditorGUILayout.PropertyField(m_SelfSizeWidthRatio, true);
            if (targetObject.selfSizeWidthRatio != WrapChildrenFitter.SelfSizeRatioMode.Unconstrained)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Slider(m_MinWidthRatio, 0,1);
                EditorGUILayout.Slider(m_MaxWidthRatio, 0,1);
                EditorGUILayout.EndHorizontal();

                targetObject.minWidthRatio = Mathf.Min(targetObject.minWidthRatio, targetObject.maxWidthRatio);
                targetObject.maxWidthRatio = Mathf.Max(targetObject.minWidthRatio, targetObject.maxWidthRatio);
            }

            EditorGUILayout.PropertyField(m_SelfSizeHeightRatio, true);
            if (targetObject.selfSizeHeightRatio != WrapChildrenFitter.SelfSizeRatioMode.Unconstrained)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Slider(m_MinHeightRatio, 0, 1);
                EditorGUILayout.Slider(m_MaxHeightRatio, 0, 1);
                EditorGUILayout.EndHorizontal();

                targetObject.minHeightRatio = Mathf.Min(targetObject.minHeightRatio, targetObject.maxHeightRatio);
                targetObject.maxHeightRatio = Mathf.Max(targetObject.minHeightRatio, targetObject.maxHeightRatio);
            }

            if (targetObject.selfSizeWidthRatio == WrapChildrenFitter.SelfSizeRatioMode.FixedSize ||
                targetObject.selfSizeHeightRatio == WrapChildrenFitter.SelfSizeRatioMode.FixedSize)
            {
                EditorGUILayout.PropertyField(m_FixedSize, true);
            }

            EditorGUILayout.PropertyField(m_LayoutGroupComplete, true);
            EditorGUILayout.PropertyField(m_NeedRecalculate, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}