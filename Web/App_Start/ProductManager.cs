﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Web.Models;

namespace Web
{
    public class ProductManager : IDisposable
    {
        public static ProductManager Create(IdentityFactoryOptions<ProductManager> options, IOwinContext context)
        {
            return new ProductManager(context.Get<ApplicationDbContext>());
        }

        private static ApplicationDbContext _db;

        public ProductManager(ApplicationDbContext applicationDbContext)
        {
            _db = applicationDbContext;
        }
        public void Dispose()
        {
            _db.Dispose();
        }

        public async Task<List<Product>> GetAllProducsAsync()
        {
            await _db.WeightCategories.ToListAsync();
            var res = await _db.Products.OrderBy(o => o.Name).ToListAsync();
            return res;
        }

        public async Task<List<ProductInOrderViewModel>> GetProducsAsync()
        {
            await _db.WeightCategories.ToListAsync();
            return (from el in await _db.Products.OrderBy(o => o.Name).ToListAsync() select new ProductInOrderViewModel(el)).ToList();
        }

        public async Task<List<ProductInOrderViewModel>> GetSalesProducsAsync()
        {
            await _db.Products.ToListAsync(); await _db.WeightCategories.ToListAsync();
            var list = await _db.OrdersDetails.Join(_db.Receipts, d => d.IdOrder, r => r.OrderId,
                        (d, r) => new { Product = d.Product, Count = d.Count, Price = d.Price }).GroupBy(arg => arg.Product.Id).Select(g =>
              new ProductInOrderViewModel
              {
                  Id = g.Key,
                  Price = g.Average(p => p.Price),
                  Count = g.Sum(p => p.Count)
              }).ToListAsync();
            foreach (var el in list)
            {
                var product = await FindAsync(el.Id);
                if (product == null) continue;
                el.Art = product.Art;
                el.MaxCount = product.Count;
                el.Name = product.Name;
            }
            return list.OrderBy(o => o.Name).ToList();
        }
        public async Task<List<ProductInOrderViewModel>> GetSalesProducsSummPriceAsync()
        {
            await _db.Products.ToListAsync(); await _db.WeightCategories.ToListAsync();
            var list = await _db.OrdersDetails.Join(_db.Receipts, d => d.IdOrder, r => r.OrderId,
                        (d, r) => new { Product = d.Product, Count = d.Count, Price = d.Price }).GroupBy(arg => arg.Product.Id).Select(g =>
              new ProductInOrderViewModel
              {
                  Id = g.Key,
                  Price = g.Average(p => p.Price),
                  Count = g.Sum(p => p.Count)
              }).ToListAsync();
            foreach (var el in list)
            {
                var product = await FindAsync(el.Id);
                if (product == null) continue;
                el.Name = product.Name;
                el.Price = el.Count * el.Price;
            }
            return list.OrderBy(o => o.Name).ToList();
        }

        public async Task<List<ChartDataViewModel>> GetSalesProducsShippingTypeAsync()
        {
            await _db.ShippingTypes.ToListAsync(); await _db.Orders.ToListAsync();
            var list = await _db.OrdersDetails.Join(_db.Receipts, d => d.IdOrder, r => r.OrderId,
                        (d, r) => new { Product = d.Product, Count = d.Count, Price = d.Price, Order = d.Order })
                        .GroupBy(arg => arg.Order.ShippingType).Select(g =>
              new ChartDataViewModel
              {
                  Id = g.Key.Id,
                  Name = g.Key.Type,
                  Count = g.Sum(p => p.Count)
              }).ToListAsync();

            return list.OrderBy(o => o.Id).ToList();
        }

        public async Task<List<ChartDataViewModel>> GetSalesProducsPaymentTypeAsync()
        {
            await _db.PaymentTypes.ToListAsync(); await _db.Orders.ToListAsync();
            var list = await _db.OrdersDetails.Join(_db.Receipts, d => d.IdOrder, r => r.OrderId,
                        (d, r) => new { Product = d.Product, Count = d.Count, Price = d.Price, Order = d.Order })
                        .GroupBy(arg => arg.Order.PaymentType).Select(g =>
              new ChartDataViewModel
              {
                  Id = g.Key.Id,
                  Name = g.Key.Type,
                  Count = g.Sum(p => p.Count)
              }).ToListAsync();

            return list.OrderBy(o => o.Id).ToList();
        }

        public async Task<List<ProductInOrderViewModel>> GetProducsInOrderAsync(string orderId)
        {
            var result = await _db.OrdersDetails.Where(o => o.IdOrder.Equals(orderId)).ToListAsync();
            if (result.Any(o => o.Product == null)) await _db.Products.ToListAsync();
            await _db.WeightCategories.ToListAsync();
            return result.Select(el => new ProductInOrderViewModel(el.Product)
            { Count = el.Count, Price = el.Price }).OrderBy(o => o.Name).ToList();
        }

        public async Task<Product> FindAsync(string id)
        {
            await _db.WeightCategories.ToListAsync();
            return await _db.Products.FindAsync(id);
        }

        public async Task AddOrUpdate(Product product)
        {
            await _db.WeightCategories.ToListAsync();
            if (string.IsNullOrEmpty(product.Id))
            {
                product.Id = Guid.NewGuid().ToString();
                _db.Entry(product).State = EntityState.Added;
            }
            else
                _db.Entry(product).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task Delete(string id)
        {
            var product = await FindAsync(id);
            _db.Entry(product).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
        }

        public async Task<List<WeightCategory>> GetWeightCategoriesAsync()
        {
            return await _db.WeightCategories.ToListAsync();
        }
        public async Task<WeightCategory> GetWeightCategoriesByIdAsync(int id)
        {
            return await _db.WeightCategories.FindAsync(id);
        }

        public async Task<List<ChartDataViewModel>> GetTransactRouteAsync()
        {
            await _db.TransactRoutes.ToListAsync();
            await _db.Receipts.ToListAsync();
            var routeRec = await _db.TransactRouteReceipts.ToListAsync();
            var list = routeRec.GroupBy(g => g.Route).Select(o => new ChartDataViewModel
            {
                Count = o.Key.Id,
                Value = o.Key.TransactValue,
                Value2 = o.Sum(p => (decimal)p.Receipt.ShippingCost)
            }).ToList();
            var val1 = 0m; var val2 = 0m;
            foreach (var el in list)
            {
                val1 += el.Value;
                el.Value = val1;
                val2 += el.Value2;
                el.Value2 = val2;
            }
            return list;
        }

    }

    public class ChartDataViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public decimal Value { get; set; }
        public decimal Value2 { get; set; }
    }
}