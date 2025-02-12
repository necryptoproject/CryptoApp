﻿using Bybit.Net.Clients;
using Bybit.Net.Interfaces.Clients;
using CryptoExchange.Entities;
using CryptoExchange.Interfaces;
using CryptoExchange.Models;
using CryptoExchange.ResponseModels;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;

namespace CryptoExchange.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class CurrencyController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private readonly BybitRestClient _bybitRestClient;
        public CurrencyController(ApplicationContext context)
        {
            _context = context;
            _bybitRestClient = new BybitRestClient();
        }
        //[HttpGet("rate")]
        //public async Task<ActionResult<ConvertResponse>> GetRate([FromQuery] ConvertRequest request)
        //{

        //    decimal rate = await _currencyService.GetRate(request.FromId, request.FromAmount, request.ToId);


        //    var response = new Con
        //    {
        //        FromId = request.FromId,
        //        ToId = request.ToId,
        //        Rate = rate,
        //        FromAmount = request.FromAmount,
        //        ToAmount = _currencyService.CalculateAmountWithComission(request.FromAmount, rate, request.Commission),
        //        Commission = request.Commission
        //    };
        //    return Ok(response);

        //}
        [HttpPost]
        public ActionResult AddCurrency([FromBody] AddCurrencyRequest request)
        {

            var currency = _context.Currencies.FirstOrDefault(x => x.Name == request.Name || x.Code == request.Code);
            if (currency != null)
            {
                return Conflict($"Currency already exist with id {currency.Id}");
            }
            var newCurrency = new Currency
            {
                Code = request.Code,
                Name = request.Name,
                Type = request.Type
            };
            var newCurrencyEntity = _context.Currencies.Add(newCurrency).Entity;
            _context.SaveChanges();
            return Ok(newCurrency.Id);

        }
        [HttpGet("all")]
        public async Task<ActionResult<List<CurrencyResponse>>> GetAll([FromQuery] CurrencyType[]? currencyTypes)
        {

            var currencies = _context.Currencies.OrderBy(x => x.Id).Select(x => new CurrencyResponse
            {
                Id = x.Id,
                ImageUrl = x.ImageUrl,
                Name = x.Name,
                Type = x.Type,
                Rate = 1.025m
            });
           
            if (currencyTypes != null && currencyTypes.Length > 0)
            {
                currencies = currencies.Where(x => currencyTypes.Contains(x.Type));
            }
            return Ok(currencies);
        }
        
    }
}
