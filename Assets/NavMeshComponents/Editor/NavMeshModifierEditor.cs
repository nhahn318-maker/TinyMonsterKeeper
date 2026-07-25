using UnityEditor;

namespace NavMeshPlus.Components.Editors
{ 
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshModifier))]
    class NavMeshModifierEditor : Editor
    {
        SerializedProperty m_AffectedAgents;
        SerializedProperty m_Area;
        SerializedProperty m_IgnoreFromBuild;
        SerializedProperty m_OverrideArea;

        void OnEnable()
        {
            if (target == null)
                return;

            m_AffectedAgents = serializedObject.FindProperty("m_AffectedAgents");
            m_Area = serializedObject.FindProperty("m_Area");
            m_IgnoreFromBuild = serializedObject.FindProperty("m_IgnoreFromBuild");
            m_OverrideArea = serializedObject.FindProperty("m_OverrideArea");


        }



        public override void OnInspectorGUI()
        {
            if (target == null || m_IgnoreFromBuild == null || m_OverrideArea == null || m_Area == null || m_AffectedAgents == null)
                return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_IgnoreFromBuild);

            EditorGUILayout.PropertyField(m_OverrideArea);
            if (m_OverrideArea.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Area);
                EditorGUI.indentLevel--;
            }

            NavMeshComponentsGUIUtility.AgentMaskPopup("Affected Agents", m_AffectedAgents);
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
