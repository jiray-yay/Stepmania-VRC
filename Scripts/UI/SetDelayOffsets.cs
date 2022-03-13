
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace StepmaniaVRC
{
    /// <summary>
    /// Changes the audio/draw delay of all StepfilesManager in scene by local user settings
    /// </summary>
    public class SetDelayOffsets : UdonSharpBehaviour
    {
        //have to be passed through scene
        public Slider sliderAudioOffset, sliderDrawOffset;
        public Text textAudioOffset, textDrawOffset;
        public StepfilesManager[] stepfilesManagers = null;

        public void SliderValueChange()
        {
            foreach(var s in stepfilesManagers)
            {
                s.audioOffset = sliderAudioOffset.value / 60f;
                s.drawOffset = sliderDrawOffset.value / 60f;
            }
            textAudioOffset.text = "AudioOffset: " + (int)sliderAudioOffset.value + "f / " + (int)(sliderAudioOffset.value * 1000f / 60f) + "ms";
            textDrawOffset.text = "DrawOffset: " + (int)sliderDrawOffset.value + "f / " + (int)(sliderDrawOffset.value * 1000f / 60f) + "ms";
        }

        public void ResetValueAudio()
        {
            sliderAudioOffset.value = 0;
        }

        public void ResetValueDraw()
        {
            sliderDrawOffset.value = 0;
        }
    }
}
