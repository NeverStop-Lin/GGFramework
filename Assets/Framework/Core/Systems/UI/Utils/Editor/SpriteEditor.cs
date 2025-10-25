namespace Framework.CoreTools.Editor
{
    [UnityEditor.CustomEditor(typeof(Sprite))]
    public class SpriteEditor : UnityEditor.Editor
    {
        private UnityEditor.SerializedProperty _selectedOptionProp;

        private void OnEnable()
        {
            _selectedOptionProp = serializedObject.FindProperty("selectedOption");
        }

        public override void OnInspectorGUI()
        {
            // 只显示自定义选项
            UnityEditor.EditorGUILayout.PropertyField(_selectedOptionProp);
            serializedObject.ApplyModifiedProperties();
        }
    }
}