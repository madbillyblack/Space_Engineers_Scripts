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
        // ADD PREFIX //
        public void AddPrefix(IMyTerminalBlock block, string prefix)
        {
            string temp_name = block.CustomName;
            if (block.IsSameConstructAs(Me) && !temp_name.StartsWith(prefix))
                block.CustomName = prefix + " " + temp_name;
        }


        // ADD SUFFIX //
        public void AddSuffix(IMyTerminalBlock block, string suffix)
        {
            string temp_name = block.CustomName;
            if (block.IsSameConstructAs(Me) && !temp_name.EndsWith(suffix))
                block.CustomName = temp_name + " " + suffix;

        }


        // DELETE PREFIX //
        public void DeletePrefix(IMyTerminalBlock block, string prefix)
        {
            if(block.CustomName.StartsWith(prefix))
            {
                string[] nameParts = block.CustomName.Split(' ');
                if (nameParts.Length > 1)
                {
                    string newName = "";
                    for(int i = 1; i < nameParts.Length; i++)
                    {
                        newName += nameParts[i] + " ";
                    }

                    block.CustomName = newName.Trim();
                }
                else
                {
                    block.CustomName = block.DefinitionDisplayNameText;
                }
            }
        }


        // DELETE SUFFIX //
        public void DeleteSuffix(IMyTerminalBlock block, string suffix)
        {
            if (block.CustomName.EndsWith(suffix))
            {
                string[] nameParts = block.CustomName.Split(' ');
                if (nameParts.Length > 1)
                {
                    string newName = "";
                    for (int i = 0; i < nameParts.Length - 1; i++)
                    {
                        newName += nameParts[i] + " ";
                    }

                    block.CustomName = newName.Trim();
                }
                else
                {
                    block.CustomName = block.DefinitionDisplayNameText;
                }
            }
        }


        // REPLACE PREFIX //
        public void ReplacePrefix(IMyTerminalBlock block, string oldTag, string newTag)
        {
            DeletePrefix(block, oldTag);
            AddPrefix(block, newTag);
        }


        // REPLACE SUFFIX //
        public void ReplaceSuffix(IMyTerminalBlock block, string oldTag, string newTag)
        {
            DeleteSuffix(block, oldTag);
            AddSuffix(block, newTag);
        }


        // SWAP TAG //
        public void SwapTag (IMyTerminalBlock block, string tag, bool swapToPrefix)
        {
            if(swapToPrefix)
            {
                DeleteSuffix(block, tag);
                AddPrefix(block, tag);
            }
            else
            {
                DeletePrefix(block, tag);
                AddSuffix(block, tag);
            }
        }


        // ADD TAGS //
        public void AddTags(string tag, bool toPrefix)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            if(toPrefix)
            {
                foreach (IMyTerminalBlock block in blocks)
                    AddPrefix(block, tag);
            }
            else
            {
                foreach (IMyTerminalBlock block in blocks)
                    AddSuffix(block, tag);
            }      
        }


        // REMOVE TAGS //
        public void RemoveTags(string tag, bool fromPrefix)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            if (fromPrefix)
            {
                foreach (IMyTerminalBlock block in blocks)
                    DeletePrefix(block, tag);
            }
            else
            {
                foreach (IMyTerminalBlock block in blocks)
                    DeleteSuffix(block, tag);
            }
        }


        // SWAP TAGS //
        public void SwapTags(string tag, bool toPrefix)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            if (toPrefix)
            {
                foreach (IMyTerminalBlock block in blocks)
                {
                    DeleteSuffix(block, tag);
                    AddPrefix(block, tag);
                }
                    
            }
            else
            {
                foreach (IMyTerminalBlock block in blocks)
                {
                    DeletePrefix(block, tag);
                    AddSuffix(block, tag);
                }
            }
        }


        // REPLACE TAGS //
        public void ReplaceTags(string [] tags, bool replacePrefix)
        {
            if (tags.Length < 3)
            {
                _statusMessage = "Insufficient arguments for TAG REPLACEMENT.\n * Be sure to include the old tag to be replaced as well as the new tag.";
                return;
            }

            string oldTag = tags[1];
            string newTag = tags[2];

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            if(replacePrefix)
            {
                foreach (IMyTerminalBlock block in blocks)
                    ReplacePrefix(block, oldTag, newTag);
            }
            else
            {
                foreach (IMyTerminalBlock block in blocks)
                    ReplaceSuffix(block, oldTag, newTag);
            }
        }
    }
}
