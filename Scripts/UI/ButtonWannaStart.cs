
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Simple buttons that starts the song for everyone and claims the stepFileManager's ownership
    /// </summary>
    public class ButtonWannaStart : UdonSharpBehaviour
    {
        public StepfilesManager stepfilesManager;//stepfilemanager associated with the song to start, should be passed in scene
        void Start()
        {

        }

        void Interact()
        {
            Networking.SetOwner(Networking.LocalPlayer, stepfilesManager.gameObject);
            if (!stepfilesManager.hasStarted)
                stepfilesManager.wannaStart = true;
            else
            {
                stepfilesManager.wannaStart = false;
                stepfilesManager.hasStarted = false;
            }
        }
    }
}
