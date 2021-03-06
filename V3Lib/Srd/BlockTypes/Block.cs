﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V3Lib.Srd.BlockTypes
{
    public abstract class Block
    {
        public int Unknown0C; // 1 for $CFH blocks, 0 for everything else
        public List<Block> Children = new List<Block>();

        public abstract void WriteData(ref BinaryWriter writer);
    }
}
