
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace StepmaniaVRC
{
    /// <summary>
    /// StepmaniaReader parses a .sm file into usable "partitions", it contains multiple data related to a chart and the tools necessary to convert a point in time to a partition's "bar" and vice-versa
    /// </summary>
    public class StepmaniaReader : UdonSharpBehaviour
    {
        const char DECIMAL_DELIMITER = '.';//For some reason on my machine has to be , but . for build. Trying cleaner method didn't work for some reason
        //public string smPath = "";
        public TextAsset smFile; //can use a path and analyse a folder with Udon => have to manually pass the .sm as TextAssets in the scene

        //We will consider 25 as the max number of partition, too lazy to do things clean with udon (can't even do structs ffs)
        public const int MAX_PARTITIONS = 25;

        public int[] partitionInfos_GameType = new int[MAX_PARTITIONS];//number of validation arrows per gamemode, dance-single is 4, double is 8, parapara is 5
        public const int BEGINNER = 0, EASY = 1, MEDIUM = 2, HARD = 3, CHALLENGE = 4, OTHER = -1;//Const to indicate the difficulty as rank
        public int[] partitionsInfos_DifficultyRank = new int[MAX_PARTITIONS];
        public int[] partitionsInfos_DifficultyNumerical = new int[MAX_PARTITIONS];
        public int[] partitionsInfos_NbNotes = new int[MAX_PARTITIONS];

        /// <summary>
        /// Resets each partitionsInfos arrays to default values
        /// </summary>
        public void resetPartitionInfos()
        {
            for (int i = 0; i < MAX_PARTITIONS; i++)
            {
                partitionInfos_GameType[i] = 4;//true;
                partitionsInfos_DifficultyRank[i] = OTHER;
                partitionsInfos_DifficultyNumerical[i] = 0;
                partitionsInfos_NbNotes[i] = 0;
            }
        }

        // Copypaste from another project
        public string musicPath;//Unused, could be useful if udon starts supporting reading files from a folder
        public string smDir;//Unused, could be useful if udon starts supporting reading files from a folder
        public double offset = 0;
        public string title = "???";
        const int DEFAULT_DICT_SIZE = 100;
        public char[][][][] partitions = null;//[partition index][bar index][bar_fraction][notes column (arrow)]
        public int nbPartitions = 0;
        public int currentPartition
        {
            get { return nbPartitions - 1; }
        }
        public int currentPartitionNbBars = 0;
        public int currentBar
        {
            get { return currentPartitionNbBars - 1; }
        }
        public int currentBarNbLines = 0;
        public int currentLine
        {
            get { return currentBarNbLines - 1; }
        }
        /// <summary>
        /// .Add() of Pseudo List of Partition, extremely unoptimized as it creates a new list with one extra space and copy the old into the new one
        /// </summary>
        public void AddPartition()
        {
            var partitionsTMP = partitions;
            partitions = new char[nbPartitions + 1][][][];
            for (int j = 0; j < nbPartitions; j++)
            {
                partitions[j] = partitionsTMP[j];
            }
            nbPartitions++;
            partitions[currentPartition] = null;
            currentPartitionNbBars = 0;
        }
        /// <summary>
        /// .Add() of Pseudo SubList Bar of Pseudo Partition, extremely unoptimized as it creates a new list with one extra space and copy the old into the new one
        /// </summary>
        public void AddBar()
        {
            var currentPartitionTMP = partitions[currentPartition];
            partitions[currentPartition] = new char[currentPartitionNbBars + 1][][];
            for (int j = 0; j < currentPartitionNbBars; j++)
            {
                partitions[currentPartition][j] = currentPartitionTMP[j];
            }
            currentPartitionNbBars++;
            partitions[currentPartition][currentBar] = null;
            currentBarNbLines = 0;
        }

        /// <summary>
        /// .Add() of Pseudo SubSubList Line of Pseudo SubList Bar of Pseudo Partition, extremely unoptimized as it creates a new list with one extra space and copy the old into the new one
        /// </summary>
        public void AddLine()
        {
            var currentBarTMP = partitions[currentPartition][currentBar];
            partitions[currentPartition][currentBar] = new char[currentBarNbLines + 1][];
            for (int j = 0; j < currentBarNbLines; j++)
            {
                partitions[currentPartition][currentBar][j] = currentBarTMP[j];
            }
            currentBarNbLines++;
            partitions[currentPartition][currentBar][currentLine] = null;
        }


        //public Dictionary<double, double> bpmsDict = new Dictionary<double, double>();
        public double[] bpmsDictKeys = null, bpmsDictValues = null;
        public int bpmsDictCount = 0;

        //public Dictionary<double, double> stopsDict = new Dictionary<double, double>();
        public double[] stopsDictKeys = null, stopsDictValues = null;
        public int stopsDictCount = 0;

        /// <summary>
        /// Headers in sm files are KEY:VALUE;
        /// Keys can be checked with a substring to :, Value needs an extra step
        /// </summary>
        /// <param name="headerLine">The full line to extract the value from</param>
        /// <returns>VALUE part of the header</returns>
        public string GetValueHeaderLine(string headerLine)
        {
            var ret = headerLine.Substring(headerLine.IndexOf(':') + 1);//get everything after "KEY:"
            //ret = ret.Substring(0, ret.Length > 2 ? ret.Length - 2 : 0);//Udon is drunk, trim at the end doesn't work for some reason
            ret = ret.Substring(0, ret.Contains(";") ? ret.IndexOf(";") : ret.Length > 2 ? ret.Length - 2 : 0);//should be cleaner, removing ;\r from VALUE;
            return ret;
        }

        /// <summary>
        /// Fills the partitions and partitionsInfo from the text of an .sm file
        /// </summary>
        /// <param name="fullfile"></param>
        public void ParseSMFile(string fullfile)
        {
            resetPartitionInfos();
            pseudoDictBpmsInit();
            pseudoDictStopsInit();

            string[] lines = fullfile.Split('\n');
            bool notesSection = false;
            int notesSince = -1;
            partitions = null;
            nbPartitions = 0;
            //Going through each line of the file:
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0)//empty line -> ignore
                    continue;
                //HEADERS
                if (lines[i][0] == '#')//# in stepfiles indicates a header for common values among partitions (song, bpms changes, stops, ...)
                {
                    string key = lines[i].Substring(0, lines[i].IndexOf(':')).Trim('#').Trim(':');//Extracting KEY from #KEY:VALUE;
                    switch (key.ToUpper())
                    {
                        case "TITLE":
                            title = GetValueHeaderLine(lines[i]);
                            break;
                        case "MUSIC":
                            musicPath = GetValueHeaderLine(lines[i]);
                            break;
                        case "OFFSET":
                            offset = double.Parse(GetValueHeaderLine(lines[i].Replace('.', DECIMAL_DELIMITER)), NumberStyles.Any, CultureInfo.InvariantCulture);//InvariantCulture should've taken care of '.' vs ',' depending on the machine, but doesn't works...
                            if (lines[i].Contains("-") && offset > 0)//fixes double.parse not working with negative numbers, other values shouldn't be negative
                                offset *= -1;
                            break;
                        case "BPMS":
                            string[] bpmpairs = GetValueHeaderLine(lines[i]).Split(',');
                            foreach (string bpmpair in bpmpairs)
                            {
                                string[] keyval = bpmpair.Split('=');
                                pseudoDictBpmsAdd(0.25 * double.Parse(keyval[0].Replace('.', DECIMAL_DELIMITER)), double.Parse(keyval[1].Replace('.', DECIMAL_DELIMITER)));
                            }
                            break;
                        case "STOPS":
                            string[] stoppairs = GetValueHeaderLine(lines[i]).Split(',');
                            foreach (string stoppair in stoppairs)
                            {
                                string[] keyval = stoppair.Split('=');
                                if (keyval.Length < 2)
                                    continue;
                                pseudoDictStopsAdd(0.25 * double.Parse(keyval[0].Replace('.', DECIMAL_DELIMITER)), double.Parse(keyval[1].Replace('.', DECIMAL_DELIMITER)));
                            }
                            break;
                        case "NOTES"://Notes header is special as it indicates we've reached a special point in the file where everything next are the partitions
                            notesSection = true;
                            notesSince = i;
                            break;
                        default:
                            break;
                    }
                    continue;
                }

                //Parafiles have awful formatting, let's try to fix the bad lines
                if (lines[i].Trim().StartsWith("//"))
                    continue;//commentary line=>exit

                //NOTES
                if (notesSection)
                {
                    switch (i - notesSince)//pre-Partition info are always in the same order
                    {
                        //Chart type (dance signle, double, ...), let's also create/initiate partition
                        case 1:
                            AddPartition();
                            AddBar();
                            if (lines[i].ToLower().Contains("dance-single"))
                                partitionInfos_GameType[currentPartition] = 4;
                            else if (lines[i].ToLower().Contains("para-single"))
                                partitionInfos_GameType[currentPartition] = 5;
                            else if (lines[i].ToLower().Contains("dance-double"))
                                partitionInfos_GameType[currentPartition] = 8;
                            break;
                        //Description/author
                        case 2:
                            break;
                        //Difficulty (one of Beginner, Easy, Medium, Hard, Challenge, Edit)
                        case 3:
                            var lowered = lines[i].ToLower();
                            if (lowered.Contains("beginner"))
                                partitionsInfos_DifficultyRank[currentPartition] = BEGINNER;
                            else if (lowered.Contains("easy"))
                                partitionsInfos_DifficultyRank[currentPartition] = EASY;
                            else if (lowered.Contains("medium"))
                                partitionsInfos_DifficultyRank[currentPartition] = MEDIUM;
                            else if (lowered.Contains("hard"))
                                partitionsInfos_DifficultyRank[currentPartition] = HARD;
                            else if (lowered.Contains("challenge"))
                                partitionsInfos_DifficultyRank[currentPartition] = CHALLENGE;
                            break;
                        //Numerical meter
                        case 4:
                            partitionsInfos_DifficultyNumerical[currentPartition] = int.Parse(lines[i].Replace(':', ' ').Trim());
                            break;
                        //Groove radar
                        case 5:
                            break;

                        //actual notes
                        default:
                            if (lines[i].Contains(";"))//(lines[i][0] == ';') some files ends with shit like ,;
                            {
                                if (partitions[currentPartition][currentBar] == null)
                                {
                                    //difficulty ends with ,->; : let's add an empty line to avoid fucking up visualizer/scoreManager
                                    AddLine();
                                    partitions[currentPartition][currentBar][currentLine] = new char[partitionInfos_GameType[currentPartition]];
                                    for (int j = 0; j < partitions[currentPartition][currentBar][currentLine].Length; j++)
                                        partitions[currentPartition][currentBar][currentLine][j] = '0';
                                }
                                notesSince = i;
                                break;
                            }
                            if (lines[i][0] == ',')
                            {
                                AddBar();
                                break;
                            }
                            if (lines[i].Trim() == "")
                                continue;
                            char[] notesgroup = lines[i].ToCharArray();
                            //removes mines as they are not supported in the simulator
                            for(int noteid = 0; noteid < notesgroup.Length; noteid++)
                            {
                                if (notesgroup[noteid] == 'M')
                                    notesgroup[noteid] = '0';
                            }
                            //
                            AddLine();
                            partitions[currentPartition][currentBar][currentLine] = notesgroup;
                            int check = partitionInfos_GameType[currentPartition];
                            //Update the number of notes=>checking if there's any note in the current line
                            for (int j = 0; j < check && j < notesgroup.Length; j++)
                            {
                                if (notesgroup[j] != '0')
                                    partitionsInfos_NbNotes[currentPartition]++;
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Just calls ParseSMFile with the text from the textAsset
        /// </summary>
        public void ParseSMFile_default()
        {
            ParseSMFile(smFile.text);
        }

        /// <summary>
        /// Time in the audio to a partition position (as a bar) considering the stops and bpm changes
        /// Prefer using TimeToBarMultiple if possible as this functions costs A LOT on the CPU on the UDON VM
        /// </summary>
        /// <param name="time">time to convert</param>
        /// <returns>A bar position</returns>
        public double TimeToBar(double time)
        {
            double current_bpm, bpm_startbar, key, to_remove, stop_start, tmp;
            current_bpm = -1;
            bpm_startbar = 0;
            for (int i = 0; i < bpmsDictCount; i++)
            {
                key = bpmsDictKeys[i];
                if (current_bpm < 0)
                    current_bpm = bpmsDictValues[i];
                if (BarToTime(key) < time)
                {
                    current_bpm = bpmsDictValues[i];
                    bpm_startbar = key;
                }
                else
                    break;
            }
            to_remove = 0;
            for (int i = 0; i < stopsDictCount; i++)
            {
                key = stopsDictKeys[i];
                if (key < bpm_startbar)
                    continue;
                stop_start = BarToTime2(key);//using btt2 because bpmdict->stopdict goes back in time and thus, the optimization would not occur
                if (stop_start > time)
                    break;
                if (stop_start + stopsDictValues[i] >= time)
                    return key;
                bpm_startbar = key;
                to_remove = stopsDictValues[i];
            }
            tmp = time - to_remove - BarToTime3(bpm_startbar);
            tmp = tmp / (4.0 * 60.0 / current_bpm);
            return bpm_startbar + tmp;
        }
        /// <summary>
        /// Time in the audio to a partition position (as a bar) considering the stops and bpm changes
        /// Does the same as TimeToBar but with ORDERED multiple times at the same time, in order to reduce compute complexity
        /// </summary>
        /// <param name="times">Ordered times to convert</param>
        /// <returns>Array of converted times to bars</returns>
        public double[] TimeToBarMultiple(double[] times)//we consider time ordonned
        {
            double current_bpm, bpm_startbar, key, to_remove, stop_start, tmp;
            current_bpm = -1;
            bpm_startbar = 0;
            double[] returns = new double[times.Length];
            int i = 0;
            int j = 0;
            bool has_passed;
            to_remove = 0;
            for (int curr_time = 0; curr_time < times.Length; curr_time++)
            {
                has_passed = false;
                while (i < bpmsDictCount)
                {
                    key = bpmsDictKeys[i];
                    if (current_bpm < 0)
                        current_bpm = bpmsDictValues[i];
                    if (BarToTime(key) < times[curr_time])
                    {
                        current_bpm = bpmsDictValues[i];
                        bpm_startbar = key;
                    }
                    else
                        break;
                    i++;
                }
                while (j < stopsDictCount)
                {
                    key = stopsDictKeys[j];
                    if (key < bpm_startbar)
                    {
                        j++;
                        continue;
                    }
                    stop_start = BarToTime2(key);//using btt2 because bpmdict->stopdict goes back in time and thus, the optimization would not occur
                    if (stop_start > times[curr_time])
                        break;
                    if (stop_start + stopsDictValues[j] >= times[curr_time])
                    {
                        has_passed = true;
                        returns[curr_time] = key;
                        break;
                    }
                    bpm_startbar = key;
                    to_remove = stopsDictValues[j];
                    j++;
                }
                if (!has_passed)
                {
                    tmp = times[curr_time] - to_remove - BarToTime3(bpm_startbar);
                    tmp = tmp / (4.0 * 60.0 / current_bpm);
                    returns[curr_time] = bpm_startbar + tmp;
                }
            }
            return returns;
        }

        //Previous (more natural) bar to time, kept it because more human-friendly but use the other one instead
        /// <summary>
        /// Tranform a bar/partition position to time
        /// </summary>
        /// <param name="bar">the bar to convert</param>
        /// <returns>the time converted</returns>
        public double _BarToTime(double bar)
        {
            bool first;
            double currentBPM, time, current_bar;
            first = true;
            currentBPM = 0;
            time = offset;
            current_bar = 0;
            for (int i = 0; i < bpmsDictCount; i++)
            {
                var key = bpmsDictKeys[i];
                if (first)
                {
                    currentBPM = bpmsDictValues[i];
                    first = false;
                    continue;
                }
                if (key >= bar)
                    break;
                else
                {
                    time += barAmountToTime(key - current_bar, currentBPM);
                    current_bar = key;
                    currentBPM = bpmsDictValues[i];
                }

            }

            for (int i = 0; i < stopsDictCount; i++)
            {
                var key = stopsDictKeys[i];
                if (key < bar)
                {
                    time += stopsDictValues[i];
                }
            }

            time += barAmountToTime(bar - current_bar, currentBPM);
            return time;
        }

        //_btt variable ->bar to time, need to keep them outside in order to keep the previous points and avoid computing again
        //will have the side effect of reducing the amount of GC calls
        double _btt_currentBPM = 0, _btt_time = 0, _btt_current_bar = 99999;
        int _btt_i_bpmsDict = 0, _btt_i_stopsDict = 0;

        /// <summary>
        /// Bar to time has 3 duplicated version used at different moments in timeToBar to avoid going back to the beginning of the list of bpms/stops each time we need a convertion
        /// </summary>
        /// <param name="bar">bar to convert</param>
        /// <returns>converted time</returns>
        public double BarToTime(double bar)
        {
            if (bar < _btt_current_bar)//previous time calculated was after the one we want->reset
            {
                _btt_currentBPM = 0;
                _btt_time = offset;
                _btt_current_bar = 0;
                _btt_i_bpmsDict = 0;
                _btt_i_stopsDict = 0;
            }

            for (_btt_i_bpmsDict = _btt_i_bpmsDict; _btt_i_bpmsDict < bpmsDictCount; _btt_i_bpmsDict++)
            {
                var key = bpmsDictKeys[_btt_i_bpmsDict];
                if (_btt_i_bpmsDict == 0)
                {
                    _btt_currentBPM = bpmsDictValues[_btt_i_bpmsDict];
                    continue;
                }
                if (key >= bar)
                    break;
                else
                {
                    _btt_time += barAmountToTime(key - _btt_current_bar, _btt_currentBPM);
                    _btt_current_bar = key;
                    _btt_currentBPM = bpmsDictValues[_btt_i_bpmsDict];
                }

            }

            for (_btt_i_stopsDict = _btt_i_stopsDict; _btt_i_stopsDict < stopsDictCount; _btt_i_stopsDict++)
            {
                var key = stopsDictKeys[_btt_i_stopsDict];
                if (key < bar)
                {
                    _btt_time += stopsDictValues[_btt_i_stopsDict];
                }
                else
                    break;
            }

            _btt_time += barAmountToTime(bar - _btt_current_bar, _btt_currentBPM);
            _btt_current_bar = bar;
            return _btt_time;
        }

        //_btt variable ->bar to time, need to keep them outside in order to keep the previous points and avoid computing again
        //will have the side effect of reducing the amount of GC calls
        //Using a 2nd one with different values, copypasted the code because can't abstract it without ref keyword
        double _btt2_currentBPM = 0, _btt2_time = 0, _btt2_current_bar = 99999;
        int _btt2_i_bpmsDict = 0, _btt2_i_stopsDict = 0;

        /// <summary>
        /// Bar to time has 3 duplicated version used at different moments in timeToBar to avoid going back to the beginning of the list of bpms/stops each time we need a convertion
        /// </summary>
        /// <param name="bar">bar to convert</param>
        /// <returns>converted time</returns>
        public double BarToTime2(double bar)
        {
            if (bar < _btt2_current_bar)//previous time calculated was after the one we want->reset
            {
                _btt2_currentBPM = 0;
                _btt2_time = offset;
                _btt2_current_bar = 0;
                _btt2_i_bpmsDict = 0;
                _btt2_i_stopsDict = 0;
            }

            for (_btt2_i_bpmsDict = _btt2_i_bpmsDict; _btt2_i_bpmsDict < bpmsDictCount; _btt2_i_bpmsDict++)
            {
                var key = bpmsDictKeys[_btt2_i_bpmsDict];
                if (_btt2_i_bpmsDict == 0)
                {
                    _btt2_currentBPM = bpmsDictValues[_btt2_i_bpmsDict];
                    continue;
                }
                if (key >= bar)
                    break;
                else
                {
                    _btt2_time += barAmountToTime(key - _btt2_current_bar, _btt2_currentBPM);
                    _btt2_current_bar = key;
                    _btt2_currentBPM = bpmsDictValues[_btt2_i_bpmsDict];
                }

            }

            for (_btt2_i_stopsDict = _btt2_i_stopsDict; _btt2_i_stopsDict < stopsDictCount; _btt2_i_stopsDict++)
            {
                var key = stopsDictKeys[_btt2_i_stopsDict];
                if (key < bar)
                {
                    _btt2_time += stopsDictValues[_btt2_i_stopsDict];
                }
                else
                    break;
            }

            _btt2_time += barAmountToTime(bar - _btt2_current_bar, _btt2_currentBPM);
            _btt2_current_bar = bar;
            return _btt2_time;
        }

        //_btt variable ->bar to time, need to keep them outside in order to keep the previous points and avoid computing again
        //will have the side effect of reducing the amount of GC calls
        //Using a 3rd one with different values, copypasted the code because can't abstract it without ref keyword
        double _btt3_currentBPM = 0, _btt3_time = 0, _btt3_current_bar = 99999;
        int _btt3_i_bpmsDict = 0, _btt3_i_stopsDict = 0;

        /// <summary>
        /// Bar to time has 3 duplicated version used at different moments in timeToBar to avoid going back to the beginning of the list of bpms/stops each time we need a convertion
        /// </summary>
        /// <param name="bar">bar to convert</param>
        /// <returns>converted time</returns>
        public double BarToTime3(double bar)
        {
            if (bar < _btt3_current_bar)//previous time calculated was after the one we want->reset
            {
                _btt3_currentBPM = 0;
                _btt3_time = offset;
                _btt3_current_bar = 0;
                _btt3_i_bpmsDict = 0;
                _btt3_i_stopsDict = 0;
            }

            for (_btt3_i_bpmsDict = _btt3_i_bpmsDict; _btt3_i_bpmsDict < bpmsDictCount; _btt3_i_bpmsDict++)
            {
                var key = bpmsDictKeys[_btt3_i_bpmsDict];
                if (_btt3_i_bpmsDict == 0)
                {
                    _btt3_currentBPM = bpmsDictValues[_btt3_i_bpmsDict];
                    continue;
                }
                if (key >= bar)
                    break;
                else
                {
                    _btt3_time += barAmountToTime(key - _btt3_current_bar, _btt3_currentBPM);
                    _btt3_current_bar = key;
                    _btt3_currentBPM = bpmsDictValues[_btt3_i_bpmsDict];
                }

            }

            for (_btt3_i_stopsDict = _btt3_i_stopsDict; _btt3_i_stopsDict < stopsDictCount; _btt3_i_stopsDict++)
            {
                var key = stopsDictKeys[_btt3_i_stopsDict];
                if (key < bar)
                {
                    _btt3_time += stopsDictValues[_btt3_i_stopsDict];
                }
                else
                    break;
            }

            _btt3_time += barAmountToTime(bar - _btt3_current_bar, _btt3_currentBPM);
            _btt3_current_bar = bar;
            return _btt3_time;
        }

        public double barAmountToTime(double barAmount, double bpm)
        {
            return barAmount * 4 * 60.0 / bpm;
        }

        //Pseudo double dicts
        //Following code is incredibly dirty, but I don't want to make objects to manage and this is the best workaround I have for the absence of ref/out/in
        /// <summary>
        /// Pseudo dict init for bpms "dict", a function for each "dict" is needed due to lack of ref keyword
        /// </summary>
        void pseudoDictBpmsInit()
        {
            bpmsDictCount = 0;
            if (bpmsDictKeys == null || bpmsDictKeys.Length < DEFAULT_DICT_SIZE)
            {
                bpmsDictKeys = new double[DEFAULT_DICT_SIZE];
                bpmsDictValues = new double[DEFAULT_DICT_SIZE];
            }
        }

        /// <summary>
        /// Pseudo dict add for bpms "dict", a function for each "dict" is needed due to lack of ref keyword
        /// </summary>
        void pseudoDictBpmsAdd(double key, double value)
        {
            bpmsDictCount++;
            if (bpmsDictCount > bpmsDictKeys.Length)//need to expand size?
            {
                double[] tmp1 = new double[bpmsDictKeys.Length + DEFAULT_DICT_SIZE], tmp2 = new double[bpmsDictKeys.Length + DEFAULT_DICT_SIZE];
                for (int i = 0; i < bpmsDictKeys.Length; i++)
                {
                    tmp1[i] = bpmsDictKeys[i];
                    tmp2[i] = bpmsDictValues[i];
                }
                bpmsDictKeys = tmp1;
                bpmsDictValues = tmp2;
            }
            bpmsDictKeys[bpmsDictCount - 1] = key;
            bpmsDictValues[bpmsDictCount - 1] = value;
        }

        /// <summary>
        /// Pseudo dict init for stops "dict", a function for each "dict" is needed due to lack of ref keyword
        /// </summary>
        void pseudoDictStopsInit()
        {
            stopsDictCount = 0;
            if (stopsDictKeys == null || stopsDictKeys.Length < DEFAULT_DICT_SIZE)
            {
                stopsDictKeys = new double[DEFAULT_DICT_SIZE];
                stopsDictValues = new double[DEFAULT_DICT_SIZE];
            }
        }

        /// <summary>
        /// Pseudo dict add for stops "dict", a function for each "dict" is needed due to lack of ref keyword
        /// </summary>
        void pseudoDictStopsAdd(double key, double value)
        {
            stopsDictCount++;
            if (stopsDictCount > stopsDictKeys.Length)//need to expand size?
            {
                double[] tmp1 = new double[stopsDictKeys.Length + DEFAULT_DICT_SIZE], tmp2 = new double[stopsDictKeys.Length + DEFAULT_DICT_SIZE];
                for (int i = 0; i < stopsDictKeys.Length; i++)
                {
                    tmp1[i] = stopsDictKeys[i];
                    tmp2[i] = stopsDictValues[i];
                }
                stopsDictKeys = tmp1;
                stopsDictValues = tmp2;
            }
            stopsDictKeys[stopsDictCount - 1] = key;
            stopsDictValues[stopsDictCount - 1] = value;
        }

        //////Things that should have used ref
        void _pseudoDictDoubleInit(double[] dictKeys, double[] dictValues, int dictCount)
        {
            dictCount = 0;
            if (dictKeys == null || dictKeys.Length < DEFAULT_DICT_SIZE)
            {
                dictKeys = new double[DEFAULT_DICT_SIZE];
                dictValues = new double[DEFAULT_DICT_SIZE];
            }
        }
        void _pseudoDictDoubleAdd(double[] dictKeys, double[] dictValues, int dictCount, double key, double value)
        {
            dictCount++;
            if (dictCount > dictKeys.Length)//need to expand size?
            {
                double[] tmp1 = new double[dictKeys.Length + DEFAULT_DICT_SIZE], tmp2 = new double[dictKeys.Length + DEFAULT_DICT_SIZE];
                for (int i = 0; i < dictKeys.Length; i++)
                {
                    tmp1[i] = dictKeys[i];
                    tmp2[i] = dictValues[i];
                }
                dictKeys = tmp1;
                dictValues = tmp2;
            }
            dictKeys[dictCount - 1] = key;
            dictValues[dictCount - 1] = value;
        }
        /*public static AudioType AudioTypeFromFilename(string filename)
        {
            string[] splits = filename.Split('.');
            string extension = splits[splits.Length - 1];
            switch (extension.ToLower())
            {
                case "mp2":
                case "mp3":
                    return AudioType.MPEG;
                case "wav":
                    return AudioType.WAV;
                case "ogg":
                    return AudioType.OGGVORBIS;
            }
            return AudioType.UNKNOWN;
        }*/
    }

}