using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Tactility.Actions
{
    [CreateAssetMenu(fileName = "newPokingAction", menuName = "Tactility/Exploratory Action/Poking")]
    public class PokingAction : ExploratoryAction
    {
        // public new HandPart[] involvedParts = new HandPart[1] { HandPart.Index };

        // public new VirtualElectrode[] virtualElectrodes = new VirtualElectrode[1];

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

        #region unity events
        // if we initialize arrays here, we will reset the exploratory action every time we open the editor
        // private void Awake()
        // {
        //     Debug.Log("[" + GetType().Name + "] Awake");
        //     involvedParts = new HandPart[1] { HandPart.Index };
        //     virtualElectrodes = new VirtualElectrode[1];
        // }

        // private void OnEnable()
        // {
        //     Debug.Log("[" + GetType().Name + "] OnEnable");
        // }

        // private void OnDisable()
        // {
        //     Debug.Log("[" + GetType().Name + "] OnDisable");
        // }

        #endregion unity events
    }

}

