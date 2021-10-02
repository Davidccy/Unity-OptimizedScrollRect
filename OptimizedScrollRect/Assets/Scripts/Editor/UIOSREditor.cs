﻿using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(UIOSR< , , >))]
public class UIOSREditor<T, TContainer, TElement> : ScrollRectEditor 
    where T : OSRData
    where TContainer : UIOSRContainer<T, TElement>
    where TElement : UIOSRElement<T> {

    #region Internal Fields
    private SerializedProperty _cellSize;
    private SerializedProperty _elementGO;
    private SerializedProperty _containerSpacing;
    private SerializedProperty _elementSpacing;
    #endregion

    #region Override Methods
    protected override void OnEnable() {
        base.OnEnable();

        _cellSize = serializedObject.FindProperty("_cellSize");
        _elementGO = serializedObject.FindProperty("_elementGO");
        _containerSpacing = serializedObject.FindProperty("_containerSpacing");
        _elementSpacing = serializedObject.FindProperty("_elementSpacing");
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        serializedObject.Update();

        // Do somthing
        EditorGUILayout.PropertyField(_cellSize);
        EditorGUILayout.PropertyField(_elementGO);
        EditorGUILayout.PropertyField(_containerSpacing);
        EditorGUILayout.PropertyField(_elementSpacing);
        // Do somthing

        serializedObject.ApplyModifiedProperties();
    }
    #endregion
}
