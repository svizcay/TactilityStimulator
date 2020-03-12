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
     * Utility to test spatio temportal pattern at different speed
     * */
    public class SpatioTemporalPatternTester : MonoBehaviour
    {
        [Header("Settings")]

        [SerializeField]
        [Range(0f, 9f)]
        [Tooltip("intensity value to use during velec definition")]
        private float intensity = 2.5f; // between 0.1 and 9mA

        [SerializeField]
        [Range(30, 500)]
        [Tooltip("pulse width value to use during velec definition")]
        private int pulseWidth = 200; // between 30 and 500us

        [SerializeField]
        [Range(1, 200)]
        [Tooltip("frequency value to submit in key pressed")]
        [Leap.Unity.Attributes.OnEditorChange("SubmitFrequency")]
        private int frequency = 35; // between 1 and 200hz

        [SerializeField]
        [Tooltip("")]
        private Actions.HandPart handPart = Actions.HandPart.Index;

        [Header("Pattern Settings")]

        [SerializeField]
        [Tooltip("pattern to test different modulation values")]
        private StimPattern pattern; // by now it only contains spatial layout (not any temporal value)
        
        /**
         * seems there is no need to use inter step interval.
         * if we can already perceive a delay when using "stim on" to start a stim.
         * check if that is still the case while keeping the stimulator always "on"
         * */
        [SerializeField]
        [Tooltip("wether or not to use Inter Step Interval")]
        private bool useISI = false;    // hide unnecesary options if this was set to false // this should also work for patterns with a single step

        [SerializeField]
        private bool useISIAfterLastStep = false;   // this should also work for patterns with a single step (that will make it like an inter pattern interval)

        // Do not specify ISI in terms of duration. If we change the speed of the pattern,
        // we don't know how fast we have to run the ISI period.
        // It's better to specify Pattern duty (% of the pattern period that is used to play actual steps)
        // [SerializeField]
        // [Tooltip("Inter Step Interval time in milliseconds")]
        // private float ISIMS = 1000f;

        /**
         * this is a mechanism to specify inter step interval duration in ms, without saying explicitly how long
         * */
        [SerializeField]
        [Tooltip("Step duty (if using Inter Step Interval ISI")]
        private float stepDuty = 1f;

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
                // we need to update step duration and inter step interval accordingly
                print("setting pattern frequency to: " + value);

                float patternDurationMS = 1000f / value;

                if (useISI)
                {
                    if (useISIAfterLastStep)
                    {
                        // make sure to include case when pattern consists of a single step (DONE: works!)
                        durations = new float[pattern.steps.Length * 2];
                        float stepsDuration = patternDurationMS * stepDuty;
                        float isisDuration = patternDurationMS - stepsDuration;
                        stepDuration = stepsDuration / pattern.steps.Length;
                        interStepIntervalDuration = isisDuration / pattern.steps.Length;
                        for (int i = 0; i < durations.Length; ++i)
                        {
                            if (i % 2 == 0) durations[i] = stepDuration;
                            else durations[i] = interStepIntervalDuration;
                        }
                    }
                    else
                    {
                        // if pattern consists of only a single step and we didn't enable a last inter step interval, throw error.
                        if (pattern.steps.Length == 1) throw new Exception("Pattern only consists of a single step and last ISI was not enabled.");
                        durations = new float[pattern.steps.Length * 2 - 1];
                        float stepsDuration = patternDurationMS * stepDuty;
                        float isisDuration = patternDurationMS - stepsDuration;
                        stepDuration = stepsDuration / pattern.steps.Length;
                        interStepIntervalDuration = isisDuration / (pattern.steps.Length - 1);    // error when pattern has only one step

                        for (int i = 0; i < durations.Length; ++i)
                        {
                            if (i % 2 == 0) durations[i] = stepDuration;
                            else durations[i] = interStepIntervalDuration;
                        }
                    }
                } else
                {
                    durations = new float[pattern.steps.Length];
                    // each element in the array has the same duration
                    stepDuration = patternDurationMS / durations.Length;
                    for (int i = 0; i < durations.Length; ++i)
                    {
                        durations[i] = stepDuration;
                    }
                }
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
        [Tooltip("Calculated ISI duration. Needs to be higher than Unity's delta time and higher than stimulator's pulse period")]
        private float interStepIntervalDuration; // to be calculated

        // external components
        private TactilityStimulatorManager stimManager;

        // internal data
        // private Stimulation currentStim;
        // hardcoded values used for the test velec
        // private const int id = 11;    // velec id to use for testing
        // private new const string name = "test";

        // pattern iteration related vars
        private float[] durations;


        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private float elapsedTimePlayingMS = 0f;

        private int patternIndexIterator = 0; // also used for playing ISIs when index is odd (and when using ISI of course)
        private int totalNrOfRealSteps; // counting both, steps ans ISIs    // this should be updated accordingly whenever the options change

        // private bool patternsParamsDirty = true; // to indicate us that we need to calculate again stepDuration

        private Stimulation[] stimulations;

        public float stimOnDelayMS = 25;
        private bool shouldSubmitStimOn = false;
        private float elapsedTimeForStimOn = 0f;

        private void Awake()
        {
            stimManager = FindObjectOfType<TactilityStimulatorManager>();

            // calculate stepDuration and isiDuration
            PatternFrequency = _patternFrequency;

            if (useISI)
            {
                totalNrOfRealSteps = (useISIAfterLastStep) ? (pattern.steps.Length * 2) : (pattern.steps.Length * 2 - 1);
            }
            else
            {
                totalNrOfRealSteps = pattern.steps.Length;
            }
        }

        private void Start()
        {
            StartCoroutine(Initialize());
        }

        void Update()
        {
            if (ready)
            {
                if (Input.GetKeyDown(startStopKeyCode))
                {
                    ToggleRunning();    // should pattern resume where it was or start over again?
                }

                if (running)
                {
                    if (shouldSubmitStimOn)
                    {
                        elapsedTimeForStimOn += (Time.deltaTime * 1000);
                        if (elapsedTimeForStimOn > stimOnDelayMS)
                        {
                            stimManager.StartAll();
                            shouldSubmitStimOn = false;
                        }
                    }

                    elapsedTimePlayingMS += (Time.deltaTime * 1000);


                    if (elapsedTimePlayingMS > durations[patternIndexIterator])
                    {
                        // time to move along the pattern pieces (either a step or a ISI)
                        int nextIndexIterator = (patternIndexIterator + 1) % totalNrOfRealSteps;

                        int stepNr = GetStepNR(patternIndexIterator);   // if -1 we were playing a ISI, if not, we were playing a stim
                        if (stepNr != -1)
                        {
                            // we we were playing a real step, stop it (set it to selected=0)
                            stimManager.SetSelected0(stimulations[stepNr].ID);
                        }


                        // if the next step is actually a step, play it
                        int nextStepNr = GetStepNR(nextIndexIterator);
                        if (nextStepNr != -1)
                        {
                            stimManager.SubmitVelecDefDirectly(stimulations[nextStepNr]);

                            // try to not submit the "stim on" right after a velec def. might require longer to process it
                            // stimManager.StartAll();
                            shouldSubmitStimOn = true;

                        }

                        patternIndexIterator = nextIndexIterator;
                        elapsedTimePlayingMS = 0; // time playing a real step (either step or ISIs)
                    }

                    // if (elapsedTimePlaying > stepDuration)

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
            print("toggle running");
            if (running) {
                // stop current stim playing
                int stepNr = GetStepNR(patternIndexIterator);
                if (stepNr != -1)
                {
                    // stop current stim
                    stimManager.SetSelected0(stimulations[stepNr].ID);
                }
            } else
            {
                // at this point any velec should have selected=0, so we need to submit a redefinition of the right one with selected=1

                // get what's the real step to play (what happens when we are in the InterPulseInterval and we resume)?
                int stepNr = GetStepNR(patternIndexIterator);

                if (stepNr != -1)
                {
                    // play step
                    stimManager.SubmitVelecDefDirectly(stimulations[stepNr]);

                    // try to not submit "stim on" immediately
                    // stimManager.StartAll();
                    shouldSubmitStimOn = true;
                    elapsedTimeForStimOn = 0;
                }
            }
            running = !running;
        }

        private int GetStepNR(int patternIndexIter)
        {
            int stepNr = -1; // negative if we were executing a "pause' inter step interval
            if (useISI)
            {
                // we don't care here if there was also a isi padded at the end
                if (patternIndexIter % 2 == 1) stepNr = -1; // we were in a ISI
                else stepNr = patternIndexIter / 2;
            } else
            {
                stepNr = patternIndexIter;
            }

            return stepNr;
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

        /**
         * we assume here stepIndex is valid
         * */
        private void SubmitStep (int stepIndex)
        {

        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SpatioTemporalPatternTester))]
    public class PatternTesterEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SpatioTemporalPatternTester script = target as SpatioTemporalPatternTester;

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

