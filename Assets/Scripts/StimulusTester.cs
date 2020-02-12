using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Tactility
{
    public class StimulusTester : MonoBehaviour
    {
        public enum KeyPressType { KeyPressedDown, KeyPressed } // down=true only the first time

        [Header("Settings")]

        [SerializeField]
        [Range(0f, 9f)]
        private float intensity = 2.5f; // between 0.1 and 9mA

        [SerializeField]
        [Range(30, 500)]
        private int pulseWidth = 200; // between 30 and 500us

        [SerializeField]
        [Range(1, 200)]
        private int frequency = 35; // between 1 and 200hz

        [Header("Settings - Key bindings")]

        [SerializeField]
        private KeyPressType keyPressType = KeyPressType.KeyPressedDown;

        [SerializeField]
        private KeyCode toggleOnOffKeyCode = KeyCode.Space;

        [SerializeField]
        private KeyCode increaseIntensityKeyCode = KeyCode.RightArrow;
        [SerializeField]
        private KeyCode decreaseIntensityKeyCode = KeyCode.LeftArrow;

        [SerializeField]
        private KeyCode increasePulseWidthKeyCode = KeyCode.L;
        [SerializeField]
        private KeyCode decreasePulseWidthKeyCode = KeyCode.J;

        [SerializeField]
        private KeyCode increaseFrequencyKeyCode = KeyCode.UpArrow;
        [SerializeField]
        private KeyCode decreaseFrequencyKeyCode = KeyCode.DownArrow;

        [Header("Debugging Info")]
        [SerializeField]
        [SouthernForge.Utils.ReadOnly]
        private bool running = false;

        // external components
        private TactilityStimulatorManager stimManager;

        private int id = 11;    // velec id to use for testing
        private new string name = "test";

        // resolutions
        private float currentResolution = 0.1f;
        private int pulseWidthResolution = 1;
        private int frequencyResolution = 1;

        // previous values
        private float previousIntensity;
        private int previousPulseWidth;
        private int previousFrequency;
        private KeyPressType previousKeyPressType;

        private Stimulation currentStim;

        delegate bool PtrToKeyDownFn(KeyCode keycode);
        private PtrToKeyDownFn keyDownFn;

        private void Awake()
        {
            stimManager = FindObjectOfType<TactilityStimulatorManager>();

            // create stim
            uint anode = 16384;
            currentStim = new Stimulation(id, name, intensity, pulseWidth, new int[] { 10 }, anode);

            // save initialValues
            previousIntensity = intensity;
            previousPulseWidth = pulseWidth;
            previousFrequency = frequency;
            previousKeyPressType = keyPressType;

            // set default key press callback
            if (keyPressType == KeyPressType.KeyPressedDown)
            {
                keyDownFn = Input.GetKeyDown;
            } else
            {
                keyDownFn = Input.GetKey;
            }
        }


        void Update()
        {

            if (Input.GetKeyDown(toggleOnOffKeyCode))
            {

                if (running)
                {
                    // stimManager.StopAll();
                    stimManager.Remove(currentStim.id);
                }
                else
                {
                    stimManager.Add(currentStim);
                }
                running = !running;
            }

            // make sure this key code don't enable the stim if the stim should not be played
            if (keyDownFn(increaseIntensityKeyCode)) IncreaseIntensity();
            if (keyDownFn(decreaseIntensityKeyCode)) DecreaseIntensity();

            if (keyDownFn(increasePulseWidthKeyCode)) IncreasePulseWidth();
            if (keyDownFn(decreasePulseWidthKeyCode)) DecreasePulseWidth();

            if (keyDownFn(increaseFrequencyKeyCode)) IncreseFrequency();
            if (keyDownFn(decreaseFrequencyKeyCode)) DecreaseFrequency();

            // update stim if the values were modifed (for now, it will enable the stimulus)
            if (previousIntensity != intensity || previousPulseWidth != pulseWidth)
            {
                previousIntensity = intensity;
                previousPulseWidth = pulseWidth;

                currentStim.intensity = intensity;
                currentStim.pulseWidth = pulseWidth;

                stimManager.Add(currentStim); // currentStim is always active for now

                running = true;
            }
            
            // check if we need to send data to the stimulator
            if (previousFrequency != frequency)
            {
                previousFrequency = frequency;
                stimManager.SetFrequency(frequency);
            }

        }


        void IncreaseIntensity ()
        {
            intensity += currentResolution;
            intensity = Mathf.Clamp(intensity, 0, 9f);
            currentStim.intensity = intensity;
            // UpdateVirtualElectrode();
        }

        void DecreaseIntensity ()
        {
            intensity -= currentResolution;
            intensity = Mathf.Clamp(intensity, 0, 9f);
            currentStim.intensity = intensity;
            // UpdateVirtualElectrode();
        }

        void IncreasePulseWidth ()
        {
            pulseWidth += pulseWidthResolution; ;
            pulseWidth = Mathf.Clamp(pulseWidth, 30, 500);
            currentStim.pulseWidth = pulseWidth;
            // UpdateVirtualElectrode();
        }

        void DecreasePulseWidth ()
        {
            pulseWidth -= pulseWidthResolution;
            pulseWidth = Mathf.Clamp(pulseWidth, 30, 500);
            currentStim.pulseWidth = pulseWidth;
            // UpdateVirtualElectrode();
        }

        void IncreseFrequency()
        {
            frequency += frequencyResolution;
            frequency = Mathf.Clamp(frequency, 1, 200);
        }

        void DecreaseFrequency()
        {
            frequency -= frequencyResolution;
            frequency = Mathf.Clamp(frequency, 1, 200);
        }

        void UpdateVirtualElectrode()
        {
            // port.WriteLine("velec 11 *name index_finger *elec 1 *cathodes 1=0,2=0,3=0,4=0,5=0,6=0,7=0,8=0,9=0,10=1,11=0,12=0,13=0,14=0,15=0,16=0,17=0,18=0,19=0,20=0,21=0,22=0,23=0,24=0,25=0,26=0,27=0,28=0,29=0,30=0,31=0,32=0, *amp 1=0,2=0,3=0,4=0,5=0,6=0,7=0,8=0,9=0,10=" + intensity + ",11=0,12=0,13=0,14=0,15=0,16=0,17=0,18=0,19=0,20=0,21=0,22=0,23=0,24=0,25=0,26=0,27=0,28=0,29=0,30=0,31=0,32=0, *width 1=0,2=0,3=0,4=0,5=0,6=0,7=0,8=0,9=0,10=300,11=0,12=0,13=0,14=0,15=0,16=0,17=0,18=0,19=0,20=0,21=0,22=0,23=0,24=0,25=0,26=0,27=0,28=0,29=0,30=0,31=0,32=0, *anode 16384 *selected 1 *sync 0");
        }
    }

}

