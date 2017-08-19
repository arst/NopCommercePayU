using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.PayU.Api.Payment
{

    class PayUOrderResponse
    {
        public string OrderId { get; set; }
        public string redirectUri { get; set; }
    }
}
