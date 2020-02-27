using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;// for UnityEvent class (visible in Inspector)

namespace Inria.Tactility
{
    public class CollisionDetector : MonoBehaviour
    {
        [Header("Settings")]

        [SerializeField]
        private LayerMask reactoTo = default;

        [SerializeField]
        private UnityEvent onCollisionEnter = default;

        [SerializeField]
        private UnityEvent onCollisionStay = default;

        [SerializeField]
        private UnityEvent onCollisionExit = default;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        private void OnCollisionEnter(Collision collision)
        {
            print("collision enter with gameObject=" + collision.gameObject.name);
            if (((1 << collision.gameObject.layer) & reactoTo) != 0)
            {
                onCollisionEnter.Invoke();
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & reactoTo) != 0)
            {
                // print("collision stay with gameObject=" + collision.gameObject.name);
                onCollisionStay.Invoke();
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            print("collision exit with gameObject=" + collision.gameObject.name);
            if (((1 << collision.gameObject.layer) & reactoTo) != 0)
            {
                onCollisionExit.Invoke();
            }
        }

    }

}

