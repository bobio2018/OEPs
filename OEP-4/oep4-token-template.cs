using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Ontology
{
    public class Ontology : SmartContract
    {
        //Token Settings
        public static string Name() => "NameOfTheToken";
        public static string Symbol() => "SymbolOfTheToken";
        public static readonly byte[] Owner = "ATrzHaicmhRj15C3Vv6e6gLfLqhSD2PtTr".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        private const ulong totalAmount = 100000000 * factor;

        //Store Key Prefix
        private static byte[] transferPrefix = "transfer".AsByteArray();
        private static byte[] totalSupply = "totalSupply".AsByteArray();
        private static byte[] approvePrefix = "approve".AsByteArray();

        public delegate void deleTransfer(byte[] from, byte[] to, BigInteger value);
        [DisplayName("transfer")]
        public static event deleTransfer Transferred;

        public delegate void deleApprove(byte[] onwer, byte[] spender, BigInteger value);
        [DisplayName("approval")]
        public static event deleApprove Approval;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "init") return Init();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    if(from.Length != 20) {
                        return false;
                    }
                    byte[] to = (byte[])args[1];
                    if(to.Length != 20) {
                        return false;
                    }
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                 if (operation == "approve")
                {
                    if (args.Length != 3) return false;
                    byte[] owner = (byte[])args[0];
                    if(owner.Length != 20) {
                        return false;
                    }
                    byte[] spender = (byte[])args[1];
                    if(spender.Length != 20) {
                        return false;
                    }
                    BigInteger value = (BigInteger)args[2];
                    return Approve(owner, spender, value);
                }
                if (operation == "transferFrom")
                {
                    if (args.Length != 4) return false;
                    byte[] sender = (byte[])args[0];
                    if(sender.Length != 20) {
                        return false;
                    }
                    byte[] from = (byte[])args[1];
                    if(from.Length != 20) {
                        return false;
                    }
                    byte[] to = (byte[])args[1];
                    if(to.Length != 20) {
                        return false;
                    }
                    BigInteger amount = (BigInteger)args[2];
                    return TransferFrom(sender, from, to, amount);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] address = (byte[])args[0];
                    return BalanceOf(address);
                }
                 if (operation == "allowance")
                {
                    if (args.Length != 2) return 0;
                    byte[] owner = (byte[])args[0];
                    byte[] spender = (byte[])args[0];
                    return Allowance(owner, spender);
                }
                if (operation == "decimals") return Decimals();
            }
            return false;
        }

        /// <summary>initialize contract parameter</summary>
        /// <returns>initialize result, success or failure</returns>
        public static bool Init()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, totalSupply);
            if (total_supply.Length != 0) return false;

            Storage.Put(Storage.CurrentContext, transferPrefix.Concat(Owner), totalAmount);
            Transferred(null, transferPrefix.Concat(Owner), totalAmount);

            Storage.Put(Storage.CurrentContext, totalSupply, totalAmount);
            return true;
        }

        /// <summary>
        ///     transfer amount of token from sender to receiver,
        ///     the address can be account or contract address which should be 20-byte
        /// </summary>
        /// <returns>transfer result, success or failure</returns>
        /// <param name="from">transfer sender address</param>
        /// <param name="to">transfer receiver address</param>
        /// <param name="value">transfer amount</param>
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value < 0) return false;
            if (!Runtime.CheckWitness(from)) return false;

            byte[] fromKey = transferPrefix.Concat(from);
            BigInteger fromValue = Storage.Get(Storage.CurrentContext, fromKey).AsBigInteger();
            if (fromValue < value) return false;
            if (fromValue == value)
                Storage.Delete(Storage.CurrentContext, fromKey);
            else
                Storage.Put(Storage.CurrentContext, fromKey, fromValue - value);

            byte[] toKey = transferPrefix.Concat(to);
            BigInteger toValue = Storage.Get(Storage.CurrentContext, toKey).AsBigInteger();
            Storage.Put(Storage.CurrentContext, toKey, toValue + value);
            Transferred(from, to, value);
            return true;
        }

        /// <summary>query balance of any address</summary>
        /// <returns>balance of the address</returns>
        /// <param name="address">account or contract address</param>
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        /// <summary>query the total supply of token</summary>
        /// <returns>total supply of token </returns>
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, totalSupply).AsBigInteger();
        }

        /// <summary>
        ///     approve allows spender to withdraw from owner account multiple times, up to the value amount
        ///     the address can be account or contract address which should be 20-byte
        /// </summary>
        /// <returns>transfer result, success or failure</returns>
        /// <param name="from">approve owner address</param>
        /// <param name="to">approve spender address</param>
        /// <param name="value">approve amount</param>
        public static bool Approve(byte[] owner, byte[] spender, BigInteger amount)
        {
            if (amount < 0) return false;
            if (!Runtime.CheckWitness(owner)) return false;

            Storage.Put(Storage.CurrentContext, approvePrefix.Concat(owner).Concat(spender), amount);
            Approval(owner, spender, amount);
            return true;
        }

         /// <summary>
        ///     transferFrom allows `sender` to withdraw amount of token from `from` account to `to` account
        ///     the address can be account or contract address which should be 20-byte
        /// </summary>
        /// <returns>transferFrom result, success or failure</returns>
        /// <param name="sender">approve owner address</param>
        /// <param name="from">approve owner address</param>
        /// <param name="to">approve spender address</param>
        /// <param name="value">approve amount</param>
        public static bool TransferFrom(byte[] sender, byte[] from, byte[] to, BigInteger amount)
        {
            if (amount < 0) return false;
            if (!Runtime.CheckWitness(sender)) return false;

            BigInteger approveValue = Storage.Get(Storage.CurrentContext, approvePrefix.Concat(from).Concat(sender)).AsBigInteger();
            if(approveValue < amount) return false;

            if (approveValue == amount)
                Storage.Delete(Storage.CurrentContext, approvePrefix.Concat(from).Concat(sender));
            else
                Storage.Put(Storage.CurrentContext, approvePrefix.Concat(from).Concat(sender), approveValue - amount);

            return Transfer(transferPrefix.Concat(from), transferPrefix.Concat(to), amount);
        }

        public static BigInteger Allowance(byte[] owner, byte[] spender)
        {
            return Storage.Get(Storage.CurrentContext, approvePrefix.Concat(owner).Concat(spender)).AsBigInteger();
        }
    }
}