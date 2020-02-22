using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;   // for VisualElement

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;
#endif

namespace Inria.Tactility.VirtualElectrodes
{
    [CreateAssetMenu(fileName = "newHandConcentricVE", menuName = "Tactility/Virtual Electrode/Hand Concentric")]
    public class HandConcentricVE : VirtualElectrode16
    {
        Dictionary<int, int[]> mapping = new Dictionary<int, int[] >()
        {
            { 1, new int[]{29,24,20,30,26,23,19,31,27,22,18,32,28,21,17,25} },
        };

        public override int[] GetCathodes(int connectorId)
        {
            List<int> channels = new List<int>();

            if (!mapping.ContainsKey(connectorId)) throw new Exception(GetType().Name + " should not be connected in connector nr " + connectorId);

            for (int i = 0; i < electrodes.Length; ++i)
            {
                if (electrodes[i] == ElectrodeType.CATHODE)
                {
                    channels.Add(mapping[connectorId][i]);
                }
            }

            return channels.ToArray();
        }

        public override uint GetAnodes(int connectorId)
        {
            if (!mapping.ContainsKey(connectorId)) throw new Exception(GetType().Name + " should not be connected in connector nr " + connectorId);

            uint result = 0;

            for (int i = 0; i < electrodes.Length; ++i)
            {
                if (electrodes[i] == ElectrodeType.ANODE)
                {
                    result += (uint) Math.Pow(2,  mapping[connectorId][i] - 1);
                }
            }

            return result;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(HandConcentricVE))]
    public class HandConcentricVEEditor : Editor
    {
        private VisualElement rootElem;
        private VisualTreeAsset visualTree;

        public void OnEnable()
        {
            rootElem = new VisualElement();

            // apart from the internal stylesheet, we can add even more
            rootElem.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/VirtualElectrodeCommon.uss"));

            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/VirtualElectrodeHandConcentric.uxml");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var virtualElectrode = target as HandConcentricVE;

            // Debug.Log("<b>CreateInspectorGUI</b> creating GUI. Nr of pads: " + virtualElectrode.electrodes.Length);

            rootElem.Clear();

            SerializedProperty idProperty = serializedObject.FindProperty("id");
            SerializedProperty nameProperty = serializedObject.FindProperty("name");

            rootElem.Add(new UnityEditor.UIElements.PropertyField(idProperty));
            rootElem.Add(new UnityEditor.UIElements.PropertyField(nameProperty));

            VisualElement electrodesLayout = visualTree.CloneTree();

            rootElem.Add(electrodesLayout);

            for (int i = 0; i < 16; i++)
            {
                EnumField enumField = electrodesLayout.Query<EnumField>(name: "M" + (i+1) );
                enumField.Init(virtualElectrode.electrodes[i]);
                switch (virtualElectrode.electrodes[i])
                {
                    case ElectrodeType.CATHODE:
                        enumField.EnableInClassList("cathode", true);
                        enumField.EnableInClassList("anode", false);
                        break;
                    case ElectrodeType.ANODE:
                        enumField.EnableInClassList("cathode", false);
                        enumField.EnableInClassList("anode", true);
                        break;
                    case ElectrodeType.NONE:
                        enumField.EnableInClassList("cathode", false);
                        enumField.EnableInClassList("anode", false);
                        break;
                }
            }

            electrodesLayout.Query<EnumField>().ForEach(enumField =>
            {
                int index = int.Parse(enumField.name.Substring(1));

                enumField.RegisterCallback< ChangeEvent<Enum> >(evnt =>
                {
                    // change serializedObject / serializedProperty and not object diretly.
                    // that will allow the system to save and undo changes
                    // serializedObject.ApplyModifiedProperties();
                    serializedObject.FindProperty("electrodes").GetArrayElementAtIndex(index - 1).enumValueIndex = (int) (ElectrodeType)(evnt.newValue);
                    serializedObject.ApplyModifiedProperties();

                    // update visual
                    switch ((ElectrodeType)evnt.newValue)
                    {
                        case ElectrodeType.CATHODE:
                            enumField.EnableInClassList("cathode", true);
                            enumField.EnableInClassList("anode", false);
                            break;
                        case ElectrodeType.ANODE:
                            enumField.EnableInClassList("cathode", false);
                            enumField.EnableInClassList("anode", true);
                            break;
                        case ElectrodeType.NONE:
                            enumField.EnableInClassList("cathode", false);
                            enumField.EnableInClassList("anode", false);
                            break;
                    }

                    enumField.MarkDirtyRepaint();
                });

            });

            return rootElem;
        }

    }

#endif
}
