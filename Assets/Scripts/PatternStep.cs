using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Tactility
{

    [CreateAssetMenu(fileName = "newPatternStep", menuName = "Tactility/Pattern Step")]
    public class PatternStep : ScriptableObject
    {
        // public HandPart[] involvedParts;

        /*
         * It's a list because a single step, might consist in the stimulation of both, the index and the thumb at the same time
         * */
        public VirtualElectrode[] virtualElectrodes;

        // // Start is called before the first frame update
        // void Start()
        // {
        //     
        // }

        // // Update is called once per frame
        // void Update()
        // {
        //     
        // }
    }

}

