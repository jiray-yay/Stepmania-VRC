
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Simple button to hide or show a gameObject, in this project, used to hide/show the pads' settings
    /// </summary>
    public class HideShowGameobject : UdonSharpBehaviour
    {
        public GameObject toShowHide;
        void Start()
        {

        }

        void Interact()
        {
            toShowHide.SetActive(!toShowHide.activeSelf);
        }
    }
}