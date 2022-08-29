using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RiivolutionIsoBuilder
{

    public class GameFilter
    {
        public string game;
        public string developer;
        public int disc;
        public int version;
        public List<string> regions;

        public GameFilter()
        {
            game = "";
            developer = "";
            disc = 0;
            version = 0;
            regions = new List<string>();
        }
    }

    public class PatchReference
    {
        public string id;
        public Dictionary<string, string> param;

        public PatchReference()
        {
            id = "";
            param = new Dictionary<string, string>();
        }
    }

    public class Choice
    {
        public string name;
        public List<PatchReference> patchReferences;

        public Choice()
        {
            name = "";
            patchReferences = new List<PatchReference>();
        }
    }

    public class Option
    {
        public string name;
        public string id;
        public List<Choice> choices;
        public uint selectedChoice;

        public Option()
        {
            name = "";
            id = "";
            choices = new List<Choice>();
            selectedChoice = 0;
        }
    }

    public class Section
    {
        public string name;
        public List<Option> options;

        public Section()
        {
            name = "";
            options = new List<Option>();
        }
    }

    public class File
    {
        public string disc;
        public string external;
        public bool resize;
        public bool create;
        public uint offset;
        public uint fileOffset;
        public uint length;

        public File()
        {
            disc = "";
            external = "";
            resize = true;
            create = false;
            offset = 0;
            fileOffset = 0;
            length = 0;
        }
    }

    public class Folder
    {
        public string disc;
        public string external;
        public bool resize;
        public bool create;
        public bool recursive;
        public uint length;

        public Folder()
        {
            disc = "";
            external = "";
            resize = true;
            create = false;
            recursive = true;
            length = 0;
        }
    }

    public class Savegame
    {
        public string external;
        public bool clone;

        public Savegame()
        {
            external = "";
            clone = true;
        }
    }

    public class Memory
    {
        public uint offset;
        public List<byte> value;
        public string valueFile;
        public List<byte> original;
        public bool ocarina;
        public bool search;
        public uint align;

        public Memory()
        {
            offset = 0;
            value = new List<byte>();
            valueFile = "";
            original = new List<byte>();
            ocarina = false;
            search = false;
            align = 1;
        }
    }

    public class Patch
    {
        public string id;
        public string root;

        //std::shared_ptr<FileDataLoader> m_file_data_loader;

        public List<File> filePatches;
        public List<Folder> folderPatches;
        public List<Savegame> savegamesPatches;
        public List<Memory> memoryPatches;

        public Patch()
        {
            id = "";
            root = "";
            filePatches = new List<File>();
            folderPatches = new List<Folder>();
            savegamesPatches = new List<Savegame>();
            memoryPatches = new List<Memory>();
        }
    }
    public class RiivoDisc
    {
        public int version;
        public GameFilter gameFilter;
        public List<Section> sections;
        public List<Patch> patches;
        public string xmlPath;

        public bool badMemPatches = false;
        public bool hasSaveGamePatches = false;

        public RiivoDisc()
        {
            version = 0;
            gameFilter = new GameFilter();
            sections = new List<Section>();
            patches = new List<Patch>();
            xmlPath = "";
            badMemPatches = false;
            hasSaveGamePatches = false;
        }


        public static RiivoDisc ParseString(string path)
        {
            Console.WriteLine("Parsing XML file...");
            long time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(System.IO.File.ReadAllText(path));
            XmlNode wiidisc = doc.DocumentElement;

            //Console.WriteLine(wiidisc.Attributes["version"].Value);
            //Console.WriteLine(wiidisc["id"].Attributes["game"].Value);
            //Console.WriteLine(wiidisc["id"].GetElementsByTagName("region")[1].Attributes["type"].Value);


            RiivoDisc disc = new RiivoDisc();
            disc.xmlPath = path;
            disc.version = wiidisc.Attributes["version"].Value().AsInt32(disc.version);
            if (disc.version != 1)
            {
                Console.WriteLine("Unsupported Riivolution version " + disc.version);
            }

            string defaultRoot = wiidisc.Attributes["root"].Value();
            XmlNode id = wiidisc["id"];
            if (id != null)
            {
                foreach (XmlAttribute attribute in id.Attributes)
                {
                    if (attribute.Name == "game")
                        disc.gameFilter.game = attribute.Value();
                    else if (attribute.Name == "developer")
                        disc.gameFilter.developer = attribute.Value();
                    else if (attribute.Name == "disc")
                        disc.gameFilter.disc = attribute.Value().AsInt32(disc.gameFilter.disc);
                    else if (attribute.Name == "version")
                        disc.gameFilter.version = attribute.Value().AsInt32(disc.gameFilter.version);
                }

                XmlNodeList xml_regions = id.GetElementsByTagName("region");
                if (xml_regions != null && xml_regions.Count > 0)
                {
                    List<string> regions = new List<string>();
                    foreach (XmlNode region in xml_regions)
                    {
                        regions.Add(region.Attributes["type"].Value());
                    }
                    disc.gameFilter.regions = regions;
                }
            }

            XmlNode options = wiidisc["options"];
            if (options != null)
            {
                foreach (XmlNode sectionNode in options.GetElementsByTagName("section"))
                {
                    Section section = new Section();

                    section.name = sectionNode.Attributes["name"].Value();
                    foreach (XmlNode optionNode in sectionNode.GetElementsByTagName("option"))
                    {
                        Option option = new Option();

                        option.id = optionNode.Attributes["id"].Value();
                        option.name = optionNode.Attributes["name"].Value();
                        option.selectedChoice = optionNode.Attributes["default"].Value().AsUInt32(option.selectedChoice);
                        Dictionary<string, string> optionParams = ReadParams(optionNode);
                        foreach (XmlNode choiceNode in optionNode.GetElementsByTagName("choice"))
                        {
                            Choice choice = new Choice();

                            choice.name = choiceNode.Attributes["name"].Value();
                            Dictionary<string, string> choiceParams = ReadParams(choiceNode, optionParams);
                            foreach (XmlNode patchrefNode in choiceNode.GetElementsByTagName("patch"))
                            {
                                PatchReference patchref = new PatchReference();

                                patchref.id = patchrefNode.Attributes["id"].Value();

                                choice.patchReferences.Add(patchref);
                            }

                            option.choices.Add(choice);
                        }

                        section.options.Add(option);
                    }

                    disc.sections.Add(section);
                }

                // Huh, I don't quite get that
                /*foreach(XmlNode macroNode in options.GetElementsByTagName("macros"))
                {
                    string macro_id = macroNode.Attributes["id"].Value;
                    foreach(Section section in disc.sections)
                    {
                        
                    }
                }*/
            }

            XmlNodeList patches = wiidisc.GetElementsByTagName("patch");
            foreach (XmlNode patchNode in patches)
            {
                Patch patch = new Patch();

                patch.id = patchNode.Attributes["id"].Value();
                patch.root = patchNode.Attributes["root"].Value();
                if (patch.root == "")
                    patch.root = defaultRoot;

                patch.root = patch.root.Replace("/", "\\");

                //Console.WriteLine("Found " + patchNode.ChildNodes.Count + " nodes in " + patch.id);
                if (patchNode.ChildNodes.Count < 1)
                    continue;

                foreach (XmlNode patchSubnode in patchNode.ChildNodes)
                {
                    string patchName = patchSubnode.Name;
                    if (patchName == "file")
                    {
                        File file = new File();

                        file.disc = patchSubnode.Attributes["disc"].Value().Replace("/", "\\");
                        file.external = patchSubnode.Attributes["external"].Value().Replace("/", "\\");
                        file.resize = patchSubnode.Attributes["resize"].Value().AsBool(file.resize);
                        file.create = patchSubnode.Attributes["create"].Value().AsBool(file.create);
                        file.offset = patchSubnode.Attributes["offset"].Value().AsUInt32(file.offset);
                        file.fileOffset = patchSubnode.Attributes["fileoffset"].Value().AsUInt32(file.fileOffset);
                        file.length = patchSubnode.Attributes["length"].Value().AsUInt32(file.length);

                        patch.filePatches.Add(file);
                    }
                    else if (patchName == "folder")
                    {
                        Folder folder = new Folder();

                        folder.disc = patchSubnode.Attributes["disc"].Value().Replace("/", "\\");
                        if (folder.disc == "\\") folder.disc = "root";
                        if (folder.disc.EndsWith("\\")) folder.disc = folder.disc.Substring(0, folder.disc.Length - 1);
                        folder.external = patchSubnode.Attributes["external"].Value().Replace("/", "\\");
                        if (folder.external.EndsWith("\\")) folder.external = folder.external.Substring(0, folder.external.Length - 1);
                        folder.resize = patchSubnode.Attributes["resize"].Value().AsBool(folder.resize);
                        folder.create = patchSubnode.Attributes["create"].Value().AsBool(folder.create);
                        folder.recursive = patchSubnode.Attributes["recursive"].Value().AsBool(folder.recursive);
                        folder.length = patchSubnode.Attributes["length"].Value().AsUInt32(folder.length);

                        patch.folderPatches.Add(folder);
                    }
                    else if (patchName == "savegame")
                    {
                        Savegame savegame = new Savegame();

                        savegame.external = patchSubnode.Attributes["external"].Value().Replace("/", "\\");
                        savegame.clone = patchSubnode.Attributes["clone"].Value().AsBool(savegame.clone);

                        disc.hasSaveGamePatches = true;

                        patch.savegamesPatches.Add(savegame);
                    }
                    else if (patchName == "memory")
                    {
                        Memory memory = new Memory();

                        memory.offset = patchSubnode.Attributes["offset"].Value().AsUInt32(memory.offset);
                        memory.value = patchSubnode.Attributes["value"].Value().ReadHexString();
                        memory.valueFile = patchSubnode.Attributes["valuefile"].Value();
                        memory.original = patchSubnode.Attributes["original"].Value().ReadHexString();
                        memory.ocarina = patchSubnode.Attributes["ocarina"].Value().AsBool(memory.ocarina);
                        memory.search = patchSubnode.Attributes["search"].Value().AsBool(memory.search);
                        memory.align = patchSubnode.Attributes["align"].Value().AsUInt32(memory.align);

                        if(memory.offset < 0x80000000)
                        {
                            memory.offset += 0x80000000;
                        }

                        if(memory.offset < 0x80004000)
                        {
                            disc.badMemPatches = true;
                        }

                        patch.memoryPatches.Add(memory);
                    }
                }

                disc.patches.Add(patch);
            }

            long diff = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time;
            float timeSpent = diff / 1000f;
            Console.WriteLine("Done parsing XML file, took " + timeSpent + "s");

            return disc;
        }

        public static Dictionary<string, string> ReadParams(XmlNode node)
        {
            return ReadParams(node, new Dictionary<string, string>());
        }

        public static Dictionary<string, string> ReadParams(XmlNode node, Dictionary<string, string> param)
        {
            foreach (XmlNode paramNode in node.GetElementsByTagName("param"))
            {
                string paramName = paramNode.Attributes["name"].Value();
                string paramValue = paramNode.Attributes["value"].Value();
                param[paramName] = paramValue;
            }
            return param;
        }
    }

    public class ConfigOptions
    {
        public string id;
        public uint default_;
        
        public ConfigOptions()
        {
            id = "";
            default_ = 0;
        }
    }

    public class Config
    {
        public int version;
        public List<ConfigOptions> options;

        public Config()
        {
            version = 2;
            options = new List<ConfigOptions>();
        }
    }
}
