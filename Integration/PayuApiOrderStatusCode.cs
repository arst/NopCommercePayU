using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.PayU.Integration
{
    class PayuApiOrderStatusCode
    {
        public const string Completed = "COMPLETED";
        public const string Rejected = "REJECTED";
        public const string Canceled = "CANCELED";
    }
}
