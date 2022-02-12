﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Originally intended to only manage the StepmaniaReaders, this class serves as a gateway between most classes of the StepmaniaVRC project. It also manages the start/stop of a song
    /// </summary>
    public class StepfilesManager : UdonSharpBehaviour
    {
        public TextAsset[] smFiles;//.sm files, but in .sm.txt because unity can't make textasset from .smn, should be passed in scene
        public AudioClip[] audioFilesForSM;//audio file associated to the smFilesn, should be passed in scene and have the same size as smFiles

        [HideInInspector]
        public StepmaniaReader[] smReaders;//smReaders that will be generated by the stepfilesManager

        public StepmaniaReader smReaderPrefab;//Prefab smReader used for the generation, should be passed in scene
        public StepfileVisualizer[] visualizers;//Visualizers that are connected to this stepfilesManager, should be passed in scene
        public ScoreManager[] scoreManagers;//ScoreManagers that are connected to this stepfilesManager, should be passed in scene and have the same size as visualizers
        public DandePadManager[] dancePadManagers;//DancePadManagers that are connected to this stepfilesmanager, should be passed in scenes and have the same size as visualizers (if game mode uses dancepads)
        public bool isParaPara = false;//is game mode using parapara pads or dance pads?
        public ParaPadManager[] paraPadManagers;////ParaPadManagers that are connected to this stepfilesmanager, should be passed in scenes and have the same size as visualizers (if game mode uses parapads)
        [UdonSynced, HideInInspector]
        public bool hasStarted = false; //had the song started for the owner of the object?
        [UdonSynced, HideInInspector]
        public bool wannaStart = false; //do this manager wants to start the song?
        public AudioSource audioSource;//AudioSource used by the object, needs to be passed in scene
        [HideInInspector]
        public bool audioSourceDidStart = false;//Did the audio start on the local instance of this object

        [UdonSynced, HideInInspector]
        public long timestampAudioStart = 0;//Timestamp of the moment the owner started the audio, used for syncing audio across players

        [UdonSynced, HideInInspector]
        public int music;//what is the smReader currently selected?

        long timestamp;//just a debug tool to know amount of time taken by file loading
        public ManagerDifficultyButtons[] managerDifficulties;//DifficultyButtonsManagers associated to the playable visualizers/scoreManagers, should be passed in scene and have he same size as visualizers/scoreManagers
        
        /// <summary>
        /// Instantiate most things and build the links between the objects passed in scene
        /// </summary>
        void Start()
        {
            timestamp = System.DateTime.Now.Ticks;
            smReaders = new StepmaniaReader[smFiles.Length];
            initReaders();

            for (int i = 0; i < scoreManagers.Length; i++)
            {
                scoreManagers[i].stepfilesManager = this;
                if (!isParaPara)
                {
                    dancePadManagers[i].scoreManager = scoreManagers[i];
                    dancePadManagers[i].initScoreManagerArrays();
                }
                else
                {
                    paraPadManagers[i].scoreManager = scoreManagers[i];
                    paraPadManagers[i].initScoreManagerArrays();
                }
                visualizers[i].scoreManager = scoreManagers[i];
                visualizers[i].stepfilesManager = this;
                scoreManagers[i]._isInit = true;
                managerDifficulties[i].stepfilesManager = this;
                managerDifficulties[i].scoreManager = scoreManagers[i];
                managerDifficulties[i].initButtons();
            }
        }

        int _reader = 0;//which file is the next to read?
        [HideInInspector]
        public bool _keepGoing = true;//is the manager still loading files?
        /// <summary>
        /// To avoid getting timeout'd by vrchat, we load only one stepfile a frame (which is still super laggy as one stepfile = 1-2 seconds)
        /// One possible optimization/QoL is to fraction the smReader's ParseSMFile function so that it does only a limited number of lines per frame
        /// </summary>
        /// <returns>bool: is the manager still loading files?</returns>
        bool initReaders()
        {
            if (!_keepGoing)
                return false;
            smReaders[_reader] = VRCInstantiate(smReaderPrefab.gameObject).GetComponent<StepmaniaReader>();
            smReaders[_reader].smFile = smFiles[_reader];
            smReaders[_reader].ParseSMFile_default();
            createSongButton(_reader);
            _reader++;
            _keepGoing = _reader < smFiles.Length;
            if (!_keepGoing)
                UnityEngine.Debug.Log("time taken loading " + smReaders.Length + " files: " + ((System.DateTime.Now.Ticks - timestamp) / 10000) + "ms");
            return true;
        }

        void Update()
        {
            if (initReaders())//Reading sm files before doing anything else
                return;
            if (!wannaStart && !hasStarted)//stop the song depending on synced variables, might need to add loading the song here for better synchronization of the song
            {
                audioSourceDidStart = false;
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                    audioSource.time = 0;//just to be sure that if there is concurrency in starting the song, it starts from the beginning
                }
            }
            if (wannaStart)//Owner wants to start but has yet to start the song
            {
                if (!Networking.IsOwner(this.gameObject))//ensure the owner is the one starting
                    return;
                audioSource.clip = audioFilesForSM[music];
                timestampAudioStart = System.DateTime.UtcNow.Ticks;
                resetScores();
                audioSource.Play();
                audioSource.time = 0;
                wannaStart = false;
                hasStarted = true;
                audioSourceDidStart = true;
                return;
            }
            if (hasStarted)//Behaviour when the song started for the owner of the gameobject
            {
                if (!audioSourceDidStart)//song has started for the owner but not for this instance->start the song accounting the delay
                {
                    audioSource.clip = audioFilesForSM[music];
                    resetScores();
                    audioSource.Play();
                    audioSource.time = (System.DateTime.UtcNow.Ticks - timestampAudioStart) / (10000f * 1000f);//timeAudio;//Sync if someone is joining
                    audioSourceDidStart = true;
                }

                if (!audioSource.isPlaying)
                {
                    //song has ended
                    hasStarted = false;
                    foreach (var visualizer in visualizers)
                    {
                        visualizer.hideAllVisualizers();
                    }
                    return;
                }

                foreach (var visualizer in visualizers)//song is still playing->need to visualize the charts
                {
                    visualizer.Visualize(audioSource.time);
                }
            }
        }

        /// <summary>
        /// resets the score of each associated scoreManager, useful when changing song or starting a song
        /// </summary>
        void resetScores()
        {
            for (int i = 0; i < scoreManagers.Length; i++)
            {
                scoreManagers[i].resetScore();
                if (!(scoreManagers[i].partition < smReaders[music].nbPartitions))
                {
                    scoreManagers[i].partition = 0;//fallback partition to avoid errors
                }
            }
        }

        public GameObject parentButtons;//StepfilesManager also manages the songlist spawning, this field is the parent of the buttonList and songButtons should be created as child of it, should be passed in scene
        public SelectSongButton songButtonPrefab;//SelectSongButton that will be cloned for the songlist, should be passed in scene
        /// <summary>
        /// Generates a song button of smReaders[i]
        /// </summary>
        /// <param name="i">index of smReader to create a songButton of</param>
        public void createSongButton(int i)
        {
            var obj = VRCInstantiate(songButtonPrefab.gameObject).GetComponent<SelectSongButton>();
            obj.transform.parent = parentButtons.transform;
            obj.textButton.text = smReaders[i].title;
            obj.gameObject.SetActive(true);
            obj.music = i;
            obj.transform.localScale = songButtonPrefab.transform.localScale;
            obj.transform.localPosition = songButtonPrefab.transform.localPosition;
            obj.transform.localRotation = songButtonPrefab.transform.localRotation;
            obj.stepfilesManager = this;
        }

        /// <summary>
        /// Changes the current selected music, is a function because used by UI + did extra steps before
        /// </summary>
        /// <param name="music">id of the smReader associated to the new current music</param>
        public void changeMusic(int music)
        {
            this.music = music;
        }
    }
}