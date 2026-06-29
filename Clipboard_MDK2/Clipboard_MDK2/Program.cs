using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
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
    public partial class Program : MyGridProgram
    {
        #region MDK2 INFO
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.
        #endregion

        #region Keep these variables
        static IMyTextSurface _surface;
        //MyIni _ini;
        static IMyProgrammableBlock _Me;
        #endregion

        #region Test Variables (Can be deleted)
        List<DestInventory> _magazines;

        const string INI_HEAD = "USAP";
        const string MAG_TAG = "[MAG";
        const string AMMO_SUPPLY = "[WEP]"; // Source inventory tag for rearming.
        const string LOADOUT = "Loadout";
        const string GATLING = "GATLING";
        const string MISSILE = "MISSILE";
        const string ARTILLERY = "ARTILLERY";
        const string ASSAULT = "ASSAULT";
        const string AUTO = "AUTO";
        const string RAIL = "RAIL";
        const string MINI_RAIL = "MINI-RAIL";

        #endregion

        public Program()
        {
            // Set static instance of this program block
            _Me = Me;

            #region Data Screen Init
            //_ini = GetIni(Me);
            _surface = Me.GetSurface(0);
            _surface.ContentType = ContentType.TEXT_AND_IMAGE;



            #endregion

            //List<IMyProjector> projectors = new List<IMyProjector>();
            //GridTerminalSystem.GetBlocksOfType<IMyProjector>(projectors);



            Build();


            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (!string.IsNullOrEmpty(argument))
            {
                _surface.WriteText(updateSource.ToString() + "\nCMD: " + argument + "\n");

                switch (argument.ToUpper())
                {
                    case "REFRESH":
                            Build();
                            break;
                    case "REARM":
                        ReloadAmmo();
                        break;
                    case "DISARM":
                        ReloadAmmo(true);
                        break;
                    case "SHOW_COUNTS":
                        ShowCounts();
                        break;
                    default:
                        _surface.WriteText("UNRECOGNIZED COMMAND", true);
                        break;
                }
            }
        }


        public void Build()
        {
            //_surface.WriteText("");

            //_programIni = GetIni(Me);
            _programIniHandler = new MyIniHandler(Me);
            _gridID = _programIniHandler.GetKey(SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());

            AddMagazines();
        }

        public void ShowCounts()
        {
            foreach(DestInventory mag in _magazines)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                mag.Inventory.GetItems(items);

                _surface.WriteText(mag.Block.CustomName + "  \n Items: " + items.Count + "\n", true);
            }
        }


        public void AddMagazines()
        {
            _magazines = new List<DestInventory>();
            List<IMyTerminalBlock> mags = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(MAG_TAG, mags);

            if (mags.Count < 1) { return; }

            foreach(IMyTerminalBlock mag in mags)
            {
                SetMagAmounts(mag);

                if (SameGridID(mag))
                {
                    DestInventory magazine = new DestInventory(DestType.MAGAZINE, mag);
                    magazine.SetLoadout();

                    _magazines.Add(magazine);
                }
            }
        }


        // SET MAG AMMOUNTS //
        public void SetMagAmounts(IMyTerminalBlock block)
        {
            string name = block.CustomName;
            if ((!name.Contains(MAG_TAG + "]") && !name.Contains(":")) || GetKey(block, SHARED, "Grid_ID", _gridID) != _gridID)
                return;

            string tag = TagFromName(name);
            string loadout = "";

            switch (tag.ToUpper())
            {
                case "GENERAL":
                    loadout = "NATO_25x184mm:1\n" +
                              "Missile200mm:1\n" +
                              "LargeCalibreAmmo:1\n" +
                              "MediumCalibreAmmo:1\n" +
                              "AutocannonClip:1\n" +
                              "LargeRailgunAmmo:1\n" +
                              "SmallRailgunAmmo:1";
                    break;
                case GATLING:
                    loadout = "NATO_25x184mm:7";
                    break;
                case MISSILE:
                    loadout = "Missile200mm:4";
                    break;
                case ARTILLERY:
                    loadout = "LargeCalibreAmmo:3";
                    break;
                case ASSAULT:
                    loadout = "MediumCalibreAmmo:2";
                    break;
                case AUTO:
                    loadout = "AutocannonClip:5";
                    break;
                case RAIL:
                    loadout = "LargeRailgunAmmo:1";
                    break;
                case MINI_RAIL:
                    loadout = "SmallRailgunAmmo:6";
                    break;
            }

            EnsureKey(block, INI_HEAD, LOADOUT, loadout);
        }

        // TAG FROM NAME //  Gets specific tag from MAG name.
        public string TagFromName(string name)
        {
            string tag = "";
            if (name.Contains(MAG_TAG + "]"))
                return "GENERAL";

            int start = name.IndexOf(MAG_TAG) + MAG_TAG.Length + 1; //Start index of tag substring - includes colon
            Echo("Start: " + start);
            tag = name.Substring(start);
            Echo(tag);
            int length = tag.IndexOf("]");
            Echo("Length: " + length);
            tag = tag.Substring(0, length);//Length of tag
            Echo(tag);

            return tag;
        }


        public void ReloadAmmo(bool unload = false)
        {
            if(_magazines.Count < 1)
            {
                _surface.WriteText("No Magazines to reload\n", true);
            }

            List<IMyInventory> sources = GetSourceInventories(AMMO_SUPPLY);
            if(sources.Count < 1)
            {
                _surface.WriteText("No source inventories located\n", true);
                return;
            }

            foreach (DestInventory mag in _magazines)
            {
                if (unload)
                {
                    if (mag.Unload(sources))
                        _surface.WriteText("UNLOADED: " + mag.Block.CustomName + "\n", true);
                    else
                        _surface.WriteText("UNLOAD ERROR: " + mag.Block.CustomName + "\n", true);
                }
                else
                {
                    if (mag.Refill(sources))
                        _surface.WriteText("RELOADED: " + mag.Block.CustomName + "\n", true);
                    else
                        _surface.WriteText("RELOAD ERROR: " + mag.Block.CustomName + "\n", true);
                }
            }
        }



        public List<IMyInventory> GetSourceInventories(string tag)
        {
            List<IMyInventory> sources = new List<IMyInventory>();
            List<IMyTerminalBlock> sourceBlocks = new List<IMyTerminalBlock>();

            GridTerminalSystem.SearchBlocksOfName(AMMO_SUPPLY, sourceBlocks);

            if (sourceBlocks.Count < 1) { return sources; }

            foreach (IMyTerminalBlock block in sourceBlocks)
            {
                if(block.HasInventory)
                    sources.Add(block.GetInventory());
            }

            return sources;
        }
    }
}
