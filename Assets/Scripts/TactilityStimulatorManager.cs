using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using System.Text;

namespace Inria.Tactility
{
    public static class StimulationConstants
    {
        // in mA
        public const float MIN_INTENSITY = 0f;
        public const float MAX_INTENSITY = 9f;

        // in hz
        public const int MIN_FREQUENCY = 1;
        public const int MAX_FREQUENCY = 200;

        // in micro seconds
        public const int MIN_PULSE_WIDTH = 30;
        public const int MAX_PULSE_WIDTH = 500;

    }

    public class Stimulation
    {
        public int      id;     // velec id
        public string   name;
        public bool     active;    // stimulator is executing it
        public float    intensity;    // current value of intensity
        public int      pulseWidth;    // current value of pulse width

        // they are already calculated for a specific connector
        public int[]    cathodes; 
        public uint     anodes; //

        public Stimulation (int id, string name, float intensity, int pulseWidth, int[] cathodes, uint anodes)
        {
            this.id = id;
            this.name = name;
            this.active = true;
            this.intensity = intensity;
            this.pulseWidth = pulseWidth;
            this.cathodes = cathodes;
            this.anodes = anodes;
        }

        /*
         * TODO: try to catch string if it's not modified that frequently or if it only changes few parameters
         * */ 
        public string GetStimCommand ()
        {
            StringBuilder builder = new StringBuilder();

            List<int> cathodesList = new List<int>(cathodes);

            builder.Append("velec ").Append(id.ToString())
                .Append(" *name ").Append(name)
                .Append(" *elec 1")
                .Append(" *cathodes ");
            for (int i = 1; i <= 32; ++i)
            {
                if (cathodesList.Contains(i))
                {
                    builder.Append(i.ToString() + "=1,");//trailing comma doesn't not matter at the end
                } else
                {
                    builder.Append(i.ToString() + "=0,");//trailing comma doesn't not matter at the end
                }
            }

            builder.Append(" *amp ");
            float finalIntensity = intensity;
            if (intensity < StimulationConstants.MIN_INTENSITY || intensity > StimulationConstants.MAX_INTENSITY)
            {
                Debug.LogWarning("clamping intensity to ["
                    + StimulationConstants.MIN_INTENSITY + "," + StimulationConstants.MAX_INTENSITY +
                    "] range for stim id=" + id + " name=" + name);

                finalIntensity = Mathf.Clamp(intensity, StimulationConstants.MIN_INTENSITY, StimulationConstants.MAX_INTENSITY);
            }

            for (int i = 1; i <= 32; ++i)
            {
                if (cathodesList.Contains(i))
                {
                    builder.Append(i.ToString() + "=" + finalIntensity + ",");//trailing comma doesn't not matter at the end
                } else
                {
                    builder.Append(i.ToString() + "=0,");//trailing comma doesn't not matter at the end
                }
            }

            builder.Append(" *width ");

            int finalPulseWidth = pulseWidth;
            if (pulseWidth < StimulationConstants.MIN_PULSE_WIDTH || pulseWidth > StimulationConstants.MAX_PULSE_WIDTH)
            {
                Debug.LogWarning("clamping pulseWidth to ["
                    + StimulationConstants.MIN_PULSE_WIDTH + "," + StimulationConstants.MAX_PULSE_WIDTH +
                    "] range for stim id=" + id + " name=" + name);

                finalPulseWidth = Mathf.Clamp(pulseWidth, StimulationConstants.MIN_PULSE_WIDTH, StimulationConstants.MAX_PULSE_WIDTH);
            }
            for (int i = 1; i <= 32; ++i)
            {
                if (cathodesList.Contains(i))
                {
                    builder.Append(i.ToString() + "=" + finalPulseWidth + ",");//trailing comma doesn't not matter at the end
                } else
                {
                    builder.Append(i.ToString() + "=0,");//trailing comma doesn't not matter at the end
                }
            }

            builder.Append(" *anode " + anodes);
            if (active)
            {
                builder.Append(" *selected 1");
            } else
            {
                builder.Append(" *selected 0");
            }

            builder.Append(" *sync 0");

            return builder.ToString();
        }
    }

    public enum Connector1ElectrodeType { None, HandConcentric }
    public enum Connector2ElectrodeType { None, HandMatrix }
    public enum Connector3ElectrodeType { None, FingerCircular, FingerMatrix }
    public enum Connector4ElectrodeType { None, FingerCircular, FingerMatrix }

    public enum Connector1HandPart { None, Palm, Dorsal }
    public enum Connector2HandPart { None, Palm, Dorsal }
    public enum Connector3HandPart { None, Index, Thumb }
    public enum Connector4HandPart { None, Index, Thumb }
    public class TactilityStimulatorManager : MonoBehaviour
    {

