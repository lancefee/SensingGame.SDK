﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensing.SDK.Contract
{
    public class TagSdkModel
    {
        public int Id { get; set; }

        public string Value { get; set; }

        public string Type { get; set; }

        public bool IsSpecial { get; set; }

        public string IconUrl { get; set; }
    }
}
