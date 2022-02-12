
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// A simple UI Slider that changes the visualization of the travel speed of notes by the StepfileVisualizer, essentially changes the barToMeter value with some maths
    /// </summary>
    public class SliderSpeedmod : UdonSharpBehaviour
    {
        public StepfileVisualizer stepfileVisualizer;//StepfileVisualizer that will be affected by the speed change

        public UnityEngine.UI.Slider slider;//Slider used
        public UnityEngine.UI.Text text;//Text of the slider

        const float BAR_TO_METER_X1 = 2f;//what is the "bar to meter" ratio than will be considered as a speedmod of 1
        void Start()
        {
            slider.value = (int)((stepfileVisualizer.barToMeter / BAR_TO_METER_X1) * 10);
        }

        public void valueChanged()
        {
            text.text = "x" + (slider.value / 10f);
            stepfileVisualizer.barToMeter = slider.value * BAR_TO_METER_X1 / 10f;
        }
    }
}