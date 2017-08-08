﻿using EthereumSamurai.Core.Services;
using EthereumSamurai.Core.Services.Erc20;
using EthereumSamurai.Core.Settings;
using EthereumSamurai.Models;
using EthereumSamurai.Models.Blockchain;
using EthereumSamurai.Models.DebugModels;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EthereumSamurai.Services
{
    public class RpcBlockReader : IRpcBlockReader
    {
        private readonly IBaseSettings _bitcoinIndexerSettings;
        private readonly IWeb3 _client;
        private readonly IDebug _debug;
        private readonly IErc20Detector _erc20Detector;

        public RpcBlockReader(IBaseSettings bitcoinIndexerSettings, IWeb3 web3, IDebug debug, IErc20Detector erc20Detector)
        {
            _bitcoinIndexerSettings = bitcoinIndexerSettings;
            _client = web3;
            _debug = debug;
            _erc20Detector = erc20Detector;
        }

        public async Task<BlockContent> ReadBlockAsync(BigInteger blockHeight)
        {
            BlockWithTransactions block = await _client.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(blockHeight));

            #region Block

            string blockHash = block.BlockHash;
            BlockModel blockModel = new BlockModel()
            {
                TransactionsCount = block.Transactions.Length,
                BlockHash = blockHash,
                Difficulty = block.Difficulty,
                ExtraData = block.ExtraData,
                GasLimit = block.GasLimit,
                GasUsed = block.GasUsed,
                LogsBloom = block.LogsBloom,
                Miner = block.Miner,
                Nonce = block.Nonce,
                Number = block.Number,
                ParentHash = block.ParentHash,
                ReceiptsRoot = block.ReceiptsRoot,
                Sha3Uncles = block.Sha3Uncles,
                Size = block.Size,
                StateRoot = block.StateRoot,
                Timestamp = block.Timestamp,
                TotalDifficulty = block.TotalDifficulty,
                TransactionsRoot = block.TransactionsRoot,

            };

            #endregion

            #region Transactions

            List<InternalMessageModel> internalMessages = new List<InternalMessageModel>();
            List<TransactionModel> blockTransactions = new List<TransactionModel>(block.Transactions.Length);

            foreach (var transaction in block.Transactions)
            {
                TransactionReceipt transactionReciept = await _client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.TransactionHash);

                TraceResultModel traceResult = null;
                try
                {
                    traceResult = await _debug.TraceTransactionAsync(transaction.From,
                    transaction.To,
                    transactionReciept.ContractAddress,
                    transaction.Value.Value,
                    transaction.TransactionHash,
                    withMemory: false,
                    withStack: true,
                    withStorage: false);

                    if (traceResult != null && !traceResult.HasError && traceResult.Transfers != null)
                    {
                        internalMessages.AddRange(traceResult.Transfers.Select(x => new InternalMessageModel()
                        {
                            BlockNumber = block.Number.Value,
                            Depth = x.Depth,
                            FromAddress = x.FromAddress,
                            MessageIndex = x.MessageIndex,
                            ToAddress = x.ToAddress,
                            TransactionHash = x.TransactionHash,
                            Value = x.Value,
                            Type = (InternalMessageModelType)x.Type,
                            BlockTimestamp = blockModel.Timestamp
                        }));
                    }
                }
                catch (Exception e)
                { }

                TransactionModel transactionModel = new TransactionModel()
                {
                    BlockTimestamp = block.Timestamp,
                    BlockHash = transaction.BlockHash,
                    BlockNumber = transaction.BlockNumber,
                    From = transaction.From,
                    Gas = transaction.Gas,
                    GasPrice = transaction.GasPrice,
                    Input = transaction.Input,
                    Nonce = transaction.Nonce,
                    To = transaction.To,
                    TransactionHash = transaction.TransactionHash,
                    TransactionIndex = transaction.TransactionIndex,
                    Value = transaction.Value,
                    GasUsed = transactionReciept.GasUsed.Value,
                    ContractAddress = transactionReciept.ContractAddress,
                    HasError = traceResult?.HasError ?? false
                };

                blockTransactions.Add(transactionModel);
            }

            IEnumerable<AddressHistoryModel> addressHistory = ExtractAddressHistory(internalMessages, blockTransactions);

            #endregion

            #region Contracts

            Dictionary<string, Tuple<string, string>> contractInfoMap = new Dictionary<string, Tuple<string, string>>();
            IEnumerable<string> deployedContracts = blockTransactions?.Where(x => x.ContractAddress != null).Select(x =>
            {
                string contractAddress = x.ContractAddress;
                contractInfoMap[contractAddress] = Tuple.Create<string, string>(x.TransactionHash, x.From);
                return contractAddress;
            })?.Concat(
                internalMessages?.Where(x => x.Type == InternalMessageModelType.CREATION).Select(x =>
                {
                    string contractAddress = x.ToAddress;
                    contractInfoMap[contractAddress] = Tuple.Create<string, string>(x.TransactionHash, x.FromAddress);
                    return contractAddress;
                }));
            List<Erc20ContractModel> erc20Contracts = new List<Erc20ContractModel>();
            foreach (var contractAddress in deployedContracts)
            {
                bool isCompatible = await _erc20Detector.IsContractErc20Compatible(contractAddress);
                if (isCompatible)
                {
                    var tuple = contractInfoMap[contractAddress];
                    erc20Contracts.Add(new Erc20ContractModel()
                    {
                        Address = contractAddress,
                        BlockHash = blockHash,
                        BlockNumber = block.Number.Value,
                        BlockTimestamp = block.Timestamp.Value,
                        DeployerAddress = tuple.Item2,
                        TokenName = "",
                        TransactionHash = tuple.Item1
                    });
                }
            }

            #endregion 

            return new BlockContent()
            {
                AddressHistory = addressHistory,
                InternalMessages = internalMessages,
                Transactions = blockTransactions,
                BlockModel = blockModel,
                CreatedErc20Contracts = erc20Contracts
            };
        }

        private static IEnumerable<AddressHistoryModel> ExtractAddressHistory(List<InternalMessageModel> internalMessages,
            List<TransactionModel> blockTransactions)
        {
            Dictionary<string, int> trHashIndexDictionary = new Dictionary<string, int>();
            IEnumerable<AddressHistoryModel> history = blockTransactions.Select(transaction =>
            {
                int index = (int)transaction.TransactionIndex;
                string trHash = transaction.TransactionHash;
                trHashIndexDictionary[trHash] = index;

                return new AddressHistoryModel()
                {
                    MessageIndex = -1,
                    TransactionIndex = (int)transaction.TransactionIndex,
                    BlockNumber = (ulong)transaction.BlockNumber,
                    BlockTimestamp = (uint)transaction.BlockTimestamp,
                    From = transaction.From,
                    HasError = transaction.HasError,
                    To = transaction.To,
                    TransactionHash = transaction.TransactionHash,
                    Value = transaction.Value,
                };
            });

            history = history.Concat(internalMessages.Select(message => new AddressHistoryModel()
            {
                MessageIndex = message.MessageIndex,
                TransactionIndex = trHashIndexDictionary[message.TransactionHash],
                TransactionHash = message.TransactionHash,
                To = message.ToAddress,
                HasError = false,
                From = message.FromAddress,
                BlockNumber = (ulong)message.BlockNumber,
                BlockTimestamp = (uint)message.BlockTimestamp,
                Value = message.Value,
            }));

            return history;
        }

        //just the tip
        public async Task<BigInteger> GetBlockCount()
        {
            HexBigInteger tip = await _client.Eth.Blocks.GetBlockNumber.SendRequestAsync();

            return tip.Value;
        }
    }
}
