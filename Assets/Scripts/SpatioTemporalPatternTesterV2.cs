using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Use Debugging as namespace and not Debug in order to avoid conflict with Unity Debug namespace
namespace Inria.Tactility.Debugging
{
    /**
     * Utility to test spatio temportal pattern at different speed.
     * the difference with v1 is that this keep the stimulator on while iterating through the pattern steps.
     * We also removed temporarly the "using ISI" (inter step interval) option.
     * Since we are keeping the stim on, we need to have 2 or more concurrent velecs running at the same time,
     * therefore, we need 2 consecutive steps to have different ids. (we will stop one after the other was already started)
     * */
    public class SpatioTemporalPatternTesterV2 : MonoBehaviour
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
                if (stimulations != null)
                {
                    for (int i = 0; i < stimulations.Length; i++)
                    {
                        // this will mark the command as dirty.
                        // it will be submitted next time
                        stimulations[i].Intensity = value;
                    }

                    // if could submit the current one being played
                    if (stimManager !=null)
                    {
                        stimManager.SubmitVelecDefDirectly(stimulations[patternIndexIterator]);
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
                if (stimulations != null)
                {
                    for (int i = 0; i < stimulations.Length; i++)
                    {
                        // this will mark the command as dirty.
                        // it will be submitted next time
                        stimulations[i].PulseWidth = value;
                    }

                    if (stimManager !=null)
                    {
                        stimManager.SubmitVelecDefDirectly(stimulations[patternIndexIterator]);
                    }
                }
            }
        }

        [SerializeField]
        [Range(1, 200)]
        [Tooltip("frequency value to submit in key pressed")]
        [Leap.Unity.Attributes.OnEditorChange("SubmitFrequency")]
        private int frequency = 200; // between 1 and 200hz

        [SerializeField]
        [Tooltip("")]
        private Actions.HandPart handPart = Actions.HandPart.Index;

        [Header("Pattern Settings")]

        [SerializeField]
        [Tooltip("pattern to test different modulation values")]
        private StimPattern pattern; // by now it only contains spatial layout (not any temporal value)
        
        // using leapmotion property attribute to execute code when updating a single property in inspector
        [Tooltip("How fast the pattern is played")]
        [SerializeField]
        [Leap.Unity.Attributes.OnEditorChange("PatternFrequency")]
        [Range(0f, 8f)]// try to figure out valid range // HAS TO BE DECLARE RIGHT BEFORE field and AFTER LeapMotion OnEditorChange
        private float _patternFrequency = 5f;  // should be something between [0 and 8Hz]

        /**
         * pattern frequency has to be lower than system frequency.
         * system frequency is how fast system cycles are executed.
         * If we have high frequency value, we run system "update" every few ms (200Hz -> every 5ms)
         * If we have low frequency value, we run system "update" very sporadicly (1Hz -> every 1000ms)
         * we should subscribe to changes to system frequency so pattern doesn't send changes so fast
         * */
        public float PatternFrequency
        {
            get { return _patternFrequency; }
            set {
                // we need to update step duration
                float patternDurationMS = 1000f / value;
                stepDuration = patternDurationMS / pattern.steps.Length;

                // also update limit for turnOffdelayMS
                if (delayVelecTurnOff)
                {
                    TurnOffDelayMS = turnOffDelayMS;
                }
            }
        }

        [SerializeField]
        [Tooltip("whether if stop the previous velec immediately after enabling the next one or if we wait some milliseconds")]
        private bool delayVelecTurnOff = false;

        [SerializeField]
        [Tooltip("time in millisecond to wait to desactive previous velec")]
        [Leap.Unity.Attributes.OnEditorChange("TurnOffDelayMS")] // needs to be before Range decorator
        private float turnOffDelayMS = 100f;    // has to be way lower than step duration

