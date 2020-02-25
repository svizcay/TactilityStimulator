using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Inria.Tactility.Actions;
using System.Text;

namespace Inria.Tactility
{
    public class Tangible : MonoBehaviour
    {
        #region exploratory action parameters
        [Header("Object Properties")]

        [SerializeField]
        [Range(0f, 10f)]
        private float stiffness = 10;

        /*
        [Header("VirtualElectrodes per Action")]

        [SerializeField]
        private VirtualElectrode pokingIndex;

        [SerializeField]
        private VirtualElectrode pinchIndex;

        [SerializeField]
        private VirtualElectrode pinchThumb;

        [SerializeField]
        private VirtualElectrode gripIndex;

        [SerializeField]
        private VirtualElectrode gripPalm;

        [SerializeField]
        private VirtualElectrode gripThumb;
        */

        [Header("Supported Actions")]

        [SerializeField]
        private ExploratoryAction poke = default;

        [SerializeField]
        private ExploratoryAction pinch;

        [SerializeField]
        private ExploratoryAction grip;

        #endregion exploratory action parameters

        private TactilityStimulatorManager stimManager;


        #region unity events
        private void Awake()
        {
            stimManager = FindObjectOfType<TactilityStimulatorManager>();
        }
        #endregion unity events

        #region haptic callbacks
        /**
         *Tthese messages are going to be emmited by the user hand similar to SteamVR Hand that sends OnHandEnter, OnHandHovering, etc messages
         * 
         * */
        public void OnPokingObjectStart(float interpenetrationVal)
        {
            print("poking obj");

            // send stimulation params to the stimulator manager
            // should we set here a particular velec id?
            // should we set here the velec name?
            // cathodes and anodes are NOT given by the velect. we only have here the pads not the channels
            // current intensity and pulse width should be set here! because those depend on both, comming info and material's properties

            // check first if the current object has an action defined to reach to such event
            if (poke != null)
            {
                // for each involved part of the hand, send its configuration to the stimulator manager
                // TODO: take into account same hand part might have multiple definitions of velecs (for instance, one per type of possible physical electrode type)
                // for now let's assume there is only one per hand part
                for (int i = 0; i < poke.virtualElectrodes.Length; ++i)
                {
                    // print("++++++++++++++++++++++++++");
                    // print(poke.virtualElectrodes[i].id);
                    // print(poke.virtualElectrodes[i].name);
                    // // print(poke.virtualElectrodes[i].electrodeArray.electrodes);
                    // StringBuilder builder = new StringBuilder();
                    // ElectrodeType[] values = poke.virtualElectrodes[i].GetElectrodes();
                    // for (int j = 0; j < values.Length; ++j)
                    // {
                    //     builder.Append(values[j]).Append(" ");
                    // }
                    // print(builder);
                    // int connector = 4;
                    // int[] cathodes = poke.virtualElectrodes[i].GetCathodes(connector);
                    // builder.Clear();
                    // builder.Append("cathodes = ");
                    // for (int j = 0; j < cathodes.Length; ++j)
                    // {
                    //     builder.Append(cathodes[j]).Append(" ");
                    // }
                    // print(builder);
                    // print("anodes = " + poke.virtualElectrodes[i].GetAnodes(connector));
                    
                    float newIntensity = interpenetrationVal * stiffness;
                    int newPulseWidth = 300;
                    // frequency??
                    stimManager.Add(poke.virtualElectrodes[i], poke.involvedParts[i], newIntensity, newPulseWidth);
                }

            }
        }

        public void OnPokingObjectHover(float interpenetrationVal)
        {
            print("still poking obj");
        }

        public void OnPokingObjectEnd()
        {
            print("stop poking object");
            if (poke != null)
            {
                for (int i = 0; i < poke.virtualElectrodes.Length; ++i)
                {
                    // stimManager.Remove(poke.virtualElectrodes[i].id); // Remove deprecated. use StopStim now
                    stimManager.StopStim(poke.virtualElectrodes[i].id);
                }

            }
        }
        #endregion haptic callback;
    }

}
