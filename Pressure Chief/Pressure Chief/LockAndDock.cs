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
		// LOCK & DOCK FUNCTIONS --------------------------------------------------------------------------------------------------------------------------

		/* OPEN LOCK // - Initiallize lock opening sequence
		 *bool openAll determines if non-AutoOpen doors are included in sequence. */
		void OpenLock(string tag, bool openAll)
		{
			int phase;
			if (openAll)
				phase = 3;
			else
				phase = 1;

			Sector sector = GetSector(tag);
			if (UnknownSector(sector, LOCK))
				return;

			if (sector.Docked() && !openAll)
			{
				SetDockedOverride(sector, true);
				return;
			}

			sector.CloseDoors();
			if (sector.Vents.Count > 0)
			{
				sector.SetPressureStatus(true);
				//sector.UpdateLights(false); // pressurized = false
				sector.SetBlink(true);

				foreach (IMyAirVent vent in sector.Vents)
				{
					vent.Depressurize = true;
				}
			}

			StageLock(sector, phase, 1); // alert sound 1
		}


		// CLOSE LOCK // - Closes lock and restores vents to pressurized.
		void CloseLock(string tag)
		{
			Sector sector = GetSector(tag);

			if (UnknownSector(sector, LOCK))
				return;

			// Restore Docked sector to normal status.
			if (sector.Docked())
				SetDockedOverride(sector, false);

			sector.CloseDoors();
			if (sector.Vents.Count > 0)
			{
				foreach (IMyAirVent vent in sector.Vents)
				{
					vent.Depressurize = false;
				}
			}

			Bulkhead bulkhead = sector.GetExteriorBulkhead();
			if(bulkhead != null)
				bulkhead.SetOverride(false);
		}


		// CYCLE LOCK // - Opens or closes lock based on locks current Override State
		void CycleLock(string tag)
		{
			Sector sector = GetSector(tag);
			if (UnknownSector(sector, LOCK))
				return;

			Bulkhead bulkhead = sector.GetExteriorBulkhead();
			if (bulkhead == null)
				return;

			if (bulkhead.Override)
				CloseLock(tag);
			else
				OpenLock(tag, false);
		}


		// TIMER CALL // - Switch command to execute timer actions depending on its current state (recorded in Custom Data of Timer)
		void TimerCall(string tag)
		{
			Sector sector = GetSector(tag);
			if (UnknownSector(sector, LOCK))
				return;

			LockTimer timer = sector.LockTimer;
			//string phase = timer.GetKey("Phase", "0");
			

			Bulkhead bulkhead = sector.GetExteriorBulkhead();
			if (bulkhead == null)
			{
				_statusMessage = "CANNOT FIND EXTERIOR BULKHEAD FOR " + tag + "!\nCheck Your VAC_TAGs!";
				return;
			}

			switch (sector.Phase)
			{
				case 1: // OVERRIDE EXTERIOR BULKHEAD
					TimerOverride(timer, sector, bulkhead, 2);
					break;
				case 2: // OPEN AUTO-OPEN DOORS ONLY
					TimerOpen(timer, sector, bulkhead, false);
					break;
				case 3:// OVERRIDE EXTERIOR BULKHEAD
					TimerOverride(timer, sector, bulkhead, 4);
					break;
				case 4:
					TimerOpen(timer, sector, bulkhead, true);
					break;
				case 5: // OVERRIDE SELF AND DOCKED PORT
					SetDockedOverride(sector, true);
					TimerOverride(timer, sector, bulkhead, 6);
					break;
				case 6: // OPEN SELF
					bulkhead.Open(false);
					_statusMessage = "Dock " + sector.Name + " is sealed.";
					sector.SetPhase(0);
					//timer.SetKey("Phase", "0"); // Consider Removing for HATCH call
					break;
				case 7: // DISENGAGE CONNECTIONS
					ActivateDock(sector, false);
					sector.SetPhase(8);
					//timer.SetKey("Phase", "8");
					timer.TriggerDelay = 10;
					timer.StartCountdown();
					_statusMessage = "Dock " + sector.Name + " disengaged.";
					break;
				case 8: // RE-ENGAGE CONNECTIONS & RESET
					ActivateDock(sector, true);
					sector.SetPhase(0);
					//timer.SetKey("Phase", "0");
					_statusMessage = "Dock " + sector.Name + " re-enabled.";
					break;
				default:
					_statusMessage = sector.Name + " timer phase: " + sector.Phase;
					break;
			}

			sector.Check();
		}


		// TIMER OVERRIDE // - First Timer Action overrides lock.
		void TimerOverride(LockTimer timer, Sector sector, Bulkhead bulkhead, int phase)
		{
			_statusMessage = "Overriding Lock " + sector.Name;
			sector.SetPhase(phase);
			//timer.SetKey("Phase", phase);
			timer.TriggerDelay = 1;
			timer.StartCountdown();
			bulkhead.SetOverride(true);


			foreach (PressureDoor pressureDoor in bulkhead.Doors)
			{
				IMyDoor door = pressureDoor.Door;
				door.GetActionWithName("OnOff_On").Apply(door);

				if (phase == 6)
				{
					bool autoOpen = ParseBool(pressureDoor.GetKey("AutoOpen", "true"));
					if (!autoOpen)
					{
						pressureDoor.SetKey("Override", "false");
					}
				}
			}

			sector.Check();
		}


		// TIMER OPEN // - Second Timer Call - opens overriden doors.  Set openAll to true to open normally disabled doors.
		void TimerOpen(LockTimer timer, Sector sector, Bulkhead bulkhead, bool openAll)
		{
			bulkhead.Open(openAll);
			sector.SetPhase(0);
			sector.SetBlink(false);
			//timer.SetKey("Phase", "0");
			_statusMessage = sector.Type + " " + sector.Name + " opened.";
			sector.Check();
		}


		// DOCK SEAL // - Attempts to lock dock connectors to other dock.
		void DockSeal(Sector sector)
		{
			if (UnknownSector(sector, DOCK))
				return;

			foreach (IMyShipConnector connector in sector.Connectors)
				connector.Connect();

			StageLock(sector, 5, 2); // phase "3", alert 2
		}


		// UNDOCK // - Close dock, get other dock, close and reset override, then start timer to separate.
		void Undock(Sector sector)
		{
			if (UnknownSector(sector, DOCK))
				return;

			sector.CloseDoors();
			CloseLock(sector.Name);

			SetDockedOverride(sector, false);
			//foreach (IMyDoor door in dockedDoors)
			//	door.CloseDoor();

			StageLock(sector, 7, 1);
		}


		// STAGE LOCK // - Executes various repeated functions for Timer Calls
		void StageLock(Sector sector, int phase, int alert)
		{
			LockTimer timer = sector.LockTimer;
			UInt32 delay;
			sector.SetPhase(phase);
			//timer.SetKey("Phase", phase);

			if (UInt32.TryParse(timer.GetKey("Delay", DELAY.ToString()), out delay))
				delay--;
			else
				delay = DELAY - 1;

			if (delay < 1)
				delay = 1;

			timer.TriggerDelay = delay;
			timer.StartCountdown();

			if (sector.LockAlarm != null)
			{
				IMySoundBlock alarm = sector.LockAlarm;
				bool autoSound = ParseBool(GetKey(alarm, INI_HEAD, "Auto-Sound-Select", "true"));

				if (autoSound)
					alarm.SelectedSound = "SoundBlockAlert" + alert;

				alarm.LoopPeriod = DELAY;
				alarm.Play();
			}
		}


		// SET DOCKED OVERIDE // - Unlocks doors in connected docking port.
		void SetDockedOverride(Sector sector, bool overriding)
		{
			IMyBlockGroup dockedGroup = GetDockedGroup(sector);
			if(dockedGroup == null)
            {
				_statusMessage = "No group found docked to port " + sector.Name + "!";
				return;
            }

			// Get Doors from Docked Group and set override status
			List<IMyDoor> dockedDoors = new List<IMyDoor>();
			dockedGroup.GetBlocksOfType<IMyDoor>(dockedDoors);
			
			if (dockedDoors.Count > 0)
            {
				foreach (IMyDoor door in dockedDoors)
				{
					if (overriding)
					{
						bool autoOpen = ParseBool(GetKey(door, INI_HEAD, "AutoOpen", "true"));
						bool exterior = GetKey(door, INI_HEAD, "Sector_A", "") == _vacTag || GetKey(door, INI_HEAD, "Sector_B", "") == _vacTag;
						if (autoOpen && exterior)
							SetKey(door, INI_HEAD, "Override", "true");
						else
							SetKey(door, INI_HEAD, "Override", "false");
					}
					else
					{
						SetKey(door, INI_HEAD, "Override", "false");
						door.CloseDoor();
					}
				}
			}

			// Get list of vents in docked sector and set depressurization to false.
			List<IMyAirVent> dockedVents = new List<IMyAirVent>();
			dockedGroup.GetBlocksOfType<IMyAirVent>(dockedVents);

			if (dockedVents.Count > 0)
            {
				foreach (IMyAirVent vent in dockedVents)
					vent.Depressurize = false;
			}

			// Toggle Systems off if docking and on if undocking
			ToggleSystems(!overriding);
		}


		// ACTIVATE DOCK // - Turns dock merge blocks on or off, and attempts to connect any possible connectors.
		void ActivateDock(Sector sector, bool activate)
		{
			string action = "OnOff_Off";
			if (activate)
				action = "OnOff_On";


			if (sector.MergeBlocks.Count < 1)
				return;

			foreach (IMyShipMergeBlock mergeBlock in sector.MergeBlocks)
				mergeBlock.GetActionWithName(action).Apply(mergeBlock);

			if (sector.Connectors.Count > 0)
			{
				foreach (IMyShipConnector connector in sector.Connectors)
				{
					if (connector.Status != MyShipConnectorStatus.Connected)
						connector.GetActionWithName("SwitchLock").Apply(connector);

					connector.GetActionWithName(action).Apply(connector);
				}
			}
		}


		// TOGGLE SYSTEMS // - Toggles Systems Group off/on when docking/undocking.
		public void ToggleSystems(bool toggleOn)
		{
			if (_systemsName == "")
				return;

			List<IMyTerminalBlock> systemBlocks = new List<IMyTerminalBlock>();
			IMyBlockGroup systemGroup = GridTerminalSystem.GetBlockGroupWithName(_systemsName);

			// Terminate if no group found.
			if (systemGroup == null)
				return;

			string action = "OnOff_Off";
			if (toggleOn)
				action = "OnOff_On";

			systemGroup.GetBlocks(systemBlocks);
			foreach (IMyTerminalBlock block in systemBlocks)
				block.GetActionWithName(action).Apply(block);
		}


		// GET DOCKED GROUP //
		IMyBlockGroup GetDockedGroup(Sector sector)
		{
			if (sector.MergeBlocks.Count < 1)
			{
				_statusMessage = "Sector " + sector.Name + " has no Merge Blocks and cannot dock.";
				return null;
			}

			List<IMyBlockGroup> globalGroups = new List<IMyBlockGroup>();
			GridTerminalSystem.GetBlockGroups(globalGroups);

			foreach(IMyBlockGroup group in globalGroups)
            {
				if(group.Name.ToUpper().Contains(GROUP_TAG))
                {
					List<IMyShipMergeBlock> mergeBlocks = new List<IMyShipMergeBlock>();
					group.GetBlocksOfType<IMyShipMergeBlock>(mergeBlocks);

					// If Group has merge blocks and is not part of the original grid
					if (mergeBlocks.Count > 0 && GetKey(mergeBlocks[0], INI_HEAD, "Grid_ID", "") != _gridID)
					{
						foreach(IMyShipMergeBlock myMergeBlock in sector.MergeBlocks)
                        {
							if(myMergeBlock.IsConnected)
                            {
								foreach (IMyShipMergeBlock mergeBlock in mergeBlocks)
								{
									// Check if merge blocks are next to eachother
									if (Vector3I.DistanceManhattan(mergeBlock.Position, myMergeBlock.Position) < 2)
										return group;
								}
							}
                        }
					}
				}
            }

			return null;
		}
	}
}
