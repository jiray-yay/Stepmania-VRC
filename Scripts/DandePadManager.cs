
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Yes it has a typo but whatever. DancepadManager represents an entire interactable dancepad
    /// </summary>
    public class DandePadManager : UdonSharpBehaviour
    {
        public DancePadArrow[] dancePadArrows;//the arrows on the ground used by this dancepad, should passed in scene
        public PressVisualizer[] pressVisualizers;//press visualizers reacting to the pad presses, should be passed in scene
        [HideInInspector]
        public bool[] areActive;//pad arrows activated this frame
        [HideInInspector]
        public bool[] wereActive;//pad arrows activated previous frame
        int[] toCheck = { (int)HumanBodyBones.LeftFoot, (int)HumanBodyBones.RightFoot, (int)HumanBodyBones.LeftHand, (int)HumanBodyBones.RightHand };//Pseudo const table

        [HideInInspector]
        public ScoreManager scoreManager;//the score manager that will use the inputs from this dancepad, hidden in inspector because should be passed through stepfilesmanager

        [HideInInspector]
        public Vector3 colliderSize = new Vector3(0.40f, 0.20f, 0.40f), rotationOffset = Vector3.zero;//Settings of the user regarding the collider
        [HideInInspector]
        public float ankleOffset = -0.2f;//Setting of the user, distance between collider of player's feet and the ankle in the direction of the toes
        [HideInInspector]
        public bool useToeBone = false;//Setting of the user, changes the method used to get the "inline" vector from ankle to toes

        public BoxCollider colliderPrefab;//For collision detection, we use for each arrow a set of colliders we stick to the player's bones, we can't just use one per arrow because of some Unity shenaningans
        
        /// <summary>
        /// Sets the colliders for each arrow + generates the internal arrays depending on how many arrow/buttons are available
        /// </summary>
        void Start()
        {
            areActive = new bool[dancePadArrows.Length];
            wereActive = new bool[dancePadArrows.Length];

            colliderPrefab.transform.parent = null;
            for(int i = 0; i < dancePadArrows.Length; i++)
            {
                dancePadArrows[i].sizeColliderOthers = 16;
                dancePadArrows[i].boxColliderOthers = new BoxCollider[dancePadArrows[i].sizeColliderOthers];
                for(int j = 0; j < dancePadArrows[i].sizeColliderOthers; j++)
                {
                    dancePadArrows[i].boxColliderOthers[j] = VRCInstantiate(colliderPrefab.gameObject).GetComponent<BoxCollider>();
                    dancePadArrows[i].boxColliderOthers[j].transform.parent = null;
                }            
            }
        }

        /// <summary>
        /// Score manager has to wait to be linked with a dancepad before having active/inactive presses
        /// </summary>
        public void initScoreManagerArrays()
        {
            scoreManager.areActive = new bool[dancePadArrows.Length];
            scoreManager.wereActive = new bool[dancePadArrows.Length];
        }

        /// <summary>
        /// Arrow visualization is made by everyone, but should be done only when someone is near the dancepad. The DancepadManager itself has a trigger collider to detect that
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
            for (int i = 0; i < dancePadArrows.Length; i++)
            {
                if (areActive[i])
                    continue;
                foreach (int bone in toCheck)
                {

                    if (useToeBone && (bone == (int)HumanBodyBones.LeftFoot))
                        areActive[i] = dancePadArrows[i].checkInside_ToeBone(player.GetBonePosition(HumanBodyBones.LeftFoot), player.GetBonePosition(HumanBodyBones.LeftToes), colliderSize, ankleOffset);
                    else if (useToeBone && (bone == (int)HumanBodyBones.RightFoot))
                        areActive[i] = dancePadArrows[i].checkInside_ToeBone(player.GetBonePosition(HumanBodyBones.RightFoot), player.GetBonePosition(HumanBodyBones.RightToes), colliderSize, ankleOffset);
                    else
                        areActive[i] = dancePadArrows[i].checkInside(player.GetBonePosition((HumanBodyBones)bone), player.GetBoneRotation((HumanBodyBones)bone) * Quaternion.Euler(rotationOffset), colliderSize, ankleOffset);
                    
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
                for (int i = 0; i < dancePadArrows.Length; i++)
                {
                    scoreManager.areActive[i] = false;
                    foreach (int bone in toCheck)
                    {
                        if (useToeBone && (bone == (int)HumanBodyBones.LeftFoot))
                            scoreManager.areActive[i] = dancePadArrows[i].checkInside_ToeBone(player.GetBonePosition(HumanBodyBones.LeftFoot), player.GetBonePosition(HumanBodyBones.LeftToes), colliderSize, ankleOffset);
                        else if (useToeBone && (bone == (int)HumanBodyBones.RightFoot))
                            scoreManager.areActive[i] = dancePadArrows[i].checkInside_ToeBone(player.GetBonePosition(HumanBodyBones.RightFoot), player.GetBonePosition(HumanBodyBones.RightToes), colliderSize, ankleOffset);
                        else
                            scoreManager.areActive[i] = dancePadArrows[i].checkInside(player.GetBonePosition((HumanBodyBones)bone), player.GetBoneRotation((HumanBodyBones)bone) * Quaternion.Euler(rotationOffset), colliderSize, ankleOffset);
                        if (scoreManager.areActive[i])
                            break;
                    }
                }
            }
        }

        public void Update()
        {
            //show the changes
            for (int i = 0; i < areActive.Length; i++)
            {
                if (wereActive[i] != areActive[i])
                    dancePadArrows[i].updateVisuals(areActive[i]);
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
                    scoreManager.processScore(scoreManager.stepfilesManager.audioSource.time + scoreManager.stepfilesManager.audioOffset);
            }
        }
    }
}