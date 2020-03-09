using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using System.IO;

namespace Inria.Tactility
{


    public enum Connector1ElectrodeType { None, HandConcentric }
    public enum Connector2ElectrodeType { None, HandMatrix }
    public enum Connector3ElectrodeType { None, FingerCircular, FingerMatrix }
    public enum Connector4ElectrodeType { None, FingerCircular, FingerMatrix }

    public enum Connector1HandPart { None, Palm, Dorsal }
    public enum Connector2HandPart { None, Palm, Dorsal }
    public enum Connector3HandPart { None, Index, Thumb }
    public enum Connector4HandPart { None, Index, Thumb }

    public class App2StimCmd
    {
        public string cmd;
        public string response = "" ;
        public int frameId;
        public float timestamp;

        public App2StimCmd(string cmd)
        {
            this.cmd = cmd;
            this.frameId = Time.frameCount;
            this.timestamp = Time.realtimeSinceStartup;
        }

        public override string ToString()
        {
            return "[" + frameId.ToString() + "]" + "[" + timestamp.ToString() + "]" + "[" + cmd + "]" + "[" + response + "]";
        }
    }
    public class TactilityStimulatorManager : MonoBehaviour
    {

        #region general settings
        [Header("General Settings")]

        [SerializeField]
        private bool verbose = true;

        [SerializeField]
        [Range(1, 200)]
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
        public bool initialized = false;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private bool running = false;   // changes should only happen during 'stim on' and 'velec id *selected 0'

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
        // the id is the velec id. this is the value used to stop a stim and whenever a velec definition is submited, we check map it to a velec id
        private Dictionary<int, Stimulation> stimulations = new Dictionary<int, Stimulation>();

        // private string commandToSubmit; // to avoid memory allocations?
        private string deviceAnswer;

        // logging commands (to be dummped into a log file)
        // all sessions should be log but let's add a conditional in case it's taking too long to dump file
        private const string logFile = "stimulatorCommand.log";
        private List<App2StimCmd> log = new List<App2StimCmd>();

        #region unity events
        private void OnEnable()
        {
            port = new SerialPort(portName, portBaudRate, portParity, portDataBits, portStopBits);
            port.ReadTimeout = readTimeout;//maybe needs to be set before opening port

            try
            {
                port.Open();
                if (verbose) print("[" + this.GetType().Name + "] Connected to bluetooth device");
                InitStimulator();
            } catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error trying to open port " + portName + ": " + e.Message);
            }

        }

        private void OnDisable()
        {
            if (initialized)
            {
                App2StimCmd cmd = new App2StimCmd("stim off");
                port.WriteLine(cmd.cmd);
                port.Close();
                log.Add(cmd);

                // dump log file
                DumpLogFile();
            }
        }

        #endregion unity events

        #region public API

        #region public direct API (no processing, just stim commands)

        /**
         * It just emits the stim on command. It doesn't sets any stim.selected=true property.
         * "Stim on" shouldn't change selected status
         */
        public void StartAll()
        {
            App2StimCmd startCmd = new App2StimCmd("stim on");
            port.WriteLine(startCmd.cmd); // should start all stim with selected=1

            // if there was no velec with selected=1, the stim status wont change to "on"
            running = false;
            foreach (var stim in stimulations.Values)
            {
                if (stim.Selected)
                {
                    // there is at least one stim still running
                    running = true;
                    break;
                }
            }

            if (verbose)
            {
                deviceAnswer = port.ReadLine();
                print("stim on -> " + deviceAnswer);
                startCmd.response = deviceAnswer;
            }

            log.Add(startCmd);
        }

