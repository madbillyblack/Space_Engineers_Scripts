using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Policy;
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
        const string ORE_TAG = "ORE";
        const string INGOT_TAG = "IGT";
        const string EMPTY_TAG = "SRT";
        const string WEP_TAG = "WEP";
        const string CMP_TAG = "CMP";
        const string TOOL_TAG = "TOOL";
        const string AMMO_TAG = "AMMO";

        public Dictionary<string, GSorter> _sorters;


        public class GSorter
        {
            public IMyConveyorSorter Sorter { get; set; }
            public List<SorterDisplay> Displays { get; set; }

            public string Tag { get; set; }

            public GSorter(IMyConveyorSorter sorter, string tag)
            { 
                Displays = new List<SorterDisplay>();
                Sorter = sorter;
                Tag = tag;
            }
        }



        // ADD SORTERS //
        public void AddSorters()
        {
            _sorters = new Dictionary<string, GSorter>();

            List<IMyConveyorSorter> sorters = new List<IMyConveyorSorter>();
            GridTerminalSystem.GetBlocksOfType<IMyConveyorSorter>(sorters);

            if (sorters.Count < 1) { return; }

            foreach (IMyConveyorSorter sorter in sorters)
            {
                string sorterTag = GetSorterTag(sorter.CustomName);
                if (string.IsNullOrEmpty(sorterTag)) { continue; }

                if (_sorters.ContainsKey(sorterTag))
                {
                    _statusMessage += "WARNING: Sorter Key already in use: " + sorterTag
                        + "\n* Block: " + sorter.CustomName;

                    continue;
                }

                _sorters.Add(sorterTag, new GSorter(sorter, sorterTag));
            }
        }


        // GET SORTER TAG //
        public string GetSorterTag(string input)
        {
            string sort = "[SRT_";
            int sortEnd = input.IndexOf(sort);

            if (sortEnd != -1)
            {
                string endstring = input.Substring(sortEnd + sort.Length);
                return endstring.Split(']')[0];
            }

            return "";
        }
    }
}
