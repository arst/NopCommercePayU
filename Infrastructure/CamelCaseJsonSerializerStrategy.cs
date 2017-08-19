using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.PayU.Infrastructure
{
    public class CamelCaseJsonSerializerStrategy : PocoJsonSerializerStrategy
    {
        protected override string MapClrMemberNameToJsonFieldName(string clrPropertyName)
        {
            return char.ToLower(clrPropertyName[0]) + clrPropertyName.Substring(1);
        }
    }
}
