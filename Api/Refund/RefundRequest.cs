using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.PayU.Api.Refund
{
    public class RefundRequest
    {
        public RefundRequest()
        {
            Description = "Refund";
        }

        public string Description { get; private set; }
        public string Amount { get; set; }
    }
}
