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
    public class FrequencyModulationTester : MonoBehaviour
    {
        [Header("Settings")]

        [SerializeField]
        [Tooltip("current intensity value to use during velec definition")]
        [Leap.Unity.Attributes.OnEditorChange("Intensity")] // needs to be before Range decorator
        [Range(0f, 9f)]
        private float intensity = 2.5f; // between 0.1 and 9mA

        private float Intensity
        {
            get { return intensity; }
            set {
                if (stimulation != null)
                {
                    stimulation.Intensity = value;

                    if (stimManager !=null)
                    {
                        stimManager.SubmitVelecDefDirectly(stimulation);
                    }
                }
            }
        }

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
        private AnimationCurve modulation;


        [SerializeField]
        [Tooltip("min frequency (base frequency)")]
        [Leap.Unity.Attributes.OnEditorChange("MinFrequency")]//BEFORE RANGE
        [Range(1, 200)]
        private int minFrequency = 35; // between 1 and 200hz

        private int MinFrequency
        {
            get { return minFrequency; }
            set {
                minFrequency = Mathf.Clamp(value, 0, maxFrequency);
            }
        }

        [SerializeField]
        [Tooltip("max frequency")]
        [Leap.Unity.Attributes.OnEditorChange("MaxFrequency")]//BEFORE RANGE
        [Range(1, 200)]
        private int maxFrequency = 200; // between 1 and 200hz

        private int MaxFrequency
        {
            get { return maxFrequency; }
            set {
                maxFrequency = Mathf.Clamp(value, minFrequency, 200);
            }
        }

        [SerializeField]
        [Tooltip("")]
        private Actions.HandPart handPart = Actions.HandPart.Index;

        [SerializeField]
        [Tooltip("virtual electrode layout")]
        private VirtualElectrode velec;

        // [Tooltip("How fast a new velec definition is submitted in Hz")]
        // [SerializeField]
        // [Leap.Unity.Attributes.OnEditorChange("PatternSpeed")]
        // [Range(0f, 200f)]// try to figure out valid range // HAS TO BE DECLARE RIGHT BEFORE field and AFTER LeapMotion OnEditorChange
        // private float _patternSpeed = 5f;  // should be something between [0 and 8Hz]

        // public float PatternSpeed
        // {
        //     get { return _patternSpeed; }
        //     set {
        //         interdefIntervalMS = 1000f / value;
        //     }
        // }

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
        private int modulatedValue;

        // [SerializeField]
        // [SouthernForge.Utils.ReadOnly]
        // [Tooltip("time in milliseconds between velec definitions")]
        // private float interdefIntervalMS; // to be calculated and updated stepDuration(patternFreq, ISI)

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private float elapsedTimeMS = 0f;

        // external components
        private TactilityStimulatorManager stimManager;

        /**
         * let's keep only one stimulation object.
         * we also want to see the efect of having the stim command marked as dirty
         * because in the real scenario, we are not going to have a stimulation object for each possible parameter
         * ie: we are going to modify in runtime a given one.
         * */
        private Stimulation stimulation;


        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private float modulationCurveTimeLengthMS;

        private void Awake()
        {
            stimManager = FindObjectOfType<TactilityStimulatorManager>();
            // PatternSpeed = _patternSpeed;

            if (modulation.length > 0)
            {
                modulationCurveTimeLengthMS = modulation.keys[modulation.length - 1].time * 1000;
            }
        }

        void Start()
        {
            StartCoroutine(Initialize());
        }

        // Update is called once per frame

        // NOTE: people dont perceive frequency linearly. try to use something different like:
        // float Portamento(float frequency1, float frequency2, float interpolator) {
        //     return frequency1 * Mathf.Pow(frequency2 / frequency1, interpolator);
        // }

        void Update()
        {
            if (ready)
            {
                if (running)    // controlled by user pressing a key
                {
                    // we dont want to use Time.time directly because that is also couting while the stim is off
                    elapsedTimeMS += (Time.deltaTime * 1000);

                    // residual is in milliseconds. Evaluate methods takes param in seconds
                    float residual = elapsedTimeMS % (modulationCurveTimeLengthMS); // it will loop naturally

                    // modulatingVal is something normalized between [0, 1]
                    float modulatingVal = Mathf.Clamp( modulation.Evaluate(residual / 1000f), 0f, 1f);
                    modulatedValue = (int)(Mathf.Lerp(MinFrequency, MaxFrequency, modulatingVal));
                    SubmitFrequency(modulatedValue);
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

        /**
         * we also need to test if the stimulator is capable of updating the frequency (receiving various commands within a short period of time) very fast
         * this method is potentially called every frame. which it might be too much.
         * */
        public void SubmitFrequency (int val)
        {
            if (stimManager != null) stimManager.SetFrequency(val);
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
                intensity,
                pulseWidth,
                velec.GetCathodes(connector),
                velec.GetAnodes(connector),
                true
            );

            SubmitFrequency(minFrequency);

            ready = true;
        }
    }

}

