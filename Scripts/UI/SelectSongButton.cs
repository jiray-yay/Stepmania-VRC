
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// A single music selection button
    /// </summary>
    public class SelectSongButton : UdonSharpBehaviour
    {
        public StepfilesManager stepfilesManager;//stepfilesmanager associated with the button
        public UnityEngine.UI.Text textButton;//ui text that shows the name of song
        public int music;//id of the music that will be selected if button is clicked
        void Start()
        {

        }
        /// <summary>
        /// Called by onclick/press
        /// </summary>
        public void doSelectSongButton()
        {
            if (!stepfilesManager.audioSource.isPlaying && !stepfilesManager.wannaStart)
            {
                Networking.SetOwner(Networking.LocalPlayer, stepfilesManager.gameObject);
                stepfilesManager.changeMusic(music);
            }
        }
    }
}
