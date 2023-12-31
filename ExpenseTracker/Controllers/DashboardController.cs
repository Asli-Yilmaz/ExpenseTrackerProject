﻿using ExpenseTracker.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using SQLitePCL;
//using Syncfusion.EJ2.Linq;
//using System.Transactions;
using ExpenseTracker.Models;
using System.Globalization;
using MessagePack.Formatters;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContex _context;
        //ctor
        
        public DashboardController(ApplicationDbContex context)
        {
            //context parameter will be pass from dependency injection
            _context = context;
        }
        public async Task<ActionResult> Index() { 
            //return last 7 days transactions
            DateTime StartDate=DateTime.Today.AddDays(-30);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();
            //total income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("c0");

            //total expense
            int TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("c0");

            //balance = total income - expense
            int Balance = TotalIncome - TotalExpense;
            //ViewBag.Balance = Balance.ToString("c0");
            //to get rid of the pharanteses
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture,"{0:c0}",Balance);

            //doughnut chart-expense by category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("c0"),
                })
                .OrderByDescending(l=>l.amount)
                .ToList();
            //spline chart- income vs expense
            //income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MM"),
                    income = k.Sum(l => l.Amount)
                }).ToList();
            //expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MM"),
                    expense = k.Sum(l => l.Amount)
                }).ToList();
            //combine insome and expense with date
            string[] days = Enumerable.Range(0, 30).Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            //don't understand anything in here, research about this.
            ViewBag.SplineChartData = from day in days
                                      join income in IncomeSummary on day equals income.day
                                      into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day=day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,

                                      };
            //recent transaction
            ViewBag.RecentTransactions= await _context.Transactions
                .Include(i=>i.Category)
                .OrderByDescending(j=>j.Date)
                .Take(5)
                .ToListAsync();
            return View();
        }
    }
    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
