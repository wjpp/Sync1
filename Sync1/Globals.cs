﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

namespace Sync1
{
    class Globals
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int CurrentRevision { get; set; }
    }
}
