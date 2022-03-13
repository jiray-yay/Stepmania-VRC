
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

        public override void Interact()
        {
            DoHideShow();
        }

        public void DoHideShow()
        {
            toShowHide.SetActive(!toShowHide.activeSelf);
        }
    }
}