        /**
         * It just emits the stim off command. // It doesn't actually sets stim.Selected=false property.
         * In anyway, using "stim off" is discourage! use velec id *selected 0 to stop stims
         */
        [Obsolete("Stop All using 'stim off' is deprecated. Set velec id *selected 0 instead to stop stim")]
        public void StopAll()
        {
            App2StimCmd stopCmd = new App2StimCmd("stim off");
            port.WriteLine(stopCmd.cmd);

            // do not set stim.Selected=false
            // in reality, they are still selected
            // and if we submit "stim on" they will resume.
            // If we change stim.Selected to false, then, if at some point, we ask for the string command
            // we will submit a velec def with selected=false, which could not be the intented action.
            foreach (var stim in stimulations.Values)
            {
                // by setting it to false, we are forcing the users to set selected=1 manually before another "stim on"
                stim.Selected = false;  // what happens if we call stim on again?
            }

            running = false;

            if (verbose)
            {
                deviceAnswer = port.ReadLine();
                print("stim off -> " + deviceAnswer);
                stopCmd.response = deviceAnswer;
            }

            log.Add(stopCmd);
        }

        /**
         * It just emits the freq command. It doesn't check or update any class data memeber.
         */
        public void SetFrequency (int val)
        {
            App2StimCmd freqCmd = new App2StimCmd("freq " + val);
            port.WriteLine(freqCmd.cmd);
            log.Add(freqCmd);

            frequency = val;
        }


        public int GetFrequency ()
        {
            return frequency;
        }

        /**
         * Low level call just meant for emitting stim <stimName> command.
         * There isn't any check here
         * */
        [Obsolete("Dont use it for playing stim. Use velec definition instead with selected=1 and 'stim on'")]
        public void PlayStim(string name)
        {
            App2StimCmd playCmd = new App2StimCmd("stim " + name);
            port.WriteLine(playCmd.cmd);

            if (verbose)
            {
                deviceAnswer = port.ReadLine();
                print(playCmd.cmd + " -> " + deviceAnswer);
                playCmd.response = deviceAnswer;
            }

            log.Add(playCmd);
        }

        /**
         * Low level call just meant for emitting velec <id> *selected 0
         * // old: There isn't any check here
         * new: now we are also updating stim internal data
         * */
        public void SetSelected0(int velecId)
        {
            App2StimCmd deselectCmd = new App2StimCmd("velec " + velecId.ToString() + " *selected 0");
            port.WriteLine(deselectCmd.cmd);

            if (stimulations.ContainsKey(velecId))
            {
                stimulations[velecId].Selected = false;
            }

            // check if there is another stim running
            running = false;
            foreach (var stim in stimulations.Values)
            {
                if (stim.Selected)
                {
                    // there is at least one stim still running
                    running = true;
                    break;
                }
            }

            if (verbose)
            {
                deviceAnswer = port.ReadLine();
                print(deselectCmd.cmd + " -> " + deviceAnswer);
                deselectCmd.response = deviceAnswer;
            }

            log.Add(deselectCmd);
        }

        /**
         * submit velec definition directly.
         * we assume stim values are okay. we don't perform any check here
         * */
        public void SubmitVelecDefDirectly (Stimulation stim)
        {
            App2StimCmd defCommand = new App2StimCmd(stim.GetStimCommand());
            port.WriteLine(defCommand.cmd);

            if (verbose)
            {
                print(defCommand.cmd);
                deviceAnswer = port.ReadLine();
                print("velec def -> " + deviceAnswer);
                defCommand.response = deviceAnswer;
            }

            log.Add(defCommand);

        }
        #endregion public direct API (no processing, just stim commands)

        /**
         * return -1 if it failed finding a valid conencted electrode for such part of the hand
         * */
        public int GetValidConnector (Actions.HandPart handPart)
        {
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

            return validConnector;

        }

        #region experimental API for PatternTester
        /**
         * The idea here is to have a higher level API (not just stimulator commands)
         * */

        #endregion experimental API for PatternTester

