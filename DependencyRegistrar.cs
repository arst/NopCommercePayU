using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Payments.PayU.Integration;
using Nop.Plugin.Payments.PayU.Integration.Services;

namespace Nop.Plugin.Payments.PayU
{
    class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order => 1;

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<PayuRestClientFactory>().As<IPayuRestClientFactory>().InstancePerLifetimeScope();
            builder.RegisterType<PayuAuthorizationService>().As<IPayuAuthorizationService>().InstancePerLifetimeScope();
            builder.RegisterType<PayuPaymentService>().As<IPayuPaymentService>().InstancePerLifetimeScope();
        }
    }
}
