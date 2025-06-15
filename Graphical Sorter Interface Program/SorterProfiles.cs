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
using VRage.Scripting;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public static class SorterProfiles
        {
            public static string OreList = " ore/iron\n"
                                        + " ore/nickel\n"
                                        + " ore/silicon\n"
                                        + " ore/cobalt\n"
                                        + " ore/magnesium\n"
                                        + " ore/silver\n"
                                        + " ore/gold\n"
                                        + " ore/uranium\n"
                                        + " ore/platinum\n"
                                        + " ice\n"
                                        + " stone\n"
                                        + " scrap\n"
                                        + " oldscrap";

            public static string IngotList = " ingot/iron\n"
                                        + " ingot/nickel\n"
                                        + " ingot/silicon\n"
                                        + " ingot/cobalt\n"
                                        + " ingot/magnesium\n"
                                        + " ingot/silver\n"
                                        + " ingot/gold\n"
                                        + " ingot/uranium\n"
                                        + " ingot/platinum\n"
                                        + " ptscrap\n"
                                        + " gravel";

            public static string ComponentList = " bpglass\n"
                                        + " computer\n"
                                        + " construction\n"
                                        + " detector\n"
                                        + " display\n"
                                        + " explosives\n"
                                        + " girder\n"
                                        + " gravgen\n"
                                        + " interiorplate\n"
                                        + " largetube\n"
                                        + " medical\n"
                                        + " metalgrid\n"
                                        + " motor\n"
                                        + " powercell\n"
                                        + " radio\n"
                                        + " reactor\n"
                                        + " smalltube\n"
                                        + " solarcell\n"
                                        + " steelplate\n"
                                        + " superconductor\n"
                                        + " thruster\n"
                                        + " canvas\n"
                                        + " engineerplushie\n"
                                        + " sabiroidplushie\n"
                                        + " ptcircuitry\n"
                                        + " ptcapacitor\n"
                                        + " ptcooling\n"
                                        + " ptframe\n"
                                        + " ptmachinery\n"
                                        + " ptpanel\n"
                                        + " ptpropulsion\n";

            public static string AmmoList = " gatlingammo\n"
                                        + " autocannonmag\n"
                                        + " assaultcannonshell\n"
                                        + " artilleryshell\n"
                                        + " srailgunsabot\n"
                                        + " lrailgunsabot\n"
                                        + " missile\n"
                                        + " fireworksred\n"
                                        + " fireworksyellow\n"
                                        + " fireworksgreen\n"
                                        + " fireworksblue\n"
                                        + " fireworkspink\n"
                                        + " fireworksrainbow";

            public static string WeaponList = " mr-20\n"
                                        + " mr-20mag\n"
                                        + " mr-50a\n"
                                        + " mr-50amag\n"
                                        + " mr-8p\n"
                                        + " mr-8pmag\n"
                                        + " mr-30e\n"
                                        + " mr-30emag\n"
                                        + " s-10\n"
                                        + " s-10mag\n"
                                        + " s-20a\n"
                                        + " s-20amag\n"
                                        + " s-10e\n"
                                        + " s-10emag\n"
                                        + " ro-1\n"
                                        + " pro-1\n"
                                        + " missile\n"
                                        + " flaregun\n"
                                        + " flareclip\n"
                                        + " 5.56x45mm";

            public static string ToolList = " anglegrinderitem\n"
                                        + " anglegrinder2item\n"
                                        + " anglegrinder3item\n"
                                        + " anglegrinder4item\n"
                                        + " welderitem\n"
                                        + " welder2item\n"
                                        + " welder3item\n"
                                        + " welder4item\n"
                                        + " handdrillitem\n"
                                        + " handdrill2item\n"
                                        + " handdrill3item\n"
                                        + " handdrill4item\n"
                                        + " hydrogenbottle\n"
                                        + " oxygenbottle";

            public static string MiscList = " medkit\n"
                                        + " powerkit\n"
                                        + " clangcola\n"
                                        + " cosmiccoffee\n"
                                        + " datapad\n"
                                        + " spacecredit\n"
                                        + " package\n"
                                        + " zonechip";

            public static readonly Dictionary<string, string[]> Lookup = new Dictionary<string, string[]>
            {
                // ORES
                {"ore/iron",new[]{"Ore/Iron","Fe","170,108,93"}},//"127,96,96"}},
                {"ore/nickel",new[]{"Ore/Nickel","Ni","145,144,124"}},
                {"ore/silicon",new[]{"Ore/Silicon","Si","87,88,83"}},
                {"ore/cobalt",new[]{"Ore/Cobalt","Co","96,194,255"}},//"73,162,194"
                {"ore/magnesium",new[]{"Ore/Magnesium","Mg","55,120,150"}},//"55,93,114"
                {"ore/silver",new[]{"Ore/Silver","Ag","201,199,176"}},
                {"ore/gold",new[]{"Ore/Gold","Au","194,175,96"}},
                {"ore/platinum",new[]{"Ore/Platinum","Pt","189,190,172"}},
                {"ore/uranium",new[]{"Ore/Uranium","U","73,66,60"}},
                {"ice",new[]{"Ore/Ice","Ice","75,110,130"}},
                {"stone",new[]{"Ore/Stone","Stone","127,127,127"}},
                {"scrap",new[]{"Ore/Scrap","Scrap","127,127,127"}},
                {"oldscrap",new[]{"Ingot/Scrap","OldScrap","127,96,72"}},
                {"organic",new[]{"Ore/Organic","Organic","127,164,88"}},

                // INGOTS
                {"ingot/iron",new[]{"Ingot/Iron","Fe","192,127,127"}},
                {"ingot/nickel",new[]{"Ingot/Nickel","Ni"}},
                {"ingot/silicon",new[]{"Ingot/Silicon","Si"}},
                {"ingot/cobalt",new[]{"Ingot/Cobalt","Co"}},
                {"ingot/magnesium",new[]{"Ingot/Magnesium","Mg"}},
                {"ingot/silver",new[]{"Ingot/Silver","Ag"}},
                {"ingot/gold",new[]{"Ingot/Gold","Au"}},
                {"ingot/platinum",new[]{"Ingot/Platinum","Pt"}},
                {"ingot/uranium",new[]{"Ingot/Uranium","U"}},
                {"gravel",new[]{"Ingot/Stone","Gravel"}},
                {"ptscrap",new[]{"Ingot/PrototechScrap","PT Scrap"}},

                // COMPONENTS
                {"bpglass",new[]{"Component/BulletproofGlass", "Glass"}},
                {"computer",new[]{"Component/Computer","CPU"}},
                {"construction",new[]{"Component/Construction","CstrComp"}},
                {"detector",new[]{"Component/Detector","Detect"}},
                {"display",new[]{"Component/Display","Display"}},
                {"explosives",new[]{"Component/Explosives","Explsv"}},
                {"girder",new[]{"Component/Girder","Girder"}},
                {"gravgen",new[]{"Component/GravityGenerator","Grav"}},
                {"interiorplate",new[]{"Component/InteriorPlate","Int.Plate"}},
                {"largetube",new[]{"Component/LargeTube","LTube"}},
                {"medical",new[]{"Component/Medical","Med."}},
                {"metalgrid",new[]{"Component/MetalGrid","Grid"}},
                {"motor",new[]{"Component/Motor","Motor"}},
                {"powercell",new[]{"Component/PowerCell","PwrCell"}},
                {"radio",new[]{"Component/RadioCommunication","Radio"}},
                {"reactor",new[]{"Component/Reactor","Reactor"}},
                {"smalltube",new[]{"Component/SmallTube","STube"}},
                {"solarcell",new[]{"Component/SolarCell","Solar"}},
                {"steelplate",new[]{"Component/SteelPlate","St.Plate"}},
                {"superconductor",new[]{"Component/Superconductor","SprCnd"}},
                {"thruster",new[]{"Component/Thrust","Thrust"}},
                {"canvas",new[]{"Component/Canvas","Canvas"}},
                {"engineerplushie",new[]{"Component/EngineerPlushie","Plush"}},
                {"sabiroidplushie",new[]{"Component/SabiroidPlushie","SPlush"}},
                {"ptcircuitry",new[]{"Component/PrototechCircuitry","pCircuit"}},
                {"ptcapacitor",new[]{"Component/PrototechCapacitor","pCpctr"}},
                {"ptcooling",new[]{"Component/PrototechCoolingUnit","pCooling"}},
                {"ptframe",new[]{"Component/PrototechFrame","pFrame"}},
                {"ptmachinery",new[]{"Component/PrototechMachinery","pMach."}},
                {"ptpanel",new[]{"Component/PrototechPanel","pPanel"}},
                {"ptpropulsion",new[]{"Component/PrototechPropulsionUnit","pProp."}},

                // SHIP AMMO
                {"gatlingammo",new[]{"AmmoMagazine/NATO_25x184mm","25mm"}},
                {"autocannonmag",new[]{"AmmoMagazine/AutocannonClip","40mm"}},
                {"assaultcannonshell",new[]{"AmmoMagazine/MediumCalibreAmmo","55mm"}},
                {"artilleryshell",new[]{"AmmoMagazine/LargeCalibreAmmo","125mm"}},
                {"srailgunsabot",new[]{"AmmoMagazine/SmallRailgunAmmo","smRail"}},
                {"lrailgunsabot",new[]{"AmmoMagazine/LargeRailgunAmmo","lgRail"}},
                {"missile",new[]{"AmmoMagazine/Missile200mm","Missile","96,96,0"}},
                {"fireworksred",new[]{"AmmoMagazine/FireworksBoxRed","Fw:Red","255,64,64"}},
                {"fireworksyellow",new[]{"AmmoMagazine/FireworksBoxYellow","FwYellow","255,255,64"}},
                {"fireworksgreen",new[]{"AmmoMagazine/FireworksBoxGreen","Fw:Green","64,127,64"}},
                {"fireworksblue",new[]{"AmmoMagazine/FireworksBoxBlue","Fw:Blue","64,64,255"}},
                {"fireworkspink",new[]{"AmmoMagazine/FireworksBoxPink","Fw:Pink","255,96,127"}},
                {"fireworksrainbow",new[]{"AmmoMagazine/FireworksBoxRainbow","Fw:Multi","192,144,255"}},

                // PERSONAL AMMO
                {"5.56x45mm",new[]{"AmmoMagazine/NATO_5p56x45mm","5.56x45mm","84,84,84"}},
                {"mr-20mag",new[]{"AmmoMagazine/AutomaticRifleGun_Mag_20rd","mr-20","84,84,84"}},
                {"mr-50amag",new[]{"AmmoMagazine/RapidFireAutomaticRifleGun_Mag_50rd","mr-50a","138,137,107"}},
                {"mr-8pmag",new[]{"AmmoMagazine/PreciseAutomaticRifleGun_Mag_5rd","mr-8p","72,84,96"}},
                {"mr-30emag",new[]{"AmmoMagazine/UltimateAutomaticRifleGun_Mag_30rd","mr-30e","137,136,106"}},
                {"s-10mag",new[]{"AmmoMagazine/SemiAutoPistolMagazine","s-10","84,84,84"}},
                {"s-20amag",new[]{"AmmoMagazine/FullAutoPistolMagazine","s-20a","96,84,84"}},
                {"s-10emag",new[]{"AmmoMagazine/ElitePistolMagazine","s-10e","96,96,84"}},
                {"flareclip",new[]{"AmmoMagazine/FlareClip","flares","176,80,56"}},

                // WEAPONS
                {"mr-20",new[]{"PhysicalGunObject/AutomaticRifleItem","mr-20","84,84,84"}},
                {"mr-50a",new[]{"PhysicalGunObject/RapidFireAutomaticRifleItem","mr-50","138,137,107"}},
                {"mr-8p",new[]{"PhysicalGunObject/PreciseAutomaticRifleItem","mr-8p","72,84,96"}},
                {"mr-30e",new[]{"PhysicalGunObject/UltimateAutomaticRifleItem","mr-30e","137,136,106"}},
                {"s-10",new[]{"PhysicalGunObject/SemiAutoPistolItem","s-10","84,84,84"}},
                {"s-20a",new[]{"PhysicalGunObject/FullAutoPistolItem","s-20a","96,84,84"}},
                {"s-10e",new[]{"PhysicalGunObject/ElitePistolItem","s-10e","96,96,84"}},
                {"ro-1",new[]{"PhysicalGunObject/BasicHandHeldLauncherItem","ro-1","112,110,120"}},
                {"pro-1",new[]{"PhysicalGunObject/AdvancedHandHeldLauncherItem","pro-1","118,121,102"}},
                {"flaregun",new[]{"PhysicalGunObject/FlareGunItem","flaregun","176,80,56"}},

                // TOOLS
                {"anglegrinderitem",new[]{"PhysicalGunObject/AngleGrinderItem",""}},
                {"anglegrinder2item",new[]{"PhysicalGunObject/AngleGrinder2Item",""}},
                {"anglegrinder3item",new[]{"PhysicalGunObject/AngleGrinder3Item",""}},
                {"anglegrinder4item",new[]{"PhysicalGunObject/AngleGrinder4Item",""}},
                {"welderitem",new[]{"PhysicalGunObject/WelderItem",""}},
                {"welder2item",new[]{"PhysicalGunObject/Welder2Item",""}},
                {"welder3item",new[]{"PhysicalGunObject/Welder3Item",""}},
                {"welder4item",new[]{"PhysicalGunObject/Welder4Item",""}},
                {"handdrillitem",new[]{"PhysicalGunObject/HandDrillItem",""}},
                {"handdrill2item",new[]{"PhysicalGunObject/HandDrill2Item",""}},
                {"handdrill3item",new[]{"PhysicalGunObject/HandDrill3Item",""}},
                {"handdrill4item",new[]{"PhysicalGunObject/HandDrill4Item",""}},
                {"hydrogenbottle",new[]{"GasContainerObject/HydrogenBottle",""}},
                {"oxygenbottle",new[]{"OxygenContainerObject/OxygenBottle",""}},

                // MISC - TODO
                {"medkit",new[]{"ConsumableItem/Medkit","Medkit"}},
                {"powerkit",new[]{"ConsumableItem/Powerkit","PwrKit"}},
                {"clangcola",new[]{"ConsumableItem/ClangCola","CLANG!"}},
                {"cosmiccoffee",new[]{"ConsumableItem/CosmicCoffee","Coffee"}},
                {"datapad",new[]{"Datapad/Datapad","DataPad"}},
                {"spacecredit",new[]{"PhysicalObject/SpaceCredit","Credits"}},
                {"package",new[]{"Package/Package","Package"}},
                {"zonechip",new[]{"Component/ZoneChip","ZoneChip"}}
            };

            public static string[] LookupItem(string item)
            {
                if (Lookup.ContainsKey(item))
                {
                    string [] arr = Lookup[item];
                    string last;

                    if (arr.Length < 3)
                        last = "";
                    else
                        last = arr[2];

                    return new string[] {"MyObjectBuilder_" + arr[0], arr[1], last};
                }

                return new string []{ item, "", ""};
            }
        }
    }
}
