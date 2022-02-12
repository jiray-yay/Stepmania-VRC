
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Visualization on the gamescreen of a "button" pad press
    /// </summary>
    public class PressVisualizer : UdonSharpBehaviour
    {
        public Vector3 pressAdd = new Vector3(0.2f, 0.2f, 0);//How much bigger is a press represented compared to an non press, can be passed in scene
        public float decreaseTime = 5f / 60f;//How much time is needed for the visualizer to go from hold visualization to default, can be passed in scene
        private Vector3 baseSize;
        void Start()
        {
            baseSize = transform.localScale;
        }

        public bool isActive = false;
        bool wasActive = false;
        float timeDesactivation = 0f;
        void LateUpdate()
        {
            if (isActive)
            {
                transform.localScale = baseSize + pressAdd;
                wasActive = true;
                isActive = false;
                return;
            }
            isActive = false;
            if (wasActive)
            {
                timeDesactivation = Time.time - Time.deltaTime;
                wasActive = false;
            }

            if (transform.localScale != baseSize)
            {
                float ratio = (Time.time - timeDesactivation) / decreaseTime;
                transform.localScale = ratio >= 1.0f ? baseSize : baseSize + (1f - ratio) * pressAdd;
            }

        }
    }
}