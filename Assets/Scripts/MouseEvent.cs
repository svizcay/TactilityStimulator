using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Inria.Tactility
{
    [RequireComponent(typeof(Collider))]
    public class MouseEvent : MonoBehaviour
    {
        [Header("Events")]

        [SerializeField]
        private UnityEvent onMouseEnter = default;

        [SerializeField]
        private UnityEvent onMouseExit = default;

        private void OnMouseEnter()
        {
            // print("mouse enter");
            onMouseEnter.Invoke();
        }

        private void OnMouseExit()
        {
            // print("mouse exit");
            onMouseExit.Invoke();
        }

    }

}

