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
		const string SYNC_TAG = "[Map Sync]"; //Tag used to indicate master sync computer.
		const string IMPORT_TAG = "[IMPORT]";

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
				AddMessage("Invalid Sync Command!");
			}
		}


		// SYNC MASTER // Finds Master Sync Block on station and syncs to or from depending on bool.
		void syncMaster(bool syncTo)
		{

			if (Me.CustomName.Contains(SYNC_TAG))
			{
				AddMessage("SYNC Requests cannot be made from SYNC terminal!");
				return;
			}

			List<IMyTerminalBlock> syncBlocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName(SYNC_TAG, syncBlocks);

			if (syncBlocks.Count < 1)
			{
				AddMessage("NO MAP MASTER FOUND.\nPlease add tag '" + SYNC_TAG + "' to the map computer's name on your station or capital ship.");
				return;
			}

			if (syncBlocks.Count > 1)
			{
				AddMessage("Multiple blocks found with tag '" + SYNC_TAG + "'! Please resolve conflict before syncing.");
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

			AddMessage("No other mapping computers available to sync!");
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

			AddMessage("MAP DATA SYNCED\n-- Planets --\nDownloaded: " + pSync[0] + "\nUploaded: " + pSync[1] + "\n\n--Waypoints--\nDownloaded: " + wSync[0] + "\nUploaded: " + wSync[1]);
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
				downUp[1] = dataB.Split('\n').Length;
			}
			else if (dataB == "")
			{
				newData = dataA;
				downUp[0] = dataA.Split('\n').Length;
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

			AddMessage("SYNC Block '" + sync.CustomName + "' contains no map settings! Please ensure that SYNC Block is also running this script!");
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
			AddMessage("Origin Grid ID: " + senderData);
		}


		// LOG BATCH //  Logs multiple pasted terminal coordinates.
		void LogBatch(string arg)
		{
			int number = ParseInt(arg, 0);
			DataDisplay display = GetDataDisplay(number);

			if (display.Surface == null)
			{
				AddMessage("No DATA DISPLAY Screen Designated!");
				return;
			}

			if (display.CurrentPage != INPUT_PAGE)
			{
				AddMessage("Please navigate to GPS INPUT page before running LOG_BATCH command.");
				return;
			}

			ImportCoordinates(display.Surface);

			display.Surface.WriteText(display.BuildPageHeader(GPS_INPUT) + BELOW_LINE);
		}


		// IMPORT FROM LCDS //
		void ImportFromLCDs(string type)
        {
			List<IMyTextPanel> lcds = new List<IMyTextPanel>();
			GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcds);

			if (lcds.Count < 1)
				return;

			foreach (IMyTextPanel lcd in lcds)
				if (lcd.CustomName.ToUpper().Contains(IMPORT_TAG))
					ImportCoordinates(lcd, type);
        }


		void ImportCoordinates(IMyTextSurface surface, string type="WAYPOINT")
        {
			StringBuilder inputText = new StringBuilder();
			surface.ReadText(inputText, false);
			string[] inputs = inputText.ToString().Split('\n');

			List<string> outputs = new List<string>();

			foreach (string entry in inputs)
				if (entry.Contains("GPS:"))
					ClipboardToLog(type, entry);
		}
	}
}
