// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Sila.Test.Base;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Crypto;
using Nethermind.Facade.Sil.RpcTransaction;
using Nethermind.Serialization.Rlp;

namespace Savm.T8n.JsonTypes;

public class InputData
{
    public Dictionary<Address, AccountState>? Alloc { get; set; }
    public EnvJson? Env { get; set; }
    public TransactionForRpc[]? Txs { get; set; }
    public TransactionMetaData[]? TransactionMetaDataList { get; set; }
    public string? TxRlp { get; set; }

    public Transaction[] GetTransactions(IRlpDecoder<Transaction> decoder, ulong chainId)
    {
        if (TxRlp is not null)
        {
            RlpReader ctx = new(Bytes.FromHexString(TxRlp));
            return decoder.DecodeArray(ref ctx);
        }

        List<Transaction> transactions = [];
        if (Txs is not null && TransactionMetaDataList is not null)
        {
            SilaEcdsa ecdsa = new(chainId);

            for (int i = 0; i < Txs.Length; i++)
            {
                Transaction transaction = (Transaction)Txs[i].ToTransaction();
                transaction.SenderAddress = null; // t8n does not accept SenderAddress from input, so need to reset senderAddress

                SignTransaction(transaction, TransactionMetaDataList[i], (LegacyTransactionForRpc)Txs[i]);

                transaction.ChainId ??= chainId;
                transaction.SenderAddress ??= ecdsa.RecoverAddress(transaction);
                transaction.Hash = transaction.CalculateHash();

                transactions.Add(transaction);
            }
        }

        return transactions.ToArray();
    }

    private static void SignTransaction(Transaction transaction, TransactionMetaData transactionMetaData, LegacyTransactionForRpc txLegacy)
    {
        if (transactionMetaData.SecretKey is not null)
        {
            PrivateKey privateKey = new(transactionMetaData.SecretKey);
            transaction.SenderAddress = privateKey.Address;

            SilaEcdsa ecdsa = new(transaction.ChainId ?? TestBlockchainIds.ChainId);

            ecdsa.Sign(privateKey, transaction, transactionMetaData.Protected ?? true);
        }
        else if (txLegacy.R.HasValue && txLegacy.S.HasValue && txLegacy.V.HasValue)
        {
            transaction.Signature = new Signature(txLegacy.R.Value, txLegacy.S.Value, txLegacy.V.Value.ToUInt64(null));
        }
    }
}
