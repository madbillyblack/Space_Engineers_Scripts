﻿using Sandbox.Game.EntityComponents;
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
		// SYNC // Switch function for all sync commands
		void sync(string cmdArg, string argData)
		{
			bool syncTo = argData == "OVERWRITE";

			if (cmdArg == "MASTER")
			{
				syncMaster(syncTo);
			}
			else if (cmdArg == "NEAREST")
			{
				syncNearest(syncTo);
			}
			else
			{
				_statusMessage = "Invalid Sync Command!";
			}
		}


		// SYNC MASTER // Finds Master Sync Block on station and syncs to or from depending on bool.
		void syncMaster(bool syncTo)
		{

			if (Me.CustomName.Contains(SYNC_TAG))
			{
				_statusMessage = "SYNC Requests cannot be made from SYNC terminal!\n";
				return;
			}

			List<IMyTerminalBlock> syncBlocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName(SYNC_TAG, syncBlocks);

			if (syncBlocks.Count < 1)
			{
				_statusMessage = "NO MAP MASTER FOUND.\nPlease add tag '" + SYNC_TAG + "' to the map computer's name on your station or capital ship.\n";
				return;
			}

			if (syncBlocks.Count > 1)
			{
				_statusMessage = "Multiple blocks found with tag '" + SYNC_TAG + "'! Please resolve conflict before syncing.\n";
				return;
			}

			syncWith(syncBlocks[0] as IMyProgrammableBlock, syncTo);
		}


		// SYNC NEAREST // Finds Nearest other mapping program and syncs to or from depending on bool.
		void syncNearest(bool syncTo)
		{
			List<IMyProgrammableBlock> computers = new List<IMyProgrammableBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(computers);

			IMyProgrammableBlock syncBlock = null;
			float nearest = float.MaxValue;

			foreach (IMyProgrammableBlock computer in computers)
			{
				if (computer.CustomData.Contains("[Map Settings]") && !(computer == Me))
				{
					float distance = Vector3.Distance(_myPos, computer.GetPosition());
					if (distance < nearest)
					{
						nearest = distance;
						syncBlock = computer;
					}
				}
			}

			if (!(syncBlock == null))
			{
				syncWith(syncBlock, syncTo);
				return;
			}

			_statusMessage = "No other mapping computers available to sync!";
		}


		// SYNC WITH // Sync map data with master sync computer.
		void syncWith(IMyProgrammableBlock syncBlock, bool syncTo)
		{

			IMyTerminalBlock blockA = syncBlock as IMyTerminalBlock;
			IMyTerminalBlock blockB = Me;

			if (syncTo)
			{
				blockA = Me;
				blockB = syncBlock as IMyTerminalBlock;
			}

			int[] pSync = mapSync(blockA, blockB, "Planet_List");
			int[] wSync = mapSync(blockA, blockB, "Waypoint_List");

			if (syncTo)
			{
				pSync = syncReverse(pSync);
				wSync = syncReverse(wSync);
			}

			syncBlock.TryRun("SYNC_ALERT " + Me.CustomName);
			Build();

			_statusMessage = "MAP DATA SYNCED\n-- Planets --\nDownloaded: " + pSync[0] + "\nUploaded: " + pSync[1] + "\n\n--Waypoints--\nDownloaded: " + wSync[0] + "\nUploaded: " + wSync[1] + "\n";
		}


		// SYNC REVERSE // reverses 2 entry int array
		int[] syncReverse(int[] input)
		{
			int[] output = new int[] { input[1], input[0] };
			return output;
		}


		// MAP SYNC //  Syncs Data for specific ini entry between two blocks, int array [from, to] mapA.
		int[] mapSync(IMyTerminalBlock mapA, IMyTerminalBlock mapB, string listName)
		{
			int[] downUp = new int[] { 0, 0 };

			if (syncBlockError(mapA))
				return downUp;
			if (syncBlockError(mapB))
				return downUp;

			MyIni iniA = DataToIni(mapA);
			MyIni iniB = DataToIni(mapB);

			string dataA = iniA.Get("Map Settings", listName).ToString();
			string dataB = iniB.Get("Map Settings", listName).ToString();
			string newData = "";

			if (dataA == "")
			{
				newData = dataB;
			}
			else if (dataB == "")
			{
				newData = dataA;
			}
			else
			{
				List<string> outputs = dataA.Split('\n').ToList();
				List<string> inputs = dataB.Split('\n').ToList();

				int startCount = outputs.Count;
				int matchCount = 0;

				foreach (string input in inputs)
				{
					string name = input.Split(';')[0];
					bool matched = false;

					foreach (string output in outputs)
					{
						if (output.StartsWith(name))
						{
							matched = true;
						}
					}

					if (matched)
						matchCount++;
					else
						outputs.Add(input);
				}

				downUp[0] = startCount - matchCount;
				downUp[1] = inputs.Count - matchCount;

				foreach (string entry in outputs)
				{
					newData += entry + "\n";
				}
			}

			iniA.Set("Map Settings", listName, newData.Trim());
			mapA.CustomData = iniA.ToString();

			iniB.Set("Map Settings", listName, newData.Trim());
			mapB.CustomData = iniB.ToString();

			return downUp;
		}


		// SYNC BLOCK ERROR // Returns false and sets error message if Sync Block has no Map Data
		bool syncBlockError(IMyTerminalBlock sync)
		{
			if (sync.CustomData.Contains("[Map Settings]"))
			{
				return false;
			}

			_statusMessage = "SYNC Block '" + sync.CustomName + "' contains no map settings! Please ensure that SYNC Block is also running this script!\n";
			return true;
		}


		// SYNC ALERT // Refreshes then sets status to alert message.
		void syncAlert(string name)
		{
			Build();
			string senderData;
			IMyTerminalBlock sender;

			try
			{
				sender = GridTerminalSystem.GetBlockWithName(name);
				senderData = GetKey(sender, SHARED, "Grid_ID", sender.CubeGrid.EntityId.ToString());
			}
			catch
			{
				senderData = "UNKNOWN";
			}
			_statusMessage = "Origin Grid ID: " + senderData + "\n";
		}


		// LOG BATCH //  Logs multiple pasted terminal coordinates.
		void LogBatch()
		{
			if (_dataSurface == null)
			{
				_statusMessage = "No DATA DISPLAY Screen Designated!";
				return;
			}

			if (_pageIndex != 0)
			{
				_statusMessage = "Please navigate to GPS INPUT page before running LOG_BATCH command.";
			}

			StringBuilder inputText = new StringBuilder();
			_dataSurface.ReadText(inputText, false);
			string[] inputs = inputText.ToString().Split('\n');

			List<string> outputs = new List<string>();

			foreach (string entry in inputs)
			{
				if (entry.StartsWith("GPS:"))
				{
					ClipboardToLog("WAYPOINT", entry);
				}
				else
				{
					outputs.Add(entry);
				}
			}

			string output = "";
			foreach (string item in outputs)
			{
				output += item + "\n";
			}

			_dataSurface.WriteText(output);
		}
	}
}