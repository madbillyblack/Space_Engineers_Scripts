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
		int _currentDisplay;

		// DATA DISPLAY // Wrapper class for blocks that display pressure monitor displays.
		public class DataDisplay
		{
			public IMyTerminalBlock Block;
			public List<DataScreen> Screens;
			public int screenCount;

			BlockIni Ini;

			public DataDisplay(IMyTerminalBlock block)
			{
				Block = block;
				Ini = new BlockIni(Block, MONITOR_HEAD);

				screenCount = (block as IMyTextSurfaceProvider).SurfaceCount;
				Screens = new List<DataScreen>();

				string s = GetKey("Screen_Indices", "0");
				string[] screens = s.Split(',');

				if (screens.Length < 1 || screenCount < 1)
					return;

				for (int i = 0; i < screens.Length; i++)
				{
					ushort index;
					if (UInt16.TryParse(screens[i], out index))
					{
						if (index < screenCount)
						{
							DataScreen screen = new DataScreen(block, (block as IMyTextSurfaceProvider).GetSurface(index), index);
							Screens.Add(screen);
						}
					}
				}
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
		}


		// DATA SCREEN // Wrapper class for individual surfaces belonging to a Monitor
		public class DataScreen
		{
			public List<Sector> Sectors;
			public IMyTerminalBlock ParentBlock;
			public IMyTextSurface Surface;
			public int ScreenIndex;
			public string IniTitle;
			public string Header;
			public bool ShowBuild;
			public bool ShowSectorType;
			public bool ShowSectorStatus;
			public bool ShowLockStatus;
			public bool ShowDoorCount;
			public bool ShowDoorNames;
			public bool ShowDoorStatus;
			public bool ShowVentCount;
			public bool ShowLightCount;
			public bool ShowMergeCount;
			public bool ShowMergeNames;
			public bool ShowMergeStatus;
			public bool ShowConnectorCount;
			public bool ShowConnectorNames;
			public bool ShowConnectorStatus;

			BlockIni Ini;
			public DataScreen(IMyTerminalBlock block, IMyTextSurface surface, int screenIndex)
			{
				Sectors = new List<Sector>();
				ParentBlock = block;
				Surface = surface;
				Surface.ContentType = ContentType.TEXT_AND_IMAGE;
				ScreenIndex = screenIndex;
				
				IniTitle = MONITOR_HEAD + " " + screenIndex;
				Ini = new BlockIni(ParentBlock, IniTitle);
				Header = GetKey("Header", "Basic");

				string sectorIni = GetKey("Sectors", "");
				string[] sectors = sectorIni.Split('\n');

				if (sectors.Length > 0)
				{
					foreach (string sector in sectors)
					{
						Sector newSector = GetSector(sector);
						if (newSector != null)
							Sectors.Add(newSector);
					}
				}

				ShowBuild = ParseBool(GetKey("Show_Build", "False"));
				ShowSectorType = ParseBool(GetKey("Sector_Type", "False"));
				ShowSectorStatus = ParseBool(GetKey("Sector_Status", "True"));
				ShowLockStatus = ParseBool(GetKey("Lock_Status", "False"));
				ShowVentCount = ParseBool(GetKey("Vent_Count", "False"));
				ShowDoorCount = ParseBool(GetKey("Door_Count", "True"));
				ShowDoorNames = ParseBool(GetKey("Door_Names", "False"));
				ShowDoorStatus = ParseBool(GetKey("Door_Status", "False"));
				ShowLightCount = ParseBool(GetKey("Light_Count", "False"));
				ShowMergeCount = ParseBool(GetKey("Merge_Count", "True"));
				ShowMergeNames = ParseBool(GetKey("Merge_Names", "False"));
				ShowMergeStatus = ParseBool(GetKey("Merge_Status", "False"));
				ShowConnectorCount = ParseBool(GetKey("Connector_Count", "True"));
				ShowConnectorNames = ParseBool(GetKey("Connector_Names", "False"));
				ShowConnectorStatus = ParseBool(GetKey("Connector_Status", "False"));
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
		}


		//ASSIGN DATA DISPLAYS// Get and set up blocks and surfaces designated as data displays
		void AssignDataDisplays()
		{
			_currentDisplay = 0;

			IMyBlockGroup dataGroup = GridTerminalSystem.GetBlockGroupWithName(_programIni.GetKey("Data_Group", "Pressure Data"));
			if (dataGroup == null)
			{
				_buildMessage += "\nOptional: No Pressure Data group found.";
				return;
			}


			List<IMyTerminalBlock> dataBlocks = new List<IMyTerminalBlock>();
			dataGroup.GetBlocks(dataBlocks);


			foreach (IMyTerminalBlock dataBlock in dataBlocks)
			{
				if ((dataBlock as IMyTextSurfaceProvider).SurfaceCount > 0 && GetKey(dataBlock, SHARED, "Grid_ID", _gridID) == _gridID)
					_dataDisplays.Add(new DataDisplay(dataBlock));
			}
		}



		// PRINT DATA
		void PrintData()
        {
			Echo(_dataDisplays.Count.ToString());
			if (_dataDisplays.Count < 1)
				return;

			IncrementDataDisplays();
			DataDisplay display = _dataDisplays[_currentDisplay];

			UpdateData(display);
		}


		// INCREMENT DATA DISPLAYS //
		void IncrementDataDisplays()
        {
			if (_dataDisplays.Count < 2)
				return;

			_currentDisplay++;
			if (_currentDisplay >= _dataDisplays.Count)
				_currentDisplay = 0;
        }


		// UPDATE MONITORS // Print Overview text to any blocks in the _dataDisplays list.
		void UpdateData(DataDisplay display)
		{
			if (display.Screens.Count > 0)
			{
				foreach (DataScreen screen in display.Screens)
				{
					string readOut = "";
					switch (screen.Header.ToLower())
					{
						case "breather":
							readOut += "PRESSURE CHIEF " + _breather[_breatherStep] + SLASHES + _overview + "\n";
							break;
						case "full":
							readOut += "PRESSURE CHIEF " + SLASHES + _overview + "\n";
							break;
						case "basic":
							readOut += "PRESSURE CHIEF " + SLASHES;
							break;
						case "blank":
							readOut += SLASHES;
							break;
						case "none":
							break;
						default:
							readOut = screen.Header + "\n";
							break;
					}

					if (screen.ShowBuild)
						readOut += _buildMessage + "\n";

					if (screen.Sectors.Count > 0)
					{
						foreach (Sector sector in screen.Sectors)
						{
							readOut += sector.Name;
							if (screen.ShowSectorType)
								readOut += " (" + sector.Type + ")";
							if (screen.ShowSectorStatus)
								readOut += " - " + sector.Vents[0].Status.ToString();
							readOut += "\n";
							if (screen.ShowVentCount)
								readOut += "* Vents: " + sector.Vents.Count + "\n";
							if (screen.ShowLightCount)
								readOut += "* Lights: " + sector.Lights.Count + "\n";

							// Door Info
							if (screen.ShowDoorCount)
								readOut += "* Doors: " + sector.Doors.Count + "\n";
							if (screen.ShowDoorNames)
							{
								foreach (IMyDoor door in sector.Doors)
								{
									readOut += "   - " + door.CustomName;
									if (screen.ShowDoorStatus)
									{
										readOut += ": " + door.Status.ToString();
										if (door.IsWorking)
											readOut += ", Unlocked";
										else
											readOut += ", Locked";
									}

									readOut += "\n";
								}
							}

							bool isDock = sector.Type.ToUpper() == DOCK;

							// Connector Info
							if (screen.ShowConnectorCount && isDock)
								readOut += "* Connectors: " + sector.Connectors.Count + "\n";
							if (screen.ShowConnectorNames && sector.Connectors.Count > 0)
							{
								foreach (IMyShipConnector connector in sector.Connectors)
								{
									readOut += "   - " + connector.CustomName;
									if (screen.ShowConnectorStatus)
										readOut += ": " + connector.Status.ToString();
									readOut += "\n";
								}
							}

							// Merge Info
							if (screen.ShowMergeCount && isDock)
								readOut += "* Merge Blocks: " + sector.MergeBlocks.Count + "\n";
							if (screen.ShowMergeNames && sector.MergeBlocks.Count > 0)
							{
								foreach (IMyShipMergeBlock mergeBlock in sector.MergeBlocks)
								{
									readOut += "   - " + mergeBlock.CustomName;
									if (screen.ShowMergeStatus)
									{
										string mergeStatus;
										bool merged = mergeBlock.IsConnected;
										if (merged)
											mergeStatus = "Connected";
										else
											mergeStatus = "Not Connected";

										readOut += ": " + mergeStatus;
									}
									readOut += "\n";
								}
							}
						}
					}

					screen.Surface.WriteText(readOut);
				}
			}
		}
	}
}
