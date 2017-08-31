﻿using System.Collections.Generic;

namespace EthereumSamurai.Models.Query
{
    public class Erc20BalanceQuery
    {
        public string AssetHolder { get; set; }

        public ulong? BlockNumber { get; set; }

        public IEnumerable<string> Contracts { get; set; }

        public int? Count { get; set; }

        public int? Start { get; set; }
    }
}