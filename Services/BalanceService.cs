﻿using CryptoExchange.Entities;
using CryptoExchange.Interfaces;
using CryptoExchange.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoExchange.Services
{
    public class BalanceService : IBalanceService
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<BalanceService> _logger;
        private readonly ICurrencyService _currencyService;

        public BalanceService(ApplicationContext context, ILogger<BalanceService> logger, ICurrencyService currencyService)
        {
            _context = context;
            _logger = logger;
            _currencyService = currencyService;
        }
        public async Task Convert(Guid userId, int fromId, int toId, decimal fromAmount, decimal commission)
        {

            try
            {
                decimal rate = await _currencyService.GetRate(fromId, fromAmount, toId);


                decimal toAmount = _currencyService.CalculateAmountWithComission(fromAmount, rate, commission);
                await Withdraw(userId, fromId, fromAmount, false);
                await TopUp(userId, toId, toAmount, false);
                _context.SaveChanges();
                _logger.LogInformation("Success convert from {0} to {1} for userId: {2}; From amount {3} to amount {4}", fromId, toId, userId, fromAmount, toAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while convert for userId: {0}", userId);
                throw ex;
            }

        }
        public async Task<List<UserBalanceResponse>> GetBalance(Guid userId, bool addZeroBalances)
        {
            var balances = await _context.Balances.AsNoTracking().Where(x => x.UserId == userId).Include(x => x.Currency).Select(x => new UserBalanceResponse
            {
                Amount = x.Value,
                Currency = x.Currency.Name,
                CurrencyType = x.Currency.Type.ToString(),
                CurrencyId = x.CurrencyId

            }).ToListAsync();
            if (addZeroBalances)
            {
                var zeroBalances = await _context.Currencies.AsNoTracking().Select(x => new UserBalanceResponse
                {
                    Currency = x.Name,
                    CurrencyType = x.Type.ToString(),
                    Amount = 0,
                    CurrencyId = x.Id
                }).ToListAsync();
                balances = balances.Union(zeroBalances).DistinctBy(x => x.Currency).ToList();
            }

            return balances;
        }
        public async Task<decimal> TopUp(Guid userId, int currencyId, decimal amount, bool isSave = true)
        {
            var balance = _context.Balances.FirstOrDefault(x => x.UserId == userId && x.CurrencyId == currencyId);

            if (balance == null)
            {
                var currency = _context.Currencies.FirstOrDefault(x => x.Id == currencyId);
                if (currency == null)
                {
                    throw new Exception($"Currency with id {currencyId} not exist");
                }
                _context.Balances.Add(new UserBalance { CurrencyId = currencyId, Value = amount, UserId = userId });
                _context.SaveChanges();
                return amount;
            }

            if (balance.Value + amount > 1_000_000_000)
            {
                throw new Exception($"Amount more than 1.000.000.000");
            }
            using (var transaction = _context.Database.BeginTransaction(isolationLevel: System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    balance.Value += amount;
                    await _context.BalanceTransactions.AddAsync(new BalanceTransaction
                    {
                        Amount = amount,
                        CurrencyId = currencyId,
                        OperationType = OperationType.TopUp,
                        UserId = userId,

                    });
                    if (isSave)
                    {
                        _context.SaveChanges();
                        transaction.Commit();

                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    _logger.LogError(ex, "Error while TopUp for userId: {0}", balance.UserId);
                    throw new Exception("Deposit error");


                }
            }
            return balance.Value;

        }
        public async Task<decimal> Withdraw(Guid userId, int currencyId, decimal amount, bool isSave = true)

        {
            var currency = _context.Currencies.FirstOrDefault(x => x.Id == currencyId);
            if (currency == null)
            {
                throw new Exception($"Currency with id {currencyId} not exist");
            }
            var balance = _context.Balances.FirstOrDefault(x => x.UserId == userId && x.CurrencyId == currencyId);
            if (balance == null)
            {
                throw new Exception("Insufficient balance");
            }

            if (balance.Value - amount < 0m)
            {
                throw new Exception("Insufficient balance");
            }
            using (var transaction = _context.Database.BeginTransaction(isolationLevel: System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    balance.Value -= amount;
                    await _context.BalanceTransactions.AddAsync(new BalanceTransaction
                    {
                        Amount = amount,
                        CurrencyId = currencyId,
                        OperationType = OperationType.Withdraw,
                        UserId = userId,

                    });
                    if (balance.Value == 0m)
                    {
                        _context.Balances.Remove(balance);
                    }
                    if (isSave)
                    {
                        _context.SaveChanges();
                        transaction.Commit();
                    }
                }


                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Error while withdraw for userId: {0}", balance.UserId);
                    throw new Exception("Withdraw error");

                }
            }

            return balance.Value;

        }


    }
}
