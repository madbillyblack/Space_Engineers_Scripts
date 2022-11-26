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
        // NEW PROFILE // - Creates profile with specified name.  If block name is also included, the profile will be based on the current inventory of that block.
        void NewProfile(string argument)
        {
            if(string.IsNullOrEmpty(argument))
            {
                Echo("NO PROFILE NAME SPECIFIED! Check command and try again!");
            }

            string[] args = argument.Split(' ');
            string profileName = args[0];
            string profileHeader = "Profile: " + profileName;
            string inventoryName = "";
            string profile;

            // If an inventory name is supplied in the argument, assemble it from the remaining array entries.
            if(args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                    inventoryName += args[i] + " ";

                List<IMyTerminalBlock> cargoBlocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName(inventoryName.Trim(), cargoBlocks);

                if(cargoBlocks.Count > 1)
                {
                    Echo("More than one inventory of name \"" + inventoryName + "\" found!");
                    return;
                }
                else if(cargoBlocks.Count == 1 && cargoBlocks[0].HasInventory)
                {
                    IMyTerminalBlock block = cargoBlocks[0];
                    IMyInventory inventory = block.GetInventory(0);

                    Echo("PROTOTYPE INVENTORY:\n* " + block.CustomName + "\n* Vol: " + (inventory.MaxVolume * 1000).ToString() + "L");
                    profile = ProfileFromInventory(inventory);
                }
                else
                {
                    Echo("No inventory of name \"" + inventoryName + "\" found!");
                    return;
                }
            }
            else
            {
                profile = DEFAULT_PROFILE;
            }

            SetKey(Me, profileHeader, LOADOUT, profile);
            AddProfileToList(profileName);
            SelectProfile(profileName);
        }


        // PROFILE FROM INVENTORY //
        string ProfileFromInventory(IMyInventory inventory)
        {
            string output = "";

            float ratio = 15.625f / (float) inventory.MaxVolume;

            int bpGlass, computer, construction, detector, display, explosives, girder, gravGen, interiorPlate, lgTube,
                medical, metalGrid, motor, powerCell, radio, reactor, smTube, solar, steelPlate, superconductor, thruster;

            bpGlass = computer = construction = detector = display = explosives = girder = gravGen = interiorPlate = lgTube = 0;
            medical = metalGrid = motor = powerCell = radio = reactor = smTube = solar = steelPlate = superconductor = thruster = 0;


            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inventory.GetItems(items);

            if(items.Count > 0)
            {
                foreach(MyInventoryItem item in items)
                {
                    string type = item.Type.SubtypeId.ToString();

                    switch(type)
                    {
                        case "BulletproofGlass":
                            bpGlass = item.Amount.ToIntSafe();
                            break;
                        case "Computer":
                            computer = item.Amount.ToIntSafe();
                            break;
                        case "Construction":
                            construction = item.Amount.ToIntSafe();
                            break;
                        case "Detector":
                            detector = item.Amount.ToIntSafe();
                            break;
                        case "Display":
                            display = item.Amount.ToIntSafe();
                            break;
                        case "Explosives":
                            explosives = item.Amount.ToIntSafe();
                            break;
                        case "Girder":
                            girder = item.Amount.ToIntSafe();
                            break;
                        case "GravityGenerator":
                            gravGen = item.Amount.ToIntSafe();
                            break;
                        case "InteriorPlate":
                            interiorPlate = item.Amount.ToIntSafe();
                            break;
                        case "LargeTube":
                            lgTube = item.Amount.ToIntSafe();
                            break;
                        case "Medical":
                            medical = item.Amount.ToIntSafe();
                            break;
                        case "MetalGrid":
                            metalGrid = item.Amount.ToIntSafe();
                            break;
                        case "Motor":
                            motor = item.Amount.ToIntSafe();
                            break;
                        case "PowerCell":
                            powerCell = item.Amount.ToIntSafe();
                            break;
                        case "RadioCommunication":
                            radio = item.Amount.ToIntSafe();
                            break;
                        case "Reactor":
                            reactor = item.Amount.ToIntSafe();
                            break;
                        case "SmallTube":
                            smTube = item.Amount.ToIntSafe();
                            break;
                        case "SolarCell":
                            solar = item.Amount.ToIntSafe();
                            break;
                        case "SteelPlate":
                            steelPlate = item.Amount.ToIntSafe();
                            break;
                        case "Superconductor":
                            superconductor = item.Amount.ToIntSafe();
                            break;
                        case "Thrust":
                            thruster = item.Amount.ToIntSafe();
                            break;
                    }
                }
            }

            output = "BulletproofGlass:"+ (int)(bpGlass * ratio) + "\n" +
                            "Computer:"+ (int)(computer * ratio) + "\n" +
                    "Construction:"+ (int)(construction * ratio) + "\n" +
                            "Detector:"+ (int)(detector * ratio) + "\n" +
                              "Display:"+ (int)(display * ratio) + "\n" +
                        "Explosives:"+ (int)(explosives * ratio) + "\n" +
                                "Girder:"+ (int)(girder * ratio) + "\n" +
                     "GravityGenerator:"+ (int)(gravGen * ratio) + "\n" +
                  "InteriorPlate:"+ (int)(interiorPlate * ratio) + "\n" +
                             "LargeTube:"+ (int)(lgTube * ratio) + "\n" +
                              "Medical:"+ (int)(medical * ratio) + "\n" +
                          "MetalGrid:"+ (int)(metalGrid * ratio) + "\n" +
                                  "Motor:"+ (int)(motor * ratio) + "\n" +
                          "PowerCell:"+ (int)(powerCell * ratio) + "\n" +
                     "RadioCommunication:"+ (int)(radio * ratio) + "\n" +
                              "Reactor:"+ (int)(reactor * ratio) + "\n" +
                             "SmallTube:"+ (int)(smTube * ratio) + "\n" +
                              "SolarCell:"+ (int)(solar * ratio) + "\n" +
                        "SteelPlate:"+ (int)(steelPlate * ratio) + "\n" +
                "Superconductor:"+ (int)(superconductor * ratio) + "\n" +
                             "Thrust:" + (int)(thruster * ratio);

            return output;
        }


        // ADD PROFILE TO LIST //
        void AddProfileToList(string profileName)
        {
            string profileList = GetKey(Me, INI_HEAD, "Profiles", PROFILE_LIST);
            SetKey(Me, INI_HEAD, "Profiles", profileName + "," + profileList);
        }
    }
}
