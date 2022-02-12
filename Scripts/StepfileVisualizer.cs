
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// StepfileVisualizer has as a main objective to show the gameplay elements of a chart, but doesn't manage the validation/scoring gameplay.
    /// It also places the "press visualizer" as we want those to be aligned with the moving notes.
    /// </summary>
    public class StepfileVisualizer : UdonSharpBehaviour
    {
        [HideInInspector]
        public StepfilesManager stepfilesManager;//Hidden because every "scene-reference" should be towards StepfilesManager in order to avoid having to much to handle
        [HideInInspector]
        public ScoreManager scoreManager;//Hidden for same reason, score manager influences what notes to hide (already validated notes should not be shown)

        public ArrowVisualizer prefabArrow;//Visualization object of a "note/arrow" that will be duplicated
        [HideInInspector]
        public ArrowVisualizer[] arrowVisualizers;//The generated visualization objects, will be used in a pooling fashion

        public ArrowVisualizer prefabHold;//Visualization object of a " hold note/arrow" that will be duplicated
        [HideInInspector]
        public ArrowVisualizer[] holdVisualizers;//The generated visualization objects, will be used in a pooling fashion

        [HideInInspector]
        public int nbVisualizersShown = 0, nbVisualizersToShow = 0, nbHoldVisualizersShown = 0, nbHoldVisualizersToShow = 0;//variables to manage the ArrowVisualizers arrays

        const int MIN_VISUALIZER = 200;
        const int MIN_HOLD = 50;

        public double[] relativeDisplay = { -0.5, 0.5 };//In bars, where in the partition relative to the current time position should we consider looking for notes to visualize? Can be customized in scene
        public float[] displayYBounds = { -2.37f, 0.25f };//Relative to the StepfileVisualizer's position, where do we consider displaying our notes (outside positions would not be shown) Can be customized in scene

        public Material mat4th, mat8th, mat16th, matHold, matHoldTrail, matOther;//We do a pseudo ddr-note color scheme, should be passed in scene

        public PressVisualizer[] pressVisualizers;//Better to let the stepvisualizer place the press visualizer where the notes will go through, should be passed in scene

        /// <summary>
        /// Generates a relatively big number of note visualizers to cover our future needs then places the press visualizers.
        /// Generating everything prior and never deleting the ArrowVisualizer should avoid framedrops due to GC calls for gameobject creation
        /// </summary>
        void Start()
        {
            nbVisualizersShown = 0;
            arrowVisualizers = new ArrowVisualizer[MIN_VISUALIZER];
            for (int i = 0; i < MIN_VISUALIZER; i++)
            {
                arrowVisualizers[i] = VRCInstantiate(prefabArrow.gameObject).GetComponent<ArrowVisualizer>();
                arrowVisualizers[i].transform.parent = this.transform;
            }
            holdVisualizers = new ArrowVisualizer[MIN_HOLD];
            for (int i = 0; i < MIN_HOLD; i++)
            {
                holdVisualizers[i] = VRCInstantiate(prefabHold.gameObject).GetComponent<ArrowVisualizer>();
                holdVisualizers[i].transform.parent = this.transform;
                holdVisualizers[i].transform.rotation = this.transform.rotation;
            }
            placePressVisualizer();
        }

        /// <summary>
        /// Places the press visualizers so that they align properly with the notes this stepfileVisualizer will generate
        /// </summary>
        public void placePressVisualizer()
        {
            for (int i = 0; i < pressVisualizers.Length; i++)
            {
                pressVisualizers[i].transform.parent = this.transform;
                pressVisualizers[i].transform.localPosition = new Vector3(visualize_distance * (i - (0.5f * pressVisualizers.Length)), 0);
            }
        }

        /// <summary>
        /// Getting a note visualizer that was not already used this frame, in the case we don't enough, generates new visualizers
        /// </summary>
        /// <returns>a usable/currently free visualizer</returns>
        ArrowVisualizer getVisualizer()
        {
            if (nbVisualizersToShow >= arrowVisualizers.Length)
            {
                var tmp = arrowVisualizers;
                arrowVisualizers = new ArrowVisualizer[arrowVisualizers.Length + MIN_VISUALIZER];
                int i;
                for (i = 0; i < tmp.Length; i++)
                    arrowVisualizers[i] = tmp[i];
                for (i = i; i < arrowVisualizers.Length; i++)
                {
                    arrowVisualizers[i] = VRCInstantiate(prefabArrow.gameObject).GetComponent<ArrowVisualizer>();
                    arrowVisualizers[i].transform.parent = this.transform;
                }
            }
            nbVisualizersToShow++;
            arrowVisualizers[nbVisualizersToShow - 1].gameObject.SetActive(true);
            return arrowVisualizers[nbVisualizersToShow - 1];
        }

        /// <summary>
        /// Getting a hold note visualizer that was not already used this frame, in the case we don't enough, generates new visualizers
        /// </summary>
        /// <returns>a usable/currently free visualizer</returns>
        ArrowVisualizer getHoldVisualizer()
        {
            if (nbHoldVisualizersToShow >= holdVisualizers.Length)
            {
                var tmp = holdVisualizers;
                holdVisualizers = new ArrowVisualizer[holdVisualizers.Length + MIN_HOLD];
                int i;
                for (i = 0; i < tmp.Length; i++)
                    holdVisualizers[i] = tmp[i];
                for (i = i; i < holdVisualizers.Length; i++)
                {
                    holdVisualizers[i] = VRCInstantiate(prefabHold.gameObject).GetComponent<ArrowVisualizer>();
                    holdVisualizers[i].transform.parent = this.transform;
                    holdVisualizers[i].transform.rotation = this.transform.rotation;
                }
            }
            nbHoldVisualizersToShow++;
            holdVisualizers[nbHoldVisualizersToShow - 1].gameObject.SetActive(true);
            return holdVisualizers[nbHoldVisualizersToShow - 1];
        }

        [HideInInspector]
        public double[] previous_hold = new double[8];//holds are the connection between 2 notes in the partition: the hold start and the hold end.... 
        [HideInInspector]
        public bool[] did_hold = new bool[8];//...As a result we need to keep track of the hold notes start/end we have seen or haven't seen while rendering this frame

        public float barToMeter = 4;//how many meters corresponds to a bar, ultimately defines the "notes travel speed" or "speedmod", higher value feels faster
        public float visualize_distance = 0.25f;//the space in horizontal axis between each note column (arrow direction)
        public float negativeForDownToUp = -1;//should be either +1 or -1, -1 makes notes travel from the bottom of the game screen to its top, +1 does the opposite

        /// <summary>
        /// Visualize should be called each frame when a partition/song is played. The visualization depends on the partition, the time in the partition and the note validation from scoreManager
        /// </summary>
        /// <param name="timeCount">time position of the song considered</param>
        public void Visualize(double timeCount)
        {
            var smReader = stepfilesManager.smReaders[stepfilesManager.music];
            var partition = scoreManager.partition;
            if (smReader.partitionsInfos_NbNotes[partition] == 0)
                return;

            var gameMode = smReader.partitionInfos_GameType[partition];//how many columns do we have per line?

            var _partition = smReader.partitions[partition];//copying the partition instead of accessing it from the reference speeds up the access for some reason, I suspect weird UDON cache shenaningans
            char[][] _partitioni;//current bar
            char[] _partitionj;//current line from the bar

            clearHolds();
            bool isScoreOwner = Networking.IsOwner(scoreManager.gameObject);//if we are the owner of the scoreManager then the note shown will also depend on it, otherwise we don't care because lack of shared variable (and lag would get in the way)
            nbVisualizersToShow = 0;//since we are rendering a new frame, we consider that we have no visualizers to show (and thus, that every visualizer can now be used)
            nbHoldVisualizersToShow = 0;
            double measurePosition = smReader.TimeToBar(timeCount);

            int start = (int)Math.Floor(measurePosition + relativeDisplay[0]);//rounding with floor/ceiling because to access half bars we need to access to the whole bar first (indexes for arrays)
            int end = 1 + (int)Math.Ceiling(measurePosition + relativeDisplay[1]);

            for (int i = start; i <= end; i++)
            {
                if (i < 0)
                    continue;
                if (i >= _partition.Length)
                    break;
                double bar_divider = _partition[i].Length;
                _partitioni = _partition[i];
                for (int j = 0; j < _partitioni.Length; j++)
                {
                    double tmp = i + (j / bar_divider);
                    _partitionj = _partitioni[j];
                    for (int k = 0; k < gameMode; k++)
                    {
                        if (tmp <= scoreManager.latestActivated[k])
                        {//note has been activated
                            continue;
                        }
                        if (_partitionj[k] == '0')//no note->leave
                            continue;
                        if (_partitionj[k] == '2')//new hold start detected
                            previous_hold[k] = tmp;
                        if (_partitionj[k] == '3')//new end hold note detected
                        {
                            did_hold[k] = true;
                            //do hold notes stuff
                            if (isScoreOwner && previous_hold[k] < 0 && !scoreManager.isHolding[k])
                                continue;//hold was failed 
                            double y0 = previous_hold[k] < 0 ? 0 : previous_hold[k] - measurePosition;
                            double y1 = tmp - measurePosition;
                            previous_hold[k] = -1;
                            y0 *= barToMeter * negativeForDownToUp;
                            y1 *= barToMeter * negativeForDownToUp;
                            if ((y0 < displayYBounds[0] && y1 < displayYBounds[0]) ||
                                (y0 > displayYBounds[1] && y1 > displayYBounds[1]))
                                continue;
                            y0 = y0 < displayYBounds[0] ? displayYBounds[0] : y0;
                            y0 = y0 > displayYBounds[1] ? displayYBounds[1] : y0;
                            y1 = y1 < displayYBounds[0] ? displayYBounds[0] : y1;
                            y1 = y1 > displayYBounds[1] ? displayYBounds[1] : y1;
                            Vector3 relativePos1 = new Vector3(visualize_distance * (k - (0.5f * gameMode)), (float)((y1 + y0) / 2.0));
                            var holdVisualizer = getHoldVisualizer();
                            holdVisualizer.transform.localPosition = relativePos1;
                            holdVisualizer.transform.localScale = new Vector3(holdVisualizer.transform.localScale.x, (float)(y0 > y1 ? y0 - y1 : y1 - y0), holdVisualizer.transform.localScale.z);
                        }
                        double y = tmp - measurePosition;
                        y *= (barToMeter * negativeForDownToUp);
                        if (y < displayYBounds[0] || y > displayYBounds[1])
                            continue;
                        Vector3 relativePos = new Vector3(visualize_distance * (k - (0.5f * gameMode)), (float)y);
                        var arrowVisualizer = getVisualizer();
                        arrowVisualizer.transform.localEulerAngles = getEulerArrow(k, gameMode);
                        arrowVisualizer.transform.localPosition = relativePos;

                        if (_partitionj[k] == '1')//Normal note->simulate ddr-note noteskin (color of array indicates if note is a 4th, 8th, 16th, other-th)
                        {
                            if (_partitioni.Length % 4 != 0)
                            {
                                arrowVisualizer.changeMat(matOther);
                                continue;
                            }
                            int baroffset = _partitioni.Length / 4;
                            if (j % baroffset == 0)
                            {
                                arrowVisualizer.changeMat(mat4th);
                                continue;
                            }
                            baroffset = _partitioni.Length / 8;
                            if (j % baroffset == 0)
                            {
                                arrowVisualizer.changeMat(mat8th);
                                continue;
                            }
                            baroffset = _partitioni.Length / 16;
                            if (j % baroffset == 0)
                            {
                                arrowVisualizer.changeMat(mat16th);
                                continue;
                            }
                            arrowVisualizer.changeMat(matOther);
                        }
                        else if (_partitionj[k] == '2')//Hold notes have specific colors/materials
                        {
                            arrowVisualizer.changeMat(matHold);
                        }
                        else if (_partitionj[k] == '3')
                        {
                            arrowVisualizer.changeMat(matHoldTrail);
                        }
                    }
                }
            }

            //edge hold case should show on entire area, pretty rare
            for (int k = 0; k < did_hold.Length; k++)
            {
                if (scoreManager.isHolding[k] && (!did_hold[k]))
                {
                    var holdVisualizer = getHoldVisualizer();
                    double y0 = 0;
                    double y1 = displayYBounds[0];
                    Vector3 relativePos1 = new Vector3(visualize_distance * (k - (0.5f * _partition[0][0].Length)), (float)((y1 + y0) / 2.0));
                    holdVisualizer.transform.position = gameObject.transform.position + relativePos1;
                    holdVisualizer.transform.localScale = new Vector3(holdVisualizer.transform.localScale.x, (float)(y0 > y1 ? y0 - y1 : y1 - y0), holdVisualizer.transform.localScale.z);
                }
            }

            clearVisualizers();//we might need to show less visualizers than previous frame->hide the unused ones
        }

        /// <summary>
        /// Arrows' rotation depends on their columns and the current gamemode. Considers the arrow's default direction is left
        /// </summary>
        /// <param name="k">column of the arrow/note</param>
        /// <param name="gameMode">how many columns they are in total, also indicates if we play dance-single/double or para-single</param>
        /// <returns>Euler angles the arrow should use</returns>
        public Vector3 getEulerArrow(int k, int gameMode)
        {
            switch (gameMode)
            {
                case 4:
                case 8:
                    switch (k)
                    {
                        case 4:
                        case 0:
                            return new Vector3(0, 0, 0);
                        case 5:
                        case 1:
                            return new Vector3(0, 0, 90);
                        case 6:
                        case 2:
                            return new Vector3(0, 0, -90);
                        case 7:
                        case 3:
                            return new Vector3(0, 0, 180);
                    }
                    break;
                case 5:
                    switch (k)
                    {
                        case 0:
                            return new Vector3(0, 0, 0);
                        case 1:
                            return new Vector3(0, 0, -45);
                        case 2:
                            return new Vector3(0, 0, -90);
                        case 3:
                            return new Vector3(0, 0, -135);
                        case 4:
                            return new Vector3(0, 0, 180);
                    }
                    break;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Hides the visualizers that should not be shown for this frame
        /// </summary>
        public void clearVisualizers()
        {
            for (int i = nbVisualizersToShow; i < nbVisualizersShown; i++)
                arrowVisualizers[i].gameObject.SetActive(false);
            nbVisualizersShown = nbVisualizersToShow;

            for (int i = nbHoldVisualizersToShow; i < nbHoldVisualizersShown; i++)
                holdVisualizers[i].gameObject.SetActive(false);
            nbHoldVisualizersShown = nbHoldVisualizersToShow;
        }

        /// <summary>
        /// Hides all the visualizers, useful for the end or start of the song
        /// </summary>
        public void hideAllVisualizers()
        {
            nbVisualizersToShow = 0;
            nbHoldVisualizersToShow = 0;
            clearVisualizers();
        }

        /// <summary>
        /// Sets default values for the holds related array
        /// </summary>
        public void clearHolds()
        {
            for (int i = 0; i < 8; i++)
            {
                previous_hold[i] = -1;
                did_hold[i] = false;
            }
        }
    }
}