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
		// INI FUNCTIONS //////////////////////////////////////////////////////////////////////////////////////////////////////

		// BLOCK INI
		class BlockIni
        {
			IMyTerminalBlock Block;
			MyIni Ini;
			string MainHeader;

			// Constructor
			public BlockIni(IMyTerminalBlock block, string header)
            {
				Block = block;
				MainHeader = header;
				Ini = GetIni(Block);
            }


			// GET KEY
			public string GetKey(string key, string defaultValue)
			{
				EnsureKey(key, defaultValue);
				return Ini.Get(MainHeader, key).ToString();
			}

			// GET HEADER
			public string GetHeader(string header, string key, string defaultValue)
            {
				EnsureHeader(header, key, defaultValue);
				return Ini.Get(header, key).ToString();
			}


			// ENSURE KEY
			void EnsureKey(string key, string defaultValue)
			{
				if (!Ini.ContainsKey(MainHeader, key))
					SetKey(key, defaultValue);
			}

			// ENSURE HEADER
			void EnsureHeader(string header, string key, string defaultValue)
            {
				if (!Ini.ContainsKey(header, key))
					SetHeader(header, key, defaultValue);
			}


			// SET KEY
			public void SetKey(string key, string value)
			{
				Ini.Set(MainHeader, key, value);
				Block.CustomData = Ini.ToString();
			}

			// SET HEADER
			public void SetHeader(string header, string key, string value)
            {
				Ini.Set(header, key, value);
				Block.CustomData = Ini.ToString();
			}
		}

		// GET INI // Get entire INI object from specified block.
		public static MyIni GetIni(IMyTerminalBlock block)
		{
			MyIni iniOuti = new MyIni();

			MyIniParseResult result;
			if (!iniOuti.TryParse(block.CustomData, out result))
			{
				block.CustomData = "---\n" + block.CustomData;
				if (!iniOuti.TryParse(block.CustomData, out result))
					throw new Exception(result.ToString());
			}

			return iniOuti;
		}


		// ENSURE KEY // Check to see if INI key exists, and if it doesn't write with default value.
		public static void EnsureKey(IMyTerminalBlock block, string header, string key, string defaultVal)
		{
			//if (!block.CustomData.Contains(header) || !block.CustomData.Contains(key))
			MyIni ini = GetIni(block);
			if (!ini.ContainsKey(header, key))
				SetKey(block, header, key, defaultVal);
		}


		// GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
		public static string GetKey(IMyTerminalBlock block, string header, string key, string defaultVal)
		{
			EnsureKey(block, header, key, defaultVal);
			MyIni blockIni = GetIni(block);
			return blockIni.Get(header, key).ToString();
		}


		// SET KEY // Update ini key for block, and write back to custom data.
		public static void SetKey(IMyTerminalBlock block, string header, string key, string arg)
		{
			MyIni blockIni = GetIni(block);
			blockIni.Set(header, key, arg);
			block.CustomData = blockIni.ToString();
		}
    }
}
