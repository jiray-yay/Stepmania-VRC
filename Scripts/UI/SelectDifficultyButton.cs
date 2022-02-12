
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// A single difficulty selection button
    /// </summary>
    public class SelectDifficultyButton : UdonSharpBehaviour
    {
        public ScoreManager scoreManager;//scoreManager/player side associated with the button
        public int partition;//partition the button will select if being pressed
        public UnityEngine.UI.Text text;//text of the button
        void Start()
        {

        }
        /// <summary>
        /// Called by onclick/press
        /// </summary>
        public void doSelectDifficulty()
        {
            if (scoreManager.stepfilesManager.audioSource.isPlaying)
                return;
            Networking.SetOwner(Networking.LocalPlayer, scoreManager.gameObject);
            scoreManager.partition = partition;
        }
    }
}