        // TODO: experimental feature. remove later
        // this is supposed to be called just once (probably on Awake or Start)
        // TODO2: we need to check again if we will keep this method!
        public void SubmitStim(Stimulation stim)
        {
            if (!stimulations.ContainsKey(stim.ID))
            {
                stimulations.Add(stim.ID, stim); // set active as true in ctor

                string stimCommand = stim.GetStimCommand();

                print(stimCommand);
                
                port.WriteLine(stimCommand); // write velec config (submit stim to stimulator)

                if (verbose)
                {
                    deviceAnswer = port.ReadLine();
                    print("velec def -> " + deviceAnswer);
                }

                // start stim. shouldn't do anything if selected was false.
                // the previous is not true at all. even if the velec was defined as selected=0
                // by starting the stim using its name rather than the global command, the stim will start playing anyway
                // we need to submit a global "stim on" at the beginning and then contorl each independently with selected true/false
                // port.WriteLine("stim " + stim.name);    
            } else
            {
                throw new Exception("There is already an existing stim with id=" + stim.ID);
            }
        }

        // TODO: we need to check again if we will keep this method!
        public void UpdateStim (Stimulation stim, bool updateSelected=false, bool newSelected=true)
        {
            if (stimulations.ContainsKey(stim.ID))
            {
                // update dynamic data (not related to active/inactive)
                // range limits are going to be verified when creating string command
                stimulations[stim.ID].Intensity = stim.Intensity;
                stimulations[stim.ID].PulseWidth = stim.PulseWidth;
                if (updateSelected) stimulations[stim.ID].Selected = newSelected;

                string command = stimulations[stim.ID].GetStimCommand();

                print(command); // console

                port.WriteLine(command); // write velec new config
                // do we need to write "stim <stimName>" again?. Let's do it just in case
                // port.WriteLine("stim " + stimulations[stim.id].name);    // start stim (if active is false, maybe it wont play)
                if (verbose)
                {
                    deviceAnswer = port.ReadLine();
                    print("velec redef -> " + deviceAnswer);
                }

            } else
            {
                throw new Exception("There is no existing stim with id=" + stim.ID);
            }

        }

