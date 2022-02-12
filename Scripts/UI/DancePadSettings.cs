
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// UI letting the user fine tuning the settings of the used colliders of the dancepads
    /// </summary>
    public class DancePadSettings : UdonSharpBehaviour
    {
        //All the elements below needs to be passed in scene
        public DandePadManager[] dancePads;
        public Slider sliderPadSize;
        public Text padSizeInfo;
        public Slider sliderColliderSizeX, sliderColliderSizeY, sliderColliderSizeZ;
        public Text colliderSizeXInfo, colliderSizeYInfo, colliderSizeZInfo;
        public Slider sliderrotationOffsetX, sliderrotationOffsetY, sliderrotationOffsetZ, ankleOffsetSlider;
        public Text rotationOffsetXInfo, rotationOffsetYInfo, rotationOffsetZInfo, ankleOffsetInfo;
        public Toggle toggleUseToeBones;

        Vector3 colliderSize = new Vector3(0.08f, 0.04f, 0.20f);
        Vector3 rotationOffset = new Vector3(270, 0, 0);//Vector3.zero;
        float ankleOffset = -0.02f;

        bool isReady = false;
        void Start()
        {
            sliderPadSize.value = dancePads[0].gameObject.transform.localScale.x;
            sliderColliderSizeX.value = colliderSize.x; sliderColliderSizeY.value = colliderSize.y; sliderColliderSizeZ.value = colliderSize.z;
            sliderrotationOffsetX.value = rotationOffset.x; sliderrotationOffsetY.value = rotationOffset.y; sliderrotationOffsetZ.value = rotationOffset.z;
            ankleOffsetSlider.value = ankleOffset;
            toggleUseToeBones.isOn = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftToes) != Vector3.zero;
            isReady = true;
            colliderUpdate();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// AutoCalibration based on the avatar's height and ratios found on the net, not 100% accurate but should be accurate enough for most people
        /// </summary>
        public void autoCalibrate()
        {
            //pad size for normal height (170cm?) = 85cm (same as ltek/arcade)
            var height = Mathf.Abs(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head).y - Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftFoot).y)
                + Mathf.Abs(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head).y - Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftUpperArm).y);//to compensate center of head + ankle differences for proportions
            sliderPadSize.value = (height / 1.60f) * 0.85f;
            var footLength = height / 6f;//ratio foot/height should be 1/6
            footLength *= 1.06f;//give at little extra
            Vector3 colliderSize_tmp = new Vector3(footLength*0.32f, footLength * 0.4f, footLength);
            ankleOffsetSlider.value = -footLength * 0.18f;
            sliderColliderSizeX.value = colliderSize_tmp.x; sliderColliderSizeY.value = colliderSize_tmp.y; sliderColliderSizeZ.value = colliderSize_tmp.z;
            Vector3 rotationOffset_tmp = new Vector3(270, 0, 0);
            sliderrotationOffsetX.value = rotationOffset_tmp.x; sliderrotationOffsetY.value = rotationOffset_tmp.y; sliderrotationOffsetZ.value = rotationOffset_tmp.z;
            toggleUseToeBones.isOn = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftToes) != Vector3.zero;
        }

        /// <summary>
        /// Called by onvaluechange of the sliderPad
        /// </summary>
        public void sliderPadSizeUpdate()
        {
            padSizeInfo.text = "Dancepad size: " + sliderPadSize.value;
            foreach (var dancePad in dancePads)
            {
                dancePad.gameObject.transform.localScale = new Vector3(sliderPadSize.value, dancePad.gameObject.transform.localScale.y, sliderPadSize.value);
            }
        }

        /// <summary>
        /// Called by the onvaluechange of the toebone
        /// </summary>
        public void changeUseToeBone()
        {
            foreach (var dancePad in dancePads)
            {
                dancePad.useToeBone = toggleUseToeBones.isOn;
            }
        }

        /// <summary>
        /// Called by the onvaluechange of whatever
        /// </summary>
        public void colliderUpdate()
        {
            if (!isReady)
                return;
            colliderSizeXInfo.text = "ColliderSize X: " + sliderColliderSizeX.value; colliderSizeYInfo.text = "ColliderSize Y: " + sliderColliderSizeY.value; colliderSizeZInfo.text = "ColliderSize Z: " + sliderColliderSizeZ.value;
            rotationOffsetXInfo.text = "RotationOffset X: " + sliderrotationOffsetX.value; rotationOffsetYInfo.text = "RotationOffset Y: " + sliderrotationOffsetY.value; rotationOffsetZInfo.text = "RotationOffset Z: " + sliderrotationOffsetZ.value;
            colliderSize.x = sliderColliderSizeX.value; colliderSize.y = sliderColliderSizeY.value; colliderSize.z = sliderColliderSizeZ.value;
            rotationOffset.x = sliderrotationOffsetX.value; rotationOffset.y = sliderrotationOffsetY.value; rotationOffset.z = sliderrotationOffsetZ.value;
            ankleOffset = ankleOffsetSlider.value; ankleOffsetInfo.text = "AnkleOffset : " + ankleOffsetSlider.value;
            foreach (var dancePad in dancePads)
            {
                dancePad.rotationOffset = rotationOffset;
                dancePad.colliderSize = colliderSize;
                dancePad.ankleOffset = ankleOffset;
            }
        }

        /// <summary>
        /// Sticking the collider each frame this gamobject is visible to one of the player's foot
        /// </summary>
        public void Update()
        {
            Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftFoot);
            if (!toggleUseToeBones.isOn)
                stickCollider(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftFoot), Networking.LocalPlayer.GetBoneRotation(HumanBodyBones.LeftFoot)*Quaternion.Euler(rotationOffset), colliderSize, ankleOffset);
            else
                stickColliderToeBone(Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftFoot), Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftToes), colliderSize, ankleOffset);
        }
        public GameObject colliderVisualizer;
        public BoxCollider boxColliderVisualizer;
        public void stickColliderToeBone(Vector3 ankleBone, Vector3 toeBone, Vector3 size, float offsetCenter)
        {
            Vector3 inLine = (toeBone - ankleBone).normalized;
            Vector3 centerPos = ankleBone + (inLine * (size.z * 0.5f + offsetCenter));

            boxColliderVisualizer.transform.parent = null;//unparenting the visualizer for no scale issues when rotating/resizing the cab prefab

            boxColliderVisualizer.transform.position = ankleBone;
            boxColliderVisualizer.transform.LookAt(toeBone);
            boxColliderVisualizer.transform.position = centerPos;
            boxColliderVisualizer.transform.localScale = size;

            boxColliderVisualizer.transform.parent = this.transform;//go back to your parent (for hide/unhide)

            colliderVisualizer.transform.parent = null;//unparenting the visualizer for no scale issues when rotating/resizing the cab prefab

            colliderVisualizer.transform.eulerAngles = Vector3.zero;//unrotate
            colliderVisualizer.transform.position = centerPos;
            colliderVisualizer.transform.localScale = boxColliderVisualizer.bounds.max - boxColliderVisualizer.bounds.min;

            colliderVisualizer.transform.parent = this.transform;//go back to your parent (for hide/unhide)
        }

        public void stickCollider(Vector3 basePos, Quaternion angle, Vector3 size, float offsetCenter)
        {
            Vector3 inLine = angle.normalized * Vector3.forward;
            Vector3 centerPos = basePos + (inLine * (size.z * 0.5f + offsetCenter));

            boxColliderVisualizer.transform.parent = null;//unparenting the visualizer for no scale issues when rotating/resizing the cab prefab

            boxColliderVisualizer.transform.position = basePos;
            boxColliderVisualizer.transform.rotation = angle;
            boxColliderVisualizer.transform.position = centerPos;
            boxColliderVisualizer.transform.localScale = size;

            boxColliderVisualizer.transform.parent = this.transform;//go back to your parent (for hide/unhide)

            colliderVisualizer.transform.parent = null;//unparenting the visualizer for no scale issues when rotating/resizing the cab prefab

            colliderVisualizer.transform.eulerAngles = Vector3.zero;//unrotate
            colliderVisualizer.transform.position = centerPos;
            colliderVisualizer.transform.localScale = boxColliderVisualizer.bounds.max - boxColliderVisualizer.bounds.min;

            colliderVisualizer.transform.parent = this.transform;//go back to your parent (for hide/unhide)
        }

    }
}