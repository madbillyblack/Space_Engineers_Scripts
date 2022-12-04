using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        const string SHARED = "Shared Data";
        const char SEPARATOR = ';';
        static string _gridID;
        const string GRID_KEY = "Grid_ID";

        // ENSURE KEY // Check to see if INI key exists, and if it doesn't write with default value.
        static void EnsureKey(IMyTerminalBlock block, string header, string key, string defaultVal)
        {
            //if (!block.CustomData.Contains(header) || !block.CustomData.Contains(key))
            MyIni ini = GetIni(block);
            if (!ini.ContainsKey(header, key))
                SetKey(block, header, key, defaultVal);
        }


        // GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
        static string GetKey(IMyTerminalBlock block, string header, string key, string defaultVal)
        {
            EnsureKey(block, header, key, defaultVal);
            MyIni blockIni = GetIni(block);
            return blockIni.Get(header, key).ToString();
        }


        // SET KEY // Update ini key for block, and write back to custom data.
        static void SetKey(IMyTerminalBlock block, string header, string key, string arg)
        {
            MyIni blockIni = GetIni(block);
            blockIni.Set(header, key, arg);
            block.CustomData = blockIni.ToString();
        }


        // GET INI // Get entire INI object from specified block.
        static MyIni GetIni(IMyTerminalBlock block)
        {
            MyIni iniOuti = new MyIni();

            MyIniParseResult result;
            if (!iniOuti.TryParse(block.CustomData, out result))
            {
                block.CustomData = "---\n" + block.CustomData;
                if (!iniOuti.TryParse(block.CustomData, out result))
                    throw new Exception(result.ToString());
            }

            return iniOuti;
        }


        // SET GRID ID // Updates Grid ID parameter for all designated blocks in Grid, then rebuilds the grid.
        void SetGridID(string arg)
        {
            string gridID;
            if (arg != "" && arg != "0")
                gridID = arg;
            else
                gridID = Me.CubeGrid.EntityId.ToString();

            SetKey(Me, SHARED, "Grid_ID", gridID);
            _gridID = gridID;

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

            foreach (IMyTerminalBlock block in blocks)
            {
                if (block.IsSameConstructAs(Me) && block.CustomData.Contains(SHARED))
                    SetKey(block, SHARED, "Grid_ID", gridID);
            }

            Build();
        }


        // INSERT ENTRY //
        public string InsertEntry(string entry, string oldString, char separator, int index, string placeHolder)
        {
            List<string> entries = StringToEntries(oldString, separator);
            
            if(index == entries.Count)
            {
                entries.Add(entry);
            }
            else if (index > entries.Count)
            {
                while (index > entries.Count)
                    entries.Add(placeHolder);

                entries.Add(entry);
            }
            else
            {
                //Insert entry into the old string.
                entries[index] = entry;
            }


            string newString = entries[0];

            if(entries.Count > 1)
            {
                for (int n = 1; n < entries.Count; n++)
                {
                    newString += separator + entries[n];
                }
            }


            return newString;
        }

        public string InsertEntry(string entry, string oldString, int index, int length, string placeHolder)
        {
            string newString;

            List<string> entries = StringToEntries(oldString, length, placeHolder);

            // If there's only one entry in the string return entry.
            if (entries.Count == 1 && length == 0)
            {
                return entry;
            }

            //Insert entry into the old string.
            entries[index] = entry;

            newString = entries[0];
            for (int n = 1; n < entries.Count; n++)
            {
                newString += SEPARATOR + entries[n];
            }

            return newString;
        }


        // STRING TO ENTRIES //		Splits string into a list of variable length, by a separator character.  If the list is shorter than 
        //		the desired length,the remainder is filled with copies of the place holder.
        public List<string> StringToEntries(string arg, int length, string placeHolder)
        {
            List<string> entries = new List<string>();
            string[] args = arg.Split(SEPARATOR);

            foreach (string argument in args)
            {
                entries.Add(argument);
            }

            while (entries.Count < length)
            {
                entries.Add(placeHolder);
            }

            return entries;
        }

        public List<string> StringToEntries(string arg, char separator)
        {
            List<string> entries = new List<string>();
            string[] args = arg.Split(separator);

            foreach (string argument in args)
            {
                entries.Add(argument);
            }

            return entries;
        }


        // On Grid //
        static bool OnGrid(IMyTerminalBlock block)
        {
            return GetKey(block, SHARED, GRID_KEY, _gridID) == _gridID;
        }
    }
}
