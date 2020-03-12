using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Tactility.Debugging
{
    /**
     * Utility class to test what's the minimum time period a stimulus has to be played
     * in order to the stimulator can produce it and the user feel it.
     * */
    public class VelecSwitchSpeedTester : MonoBehaviour
    {
        [Header("Settings")]

        [SerializeField]
        [Tooltip("intensity value to use during velec definition")]
        [Leap.Unity.Attributes.OnEditorChange("Intensity")] // needs to be before Range decorator
        [Range(0f, 9f)]
        private float intensity = 2.5f; // between 0.1 and 9mA

        private float Intensity
        {
            get { return intensity; }
            set {
                if (stimulation != null)
                {
                    // it will check intensity values, mark stringCommand as dirty
                    // but it wont submit it immediatelly
                    stimulation.Intensity = value;

                    // submit if
                    if (stimManager !=null)
                    {
                        stimManager.SubmitVelecDefDirectly(stimulation);
                        // if stimulator was on, this stim will be updated immediately, otherwise it will wait for next time
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

                    if (stimManager !=null)
                    {
                        stimManager.SubmitVelecDefDirectly(stimulation);
                        // if stimulator was on, this stim will be updated immediately, otherwise it will wait for next time
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

        [SerializeField]
        [Tooltip("cycle duty")]
        private float cycleDuty = 1f;

        [Tooltip("How fast the cycle is repeated in Hz")]
        [SerializeField]
        [Leap.Unity.Attributes.OnEditorChange("Speed")]
        [Range(0f, 8f)]// try to figure out valid range // HAS TO BE DECLARE RIGHT BEFORE field and AFTER LeapMotion OnEditorChange
        private float _speed = 5f;  // should be something between [0 and 8Hz]

        public float Speed
        {
            get { return _speed; }
            set {
                float cycleDurationMS = 1000f / value;
                velecOnTimeIntervalMS = cycleDurationMS * cycleDuty;
                velecOffTimeIntervalMS = cycleDurationMS - velecOnTimeIntervalMS;
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
        [Tooltip("time in milliseconds that velec is ON")]
        private float velecOnTimeIntervalMS; // to be calculated and updated stepDuration(patternFreq, ISI)

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        [Tooltip("time in milliseconds that velec is OFF")]
        private float velecOffTimeIntervalMS; // to be calculated and updated stepDuration(patternFreq, ISI)

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private float elapsedTimeMS = 0f;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        [Tooltip("wether if we are in the active or inactive part of the cycle")]
        private bool stimIsON = false;  // we can be executing the cycle but within it, we can be in the OFF part of the cycle

        // external components
        private TactilityStimulatorManager stimManager;

        private Stimulation stimulation;

        private bool firstTime = true;


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
                    // if it was just set to ON with the key, we are adding elapsed time from the previous frame when in reality we shoulnd't
                    // there will be an offset of few milliseconds (what it takes to render a normal frame)
                    // now it's fine since we move the toggle check at the end of the frame
                    elapsedTimeMS += (Time.deltaTime * 1000);   

                    if (stimIsON && elapsedTimeMS >= velecOnTimeIntervalMS)
                    {
                        // time to switch to the part of the cycle that is off
                        stimManager.SetSelected0(stimulation.ID);

                        stimIsON = false;
                        elapsedTimeMS = 0f;
                    }
                    
                    // don't use else-if. if duty cycle was 1, we need to submit selected=1 immediately
                    if (!stimIsON && elapsedTimeMS >= velecOffTimeIntervalMS)
                    {
                        // time to switch to the part of the cycle that is on
                        stimManager.SubmitVelecDefDirectly(stimulation);
                        stimManager.StartAll();

                        // NOTE: order shouldn't matter (setting this before or after of emitting stim commands)
                        stimIsON = true;
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

        public void ToggleRunning ()
        {
            if (running)
            {
                // stop all stim
                stimManager.SetSelected0(stimulation.ID);   // should i do it only if we were in the active part of the cycle?
            } else
            {
                if (stimIsON)
                {
                    // we were running the active part of the cycle
                    // and that was turned off with selected=0
                    // we need to enable it again
                    stimManager.SubmitVelecDefDirectly(stimulation);
                    stimManager.StartAll();

                    // not required (let's not touch elapsedTime here!)
                    // stimIsON = true;
                    // elapsedTimeMS = 0f; // should we reset the elaped time here??

                } else
                {
                    // we were running the inactive part of the cycle. continue like that
                    // or it can also be that is the first time pressing "start"

                    // do something for the "first time" case otherwise the active part will
                    // be played after a delay (the duration of the inactive part)
                    if (firstTime)
                    {
                        stimManager.SubmitVelecDefDirectly(stimulation);
                        stimManager.StartAll();

                        stimIsON = true;
                        elapsedTimeMS = 0f;

                        firstTime = false;
                    }

                }

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
                intensity,
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

