using Autofac;
using Autofac.Core;
using Autofac.Integration.Mvc;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using CLF.Plugin.Shipping.FreeShipping.Data;
using CLF.Plugin.Shipping.FreeShipping.Domain;
using CLF.Plugin.Shipping.FreeShipping.Services;

namespace CLF.Plugin.Shipping.FreeShipping
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder)
        {         
            builder.RegisterType<FreeShippingByOrderTotalService>().As<IFreeShippingByOrderTotalService>().InstancePerHttpRequest();
            //data layer
            var dataSettingsManager = new DataSettingsManager();
            var dataProviderSettings = dataSettingsManager.LoadSettings();

            if (dataProviderSettings != null && dataProviderSettings.IsValid())
            {
                //register named context
                builder.Register<IDbContext>(c => new ShippingFreeOrdersOverObjectContext(dataProviderSettings.DataConnectionString))
                    .Named<IDbContext>("nop_object_context_freeshipping_zip")
                    .InstancePerHttpRequest();

                builder.Register<ShippingFreeOrdersOverObjectContext>(c => new ShippingFreeOrdersOverObjectContext(dataProviderSettings.DataConnectionString))
                    .InstancePerHttpRequest();
            }
            else
            {
                //register named context
                builder.Register<IDbContext>(c => new ShippingFreeOrdersOverObjectContext(c.Resolve<DataSettings>().DataConnectionString))
                    .Named<IDbContext>("nop_object_context_freeshipping_zip")
                    .InstancePerHttpRequest();

                builder.Register<ShippingFreeOrdersOverObjectContext>(c => new ShippingFreeOrdersOverObjectContext(c.Resolve<DataSettings>().DataConnectionString))
                    .InstancePerHttpRequest();
            }

            //override required repository with our custom context
            builder.RegisterType<EfRepository<FreeShippingByOrderTotalsRecord>>()
                .As<IRepository<FreeShippingByOrderTotalsRecord>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>("nop_object_context_freeshipping_zip"))
                .InstancePerHttpRequest();
        }

        public int Order
        {
            get { return 1; }
        }
    }
}
