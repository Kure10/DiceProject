using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;

[CustomEditor(typeof(DiceGameController))]
public class GameSystemEditor : Editor
{
    public bool ShowMainMenu = false;

    //AnimBool customizedValues;

    //float enemyScale = 1f;

    //SerializedProperty m_IntProp;
    //SerializedProperty m_VectorProp;
    //SerializedProperty m_GameObjectProp;

    //SerializedProperty _myBoolSP;

    private void OnEnable()
    {
        
        //// Fetch the objects from the GameObject script to display in the inspector
        //m_IntProp = serializedObject.FindProperty("_easyDisciplineCount");
        //m_VectorProp = serializedObject.FindProperty("m_MyVector");
        //m_GameObjectProp = serializedObject.FindProperty("m_MyGameObject");

        //_myBoolSP = serializedObject.FindProperty("_newThrowStart");

    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
       // DiceGameController dgc = (DiceGameController)target;
        var myScript = target as DiceGameController;

        ShowMainMenu = GUILayout.Toggle(ShowMainMenu, "Show List Of Disciplines");

        if (ShowMainMenu)
        {
            EditorGUILayout.Space();
            EditorGUI.indentLevel++;
            
            var myEnumMemberCount = Enum.GetNames(typeof(DiceGameController.DisciplineCondition)).Length;
            var justForEnumList = DiceGameController.DisciplineCondition.ones;

            for (int i = 0; i < myEnumMemberCount; i++)
            {
                justForEnumList = (DiceGameController.DisciplineCondition)i;
                EditorGUILayout.LabelField(justForEnumList.ToString());
            }


           // _myBoolSP.boolValue = EditorGUILayout.Toggle("A Boolean", _myBoolSP.boolValue);

            // EditorGUILayout.LabelField(serializedObject.FindProperty("_menuPanel").objectReferenceValue);
            // EditorGUILayout.PropertyField(myScript.ForEditor, new GUIContent("Vector Object"));
            // myScript.k = EditorGUILayout.IntSlider("I field:", myScript.k, 1, 2);

            // EditorGUILayout.ObjectField(serializedObject.FindProperty("_menuPanel").objectReferenceValue);


            EditorGUI.indentLevel--;
        }

        //myScript.show = GUILayout.Toggle(myScript.show, "show");

        //if (myScript.show)
        //{
        //    myScript.k = EditorGUILayout.IntSlider("I field:", myScript.k, 1, 100);
        //    EditorGUILayout.PropertyField(m_IntProp, new GUIContent("Int Field"), GUILayout.Height(20));
        //    //  EditorGUILayout.PropertyField(m_VectorProp, new GUIContent("Vector Object"));
        //    // EditorGUILayout.PropertyField(m_GameObjectProp, new GUIContent("Game Object"));


        //}


        // myScript.i = EditorGUILayout.IntSlider("I field:", myScript.i, 1, 100);

        //customizedValues.target = EditorGUILayout.ToggleLeft("CustomShow", customizedValues.target);

        //if (EditorGUILayout.BeginFadeGroup(customizedValues.faded))
        //{
        //    EditorGUI.indentLevel++;

        //    enemyScale = EditorGUILayout.FloatField("Size Scale", enemyScale);

        //    EditorGUI.indentLevel--;
        //}

        //if(GUILayout.Button)
        //{

        //}

        serializedObject.ApplyModifiedProperties();
    }
}
