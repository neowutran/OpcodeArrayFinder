using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OpcodeFinder
{
    public class OpcodeScanner
    {
        static void Main(string[] args)
        {
            new OpcodeScanner();
        }

        public OpcodeScanner()
        {
            Find();
        }

        private static readonly List<String> KNOWN_OPCODE = new List<String>(){
            "4DBD",
            "4DBC",
            "6AB6",
            "8894",
            "5BC1",
            "D3EB",
            "F445",
            "D92A",
            "D0F6",
            "CE52",
            "A4DA",
            "DA52",
            "BAA8",
            "E2B9",
            "7B81",
            "E52F",
            "A3CD",
            "E12E",
            "9C50",
            "7991",
            "F61A",
            "E3F1"
        }.Distinct().ToList();
      

        public void Find()
        {
            var teraProcess = Process.GetProcessesByName("tera").SingleOrDefault();
            if (teraProcess == null)
            {
                return;
            }
            using (var memoryScanner = new MemoryScanner(teraProcess))
            {
                foreach (var region in memoryScanner.MemoryRegions().Where(
                    x => x.Protect.HasFlag(MemoryScanner.AllocationProtectEnum.PAGE_READWRITE) &&
                    x.State.HasFlag(MemoryScanner.StateEnum.MEM_COMMIT) &&
                    x.Type.HasFlag(MemoryScanner.TypeEnum.MEM_PRIVATE)))
                {
                    try
                    {
                        var patternData = BitConverter.ToString(memoryScanner.ReadMemory(region.BaseAddress, (int)region.RegionSize));
                        patternData = patternData.Replace("-", "");
                        ShittyPathFinder(patternData, region.BaseAddress);
                    }
                    catch { }
                }
            }
        }

        private static readonly int ShittyArbitraryMaxDistance = 90000;
        //TODO improve this shitty quick and dirty
        public void ShittyPathFinder(string data, uint baseAddress)
        {
            biggestDistance = 0;
            var allOpcodeAllIndexes = FindAllIndexesForAllOpcode(data);
            var firstOpcode = allOpcodeAllIndexes.Keys.ElementAt(0);
            allOpcodeAllIndexes.TryGetValue(firstOpcode, out var listIndexes);
            
            foreach (var index in listIndexes)
            {
                bool closeEnought = true;
                foreach(var listOffset in allOpcodeAllIndexes.Values)
                {
                    if(!AnyIsCloseEnought(index, listOffset))
                    {
                        closeEnought = false;
                        break;
                    }
                }
                if (closeEnought)
                {
                    Debug.WriteLine("Possible offset for "+ firstOpcode + ":" + (index/2).ToString("X") + " ; base offset:" + baseAddress.ToString("X") + " ; total:" + (baseAddress + (index/2)).ToString("X") + " ; biggest distance:"+biggestDistance);
                }
            }
        }

        private int biggestDistance;

        public bool AnyIsCloseEnought(int initialOffset, List<int> offsetsCurrentOpcode)
        {
            foreach(var offset in offsetsCurrentOpcode)
            {
                if(IsCloseEnought(offset, initialOffset))
                {
                    return true;
                }
            }
            return false;

        }

        public bool IsCloseEnought(int firstOffset, int secondOffset)
        {
            var dist = Math.Abs(secondOffset - firstOffset)/2;
            if (dist > ShittyArbitraryMaxDistance)
            {
                return false;
            }
            if (biggestDistance < dist)
            {
                biggestDistance = dist;
            }
            return true;
        }

        public Dictionary<String, List<int>> FindAllIndexesForAllOpcode(string data)
        {
            var result = new Dictionary<String, List<int>>();
            foreach(var opcode in KNOWN_OPCODE)
            {
                result.Add(opcode, FindAllMatchIndexes(data, opcode, 0));
            }

            return result;

        }

        public List<int> FindAllMatchIndexes(string data, string match, int currentIndex)
        {
            var matchIndexes = new List<int>();
            var newIndex = data.IndexOf(match, currentIndex);
            if(newIndex == -1){ return matchIndexes; }
            matchIndexes.Add(newIndex);
            matchIndexes.Concat(FindAllMatchIndexes(data, match, newIndex+1));
            return matchIndexes;
        }

 
    }
}
