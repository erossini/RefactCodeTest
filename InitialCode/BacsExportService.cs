using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Raven.Client.Documents;
using Refact.CodeTest.Domain;

namespace Refact.CodeTest
{
    public class BacsExportService
    {
        private const string NOT_AVAILABLE = "NOT AVAILABLE";

        private readonly IDocumentStore documentStore;

        public BacsExportService()
        {
            documentStore = new DocumentStore { Urls = new[] { "http://localhost" }, Database = "Export" };
            documentStore.Initialize();
        }

        public async Task ExportZip(BacsExportType bacsExportType)
        {
            if (bacsExportType == BacsExportType.None)
            {
                const string invalidExportTypeMessage = "No export type provided.";
                throw new Exception(invalidExportTypeMessage);
            }

            var startDate = DateTime.Now.AddMonths(-1);
            var endDate = DateTime.Now;

            try
            {
                List<BacsResult> payments;
                switch (bacsExportType)
                {
                    case BacsExportType.Agency:
                        if (Application.Settings["EnableAgencyPayments"] == "true")
                        {
                            payments = await GetAgencyPayments(startDate, endDate);
                            SavePayments(payments, bacsExportType);
                        }

                        break;
                    case BacsExportType.Supplier:
                        var supplierBacsExport = GetSupplierPayments(startDate, endDate);
                        SaveSupplierBacsExport(supplierBacsExport);
                        break;
                    default:
                        throw new Exception("Invalid BACS Export Type.");
                }

            }
            catch (InvalidOperationException inOpEx)
            {
                throw new Exception(inOpEx.Message);
            }
        }

        private async Task<List<BacsResult>> GetAgencyPayments(DateTime startDate, DateTime endDate)
        {
            var paymentRepository = new PaymentsRepository();
            var payments = paymentRepository.GetBetweenDates(startDate, endDate);

            if (!payments.Any())
            {
                throw new InvalidOperationException(string.Format("No agency payments found between dates {0:dd/MM/yyyy} to {1:dd/MM/yyyy}", startDate, endDate));
            }

            var agencies = await GetAgenciesForPayments(payments);

            return BuildAgencyPayments(payments, agencies);
        }

        private async Task<List<Agency>> GetAgenciesForPayments(IList<Payment> payments)
        {
            var agencyIds = payments.Select(x => x.AgencyId).Distinct().ToList();

            using (var session = documentStore.OpenAsyncSession())
            {
                return (await session.LoadAsync<Agency>(agencyIds)).Values.ToList();
            }
        }

        private List<BacsResult> BuildAgencyPayments(IEnumerable<Payment> payments, List<Agency> agencies)
        {
            return (from p in payments
                    let agency = agencies.FirstOrDefault(x => x.Id == p.AgencyId)
                    where agency != null && agency.BankDetails != null
                    let bank = agency.BankDetails
                    select new BacsResult
                    {
                        AccountName = bank.AccountName,
                        AccountNumber = bank.AccountNumber,
                        SortCode = bank.SortCode,
                        Amount = p.Balance,
                        Ref = string.Format("SONOVATE{0}", p.PaymentDate.ToString("ddMMyyyy"))
                    }).ToList();
        }

        private void SavePayments(IEnumerable<BacsResult> payments, BacsExportType type)
        {
            var filename = string.Format("{0}_BACSExport.csv", type);

            using (var writer = new StreamWriter(new FileStream(filename, FileMode.Create)))
            {
                using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.CurrentCulture))
                {
                    csv.WriteRecords(payments);
                }
            }
        }

        private SupplierBacsExport GetSupplierPayments(DateTime startDate, DateTime endDate)
        {
            var invoiceTransactions = new InvoiceTransactionRepository();
            var candidateInvoiceTransactions = invoiceTransactions.GetBetweenDates(startDate, endDate);

            if (!candidateInvoiceTransactions.Any())
            {
                throw new InvalidOperationException(string.Format("No supplier invoice transactions found between dates {0} to {1}", startDate, endDate));
            }

            var candidateBacsExport = CreateCandidateBacxExportFromSupplierPayments(candidateInvoiceTransactions);

            return candidateBacsExport;
        }
        private SupplierBacsExport CreateCandidateBacxExportFromSupplierPayments(IList<InvoiceTransaction> supplierPayments)
        {
            var candidateBacsExport = new SupplierBacsExport
            {
                SupplierPayment = new List<SupplierBacs>()
            };

            candidateBacsExport.SupplierPayment = BuildSupplierPayments(supplierPayments);

            return candidateBacsExport;
        }

        private List<SupplierBacs> BuildSupplierPayments(IEnumerable<InvoiceTransaction> invoiceTransactions)
        {
            var results = new List<SupplierBacs>();

            var transactionsByCandidateAndInvoiceId = invoiceTransactions.GroupBy(transaction => new
            {
                transaction.InvoiceId,
                transaction.SupplierId
            });

            foreach (var transactionGroup in transactionsByCandidateAndInvoiceId)
            {
                var candidateRepository = new CandidateRepository();
                var candidate = candidateRepository.GetById(transactionGroup.Key.SupplierId);

                if (candidate == null)
                {
                    throw new InvalidOperationException(string.Format("Could not load candidate with Id {0}",
                        transactionGroup.Key.SupplierId));
                }

                var result = new SupplierBacs();

                var bank = candidate.BankDetails;

                result.AccountName = bank.AccountName;
                result.AccountNumber = bank.AccountNumber;
                result.SortCode = bank.SortCode;
                result.PaymentAmount = transactionGroup.Sum(invoiceTransaction => invoiceTransaction.Gross);
                result.InvoiceReference = string.IsNullOrEmpty(transactionGroup.First().InvoiceRef)
                    ? NOT_AVAILABLE
                    : transactionGroup.First().InvoiceRef;
                result.PaymentReference = string.Format("SONOVATE{0}",
                    transactionGroup.First().InvoiceDate.GetValueOrDefault().ToString("ddMMyyyy"));

                results.Add(result);
            }

            return results;
        }

        private void SaveSupplierBacsExport(SupplierBacsExport supplierBacsExport)
        {
            var fileName = string.Format("{0}_BACSExport.csv", BacsExportType.Supplier);

            using (var writer = new StreamWriter(new FileStream(fileName, FileMode.Create)))
            {
                using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.CurrentCulture))
                {
                    csv.WriteRecords(supplierBacsExport.SupplierPayment);
                }
            }
        }
    }
}