using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Tactility.Utils
{
    public class ResetInteractableObjs : MonoBehaviour
    {
        [Header("Settings")]

        [SerializeField]
        private KeyCode resetKey = KeyCode.R;

        [SerializeField]
        private Transform[] objects;

        private Vector3[] originalPos;
        private Quaternion[] originalRot;

        // private data

        private void Awake()
        {
            originalPos = new Vector3[objects.Length];
            originalRot = new Quaternion[objects.Length];

            for (int i = 0; i < objects.Length; ++i)
            {
                originalPos[i] = objects[i].transform.position;
                originalRot[i] = objects[i].transform.rotation;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(resetKey)) Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < objects.Length; ++i)
            {
                objects[i].transform.position = originalPos[i];
                objects[i].transform.rotation = originalRot[i];

                Rigidbody rb = objects[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            
        }
    }

}

