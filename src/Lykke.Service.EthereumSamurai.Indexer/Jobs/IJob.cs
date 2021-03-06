﻿using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Job.EthereumSamurai.Jobs
{
    public interface IJob
    {
        string Id { get; }

        int Version { get; }

        Task RunAsync();

        Task RunAsync(CancellationToken cancellationToken);
    }
}