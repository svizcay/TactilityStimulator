using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Tactility
{

    [CreateAssetMenu(fileName = "newStimPattern", menuName = "Tactility/Stimulation Pattern")]
    public class StimPattern : ScriptableObject
    {
        // public HandPart[] involvedParts;

        /*
         * List of steps
         * */
        public PatternStep[] steps;

    }

}

