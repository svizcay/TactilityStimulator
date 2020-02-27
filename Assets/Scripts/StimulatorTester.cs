using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Use Debugging as namespace and not Debug in order to avoid conflict with Unity Debug namespace
namespace Inria.Tactility.Debugging
{
    /**
     * Utility to test different capabilites of the stimulator and see how this reponds to commands.
     * It's basically used to associate keys with commands. (try to avoid toggle buttons since we don't know the internal state of the system yet)
     * list of commands that we would like to submit from here:
     * - send velec definition (having the option to choose between selected 0 or 1)
     * - resend velec definition with new parameters (intensity, pulse width, selected)
     * - send stim on/off   # avoid using stim off (use selected=0 instead).
     * - send stim <name>   # avoid using this (use selected=1 + stim on instead)
     * - send velev <id> *selected 0 or 1   # selected=1 not working (use velec re definition instead)
     * avoid reacting to "OnValidate". it might send velec redinition when we dont actually want to do it
     * optional: add setting to emit frequency command
     * */
    public class StimulatorTester : MonoBehaviour
    {
        [Header("Settings")]

        /** 
         * when playing stim with "stim <name>", selected value is ignored
         * when there was a previous stim being played with "stim on", if velec is defined with selected=1, this will start automatically
         * */
        [SerializeField]
        [Tooltip("selected value to send during velec definition")]
        private bool velecDefSelected = false;  // 

        /** 
         * velec <id> *selected 0: will stop stim and won't be able to resume again
         * velec <id> *selected 1: doesn't work
         * */
        // [SerializeField]
        // [Tooltip("selected value to send when submitting velec selected commmand")]
        // private bool velecSetSelected = false;

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

        [Header("Settings - Key bindings")]

        [SerializeField]
        private KeyCode submitVelecDefinitionKeyCode = KeyCode.Alpha5;

        [SerializeField]
        private KeyCode submitStimOnKeyCode = KeyCode.Alpha6;

        [SerializeField]
        private KeyCode submitStimOffKeyCode = KeyCode.Alpha7;

        [SerializeField]
        private KeyCode submitVelecSelected0KeyCode = KeyCode.Alpha8;

        [SerializeField]
        private KeyCode submitStimPlayThisKeyCode = KeyCode.Alpha9;

        [SerializeField]
        private KeyCode submitFrequencyKeyCode = KeyCode.Alpha0;

        [Header("Debugging Info")]

        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private bool ready = false;

        // internal data
        private Stimulation currentStim;

        // external components
        private TactilityStimulatorManager stimManager;

        // hardcoded values used for the test velec
        private const int id = 11;    // velec id to use for testing
        private new const string name = "test";

        private void Awake()
        {
            stimManager = FindObjectOfType<TactilityStimulatorManager>();
        }

        private void Start()
        {
            StartCoroutine(Initialize());
        }

        void Update()
        {
            if (ready)
            {
                if (Input.GetKeyDown(submitVelecDefinitionKeyCode))
                {
                    SubmitVelecDef();
                } else if (Input.GetKeyDown(submitStimOnKeyCode))
                {
                    SubmitStimOn();
                } else if (Input.GetKeyDown(submitStimOffKeyCode))
                {
                    SubmitStimOff();
                } else if (Input.GetKeyDown(submitVelecSelected0KeyCode))
                {
                    SubmitVelecSelected0();
                } else if (Input.GetKeyDown(submitStimPlayThisKeyCode))
                {
                    SubmitPlayThisStim();
                } else if (Input.GetKeyDown(submitFrequencyKeyCode))
                {
                    SubmitFrequency();
                }
            }
        }

        // gets called continously (ie, while moving a slider, gets called multiple times and not just on button release)
        // shouldn't activa the stim if stim is off
        // private void OnValidate()
        // {
        //     // print(Time.frameCount + " OnValidate");
        //     // if (currentStim != null)
        //     // {
        //     //     currentStim.intensity = intensity;
        //     //     currentStim.pulseWidth = pulseWidth;

        //     //     stimManager.UpdateStim(currentStim, true, newSelectedValue);
        //     //     // stimManager.UpdateStim(currentStim);

        //     //     previousIntensity = intensity;
        //     //     previousPulseWidth = pulseWidth;
        //     // }
        // }

        IEnumerator Initialize()
        {
            while (!stimManager.initialized)
            {
                yield return null;
            }

            // create stim
            uint anode = 16384;
            currentStim = new Stimulation(id, name, intensity, pulseWidth, new int[] { 10 }, anode, velecDefSelected);

            // submit velec definition to stimManager
            // stimManager.SubmitStim(currentStim);

            ready = true;
        }

        public void SubmitVelecDef()
        {
            // update internal stim object and send it (bypass stimManager logic)
            currentStim.active = velecDefSelected;
            currentStim.intensity = intensity;
            currentStim.pulseWidth = pulseWidth;
            stimManager.SubmitVelecDefDirectly(currentStim); // velec def
        }

        public void SubmitStimOn()
        {
            stimManager.StartAll(); // stim on
        }

        public void SubmitStimOff()
        {
            stimManager.StopAll(); // stim off
        }

        public void SubmitVelecSelected0 ()
        {
            stimManager.SetSelected0(currentStim.id);
        }

        public void SubmitPlayThisStim ()
        {
            stimManager.PlayStim(currentStim.name);
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
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(StimulatorTester))]
    public class StimulatorTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            StimulatorTester script = target as StimulatorTester;

            if (!script.IsReady()) return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Submit Velec Definition"))
            {
                script.SubmitVelecDef();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Submit Stim On"))
            {
                script.SubmitStimOn();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Submit Stim Off"))
            {
                script.SubmitStimOff();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Submit Velect Selected 0"))
            {
                script.SubmitVelecSelected0();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Submit Play this Stim"))
            {
                script.SubmitPlayThisStim();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Submit Frequency"))
            {
                script.SubmitFrequency();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

#endif

}

