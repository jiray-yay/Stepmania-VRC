
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// One of the interactable arrow of a parapad
    /// </summary>
    public class ParaPadArrow : UdonSharpBehaviour //I wish U# could handle inheritance ;_; , this class is copypasted then edited from DancePadArrow
    {
        public Material activatedMat = null;//material/color that will represent a pad press, should be passed in scene
        [HideInInspector]
        public Material baseMat = null;//Default material/color, no need to pass in scene
        public Material activatedLineMat = null;//Material of activated arrow for the ray visualization, should be passed in scene
        public Material baseLineMat = null;//Material of inactivated arrow for the ray visualization, should be passed in scene
        [HideInInspector]
        public MeshRenderer meshRenderer = null;//Renderer to modify with the above mats
        [HideInInspector]
        public LineRenderer lineRenderer = null;//LineRenderer to modify with the above mats
        void Start()
        {
            if (meshRenderer == null)
                meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (baseMat == null)
                baseMat = meshRenderer.material;
            if (lineRenderer == null)
                lineRenderer = gameObject.GetComponent<LineRenderer>();
        }

        
        /// <summary>
        /// Update the visuals depending on if the arrow is being "pressed" or not
        /// </summary>
        /// <param name="asActive">is arrow being pressed</param>
        /// <param name="y_base">full height of the arrow's laser when not activated/pressed</param>
        public void updateVisuals(bool asActive, float y_base)
        {
            //TODO : lineRenderer shenaningans
            meshRenderer.material = asActive ? activatedMat : baseMat;
            lineRenderer.material = asActive ? activatedLineMat : baseLineMat;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, new Vector3(transform.position.x, asActive?activated_y:y_base,transform.position.z));
        }

        float activated_y = 0;//height at which the arrow is being activated by a user's hand
        /// <summary>
        /// Checks if a pseudo-collider from the hand of a user is inside the arrow of the parapad
        /// </summary>
        /// <param name="handPos">Position of the hand/wrist bone</param>
        /// <param name="handAngle">Angle of hand bone</param>
        /// <param name="angleOffset">offset to add to hand bone angle to be in line with the fingers</param>
        /// <param name="handOffset">how much the center of the hand is from the wrist in the inline direction</param>
        /// <param name="handSize">radius of the pseudo sphere collider used</param>
        /// <returns>is the psuedo collider activating the para pad arrow?</returns>
        public bool checkInside(Vector3 handPos, Quaternion handAngle, Vector3 angleOffset, float handOffset, float handSize)
        {
            //go inline by offset from handPos
            //Vector3 centerPos = handPos + (Quaternion.Euler(handAngle.eulerAngles+angleOffset).normalized * Vector3.forward * handOffset);
            Vector3 centerPos = handPos + ((handAngle * Quaternion.Euler(angleOffset)) * Vector3.forward * handOffset);
            //do not consider y axis in distance check, use ^2 dist for faster computation
            bool tmp = ((centerPos.x - transform.position.x) * (centerPos.x - transform.position.x)) + ((centerPos.z - transform.position.z) * (centerPos.z - transform.position.z)) < (handSize * handSize);
            if (tmp)
                activated_y = handPos.y;
            return tmp;
        }
    }
}