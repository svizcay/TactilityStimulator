using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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

        // inter pulse interval in milliseconds
        public const float IPI_MS = 0.5f; // 500us
    }
    public class Stimulation
    {
        // static data
        public int ID   // velec id
        {
            get;
            private set;
        }
        public string Name // velec name
        {
            get;
            private set;
        }

        // dynamic data

        // stimulator is executing it (stim <stimName> + selected=1)
        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set {
                this._selected = value;
                UpdateCommandStrSelected();
            }

        }

        private float    _intensity;    // current value of intensity
        private int      _pulseWidth;    // current value of pulse width

        // they are already calculated for a specific connector
        // static or dynamic data?
        // let's keep it static for now
        // TODO: make it dynamic later for spatio-temporal patterns
        private int[]    _cathodes;
        public uint     anodes; //

        // whenever setting the catodes, we need to set commandStrIntensity and commandStrPulseWidth

        // properties (values are checked when they are set)

        public int[] Cathodes
        {
            get { return this._cathodes; }
            set {
                bool updateIntensityAndPulseWidthCommand = (this._cathodes == null) ? false : true;
                this._cathodes = value;
                UpdateCommandStrCathodes();

                if (updateIntensityAndPulseWidthCommand)
                {
                    UpdateCommandStrItensity();
                    UpdateCommandStrPulseWidth();
                }
            }

        }

        public float Intensity
        {
            get {
                return this._intensity;
            }

            set {
                this._intensity = CheckIntensity(value);
                UpdateCommandStrItensity();
            }
        }

        public int PulseWidth
        {
            get {
                return this._pulseWidth;
            }

            set {
                this._pulseWidth = CheckPulseWidth(value);
                UpdateCommandStrPulseWidth();
            }
        }

        private string commandStrCathodes = "";
        private string commandStrIntensity = "";
        private string commandStrPulseWidth = "";
        private string commandStrSelected = "";

        private bool commandStrDirty = true;
        private string cachedCommand = "";

        /**
         * cathodes: list of channel to used as cathodes (already taking into acocunt electrode type and connector being used)
         * anodes: long int telling the bitmaks of channels used as anodes (already taking into account electrode type and connector being used)
         * */
        public Stimulation (int id, string name, float intensity, int pulseWidth, int[] cathodes, uint anodes, bool selected = true)
        {
            if (id < 10 || id > 16) throw new Exception("Invalid velec id=" + id + ". We can only used ids in the range [10-16]");
            this.ID = id;
            this.Name = name;
            this.Selected = selected;

            this.Cathodes = cathodes;
            this.anodes = anodes;

            this.Intensity = intensity;     // check will be performed in setter
            this.PulseWidth = pulseWidth;   // check will be performed in setter
        }

        /**
         * Doesn't throw an exception but will warn the user about clampping.
         * */
        private float CheckIntensity (float val)
        {
            float finalIntensity = val;

            if (val < StimulationConstants.MIN_INTENSITY || val > StimulationConstants.MAX_INTENSITY)
            {
                UnityEngine.Debug.LogWarning("clamping intensity to ["
                    + StimulationConstants.MIN_INTENSITY + "," + StimulationConstants.MAX_INTENSITY +
                    "] range for stim id=" + ID + " name=" + Name);

                finalIntensity = Mathf.Clamp(val, StimulationConstants.MIN_INTENSITY, StimulationConstants.MAX_INTENSITY);
            }

            return finalIntensity;
        }

        /**
         * Doesn't throw an exception but will warn the user about clampping.
         * */
        private int CheckPulseWidth (int val)
        {
            int finalPulseWidth = val;
            if (val < StimulationConstants.MIN_PULSE_WIDTH || val > StimulationConstants.MAX_PULSE_WIDTH)
            {
                UnityEngine.Debug.LogWarning("clamping pulseWidth to ["
                    + StimulationConstants.MIN_PULSE_WIDTH + "," + StimulationConstants.MAX_PULSE_WIDTH +
                    "] range for stim id=" + ID + " name=" + Name);

                finalPulseWidth = Mathf.Clamp(val, StimulationConstants.MIN_PULSE_WIDTH, StimulationConstants.MAX_PULSE_WIDTH);
            }

            return finalPulseWidth;
        }

        private void UpdateCommandStrCathodes ()
        {
            List<int> cathodesList = new List<int>(_cathodes);

            StringBuilder builder = new StringBuilder();

            builder.Append(" *cathodes ");
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

            commandStrCathodes = builder.ToString();

            commandStrDirty = true;
        }

        private void UpdateCommandStrSelected ()
        {
            if (Selected)
            {
                commandStrSelected = " *selected 1";
            } else
            {
                commandStrSelected = " *selected 0";
            }

            commandStrDirty = true;
        }

        /**
         * This is supposed to be called when cathodes are well known and we have just updated the intensity value.
         * Intensity value has already being clamped
         * */
        private void UpdateCommandStrItensity ()
        {
            List<int> cathodesList = new List<int>(_cathodes); // conver them to a list just for the sake of using Contains method. Use always a list later in the future

            StringBuilder builder = new StringBuilder();
            builder.Append(" *amp ");

            for (int i = 1; i <= 32; ++i)
            {
                if (cathodesList.Contains(i))
                {
                    builder.Append(i.ToString() + "=" + Intensity + ",");//trailing comma doesn't not matter at the end
                } else
                {
                    builder.Append(i.ToString() + "=0,");//trailing comma doesn't not matter at the end
                }
            }

            commandStrIntensity = builder.ToString();

            commandStrDirty = true;
        }

        private void UpdateCommandStrPulseWidth ()
        {
            List<int> cathodesList = new List<int>(_cathodes); // conver them to a list just for the sake of using Contains method. Use always a list later in the future

            StringBuilder builder = new StringBuilder();

            builder.Append(" *width ");

            for (int i = 1; i <= 32; ++i)
            {
                if (cathodesList.Contains(i))
                {
                    builder.Append(i.ToString() + "=" + PulseWidth + ",");//trailing comma doesn't not matter at the end
                } else
                {
                    builder.Append(i.ToString() + "=0,");//trailing comma doesn't not matter at the end
                }
            }

            commandStrPulseWidth = builder.ToString();

            commandStrDirty = true;
        }

        /*
         * TODO: try to catch string if it's not modified that frequently or if it only changes few parameters
         * this command only needs to be sent when the parameters have changed! (dont sent them every frame)
         * */ 
        public string GetStimCommand ()
        {
            if (commandStrDirty)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("velec ").Append(ID.ToString())
                    .Append(" *name ").Append(Name)
                    .Append(" *elec 1")
                    .Append(commandStrCathodes)
                    .Append(commandStrIntensity)
                    .Append(commandStrPulseWidth)
                    .Append(" *anode " + anodes)
                    .Append(commandStrSelected)
                    .Append(" *sync 0");

                cachedCommand = builder.ToString();
                commandStrDirty = false;
            }

            return cachedCommand;
        }
    }

}
