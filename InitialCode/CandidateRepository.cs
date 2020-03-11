using Refact.CodeTest.Domain;
using System.Collections.Generic;

namespace Refact.CodeTest
{
    internal class CandidateRepository
    {
        public Candidate GetById(string supplierId)
        {
            var candidates = new Dictionary<string, Candidate>
            {
                { "Supplier 1", new Candidate { BankDetails = new BankDetails{ AccountName = "Account 1", AccountNumber = "00000001", SortCode = "00-00-01"}}},  
                { "Supplier 2", new Candidate { BankDetails = new BankDetails{ AccountName = "Account 2", AccountNumber = "00000001", SortCode = "00-00-02"}}},  
                { "Supplier 3", new Candidate { BankDetails = new BankDetails{ AccountName = "Account 3", AccountNumber = "00000001", SortCode = "00-00-03"}}},  
                { "Supplier 4", new Candidate { BankDetails = new BankDetails{ AccountName = "Account 4", AccountNumber = "00000001", SortCode = "00-00-04"}}},  
                { "Supplier 5", new Candidate { BankDetails = new BankDetails{ AccountName = "Account 5", AccountNumber = "00000001", SortCode = "00-00-05"}}},  
            };

            return candidates[supplierId];
        }
    }
}