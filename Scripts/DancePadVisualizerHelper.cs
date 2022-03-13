
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Visualize the feet positions of the local player on a pad visualizer
    /// </summary>
    public class DancePadVisualizerHelper : UdonSharpBehaviour
    {
        public DandePadManager dandePadManager;//dancePad to visualize, using the manager to get the parameters of the foot
        public BoxCollider dancePadManagerCollider; //the dancePad to visualize, we use it's boxCollider to get the position of the bounds
        public GameObject leftFootVisualizer, rightFootVisualizer; //object we will show to represent each foot
        void Update()
        {
            Vector3 posLeft = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftFoot) + (((Networking.LocalPlayer.GetBoneRotation(HumanBodyBones.LeftFoot) * Quaternion.Euler(dandePadManager.rotationOffset)).normalized * Vector3.forward) * (dandePadManager.colliderSize.z * 0.5f + dandePadManager.ankleOffset));
            Vector3 posRight = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightFoot) + (((Networking.LocalPlayer.GetBoneRotation(HumanBodyBones.RightFoot) * Quaternion.Euler(dandePadManager.rotationOffset)).normalized * Vector3.forward) * (dandePadManager.colliderSize.z * 0.5f + dandePadManager.ankleOffset));
            Vector3 boundsSize = dancePadManagerCollider.bounds.max - dancePadManagerCollider.bounds.min;
            if (dancePadManagerCollider.bounds.Contains(posLeft))
            {
                leftFootVisualizer.gameObject.SetActive(true);
                Vector3 leftSize = posLeft - dancePadManagerCollider.bounds.min;
                leftFootVisualizer.transform.localPosition = new Vector3((leftSize.x / boundsSize.x - 0.5f) * transform.localScale.x, 0, (leftSize.z / boundsSize.z - 0.5f) * transform.localScale.z);
            }
            else
                leftFootVisualizer.gameObject.SetActive(false);
            if (dancePadManagerCollider.bounds.Contains(posRight))
            {
                rightFootVisualizer.gameObject.SetActive(true);
                Vector3 rightSize = posRight - dancePadManagerCollider.bounds.min;
                rightFootVisualizer.transform.localPosition = new Vector3((rightSize.x / boundsSize.x - 0.5f) * transform.localScale.x, 0, (rightSize.z / boundsSize.z - 0.5f) * transform.localScale.z);
            }
            else
                rightFootVisualizer.gameObject.SetActive(false);
        }
    }
}