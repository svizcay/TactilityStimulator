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
    [CreateAssetMenu(fileName = "newFingerMatrixVE", menuName = "Tactility/Virtual Electrode/Finger Matrix")]
    public class FingerMatrixVE : VirtualElectrode8
    {
        // dictionary's key is the connectorId
        // then, to get the channel for pad=3, we access the array at position pad-1
        Dictionary<int, int[]> mapping = new Dictionary<int, int[] >()
        {
            { 3, new int[]{4,5,2,3,8,6,1,7} },
            { 4, new int[]{10,14,9,12,16,11,13,15} },
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

    [CustomEditor(typeof(FingerMatrixVE))]
    public class FingerMatrixVEEditor : Editor
    {
        private VisualElement rootElem;
        private VisualTreeAsset visualTree;

        public void OnEnable()
        {
            rootElem = new VisualElement();
            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/VirtualElectrodeTemplate_2.uxml");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var virtualElectrode = target as FingerMatrixVE;

            // Debug.Log("<b>CreateInspectorGUI</b> creating GUI. Nr of pads: " + virtualElectrode.electrodes.Length);

            rootElem.Clear();

            SerializedProperty idProperty = serializedObject.FindProperty("id");
            SerializedProperty nameProperty = serializedObject.FindProperty("name");

            rootElem.Add(new UnityEditor.UIElements.PropertyField(idProperty));
            rootElem.Add(new UnityEditor.UIElements.PropertyField(nameProperty));

            VisualElement electrodesLayout = visualTree.CloneTree();

            rootElem.Add(electrodesLayout);

            for (int i = 0; i < 8; i++)
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

