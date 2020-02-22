using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inria.Utils
{
    /// <summary>
    ///  Basic Free Fly Camera sibr style (move with A-S-D-W and rotate with I-J-K-L keys).
    ///  Press space bar to move faster.
    /// </summary>
    public class FreeFlyCamera : MonoBehaviour {

        [Header("Settings")]
        public float movSpeed = 10;
        private float speedMultiplier = 5f;

        private float mouseHorizontalSensitivity = 3.0f;
        private float mouseVerticalSensitivity = 3.0f;

        private Vector3 movDirection;

        //public Vector2 mouseDirection;
        public bool useMouseToRotate = false;
        private float minVerticalAngle = -89;
        private float maxVerticalAngle = 89;

        public float rotationX = 0;
        private float rotationY = 0;
        private float rotationZ = 0;

        public float rotationSpeed = 20;

        public KeyCode resetTransformKey = KeyCode.R;
        private Vector3 originalPos;
        private Quaternion originalRot;

        #region unity events

        private void Awake()
        {
            originalPos = transform.position;
            originalRot = transform.rotation;
        }

        void Update () {


            // rotate first
            if (useMouseToRotate)
            {
                rotationX -= Input.GetAxis("Mouse Y") * mouseVerticalSensitivity;
                rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

                float delta = Input.GetAxis("Mouse X") * mouseHorizontalSensitivity;
                rotationY = transform.localEulerAngles.y + delta;

                transform.localEulerAngles = new Vector3(rotationX, rotationY, 0);
            } else
            {
                //float delta = Input.GetKey(KeyCode.I) ? 1 : 0;
                //delta *= mouseVerticalSensitivity;
                //rotationX = transform.localEulerAngles.x - delta;

                float deltaX = Input.GetKey(KeyCode.I) ? -1 : 0;
                deltaX +=  Input.GetKey(KeyCode.K) ? +1 : 0;

                float deltaY = Input.GetKey(KeyCode.J) ? -1 : 0;
                deltaY += Input.GetKey(KeyCode.L) ? +1 : 0;

                float deltaZ = Input.GetKey(KeyCode.O) ? -1 : 0;
                deltaZ += Input.GetKey(KeyCode.U) ? +1 : 0;

                transform.Rotate(new Vector3(deltaX, deltaY, deltaZ) * Time.deltaTime * rotationSpeed);

                //transform.localEulerAngles = new Vector3(rotationX, rotationY, rotationZ);
            }



            // translate
            movDirection.x = Input.GetAxisRaw("Horizontal");   // right
            movDirection.z = Input.GetAxisRaw("Vertical");     // forward
            movDirection.y = Input.GetKey(KeyCode.Q) ? -1 : 0;
            movDirection.y += Input.GetKey(KeyCode.E) ? 1 : 0;

            movDirection.Normalize();

            bool firePressed = Input.GetButton("Jump");

            float speedFactor = firePressed ? speedMultiplier : 1;

            movDirection *= Time.deltaTime * movSpeed * speedFactor;

            //transform.position = transform.position + movDirection;

            transform.Translate(movDirection);

            if (Input.GetKeyDown(resetTransformKey)) ResetTransform();
        }

        #endregion unity events

        #region private methods
        private void ResetTransform ()
        {
            transform.position = originalPos;
            transform.rotation = originalRot;
        }
        #endregion
    }

}

