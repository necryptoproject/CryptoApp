﻿using CryptoExchange.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoExchange.ResponseModels
{
    public class PaymentResponse
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public decimal FromAmount { get; set; }
        public CurrencyResponse FromCurrency { get; set; }
        public string? Title { get; set; }
        public decimal? ToAmount { get; set; }
        public NetworkResponse? ToNetwork { get; set; }
        public CurrencyResponse? ToCurrency { get; set; }
        public string? WalletAddress { get; set; }
        public PaymentStatus Status { get; set; }
    }
}
