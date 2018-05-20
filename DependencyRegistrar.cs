using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Payments.PayuRedirect.Integration;
using Nop.Plugin.Payments.PayuRedirect.Integration.Services;

namespace Nop.Plugin.Payments.PayuRedirect
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
