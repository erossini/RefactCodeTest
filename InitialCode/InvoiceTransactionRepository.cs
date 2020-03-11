using Refact.CodeTest.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Refact.CodeTest
{
    internal class InvoiceTransactionRepository
    {
        public List<InvoiceTransaction> GetBetweenDates(DateTime startDate, DateTime endDate)
        {
            return new List<InvoiceTransaction>
            {
                new InvoiceTransaction { InvoiceDate = new DateTime(2019, 4, 26), InvoiceId = "0001", SupplierId = "Supplier 1", InvoiceRef = "Ref0001", Gross = 10000.00m},
                new InvoiceTransaction { InvoiceDate = new DateTime(2019, 4, 14), InvoiceId = "0002", SupplierId = "Supplier 2", InvoiceRef = "Ref0002", Gross = 7300.00m},
                new InvoiceTransaction { InvoiceDate = new DateTime(2019, 4, 17), InvoiceId = "0003", SupplierId = "Supplier 3", InvoiceRef = "Ref0003", Gross = 2000.60m},
                new InvoiceTransaction { InvoiceDate = new DateTime(2019, 4, 1), InvoiceId = "0004", SupplierId = "Supplier 4", InvoiceRef = "Ref0004", Gross = 9800.00m},
                new InvoiceTransaction { InvoiceDate = new DateTime(2019, 4, 5), InvoiceId = "0005", SupplierId = "Supplier 5", InvoiceRef = "Ref0005", Gross = 4000.60m},
            }.Where(transaction => transaction.InvoiceDate >= startDate && transaction.InvoiceDate <= endDate).ToList();
        }
    }
}