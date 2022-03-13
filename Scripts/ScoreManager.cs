
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// ScoreManager handles the "timing" part of the gameplay and computes the user's score. It also also manages the lifebar
    /// </summary>
    public class ScoreManager : UdonSharpBehaviour
    {
        public int gameMode = 4;//Number of arrow columns/ dance-single or dance-double or para-single

        [UdonSynced]
        public int partition = 3;//partition of the current song from stepfilesManager used by the player owner of this scoreManager

        [HideInInspector]
        public StepfilesManager stepfilesManager;//Hidden because every "scene-reference" should be towards StepfilesManager in order to avoid having to much to handle

        [HideInInspector]
        public double[] latestActivated = new double[8];//bar position of the latest activated note for each column (note that had a judgment, even a miss)

        [UdonSynced, HideInInspector]
        public int nbMarvelousOK = 0, nbPerfects = 0, nbGreats = 0, nbGood = 0, nbBoo = 0, nbMiss = 0;//Atomic elements that will then be used to compute the score

        [UdonSynced, HideInInspector]
        public int combo = 0, lastRating = 0;//Not used to compute the score, but gives nice feedback to the player
        public const int MARVELOUS = 0, PERFECT = 1, GREAT = 2, GOOD = 3, BOO = 4, MISS = 5;
        [UdonSynced, HideInInspector]
        public double lastChangeTime = 0, lastCheckedTime = 0, lastMissCheck = 0;//last verification times, used in order to avoid checking the same things over and over
        
        [HideInInspector]
        public bool[] isHolding = new bool[8];//Is the column being held (a start hold note passed a validation)

        [HideInInspector]
        public bool[] areActive;//columns held this frame, a press is detected if wereActive was false but this one is true
        [HideInInspector]
        public bool[] wereActive;//columns held previous frame

        const int MAX_RELEASE_FRAMES = 3;//has to release for more than 3 frames for hold to fail, adds some leniency to the hold notes
        [HideInInspector]
        public int[] releaseLimit = new int[8];//array that manages that leniency for each column

        //Timings are the same as DDR, given in seconds, more lenient because considering previous frame to compensate lag
        const double TIMING_MARVELOUS = 16.7 / 1000.0, TIMING_PERFECT = 33.0 / 1000.0;//, TIMING_GREAT = 92.0 / 1000.0, TIMING_GOOD = 142.0 / 1000.0, TIMING_BOO = 170.0 / 1000.0;
        const double TIMING_GREAT = 66.0 / 1000.0, TIMING_GOOD = 92.0 / 1000.0, TIMING_BOO = 92.0 / 1000.0;//Shifting great/good/boo to reduce the amount of innermost checks

        [HideInInspector]
        public bool _isInit = false;//before doing anything, we wait for the stepfilesmanager to pass everything we need

        [UdonSynced, HideInInspector]
        public float lifeBar = 0.5f;

        private void Update()
        {
            if (!_isInit)
                return;
            if (stepfilesManager._keepGoing)
                return;
            if (stepfilesManager.hasStarted && stepfilesManager.audioSource.isPlaying)
            {
                displayScore();
            }
            else
            {
                endResults();
            }
        }

        //Unused, could be used if we want to manually update instead of letting unity do things
        public void do_Update()
        {
            if (stepfilesManager == null)
                return;
            if (stepfilesManager.hasStarted && stepfilesManager.audioSource.isPlaying)
                processScore(stepfilesManager.audioSource.time + stepfilesManager.audioOffset);
            if (lastChangeTime != 0 || (stepfilesManager.hasStarted && stepfilesManager.audioSource.isPlaying))
                displayScore();
        }

        //declaring prior to avoid gc calls
        double measureStartMissCheck;
        double measureEndMissCheck;
        int start, end;
        double bar_divider, tmp_bar;
        double measureStartChecks, measureEndChecks;
        bool[] hadAValueChange = new bool[8];
        double barStart_MARVELOUS, barEnd_MARVELOUS, barStart_PERFECT, barEnd_PERFECT, barStart_GREAT, barEnd_GREAT, barStart_GOOD, barEnd_GOOD, currentTimeBar;
        double[] ttConvert = new double[9];
        double[] ttbars = new double[9];

        /// <summary>
        /// Processes the note judgment according to the player's button press and the time on the music for the current partition
        /// </summary>
        /// <param name="time">time in the music</param>
        public void processScore(double time)
        {
            var stepfile = stepfilesManager.music;
            if (stepfilesManager.smReaders[stepfile].partitionsInfos_NbNotes[partition] == 0)
                return;

            var _partition = stepfilesManager.smReaders[stepfile].partitions[partition];
            char[][] _partitioni;
            char[] _partitionj;

            //Possible optimization -> regroup all time to bar into one timetobarmultiple, had issue when trying it for unknown reason so gave up on this and do only the judgment timings grouped

            //Hold release
            checkRelease();

            //Miss check
            measureStartMissCheck = stepfilesManager.smReaders[stepfile].TimeToBar(lastMissCheck);
            lastMissCheck = lastCheckedTime - TIMING_BOO;
            measureEndMissCheck = stepfilesManager.smReaders[stepfile].TimeToBar(lastMissCheck);
            start = (int)Math.Floor(measureStartMissCheck); end = (int)Math.Floor(measureEndMissCheck);
            for (int i = start; i <= end; i++)
            {
                if (i < 0)
                    continue;
                if (i >= _partition.Length)
                    break;
                bar_divider = _partition[i].Length;
                _partitioni = _partition[i];
                for (int j = 0; j < _partitioni.Length; j++)
                {
                    tmp_bar = i + (j / bar_divider);
                    if (tmp_bar < measureStartMissCheck || tmp_bar > measureEndMissCheck)
                        continue;
                    _partitionj = _partitioni[j];
                    for (int k = 0; k < gameMode; k++)
                    {
                        if (latestActivated[k] >= tmp_bar)//already went there
                            continue;
                        if (_partitionj[k] == '0')
                            continue;
                        if (_partitionj[k] == '3')//no double punition for missed hold notes
                            continue;
                        //Something was missed
                        nbMiss++; changeLife(MISS);
                        combo = 0;
                        lastRating = MISS;
                        latestActivated[k] = tmp_bar;
                        lastChangeTime = time;
                    }
                }
            }
            //PressChecks
            measureStartChecks = measureEndMissCheck;
            start = (int)Math.Floor(measureStartChecks);
            measureEndChecks = stepfilesManager.smReaders[stepfile].TimeToBar(time + TIMING_BOO);
            end = (int)Math.Ceiling(measureEndChecks);
            for (int i = 0; i < 8; i++)
                hadAValueChange[i] = false;

            //can't use {} assign with udon
            ttConvert[0] = lastCheckedTime - TIMING_GOOD; ttConvert[1] = lastCheckedTime - TIMING_GREAT; ttConvert[2] = lastCheckedTime - TIMING_PERFECT; ttConvert[3] = lastCheckedTime - TIMING_MARVELOUS;
            lastCheckedTime = time;//too lazy to change "properly"
            ttConvert[4] = time;
            ttConvert[5] = lastCheckedTime + TIMING_MARVELOUS; ttConvert[6] = lastCheckedTime + TIMING_PERFECT; ttConvert[7] = lastCheckedTime + TIMING_GREAT; ttConvert[8] = lastCheckedTime + TIMING_GOOD;
            ttbars = stepfilesManager.smReaders[stepfile].TimeToBarMultiple(ttConvert);
            barStart_GOOD = ttbars[0]; barStart_GREAT = ttbars[1]; barStart_PERFECT = ttbars[2]; barStart_MARVELOUS = ttbars[3];
            currentTimeBar = ttbars[4];
            barEnd_MARVELOUS = ttbars[5]; barEnd_PERFECT = ttbars[6]; barEnd_GREAT = ttbars[7]; barEnd_GOOD = ttbars[8];

            for (int i = start; i <= end; i++)
            {
                if (i < 0)
                    continue;
                if (i >= _partition.Length)
                    break;
                bar_divider = _partition[i].Length;
                _partitioni = _partition[i];
                for (int j = 0; j < _partitioni.Length; j++)
                {
                    tmp_bar = i + (j / bar_divider);
                    if (tmp_bar < measureStartChecks || tmp_bar > measureEndChecks)
                        continue;
                    _partitionj = _partitioni[j];
                    for (int k = 0; k < gameMode; k++)
                    {
                        if (latestActivated[k] >= tmp_bar)//already went past there
                            continue;
                        if (_partitionj[k] == '0')
                            continue;
                        if (_partitionj[k] == '3')
                        {
                            if (tmp_bar <= currentTimeBar && isHolding[k])
                            {
                                isHolding[k] = false;
                                latestActivated[k] = tmp_bar;
                                lastChangeTime = time;
                                nbMarvelousOK++; changeLife(MARVELOUS);
                                combo++;
                                lastRating = MARVELOUS;
                            }
                            continue;
                        }

                        //There is a note to press (=='1' or '2')
                        if ((!hadAValueChange[k]) && areActive[k] && !wereActive[k])
                        {
                            if (stepfilesManager.smReaders[stepfile].partitions[partition][i][j][k] == '2')//hold note start
                            {
                                addHolding(k);
                            }
                            latestActivated[k] = tmp_bar;//latest activated step/bar is this one
                            lastChangeTime = time;
                            if (tmp_bar > currentTimeBar)//early step
                            {
                                hadAValueChange[k] = true; //stepped early->no further validation for this arrow
                                                           //humme yandev style coding
                                if (tmp_bar < barEnd_MARVELOUS)
                                {
                                    nbMarvelousOK++; changeLife(MARVELOUS);
                                    combo++;
                                    lastRating = MARVELOUS;
                                }
                                else if (tmp_bar < barEnd_PERFECT)
                                {
                                    nbPerfects++; changeLife(PERFECT);
                                    combo++;
                                    lastRating = PERFECT;
                                }
                                else if (tmp_bar < barEnd_GREAT)
                                {
                                    nbGreats++; changeLife(GREAT);
                                    combo++;
                                    lastRating = GREAT;
                                }
                                else if (tmp_bar < barEnd_GOOD)
                                {
                                    nbGood++; changeLife(GOOD);
                                    combo = 0;
                                    lastRating = GOOD;
                                }
                                else
                                {
                                    nbBoo++; changeLife(BOO);
                                    combo = 0;
                                    lastRating = BOO;
                                }
                            }
                            else
                            {
                                //hummmmmme yandev style coding part 2
                                if (tmp_bar > barStart_MARVELOUS)
                                {
                                    nbMarvelousOK++; changeLife(MARVELOUS);
                                    combo++;
                                    lastRating = MARVELOUS;
                                    latestActivated[k] = tmp_bar;
                                }
                                else if (tmp_bar > barStart_PERFECT)
                                {
                                    nbPerfects++; changeLife(PERFECT);
                                    combo++;
                                    lastRating = PERFECT;
                                    latestActivated[k] = tmp_bar;
                                }
                                else if (tmp_bar > barStart_GREAT)
                                {
                                    nbGreats++; changeLife(GREAT);
                                    combo++;
                                    lastRating = GREAT;
                                    latestActivated[k] = tmp_bar;
                                }
                                else if (tmp_bar > barStart_GOOD)
                                {
                                    nbGood++; changeLife(GOOD);
                                    combo = 0;
                                    lastRating = GOOD;
                                }
                                else
                                {
                                    nbBoo++; changeLife(BOO);
                                    combo = 0;
                                    lastRating = BOO;
                                }
                            }

                        }
                    }
                }
            }

            for (int i = 0; i < areActive.Length; i++)
            {
                wereActive[i] = areActive[i];
                areActive[i] = false;
            }
            lastCheckedTime = time;
        }

        //Based on https://github.com/stepmania/stepmania/blob/984dc8668f1fedacf553f279a828acdebffc5625/src/LifeMeterBar.cpp
        //For values used https://github.com/stepmania/stepmania/blob/984dc8668f1fedacf553f279a828acdebffc5625/Themes/_fallback/base._ini + ctrl-F "LifeMeterBar", getting all const just in case but don't need all
        //W0/1/2/3/4/5 = Marvelous/Perfect/Great/Good/Boo/Miss
        const float DangerThreshold = 0.2f, InitialValue = 0.5f, HotValue = 1.0f, LifeMultiplier = 1.0f, LifePercentChangeMARVELOUS = 0.008f, LifePercentChangePERFECT = 0.008f, LifePercentChangeGREAT = 0.004f,
            LifePercentChangeGOOD = 0, LifePercentChangeBOO = -0.04f, LifePercentChangeMISS = -0.08f, LifePercentChangeCheckpointMiss = -0.002f, LifePercentChangeCheckpointHit = 0.002f;
        const int MinStayAlive = GREAT, MinScoreToFlash = GREAT, UnderX = 0, UnderY = 0, DangerX = 0, DangerY = 0, StreamX = 0, StreamY = 0, OverX = 0, OverY = 0;
        const bool FlashNoteOnHit = false;
        const float LifeDifficulty = 1.0f;//reduce this if you want life to be on the kinder side

        /// <summary>
        /// Changes the value of the lifeBar depending on the jugment of a note
        /// </summary>
        /// <param name="note">judgment of a note (one of the constants MARVELOUS, PERFECT, ...)</param>
        void changeLife(int note)
        {
            if (lifeBar <= 0)
                return;
            float deltaLife = 0;
            switch (note)
            {
                case MARVELOUS:
                    deltaLife = LifePercentChangeMARVELOUS;
                    break;
                case PERFECT:
                    deltaLife = LifePercentChangePERFECT;
                    break;
                case GREAT:
                    deltaLife = LifePercentChangeGREAT;
                    break;
                case GOOD:
                    deltaLife = LifePercentChangeGOOD;
                    break;
                case BOO:
                    deltaLife = LifePercentChangeBOO;
                    break;
                case MISS:
                    deltaLife = LifePercentChangeMISS;
                    break;
            }
            if (deltaLife > 0)
                deltaLife *= LifeDifficulty;
            else
                deltaLife /= LifeDifficulty;
            lifeBar += deltaLife;
            if (lifeBar > 1)
                lifeBar = 1;
            if (lifeBar < 0)
                lifeBar = 0;
        }

        /// <summary>
        /// Was an hold note released before validation?
        /// </summary>
        void checkRelease()
        {
            for (int i = 0; i < isHolding.Length; i++)
            {
                if (!isHolding[i])
                    continue;
                if (!areActive[i])
                    releaseLimit[i]++;
                else
                    releaseLimit[i] = 0;

                if (releaseLimit[i] >= MAX_RELEASE_FRAMES)
                    isHolding[i] = false;
            }
        }
        /// <summary>
        /// The player successed in a start hold note, so we record that he has to keep holding
        /// </summary>
        /// <param name="i">column of the hold note</param>
        void addHolding(int i)
        {
            isHolding[i] = true;
            releaseLimit[i] = 0;
        }

        public UnityEngine.UI.Text scoreDisplay;//UI Text in which will be displayed the score, should be passed in scene
        public UnityEngine.UI.Text judgmentDisplay;//UI Text in which will be displayed the judgment and combo, should be passed in scene

        /// <summary>
        /// Updates the score and judgment displays
        /// </summary>
        public void displayScore()
        {

            scoreDisplay.text = ((int)score) + "";
            switch (lastRating)
            {
                case MARVELOUS:
                    judgmentDisplay.color = Color.gray;
                    judgmentDisplay.text = "MARVELOUS\n" + combo;
                    break;
                case PERFECT:
                    judgmentDisplay.color = Color.yellow;
                    judgmentDisplay.text = "PERFECT\n" + combo;
                    break;
                case GREAT:
                    judgmentDisplay.color = Color.green;
                    judgmentDisplay.text = "GREAT\n" + combo;
                    break;
                case GOOD:
                    judgmentDisplay.color = Color.cyan;
                    judgmentDisplay.text = "GOOD";
                    break;
                case BOO:
                    judgmentDisplay.color = Color.blue;
                    judgmentDisplay.text = "BOO";
                    break;
                case MISS:
                    judgmentDisplay.color = Color.red;
                    judgmentDisplay.text = "MISS";
                    break;
            }
            displayLife();
            if (lifeBar <= 0)
                scoreDisplay.text += " (FAILED)";
        }
        /// <summary>
        /// Displays the end results (detailed numbers of marvelous/perfect/.../miss
        /// </summary>
        public void endResults()
        {
            scoreDisplay.text = ((int)score) + (lifeBar <= 0 ? " (FAILED)" : "");
            judgmentDisplay.color = Color.gray;
            judgmentDisplay.text = "MARVELOUS: " + nbMarvelousOK + "\nPERFECT: " + nbPerfects + "\nGREAT: " + nbGreats + "\nGOOD: " + nbGood + /*"\nBOO: " + nbBoo +*/ "\nMISS: " + nbMiss;
        }

        public UnityEngine.UI.Image imageLifeBar;//Visualizer element of the lifebar, should be passed in scene
        public Material materialLifeHot, materialLifeDanger, materialLifeDefault;//Materials/Colors used for the different states of the lifebar, Hot means full lifebar, Danger is low life
        /// <summary>
        /// Updates the visualization of the lifebar
        /// </summary>
        public void displayLife()
        {
            imageLifeBar.transform.localScale = new Vector3(lifeBar, imageLifeBar.transform.localScale.y, imageLifeBar.transform.localScale.z);
            if (lifeBar >= HotValue)
                imageLifeBar.material = materialLifeHot;
            else if (lifeBar <= DangerThreshold)
                imageLifeBar.material = materialLifeDanger;
            else
                imageLifeBar.material = materialLifeDefault;
        }

        /// <summary>
        /// Put all score-related variables to their default values
        /// </summary>
        public void resetScore()
        {
            for (int i = 0; i < 8; i++)
            {
                latestActivated[i] = 0;
                isHolding[i] = false;
            }
            nbMarvelousOK = 0; nbPerfects = 0; nbGreats = 0; nbGood = 0; nbBoo = 0; nbMiss = 0;
            combo = 0; lastRating = 0;
            lastChangeTime = 0; lastCheckedTime = 0; lastMissCheck = 0;
            lifeBar = InitialValue;
        }

        //StepScore (from remywiki): 1,000,000 ÷ (Number of steps [A jump will be considered a step in scoring] + Number of freezes [A pair of freeze that starts at the same time will be considered as one freeze in scoring] + Number of 4-way or 8-way Shock Arrows
        //simplified for holds, each hold is just considered as 2 step in scoring
        public double stepScore
        {
            get { return 1000000.0 / (stepfilesManager.smReaders[stepfilesManager.music].partitionsInfos_NbNotes[partition] != 0 ? stepfilesManager.smReaders[stepfilesManager.music].partitionsInfos_NbNotes[partition] : 1); }
        }

        //Remywiki: Score: SC x (number of Marvelous + number of OKs) + (SC - 10) x (number of Perfects) + [(SC * 3/5) - 10] x number of Greats + [(SC * 1/5) - 10] x number of Goods
        public double score
        {
            get { return stepScore * (nbMarvelousOK) + (stepScore - 10) * nbPerfects + (stepScore * 3 / 5) * nbGood + (stepScore / 2 - 10) * nbGood; }
        }
    }
}