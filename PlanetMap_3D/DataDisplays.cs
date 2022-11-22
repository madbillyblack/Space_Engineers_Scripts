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
		const string DATA_PAGE = "Current Page";
		const string DATA_SCROLL = "Scroll Level";
		const string SCREEN_KEY = "Screen Index";
		const string SYSTEM_TITLE = "SYSTEM DATA";
		const string PLANET_TITLE = "PLANETS";
		const string WAYPOINT_TITLE = "WAYPOINTS";
		const string GPS_INPUT = "GPS INPUT";
		
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
			//string ActivePlanetName;
			//string ActiveWaypointName;
			
			// Constructor //
			public DataDisplay(IMyTerminalBlock block)
            {
				Owner = block;
				ScreenIndex = ParseInt(GetKey(block, DATA_HEADER, SCREEN_KEY, "0"), 0);
				CurrentPage = ParseInt(GetKey(block, DATA_HEADER, DATA_PAGE, "0"), 0);
				ScrollIndex = ParseInt(GetKey(block, DATA_HEADER, DATA_SCROLL, "0"), 0);

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
				DisplayPage();
			}

			// Scroll Up
			public void ScrollUp()
            {
				if(ScrollIndex > 0)
					ScrollIndex--;

				DisplayPage();

				//SetScroll(ScrollIndex--);
            }

			// Next Page //
			public void NextPage()
            {
				CurrentPage++;
				if (CurrentPage > PAGE_LIMIT)
					CurrentPage = 0;

				SetKey(Owner, DATA_HEADER, DATA_PAGE, CurrentPage.ToString());

				ScrollIndex = 0;
				SetKey(Owner, DATA_HEADER, DATA_SCROLL, "0");

				DisplayPage();
			}

			// Previous Page //
			public void PreviousPage()
            {
				CurrentPage--;
				if (CurrentPage < 0)
					CurrentPage = PAGE_LIMIT;

				SetKey(Owner, DATA_HEADER, DATA_PAGE, CurrentPage.ToString());

				ScrollIndex = 0;
				SetKey(Owner, DATA_HEADER, DATA_SCROLL, "0");

				DisplayPage();
			}

			// WRITE PAGE HEADER //
			string BuildPageHeader(string pageTitle)
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
						SetKey(Owner, DATA_HEADER, DATA_SCROLL, ScrollIndex.ToString());
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
					output = title + "~Input Terminal Coords Below This Line~";

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
			public void DisplayPage()
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
						DisplayGPSInput();
						break;
					case CLIPBOARD_PAGE:
						DisplayClipboard();
						break;
					case MAP_PAGE:
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
			Echo("Assigning Data Displays");

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
					DataDisplay display = new DataDisplay(block);

					if(display.Surface != null)
                    {
						display.IDNumber = _dataDisplays.Count;
						display.Surface.ContentType = ContentType.TEXT_AND_IMAGE;
						_dataDisplays.Add(display);
                    }
                }
            }

			Echo("Data Displays: " + _dataDisplays.Count);
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
			if (_waypointList.Count < 1)
				return;

			_waypointDataPage = new List<string>();

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


		// UPDATE DISPLAYS // Write current data to all Data Displays
		void UpdateDisplays()
        {
			if (_dataDisplays.Count < 1)
				return;

			foreach (DataDisplay display in _dataDisplays)
				display.DisplayPage();
		}
	}
}
