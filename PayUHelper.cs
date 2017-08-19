using Nop.Web.Framework;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web;

namespace Nop.Plugin.Payments.Payu
{
    public class PayuHelper
    {
        public string getchecksum(string key, string txnid, string amount, string productinfo, string firstname, string email, string salt)
        {
            string checksumString = string.Concat(new string[]
            {
                key,
                "|",
                txnid,
                "|",
                amount,
                "|",
                productinfo,
                "|",
                firstname,
                "|",
                email,
                "|||||||||||",
                salt
            });
            return this.Generatehash512(checksumString);
        }

        public string verifychecksum(string MerchantId, string OrderId, string Amount, string productinfo, string firstname, string email, string status, string salt)
        {
            string hashStr = string.Concat(new string[]
            {
                salt,
                "|",
                status,
                "|||||||||||",
                email,
                "|",
                firstname,
                "|",
                productinfo,
                "|",
                Amount,
                "|",
                OrderId,
                "|",
                MerchantId
            });
            return this.Generatehash512(hashStr);
        }

        public string Generatehash512(string text)
        {
            byte[] message = Encoding.UTF8.GetBytes(text);
            UnicodeEncoding UE = new UnicodeEncoding();
            SHA512Managed hashString = new SHA512Managed();
            string hex = "";
            byte[] hashValue = hashString.ComputeHash(message);
            byte[] array = hashValue;
            for (int i = 0; i < array.Length; i++)
            {
                byte x = array[i];
                hex += string.Format("{0:x2}", x);
            }
            return hex;
        }

        public string getSig(RemotePost post, string secondKey)
        {
            var sortedParams = new SortedDictionary<string, string>(post.Params.AllKeys.ToDictionary(k => k, k => post.Params[k]));
            StringBuilder builder = new StringBuilder();
            foreach (var param in sortedParams)
            {
                String p = String.Format("{0}={1}",param.Key, HttpUtility.UrlEncode(param.Value, Encoding.UTF8));
                builder.Append(p);
                builder.Append("&");
            }

            var toHash = builder.ToString() + secondKey;

            return Generatehash512(toHash);
        }
    }
}
