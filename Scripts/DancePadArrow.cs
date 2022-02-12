
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// One of the interactable arrow of a dancepad
    /// </summary>
    public class DancePadArrow : UdonSharpBehaviour
    {
        public Material activatedMat = null;//material/color that will represent a pad press, should be passed in scene
        [HideInInspector]
        public Material baseMat = null;//Default material/color, no need to pass in scene
        [HideInInspector]
        public MeshRenderer meshRenderer = null;//Renderer to modify with the above mats

        public BoxCollider boxCollider = null;//Collider of the arrow itself

        [HideInInspector]
        public BoxCollider[] boxColliderOthers = null;//Generated collider that are sticked on the bones of a player
        [HideInInspector]
        public int latestBoxColliderOther, sizeColliderOthers;//manages the array above

        BoxCollider boxColliderOther = null;//prefab of the collider being sticked on the bones of a player
        void Start()
        {
            if (boxCollider == null)
                boxCollider = gameObject.GetComponent<BoxCollider>();
            if (meshRenderer == null)
                meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (baseMat == null)
                baseMat = meshRenderer.material;
        }

        /// <summary>
        /// Checks if a pseudo-collider inside the arrow of the dancepad
        /// </summary>
        /// <param name="basePos">base position of the collider without the offset</param>
        /// <param name="angle">angle the collider is looking at</param>
        /// <param name="size">size of the pseudo collider</param>
        /// <param name="offsetCenter">how much basePos should moved inLine of the angle to be the center</param>
        /// <returns>Is a pseudo-collider inside the arrow of the dancepad?</returns>
        public bool checkInside(Vector3 basePos, Quaternion angle, Vector3 size, float offsetCenter)
        {
            latestBoxColliderOther = (latestBoxColliderOther + 1) % sizeColliderOthers;
            boxColliderOther = boxColliderOthers[latestBoxColliderOther];

            Vector3 inLine = angle.normalized * Vector3.forward;
            Vector3 centerPos = basePos + (inLine * (size.z * 0.5f + offsetCenter));
            boxColliderOther.transform.position = centerPos;
            boxColliderOther.transform.rotation = angle;
            boxColliderOther.transform.localScale = size;
            return boxCollider.bounds.Intersects(boxColliderOther.bounds);
        }

        /// <summary>
        /// Checks if a pseudo-collider inside the arrow of the dancepad. Considers 2 positions instead of a position and an angle
        /// </summary>
        /// <param name="ankleBone">base position of the collider without the offset</param>
        /// <param name="toeBone">position the collider is looking at from the base position</param>
        /// <param name="size">size of the pseudo collider</param>
        /// <param name="offsetCenter">how much the base position should move towards the position it should look at</param>
        /// <returns>Is a pseudo-collider inside the arrow of the dancepad?</returns>
        public bool checkInside_ToeBone(Vector3 ankleBone, Vector3 toeBone, Vector3 size, float offsetCenter)
        {
            latestBoxColliderOther = (latestBoxColliderOther + 1) % sizeColliderOthers;
            boxColliderOther = boxColliderOthers[latestBoxColliderOther];
            Vector3 inLine = (toeBone - ankleBone).normalized;
            Vector3 centerPos = ankleBone + (inLine * (size.z * 0.5f + offsetCenter));

            boxColliderOther.transform.position = ankleBone;
            boxColliderOther.transform.LookAt(toeBone);
            boxColliderOther.transform.position = centerPos;
            boxColliderOther.transform.localScale = size;
            return boxCollider.bounds.Intersects(boxColliderOther.bounds);
        }

        /// <summary>
        /// changes the material depending on if this arrow is activated or not
        /// </summary>
        /// <param name="asActive">is the arrow activated?</param>
        public void updateVisuals(bool asActive)
        {
            meshRenderer.material = asActive ? activatedMat : baseMat;
        }
    }
}