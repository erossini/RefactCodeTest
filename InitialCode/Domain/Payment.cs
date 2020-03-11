using System;

namespace Refact.CodeTest.Domain
{
    internal class Payment
    {
        public string AgencyId { get; set; }
        public decimal Balance { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}