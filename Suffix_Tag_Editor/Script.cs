/*

SUFFIX EDITOR
-------------------------------------------------
Allows user to add, change, and delete tags from the end of all terminal blocks of a grid in one go.

HOW TO USE
------------------------------------------------------
Run program with one of the following arguments (case sensitive):

NewTag
-----------
Run this argument followed by the tag you want added to the end of all your block names
Example: "NewTag ABC" (without quotation marks) will add "ABC" to the end of every terminal block

Rename
-----------
Just like NewTag run this followed by a tag that you want all current tags to be changed to.
Example: If your ship blocks have already been tagged with "ABC" and you run argument "Rename XYZ" all "ABC" tags will be replaced with
"XYZ"

ClearTag
------------
Run this argument by itself.  If your blocks have been tagged, and you run this script, it will remove the suffix tags.


WARNINGS!!! (Please Read)
***********************************
I wrote this script mainly for personal use, and so I have not put a lot of safety features in yet.  Beware of the following:

- ClearTag and Rename both work by deleting the last word in a terminal block's name.  DO NOT run them unless all blocks already have
  a tag, or at least two words in their name.  If there is only one word in the block's name, it's name might be blanked out if ClearTag is run.

- This script will rename all terminal blocks in all attached grids.  Try not to be docked to anything you don't want tagged.


*/




public void Main(string argument)
{
    string[] argArr = argument.Split(' ');
    string action = argArr[0];
    switch (action)
    {
        case "NewTag":
            newTag(argArr[1]);
            break;
        case "ClearTag":
            clearTag();
            break;
        case "Rename":
            clearTag();
            newTag(argArr[1]);
            break;
    }
}


void newTag(string tag)
{
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(blocks);
    string temp_name = "";
    for (int i = 0; i<blocks.Count; i++)
    {
        temp_name = blocks[i].CustomName;
        if(Me.CubeGrid == blocks[i].CubeGrid && !temp_name.EndsWith(tag))
        {
            blocks[i].SetCustomName(temp_name + " " + tag);
        }
    }
}


void clearTag()
{
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>(); 
    GridTerminalSystem.GetBlocks(blocks);
    for (int i = 0; i<blocks.Count; i++)
    {
        string old_name = blocks[i].CustomName;
        string new_name = old_name.Substring(0, old_name.LastIndexOf(' '));
        //new_name = new_name.Strip;
        blocks[i].SetCustomName(new_name);
    }
}