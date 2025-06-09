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
        public class Logger
        {
            public List<string> Messages;
            public int StartIndex;
            public string Command;

            private enum Level { INFO, WARNING, ERROR};

            public Logger()
            {
                Messages = new List<string>();
                StartIndex = 0;
                Command = "";
            }

            public void LogInfo(string msg) { LogMessage(Level.INFO, msg); }
            public void LogWarning(string msg) { LogMessage(Level.WARNING, msg); }
            public void LogError(string msg) { LogMessage(Level.ERROR, msg); }

            private void LogMessage(Level level, string message)
            {
                string prefix;

                switch(level)
                {
                    case Level.WARNING:
                        prefix = "WARNING: ";
                        break;
                    case Level.ERROR:
                        prefix = "ERROR: ";
                        break;
                    case Level.INFO:
                    default:
                        prefix = "* ";
                        break;
                }

                Messages.Add(prefix + message);
            }

            public void Scroll(bool scrollBack = false)
            {
                if (Messages.Count < 2) return;

                if(scrollBack) 
                    StartIndex--;
                else
                    StartIndex++;

                if (StartIndex < 0)
                    StartIndex = Messages.Count - 1;
                else if(StartIndex >= Messages.Count)
                    StartIndex = 0;
            }

            public string PrintMessages()
            {
                string output = "";

                if (!String.IsNullOrEmpty(Command))
                    output += " CMD: " + Command+ "\n";

                output += "-- Message Log " + DASHES + DASHES;

                if (Messages.Count < 1) return output;

                for(int i = StartIndex; i < Messages.Count; i++)
                {
                    output += "\n" + Messages[i].ToString();
                }
                
                return output + "\n" + DASHES + DASHES + DASHES;
            }
        }
    }
}
