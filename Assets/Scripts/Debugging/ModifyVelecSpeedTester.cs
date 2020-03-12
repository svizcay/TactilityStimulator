using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Tactility.Debugging
{
    /**
     * Utility class to test what's the minimum time period that we need to wait to submit a new velec definition
     * while this is being played.
     * if this is quite fast and the problem between doing spatio-temporal patterns was the called to "stim on",
     * maybe we can start the next step of the spatio-temporal pattern before pausing the current one, and then,
     * once the next step was started by the stimulator, stop the first one (with that, we won't need to call "stim on")
     * */
    public class ModifyVelecSpeedTester : MonoBehaviour
    {
        [Header("Settings")]

        [SerializeField]
        [Tooltip("One of the intensity values to test")]
        [Range(0f, 9f)]
        private float intensity1 = 2.5f; // between 0.1 and 9mA

        [SerializeField]
        [Tooltip("The other intensity value to test")]
        [Range(0f, 9f)]
        private float intensity2 = 5f; // between 0.1 and 9mA

        [SerializeField]
        [Tooltip("pulse width value to use during velec definition")]
        [Leap.Unity.Attributes.OnEditorChange("PulseWidth")] // needs to be before Range decorator
        [Range(30, 500)]
        private int pulseWidth = 200; // between 30 and 500us

        public int PulseWidth
        {
            get { return pulseWidth; }
            set {
                if (stimulation != null)
                {
                    // it will check intensity values, mark stringCommand as dirty
                    // but it wont submit it immediatelly
                    stimulation.PulseWidth = value;

                    // maybe not needed here because we are testing velec def speed.
                    // it's not going to be long until next natural velec def submition
                    if (stimManager !=null)
                    {
                        stimManager.SubmitVelecDefDirectly(stimulation);
                    }
                }
            }
        }

        [SerializeField]
        [Range(1, 200)]
        [Tooltip("frequency value to submit in key pressed")]
        [Leap.Unity.Attributes.OnEditorChange("SubmitFrequency")]
        private int frequency = 35; // between 1 and 200hz

        [SerializeField]
        [Tooltip("")]
        private Actions.HandPart handPart = Actions.HandPart.Index;

        [SerializeField]
        [Tooltip("virtual electrode layout")]
        private VirtualElectrode velec;

        [Tooltip("How fast a new velec definition is submitted in Hz")]
        [SerializeField]
        [Leap.Unity.Attributes.OnEditorChange("Speed")]
        [Range(0f, 200f)]// try to figure out valid range // HAS TO BE DECLARE RIGHT BEFORE field and AFTER LeapMotion OnEditorChange
        private float _speed = 5f;  // should be something between [0 and 8Hz]

        public float Speed
        {
            get { return _speed; }
            set {
                interdefIntervalMS = 1000f / value;
            }
        }

        [Header("Settings - Key bindings")]

        [SerializeField]
        private KeyCode startStopKeyCode = KeyCode.P;

        [Header("Debugging Info")]

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private bool ready = false;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private bool running = false;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        [Tooltip("time in milliseconds between velec definitions")]
        private float interdefIntervalMS; // to be calculated and updated stepDuration(patternFreq, ISI)

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private float elapsedTimeMS = 0f;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        [Tooltip("if true, playing intensity val1; if false, play intensity val2")]
        private bool playIntensity1 = true;  // we can be executing the cycle but within it, we can be in the OFF part of the cycle

        // external components
        private TactilityStimulatorManager stimManager;

        /**
         * let's keep only one stimulation object.
         * we also want to see the efect of having the stim command marked as dirty
         * because in the real scenario, we are not going to have a stimulation object for each possible parameter
         * ie: we are going to modify in runtime a given one.
         * */
        private Stimulation stimulation;

        private void Awake()
        {
            stimManager = FindObjectOfType<TactilityStimulatorManager>();
            Speed = _speed;
        }

        void Start()
        {
            StartCoroutine(Initialize());
        }

        // Update is called once per frame
        void Update()
        {
            if (ready)
            {
                if (running)    // controlled by user pressing a key
                {
                    elapsedTimeMS += (Time.deltaTime * 1000);   

                    if (elapsedTimeMS >= interdefIntervalMS)
                    {
                        // if we were playing intensity1, play intensity2
                        // NOTE: this will mark command is dirty
                        stimulation.Intensity = (playIntensity1) ? intensity2 : intensity1; 
                        stimManager.SubmitVelecDefDirectly(stimulation);

                        playIntensity1 = !playIntensity1;
                        elapsedTimeMS = 0f;
                    }
                }

                // should we do it at the begining of the frame or at the end?
                if (Input.GetKeyDown(startStopKeyCode))
                {
                    ToggleRunning();    // should pattern resume where it was or start over again?
                }
            }
            
        }

        // this should be in charge of calling "stim on" if the global sitm was not initialized or stopped
        public void ToggleRunning ()
        {
            if (running)
            {
                // stop all stim
                stimManager.SetSelected0(stimulation.ID);   // should i do it only if we were in the active part of the cycle?
            } else
            {
                // start/resume stim
                stimManager.SubmitVelecDefDirectly(stimulation);    // it will use previous used intensity
                stimManager.StartAll();
            }

            running = !running;
        }

        public void SubmitFrequency ()
        {
            if (stimManager != null) stimManager.SetFrequency(frequency);
        }

        IEnumerator Initialize()
        {
            while (!stimManager.initialized)
            {
                yield return null;
            }

            int connector = stimManager.GetValidConnector(handPart);    

            stimulation = new Stimulation(
                velec.id,   // NOTE: by now only accessing the first electrode of each step (add later electrode for additional fingers)
                velec.name,
                intensity1,
                pulseWidth,
                velec.GetCathodes(connector),
                velec.GetAnodes(connector),
                true
            );

            SubmitFrequency();

            ready = true;
        }
    }

}

