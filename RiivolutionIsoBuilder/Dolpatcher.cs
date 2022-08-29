using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiivolutionIsoBuilder
{
    class Dolpatcher
    {
        List<uint> dolOffsets = new List<uint>();
        List<uint> realPointers = new List<uint>();
        List<uint> sectionSizes = new List<uint>();
        byte[] dol = new byte[0];

		List<int> newSectionsIndexes = new List<int>();

		string dolPath = "";
		bool isSilent;


		public Dolpatcher(string dolPath, bool isSilent)
        {
            dol = System.IO.File.ReadAllBytes(dolPath);
            Console.WriteLine("Decoding DOL file header...");

            for (int i = 0; i < 0x48; i += 4)
            {
                dolOffsets.Add(BitConverter.ToUInt32(dol.Skip(i).Take(4).Reverse().ToArray(), 0));
                realPointers.Add(BitConverter.ToUInt32(dol.Skip(i + 0x48).Take(4).Reverse().ToArray(), 0));
                sectionSizes.Add(BitConverter.ToUInt32(dol.Skip(i + 0x90).Take(4).Reverse().ToArray(), 0));
            }

			this.dolPath = dolPath;
			this.isSilent = isSilent;
		}

		public void saveDol()
		{
			reencodeDolHeader();
			System.IO.File.WriteAllBytes(dolPath, dol);
		}

        public bool doMemoryPatch(uint offset, byte[] value, byte[] original)
        {
			if(offset < 0x80004000)
			{
				Console.WriteLine("WARNING: This patch is being applied to 0x" + offset.ToString("X8") + ", which can break compatibility with USB Loaders.");
			}

			uint dolOffs = getOffsetFromPointer(offset);
			if (dolOffs == 0) //Out of dol range
			{
				int newSectionIndex = getNextEmptySection();

				if (newSectionIndex < 0)
				{
					Console.WriteLine("Can't create a new section for pointer 0x" + offset.ToString("X8") + ": No free section available.");
					return false;
				}

				int highestSection = getHighestSection();

				byte[] previousSections = dol.Take((int)(dolOffsets[highestSection] + sectionSizes[highestSection])).ToArray();
				byte[] footer = dol.Skip((int)(dolOffsets[highestSection] + sectionSizes[highestSection])).ToArray();
				dol = previousSections.Concat(value).Concat(footer).ToArray();
				dolOffsets[newSectionIndex] = dolOffsets[highestSection] + sectionSizes[highestSection];
				realPointers[newSectionIndex] = offset;
				sectionSizes[newSectionIndex] = (uint)value.Length;

				for (int i = 0; i < 4; i++)
				{
					dol[4 * newSectionIndex + i] = Convert.ToByte(dolOffsets[newSectionIndex].ToString("X8").Substring(i * 2, 2), 16);
					dol[4 * newSectionIndex + i + 0x48] = Convert.ToByte(realPointers[newSectionIndex].ToString("X8").Substring(i * 2, 2), 16);
					dol[4 * newSectionIndex + i + 0x90] = Convert.ToByte(sectionSizes[newSectionIndex].ToString("X8").Substring(i * 2, 2), 16);
				}

				newSectionsIndexes.Add(newSectionIndex);

				dolOffs = getOffsetFromPointer(offset);

				if (!isSilent) Console.WriteLine("Created new section at 0x" + dolOffs.ToString("X") + ", pointing at " + offset.ToString("X8") + " (section index: " + newSectionIndex + ").");

				if (mergeSectionsThatCanBeMerged())
				{
					if (!isSilent) Console.WriteLine("Merged sections that could be merged.");
				}

				return true;
			}
			else
			{
				bool isOriginal = original.Length > 0;

				List<byte> seq = new List<byte>();
				for (int i = (int)dolOffs; i < dolOffs + original.Length; i++)
				{
					seq.Add(dol[i]);
				}
				if ((isOriginal && seq.ToArray().SequenceEqual(original)) || !isOriginal)
				{
					try
					{
						int j = 0;
						for (int i = (int)dolOffs; i < dolOffs + value.Length; i++)
						{
							dol[i] = value[j];
							j++;
						}
						if (!isSilent) Console.WriteLine("Patched " + value.AsString() + " at " + offset.ToString("X8") + ((isOriginal) ? (" over " + seq.AsString()) : ""));

						return true;
					}
					catch(Exception e)
					{
						Console.WriteLine("Patch " + offset.ToString("X8") + " starts within the DOL range but ends out of it");
						return false;
					}
				}
				else
				{
					if (!isSilent) Console.WriteLine("Patch " + offset.ToString("X8") + " doesn't answer to original " + original.AsString() + " (has " + seq.AsString() + ") -> Skipping it.");
					return false;
				}
			}
		}

		public uint getOffsetFromPointer(uint pointer)
		{
			for (int i = 0; i < dolOffsets.Count; i++)
			{
				if (pointer >= realPointers[i] && pointer < (realPointers[i] + sectionSizes[i]))
				{
					return dolOffsets[i] + (pointer - realPointers[i]);
				}
			}

			return 0;
		}

		public int getHighestSection()
		{
			int sectionIndex = 0;
			for (int i = 0; i < dolOffsets.Count; i++)
			{
				if (dolOffsets[i] > dolOffsets[sectionIndex])
				{
					sectionIndex = i;
				}
			}
			return sectionIndex;
		}

		public int getNextEmptySection()
		{
			for (int i = 0; i < dolOffsets.Count; i++)
			{
				if (dolOffsets[i] == 0)
				{
					return i;
				}
			}
			return -1;
		}

		public bool mergeSectionsThatCanBeMerged()
		{
			bool didMerged = false;
			foreach (int newSection in newSectionsIndexes)
			{
				foreach (int otherSection in newSectionsIndexes)
				{
					if (otherSection == newSection)
					{
						continue;
					}
					if (realPointers[newSection] + sectionSizes[newSection] == realPointers[otherSection])
					{
						sectionSizes[newSection] += sectionSizes[otherSection];
						dolOffsets[otherSection] = 0;
						realPointers[otherSection] = 0;
						sectionSizes[otherSection] = 0;
						didMerged = true;
					}
				}
			}
			reencodeDolHeader();
			return didMerged;
		}

		public void reencodeDolHeader()
		{
			for (int i = 0; i < dolOffsets.Count; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					dol[4 * i + j] = Convert.ToByte(dolOffsets[i].ToString("X8").Substring(j * 2, 2), 16);
					dol[4 * i + j + 0x48] = Convert.ToByte(realPointers[i].ToString("X8").Substring(j * 2, 2), 16);
					dol[4 * i + j + 0x90] = Convert.ToByte(sectionSizes[i].ToString("X8").Substring(j * 2, 2), 16);
				}
			}
		}
	}
}
