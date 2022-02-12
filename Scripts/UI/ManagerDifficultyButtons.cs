
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Handles the management of the difficulty buttons which needs to be changed everytime a user select a new music to play
    /// </summary>
    public class ManagerDifficultyButtons : UdonSharpBehaviour
    {
        public ScoreManager scoreManager;//Scoremanager/player side affected by the difficulty buttons managed
        public StepfilesManager stepfilesManager;//StepfileManager used by the scoremanager
        SelectDifficultyButton[] buttons;//buttons generated
        public SelectDifficultyButton prefabButton;//template to generate the buttons
        public GameObject parentButton;//buttons should be placed in a scroll list, thus we need the parent that represents the scroll list

        bool isInit = false;//did the stepfilesManager called InitButtons?
        int previousMusic = -1;//last known music

        /// <summary>
        /// pre-instantiate the buttons for a pooling-style management
        /// </summary>
        public void initButtons()
        {
            buttons = new SelectDifficultyButton[15];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = VRCInstantiate(prefabButton.gameObject).GetComponent<SelectDifficultyButton>();
                buttons[i].transform.parent = parentButton.transform;
                buttons[i].gameObject.SetActive(false);
                buttons[i].scoreManager = scoreManager;
                buttons[i].transform.localScale = prefabButton.transform.localScale;
                buttons[i].transform.localPosition = prefabButton.transform.localPosition;
                buttons[i].transform.localRotation = prefabButton.transform.localRotation;
            }
            isInit = true;
        }

        void Update()
        {
            if (!isInit)
                return;
            if (stepfilesManager._keepGoing)//stepfiles_manager isn't ready yet/all reader objects haven't finished
                return;
            if (previousMusic == stepfilesManager.music)
                return;
            previousMusic = stepfilesManager.music;
            musicChanged(previousMusic);
        }

        //when the last known music is changed, it means that we switched the .sm file => partitions/difficulties are therefore different and difficulty buttons should be remade
        void musicChanged(int music)
        {
            foreach (var b in buttons)
                b.gameObject.SetActive(false);
            int nbPartitions = 0;
            for (int i = 0; i < stepfilesManager.smReaders[music].nbPartitions; i++)
            {
                if (stepfilesManager.smReaders[music].partitionInfos_GameType[i] == scoreManager.gameMode)
                {
                    buttons[nbPartitions].partition = i;
                    switch (stepfilesManager.smReaders[music].partitionsInfos_DifficultyRank[i])
                    {
                        case 0://Thanks udon/udon# for not letting people use constants from another class, shaking my smh
                            buttons[nbPartitions].text.text = "BEGINNER";
                            break;
                        case 1:
                            buttons[nbPartitions].text.text = "EASY";
                            break;
                        case 2:
                            buttons[nbPartitions].text.text = "MEDIUM";
                            break;
                        case 3:
                            buttons[nbPartitions].text.text = "HARD";
                            break;
                        case 4:
                            buttons[nbPartitions].text.text = "CHALLENGE";
                            break;
                        default:
                            if (stepfilesManager.smReaders[music].partitionsInfos_DifficultyNumerical[i] == 0)
                                continue;
                            buttons[nbPartitions].text.text = "EDIT";
                            break;
                    }
                    buttons[nbPartitions].text.text += " (" + stepfilesManager.smReaders[music].partitionsInfos_DifficultyNumerical[i] + ")";
                    buttons[nbPartitions].gameObject.SetActive(true);
                    nbPartitions++;
                }

                if ((scoreManager.partition >= stepfilesManager.smReaders[music].nbPartitions) || (stepfilesManager.smReaders[music].partitionInfos_GameType[scoreManager.partition] != scoreManager.gameMode) || (stepfilesManager.smReaders[music].partitionsInfos_DifficultyRank[scoreManager.partition] == 0))
                {
                    scoreManager.partition = buttons[0].partition;//fallback partition
                }
            }
        }
    }
}