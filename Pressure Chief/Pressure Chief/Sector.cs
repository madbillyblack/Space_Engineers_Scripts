﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.CodeDom;
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
		const string LOCK = "LOCK";
		const string DOCK = "DOCK";
		const string SEALING = "Checking Seal";

		// SECTOR // - Class that includes all components for a specific room.
		public class Sector
		{
			public IMyBlockGroup Group;
			public string Name; // Name or Designation for Sector
			public List<IMyAirVent> Vents;
			public List<IMyDoor> Doors;
			public List<PressureLight> Lights;
			//public List<IMyTextSurface> Surfaces; // LCD screens used to display pressure readings
			public List<IMyShipMergeBlock> MergeBlocks;
			public List<IMyShipConnector> Connectors;
			//public List<Bulkhead> Bulkheads;
			public List<GaugeSurface> Gauges;
			public string Type; // Room, Lock, Dock, or Vacuum
			public string NormalColor; // Default pressurized light color for sector
			public string EmergencyColor; // Default depressurized light color for sector
			public string Status; // Current pressure status read from main Vents[0]
			public LockTimer LockTimer;
			public IMySoundBlock LockAlarm;
			public bool IsPressurized;
			public bool Depressurizing;
			public bool HasChanged;
			public bool IsStaging;
			public int AutoCloseDelay;
			//public int CurrentDelayTime;
			public int Phase;
			public bool CheckingSeal;

			BlockIni Ini;

			// Constructor
			public Sector(IMyBlockGroup blockGroup)
			{
				//Initialize Lists
				Vents = new List<IMyAirVent>();
				Doors = new List<IMyDoor>();
				Lights = new List<PressureLight>();
				MergeBlocks = new List<IMyShipMergeBlock>();
				Connectors = new List<IMyShipConnector>();
				Gauges = new List<GaugeSurface>();
				IsStaging = false;
				//Surfaces = new List<IMyTextSurface>(); //OBSOLETE
				//Bulkheads = new List<Bulkhead>(); //OBSOLETE

				Group = blockGroup;
				SetName();
				AssignVents();
				AssignTimer();
				AssignDoors(); //And add to Bulkhead
				AssignLights();
				UpdateLights(IsPressurized);
				
			}

			// GET KEY
			public string GetKey(string key, string defaultValue)
			{
				return Ini.GetKey(key, defaultValue);
			}

			// SET KEY
			public void SetKey(string key, string value)
			{
				Ini.SetKey(key, value);
			}

			// SET NAME
			private void SetName()
			{
				string[] nameArray = Group.Name.Split(':');
				if (nameArray.Length < 2)
				{
					_buildMessage += "\nINVALID SECTOR GROUP NAME!\n - " + Group.Name;
					return;
				}
				Name = nameArray[1].Trim();

				// Designate exterior sector
				if (Name == _vacTag)
					Type = "Vacuum";
				else
					Type = "Room";
			}


			// ASSIGN VENTS
			private void AssignVents()
			{
				Group.GetBlocksOfType<IMyAirVent>(Vents);
				if (Vents.Count < 1)
				{
					_buildMessage += "\nNO AIR VENTS ASSIGNED TO GROUP\n - " + Group.Name + "!";
					return;
				}

				IMyAirVent airVent = Vents[0];
				Ini = new BlockIni(airVent, INI_HEAD);
				//Vents.Add(airVent);
				Status = GetKey("Status", airVent.Status.ToString());
				IsPressurized = airVent.GetOxygenLevel() >= 1 - _differential;

				//Name = TagFromName(airVent.CustomName);
				NormalColor = GetKey("Normal_Color", NORMAL);
				EmergencyColor = GetKey("Emergency_Color", EMERGENCY);
			}


			// ASSIGN DOORS
			private void AssignDoors()
			{
				Group.GetBlocksOfType<IMyDoor>(Doors);
				if (Doors.Count < 1)
				{
					_buildMessage += "\nNO DOORS ASSIGNED TO GROUP\n - " + Group.Name + "!";
					return;
				}

				// Assign Sector to Bulkhead
				foreach (IMyDoor door in Doors)
				{
					Bulkhead bulkhead = BulkheadFromDoor(door);
					if (bulkhead != null && bulkhead.Sectors.Count < 2 && !bulkhead.Sectors.Contains(this))
                    {
						bulkhead.Sectors.Add(this);
						bulkhead.SetAutoClose();
					}
				}
			}


			// ASSIGN LIGHTS
			private void AssignLights()
			{
				List<IMyLightingBlock> lightingBlocks = new List<IMyLightingBlock>();
				Group.GetBlocksOfType<IMyLightingBlock>(lightingBlocks);
				if (lightingBlocks.Count < 1)
					return;

				foreach (IMyLightingBlock lightingBlock in lightingBlocks)
				{
					PressureLight light = new PressureLight(lightingBlock, IsAirLock());
					Lights.Add(light);
				}
			}


			// ASSIGN TIMER
			private void AssignTimer()
			{
				List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
				Group.GetBlocksOfType<IMyTimerBlock>(timers);
				Phase = 0;

				if (timers.Count < 1)
                {
					return;
				}	
				else if (timers.Count > 1)
				{
					_buildMessage += "\nWARNING: Sector " + Name + " has more than 1 timer assigned.\n * Only timer " + timers[0].CustomName + " will be used.";
				}
				else if (Type == "Vacuum")
                {
					_buildMessage += "\nWARNING: Exterior Sector has no purpose for a Timer block. Either remove timer from Exterior Sector Group, or Rename Group to not be exterior.";
					return;
                }

				LockTimer = new LockTimer(timers[0]);
				Type = LOCK;
				Phase = ParseUInt(LockTimer.GetKey("Phase", "0"));

				if(Phase == 1 || Phase == 3)
					IsStaging = true;

				AssignAlarm();
				
				// Check if Lock has merge blocks required to be a dock.
				AssignMergeBlocks();
			}


			// ASSIGN ALARM
			private void AssignAlarm()
			{
				List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();
				Group.GetBlocksOfType<IMySoundBlock>(soundBlocks);

				if (soundBlocks.Count < 1)
					return;
				else if (soundBlocks.Count > 1)
				{
					_buildMessage += "\nWARNING: Sector " + Name + " has more than 1 sound block assigned.\n * Only sound block " + soundBlocks[0].CustomName + " will be used.";
				}

				LockAlarm = soundBlocks[0];

				if (LockTimer == null)
					_buildMessage += "\nWARNING: Sector " + Name + " has sound block assigned but no timer\n * Air Locks require a timer to function!";
			}


			// ASSIGN MERGE BLOCKS
			private void AssignMergeBlocks()
			{
				Group.GetBlocksOfType<IMyShipMergeBlock>(MergeBlocks);
				if (MergeBlocks.Count < 1)
					return;

				Type = DOCK;
				CheckingSeal = ParseBool(GetKey(SEALING,"False"));
				AssignConnectors();
			}


			// ASSIGN CONNECTORS
			private void AssignConnectors()
			{
				Group.GetBlocksOfType<IMyShipConnector>(Connectors);

				if (Connectors.Count > 0 && MergeBlocks.Count < 1)
				{
					_buildMessage += "\nWARNING: Sector " + Name + " has no merge blocks assigned.\n * Docking Ports require at least 1 merge block in order to function.";
				}
			}


			// CHECK - Check all pressure status between this and all neighboring sectors and update lights based on pressure status.
			public void Check()
			{
				bool airlockActive = IsAirLock() && Phase > 0 && Phase < 5;

				bool pressurized = (Vents[0].GetOxygenLevel() >= 1 - _differential) && !airlockActive;
				bool depressurize = Vents[0].Depressurize;

				if (IsPressurized == pressurized && Depressurizing == depressurize)
					HasChanged = false;
				else
					HasChanged = true;

				IsPressurized = pressurized;
				Depressurizing = depressurize;
				DrawGauges();
				//AutoClose();

				if (!HasChanged)
					return;

				UpdateLights(pressurized);

				if (Type == "Room")
					CloseDoors();
				else if (Type == DOCK && CheckingSeal)
					CheckSeal(this);

			}


			// UPDATE STATUS - Update the pressurization status of vent's custom data, and closed doors if status has changed. 
			public void UpdateStatus()
			{
				IMyAirVent airVent = Vents[0];
				string oldStatus = GetKey("Status", "0");
				float o2Level;
				if (!float.TryParse(oldStatus, out o2Level))
					o2Level = 0;

				if (Type == "Room")
				{
					if (Math.Abs(o2Level - airVent.GetOxygenLevel()) > _differential)
						CloseDoors();
				}

				SetKey("Status", airVent.GetOxygenLevel().ToString());
			}


			// CLOSE DOORS //
			public void CloseDoors()
			{
				if (Doors.Count < 1)
					return;

				foreach (IMyDoor myDoor in Doors)
				{
					myDoor.CloseDoor();
				}
			}



			// GET EXTERIOR BULKHEAD - Returns bulkhead between this sector and exterior
			public Bulkhead GetExteriorBulkhead()
			{
				if (_bulkheads.Count > 0)
				{
					// Return null if exterior vent.
					if (Name == _vacTag)
					{
						_statusMessage = "Command not applicable to Exterior";
						return null;
					}

					foreach (Bulkhead bulkhead in _bulkheads)
					{
						if (bulkhead.Sectors.Contains(this) && bulkhead.Sectors.Contains(GetSector(_vacTag)))
							return bulkhead;
					}
				}

				_statusMessage = "Can't find exterior bulkhead for " + Name + "!";
				return null;
			}


			// SET COLOR //
			public void SetColor(string colorData, bool emergency)
			{

				if (emergency)
				{
					foreach (PressureLight light in Lights)
					{
						light.SetKey("Emergency_Color", colorData);
						light.EmergencyColor = ColorFromString(colorData);
					}
					//IniKey.SetKey(Vents[0], INI_HEAD, "Emergency_Color", colorData);
				}
				else
				{
					foreach (PressureLight light in Lights)
					{
						light.SetKey("Normal_Color", colorData);
						light.NormalColor = ColorFromString(colorData);
					}
					//IniKey.SetKey(Vents[0], INI_HEAD, "Normal_Color", colorData);
				}
			}


			// SET RADIUS //
			public void SetRadius(string radius, bool emergency)
			{
				foreach (PressureLight light in Lights)
				{
					if (emergency)
					{
						light.SetKey("Emergency_Radius", radius);
						light.EmergencyRadius = float.Parse(radius);
					}

					else
					{
						light.SetKey("Normal_Radius", radius);
						light.NormalRadius = float.Parse(radius);
					}
				}
			}


			// SET INTENSITY //
			public void SetIntensity(string intensity, bool emergency)
			{
				foreach (PressureLight light in Lights)
				{
					if (emergency)
						light.SetKey("Emergency_Intensity", intensity);
					else
						light.SetKey("Normal_Intensity", intensity);
				}
			}

			
			// SET PRESSURE STATUS // Update IsPressurized bool based on Vent Pressure.
			public void SetPressureStatus()
			{
				IsPressurized = Vents[0].GetOxygenLevel() >= 1 - _differential;
			}

			public void SetPressureStatus(bool doorOverride)
			{
				bool pressurized = Vents[0].GetOxygenLevel() >= 1 - _differential;

				if (pressurized && !doorOverride)
					IsPressurized = true;
				else
					IsPressurized = false;
			}

			// SET BLINK
			public void SetBlink(bool blinkOn)
			{
				if (Lights.Count< 1)
					return;

				foreach (PressureLight light in Lights)
					light.SetBlink(blinkOn);
            }


			// UPDATE LIGHTS //
			public void UpdateLights(bool pressurized)
			{
				if (Lights.Count < 1)
					return;

				//bool depressurized = Vents[0].GetOxygenLevel() < 0.7 || Vents[0].Depressurize;

				foreach (PressureLight myLight in Lights)
				{
					myLight.SetState(pressurized);
					/*
					if (!pressurized || Vents[0].Depressurize)
					{
						myLight.LightBlock.Color = Util.ColorFromString(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Emergency_Color", EmergencyColor));
						try
						{
							myLight.LightBlock.Radius = float.Parse(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Emergency_Radius", "0"));
							myLight.LightBlock.Intensity = float.Parse(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Emergency_Intensity", "0"));
						}
						catch
						{
							_statusMessage = "WARNING: " + myLight.LightBlock.CustomName +
											"\ncontains invalid parameters!" +
											"\nCheck Custom Data inputs!";
						}
					}
					else
					{
						myLight.LightBlock.Color = Util.ColorFromString(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Normal_Color", NormalColor));

						try
						{
							myLight.LightBlock.Radius = float.Parse(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Normal_Radius", "0"));
							myLight.LightBlock.Intensity = float.Parse(IniKey.GetKey(myLight.LightBlock, INI_HEAD, "Normal_Intensity", "0"));
						}
						catch
						{
							_statusMessage = "WARNING: " + myLight.LightBlock.CustomName +
											"\ncontains invalid parameters!" +
											"\nCheck Custom Data inputs!";
						}
					}
					*/
				}
			}


			// DOCKED // Returns if sector is docked to another grid
			public bool Docked()
			{
				if (Type != DOCK || MergeBlocks.Count < 1)
					return false;

				foreach (IMyShipMergeBlock mergeBlock in MergeBlocks)
				{
					if (mergeBlock.IsConnected)
						return true;
				}

				return false;
			}

			/*
			// AUTO CLOSE //
			public void AutoClose()
            {
				// If delay set to 0 or no doors open exit
				if(AutoCloseDelay < 1)
                {
					return;
                }

				if (!HasOpenDoors())
                {
					CurrentDelayTime = AutoCloseDelay;
					return;
				}
					

				CurrentDelayTime--;
				if (CurrentDelayTime < 1)
					CloseDoors();
            }
			*/

			// HAS OPEN DOORS //
			public bool HasOpenDoors()
            {
				if(Doors.Count > 0)
                {
					foreach(IMyDoor door in Doors)
                    {
						if(door.OpenRatio > 0.9f)
                        {
							return true;
                        }
                    }
                }

				return false;
            }

			// SET AUTO CLOSE
			public void SetAutoCloseDelay(string delayTime, bool writeToData = true)
            {
				//AutoCloseDelay = (ParseUInt(delayTime) * _autoCloseFactor)/10; // Division by 10 is for backwards compatibility
				AutoCloseDelay = ParseUInt(delayTime);// * (int)(20 * _autoCloseFactor / _sectors.Count +1); // +1 to avoid 0 on upadate100

				//if (AutoCloseDelay > 0)
				//	CurrentDelayTime = AutoCloseDelay;

				if(writeToData)
                {
					SetKey("Auto_Close_Delay", AutoCloseDelay.ToString());
				}
            }


			// DRAW GAUGES //
			public void DrawGauges()
            {
				if (Gauges.Count < 1)
					return;

				foreach(GaugeSurface gauge in Gauges)
                {
					DrawSingleSectorGauge(gauge, this);
                }
            }

			// IS LOCK
			public bool IsAirLock()
            {
				return Type == LOCK || Type == DOCK;
            }

			// SET PHASE
			public void SetPhase(int phase)
			{
				Phase = phase;
				LockTimer.SetKey("Phase", Phase.ToString());
			}
		}


		// LOCK TIMER CLASS //
		public class LockTimer
		{
			public IMyTimerBlock Timer;
			BlockIni Ini;

			// Constructor
			public LockTimer(IMyTimerBlock timer)
			{
				Timer = timer;
				Ini = new BlockIni(Timer, INI_HEAD);
			}

			// GET KEY
			public string GetKey(string key, string defaultValue)
			{
				return Ini.GetKey(key, defaultValue);
			}

			// SET KEY
			public void SetKey(string key, string value)
			{
				Ini.SetKey(key, value);
			}

			// TRIGGER DELAY
			public float TriggerDelay
            {
                get { return Timer.TriggerDelay; }
				set { Timer.TriggerDelay = value; }
            }

			//START COUNTDOWN
			public void StartCountdown()
            {
				Timer.StartCountdown();
            }
		}
	}
}
