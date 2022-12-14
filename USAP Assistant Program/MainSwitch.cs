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
        void MainSwitch(string argument)
        {
            if (!string.IsNullOrEmpty(argument))
            {
                Echo("CMD: " + argument);

                string[] args = argument.Split(' ');
                string arg = args[0].ToUpper();

                string cmdArg = "";
                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        cmdArg += args[i] + " ";
                    }

                    cmdArg = cmdArg.Trim();
                }

                switch (arg)
                {
                    case "REFRESH":
                        Build();
                        break;
                    case "UNLOAD":
                        Unstock(_miningCargos, ORE_DEST, false);
                        Unstock(_constructionCargos, COMP_SUPPLY, true);
                        break;
                    case "RELOAD":
                        Restock(_magazines, AMMO_SUPPLY);
                        Restock(_reactors, FUEL_SUPPLY);
                        Restock(_o2Generators, ICE_SUPPLY);
                        break;
                    case "REFUEL":
                        Restock(_reactors, FUEL_SUPPLY);
                        Restock(_o2Generators, ICE_SUPPLY);
                        break;
                    case "RESUPPLY":
                        Unstock(_constructionCargos, COMP_SUPPLY, true);
                        Restock(_constructionCargos, COMP_SUPPLY);
                        break;
                    case "CRUISE_THRUSTERS_ON":
                        CruiseThrustersOn();
                        break;
                    case "CRUISE_THRUSTERS_OFF":
                        CruiseThrustersOff();
                        break;
                    case "TOGGLE_CRUISE_THRUSTERS":
                        ToggleCruiseThrusters();
                        break;
                    case "SELECT_PROFILE":
                        SelectProfile(cmdArg);
                        break;
                    case "UPDATE_PROFILES":
                        UpdateProfiles();
                        break;
                    case "NEW_PROFILE":
                        NewProfile(cmdArg);
                        break;
                    case "SET_GRID_ID":
                        SetGridID(cmdArg);
                        break;
                    case "ADD_PREFIX":
                        AddTags(cmdArg, true);
                        break;
                    case "ADD_SUFFIX":
                        AddTags(cmdArg, false);
                        break;
                    case "DELETE_PREFIX":
                        RemoveTags(cmdArg, true);
                        break;
                    case "DELETE_SUFFIX":
                        RemoveTags(cmdArg, false);
                        break;
                    case "REPLACE_PREFIX":
                        ReplaceTags(args, true);
                        break;
                    case "REPLACE_SUFFIX":
                        ReplaceTags(args, false);
                        break;
                    case "SWAP_TO_PREFIX":
                        SwapTags(cmdArg, true);
                        break;
                    case "SWAP_TO_SUFFIX":
                        SwapTags(cmdArg, false);
                        break;
                    case "SET_LOAD_COUNT":
                        SetLoadCount(cmdArg);
                        break;
                    case "RESET_LOAD_COUNT":
                        SetLoadCount("0");
                        break;
                    case "TOGGLE_GEAR":
                        if (_landingGear != null)
                            _landingGear.Toggle();
                        break;
                    case "GEAR_DOWN":
                        if (_landingGear != null)
                            _landingGear.Extend();
                        break;
                    case "GEAR_UP":
                        if (_landingGear != null)
                            _landingGear.Retract();
                        break;
                    case "GEAR_TIMER": // Lets timer cycle gear based on corrent state
                        if (_landingGear != null)
                            _landingGear.TimerCall();
                        break;
                    case "TIMER_LOCK": // Basic timer call that ends gear movement
                        if (_landingGear != null)
                            _landingGear.TimerLock();
                        break;
                    case "SWAP_GEAR_DIRECTION":
                    case "SWAP_GEAR_DIRECTIONS":
                        if (_landingGear != null)
                            _landingGear.SwapDirections();
                        break;
                    case "ON_RETRACT":
                        SetRetractBehavior(cmdArg);
                        break;
                    case "ON_EXTEND":
                        SetExtendBehavior(cmdArg);
                        break;
                    case "CLEAR_GEAR_DATA":
                        if (_landingGear != null)
                            _landingGear.ClearData();
                        break;
                    case "LOCK":
                        if (_landingGear != null)
                            _landingGear.Lock();
                        break;
                    case "UNLOCK":
                        if (_landingGear != null)
                            _landingGear.Unlock();
                        break;
                    case "SWITCH_LOCK":
                        if (_landingGear != null)
                            _landingGear.SwitchLock();
                        break;
                    case "THROTTLE_UP":
                        ThrottleUp(cmdArg);
                        break;
                    case "THROTTLE_DOWN":
                        ThrottleDown(cmdArg);
                        break;
                    case "BUTTON_1":
                        PressButton(cmdArg, 1);
                        break;
                    case "BUTTON_2":
                        PressButton(cmdArg, 2);
                        break;
                    case "BUTTON_3":
                        PressButton(cmdArg, 3);
                        break;
                    case "BUTTON_4":
                        PressButton(cmdArg, 4);
                        break;
                    case "BUTTON_5":
                        PressButton(cmdArg, 5);
                        break;
                    case "BUTTON_6":
                        PressButton(cmdArg, 6);
                        break;
                    case "BUTTON_7":
                        PressButton(cmdArg, 7);
                        break;
                    case "NEXT_MENU":
                        NextMenuPage(cmdArg);
                        break;
                    case "PREVIOUS_MENU":
                        PreviousMenuPage(cmdArg);
                        break;
                    default:
                        TriggerCall(argument);
                        break;
                }
            }
            else if (!_cruiseThrustersOn)
            {
                Echo("NO ARGUMENT");
            }
        }
    }
}
