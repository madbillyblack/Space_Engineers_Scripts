﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
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
        const string WEP_TAG = "WEP";
        const string CMP_TAG = "CMP";
        const string TOOL_TAG = "TOOL";
        const string AMMO_TAG = "AMMO";
        const string MSC_TAG = "MISC";
        const string LIST_KEY = "FilterList";

        const string DF_BG = "0,88,151";
        const string DF_TITLE = "192,192,0";
        const string DF_LABEL = "179,237,255";

        public static Color _defBgColor;
        public static Color _defButtonColor;
        public static Dictionary<string, GSorter> _sorters;

        public class GSorter
        {
            public IMyConveyorSorter SorterBlock { get; set; }
            public string Tag { get; set; }
            public string[] Filters { get; set; }
            public MyIniHandler IniHandler { get; set; }
            public Color BackgroundColor { get; set; }
            public Color TitleColor { get; set; }
            public Color LabelColor { get; set; }
            public Color ButtonColor { get; set; }

            public GSorter(IMyConveyorSorter sorter, string tag)
            { 
                SorterBlock = sorter;
                Tag = tag;

                IniHandler = new MyIniHandler(sorter);

                AddFilters();
                InitColors();
            }

            public int ActiveFilterCount()
            {
                if(Filters.Length < 1) return 0;

                int count = 0;
                List<MyInventoryItemFilter> filters = new List<MyInventoryItemFilter>();
                SorterBlock.GetFilterList(filters);

                foreach (string filter in Filters)
                {
                    if(IsFilterActive(SorterProfiles.LookupItem(filter)[0], filters))
                        count++;
                }

                return count;
            }

            private void AddFilters()
            {
                string filterList;

                if (!IniHandler.HasKey(MAIN_HEADER, LIST_KEY))
                {
                    filterList = GetDefaultList(Tag);
                    IniHandler.SetKey(MAIN_HEADER, LIST_KEY, filterList);
                }
                else
                {
                    filterList = IniHandler.GetKey(MAIN_HEADER, LIST_KEY, "");
                }

                Filters = filterList.Split('\n');

                if (Filters.Length < 1) return;

                // Trim entries of leading white space
                for(int i = 0; i < Filters.Length; i++)
                {
                    Filters[i] = Filters[i].Trim();
                }
            }

            void InitColors()
            {
                string cHeader = MAIN_HEADER + " Colors";

                BackgroundColor = ParseColor(IniHandler.GetKey(cHeader, "BackgroundColor", GetBgColor()));
                ButtonColor = BackgroundColor * 1.12f;
                TitleColor = ParseColor(IniHandler.GetKey(cHeader, "TitleColor", DF_TITLE));
                LabelColor = ParseColor(IniHandler.GetKey(cHeader, "LabelColor", DF_LABEL));
            }

            string GetBgColor()
            {
                string sortType = Tag.Split('_')[0].ToUpper();

                if (SorterProfiles.Colors.ContainsKey(sortType))
                    return SorterProfiles.Colors[sortType];
                else
                    return DF_BG;
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

                if (!SameGridID(sorter)) { continue; }

                if (_sorters.ContainsKey(sorterTag))
                {
                    _logger.LogWarning("Sorter Key already in use: " + sorterTag
                        + "\n* Block: " + sorter.CustomName);

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


        // GET DEFAULT LIST
        public static string GetDefaultList(string tag)
        {
            string type = tag.Split('_')[0].Trim();

            switch(type.ToUpper())
            {
                case ORE_TAG:
                    return SorterProfiles.OreList;
                case INGOT_TAG:
                    return SorterProfiles.IngotList;
                case CMP_TAG:
                    return SorterProfiles.ComponentList;
                case AMMO_TAG:
                    return SorterProfiles.AmmoList;
                case WEP_TAG:
                    return SorterProfiles.WeaponList;
                case TOOL_TAG:
                    return SorterProfiles.ToolList;
                case MSC_TAG:
                    return SorterProfiles.MiscList;
            }

            return "";
        }
    }
}