        private float TurnOffDelayMS
        {
            get { return turnOffDelayMS; }
            set {
                turnOffDelayMS = Mathf.Clamp(value, 0, 0.9f * stepDuration);
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

        // duration of each step is going to be calcualted based of the target pattern's speed
        // and how much we have decided the ISI will last (should this also be quicker if we increase the pattern frequency? A) yes)
        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        [Tooltip("Calculated step duration. Needs to be higher than Unity's delta time and higher than stimulator's pulse period")]
        private float stepDuration; // to be calculated and updated stepDuration(patternFreq, ISI)

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private float elapsedTimePlayingMS = 0f;

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private int patternIndexIterator = 0;

        private Stimulation[] stimulations;

        private int iteratorToStop = -1;

        // external components
        private TactilityStimulatorManager stimManager;

        private void Awake()
        {
            stimManager = FindObjectOfType<TactilityStimulatorManager>();

            // calculate stepDuration and isiDuration
            PatternFrequency = _patternFrequency;   // it will also update turnOffDelay if appropiate
        }

        private void Start()
        {
            StartCoroutine(Initialize());
        }

        void Update()
        {
            if (ready)
            {
                if (running)
                {
                    elapsedTimePlayingMS += (Time.deltaTime * 1000);

                    if (delayVelecTurnOff && elapsedTimePlayingMS >= turnOffDelayMS && iteratorToStop != -1)
                    {
                        stimManager.SetSelected0(stimulations[iteratorToStop].ID);
                        iteratorToStop = -1;
                    }


                    if (elapsedTimePlayingMS > stepDuration)
                    {
                        // submit next step
                        int nextStep = (patternIndexIterator + 1) % pattern.steps.Length;
                        stimManager.SubmitVelecDefDirectly(stimulations[nextStep]);

                        // stop current one (maybe add some delay before doing so)
                        if (!delayVelecTurnOff)
                        {
                            stimManager.SetSelected0(stimulations[patternIndexIterator].ID);
                        } else
                        {
                            iteratorToStop = patternIndexIterator;
                        }

                        patternIndexIterator = nextStep;
                        elapsedTimePlayingMS = 0;
                    }
                }

                if (Input.GetKeyDown(startStopKeyCode))
                {
                    ToggleRunning();    // should pattern resume where it was or start over again?
                }
            }
        }

        IEnumerator Initialize()
        {
            while (!stimManager.initialized)
            {
                yield return null;
            }

            // create stimulations
            stimulations = new Stimulation[pattern.steps.Length];
            for (int i = 0; i < stimulations.Length; ++i)
            {
                // will throw an exception if there is not electrode connect for that part of the hand
                int connector = stimManager.GetValidConnector(handPart);    
                stimulations[i] = new Stimulation(
                    pattern.steps[i].virtualElectrodes[0].id,   // NOTE: by now only accessing the first electrode of each step (add later electrode for additional fingers)
                    pattern.steps[i].virtualElectrodes[0].name,
                    intensity,
                    pulseWidth,
                    pattern.steps[i].virtualElectrodes[0].GetCathodes(connector),
                    pattern.steps[i].virtualElectrodes[0].GetAnodes(connector),
                    true
                );
            }

            SubmitFrequency();

            ready = true;
        }

        public void ToggleRunning ()
        {
            if (running)
            {
                stimManager.SetSelected0(stimulations[patternIndexIterator].ID);
                if (delayVelecTurnOff && iteratorToStop != -1)
                {
                    stimManager.SetSelected0(stimulations[iteratorToStop].ID);
                    iteratorToStop = -1;
                }
            } else
            {
                stimManager.SubmitVelecDefDirectly(stimulations[patternIndexIterator]);
                stimManager.StartAll();
            }
            running = !running;
        }

        public void SubmitFrequency ()
        {
            if (stimManager != null) stimManager.SetFrequency(frequency);
        }

        // to keep ready private (only accessed by custom editor)
        public bool IsReady()
        {
            return ready;
        }

        // to keep ready private (only accessed by custom editor)
        public bool IsRunning()
        {
            return running;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SpatioTemporalPatternTesterV2))]
    public class PatternTesterEditorV2 : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SpatioTemporalPatternTesterV2 script = target as SpatioTemporalPatternTesterV2;

            if (!script.IsReady()) return;

            bool running = script.IsRunning();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(running ? "Stop" : "Play" ))
            {
                script.ToggleRunning();
            }
            EditorGUILayout.EndHorizontal();
        }

    }

#endif
}