        public void Add(VirtualElectrode virtualElectrode, Actions.HandPart handPart, float intensity, int pulseWidth)
        {
            if (stimulations.ContainsKey(virtualElectrode.id))
            {
                // update stim params

                // check if intensity or pulse width are new
                bool newIntensity = !Mathf.Approximately(stimulations[virtualElectrode.id].Intensity, intensity);
                bool newPulseWidth = (stimulations[virtualElectrode.id].PulseWidth != pulseWidth);

                if (newIntensity || newPulseWidth || stimulations[virtualElectrode.id].Selected == false)
                {
                    // update values and send new command to stimualtor
                    stimulations[virtualElectrode.id].Selected = true;
                    stimulations[virtualElectrode.id].Intensity = intensity;
                    stimulations[virtualElectrode.id].PulseWidth = pulseWidth;

                    print(stimulations[virtualElectrode.id].GetStimCommand());
                    port.WriteLine(stimulations[virtualElectrode.id].GetStimCommand());

                    port.WriteLine("stim " + stimulations[virtualElectrode.id].Name);
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
                port.WriteLine("stim " + stim.Name);

            }

        }

        // it will update selected to 1.
        // command: stim <stimName> need to be sent previously
        // it shouldnt do anything else but setting selected to 1
        [Obsolete("Don't use it. Setting selected 1 doesn't work")]
        public void EnableStim(int id)
        {
            SetStimOnOff(id, true);
        }

        public void DisableStim(int id)
        {
            SetStimOnOff(id, false);
        }

        [Obsolete("Do not use it anymore. Play the stim by name which doesn't behave as expected")]
        public void PlayStim(int id)
        {
            if (stimulations.ContainsKey(id))
            {
                string command = "stim " + stimulations[id].Name;
                port.WriteLine(command);

                if (verbose)
                {
                    deviceAnswer = port.ReadLine();
                    print(command + " -> " + deviceAnswer);
                }
            }

        }
        #endregion public API

        #region private methods
        /**
         * should be only called if the connection to the stim port was successful
         * */
        void InitStimulator ()
        {
            string rawAnswer;   // for system reponses

            App2StimCmd startCmd = new App2StimCmd("iam TACTILITY");
            port.WriteLine(startCmd.cmd);

            if (verbose)
            {
                deviceAnswer = port.ReadLine();
                print(deviceAnswer);
                startCmd.response = deviceAnswer;
            }
            log.Add(startCmd);

            // query device
            App2StimCmd batteryCmd = new App2StimCmd("battery ?");
            port.WriteLine(batteryCmd.cmd);
            rawAnswer = port.ReadLine();
            batteryLevel = rawAnswer.Remove(0, rawAnswer.IndexOf("battery") + 8);
            batteryCmd.response = batteryLevel;
            log.Add(batteryCmd);

            App2StimCmd hardwareCmd = new App2StimCmd("hardware ?");
            port.WriteLine(hardwareCmd.cmd);
            rawAnswer = port.ReadLine();
            hardware = rawAnswer.Remove(0, rawAnswer.IndexOf("hardware") + 9);
            hardwareCmd.response = hardware;
            log.Add(hardwareCmd);

            App2StimCmd firmwareCmd = new App2StimCmd("firmware ?");
            port.WriteLine(firmwareCmd.cmd);
            rawAnswer = port.ReadLine();
            firmware = rawAnswer.Remove(0, rawAnswer.IndexOf("firmware") + 9);
            firmwareCmd.response = firmware;
            log.Add(firmwareCmd);

            App2StimCmd padsQTYCmd = new App2StimCmd("elec 1 *pads_qty 32");
            port.WriteLine(padsQTYCmd.cmd);
            log.Add(padsQTYCmd);

            SetFrequency(initialFrequency);

            // deactive all previous virtual electrodes
            DeactiveAllVE();

            // if there was any exeption before (while writing to the port, we wont get to this point)
            initialized = true; 
        }

        private void DumpLogFile ()
        {
            StreamWriter writer = new StreamWriter(logFile, true);
            DateTime currentDate = DateTime.Now;
            string msg = "[" + currentDate.ToString("dd/MM/yyyy HH:mm:ss") + "] new session";
            writer.WriteLine(msg);
            for (int i = 0; i < log.Count; ++i)
            {
                writer.WriteLine(log[i]);
            }
            writer.WriteLine();
            writer.Close();
        }

        private void DeactiveAllVE ()
        {
            for (int i = 1; i <= 16; ++i)
            {
                App2StimCmd deselectCmd = new App2StimCmd("velec " + i.ToString() + " *selected 0");
                port.WriteLine(deselectCmd.cmd);

                if (verbose)
                {
                    deviceAnswer = port.ReadLine();
                    print(deselectCmd.cmd + " -> " + deviceAnswer);
                    deselectCmd.response = deviceAnswer;
                }

                log.Add(deselectCmd);
            }
        }

        /**
         * set velec id *select [0|1]
         * if set to zero, stops stim this was being played
         * */
        private void SetStimOnOff (int id, bool value)
        {
            if (stimulations.ContainsKey(id))
            {
                stimulations[id].Selected = value;
                string valueStr = (value) ? "1" : "0";
                App2StimCmd updateSelectedCmd = new App2StimCmd("velec " + id.ToString() + " *selected " + valueStr);
                port.WriteLine(updateSelectedCmd.cmd);
                if (verbose)
                { 
                    deviceAnswer = port.ReadLine();
                    print(updateSelectedCmd.cmd + " -> " + deviceAnswer);
                    updateSelectedCmd.response = deviceAnswer;
                }
                log.Add(updateSelectedCmd);
            } else {
                throw new Exception("No available stim with id=" + id);
            }

        }

        #endregion private methods
    }

}

