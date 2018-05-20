using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.PayU.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.Payu.PosId")]
        public string PosId
        {
            get;
            set;
        }

        [NopResourceDisplayName("Plugins.Payments.Payu.OAuthClientId")]
        public string OAuthClientId
        {
            get;
            set;
        }

        [NopResourceDisplayName("Plugins.Payments.Payu.OAuthClientSecret")]
        public string OAuthClientSecret
        {
            get;
            set;
        }

        [NopResourceDisplayName("Plugins.Payments.Payu.BaseUrl")]
        public string BaseUrl
        {
            get;
            set;
        }

        [NopResourceDisplayName("Plugins.Payments.Payu.SecondKey")]
        public string SecondKey
        {
            get;
            set;
        }

        [NopResourceDisplayName("Plugins.Payments.Payu.AdditionalFee")]
        public int AdditionalFee
        {
            get;
            set;
        }

        [NopResourceDisplayName("Plugins.Payments.Payu.Currency")]
        public string Currency
        {
            get;
            set;
        }

        public TransactMode TransactModeId
        {
            get;
            set;
        }

        [NopResourceDisplayName("Plugins.Payments.Payu.TransactionMode")]
        public SelectList TransactModeValues
        {
            get;
            set;
        }
    }
}
