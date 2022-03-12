
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// An interactable ParaPara pad
    /// </summary>
    public class ParaPadManager : UdonSharpBehaviour //I wish U# could handle inheritance ;_; , this class is copypasted then edited from DancePadManager
    {
        public ParaPadArrow[] paraPadArrows;//the para pad bases on the ground used by this parapad, should passed in scene
        public PressVisualizer[] pressVisualizers;//press visualizers reacting to the pad presses, should be passed in scene
        [HideInInspector]
        public bool[] areActive;//pad arrows activated this frame
        [HideInInspector]
        public bool[] wereActive;//pad arrows activated previous frame

        int[] toCheck = { (int)HumanBodyBones.LeftHand, (int)HumanBodyBones.RightHand };//Pseudo const table

        [HideInInspector]
        public ScoreManager scoreManager;//the score manager that will use the inputs from this dancepad, hidden in inspector because should be passed through stepfilesmanager

        [HideInInspector]
        public Vector3 rotationOffset = Vector3.zero;//Setting of the user representing the inclinaison offset for angle from wrist to fingers for the collider
        [HideInInspector]
        public float colliderSize = 0.1f, handOffset = -0.2f, y_base = 3f;//Setting of the user for the "collider" size and position

        [HideInInspector]
        public bool useHaptics = false;

        void Start()
        {
            areActive = new bool[paraPadArrows.Length];
            wereActive = new bool[paraPadArrows.Length];
        }

        /// <summary>
        /// Score manager has to wait to be linked with a parapad before having active/inactive presses
        /// </summary>
        public void initScoreManagerArrays()
        {
            scoreManager.areActive = new bool[paraPadArrows.Length];
            scoreManager.wereActive = new bool[paraPadArrows.Length];
        }

        /// <summary>
        /// Arrow visualization is made by everyone, but should be done only when someone is near the parapad. The ParaPadManager itself has a trigger collider to detect that
        /// </summary>
        /// <param name="player"></param>
        public override void OnPlayerTriggerStay(VRC.SDKBase.VRCPlayerApi player)
        {
            activateArrows(player);
        }

        /// <summary>
        /// Manages the presses on each arrow, both only for visual (with anyone) or for the actual score (if the player stepping in is the owner of the score manager)
        /// </summary>
        /// <param name="player">Player stepping on the pad</param>
        public void activateArrows(VRC.SDKBase.VRCPlayerApi player)
        {
            //everyone affects the activation of the visual
            for (int i = 0; i < paraPadArrows.Length; i++)
            {
                if (areActive[i])
                    continue;
                foreach (int bone in toCheck)
                {

                    areActive[i] = paraPadArrows[i].checkInside(player.GetBonePosition((HumanBodyBones)bone),player.GetBoneRotation((HumanBodyBones)bone),rotationOffset,handOffset,colliderSize);
                    if (areActive[i])
                    {
                        pressVisualizers[i].isActive = true;
                        break;
                    }
                }
            }

            //Player of machine is in pad and owns the score = update the actual step check
            if (player.playerId == Networking.LocalPlayer.playerId && player.IsOwner(scoreManager.gameObject))
            {
                for (int i = 0; i < paraPadArrows.Length; i++)
                {
                    scoreManager.areActive[i] = false;
                    foreach (int bone in toCheck)
                    {
                        scoreManager.areActive[i] = paraPadArrows[i].checkInside(player.GetBonePosition((HumanBodyBones)bone), player.GetBoneRotation((HumanBodyBones)bone), rotationOffset, handOffset, colliderSize);
                        if (useHaptics)//using haptics option->vibrate if entering
                            if ((!wereActive[i]) && scoreManager.areActive[i])
                                player.PlayHapticEventInHand(bone == (int)HumanBodyBones.LeftHand ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right, 1f / 60f, 1, 1);
                        if (scoreManager.areActive[i])
                            break;
                    }
                }
            }
        }

        //Unchanged from DancePadManager
        public void Update()
        {
            //show the changes
            for (int i = 0; i < areActive.Length; i++)
            {
                paraPadArrows[i].updateVisuals(areActive[i], y_base);
            }
            //prepare the active arrays for next frame
            for (int i = 0; i < areActive.Length; i++)
            {
                wereActive[i] = areActive[i];
                areActive[i] = false;
            }

            //update score
            if (scoreManager == null)
                return;
            if (Networking.LocalPlayer.IsOwner(scoreManager.gameObject))
            {
                if (scoreManager.stepfilesManager.hasStarted && scoreManager.stepfilesManager.audioSource.isPlaying)
                {
                    scoreManager.processScore(scoreManager.stepfilesManager.audioSource.time);
                }
            }
        }
    }
}
