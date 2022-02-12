
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Simple class representing an arrow on the gamescreen
    /// </summary>
    public class ArrowVisualizer : UdonSharpBehaviour
    {
        public Renderer[] myRenderers;//Renderers associated to the visualizer, should be passed to the prefab in the scene. Multiple in case we have sub elements
        void Start()
        {

        }

        /// <summary>
        /// Material/Color of the arrow should change depending on whatever we want, change it for each renderer associated to this ArrowVisualizer
        /// </summary>
        /// <param name="mat">Material/Color to change this arrow to</param>
        public void changeMat(Material mat)
        {
            foreach (var renderer in myRenderers)
                renderer.material = mat;
        }
    }
}