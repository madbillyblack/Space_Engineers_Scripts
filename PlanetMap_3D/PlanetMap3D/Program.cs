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
	partial class Program : MyGridProgram
	{
		// This file contains your actual script.
		//
		// You can either keep all your code here, or you can create separate
		// code files to make your program easier to navigate while coding.
		//
		// In order to add a new utility class, right-click on your project, 
		// select 'New' then 'Add Item...'. Now find the 'Space Engineers'
		// category under 'Visual C# Items' on the left hand side, and select
		// 'Utility Class' in the main area. Name it in the box below, and
		// press OK. This utility class will be merged in with your code when
		// deploying your final script.
		//
		// You can also simply create a new utility class manually, you don't
		// have to use the template if you don't want to. Just do so the first
		// time to see what a utility class looks like.
		// 
		// Go to:
		// https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
		//
		// to learn more about ingame scripts.



		// START HERE //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		//////////////////////
		// PLANET MAP 3D //
		///////////////////// 1.3.0

		// USER CONSTANTS //  Feel free to alter these as needed.

		// Ship
		const int SHIP_RED = 127;   //Red RGB value for ship pointer
		const int SHIP_GREEN = 127; //Green RGB value for ship pointer
		const int SHIP_BLUE = 192;  //Blue RGB value for ship pointer
		const int SHIP_SCALE = 24;

		// Planets
		const float DIAMETER_MIN = 6; //Minimum Diameter For Distant Planets
		const int HASH_LIMIT = 125; //Minimum diameter size to print Hashmarks on planet.

		// Waypoints
		const float JUMP_RATIO = 2; // Ratio of distance from center to radius for viable jump points
		const int MARKER_WIDTH = 8; // Width of GPS Markers
		const int FOCAL_MOD = 250; // Mod for waypoint scale

		// View Controls
		const int ANGLE_STEP = 5; // Basic angle in degrees of step rotations.
		const int MAX_PITCH = 90; // Maximum (+/-) value of map pitch. [Not recommended above 90]
		const int MOVE_STEP = 5000; // Step size for translation (move) commands.
		const float ZOOM_STEP = 1.5f; // Factor By which map is zoomed in and out (multiplied).
		const int ZOOM_MAX = 1000000000; // Max value for Focal Length

		// View Defaults
		const int DV_RADIUS = 262144; //Default View Radius
		const int DV_FOCAL = 256; //Default Focal Length
		const int DV_ALTITUDE = -15; //Default Altitude (angle)
		const int BRIGHTNESS_LIMIT = 4;


		// THERE IS NO REASON TO ALTER ANYTHING BELOW THIS LINE! //////////////////////////////////////////////////////////////////////////////////////////////////////////


		// OTHER CONSTANTS //
		const string SYNC_TAG = "[SYNC]"; //Tag used to indicate master sync computer.
		const int BAR_HEIGHT = 20; //Default height of parameter bars
		const int TOP_MARGIN = 8; // Margin for top and bottom of frame
		const int SIDE_MARGIN = 15; // Margin for sides of frame
		const int MAX_VALUE = 1073741824; //General purpose MAX value = 2^30
		const int DATA_PAGES = 5;  // Number of Data Display Pages
		const string SLASHES = " //////////////////////////////////////////////////////////////";
		const string GPS_INPUT = "// GPS INPUT ";
		const string DEFAULT_SETTINGS = "[Map Settings]\nMAP_Tag=[MAP]\nMAP_Index=0\nData_Tag=[Map Data]\nGrid_ID=\nData_Index=0\nReference_Name=[Reference]\nSlow_Mode=false\nCycle_Step=5\nPlanet_List=\nWaypoint_List=\n";
		const string PROGRAM_HEAD = "Map Settings";
		
		/*
		 string _defaultDisplay = "[mapDisplay]\nGrid_ID=\nCenter=(0,0,0)\nMode=FREE\nFocalLength="
									+ DV_FOCAL + "\nRotationalRadius=" + DV_RADIUS + "\nAzimuth=0\nAltitude="
									+ DV_ALTITUDE + "\nIndexes=\ndX=0\ndY=0\ndZ=0\ndAz=0\nGPS=True\nNames=True\nShip=True\nInfo=True\nPlanet=";
		*/


		string[] _cycleSpinner = { "--", " / ", " | ", " \\ " };



		// GLOBALS //
		MyIni _mapLog = new MyIni();
		string _mapTag;
		string _refName;
		string _dataName;
		string _previousCommand;
		int _mapIndex;
		int _dataIndex;
		int _pageIndex;
		int _scrollIndex = 0;
		int _azSpeed;
		int _planetIndex;
		bool _gpsActive;
		bool _showMapParameters;
		bool _showShip;
		bool _showNames;
		bool _lightOn;
		bool _planets;
		bool _planetToLog;
		bool _slowMode = false;
		int _cycleLength;
		int _cycleStep;
		int _sortCounter = 0;
		float _brightnessMod;
		static string _statusMessage;
		string _activePlanet = "";
		string _activeWaypoint = "";
		string _clipboard = "";
		Vector3 _trackSpeed;
		Vector3 _origin = new Vector3(0, 0, 0);
		Vector3 _myPos;
		List<IMyTerminalBlock> _mapBlocks;
		List<IMyTerminalBlock> _dataBlocks = new List<IMyTerminalBlock>();
		Planet _nearestPlanet;

		IMyTextSurface _dataSurface;
		IMyTerminalBlock _refBlock;


		// PROGRAM ///////////////////////////////////////////////////////////////////////////////////////////////
		public Program()
		{
			//Load Saved Variables
			String[] loadData = Storage.Split('\n');
			if (loadData.Length > 8)
			{
				//Previously Compiled
				_planetIndex = int.Parse(loadData[0]);
				_gpsActive = bool.Parse(loadData[1]);
				_azSpeed = int.Parse(loadData[2]);
				_trackSpeed = StringToVector3(loadData[3]);
				_showMapParameters = bool.Parse(loadData[4]);
				_showNames = bool.Parse(loadData[5]);
				_pageIndex = int.Parse(loadData[6]);
				_brightnessMod = float.Parse(loadData[7]);
				_showShip = bool.Parse(loadData[8]);
			}
			else
			{
				//Newly Compiled
				_planetIndex = 0;
				_gpsActive = true;
				_azSpeed = 0;
				_trackSpeed = new Vector3(0, 0, 0);
				_showMapParameters = true;
				_showNames = true;
				_pageIndex = 0;
				_brightnessMod = 1;
				_showShip = true;
			}

			string oldData = Me.CustomData;
			string newData = DEFAULT_SETTINGS;

			_statusMessage = "";
			_planetToLog = false;

			if (!oldData.Contains("[Map Settings]"))
			{
				if (oldData.StartsWith("["))
				{
					newData += oldData;
				}
				else
				{
					newData += "---\n\n" + oldData;
				}
				Me.CustomData = newData;
			}

			Build();
			_previousCommand = "NEWLY LOADED";

			// Set the continuous update frequency of this script
			if (_slowMode)
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			else
				Runtime.UpdateFrequency = UpdateFrequency.Update10;
		}


		public void Save()
		{
			String saveData = _planetIndex.ToString() + "\n" + _gpsActive.ToString() + "\n" + _azSpeed.ToString();
			saveData += "\n" + Vector3ToString(_trackSpeed) + "\n" + _showMapParameters.ToString() + "\n" + _showNames.ToString();
			saveData += "\n" + _pageIndex.ToString() + "\n" + _brightnessMod.ToString() + "\n" + _showShip.ToString();

			Storage = saveData;
		}


		// MAIN ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Main(string argument)
		{
			_planets = _planetList.Count > 0;
			_myPos = _refBlock.GetPosition();

			Echo("////// PLANET MAP 3D ////// " + _cycleSpinner[_cycleStep % _cycleSpinner.Length]);
			Echo(_previousCommand);
			Echo(_statusMessage);
			Echo("MAP Count: " + _mapList.Count);

			if (_dataSurface == null)
			{
				Echo("Data Screen: Unassigned");
			}
			else
			{
				Echo("Data Screen: Active");
			}

			if (_planets)
			{
				Echo("Planet Count: " + _planetList.Count);
				Planet planet = _planetList[_planetList.Count - 1];
			}
			else
			{
				Echo("No Planets Logged!");
			}

			if (_waypointList.Count > 0)
			{
				Echo("GPS Count: " + _waypointList.Count + "\n");
				//Waypoint waypoint = _waypointList[_waypointList.Count - 1];
			}
			else
			{
				Echo("No Waypoints Logged!");
			}

			if (_mapList.Count > 0)
			{
				CycleExecute();
				ButtonTimer();

				if (argument != "")
				{
					MainSwitch(argument);
				}

				if (_planets)
				{
					if (_cycleStep == _cycleLength || _previousCommand == "NEWLY LOADED")
					{
						SortByNearest(_planetList);
					}
					_nearestPlanet = _planetList[0];
				}

				DrawMaps();
			}
			else
			{
				SetGridID();

				if (_mapList.Count < 1)
					_statusMessage = "NO MAP DISPLAY FOUND!\nPlease add tag " + _mapTag + " to desired block.\n";
			}

			if (_dataBlocks.Count > 0)
				DisplayData();
		}


		// VIEW FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// SHOW //
		void Show(List<StarMap> maps, string attribute, int state)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				switch (attribute)
				{
					case "GPS":
						if (state == 3)
						{
							cycleGPSForList(maps);
						}
						else
						{
							map.gpsState = state;
						}
						break;
					case "NAMES":
						map.showNames = setState(map.showNames, state);
						break;
					case "SHIP":
						map.showShip = setState(map.showShip, state);
						break;
					case "INFO":
						map.showInfo = setState(map.showInfo, state);
						break;
					default:
						_statusMessage = "INVALID DISPLAY COMMAND";
						break;
				}

				MapToParameters(map);
			}
		}


		// CYCLE GPS //
		void cycleGPS(StarMap map)
		{
			map.gpsState++;
			if (map.gpsState > 2)
				map.gpsState = 0;
		}

		void cycleGPSForList(List<StarMap> maps)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				cycleGPS(map);
			}
		}







		// DATA TO LOG //
		public void DataToLog()
		{
			MyIni mapIni = DataToIni(Me);

			if (_waypointList.Count > 0)
			{
				String waypointData = "";
				foreach (Waypoint waypoint in _waypointList)
				{
					waypointData += WaypointToString(waypoint) + "\n";
				}
				mapIni.Set(PROGRAM_HEAD, "Waypoint_List", waypointData);
			}

			String planetData = "";
			if (_planets)
			{
				foreach (Planet planet in _planetList)
				{
					planetData += planet.ToString() + "\n";
				}
			}

			if (_unchartedList.Count > 0)
			{
				foreach (Planet uncharted in _unchartedList)
				{
					planetData += uncharted.ToString() + "\n";
				}
			}

			if (planetData != "")
			{
				mapIni.Set(PROGRAM_HEAD, "Planet_List", planetData);
			}

			Me.CustomData = mapIni.ToString();
		}


		// MAP TO PARAMETERS // Writes map object to CustomData of Display Block
		public void MapToParameters(StarMap map)
		{
			MyIni lcdIni = DataToIni(map.block);

			int i = 0;

			string blockIndex = lcdIni.Get("mapDisplay", "Indexes").ToString();
			string[] indexes = blockIndex.Split(',');

			int entries = indexes.Length;

			if (entries > 0)
			{
				for (int j = 0; j < entries; j++)
				{
					if (map.index.ToString() == indexes[j])
					{
						i = j;  //This is the array position of the screen index for this map.
					}
				}
			}

			// Read the old Ini Data and split into string arrays. Insert the new data into the arrays.
			string newIndexes = InsertEntry(map.index.ToString(), blockIndex, ',', i, entries, "0");
			string newCenters = InsertEntry(Vector3ToString(map.center), lcdIni.Get("mapDisplay", "Center").ToString(), ';', i, entries, "(0,0,0)");
			string newModes = InsertEntry(map.mode, lcdIni.Get("mapDisplay", "Mode").ToString(), ',', i, entries, "FREE");
			string newFocal = InsertEntry(map.focalLength.ToString(), lcdIni.Get("mapDisplay", "FocalLength").ToString(), ',', i, entries, DV_FOCAL.ToString());
			string newRadius = InsertEntry(map.rotationalRadius.ToString(), lcdIni.Get("mapDisplay", "RotationalRadius").ToString(), ',', i, entries, DV_RADIUS.ToString());
			string newAzimuth = InsertEntry(map.azimuth.ToString(), lcdIni.Get("mapDisplay", "Azimuth").ToString(), ',', i, entries, "0");
			string newAltitude = InsertEntry(map.altitude.ToString(), lcdIni.Get("mapDisplay", "Altitude").ToString(), ',', i, entries, DV_ALTITUDE.ToString());
			string newDX = InsertEntry(map.dX.ToString(), lcdIni.Get("mapDisplay", "dX").ToString(), ',', i, entries, "0");
			string newDY = InsertEntry(map.dY.ToString(), lcdIni.Get("mapDisplay", "dY").ToString(), ',', i, entries, "0");
			string newDZ = InsertEntry(map.dZ.ToString(), lcdIni.Get("mapDisplay", "dZ").ToString(), ',', i, entries, "0");
			string newDAz = InsertEntry(map.dAz.ToString(), lcdIni.Get("mapDisplay", "dAz").ToString(), ',', i, entries, "0");
			string newGPS = InsertEntry(map.gpsStateToMode(), lcdIni.Get("mapDisplay", "GPS").ToString(), ',', i, entries, "True");
			string newNames = InsertEntry(map.showNames.ToString(), lcdIni.Get("mapDisplay", "Names").ToString(), ',', i, entries, "True");
			string newShip = InsertEntry(map.showShip.ToString(), lcdIni.Get("mapDisplay", "Ship").ToString(), ',', i, entries, "True");
			string newInfo = InsertEntry(map.showInfo.ToString(), lcdIni.Get("mapDisplay", "Info").ToString(), ',', i, entries, "True");
			string newPlanets = InsertEntry(map.activePlanetName, lcdIni.Get("mapDisplay", "Planet").ToString(), ',', i, entries, "[null]");
			string newWaypoints = InsertEntry(map.activeWaypointName, lcdIni.Get("mapDisplay", "Waypoint").ToString(), ',', i, entries, "[null]");

			// Update the Ini Data.
			lcdIni.Set("mapDisplay", "Center", newCenters);
			lcdIni.Set("mapDisplay", "Mode", newModes);
			lcdIni.Set("mapDisplay", "FocalLength", newFocal);
			lcdIni.Set("mapDisplay", "RotationalRadius", newRadius);
			lcdIni.Set("mapDisplay", "Azimuth", newAzimuth);
			lcdIni.Set("mapDisplay", "Altitude", newAltitude);
			lcdIni.Set("mapDisplay", "Indexes", newIndexes);
			lcdIni.Set("mapDisplay", "dX", newDX);
			lcdIni.Set("mapDisplay", "dY", newDY);
			lcdIni.Set("mapDisplay", "dZ", newDZ);
			lcdIni.Set("mapDisplay", "dAz", newDAz);
			lcdIni.Set("mapDisplay", "GPS", newGPS);
			lcdIni.Set("mapDisplay", "Names", newNames);
			lcdIni.Set("mapDisplay", "Ship", newShip);
			lcdIni.Set("mapDisplay", "Info", newInfo);
			lcdIni.Set("mapDisplay", "Planet", newPlanets);
			lcdIni.Set("mapDisplay", "Waypoint", newWaypoints);

			map.block.CustomData = lcdIni.ToString();
		}


		// CLIPBOARD TO LOG //
		void ClipboardToLog(string markerType, string clipboard)
		{
			string[] waypointData = clipboard.Split(':');
			if (waypointData.Length < 6)
			{
				_statusMessage = "Does not match GPS format:/nGPS:<name>:X:Y:Z:<color>:";
				return;
			}

			Vector3 position = new Vector3(float.Parse(waypointData[2]), float.Parse(waypointData[3]), float.Parse(waypointData[4]));
			LogWaypoint(waypointData[1], position, markerType, waypointData[5]);
		}


		// LOG TO CLIPBOARD //
		string LogToClipboard(string waypointName)
		{
			Waypoint waypoint = GetWaypoint(waypointName);
			if (waypoint == null)
			{
				_statusMessage = "No waypoint " + waypointName + " found!";
				return _statusMessage;
			}

			Vector3 location = waypoint.position;
			string output = "GPS:" + waypoint.name + ":" + location.X + ":" + location.Y + ":" + location.Z + ":#FF75C9F1:";

			return output;
		}


		// LOG WAYPOINT //
		public void LogWaypoint(String waypointName, Vector3 position, String markerType, String waypointColor)
		{
			if (waypointName == "")
			{
				_statusMessage = "No Waypoint Name Provided! Please Try Again.\n";
				return;
			}

			Waypoint waypoint = GetWaypoint(waypointName);

			if (waypoint != null)
			{
				_statusMessage = "Waypoint " + waypointName + " already exists! Please choose different name.\n";
				return;
			}

			waypoint = new Waypoint();
			waypoint.name = waypointName;
			waypoint.position = position;
			waypoint.marker = markerType;
			waypoint.isActive = true;
			waypoint.color = waypointColor;

			_waypointList.Add(waypoint);
			DataToLog();
			foreach (StarMap map in _mapList)
			{
				UpdateMap(map);
			}
		}


		// SET WAYPOINT STATE //
		public void SetWaypointState(String waypointName, int state)
		{
			Waypoint waypoint = GetWaypoint(waypointName);

			if (waypoint == null)
			{
				WaypointError(waypointName);
				return;
			}

			//State Switch: 0 => Deactivate, 1 => Activate, 2 => Toggle
			switch (state)
			{
				case 0:
					waypoint.isActive = false;
					break;
				case 1:
					waypoint.isActive = true;
					break;
				case 2:
					waypoint.isActive = !waypoint.isActive;
					break;
				case 3:
					_waypointList.Remove(waypoint);
					_statusMessage = "Waypoint deleted: " + waypointName + " \n";
					break;
				default:
					_statusMessage = "Invalid waypoint state int!\n";
					break;
			}

			DataToLog();
		}


		// PLOT JUMP POINT //
		void PlotJumpPoint(string planetName)
		{
			Planet planet = GetPlanet(planetName);
			{
				if (planet == null)
				{
					PlanetError(planetName);
				}
			}

			int designation = 1;
			string name = planet.name + " Orbit ";
			Waypoint jumpPoint = GetWaypoint(name + designation);

			while (jumpPoint != null)
			{
				designation++;
				jumpPoint = GetWaypoint(name + designation);
			}

			Vector3 position = planet.position + (_myPos - planet.position) / Vector3.Distance(_myPos, planet.position) * planet.radius * JUMP_RATIO;

			LogWaypoint(name + designation, position, "WAYPOINT", "WHITE");
		}


		// PROJECT POINT//
		void ProjectPoint(string marker, string arg)
		{
			string[] args = arg.Split(' ');

			if (args.Length < 2)
			{
				_statusMessage = "INSUFFICIENT ARGUMENT!\nPlease include arguments <DISTANCE(in meters)> <WAYPOINT NAME>";
				return;
			}

			int distance;
			if (int.TryParse(args[0], out distance))
			{
				string name = "";
				for (int i = 1; i < args.Length; i++)
				{
					name += args[i] + " ";
				}

				Vector3 location = _myPos + _refBlock.WorldMatrix.Forward * distance;

				LogWaypoint(name.Trim(), location, marker, "WHITE");

				return;
			}

			_statusMessage = "DISTANCE ARGEMENT FAILED!\nPlease include Distance in meters. Do not include unit.";
		}


		// PLANET ERROR //
		void PlanetError(string name)
		{
			_statusMessage = "No planet " + name + " found!";
		}


		// WAYPOINT ERROR //
		void WaypointError(string name)
		{
			_statusMessage = "No waypoint " + name + " found!";
		}


		// NEW PLANET //
		public void NewPlanet(String planetName)
		{
			Planet planet = GetPlanet(planetName);

			if (planet != null)
			{
				_statusMessage = "Planet " + planetName + " already exists! Please choose different name.\n";
				return;
			}

			planetName += ";;;;;;;";
			planet = new Planet(planetName);
			planet.SetPoint(1, _myPos);

			_unchartedList.Add(planet);
			DataToLog();
		}


		// DELETE PLANET //
		public void DeletePlanet(String planetName)
		{
			Planet alderaan = GetPlanet(planetName);

			if (alderaan == null)
			{
				PlanetError(planetName);
				return;
			}

			_unchartedList.Remove(alderaan);
			_planetList.Remove(alderaan);
			DataToLog();
            _statusMessage = "PLANET DELETED: " + planetName + "\n\nDon't be too proud of this TECHNOLOGICAL TERROR you have constructed. The ability to DESTROY a PLANET is insignificant next to the POWER of the FORCE.\n";


            _planets = _planetList.Count > 0;

			if (_planets)
				_nearestPlanet = _planetList[0];
		}


		// LOG NEXT //
		public void LogNext(String planetName)
		{
			Planet planet = GetPlanet(planetName);

			if (planet == null)
			{
				PlanetError(planetName);
				return;
			}

			String[] planetData = planet.ToString().Split(';');

			if (planetData[4] == "")
			{
				planet.SetPoint(1, _myPos);
			}
			else if (planetData[5] == "")
			{
				planet.SetPoint(2, _myPos);
			}
			else if (planetData[6] == "")
			{
				planet.SetPoint(3, _myPos);
			}
			else
			{
				planet.SetPoint(4, _myPos);

				if (!planet.isCharted)
				{
					_planetList.Add(planet);
					_unchartedList.Remove(planet);
					_planets = true;
				}

				planet.CalculatePlanet();
				_planetToLog = true; // Specify that DataToLog needs to be called in CycleExecute.

				foreach (StarMap map in _mapList)
				{
					UpdateMap(map);
				}
			}

			DataToLog();
		}


		// SET PLANET COLOR //
		void SetPlanetColor(String argument)
		{
			String[] args = argument.Split(' ');
			String planetColor = args[0];

			if (args.Length < 2)
			{
				_statusMessage = "Insufficient Argument.  COLOR_PLANET requires COLOR and PLANET NAME.\n";
			}
			else
			{
				String planetName = "";
				for (int p = 1; p < args.Length; p++)
				{
					planetName += args[p] + " ";
				}
				planetName = planetName.Trim(' ').ToUpper();

				Planet planet = GetPlanet(planetName);

				if (planet != null)
				{
					planet.color = planetColor;
					_statusMessage = planetName + " color changed to " + planetColor + ".\n";
					DataToLog();
					return;
				}

				PlanetError(planetName);
			}
		}


		// SET WAYPOINT COLOR //
		void SetWaypointColor(String argument)
		{
			String[] args = argument.Split(' ');
			String waypointColor = args[0];

			if (args.Length < 2)
			{
				_statusMessage = "Insufficient Argument.  COLOR_WAYPOINT requires COLOR and WAYPOINT NAME.\n";
			}
			else
			{
				String waypointName = "";
				for (int w = 1; w < args.Length; w++)
				{
					waypointName += args[w] + " ";
				}
				waypointName = waypointName.Trim(' ').ToUpper();

				Waypoint waypoint = GetWaypoint(waypointName);

				if (waypoint != null)
				{
					waypoint.color = waypointColor;
					_statusMessage = waypointName + " color changed to " + waypointColor + ".\n";
					DataToLog();
					return;
				}

				WaypointError(waypointName);
			}
		}


		// SET WAYPOINT TYPE //
		void SetWaypointType(string arg, string waypointName)
		{
			Waypoint waypoint = GetWaypoint(waypointName);

			if (waypoint == null)
			{
				WaypointError(waypointName);
				return;
			}

			waypoint.marker = arg;
			DataToLog();
		}

		// ZOOM // Changes Focal Length of Maps. true => Zoom In / false => Zoom Out
		void Zoom(List<StarMap> maps, string arg)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				int doF = map.focalLength;
				float newScale;

				if (arg == "IN")
				{
					newScale = doF * ZOOM_STEP;
				}
				else
				{
					newScale = doF / ZOOM_STEP;
				}


				if (newScale > ZOOM_MAX)
				{
					doF = ZOOM_MAX;
				}
				else if (newScale < 1)
				{
					doF = 1;
				}
				else
				{
					doF = (int)newScale;
				}

				map.focalLength = doF;
			}
		}


		// ADJUST RADIUS //
		void AdjustRadius(List<StarMap> maps, bool increase)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				int radius = map.rotationalRadius;

				if (increase)
				{
					radius *= 2;
				}
				else
				{
					radius /= 2;
				}

				if (radius < map.focalLength)
				{
					radius = map.focalLength;
				}
				else if (radius > MAX_VALUE)
				{
					radius = MAX_VALUE;
				}

				map.rotationalRadius = radius;
			}
		}


		// MOVE CENTER //
		void MoveCenter(List<StarMap> maps, string movement)
		{
			if (NoMaps(maps))
				return;

			float step = (float)MOVE_STEP;
			float x = 0;
			float y = 0;
			float z = 0;

			switch (movement)
			{
				case "LEFT":
					x = step;
					break;
				case "RIGHT":
					x = -step;
					break;
				case "UP":
					y = step;
					break;
				case "DOWN":
					y = -step;
					break;
				case "FORWARD":
					z = step;
					break;
				case "BACKWARD":
					z = -step;
					break;
			}
			Vector3 moveVector = new Vector3(x, y, z);

			foreach (StarMap map in maps)
			{
				if (map.mode == "FREE" || map.mode == "WORLD")
				{
					map.center += rotateMovement(moveVector, map);
				}
				else
				{
					_statusMessage = "Translation controls only available in FREE & WORLD modes.";
				}
			}
		}


		// TRACK CENTER //		Adjust translational speed of map.
		void TrackCenter(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				switch (direction)
				{
					case "LEFT":
						map.dX += MOVE_STEP;
						break;
					case "RIGHT":
						map.dX -= MOVE_STEP;
						break;
					case "UP":
						map.dY += MOVE_STEP;
						break;
					case "DOWN":
						map.dY -= MOVE_STEP;
						break;
					case "FORWARD":
						map.dZ += MOVE_STEP;
						break;
					case "BACKWARD":
						map.dZ -= MOVE_STEP;
						break;
					default:
						_statusMessage = "Error with Track Command";
						break;
				}
			}
		}


		// ROTATE MAPS //
		void RotateMaps(List<StarMap> maps, string direction)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				switch (direction)
				{
					case "LEFT":
						map.yaw(ANGLE_STEP);
						break;
					case "RIGHT":
						map.yaw(-ANGLE_STEP);
						break;
					case "UP":
						map.pitch(-ANGLE_STEP);
						break;
					case "DOWN":
						map.pitch(ANGLE_STEP);
						break;
				}
			}
		}


		// SPIN MAPS //		Adjust azimuth speed of maps.
		void SpinMaps(List<StarMap> maps, string direction, int deltaAz)
		{
			if (NoMaps(maps))
				return;

			if (direction == "RIGHT")
				deltaAz *= -1;

			foreach (StarMap map in maps)
			{
				map.dAz += deltaAz;
			}
		}


		// STOP MAPS //		Halt all translational and azimuthal speed in maps.
		void StopMaps(List<StarMap> maps)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				map.dX = 0;
				map.dY = 0;
				map.dZ = 0;
				map.dAz = 0;
			}
		}


		// MAPS TO SHIP //		Centers maps on ship
		void MapsToShip(List<StarMap> maps)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				map.center = _myPos;
			}
		}


		// CENTER WORLD //	   Updates Map Center to the Average of all charted Planets
		void CenterWorld(StarMap map)
		{
			map.altitude = -15;
			map.azimuth = 45;
			map.focalLength = 256;
			map.rotationalRadius = 4194304;
			Vector3 worldCenter = new Vector3(0, 0, 0);

			if (_planets)
			{
				foreach (Planet planet in _planetList)
				{
					worldCenter += planet.position;
				}

				worldCenter /= _planetList.Count;
			}

			map.center = worldCenter;
		}


		// CENTER SHIP //
		void CenterShip(StarMap map)
		{
			DefaultView(map);
			map.center = _myPos;
		}


		// ALIGN SHIP //
		void AlignShip(StarMap map)
		{
			Vector3 heading = _refBlock.WorldMatrix.Forward;
			int newAz = DegreeAdd((int)ToDegrees((float)Math.Atan2(heading.Z, heading.X)), -90);

			int newAlt = (int)ToDegrees((float)Math.Asin(heading.Y));// -25;
			if (newAlt < -90)
			{
				newAlt = DegreeAdd(newAlt, 180);
				newAz = DegreeAdd(newAz, 180);
			}

			map.altitude = newAlt;
			map.azimuth = newAz;
			map.center = _myPos;
		}


		// ALIGN ORBIT //
		void AlignOrbit(StarMap map)
		{
			if (_planetList.Count < 1)
			{
				return;
			}

			if (map.activePlanet == null)
			{
				if (_nearestPlanet == null)
				{
					Echo("No Nearest Planet Set!");
					return;
				}

				SelectPlanet(_nearestPlanet, map);
			}

			Vector3 planetPos = map.activePlanet.position;

			map.center = (_myPos + planetPos) / 2;
			map.altitude = 0;
			Vector3 orbit = _myPos - planetPos;
			map.azimuth = (int)ToDegrees((float)Math.Abs(Math.Atan2(orbit.Z, orbit.X) + (float)Math.PI * 0.75f)); //  )

			//Get largest component distance between ship and planet.
			//double span = Math.Sqrt(orbit.LengthSquared() - Math.Pow(orbit.Y,2));
			float span = orbit.Length();

			double newRadius = 1.25f * map.focalLength * span / map.viewport.Height;

			if (newRadius > MAX_VALUE || newRadius < 0)
			{
				newRadius = MAX_VALUE;
				double newZoom = 0.8f * map.viewport.Height * (MAX_VALUE / span);
				map.focalLength = (int)newZoom;
			}

			map.rotationalRadius = (int)newRadius;
		}


		// PLANET MODE //
		void PlanetMode(StarMap map)
		{
			map.focalLength = DV_FOCAL;
			map.dAz = 0;
			map.dX = 0;
			map.dY = 0;
			map.dZ = 0;

			if (map.viewport.Width > 500)
			{
				map.focalLength *= 4;
			}

			if (_planets)
			{
				SortByNearest(_planetList);
				map.activePlanet = _planetList[0];
				ShipToPlanet(map);

				if (map.activePlanet.radius < 30000)
				{
					map.focalLength *= 4;
				}
			}

			map.rotationalRadius = DV_RADIUS;
			map.mode = "PLANET";
		}


		// SET MAP MODE //
		void SetMapMode(StarMap map, string mapMode)
		{
			if (mapMode == "WORLD")
			{
				CenterWorld(map);
			}
			else
			{
				CenterShip(map);
			}

			if (mapMode == "PLANET")
			{
				PlanetMode(map);
			}
			else if (mapMode == "ORBIT")
			{
				AlignOrbit(map);
			}
			else if (mapMode == "CHASE")
			{
				AlignShip(map);
			}

			map.mode = mapMode;
		}


		// CHANGE MODE //
		void ChangeMode(string mapMode, List<StarMap> maps)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				SetMapMode(map, mapMode);
			}
		}


		// CYCLE MODE //
		void CycleMode(List<StarMap> maps, bool cycleUp)
		{
			if (NoMaps(maps))
				return;

			_activePlanet = "";
			string[] modes = { "FREE", "SHIP", "CHASE", "PLANET", "ORBIT", "WORLD" };
			int length = modes.Length;

			foreach (StarMap map in maps)
			{
				int modeIndex = 0;
				for (int i = 0; i < length; i++)
				{
					// Find Current Map Mode
					if (map.mode.ToUpper() == modes[i])
					{
						modeIndex = i;
					}
				}

				// Cycle Mode Up/Down by 1
				if (cycleUp)
				{
					modeIndex++;
				}
				else
				{
					modeIndex--;
				}

				if (modeIndex >= length)
				{
					modeIndex = 0;
				}
				else if (modeIndex < 0)
				{
					modeIndex = length - 1;
				}

				SetMapMode(map, modes[modeIndex]);
			}
		}


		// SHIP TO PLANET //   Aligns the map so that the ship appears above the center of the planet.
		void ShipToPlanet(StarMap map)
		{
			if (_planets)
			{
				Planet planet = _nearestPlanet;

				Vector3 shipVector = _myPos - planet.position;
				float magnitude = Vector3.Distance(_myPos, planet.position);

				float azAngle = (float)Math.Atan2(shipVector.Z, shipVector.X);
				float altAngle = (float)Math.Asin(shipVector.Y / magnitude);

				map.center = planet.position;
				map.azimuth = DegreeAdd((int)ToDegrees(azAngle), 90);
				map.altitude = (int)ToDegrees(-altAngle);
			}
		}


		// DEFAULT VIEW //
		void DefaultView(StarMap map)
		{
			map.mode = "FREE";

			map.center = new Vector3(0, 0, 0);
			map.focalLength = DV_FOCAL;

			if (map.viewport.Width > 500)
			{
				map.focalLength *= 3;
			}

			map.rotationalRadius = DV_RADIUS;
			map.azimuth = 0;
			map.altitude = DV_ALTITUDE;
		}


		// MAPS TO DEFAULT //
		void MapsToDefault(List<StarMap> maps)
		{
			if (maps.Count < 1)
				return;

			foreach (StarMap map in maps)
			{
				DefaultView(map);
			}
		}


		// TOOL FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////


		// UPDATE TAGS // Add Grid Tag to all map displays on grid
		void SetGridID()
		{
			_gridID = Me.CubeGrid.EntityId.ToString();
			SetKey(Me, SHARED, "Grid_ID", _gridID);

			_statusMessage += "Grid ID set to:\n" + _gridID + "\n";

			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

			foreach (IMyTerminalBlock block in blocks)
			{
				if (block.IsSameConstructAs(Me) && block.CustomData.Contains(SHARED))
					SetKey(block, SHARED, "Grid_ID", _gridID);
			}

			Build();
		}


		// ON GRID // Checks if map blocks are part of current ship, even if merged
		bool onGrid(IMyTerminalBlock mapTerminal)
		{
			if (!mapTerminal.IsSameConstructAs(Me))
				return false;

			string iniGrid = GetKey(mapTerminal, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());

			if (iniGrid == _gridID)
				return true;
			else
				return false;
		}

		// DATA TO INI //
		MyIni DataToIni(IMyTerminalBlock block)
		{
			MyIni iniOuti = new MyIni();

			MyIniParseResult result;
			if (!iniOuti.TryParse(block.CustomData, out result))
				throw new Exception(result.ToString());

			return iniOuti;
		}

		// COLOR SWITCH //
		Color ColorSwitch(string colorString, bool isWaypoint)
		{
			colorString = colorString.ToUpper();

			if (colorString.StartsWith("#"))
				return HexToColor(colorString);

			Color colorOut = new Color(8, 8, 8);
			if (isWaypoint)
				colorOut = Color.White;

			switch (colorString)
			{
				case "RED":
					colorOut = new Color(32, 0, 0);
					break;
				case "GREEN":
					colorOut = new Color(0, 32, 0);
					break;
				case "BLUE":
					colorOut = new Color(0, 0, 32);
					break;
				case "YELLOW":
					colorOut = new Color(127, 127, 26);
					break;
				case "MAGENTA":
					colorOut = new Color(64, 0, 64);
					break;
				case "PURPLE":
					colorOut = new Color(24, 0, 48);
					break;
				case "CYAN":
					colorOut = new Color(0, 32, 32);
					break;
				case "LIGHTBLUE":
					colorOut = new Color(32, 32, 96);
					break;
				case "ORANGE":
					colorOut = new Color(32, 16, 0);
					break;
				case "TAN":
					colorOut = new Color(153, 100, 48);
					break;
				case "BROWN":
					colorOut = new Color(38, 25, 12);
					break;
				case "RUST":
					colorOut = new Color(64, 20, 16);
					break;
				case "GRAY":
					colorOut = new Color(16, 16, 16);
					break;
				case "GREY":
					colorOut = new Color(16, 16, 16);
					break;
				case "WHITE":
					colorOut = new Color(64, 64, 64);
					if (isWaypoint)
						colorOut = Color.White;
					break;
				default:
					colorOut = new Color(8, 8, 8);
					break;
			}

			return colorOut;
		}

		// HEX TO COLOR //
		Color HexToColor(string hexString)
		{

			if (hexString.Length != 9 && hexString.Length != 7)
				return Color.White;
			int i = 3; // Starting index for ARGB format.

			if (hexString.Length == 7)
				i = 1; // Starting index for normal HEX.

			int r, g, b = 0;

			r = Convert.ToUInt16(hexString.Substring(i, 2), 16);
			g = Convert.ToUInt16(hexString.Substring(i + 2, 2), 16);
			b = Convert.ToUInt16(hexString.Substring(i + 4, 2), 16);

			return new Color(r, g, b);
		}


		// SET STATE // Sets specified boolean to true, false, or toggle
		bool setState(bool attribute, int state)
		{
			switch (state)
			{
				case 0:
					attribute = false;
					break;
				case 1:
					attribute = true;
					break;
				case 3:
					attribute = !attribute;
					break;
			}

			return attribute;
		}


		// INSERT ENTRY //
		public string InsertEntry(string entry, string oldString, char separator, int index, int length, string placeHolder)
		{
			string newString;

			List<string> entries = StringToEntries(oldString, separator, length, placeHolder);

			// If there's only one entry in the string return entry.
			if (entries.Count == 1 && length == 0)
			{
				return entry;
			}

			//Insert entry into the old string.
			entries[index] = entry;

			newString = entries[0];
			for (int n = 1; n < entries.Count; n++)
			{
				newString += separator + entries[n];
			}

			return newString;
		}


		// STRING TO ENTRIES //		Splits string into a list of variable length, by a separator character.  If the list is shorter than 
		//		the desired length,the remainder is filled with copies of the place holder.
		public List<string> StringToEntries(string arg, char separator, int length, string placeHolder)
		{
			List<string> entries = new List<string>();
			string[] args = arg.Split(separator);

			foreach (string argument in args)
			{
				entries.Add(argument);
			}

			while (entries.Count < length)
			{
				entries.Add(placeHolder);
			}

			return entries;
		}


		// STRING TO WAYPOINT //
		public Waypoint StringToWaypoint(String argument)
		{
			Waypoint waypoint = new Waypoint();
			String[] wayPointData = argument.Split(';');
			if (wayPointData.Length > 3)
			{
				waypoint.name = wayPointData[0];
				waypoint.position = StringToVector3(wayPointData[1]);
				waypoint.marker = wayPointData[2];
				waypoint.isActive = wayPointData[3].ToUpper() == "ACTIVE";
			}

			if (wayPointData.Length < 5)
			{
				waypoint.color = "WHITE";
			}
			else
			{
				waypoint.color = wayPointData[4];
			}
			return waypoint;
		}


		// WAYPOINT TO STRING //
		public String WaypointToString(Waypoint waypoint)
		{
			String output = waypoint.name + ";" + Vector3ToString(waypoint.position) + ";" + waypoint.marker;

			String activity = "INACTIVE";
			if (waypoint.isActive)
			{
				activity = "ACTIVE";
			}
			output += ";" + activity + ";" + waypoint.color;

			return output;
		}


		// VECTOR3 TO STRING //
		public static string Vector3ToString(Vector3 vec3)
		{
			String newData = "(" + vec3.X + "," + vec3.Y + "," + vec3.Z + ")";
			return newData;
		}


		// STRING TO VECTOR3 //
		public static Vector3 StringToVector3(string sVector)
		{
            try
            {
				// Remove the parentheses
				if (sVector.StartsWith("(") && sVector.EndsWith(")"))
				{
					sVector = sVector.Substring(1, sVector.Length - 2);
				}

				// split the items
				string[] sArray = sVector.Split(',');

				// store as a Vector3
				Vector3 result = new Vector3(
					ParseFloat(sArray[0], 0),
					ParseFloat(sArray[1], 0),
					ParseFloat(sArray[2], 0));

				return result;
			}
			catch
            {
				return Vector3.Zero;
            }

			
		}


		// ABBREVIATE VALUE //	 Abbreviates float value to k/M/G notation (i.e. 1000 = 1k). Returns string.
		public string abbreviateValue(float valueIn)
		{
			string abbreviatedValue;
			if (valueIn <= -1000000000 || valueIn >= 1000000000)
			{
				valueIn = valueIn / 1000000000;
				abbreviatedValue = valueIn.ToString("0.0") + "G";
			}
			else if (valueIn <= -1000000 || valueIn >= 1000000)
			{
				valueIn = valueIn / 1000000;
				abbreviatedValue = valueIn.ToString("0.0") + "M";
			}
			else if (valueIn <= -1000 || valueIn >= 1000)
			{
				valueIn = valueIn / 1000;
				abbreviatedValue = valueIn.ToString("0.0") + "k";
			}
			else
			{
				abbreviatedValue = valueIn.ToString("F0");
			}
			return abbreviatedValue;
		}


		// REPLACE COLUMN //
		static void ReplaceColumn(double[,] matrix1, double[,] matrix2, double[] t, int column)
		{
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					matrix2[i, j] = matrix1[i, j];
				}
			}
			matrix2[0, column] = t[0];
			matrix2[1, column] = t[1];
			matrix2[2, column] = t[2];
			matrix2[3, column] = t[3];
		}


		// T-VALUE //
		public static double TValue(Vector3 vec3)
		{
			double result;
			double x = vec3.X;
			double y = vec3.Y;
			double z = vec3.Z;

			result = -1 * (x * x + y * y + z * z);
			return result;
		}


		// Det4 // Gets determinant of a 4x4 Matrix
		public static double Det4(double[,] q)
		{
			double a = q[0, 0];
			double b = q[0, 1];
			double c = q[0, 2];
			double d = q[0, 3];
			double e = q[1, 0];
			double f = q[1, 1];
			double g = q[1, 2];
			double h = q[1, 3];
			double i = q[2, 0];
			double j = q[2, 1];
			double k = q[2, 2];
			double l = q[2, 3];
			double m = q[3, 0];
			double n = q[3, 1];
			double o = q[3, 2];
			double p = q[3, 3];

			double determinant = a * (f * (k * p - l * o) - g * (j * p - l * n) + h * (j * o - k * n))
								- b * (e * (k * p - l * o) - g * (i * p - l * m) + h * (i * o - k * m))
								+ c * (e * (j * p - l * n) - f * (i * p - l * m) + h * (i * n - j * m))
								- d * (e * (j * o - k * n) - f * (i * o - k * m) + g * (i * n - j * m));


			return determinant;
		}


		// PLANET SORT //  Sorts Planets List by Transformed Z-Coordinate from largest to smallest. For the purpose of sprite printing.
		public void PlanetSort(List<Planet> planets, StarMap map)
		{
			int length = planets.Count;

			for (int i = 0; i < planets.Count - 1; i++)
			{
				for (int p = 1; p < length; p++)
				{
					Planet planetA = planets[p - 1];
					Planet planetB = planets[p];

					if (planetA.transformedCoords[map.number].Z < planetB.transformedCoords[map.number].Z)
					{
						planets[p - 1] = planetB;
						planets[p] = planetA;
					}
				}

				length--;
				if (length < 2)
				{
					return;
				}
			}
		}


		// SORT BY NEAREST //	 Sorts Planets by nearest to farthest.
		public void SortByNearest(List<Planet> planets)
		{
			int length = planets.Count;
			if (length > 1)
			{
				for (int i = 0; i < length - 1; i++)
				{
					for (int p = 1; p < length; p++)
					{
						Planet planetA = planets[p - 1];
						Planet planetB = planets[p];

						float distA = Vector3.Distance(planetA.position, _myPos);
						float distB = Vector3.Distance(planetB.position, _myPos);

						if (distB < distA)
						{
							planets[p - 1] = planetB;
							planets[p] = planetA;
						}
					}

					length--;
					if (length < 2)
					{
						return;
					}
				}
			}
		}


		// SORT WAYPOINTS //
		void SortWaypoints(List<Waypoint> waypoints)
		{
			int length = waypoints.Count;

			for (int i = 0; i < length - 1; i++)
			{
				for (int w = 1; w < length; w++)
				{
					Waypoint pointA = waypoints[w - 1];
					Waypoint pointB = waypoints[w];

					float distA = Vector3.Distance(pointA.position, _myPos);
					float distB = Vector3.Distance(pointB.position, _myPos);

					if (distB < distA)
					{
						waypoints[w - 1] = pointB;
						waypoints[w] = pointA;
					}
				}

				length--;
				if (length < 2)
				{
					return;
				}
			}
		}


		// TRANSFORM VECTOR //	   Transforms vector location of planet or waypoint for StarMap view.
		public Vector3 transformVector(Vector3 vectorIn, StarMap map)
		{
			double xS = vectorIn.X - map.center.X; //Vector X - Map X
			double yS = vectorIn.Y - map.center.Y; //Vector Y - Map Y
			double zS = vectorIn.Z - map.center.Z; //Vector Z - Map Z 

			double r = map.rotationalRadius;

			double cosAz = Math.Cos(ToRadians(map.azimuth));
			double sinAz = Math.Sin(ToRadians(map.azimuth));

			double cosAlt = Math.Cos(ToRadians(map.altitude));
			double sinAlt = Math.Sin(ToRadians(map.altitude));

			// Transformation Formulas from Matrix Calculations
			double xT = cosAz * xS + sinAz * zS;
			double yT = sinAz * sinAlt * xS + cosAlt * yS - sinAlt * cosAz * zS;
			double zT = -sinAz * cosAlt * xS + sinAlt * yS + cosAz * cosAlt * zS + r;

			Vector3 vectorOut = new Vector3(xT, yT, zT);
			return vectorOut;
		}


		// ROTATE VECTOR //
		public Vector3 rotateVector(Vector3 vecIn, StarMap map)
		{
			float x = vecIn.X;
			float y = vecIn.Y;
			float z = vecIn.Z;

			float cosAz = (float)Math.Cos(ToRadians(map.azimuth));
			float sinAz = (float)Math.Sin(ToRadians(map.azimuth));

			float cosAlt = (float)Math.Cos(ToRadians(map.altitude));
			float sinAlt = (float)Math.Sin(ToRadians(map.altitude));

			float xT = cosAz * x + sinAz * z;
			float yT = sinAz * sinAlt * x + cosAlt * y - sinAlt * cosAz * z;
			float zT = -sinAz * cosAlt * x + sinAlt * y + cosAz * cosAlt * z;

			Vector3 vecOut = new Vector3(xT, yT, zT);
			return vecOut;
		}


		// ROTATE MOVEMENT //	 Rotates Movement Vector for purpose of translation.
		public Vector3 rotateMovement(Vector3 vecIn, StarMap map)
		{
			float x = vecIn.X;
			float y = vecIn.Y;
			float z = vecIn.Z;

			float cosAz = (float)Math.Cos(ToRadians(-map.azimuth));
			float sinAz = (float)Math.Sin(ToRadians(-map.azimuth));

			float cosAlt = (float)Math.Cos(ToRadians(-map.altitude));
			float sinAlt = (float)Math.Sin(ToRadians(-map.altitude));

			float xT = cosAz * x + sinAz * sinAlt * y + sinAz * cosAlt * z;
			float yT = cosAlt * y - sinAlt * z;
			float zT = -sinAz * x + cosAz * sinAlt * y + cosAz * cosAlt * z;

			Vector3 vecOut = new Vector3(xT, yT, zT);
			return vecOut;
		}


		// CYCLE EXECUTE // Wait specified number of cycles to execute cyclial commands
		public void CycleExecute()
		{
			_cycleStep--;

			// EXECUTE CYCLE DELAY FUNCTIONS
			if (_cycleStep < 0)
			{
				_cycleStep = _cycleLength;

				//Toggle Indicator LightGray
				_lightOn = !_lightOn;

				if (_waypointList.Count > 0)
				{
					_sortCounter++;
					if (_sortCounter >= 10)
					{
						SortWaypoints(_waypointList);
						_sortCounter = 0;
					}
				}

				if (_planets)
				{
					//Sort Planets by proximity to ship.
					SortByNearest(_planetList);
					_nearestPlanet = _planetList[0];

					if (_mapList.Count < 1)
						return;

					foreach (StarMap map in _mapList)
					{
						if (map.mode == "PLANET" || map.mode == "CHASE" || map.mode == "ORBIT")
						{
							UpdateMap(map);
						}
					}
				}

				if (_planetToLog)
				{
					DataToLog();
					_planetToLog = false;
				}
			}
		}


		// LIST TO NAMES // Builds multi-line string of names from list entries
		string listToStrings(List<string> inputs)
		{
			string output = "";

			if (inputs.Count < 1)
				return output;

			foreach (string input in inputs)
			{
				output += input + "\n";
			}

			return output.Trim();
		}


		// REFRESH // - Updates map info from map's custom data
		void Build()
		{
			_planetList = new List<Planet>();
			_unchartedList = new List<Planet>();
			_waypointList = new List<Waypoint>();
			_mapList = new List<StarMap>();
			_mapBlocks = new List<IMyTerminalBlock>();
			_mapMenus = new List<MapMenu>();

			_mapLog = DataToIni(Me);

			//Name of screen to print map to.
			_mapTag = _mapLog.Get(PROGRAM_HEAD, "MAP_Tag").ToString();

			//Index of screen to print map to.
			_mapIndex = _mapLog.Get(PROGRAM_HEAD, "MAP_Index").ToUInt16();

			//Index of screen to print map data to.
			_dataIndex = _mapLog.Get(PROGRAM_HEAD, "Data_Index").ToUInt16();

			//Name of reference block
			_refName = _mapLog.Get(PROGRAM_HEAD, "Reference_Name").ToString();

			//Name of Data Display Block
			_dataName = _mapLog.Get(PROGRAM_HEAD, "Data_Tag").ToString();

			//Slow Mode
			_slowMode = ParseBool(GetKey(Me, PROGRAM_HEAD, "Slow_Mode", "false"));

			//Grid Tag
			_gridID = GetKey(Me, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());

			if (_gridID == "")
			{
				_gridID = Me.CubeGrid.EntityId.ToString();
				_mapLog.Set(SHARED, "Grid_ID", _gridID);
				Me.CustomData = _mapLog.ToString();
			}

			//Cycle Step Length
			try
			{
				_cycleLength = Int16.Parse(GetKey(Me, PROGRAM_HEAD, "Cycle_Step", "5"));
			}
			catch
			{
				_cycleLength = 5;
			}
			_cycleStep = _cycleLength;

			if (_mapTag == "" || _mapTag == "<name>")
			{
				Echo("No LCD specified!!!");
			}
			else
			{
				GridTerminalSystem.SearchBlocksOfName(_mapTag, _mapBlocks);
				foreach (IMyTerminalBlock mapBlock in _mapBlocks)
				{
					if (onGrid(mapBlock))
					{
						List<StarMap> maps = ParametersToMaps(mapBlock);

						if (maps.Count > 0)
						{
							foreach (StarMap map in maps)
							{
								activateMap(map);
								MapToParameters(map);
							}
						}
					}
				}
			}

			if (_dataName != "" || _dataName != "<name>")
			{
				GridTerminalSystem.SearchBlocksOfName(_dataName, _dataBlocks);
				if (_dataBlocks.Count > 0)
				{
					IMyTextSurfaceProvider dataBlock = _dataBlocks[0] as IMyTextSurfaceProvider;
					_dataSurface = dataBlock.GetSurface(_dataIndex);
					_dataSurface.ContentType = ContentType.TEXT_AND_IMAGE;
				}
			}

			if (_refName == "" || _refName == "<name>")
			{
				_statusMessage = "WARNING: No Reference Block Name Specified!\nMay result in false orientation!";
				_refBlock = Me as IMyTerminalBlock;
			}
			else
			{
				List<IMyTerminalBlock> refBlocks = new List<IMyTerminalBlock>();
				GridTerminalSystem.SearchBlocksOfName(_refName, refBlocks);
				if (refBlocks.Count > 0)
				{
					_refBlock = refBlocks[0] as IMyTerminalBlock;
					Echo("Reference: " + _refBlock.CustomName);
				}
				else
				{
					_statusMessage = "WARNING: No Block containing " + _refName + " found.\nMay result in false orientation!";
					_refBlock = Me as IMyTerminalBlock;
				}
			}



			string planetData = _mapLog.Get(PROGRAM_HEAD, "Planet_List").ToString();

			string[] mapEntries = planetData.Split('\n');
			foreach (string planetString in mapEntries)
			{
				if (planetString.Contains(";"))
				{
					Planet planet = new Planet(planetString);
					if (planet.isCharted)
					{
						_planetList.Add(planet);
					}
					else
					{
						_unchartedList.Add(planet);
					}
				}
			}
			_planets = _planetList.Count > 0;

			string waypointData = _mapLog.Get(PROGRAM_HEAD, "Waypoint_List").ToString();
			string[] gpsEntries = waypointData.Split('\n');

			foreach (string waypointString in gpsEntries)
			{
				if (waypointString.Contains(";"))
				{
					Waypoint waypoint = StringToWaypoint(waypointString);
					_waypointList.Add(waypoint);
				}
			}

			AssignMenus();

			// Start with indicator light on.
			_lightOn = true;
		}


		// UPDATE MAP // Transform logged locations based on map parameters
		public void UpdateMap(StarMap map)
		{
			if (_mapList.Count == 0)
				return;

			if (_planets)
			{
				foreach (Planet planet in _planetList)
				{
					Vector3 newCenter = transformVector(planet.position, map);

					if (planet.transformedCoords.Count < _mapList.Count)
					{
						planet.transformedCoords.Add(newCenter);
					}
					else
					{
						planet.transformedCoords[map.number] = newCenter;
					}
				}
			}

			if (_waypointList.Count > 0)
			{
				foreach (Waypoint waypoint in _waypointList)
				{
					Vector3 newPos = transformVector(waypoint.position, map);
					if (waypoint.transformedCoords.Count < _mapList.Count)
					{
						waypoint.transformedCoords.Add(newPos);
					}
					else
					{
						waypoint.transformedCoords[map.number] = newPos;
					}
				}
			}
		}


		// ACTIVATE MAP // activates tagged map screen without having to recompile.
		void activateMap(StarMap map)
		{
			IMyTextSurfaceProvider mapBlock = map.block as IMyTextSurfaceProvider;
			map.drawingSurface = mapBlock.GetSurface(map.index);
			PrepareTextSurfaceForSprites(map.drawingSurface);

			// Calculate the viewport offset by centering the surface size onto the texture size
			map.viewport = new RectangleF(
			(map.drawingSurface.TextureSize - map.drawingSurface.SurfaceSize) / 2f,
				map.drawingSurface.SurfaceSize
			);
			map.number = _mapList.Count;
			_mapList.Add(map);
			UpdateMap(map);
		}
	}
}
