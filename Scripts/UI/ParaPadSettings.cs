
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// UI letting the user fine tuning the settings of the used colliders of the parapads
    /// </summary>
    public class ParaPadSettings : UdonSharpBehaviour //Mostly the same as DancePadSettings but with less things
    {
        //All the elements below needs to be passed in scene
        public ParaPadManager[] paraPads;
        public Slider sliderPadSize;
        public Text padSizeInfo;
        public Slider sliderColliderSize;
        public Text colliderSizeInfo;
        public Slider sliderrotationOffsetX, sliderrotationOffsetY, sliderrotationOffsetZ, handOffsetSlider;
        public Text rotationOffsetXInfo, rotationOffsetYInfo, rotationOffsetZInfo, handOffsetInfo;
        public Toggle toggleHaptics;

        float colliderSize = 0.1f;
        Vector3 rotationOffset = new Vector3(270,0,0);
        float handOffset = -0.02f;

        bool isReady = false;
        void Start()
        {
            sliderPadSize.value = paraPads[0].gameObject.transform.localScale.x;
            sliderColliderSize.value = colliderSize;
            sliderrotationOffsetX.value = rotationOffset.x; sliderrotationOffsetY.value = rotationOffset.y; sliderrotationOffsetZ.value = rotationOffset.z;
            handOffsetSlider.value = handOffset;
            isReady = true;
            colliderUpdate();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// AutoCalibration based on the avatar's height and ratios found on the net, not 100% accurate but should be accurate enough for most people
        /// </summary>
        public void autoCalibrate()
        {
            var height = Mathf.Abs(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head).y - Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftFoot).y)
                + Mathf.Abs(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head).y - Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftUpperArm).y);//to compensate center of head + ankle differences for proportions
            sliderPadSize.value = (height / 1.60f) * 0.85f;
            var handSize = Vector3.Distance(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftLowerArm), Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftHand)) / 1.618f;
            handOffsetSlider.value = handSize/2f;
            sliderColliderSize.value = handSize/2f;
            Vector3 rotationOffset_tmp = new Vector3(270, 0, 0);
            sliderrotationOffsetX.value = rotationOffset_tmp.x; sliderrotationOffsetY.value = rotationOffset_tmp.y; sliderrotationOffsetZ.value = rotationOffset_tmp.z;
        }

        /// <summary>
        /// Called by onvaluechange of the sliderPad
        /// </summary>
        public void sliderPadSizeUpdate()
        {
            padSizeInfo.text = "ParaPad size: " + sliderPadSize.value;
            foreach (var paraPad in paraPads)
            {
                paraPad.gameObject.transform.localScale = new Vector3(sliderPadSize.value, paraPad.gameObject.transform.localScale.y, sliderPadSize.value);
            }
        }

        /// <summary>
        /// Called by the onvaluechange of whatever
        /// </summary>
        public void colliderUpdate()
        {
            if (!isReady)
                return;
            colliderSizeInfo.text = "ColliderSize : " + sliderColliderSize.value;
            rotationOffsetXInfo.text = "RotationOffset X: " + sliderrotationOffsetX.value; rotationOffsetYInfo.text = "RotationOffset Y: " + sliderrotationOffsetY.value; rotationOffsetZInfo.text = "RotationOffset Z: " + sliderrotationOffsetZ.value;
            colliderSize = sliderColliderSize.value;
            rotationOffset.x = sliderrotationOffsetX.value; rotationOffset.y = sliderrotationOffsetY.value; rotationOffset.z = sliderrotationOffsetZ.value;
            handOffset = handOffsetSlider.value; handOffsetInfo.text = "HandOffset : " + handOffsetSlider.value;
            foreach (var paraPad in paraPads)
            {
                paraPad.rotationOffset = rotationOffset;
                paraPad.colliderSize = colliderSize;
                paraPad.handOffset = handOffset;

                //putting haptics here is a bit dirty but not significant enough for cleaner separate function
                paraPad.useHaptics = toggleHaptics.isOn;
            }
        }

        /// <summary>
        /// Sticking the collider each frame this gamobject is visible to one of the player's hand
        /// </summary>
        public void Update()
        {
            stickCollider(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftHand), Networking.LocalPlayer.GetBoneRotation(HumanBodyBones.LeftHand), rotationOffset, handOffset, colliderSize);
        }
        public GameObject colliderVisualizer;
        public void stickCollider(Vector3 handPos, Quaternion handAngle, Vector3 angleOffset, float handOffset, float handSize)
        {
            Vector3 centerPos = handPos + ((handAngle * Quaternion.Euler(angleOffset)) * Vector3.forward * handOffset);
            colliderVisualizer.transform.position = centerPos;
            colliderVisualizer.transform.parent = null;
            colliderVisualizer.transform.localScale = new Vector3(handSize*2f, handSize * 2f, handSize * 2f);
            colliderVisualizer.transform.parent = this.transform;
        }
    }
}
