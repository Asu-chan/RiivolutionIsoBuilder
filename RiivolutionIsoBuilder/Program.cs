using ExtensionMethods;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace RiivolutionIsoBuilder
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Asu's Riivolution ISO Builder - v1.0.0 - Build Date: " + Properties.Resources.BuildDate);
            // Console.WriteLine("Special Beta Test Build - Please build all the mods you can think of with this ISO Builder and report me any bugs on discord: Asu-chan#2929\r\n\r\n");
            RiivolutionUniversalISOBuilder ruib = new RiivolutionUniversalISOBuilder();
            ruib.Main(args);
        }
    }

    public class RiivolutionUniversalISOBuilder
    {
        public void Main(string[] args)
        {
            string ISOPath = "", xmlPath = "", outPath = "", singleChoice = "Ask", newTitleID = "", newGameName = "";
            bool isSilent = false, deleteISO = true, isThereDefinedPaths = false, skipAllDone = false;
            int ignoreLevel = 0;
            if (args.Length > 0) // Arguments
            {
                if (args[0] == "-h" || args[0] == "--help" || args[0] == "/?")
                {
                    Console.WriteLine("A tool to patch Nintendo Wii ISO files using Riivolution XML files.\r\n\r\n");
                    Console.WriteLine("Usage: UniversalISOBuilder.exe <ISO Path> <Riivolution XML file path> <Output ISO/WBFS path> [options]");
                    Console.WriteLine("       UniversalISOBuilder.exe [options]");
                    Console.WriteLine("       UniversalISOBuilder.exe");
                    Console.WriteLine("       In the 2nd and 3rd cases, you will be asked for the file paths.\r\n\r\n"); // Thanks to Mullkaw for correcting my weird-sounding english! ^^
                    Console.WriteLine("Options: --silent                  -> Prevents from displaying any console outputs apart from the necessary ones");
                    Console.WriteLine("         --always-single-choice    -> Enables any option that only has one choice");
                    Console.WriteLine("         --never-single-choice     -> Disable any option that only has one choice");
                    Console.WriteLine("         --title-id <TitleID>      -> Changes the TitleID of the output rom; Use periods in place of characters in the TitleID that shouldn't be replaced.");
                    Console.WriteLine("         --game-name <Game name>   -> Changes the game name of the output rom; If the name you want to set contains spaces, put it between quotes.");
                    Console.WriteLine("         --keep-extracted-iso      -> Prevents the temporary extracted patched rom folder from being deleted at the end of the build process");
                    Console.WriteLine("         --ignore-warnings         -> If the builder hits any warning, it'll be ignored and building will proceed [USE IF YOU KNOW WHAT YOU ARE DOING]");
                    Console.WriteLine("         --ignore-errors           -> If the builder hits any error or warning, it'll be ignored and building will proceed [USE IF YOU KNOW WHAT YOU ARE DOING]");
                    return;
                }

                if (args.Contains("--silent"))
                {
                    isSilent = true;
                    Console.WriteLine("Silent Mode: true");
                }

                if (args.Contains("--skip-all-done")) // Not putting this in the help page, it's just for my personal use
                {
                    skipAllDone = true;
                    Console.WriteLine("Skip All Done: true");
                }

                if (args.Contains("--always-single-choice"))
                {
                    singleChoice = "Always";
                }
                else if (args.Contains("--never-single-choice"))
                {
                    singleChoice = "Never";
                }

                if (args.Contains("--title-id"))
                {
                    newTitleID = args[Array.IndexOf(args, "--title-id") + 1];
                    if (newTitleID.Length != 6) { Console.WriteLine("Invalid TitleID " + newTitleID + "."); return; }
                    Console.WriteLine("TitleID Change: " + newTitleID);
                }

                if (args.Contains("--game-name"))
                {
                    newGameName = args[Array.IndexOf(args, "--game-name") + 1];
                    Console.WriteLine("Game name Change: " + newGameName);
                }

                if (args.Contains("--keep-extracted-iso"))
                {
                    deleteISO = false;
                }


                if (args.Contains("--ignore-errors"))
                {
                    Console.WriteLine("Ignore Errors & Warnings: true");
                    ignoreLevel = 2;
                }
                else if (args.Contains("--ignore-warnings"))
                {
                    Console.WriteLine("Ignore Warnings: true");
                    ignoreLevel = 1;
                }

                if (args[0].Contains(".iso") || args[0].Contains(".wbfs"))
                {
                    ISOPath = args[0];
                    xmlPath = args[1];
                    outPath = args[2];
                    isThereDefinedPaths = true;
                }

                if (isThereDefinedPaths) // Paths are already defined? Directly do the stuff.
                {
                    if (!System.IO.File.Exists(ISOPath) || !System.IO.File.Exists(xmlPath))
                    {
                        Console.WriteLine("Can't find ISO or XML file: No such file or directory.");
                    }

                    doStuff(ISOPath, xmlPath, outPath, singleChoice, newTitleID, newGameName, isSilent, deleteISO, ignoreLevel, skipAllDone);
                    return;
                }
            }
            if (args.Length == 0 || !isThereDefinedPaths) // No paths defined? Ask for them.
            {
                // Getting ISO path
                Console.WriteLine("Please select an ISO/WBFS file to patch.");
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Nintendo Wii ISO Rom File|*.iso|Nintendo Wii WBFS Rom File|*.wbfs|All files (*.*)|*.*";
                    dialog.FilterIndex = 1;
                    dialog.RestoreDirectory = true;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        Console.WriteLine("Please select a Riivolution XML file.");

                        // Getting XML path
                        using (OpenFileDialog dialog2 = new OpenFileDialog())
                        {
                            dialog2.Filter = "Riivolution Extensible Markup Language File|*.xml|All files (*.*)|*.*";
                            dialog2.FilterIndex = 1;
                            dialog2.RestoreDirectory = true;
                            if (dialog2.ShowDialog() == DialogResult.OK)
                            {
                                Console.WriteLine("Please choose where you want your patched rom file to be saved.");

                                // Getting output ISO/WBFS path
                                SaveFileDialog textDialog;
                                textDialog = new SaveFileDialog();
                                textDialog.Filter = "Nintendo Wii ISO Rom File|*.iso|Nintendo Wii WBFS Rom File|*.wbfs|All files (*.*)|*.*";
                                textDialog.DefaultExt = "wbfs";
                                if (textDialog.ShowDialog() == DialogResult.OK)
                                {
                                    System.IO.Stream fileStream = textDialog.OpenFile();
                                    System.IO.StreamWriter sw = new System.IO.StreamWriter(fileStream);
                                    outPath = ((FileStream)(sw.BaseStream)).Name;
                                    sw.Close();

                                    doStuff(dialog.FileName, dialog2.FileName, outPath, singleChoice, newTitleID, newGameName, isSilent, deleteISO, ignoreLevel, skipAllDone);
                                }
                                else
                                {
                                    Console.WriteLine("ISO/WBFS saving cancelled. Closing...");
                                    return;
                                }
                            }
                            else
                            {
                                Console.WriteLine("XML selecting cancelled. Closing...");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("ISO selecting cancelled. Closing...");
                        return;
                    }
                }
            }
        }

        RiivoDisc disc = new RiivoDisc();

        string gameID = "";
        string region = "";
        string maker = "";

        public void doStuff(string ISOPath, string xmlPath, string outPath, string singleChoice, string newTitleID, string newGameName, bool isSilent, bool deleteISO, int ignoreLevel, bool skipAllDone)
        {
            Random random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string extDir = Path.GetDirectoryName(Application.ExecutablePath) + "\\" + Path.GetFileNameWithoutExtension(xmlPath) + "-" + new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());

            disc = RiivoDisc.ParseString(xmlPath);
            string rootPath = Path.GetFullPath(Path.Combine(xmlPath, @"..\..\"));

            List<byte> id = new List<byte>();
            List<byte> name = new List<byte>();
            using (FileStream iso = new FileStream(ISOPath, FileMode.Open))
            {
                string header = "";
                for (int i = 0; i < 4; i++)
                {
                    char c = ((char)iso.ReadByte());
                    header += c;
                    if(!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                    {
                        Console.WriteLine("ERROR: This isn't a Nintendo Wii ROM File");
                        if(ignoreLevel < 2) return;
                    }
                }
                if (header == "WBFS")
                {
                    iso.Position = 0x200;
                    for (int i = 0; i < 8; i++)
                    {
                        id.Add((byte)iso.ReadByte());
                    }

                    iso.Position = 0x220;

                    while (true)
                    {
                        byte nameByte = (byte)iso.ReadByte();
                        if (nameByte == 0)
                        {
                            break;
                        }
                        name.Add(nameByte);
                    }
                }
                else
                {
                    iso.Position = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        id.Add((byte)iso.ReadByte());
                    }

                    iso.Position = 0x20;

                    while (true)
                    {
                        byte nameByte = (byte)iso.ReadByte();
                        if (nameByte == 0)
                        {
                            break;
                        }
                        name.Add(nameByte);
                    }
                }
            }

            string titleID = Encoding.UTF8.GetString(id.ToArray(), 0, 6);
            string gameName = Encoding.UTF8.GetString(name.ToArray(), 0, name.Count);

            gameID = titleID.Substring(0, 3);
            region = titleID.Substring(3, 1);
            maker = titleID.Substring(4, 2);

            Console.WriteLine("TitleID found: " + titleID + "\r\nGame name found: " + gameName + "\r\n");

            if (disc.gameFilter.game != gameID)
            {
                Console.WriteLine("Warning: This riivolution patch only applied to the game that has the TitleID " + disc.gameFilter.game + ".\r\nYours uses the TitleID " + gameID + " and therefore cannot be patched.");
                if (ignoreLevel < 1)
                {
                    Console.WriteLine("If you really know what you're doing and want to ignore this, try to run this tool again with the \"--ignore-warnings\" option.");
                    return;
                }
            }

            if (disc.gameFilter.regions.Count > 0)
            {
                if (!disc.gameFilter.regions.Contains(region))
                {
                    Console.WriteLine("Unsupported region (" + region + ").");
                    if (ignoreLevel < 1)
                    {
                        Console.WriteLine("If you really know what you're doing and want to ignore this, try to run this tool again with the \"--ignore-warnings\" option.");
                        return;
                    }
                }
            }

            if (newTitleID == "")
            {
                Console.WriteLine("No custom Title ID were specified, therefore your mod will use the same save slot as the game you're modding, which could cause issues with some mods!");
            }

            Console.WriteLine("Extracting ISO...");

            if (ISOPath.Contains(" "))
            {
                ISOPath = "\"" + ISOPath + "\"";
            }

            runCommand("tools\\wit.exe", "extract -s " + ISOPath + " -1 -n " + titleID + " . \"" + extDir + "\" --psel=DATA -ovv", isSilent);

            Console.WriteLine("ISO Extracted.");

            List<Patch> patches = new List<Patch>();

            if (!(disc.sections.Count == 1 && disc.sections[0].options.Count == 1 && disc.sections[0].options[0].choices.Count == 1))
            {
                foreach (Section section in disc.sections)
                {
                    Console.WriteLine("-Section: \"" + section.name + "\"");
                    foreach (Option option in section.options)
                    {
                        Console.WriteLine("  Option: \"" + option.name + "\"");
                        if (option.choices.Count > 1)
                        {
                            Console.WriteLine("   Choices available:\r\n    0. None");
                            List<string> choices = new List<string>();
                            for (int i = 0; i < option.choices.Count; i++)
                            {
                                Console.WriteLine("    " + (i + 1) + ". " + option.choices[i].name);
                                choices.Add(option.choices[i].name);
                            }
                            int choosed = -1;
                            while (true)
                            {
                                Console.Write("Please enter the number of the choice you want to use: ");
                                try
                                {
                                    choosed = Convert.ToInt32(Console.ReadLine());
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("That's not a number!");
                                    continue;
                                }

                                if (choosed <= choices.Count && choosed >= 0)
                                {
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("This isn't a valid choice!");
                                }
                            }
                            if (choosed == 0)
                            {
                                continue;
                            }
                            foreach (PatchReference patchref in option.choices[choosed - 1].patchReferences)
                            {
                                int patchToFind = findPatchIndexByName(patchref.id);
                                if(patchToFind < 0)
                                {
                                    Console.WriteLine("Unable to find patch " + patchref.id);
                                    if (ignoreLevel < 1)
                                    {
                                        Console.WriteLine("If you really know what you're doing and want to ignore this, try to run this tool again with the \"--ignore-warnings\" option.");
                                        return;
                                    }
                                }
                                patches.Add(disc.patches[patchToFind]);
                            }
                        }
                        else
                        {
                            string answer = "";
                            if (singleChoice == "Ask")
                            {
                                Console.Write("   Only one choice found: " + option.choices[0].name + " - Use it? (Yes/No): ");
                                answer = Console.ReadLine();
                            }
                            else if (singleChoice == "Always")
                            {
                                answer = "yes";
                            }

                            if (answer.Equals("yes", StringComparison.InvariantCultureIgnoreCase) || answer.Equals("y", StringComparison.InvariantCultureIgnoreCase))
                            {
                                foreach (PatchReference patchref in option.choices[0].patchReferences)
                                {
                                    int patchToFind = findPatchIndexByName(patchref.id);
                                    if (patchToFind < 0)
                                    {
                                        Console.WriteLine("Unable to find patch " + patchref.id);
                                        if (ignoreLevel < 1)
                                        {
                                            Console.WriteLine("If you really know what you're doing and want to ignore this, try to run this tool again with the \"--ignore-warnings\" option.");
                                            return;
                                        }
                                    }
                                    patches.Add(disc.patches[patchToFind]);
                                }
                            }
                        }
                    }
                }
            }
            else // No need to ask what to enable and what to disable if there's only one possiblity.
            {
                foreach (PatchReference patchref in disc.sections[0].options[0].choices[0].patchReferences)
                {
                    int patchToFind = findPatchIndexByName(patchref.id);
                    if (patchToFind < 0)
                    {
                        Console.WriteLine("Unable to find patch " + patchref.id);
                        if (ignoreLevel < 1)
                        {
                            Console.WriteLine("If you really know what you're doing and want to ignore this, try to run this tool again with the \"--ignore-warnings\" option.");
                            return;
                        }
                    }
                    patches.Add(disc.patches[patchToFind]);
                }
            }

            Dolpatcher dp = new Dolpatcher(extDir + "\\sys\\main.dol", isSilent);

            System.Diagnostics.Process copy = new System.Diagnostics.Process();
            copy.StartInfo.FileName = "cmd.exe";
            copy.StartInfo.UseShellExecute = false;
            copy.StartInfo.RedirectStandardOutput = true;
            copy.StartInfo.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            foreach (Patch patch in patches)
            {
                if (patch.root.StartsWith("\\"))
                    patch.root = patch.root.Substring(1);

                Console.WriteLine("Patching " + patch.id + "...");
                Console.WriteLine("Got " + patch.filePatches.Count + " File Patches.");
                Console.WriteLine("Got " + patch.folderPatches.Count + " Folder Patches.");
                Console.WriteLine("Got " + patch.memoryPatches.Count + " Memory Patches.");
                foreach(File filePatch in patch.filePatches)
                {
                    doStringTIDReplacements(ref filePatch.external);

                    string file = rootPath + patch.root + "\\" + filePatch.external;
                    string extPath = extDir + "\\files" + filePatch.disc;
                    //Console.WriteLine("Copying " + path + " to " + extPath);

                    if ((filePatch.disc == null || filePatch.disc == "") ? true : filePatch.create) // Avoid running useless copy commands for files that don't exist AND don't have the "created" flag enabled.
                    {
                        if (filePatch.disc != "" && filePatch.disc != null)
                        {
                            if (!isSilent) Console.WriteLine("Copying " + patch.root + "\\" + filePatch.external);

                            copy.StartInfo.Arguments = "/C copy /b \"" + file + "\" \"" + extPath + "\"";
                        }
                        else if (System.IO.File.Exists(file))
                        {
                            if (!isSilent) Console.WriteLine("Searching manually for file named " + Path.GetFileName(file));
                            string foundFile = ProcessDirectory(extDir + "\\files\\", Path.GetFileName(file));
                            if (foundFile != "")
                            {
                                if (!isSilent) Console.WriteLine("Found file " + foundFile);
                                copy.StartInfo.Arguments = "/C copy /b \"" + file + "\" \"" + foundFile + "\"";
                            }
                            else
                            {
                                if (!isSilent) Console.WriteLine("Cannot find file " + file + " in the disc\r\n");
                                continue;
                            }
                            if (!isSilent) { Console.WriteLine(""); } // Just for good-looking purposes.
                            continue;
                        }

                        copy.Start();
                        if (!isSilent) { Console.WriteLine(copy.StandardOutput.ReadToEnd()); } else { copy.StandardOutput.ReadToEnd(); }
                        copy.WaitForExit();
                    }
                }

                foreach(Folder folderPatch in patch.folderPatches)
                {
                    doStringTIDReplacements(ref folderPatch.external);

                    string path = rootPath + patch.root + "\\" + folderPatch.external;
                    string extPath = extDir + "\\files" + ((folderPatch.disc == "root") ? "" : folderPatch.disc);
                    //Console.WriteLine("Copying " + path + " to " + extPath);

                    if (folderPatch.create && !Directory.Exists(extPath))
                    {
                        if (!isSilent) Console.WriteLine("Creating directory " + extPath);
                        runCommand("cmd.exe", "/C mkdir \"" + extPath + "\"", isSilent);
                    }

                    if ((folderPatch.disc == null || folderPatch.disc == "") ? true : Directory.Exists(extPath)) // Avoid running useless copy commands for folders that doesn't exist AND don't have the "created" flag enabled (this is used for region-specific folders in NewerSMBW, for example)
                    {
                        if (folderPatch.disc != "" && folderPatch.disc != null)
                        {
                            if (!isSilent) Console.WriteLine("Copying " + path + " to " + extPath);
                            string recursive = "";
                            if (folderPatch.recursive == true)
                            {
                                recursive = " /E";
                            }
                            copy.StartInfo.Arguments = "/C xcopy \"" + path + "\" \"" + extPath + "\"" + recursive + " /C /I /Y";
                        }
                        else if (Directory.Exists(path))
                        {
                            if (!isSilent) Console.WriteLine("Searching manually for files contained in " + path);
                            foreach (string file in Directory.GetFiles(path))
                            {
                                if (!isSilent) Console.WriteLine("Searching for " + file);
                                string foundFile = ProcessDirectory(extDir + "\\files\\", Path.GetFileName(file));
                                if (foundFile != "")
                                {
                                    if (!isSilent) Console.WriteLine("Found file " + foundFile);
                                    copy.StartInfo.Arguments = "/C copy /b \"" + file + "\" \"" + foundFile + "\"";

                                    copy.Start();
                                    if (!isSilent) Console.WriteLine(copy.StandardOutput.ReadToEnd());
                                    copy.WaitForExit();
                                }
                                else
                                {
                                    if (!isSilent) Console.WriteLine("Cannot find file " + file + " in the disc\r\n");
                                    continue;
                                }
                            }
                            if (!isSilent) Console.WriteLine(""); // Just for good-looking purposes.
                            continue;
                        }

                        copy.Start();
                        if (!isSilent) { Console.WriteLine(copy.StandardOutput.ReadToEnd()); } else { copy.StandardOutput.ReadToEnd(); }
                        copy.WaitForExit();
                    }
                }

                foreach (Memory memoryPatch in patch.memoryPatches)
                {
                    if(memoryPatch.valueFile == "")
                    {
                        if (!isSilent) Console.WriteLine("Trying to patch " + memoryPatch.value.AsString() + " at 0x" + memoryPatch.offset.ToString("X8") + "...");

                        bool success = dp.doMemoryPatch(memoryPatch.offset, memoryPatch.value.ToArray(), memoryPatch.original.ToArray());

                        if (!isSilent && success) Console.WriteLine("Success.\r\n");
                        else if (!isSilent && !success) Console.WriteLine("Couldn't apply this patch (This isn't necessarely an error, this is usually meant for different regions support).\r\n");
                    }
                    else
                    {
                        doStringTIDReplacements(ref memoryPatch.valueFile);
                        string memFilePath = rootPath + patch.root + "\\" + memoryPatch.valueFile;
                        if(System.IO.File.Exists(memFilePath))
                        {
                            if(!isSilent) Console.WriteLine("Binary file " + memoryPatch.valueFile + " found, patching...");

                            bool success = dp.doMemoryPatch(memoryPatch.offset, System.IO.File.ReadAllBytes(memFilePath), memoryPatch.original.ToArray());

                            if (!isSilent && success) Console.WriteLine("Binary file " + memoryPatch.valueFile + " patched successfully.\r\n");
                            else if (!isSilent && !success) Console.WriteLine("Binary file " + memoryPatch.valueFile + " coudln't be patched.\r\n");
                        }
                    }
                }
            }

            if (!skipAllDone)
            {
                if (disc.hasSaveGamePatches)
                {
                    MessageBox.Show("WARNING: This mod uses savegame patches, which can't be integrated into an ISO/WBFS.\r\n" +
                        "This isn't a problem for most mods, but by default the same save slot as the game you're modding will be used for the mod you're trying to build.\r\n" +
                        "THIS PROBLEM CAN BE AVOIDED BY CHANGING THE TITLEID OF YOUR MOD, SEE HELP PAGE (-h or --help) FOR MORE INFO\r\n\r\n" +
                        "However, some mods use this kind of patch to have the default save file changed."/*" If this is your case, ask for support on discord at Asu-chan#2929"*/, "Memory Patches Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (disc.badMemPatches)
                {
                    MessageBox.Show("WARNING: Due to some memory patches being applied under 0x80004000, your game may not work on USB Loaders.\r\n\r\n" +
                        "This does NOT necessarely mean that any error occured; If you play your patched rom on Dolphin Emulator you should be fine.\r\n\r\n" +
                        "A fix for this is planned, but it's no easy task so don't expect it to see the light of the day too quickly.\r\n\r\n" +
                        "If you can, ask your mod's maker to put their code hacks in a different place.", "Memory Patches Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            dp.saveDol();

            Console.WriteLine("Done patching, rebuilding...");

            if (newTitleID != "")
            {
                string oldtid = titleID;
                char[] oldttid = titleID.ToCharArray();
                char[] newttid = newTitleID.ToCharArray();
                for (int i = 0; i < 6; i++)
                {
                    if (newttid[i] == '.')
                    {
                        continue;
                    }
                    oldttid[i] = newttid[i];
                }
                titleID = new string(oldttid);
                gameID = titleID.Substring(0, 3);
                Console.WriteLine("Changing TitleID from " + oldtid + " to " + newTitleID + " -> " + titleID);
            }

            if(newGameName != "")
            {
                Console.WriteLine("Changing game name from " + gameName + " to " + newGameName);
                gameName = newGameName;
            }
            else
            {
                gameName += " [MODDED]";
            }

            Console.WriteLine("");

            runCommand("tools\\wit.exe", "copy \"" + extDir + "\" \"" + outPath + "\" -ovv --disc-id=" + titleID + " --tt-id=" + gameID + " --name \"" + gameName + "\"", isSilent);

            if (deleteISO)
            {
                if (!isSilent) { Console.WriteLine("Removing extracted patched rom directory..."); }
                runCommand("cmd.exe", "/C rmdir \"" + extDir + "\" /s /q", isSilent);
                if (!isSilent) { Console.WriteLine("Removed sucessfully"); }
            }

            Console.WriteLine("All done!");

            if(!skipAllDone) Console.ReadLine();
        }

        // This part is from https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.getfiles?view=net-5.0
        public static string ProcessDirectory(string targetDirectory, string wantedFile)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                string maybeFound = ProcessFile(fileName, wantedFile);
                if (maybeFound != "")
                {
                    return maybeFound;
                }
            }

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                string maybeFound = ProcessDirectory(subdirectory, wantedFile);
                if (maybeFound != "")
                {
                    return maybeFound;
                }
            }

            return "";
        }
        public static string ProcessFile(string path, string wantedFile)
        {
            string filename = Path.GetFileName(path);
            if (filename.Equals(wantedFile, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
            return "";
        }

        public void doStringTIDReplacements(ref string str)
        {
            if (str.Contains("{$__region}"))
                str = str.Replace("{$__region}", region);
            if (str.Contains("{$__gameid}"))
                str = str.Replace("{$__gameid}", gameID);
            if (str.Contains("{$__maker}"))
                str = str.Replace("{$__maker}", maker);
        }

        public int findPatchIndexByName(string name)
        {
            for(int i = 0; i < disc.patches.Count; i++)
            {
                if(disc.patches[i].id == name)
                {
                    return i;
                }
            }

            return -1;
        }

        public void runCommand(string executable, string arguments, bool isSilent)
        {
            System.Diagnostics.Process command = new System.Diagnostics.Process();
            command.StartInfo.FileName = executable;
            command.StartInfo.Arguments = arguments;
            command.StartInfo.UseShellExecute = false;
            command.StartInfo.RedirectStandardOutput = true;
            command.StartInfo.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            command.Start();
            while (!command.StandardOutput.EndOfStream && !isSilent)
            {
                Console.WriteLine(command.StandardOutput.ReadLine());
            }
            command.WaitForExit();
        }
    }
}
