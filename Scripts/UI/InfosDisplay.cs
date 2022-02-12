
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// Manages the UI regarding the active players + difficulties + song, is the panel above the game screen
    /// </summary>
    public class InfosDisplay : UdonSharpBehaviour
    {
        //all shall be passed in scene
        public UnityEngine.UI.Text[] textsScores;//Player name + difficulty played
        public ScoreManager[] scoreManagers;//Score manager to base the display on
        public StepfilesManager stepfilesManager;//StepfileManager to base the display on
        public UnityEngine.UI.Text textSong;//Title of the song

        void Update()
        {
            if (stepfilesManager._keepGoing)
                return;
            for (int i = 0; i < textsScores.Length; i++)
            {
                textsScores[i].text = Networking.GetOwner(scoreManagers[i].gameObject).displayName + " - ";
                switch (stepfilesManager.smReaders[stepfilesManager.music].partitionsInfos_DifficultyRank[scoreManagers[i].partition])
                {
                    case 0://Thanks udon/udon# for not letting people use constants from another class, shaking my smh
                        textsScores[i].text += "BEGINNER";
                        break;
                    case 1:
                        textsScores[i].text += "EASY";
                        break;
                    case 2:
                        textsScores[i].text += "MEDIUM";
                        break;
                    case 3:
                        textsScores[i].text += "HARD";
                        break;
                    case 4:
                        textsScores[i].text += "CHALLENGE";
                        break;
                    default:
                        textsScores[i].text += "EDIT";
                        break;
                }

            }
            textSong.text = stepfilesManager.smReaders[stepfilesManager.music].title;
        }
    }
}