﻿using EthereumSamurai.Models.Blockchain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EthereumSamurai.Core.Repositories
{
    public interface ITransactionRepository
    {
        Task SaveAsync(TransactionModel TransactioModel);
    }
}
