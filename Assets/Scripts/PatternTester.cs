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
     * Utility to test modulation of different patterns
     * */
    public class PatternTester : MonoBehaviour
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
        private int frequency = 35; // between 1 and 200hz

        [Header("Pattern Settings")]

        [SerializeField]
        [Tooltip("pattern to test different modulation values")]
        private StimPattern pattern; // by now it only contains spatial layout (not any temporal value)
        
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
                // simplest case: No Inter Step Interval
                if (!useISI)
                {
                    // divide pattern period in equal parts for each step
                    int nrSteps = pattern.steps.Length;
                    float patternDurationMS = 1000.0f / value;   // 'value' is the intended frequency

                    stepDuration = patternDurationMS / nrSteps; // this to be valid, should be in the order of milliseconds and higher than system period

                    // now we should check the speed of the stimulator
                    // keep it out for the moment
                    // int systemFreq = stimManager.GetFrequency();
                    // float systemPeriodMS = 1000.0f / systemFreq;

                } else
                {
                    if (useISIAfterLastStep)
                    {

                    }

                }
                _patternFrequency = value;
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
        private Stimulation currentStim;
        // hardcoded values used for the test velec
        private const int id = 11;    // velec id to use for testing
        private new const string name = "test";

        // pattern iteration related vars
        private float[] durations;
        private float elapsedTimePlaying = 0f;
        private int patternIndexIterator = 0; // also used for playing ISIs when index is odd (and when using ISI of course)

        private bool patternsParamsDirty = true; // to indicate us that we need to calculate again stepDuration

        private void Awake()
        {
            stimManager = FindObjectOfType<TactilityStimulatorManager>();
            CalculateDurations();
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
                    elapsedTimePlaying += Time.deltaTime;
                    if (elapsedTimePlaying > durations[patternIndexIterator])
                    {
                        // time to move along the pattern pieces (either a step or a ISI)
                        
                        // patternIndexIterator

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

            // create stim
            uint anode = 16384;
            currentStim = new Stimulation(id, name, intensity, pulseWidth, new int[] { 10 }, anode, true);

            // submit velec definition to stimManager
            // stimManager.SubmitStim(currentStim);

            ready = true;
        }

        public void ToggleRunning ()
        {
            if (running) {
                // stop
                stimManager.SetSelected0(currentStim.ID);
                elapsedTimePlaying = 0;
            } else
            {
                // play (from the beginning)
                // stimManager.SubmitVelecDefDirectly(currentStim); // velec def
                if (pattern != null && pattern.steps.Length > 0)
                {
                    SubmitStep(0);
                }
            }
            running = !running;
        }

        public void SubmitFrequency ()
        {
            stimManager.SetFrequency(frequency);
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

        // to execute every time the frequency changes or some option that modifies ISI
        private void CalculateDurations ()
        {
            float patternDurationMS = 1000f / _patternFrequency;

            if (useISI)
            {
                if (useISIAfterLastStep)
                {
                    // make sure to include case when pattern consists of a single step (DONE: works!)
                    durations = new float[pattern.steps.Length * 2];
                    float stepsDuration = patternDurationMS * stepDuty;
                    float isisDuration = patternDurationMS - stepsDuration;
                    float singleStepDuration = stepsDuration / pattern.steps.Length;
                    float singleISIDuration = isisDuration / pattern.steps.Length;
                    for (int i = 0; i < durations.Length; ++i)
                    {
                        if (i % 2 == 0) durations[i] = singleStepDuration;
                        else durations[i] = singleISIDuration;
                    }
                }
                else
                {
                    // if pattern consists of only a single step and we didn't enable a last inter step interval, throw error.
                    if (pattern.steps.Length == 1) throw new Exception("Pattern only consists of a single step and last ISI was not enabled.");
                    durations = new float[pattern.steps.Length * 2 - 1];
                    float stepsDuration = patternDurationMS * stepDuty;
                    float isisDuration = patternDurationMS - stepsDuration;
                    float singleStepDuration = stepsDuration / pattern.steps.Length;
                    float singleISIDuration = isisDuration / (pattern.steps.Length - 1);    // error when pattern has only one step

                    for (int i = 0; i < durations.Length; ++i)
                    {
                        if (i % 2 == 0) durations[i] = singleStepDuration;
                        else durations[i] = singleISIDuration;
                    }
                }
            } else
            {
                durations = new float[pattern.steps.Length];
                // each element in the array has the same duration
                float individualDuration = patternDurationMS / durations.Length;
                for (int i = 0; i < durations.Length; ++i)
                {
                    durations[i] = individualDuration;
                }
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PatternTester))]
    public class PatternTesterEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PatternTester script = target as PatternTester;

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

