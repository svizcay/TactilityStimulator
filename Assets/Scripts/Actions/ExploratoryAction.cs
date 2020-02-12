using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Tactility.Actions
{
    public enum HandPart { Index, Thumb, Palm, Dorsal }

    // [CreateAssetMenu(fileName = "newExploratoryAction", menuName = "Tactility/Exploratory Action")]
    public class ExploratoryAction : ScriptableObject
    {
        public HandPart[] involvedParts;
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

