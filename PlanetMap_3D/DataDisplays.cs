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
		const string DATA_TAG = "[Map Data]";
		const string DATA_HEADER = "Data Display";
		const string DATA_SCREENS = "Data Display Screens";
		const string DATA_PAGE = "Current Page";
		const string DATA_SCROLL = "Scroll Level";
		const string SCREEN_KEY = "Screen Index";
		const string SYSTEM_TITLE = "SYSTEM DATA";
		const string PLANET_TITLE = "PLANETS";
		const string WAYPOINT_TITLE = "WAYPOINTS";
		const string GPS_INPUT = "GPS INPUT";
		const string BELOW_LINE = "~Input Terminal Coords Below This Line~";

		// DATA PAGE CONSTANTS
		const int PAGE_LIMIT = 5;
		const int SYSTEM_PAGE = 0;
		const int PLANET_PAGE = 1;
		const int WAYPOINT_PAGE = 2;
		const int INPUT_PAGE = 3;
		const int CLIPBOARD_PAGE = 4;
		const int MAP_PAGE = 5;

		//const int DATA_PAGES = 6;  // Number of Data Display Pages
		static List<string> _systemDataPage;
		static List<string> _planetDataPage;
		static List<string> _waypointDataPage;
		static List<string> _mapDataPage;
		//List<string> _dataPage4;
		static List<DataDisplay> _dataDisplays;


		// DATA DISPLAY //
		public class DataDisplay
        {
			public int CurrentPage;
			public int ScrollIndex;
			int ScreenIndex;
			public int IDNumber;
			public IMyTerminalBlock Owner;
			public IMyTextSurface Surface;
			public string Header;
			//string ActivePlanetName;
			//string ActiveWaypointName;
			
			// Constructor //
			public DataDisplay(IMyTerminalBlock block, int screenNumber)
            {
				Owner = block;
				ScreenIndex = screenNumber;
				
				//ScreenIndex = ParseInt(GetKey(block, DATA_HEADER, SCREEN_KEY, "0"), 0);

				Header = DATA_HEADER;
				if ((block as IMyTextSurfaceProvider).SurfaceCount > 1)
					Header += " - Screen " + ScreenIndex;

				CurrentPage = ParseInt(GetKey(block, Header, DATA_PAGE, "0"), 0);
				ScrollIndex = ParseInt(GetKey(block, Header, DATA_SCROLL, "0"), 0);

				//ActivePlanetName = "";
				//ActiveWaypointName = "";
				
                try
                {
					Surface = (Owner as IMyTextSurfaceProvider).GetSurface(ScreenIndex);
                }
				catch
                {
					Surface = null;
					AddMessage("Screen Index Error for Data Display on\n\"" + Owner.CustomName + "\"");
                }
			}

			// Scroll Down //
			public void ScrollDown()
            {
				//SetScroll(ScrollIndex++);	
				ScrollIndex++;
				DisplayPage(true);
			}

			// Scroll Up
			public void ScrollUp()
            {
				if(ScrollIndex > 0)
					ScrollIndex--;

				DisplayPage(true);

				//SetScroll(ScrollIndex--);
            }

			// Next Page //
			public void NextPage()
            {
				CurrentPage++;
				if (CurrentPage > PAGE_LIMIT)
					CurrentPage = 0;

				SetKey(Owner, Header, DATA_PAGE, CurrentPage.ToString());

				ScrollIndex = 0;
				SetKey(Owner, Header, DATA_SCROLL, "0");

				DisplayPage(true);
			}

			// Previous Page //
			public void PreviousPage()
            {
				CurrentPage--;
				if (CurrentPage < 0)
					CurrentPage = PAGE_LIMIT;

				SetKey(Owner, Header, DATA_PAGE, CurrentPage.ToString());

				ScrollIndex = 0;
				SetKey(Owner, Header, DATA_SCROLL, "0");

				DisplayPage(true);
			}

			// WRITE PAGE HEADER //
			public string BuildPageHeader(string pageTitle)
			{
				string displayNumber = "";

				if (_dataDisplays.Count > 1)
					displayNumber = "[" + IDNumber + "] ";

				return "// " + displayNumber + pageTitle + SLASHES + "\n";
			}

			// GET SCROLLED PAGE // -  Return page data from string list, based on display's scroll index
			string GetScrolledPage(List<string> entries)
			{
				string output = "";

				if (entries.Count > 0)
				{
					// If display is scrolled past the end of the page, set it to the end of the page
					if (ScrollIndex >= entries.Count)
					{
						ScrollIndex = entries.Count - 1;
						SetKey(Owner, Header, DATA_SCROLL, ScrollIndex.ToString());
					}

					for (int i = ScrollIndex; i < entries.Count; i++)
						output += entries[i] + "\n\n";
				}

				return output;
			}

			// DISPLAY STAT DATA //
			void DisplaySystemData()
            {
				Surface.WriteText(BuildPageHeader(SYSTEM_TITLE) + GetScrolledPage(_systemDataPage));
            }


			// DISPLAY PLANET DATA // - Writes Planet Title and Data to Display Surface
			void DisplayPlanetData()
			{
				Surface.WriteText(BuildPageHeader(PLANET_TITLE) + GetScrolledPage(_planetDataPage));
			}

			// DISPLAY WAYPOINT DATA //
			void DisplayWaypointData()
			{
				Surface.WriteText(BuildPageHeader(WAYPOINT_TITLE) + GetScrolledPage(_waypointDataPage));
				// old code
				#region
				/*
				string output = "// WAYPOINT LIST" + SLASHES;

				List<string> waypointData = new List<string>();
				waypointData.Add("Count: " + _waypointList.Count);

				if (_waypointList.Count > 0)
				{
					foreach (Waypoint waypoint in _waypointList)
					{
						float distance = Vector3.Distance(_myPos, waypoint.position) / 1000;
						string status = "Active";
						if (!waypoint.isActive)
						{
							status = "Inactive";
						}
						string waypointEntry = "		";
						if (waypoint.name.ToUpper() == _activeWaypoint.ToUpper())
						{
							waypointEntry = ">>> ";
						}
						waypointEntry += waypoint.name + "	  " + waypoint.marker + "	  " + status + "	  dist: " + distance.ToString("N1") + "km";
						waypointData.Add(waypointEntry);
					}
				}

				output += ScrollToString(waypointData);

				surface.WriteText(output);
				*/
				#endregion
			}

			// DISPLAY GPS INPUT //
			public void DisplayGPSInput()
			{
				/*
				if (_cycleStep > 0)
					return;*/

				// Gets previous displayed text
				StringBuilder menuText = new StringBuilder();
				Surface.ReadText(menuText, false);

				string output = menuText.ToString();

				string title = BuildPageHeader(GPS_INPUT);

				if (!(output.StartsWith(title)))
					output = title + BELOW_LINE;

				Surface.WriteText(output);
			}

			// DISPLAY CLIPBOARD //
			void DisplayClipboard()
			{
				Surface.WriteText(BuildPageHeader("CLIPBOARD") + "\n" + _clipboard);
			}


			// DISPLAY MAP DATA //
			void DisplayMapData()
            {
				Surface.WriteText(BuildPageHeader("MAP SCREENS") + GetScrolledPage(_mapDataPage));
            }


			// DISPLAY DATA // Write current data to individual data display depending on current page
			public void DisplayPage(bool fromCommand)
			{
				switch (CurrentPage)
				{
					case SYSTEM_PAGE:
						DisplaySystemData();
						break;
					case PLANET_PAGE:
						DisplayPlanetData();
						break;
					case WAYPOINT_PAGE:
						DisplayWaypointData();
						break;
					case INPUT_PAGE:
						if(fromCommand)
							DisplayGPSInput();
						break;
					case CLIPBOARD_PAGE:
						if (fromCommand)
								DisplayClipboard();
						break;
					case MAP_PAGE:
						if (fromCommand)
							DisplayMapData();
						break;
				}
			}


			/*
			// SET ACTIVE WAYPOINT //
			public void SetActiveWaypoint(string waypointName)
            {
				ActivePlanetName = "";
				ActiveWaypointName = waypointName;
            }

			// Set Active Planet //
			public void SetActivePlanet(string planetName)
            {
				ActiveWaypointName = "";
				ActivePlanetName = planetName;
            }
			*/
		}


		// ASSIGN DATA DISPLAYS //
		void AssignDataDisplays()
        {
			_dataDisplays = new List<DataDisplay>();
			//Echo("Assigning Data Displays");

			// Get Tagged Display Blocks
			List<IMyTerminalBlock> displayBlocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName(DATA_TAG, displayBlocks);

			if (displayBlocks.Count < 1)
				return;

			// Initialize data pages and populate lists.
			UpdatePageData();

			foreach(IMyTerminalBlock block in displayBlocks)
            {
				if(OnGrid(block))
                {
					AddScreensFromData(block);

					/*
					DataDisplay display = new DataDisplay(block);

					if(display.Surface != null)
                    {
						display.IDNumber = _dataDisplays.Count;
						display.Surface.ContentType = ContentType.TEXT_AND_IMAGE;
						_dataDisplays.Add(display);
                    }*/
                }
            }

			//Echo("Data Displays: " + _dataDisplays.Count);
        }


		// ADD SCREENS FROM DATA //
		void AddScreensFromData(IMyTerminalBlock block)
        {
			int screenCount = (block as IMyTextSurfaceProvider).SurfaceCount;

			if (screenCount < 1)
            {
				AddMessage("Block \"" + block.CustomName + "\" contains no surfaces for Data Display!");
				return;
			}
			else if (screenCount == 1)
            {
				AddDataDisplay(block, 0);
            }
			else
            {
				// Multi-screen INI Bool Selector
				string defaultBool = "True";

				for(int i = 0; i < screenCount; i++)
                {
					if (ParseBool(GetKey(block, DATA_SCREENS, "Show on Screens " + i, defaultBool)))
						AddDataDisplay(block, i);

					// Set default bool to FALSE for all but screen 0.
					defaultBool = "False";
                }
            }
        }


		// ADD DATA DISPLAY //
		void AddDataDisplay(IMyTerminalBlock block, int screenNumber)
        {
			DataDisplay display = new DataDisplay(block, screenNumber);

			if (display.Surface != null)
			{
				display.IDNumber = _dataDisplays.Count;
				display.Surface.ContentType = ContentType.TEXT_AND_IMAGE;
				_dataDisplays.Add(display);
			}
		}


		// INITIALIZE DATA //
		void UpdatePageData()
        {
			// Initialize Data Display Lists
			UpdateSystemDataPage();
			UpdatePlanetDataPage();
			UpdateWaypointDataPage();
		}


		// UPDATE MAP DATA PAGE //
		static void UpdateMapDataPage()
		{
			_mapDataPage = new List<string>();

			if (_mapList.Count < 1)
					return;

			foreach(StarMap map in _mapList)
			{
				_mapDataPage.Add("Map " + map.Number + " --- " + map.Viewport.Width + " x " + map.Viewport.Height
							+ "\n * Block: " + map.Block.CustomName
							+ "\n * Screen: " + map.Index);
			}
		}


		// UPDATE SYSTEM DATA PAGE //
		void UpdateSystemDataPage()
        {
			_systemDataPage = new List<string>();
			_systemDataPage.Add("  Command: " + _previousCommand);
			_systemDataPage.Add("  Maps: " + _mapList.Count + " -- Data Screens: " + _dataDisplays.Count + " -- Menus: " + _mapMenus.Count);
			_systemDataPage.Add("  Planets: " + _planetList.Count + " -- Waypoints: " + _waypointList.Count);
			
			_systemDataPage.Add("Messages:");

			if (_messages.Count < 0)
				return;

			for (int i = _messages.Count - 1; i > -1; i--)
				_systemDataPage.Add(_messages[i]);
        }


		// UPDATE PLANET DATA PAGE //
		void UpdatePlanetDataPage()
        {
			if (!_planets)
				return;
			_planetDataPage = new List<string>();


			foreach (Planet planet in _planetList)
            {
				_planetDataPage.Add(planet.name + "\n * Radius: " + (planet.radius / 1000).ToString("N1") + "km -- Dist: " + (planet.Distance / 1000).ToString("N1") + " km");
            }
        }


		// UPDATE WAYPOINT DATA PAGE //
		void UpdateWaypointDataPage()
        {
			_waypointDataPage = new List<string>();

			if (_waypointList.Count < 1)
				return;

			foreach (Waypoint waypoint in _waypointList)
            {
				_waypointDataPage.Add(waypoint.name + " --- " + waypoint.marker + "\n * Dist: " + (waypoint.Distance / 1000).ToString("N1") + " km");
            }
        }


		// GET DATA DISPLAY //
		static DataDisplay GetDataDisplay(int displayID)
        {
			if(displayID >= _dataDisplays.Count || displayID < 0)
				return null;

			return _dataDisplays[displayID];
        }


		// DATA DISPLAY FROM STRING //
		DataDisplay DataDisplayFromString(string dataID)
        {
			int index;

			if (dataID == "")
				index = 0;
			else
				index = ParseInt(dataID, int.MaxValue);

			return GetDataDisplay(index);
		}


		// NEXT DATA PAGE //
		void NextDataPage(string dataID, bool next = true)
        {
			DataDisplay display = DataDisplayFromString(dataID);

			if (display == null)
				return;

			if (next)
				display.NextPage();
			else
				display.PreviousPage();
        }


		// SCROLL DATA //
		void ScrollData(string direction, string dataID)
        {
			DataDisplay display = DataDisplayFromString(dataID);

			if (display == null)
				return;

			if (direction.ToUpper() == "UP")
				display.ScrollUp();
			else
				display.ScrollDown();
		}


		// UPDATE DISPLAYS // Write current data to all Data Displays
		void UpdateDisplays(bool fromCommand = false)
        {
			if (_dataDisplays.Count < 1)
				return;

			foreach (DataDisplay display in _dataDisplays)
				display.DisplayPage(fromCommand);
		}
	}
}
