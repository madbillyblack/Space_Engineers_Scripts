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
                _statusMessage += "NO PROFILE NAME SPECIFIED! Check command and try again!";
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
                    _statusMessage += "More than one inventory of name \"" + inventoryName + "\" found!";
                    return;
                }
                else if(cargoBlocks.Count == 1 && cargoBlocks[0].HasInventory)
                {
                    profile = ProfileFromInventory(cargoBlocks[0].GetInventory(0));
                }
                else
                {
                    _statusMessage += "No inventory of name \"" + inventoryName + "\" found!";
                    return;
                }
            }
            else
            {
                profile = DEFAULT_PROFILE;
            }

            SetKey(Me, profileHeader, LOADOUT, profile);
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
                            bpGlass++;
                            break;
                        case "Computer":
                            computer++;
                            break;
                        case "Construction":
                            construction++;
                            break;
                        case "Detector":
                            detector++;
                            break;
                        case "Display":
                            display++;
                            break;
                        case "Explosives":
                            explosives++;
                            break;
                        case "Girder":
                            girder++;
                            break;
                        case "GravityGenerator":
                            gravGen++;
                            break;
                        case "InteriorPlate":
                            interiorPlate++;
                            break;
                        case "LargeTube":
                            lgTube++;
                            break;
                        case "Medical":
                            medical++;
                            break;
                        case "MetalGrid":
                            metalGrid++;
                            break;
                        case "Motor":
                            motor++;
                            break;
                        case "PowerCell":
                            powerCell++;
                            break;
                        case "RadioCommunication":
                            radio++;
                            break;
                        case "Reactor":
                            reactor++;
                            break;
                        case "SmallTube":
                            smTube++;
                            break;
                        case "SolarCell":
                            solar++;
                            break;
                        case "SteelPlate":
                            steelPlate++;
                            break;
                        case "Superconductor":
                            superconductor++;
                            break;
                        case "Thrust":
                            thruster++;
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
    }
}
