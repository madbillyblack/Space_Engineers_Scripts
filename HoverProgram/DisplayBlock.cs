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
        const string DISPLAY_TAG = "[Hover Display]";
        const string DISPLAY_HEADER = "Hover Display";
        const string VAR_TAG = "Show on screen ";
        List<DisplayBlock> _displayBlocks;

        readonly string[] _breather = { @"\/////////",
                                        @"/\////////",
                                        @"//\///////",
                                        @"///\//////",
                                        @"////\/////",
                                        @"/////\////",
                                        @"//////\///",
                                        @"///////\//",
                                        @"////////\/",
                                        @"/////////\"};

        public string _currentBreath;
        public int _cycleCount;
        public string _data;

        public class DisplayBlock
        {
            MyIni Ini;
            public List<IMyTextSurface> Surfaces;
            public IMyTerminalBlock Block;

            public DisplayBlock (IMyTerminalBlock block)
            {
                
                Surfaces = new List<IMyTextSurface> ();
                IMyTextSurfaceProvider provider = block as IMyTextSurfaceProvider;
                Block = block;
                Ini = GetIni(Block);

                if (provider.SurfaceCount == 1)
                    PrepareSurface(provider.GetSurface(0));
                else
                {
                    if(GetKey(VAR_TAG + "0", true))
                        PrepareSurface(provider.GetSurface(0));

                    for (int i = 1; i < provider.SurfaceCount; i++)
                    {
                        if (GetKey(VAR_TAG + i, false))
                            PrepareSurface(provider.GetSurface(i));
                    }
                }
            }

            public void SetKey(string key, bool value)
            {
                Ini.Set(DISPLAY_HEADER, key, value);
                Block.CustomData = Ini.ToString();
            }

            public bool GetKey(string key, bool defaultBool)
            {
                if (!Ini.ContainsKey(DISPLAY_HEADER, key))
                {
                    SetKey(key, defaultBool);
                    return defaultBool;
                }

                return Ini.Get(DISPLAY_HEADER, key).ToBoolean();
            }

            public void WriteData(string data)
            {
                if (Surfaces.Count < 1) return;

                foreach (IMyTextSurface surface in Surfaces)
                    surface.WriteText(data);
            }

            void PrepareSurface(IMyTextSurface surface)
            {
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
                Surfaces.Add(surface);
            }
        }


        // ADD DISPLAYS //
        public void AddDisplays()
        {
            _displayBlocks = new List<DisplayBlock>();

            List<IMyTerminalBlock> taggedBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(DISPLAY_TAG, taggedBlocks);

            if (taggedBlocks.Count < 1) return;

            foreach (IMyTerminalBlock block in taggedBlocks)
            {
                if(SameGridID(block) && (block as IMyTextSurfaceProvider).SurfaceCount > 0)
                    _displayBlocks.Add(new DisplayBlock(block));
            }
        }


        // DISPLAY DATA //
        public void DisplayData()
        {
            _data = "// HOVER PROGRAM " + _currentBreath +"\nMode: " + _mode + "   Target Height: " + _hoverHeight +
                            "m\nGains: " + _kP.ToString("0.##") + ", " + _kI.ToString("0.##") + ", " + _kD.ToString("0.##") +
                            "\nCmd: " + _lastCommand + "\nMsg:\n" + _statusMessage;

            if (_displayBlocks.Count < 1) return;

            foreach (DisplayBlock block in _displayBlocks)
                block.WriteData(_data);
        }


        // CYCLE BREATH //
        public void CycleBreath()
        {
            if (_cycleCount % 10 == 0)
            {
                _currentBreath = _breather[_cycleCount / 10];
                DisplayData();
            }

            _cycleCount++;
            if (_cycleCount > 99)
                _cycleCount = 0;
        }
    }
}
