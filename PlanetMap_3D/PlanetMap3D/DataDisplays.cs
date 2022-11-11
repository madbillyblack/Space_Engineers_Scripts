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
		const string PLANET_TITLE = "PLANETS";
		const string WAYPOINT_TITLE = "WAYPOINTS";
		const string GPS_INPUT = "GPS INPUT";
		const int PAGE_LIMIT = 3;
		const int DATA_PAGES = 4;  // Number of Data Display Pages
		//static List<string> _dataPage0;
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
			
			// Constructor //
			public DataDisplay(IMyTerminalBlock block)
            {
				Owner = block;
				ScreenIndex = ParseInt(GetKey(block, DATA_HEADER, SCREEN_KEY, "0"), 0);
				CurrentPage = ParseInt(GetKey(block, DATA_HEADER, DATA_PAGE, "0"), 0);
				ScrollIndex = ParseInt(GetKey(block, DATA_HEADER, DATA_SCROLL, "0"), 0);

                try
                {
					Surface = (Owner as IMyTextSurfaceProvider).GetSurface(ScreenIndex);
                }
				catch
                {
					Surface = null;
					_statusMessage += "Screen Index Error for Data Display on\n\"" + Owner.CustomName + "\"\n";
                }
			}

			// Set Scroll //
			void SetScroll(int scroll)
            {
				ScrollIndex = scroll;

				if (ScrollIndex < 0)
					ScrollIndex = 0;

				SetKey(Owner, DATA_HEADER, DATA_SCROLL, ScrollIndex.ToString());
				DisplayData(this);
			}

			// Scroll Down //
			public void ScrollDown()
            {
				SetScroll(ScrollIndex++);	
			}

			// Scroll Up
			public void ScrollUp()
            {
				SetScroll(ScrollIndex--);
            }

			// Set Page //
			void SetPage(int page)
            {
				SetScroll(0);
				CurrentPage = page;

				if (CurrentPage > PAGE_LIMIT)
					CurrentPage = 0;
				else if (CurrentPage < 0)
					CurrentPage = PAGE_LIMIT;

				SetKey(Owner, DATA_HEADER, DATA_PAGE, CurrentPage.ToString());
				DisplayData(this);
			}

			// Next Page //
			public void NextPage()
            {
				SetPage(CurrentPage++);
            }

			// Previous Page //
			public void PreviousPage()
            {
				SetPage(CurrentPage--);
            }
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
			InitializeData();

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
		void InitializeData()
        {
			// Initialize Data Display Lists
			//_dataPage0 = new List<string>();
			_planetDataPage = new List<string>();
			_waypointDataPage = new List<string>();
			_mapDataPage = new List<string>();
			//_dataPage4 = new List<string>();

			//UpdateMapDataPage();
			UpdatePlanetDataPage();
			UpdateWaypointDataPage();
		}


		// UPDATE MAP DATA PAGE //
		static void UpdateMapDataPage()
        {
			//_dataPage0.Add("// MAP DATA" + SLASHES);

			if (_mapList.Count < 1)
				return;

			foreach(StarMap map in _mapList)
            {
				_mapDataPage.Add("Map " + map.Number + "\n * Size: " + map.Viewport.Width + " x " + map.Viewport.Height);
            }
		}



		// UPDATE PLANET DATA PAGE //
		void UpdatePlanetDataPage()
        {
			if (!_planets)
				return;

			foreach(Planet planet in _planetList)
            {
				_planetDataPage.Add(planet.name + "\n * Radius: " + (planet.radius / 1000).ToString("N1") + "km\n * Dist: " + (planet.Distance / 1000).ToString("N1") + " km\n");
            }
        }


		// UPDATE WAYPOINT DATA PAGE //
		void UpdateWaypointDataPage()
        {
			if (_waypointList.Count < 1)
				return;

			foreach(Waypoint waypoint in _waypointList)
            {
				_waypointDataPage.Add(waypoint.name + "\n * Type: " + waypoint.marker + "\n * Dist: " + (waypoint.Distance / 1000).ToString("N1") + " km\n");
            }
        }


		// GET DATA DISPLAY //
		static DataDisplay GetDataDisplay(int displayID)
        {
			if(displayID >= _dataDisplays.Count || displayID < 0)
				return null;

			return _dataDisplays[displayID];
        }


		// DISPLAY DATA // Write current data to individual data display depending on current page
		static void DisplayData(DataDisplay display)
		{
			switch (display.CurrentPage)
			{
				case 0:
					DisplayPlanetData(display);
					break;
				case 1:
					DisplayWaypointData(display);
					break;
				case 2:
					DisplayGPSInput(display);
					break;
				case 3:
					DisplayClipboard(display);
					break;/*
				case 4:
					DisplayMapData(display.Surface);
					break;*/
			}
		}


		// UPDATE DISPLAYS // Write current data to all Data Displays
		void UpdateDisplays()
        {
			if (_dataDisplays.Count < 1)
				return;

			foreach (DataDisplay display in _dataDisplays)
				DisplayData(display);
		}


		// DISPLAY GPS INPUT //
		static void DisplayGPSInput(DataDisplay display)
		{
			if (_cycleStep > 0)
				return;

			StringBuilder menuText = new StringBuilder();
			display.Surface.ReadText(menuText, false);

			string output = menuText.ToString();

			if (!output.Contains(GPS_INPUT))
				output = BuildPageHeader(display, GPS_INPUT) + "~Input Terminal Coords Below This Line~";

			display.Surface.WriteText(output);
		}


		// DISPLAY PLANET DATA // - Writes Planet Title and Data to Display Surface
		static void DisplayPlanetData(DataDisplay display)
		{
			display.Surface.WriteText(BuildPageHeader(display, PLANET_TITLE) + GetScrolledPage(display, _planetDataPage));

			// old code
			#region
			/*
			string output = "// PLANET LIST" + SLASHES;
			List<string> planetData = new List<string>();
			planetData.Add("Charted: " + _planetList.Count + "	  Uncharted: " + _unchartedList.Count);

			if (_planets)
			{
				foreach (Planet planet in _planetList)
				{
					float surfaceDistance = (Vector3.Distance(planet.position, _myPos) - planet.radius) / 1000;
					if (surfaceDistance < 0)
					{
						surfaceDistance = 0;
					}
					string planetEntry;
					if (planet.name.ToUpper() == _activePlanet.ToUpper())
					{
						planetEntry = ">>> ";
					}
					else
					{
						planetEntry = "		   ";
					}

					planetEntry += planet.name + "	  R: " + abbreviateValue(planet.radius) + "m    dist: " + surfaceDistance.ToString("N1") + "km";

					planetData.Add(planetEntry);
				}
			}

			if (_unchartedList.Count > 0)
			{
				string unchartedHeader = "\n----Uncharted Planets----";
				planetData.Add(unchartedHeader);
				foreach (Planet uncharted in _unchartedList)
				{
					float unchartedDistance = Vector3.Distance(_myPos, uncharted.GetPoint(1)) / 1000;

					string unchartedEntry = "  " + uncharted.name + "	 dist: " + unchartedDistance.ToString("N1") + "km";

					planetData.Add(unchartedEntry);
				}
			}

			output += ScrollToString(planetData);


			surface.WriteText(output);
			*/
			#endregion
		}


		// DISPLAY WAYPOINT DATA //
		static void DisplayWaypointData(DataDisplay display)
		{
			display.Surface.WriteText(BuildPageHeader(display, WAYPOINT_TITLE) + GetScrolledPage(display, _waypointDataPage));
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


		// DISPLAY MAP DATA //
		/*
        void DisplayMapData(IMyTextSurface surface)
		{
			string output = "// MAP DATA" + SLASHES;

			if (_statusMessage != "")
				output += "\n" + _statusMessage;

			if (_mapBlocks.Count > 0)
			{
				List<string> mapData = new List<string>();
				foreach (StarMap map in _mapList)
				{
					mapData.Add("MAP " + map.Number + " --- " + map.Viewport.Width.ToString("N0") + " x " + map.Viewport.Height.ToString("N0") + " --- " + map.Mode + " Mode");
					mapData.Add("   On: " + map.Block.CustomName + "   Screen: " + map.Index);

					if (map.ActivePlanetName != "")
						mapData.Add("   Selected Planet: " + map.ActivePlanetName);

					string hidden = "   Hidden:";
					if (!map.ShowInfo)
						hidden += " Stat-Bars ";

					if (map.GpsState == 0)
						hidden += " GPS ";

					if (!map.ShowNames)
						hidden += " Names ";

					if (!map.ShowShip)
						hidden += " Ship";

					if (map.GpsState == 0 || !map.ShowInfo || !map.ShowShip && !map.ShowNames)
						mapData.Add(hidden);

					mapData.Add("   Center: " + Vector3ToString(map.Center));
					mapData.Add("   Azimuth: " + map.Azimuth + "°   Altitude: " + map.Altitude * -1 + "°");
					mapData.Add("   Focal Length: " + abbreviateValue(map.FocalLength) + "   Radius: " + abbreviateValue(map.RotationalRadius) + "\n");
				}

				output += ScrollToString(mapData);
			}
			else
			{
				output += "\n\n NO MAP BLOCKS TO DISPLAY!!!";
			}

			surface.WriteText(output);
		}
		*/


		// DISPLAY CLIPBOARD //
		static void DisplayClipboard(DataDisplay display)
		{
			display.Surface.WriteText(BuildPageHeader(display, "CLIPBOARD") + "\n" + _clipboard);

			//string output = "// CLIPBOARD" + SLASHES + "\n\n" + _clipboard;
			//surface.WriteText(output);
		}


		// SCROLL TO STRING // Returns string from List based on ScrollIndex
		string ScrollToString(List<string> dataList)
		{
			string output = "";

			if (dataList.Count > 0 && _scrollIndex < dataList.Count)
			{
				for (int d = _scrollIndex; d < dataList.Count; d++)
				{
					output += "\n" + dataList[d];
				}
			}

			return output;
		}


		// NEXT PAGE //
		void NextPage(bool next)
		{
			if (next)
			{
				_pageIndex++;
				if (_pageIndex >= DATA_PAGES)
					_pageIndex = 0;
				return;
			}

			if (_pageIndex == 0)
				_pageIndex = DATA_PAGES;

			_pageIndex--;
		}


		// PAGE SCROLL //
		void pageScroll(string arg)
		{
			switch (arg)
			{
				case "UP":
					_scrollIndex--;
					if (_scrollIndex < 0)
						_scrollIndex = 0;
					break;
				case "DOWN":
					_scrollIndex++;
					break;
				case "HOME":
					_scrollIndex = 0;
					break;
				default:
					_statusMessage = "INVALID SCROLL COMMAND";
					break;
			}
		}


		// WRITE PAGE HEADER //
		static string BuildPageHeader(DataDisplay display, string pageTitle)
        {
			string displayNumber = "";

			if(_dataDisplays.Count > 1)
				displayNumber = "[" + display.IDNumber + "] ";

			return "// " + displayNumber + pageTitle + SLASHES + "\n";
        }


		// GET SCROLLED PAGE // -  Return page data from string list, based on display's scroll index
		static string GetScrolledPage(DataDisplay display, List<string> entries)
        {
			string output = "";

			if(entries.Count > 0)
            {
				// If display is scrolled past the end of the page, set it to the end of the page
				if (display.ScrollIndex >= entries.Count)
                {
					display.ScrollIndex = entries.Count - 1;
					SetKey(display.Owner, DATA_HEADER, DATA_SCROLL, display.ScrollIndex.ToString());
				}

				for (int i = display.ScrollIndex; i < entries.Count; i++)
					output += entries[i] + "\n\n";
			}

			return output;
        }
	}
}