        #region general settings
        [Header("General Settings")]

        [SerializeField]
        private bool verbose = true;

        [SerializeField]
        private int initialFrequency = 35;

        #endregion general setting

        #region port settings
        [Header("Port Settings")]

        [SerializeField]
        private string portName = "COM5";

        [SerializeField]
        [Tooltip("time out in ms to try to read back from bluetooth device")]
        private int readTimeout = 1000; // 1 sec
        #endregion port settings

        #region connectors
        [Header("Settings - Electrodes Physical Connection")]

        [SerializeField]
        [Tooltip("Electrode connected in Connector 1 (only hand concentric)")]
        private Connector1ElectrodeType connector1 = Connector1ElectrodeType.None;

        [SerializeField]
        [Tooltip("Electrode connected in Connector 2 (only hand matrix)")]
        private Connector2ElectrodeType connector2 = Connector2ElectrodeType.None;

        [SerializeField]
        [Tooltip("Electrode connected in Connector 3 (figer circular or finger matrix)")]
        private Connector3ElectrodeType connector3 = Connector3ElectrodeType.None;

        [SerializeField]
        [Tooltip("Electrode connected in Connector 4 (figer circular or finger matrix)")]
        private Connector4ElectrodeType connector4 = Connector4ElectrodeType.None;

        [Header("Settings - Electrodes Hand Placement")]

        [SerializeField]
        [Tooltip("Wheter electrode in connector 1 is connected to palm or dorsal")]
        private Connector1HandPart connector1HandPart = Connector1HandPart.None;

        [SerializeField]
        [Tooltip("Wheter electrode in connector 2 is connected to palm or dorsal")]
        private Connector2HandPart connector2HandPart = Connector2HandPart.None;

        [SerializeField]
        [Tooltip("Wheter electrode in connector 3 is connected to index or thumb")]
        private Connector3HandPart connector3HandPart = Connector3HandPart.None;

        [SerializeField]
        [Tooltip("Wheter electrode in connector 4 is connected to index or thumb")]
        private Connector4HandPart connector4HandPart = Connector4HandPart.None;

        #endregion connectors

        #region debugging information
        [Header("Debugging Info")]

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private bool running = false;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private int frequency;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private string batteryLevel;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private string hardware;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private string firmware;

        #endregion debugging information


        #region port constant values
        private const int portBaudRate = 115200;
        // 8-N-1
        private const int portDataBits = 8;
        private const Parity portParity = Parity.None;
        private const StopBits portStopBits = StopBits.One;
        #endregion por constant values

        private SerialPort port;

        // private Dictionary<string, Stimulation> stimulations = new Dictionary<string, Stimulation>();
        private Dictionary<int, Stimulation> stimulations = new Dictionary<int, Stimulation>();

        private string deviceAnswer;

        #region unity events
        private void OnEnable()
        {
            port = new SerialPort(portName, portBaudRate, portParity, portDataBits, portStopBits);
            port.ReadTimeout = readTimeout;//maybe needs to be set before opening port

            try
            {
                port.Open();
                if (verbose) print("[" + this.GetType().Name + "] Connected to bluetooth device");
            } catch (Exception e)
            {
                Debug.LogError("Error trying to open port " + portName + ": " + e.Message);
            }

            InitStimulator();
        }

        private void OnDisable()
        {
            port.WriteLine("stim off");
            port.Close();
        }

        #endregion unity events

