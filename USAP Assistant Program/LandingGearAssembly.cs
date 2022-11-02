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
        public class LandingGearAssembly
        {
            public List<IMyPistonBase> Pistons;
            public List<IMyMotorStator> Stators;
            public List<IMyLandingGear> LandingPlates;
            public List<IMyShipConnector> Connectors;
            public List<IMyShipMergeBlock> MergeBlocks;
            public List<IMyLightingBlock> Lights;

            public IMyTimerBlock Timer;
            public bool IsExtended;
            public string Status;

            public LandingGearAssembly(IMyTimerBlock timer)
            {
                Pistons = new List<IMyPistonBase>();
                Stators = new List<IMyMotorStator>();
                LandingPlates = new List<IMyLandingGear>();
                Connectors = new List<IMyShipConnector>();
                MergeBlocks = new List<IMyShipMergeBlock>();
                Lights = new List<IMyLightingBlock>();

                Timer = timer;

                IsExtended = ParseBool(GetKey(timer, INI_HEAD, "Extended", "True"));
                if (IsExtended)
                    Status = "Extended";
                else
                    Status = "Retracted";

                EnsureKey(timer, INI_HEAD, "Extension Delay", timer.TriggerDelay.ToString());
                EnsureKey(timer, INI_HEAD, "Retraction Delay", timer.TriggerDelay.ToString());
            }


            // EXTEND //
            public void Extend()
            {
                if (IsExtended)
                    return;

                Activate(true);
            }

            public void Retract()
            {
                Unlock();

                if (!IsExtended)
                    return;

                Activate(false);
            }


            // ACTIVATE //
            public void Activate(bool extending)
            {
                // Lock out further activation if currently extending or retracting.
                if (Timer.IsCountingDown)
                    return;

                IsExtended = extending;
                SetKey(Timer, INI_HEAD, "Extended", extending.ToString());

                if (LandingPlates.Count > 0)
                    foreach (IMyLandingGear landingPlate in LandingPlates)
                        DisengageLandingPlate(landingPlate);

                if (MergeBlocks.Count > 0)
                    foreach (IMyShipMergeBlock mergeBlock in MergeBlocks)
                        DisengageMergeBlock(mergeBlock, extending);

                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        EngagePiston(piston, extending);

                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        EngageStator(stator, extending);

                if (Lights.Count > 0)
                    foreach (IMyLightingBlock light in Lights)
                        ActivateLandingLight(light, extending);

                float delay;
                if (extending)
                {
                    delay = ParseFloat(GetKey(Timer, INI_HEAD, "Extension Delay", Timer.TriggerDelay.ToString()), Timer.TriggerDelay);
                    Status = "Extending...";
                }
                else
                {
                    delay = ParseFloat(GetKey(Timer, INI_HEAD, "Retraction Delay", Timer.TriggerDelay.ToString()), Timer.TriggerDelay);
                    Status = "Retracting...";
                }

                Timer.TriggerDelay = delay;
                Timer.StartCountdown();
            }


            public void Toggle()
            {
                if (IsExtended)
                    Retract();
                else
                    Extend();
            }


            public void TimerCall()
            {
                if (Status == "Extending..." || Status == "Retracting...")
                    TimerLock();
                else
                    Toggle();
            }

            public void TimerLock()
            {
                if (IsExtended)
                    Status = "Extended";
                else
                    Status = "Retracted";

                // Deactivate pistons
                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        DisengagePiston(piston);

                // Deactivate rotors and hinges
                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        DisengageStator(stator);

                // Set Mag Plates to AutoLock or Off
                if (LandingPlates.Count > 0)
                    foreach (IMyLandingGear landingPlate in LandingPlates)
                        EngageLandingPlate(landingPlate);

                // Reengage Merge Blocks
                if (MergeBlocks.Count > 0)
                    foreach (IMyShipMergeBlock mergeBlock in MergeBlocks)
                        EngageMergeBlock(mergeBlock);
            }


            // SWAP DIRECTIONS //
            public void SwapDirections()
            {
                IsExtended = !IsExtended;
                SetKey(Timer, INI_HEAD, "Extended", IsExtended.ToString());

                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        SwapVelocities(piston);

                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        SwapVelocities(stator);
            }

            // CLEAR DATA // - Temp
            public void ClearData()
            {
                Timer.CustomData = "";

                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        piston.CustomData = "";

                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        stator.CustomData = "";
            }


            public bool IsParked()
            {
                if (_landingGear.Connectors.Count > 0)
                    foreach (IMyShipConnector connector in Connectors)
                        if (connector.Status == MyShipConnectorStatus.Connected)
                            return true;

                if (_landingGear.LandingPlates.Count > 0)
                    foreach (IMyLandingGear landingPlate in LandingPlates)
                        if (landingPlate.IsLocked)
                            return true;

                return false;
            }


            public void Lock()
            {
                if (Status != "Extended")
                    return;

                if (LandingPlates.Count > 0)
                    foreach (IMyLandingGear landingPlate in LandingPlates)
                        landingPlate.Lock();

                if (Connectors.Count > 0)
                    foreach (IMyShipConnector connector in Connectors)
                        connector.Connect();

            }


            public void Unlock()
            {
                if (Status != "Extended")
                    return;

                if (LandingPlates.Count > 0)
                    foreach (IMyLandingGear landingPlate in LandingPlates)
                        landingPlate.Unlock();

                if (Connectors.Count > 0)
                    foreach (IMyShipConnector connector in Connectors)
                        connector.Disconnect();
            }


            public void SwitchLock()
            {
                if (Status != "Extended")
                    return;

                if (IsParked())
                    Unlock();
                else
                    Lock();
            }
        }


        // INIT FUNCTIONS ---------------------------------------------------------------------------------------------------------------------------------------

        // ASSEMBLE LANDING GEAR //
        void AssembleLandingGear()
        {
            _landingGear = null;

            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timers);

            if (timers.Count < 1)
                return;

            foreach (IMyTimerBlock timer in timers)
            {
                if (timer.CustomName.Contains(GEAR_TAG) && GetKey(timer, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString()) == _gridID)
                {
                    _landingGear = new LandingGearAssembly(timer);

                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.SearchBlocksOfName(GEAR_TAG, blocks);

                    foreach (IMyTerminalBlock block in blocks)
                    {
                        if (GetKey(block, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString()) == _gridID)
                        {
                            switch (block.DefinitionDisplayNameText)
                            {
                                case "Piston":
                                    AssignLandingPiston(block as IMyPistonBase);
                                    break;
                                case "Connector":
                                    _landingGear.Connectors.Add(block as IMyShipConnector);
                                    break;
                                case "Rotor":
                                case "Advanced Rotor":
                                case "Hinge":
                                case "Hinge 3x3":
                                    AssignLandingStator(block as IMyMotorStator);
                                    break;
                                case "Landing Gear":
                                case "Magnetic Plate":
                                case "Large Magnetic Plate":
                                    AssignLandingPlate(block as IMyLandingGear);
                                    break;
                                case "Spotlight":
                                case "Searchlight":
                                case "Light Panel":
                                case "Offset Spotlight":
                                case "Offset Light":
                                case "Rotating Light":
                                case "Corner Light - Double":
                                case "Corner Light":
                                case "Interior Light":
                                    AssignLandingLight(block as IMyLightingBlock);
                                    break;
                                case "Merge Block":
                                case "Small Merge Block":
                                    AssignMergeBlock(block as IMyShipMergeBlock);
                                    break;
                            }
                        }
                    }

                    return;
                }
            }
        }


        // ASSIGN LANDING PLATE //
        void AssignLandingPlate(IMyLandingGear landingPlate)
        {
            EnsureKey(landingPlate, INI_HEAD, "On Retract", "AutoLock");
            EnsureKey(landingPlate, INI_HEAD, "On Extend", "AutoLock");
            _landingGear.LandingPlates.Add(landingPlate);
        }


        // ASSIGN LANDING STATOR //
        void AssignLandingStator(IMyMotorStator stator)
        {
            string defaultBool;
            float velocity = stator.TargetVelocityRPM;

            if ((_landingGear.IsExtended && velocity >= 0) || (!_landingGear.IsExtended && velocity < 0))
                defaultBool = "True";
            else
                defaultBool = "False";

            // Determine whether stator motion is positive or negative in relation to extension/retraction
            bool extendToPositive = ParseBool(GetKey(stator, INI_HEAD, "Extend To Positive", defaultBool));

            // Set default extension and retraction velocities based on current velocities
            float defaultExtend, defaultRetract;
            if ((extendToPositive && velocity > 0) || !extendToPositive && velocity < 0)
            {
                defaultExtend = velocity;
                defaultRetract = -velocity;
            }
            else
            {
                defaultExtend = -velocity;
                defaultRetract = velocity;
            }

            EnsureKey(stator, INI_HEAD, "Extend Velocity", defaultExtend.ToString("0.00"));
            EnsureKey(stator, INI_HEAD, "Retract Velocity", defaultRetract.ToString("0.00"));
            EnsureKey(stator, INI_HEAD, "Off When Stationary", "false");

            _landingGear.Stators.Add(stator);
        }


        // ASSIGN LANDING PISTON //
        void AssignLandingPiston(IMyPistonBase piston)
        {
            string defaultBool;
            float velocity = piston.Velocity;

            if ((_landingGear.IsExtended && velocity >= 0) || (!_landingGear.IsExtended && velocity < 0))
                defaultBool = "True";
            else
                defaultBool = "False";

            // Determine if extension corresponds to positive velocity
            bool extendToPositive = ParseBool(GetKey(piston, INI_HEAD, "Extend To Positive", defaultBool));

            // Set default extension and retraction velocities based on current velocities
            float defaultExtend, defaultRetract;
            if ((extendToPositive && velocity > 0) || !extendToPositive && velocity < 0)
            {
                defaultExtend = velocity;
                defaultRetract = -velocity;
            }
            else
            {
                defaultExtend = -velocity;
                defaultRetract = velocity;
            }

            EnsureKey(piston, INI_HEAD, "Extend Velocity", defaultExtend.ToString("0.00"));
            EnsureKey(piston, INI_HEAD, "Retract Velocity", defaultRetract.ToString("0.00"));
            EnsureKey(piston, INI_HEAD, "Off When Stationary", "false");

            _landingGear.Pistons.Add(piston);
        }


        // ASSIGN LANDING LIGHT //
        void AssignLandingLight(IMyLightingBlock light)
        {
            string defaultExtended, defaultRetracted;
            string currentColor = light.Color.R + "," + light.Color.G + "," + light.Color.B;

            if ((_landingGear.IsExtended && light.IsWorking) || (!_landingGear.IsExtended && !light.IsWorking))
            {
                defaultExtended = currentColor;
                defaultRetracted = "0,0,0";
            }
            else
            {
                defaultExtended = "0,0,0";
                defaultRetracted = currentColor;
            }

            EnsureKey(light, INI_HEAD, "Color on Extend", defaultExtended);
            EnsureKey(light, INI_HEAD, "Color on Retract", defaultRetracted);

            _landingGear.Lights.Add(light);
        }


        // ASSIGN MERGE BLOCK //
        void AssignMergeBlock(IMyShipMergeBlock mergeBlock)
        {
            EnsureKey(mergeBlock, INI_HEAD, "Disable on  Extend", "False");
            EnsureKey(mergeBlock, INI_HEAD, "Disable on Retract", "True");
            EnsureKey(mergeBlock, INI_HEAD, "Enable When Stopped", "True");

            _landingGear.MergeBlocks.Add(mergeBlock);
        }


        // ACTIVATION FUNCTIONS ---------------------------------------------------------------------------------------------------------------------------------

        // ENGAGE STATOR // - For use when assembly is in motion
        static void EngageStator(IMyMotorStator stator, bool extending)
        {
            stator.GetActionWithName("OnOff_On").Apply(stator);
            stator.RotorLock = false;

            if (_landingGear.IsExtended)
                stator.TargetVelocityRPM = ParseFloat(GetKey(stator, INI_HEAD, "Extend Velocity", "0"), 0);
            else
                stator.TargetVelocityRPM = ParseFloat(GetKey(stator, INI_HEAD, "Retract Velocity", "0"), 0);
        }


        // DISENGAGE STATOR // - For use when assembly is at rest
        static void DisengageStator(IMyMotorStator stator)
        {
            stator.TargetVelocityRPM = 0;

            if (ParseBool(GetKey(stator, INI_HEAD, "Off When Stationary", "false")))
                stator.GetActionWithName("OnOff_Off").Apply(stator);
            else
                stator.RotorLock = true;
        }


        // ENGAGE PISTON //
        static void EngagePiston(IMyPistonBase piston, bool extending)
        {
            piston.GetActionWithName("OnOff_On").Apply(piston);

            if (extending)
                piston.Velocity = ParseFloat(GetKey(piston, INI_HEAD, "Extend Velocity", "0"), 0);
            else
                piston.Velocity = ParseFloat(GetKey(piston, INI_HEAD, "Retract Velocity", "0"), 0);
        }


        // DISENGAGE PISTON //
        static void DisengagePiston(IMyPistonBase piston)
        {
            piston.Velocity = 0;

            if (ParseBool(GetKey(piston, INI_HEAD, "Off When Stationary", "false")))
                piston.GetActionWithName("OnOff_Off").Apply(piston);
        }


        // ACTIVATE LANDING LIGHT //
        static void ActivateLandingLight(IMyLightingBlock light, bool extending)
        {
            if (extending)
                light.Color = ParseColor(GetKey(light, INI_HEAD, "Color on Extend", "255,0,127"));
            else
                light.Color = ParseColor(GetKey(light, INI_HEAD, "Color on Retract", "0,0,0"));
        }


        // DISENGAGE LANDING PLATE // - For use when gear assembly is in motion (extending or retracting)
        static void DisengageLandingPlate(IMyLandingGear landingGear)
        {
            landingGear.GetActionWithName("OnOff_On").Apply(landingGear);
            landingGear.Unlock();
            landingGear.AutoLock = false;
        }


        // ENGAGE LANDING PLATE // - For use when gear assembly is at rest (extended or retracted)
        static void EngageLandingPlate(IMyLandingGear landingGear)
        {
            if (_landingGear.IsExtended)
            {
                string onExtend = GetKey(landingGear, INI_HEAD, "On Extend", "AutoLock").ToUpper();

                // Check if string is any reasonable permutation of "AutoLock"
                switch (onExtend)
                {
                    case "AUTOLOCK":
                    case "AUTO LOCK":
                    case "AUTO-LOCK":
                    case "AUTO_LOCK":
                        landingGear.AutoLock = true;
                        break;
                }
            }
            else if (!_landingGear.IsExtended)
            {
                string onRetract = GetKey(landingGear, INI_HEAD, "On Retract", "AutoLock").ToUpper();

                switch (onRetract)
                {
                    case "AUTOLOCK":
                    case "AUTO LOCK":
                    case "AUTO-LOCK":
                    case "AUTO_LOCK":
                        landingGear.AutoLock = true;
                        break;
                    case "OFF":
                    case "TURNOFF":
                    case "TURN OFF":
                        landingGear.GetActionWithName("OnOff_Off").Apply(landingGear);
                        break;
                }
            }
        }


        // DISENGAGE MERGE BLOCK //
        static void DisengageMergeBlock(IMyShipMergeBlock mergeBlock, bool extending)
        {
            bool disableBlock;

            if (extending)
                disableBlock = ParseBool(GetKey(mergeBlock, INI_HEAD, "Disable on  Extend", "False"));
            else
                disableBlock = ParseBool(GetKey(mergeBlock, INI_HEAD, "Disable on  Retract", "True"));

            string action;
            if (disableBlock)
                action = "OnOff_Off";
            else
                action = "OnOff_On";

            mergeBlock.GetActionWithName(action).Apply(mergeBlock);
        }


        // ENGAGE MERGE BLOCK //
        static void EngageMergeBlock(IMyShipMergeBlock mergeBlock)
        {
            if (ParseBool(GetKey(mergeBlock, INI_HEAD, "Enable When Stopped", "True")))
                mergeBlock.GetActionWithName("OnOff_On").Apply(mergeBlock);
        }

        // SET RETRACT BEHAVIOR //
        void SetRetractBehavior(string behavior)
        {
            if (_landingGear == null || _landingGear.LandingPlates.Count < 1)
                return;

            foreach (IMyLandingGear landingPlate in _landingGear.LandingPlates)
                SetKey(landingPlate, INI_HEAD, "On Retract", behavior);
        }


        // SWAP VELOCITIES //
        static void SwapVelocities(IMyTerminalBlock block)
        {
            bool extendToPositive = !(ParseBool(GetKey(block, INI_HEAD, "Extend To Positive", "")));
            SetKey(block, INI_HEAD, "Extend To Positive", extendToPositive.ToString());

            string extendVelocity = GetKey(block, INI_HEAD, "Extend Velocity", "");
            string retractVelocity = GetKey(block, INI_HEAD, "Retract Velocity", "");

            if (extendVelocity != "")
                SetKey(block, INI_HEAD, "Retract Velocity", extendVelocity);

            if (retractVelocity != "")
                SetKey(block, INI_HEAD, "Extend Velocity", retractVelocity);
        }
    }
}
