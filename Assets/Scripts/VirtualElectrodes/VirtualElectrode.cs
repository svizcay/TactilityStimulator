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

namespace Inria.Tactility
{
    public enum ElectrodeType { NONE, CATHODE, ANODE }

    [Serializable]
    public abstract class VirtualElectrode : ScriptableObject
    {
        public int id = 10;
        public new string name = "virtualElectrode";

        /**
         * list of active channels, example if channels are M1 and M8
         * in finger matrix array connected to connect 3
         * that will give us channels 4 and 7
         * and the list of ints will be {4,7} (active channels to be used in the stim)
         * */
        public abstract int[] GetCathodes(int connectorId);

        public abstract uint GetAnodes(int connectorId);
        public abstract ElectrodeType[] GetElectrodes();
    }

    [Serializable]
    public abstract class VirtualElectrode8 : VirtualElectrode
    {
        [SerializeField]
        public ElectrodeType[] electrodes = new ElectrodeType[8];

        public override ElectrodeType[] GetElectrodes()
        {
            return electrodes;
        }
    }

    [Serializable]
    public abstract class VirtualElectrode16 : VirtualElectrode
    {
        [SerializeField]
        public ElectrodeType[] electrodes = new ElectrodeType[16];

        public override ElectrodeType[] GetElectrodes()
        {
            return electrodes;
        }
    }
}