        #region public API
        // TODO: experimental feature remove later
        public void Add (Stimulation stim)
        {
            if (stimulations.ContainsKey(stim.id))
            {
                // update (do i need it? even if i'm using stim directly?)

                // check if values are new, if they are, update command and star again stim
                print(stim.GetStimCommand());
                port.WriteLine(stim.GetStimCommand()); // write velec config
                port.WriteLine("stim " + stim.name);    // start stim (if active is false, maybe it wont play)

            } else
            {
                // insert (no need to check for connectors (there are hard-coded)
                stimulations.Add(stim.id, stim); // set active as true in ctor
                print(stim.GetStimCommand());
                port.WriteLine(stim.GetStimCommand()); // write velec config
                port.WriteLine("stim " + stim.name);    // start stim
            }

        }
        public void Add(VirtualElectrode virtualElectrode, Actions.HandPart handPart, float intensity, int pulseWidth)
        {
            if (stimulations.ContainsKey(virtualElectrode.id))
            {
                // update stim params

                // check if intensity or pulse width are new
                bool newIntensity = !Mathf.Approximately(stimulations[virtualElectrode.id].intensity, intensity);
                bool newPulseWidth = (stimulations[virtualElectrode.id].pulseWidth != pulseWidth);

                if (newIntensity || newPulseWidth || stimulations[virtualElectrode.id].active == false)
                {
                    // update values and send new command to stimualtor
                    stimulations[virtualElectrode.id].active = true;
                    stimulations[virtualElectrode.id].intensity = intensity;
                    stimulations[virtualElectrode.id].pulseWidth = pulseWidth;

                    print(stimulations[virtualElectrode.id].GetStimCommand());
                    port.WriteLine(stimulations[virtualElectrode.id].GetStimCommand());

                    port.WriteLine("stim " + stimulations[virtualElectrode.id].name);
                }


            } else
            {
                // add new entry to list of stims

                // find if there is a electrode connected for the right part of the hand
                // that this stimulus was created
                int validConnector = -1;
                if (handPart == Actions.HandPart.Palm)
                {
                    // try to find if there is an electrode targeting the palm (electrodes connected in connector 1 or 2)
                    bool connector1IsValid = (connector1 != Connector1ElectrodeType.None) && (connector1HandPart == Connector1HandPart.Palm);
                    bool connector2IsValid = (connector2 != Connector2ElectrodeType.None) && (connector2HandPart == Connector2HandPart.Palm);

                    if (!(connector1IsValid || connector2IsValid)) throw new Exception("No electrode connected to stimulate " + handPart);

                    validConnector = (connector1IsValid) ? 1 : 2;
                } else if (handPart == Actions.HandPart.Dorsal)
                {
                    bool connector1IsValid = (connector1 != Connector1ElectrodeType.None) && (connector1HandPart == Connector1HandPart.Dorsal);
                    bool connector2IsValid = (connector2 != Connector2ElectrodeType.None) && (connector2HandPart == Connector2HandPart.Dorsal);

                    if (!(connector1IsValid || connector2IsValid)) throw new Exception("No electrode connected to stimulate " + handPart);

                    validConnector = (connector1IsValid) ? 1 : 2;
                } else if (handPart == Actions.HandPart.Index)
                {
                    bool connector3IsValid = (connector3 != Connector3ElectrodeType.None) && (connector3HandPart == Connector3HandPart.Index);
                    bool connector4IsValid = (connector4 != Connector4ElectrodeType.None) && (connector4HandPart == Connector4HandPart.Index);

                    if (!(connector3IsValid || connector4IsValid)) throw new Exception("No electrode connected to stimulate " + handPart);

                    validConnector = (connector3IsValid) ? 3 : 4;
                } else if (handPart == Actions.HandPart.Thumb)
                {
                    bool connector3IsValid = (connector3 != Connector3ElectrodeType.None) && (connector3HandPart == Connector3HandPart.Thumb);
                    bool connector4IsValid = (connector4 != Connector4ElectrodeType.None) && (connector4HandPart == Connector4HandPart.Thumb);

                    if (!(connector3IsValid || connector4IsValid)) throw new Exception("No electrode connected to stimulate " + handPart);

                    validConnector = (connector3IsValid) ? 3 : 4;
                }

                Stimulation stim = new Stimulation(
                    virtualElectrode.id,
                    virtualElectrode.name,
                    intensity,
                    pulseWidth,
                    virtualElectrode.GetCathodes(validConnector),
                    virtualElectrode.GetAnodes(validConnector)
                );

                stimulations.Add(virtualElectrode.id, stim);

                print(stim.GetStimCommand());
                port.WriteLine(stim.GetStimCommand());
                port.WriteLine("stim " + stim.name);

            }

        }

        public void Remove (int id)
        {
            if (stimulations.ContainsKey(id))
            {
                stimulations[id].active = false;
            }
            port.WriteLine("velec " + id.ToString() + " *selected 0");
        }

        public void StopAll()
        {
            port.WriteLine("stim off");
        }

        public void SetFrequency (int val)
        {
            frequency = val;
            port.WriteLine("freq " + val);
        }
        #endregion public API

        #region private methods
        void InitStimulator ()
        {
            port.WriteLine("iam TACTILITY");

            if (verbose)
            {
                deviceAnswer = port.ReadLine();
                print(deviceAnswer);
            }

            // query device
            string rawAnswer;
            port.WriteLine("battery ?");
            rawAnswer = port.ReadLine();
            batteryLevel = rawAnswer.Remove(0, rawAnswer.IndexOf("battery") + 8);

            port.WriteLine("hardware ?");
            rawAnswer = port.ReadLine();
            hardware = rawAnswer.Remove(0, rawAnswer.IndexOf("hardware") + 9);

            port.WriteLine("firmware ?");
            rawAnswer = port.ReadLine();
            firmware = rawAnswer.Remove(0, rawAnswer.IndexOf("firmware") + 9);

            port.WriteLine("elec 1 *pads_qty 32");
            // port.WriteLine("freq " + initialFrequency);
            SetFrequency(initialFrequency);
        }
        #endregion private methods
    }

}

