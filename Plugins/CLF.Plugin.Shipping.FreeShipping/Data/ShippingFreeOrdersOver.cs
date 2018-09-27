using System.Data.Entity.ModelConfiguration;
using CLF.Plugin.Shipping.FreeShipping.Domain;

namespace CLF.Plugin.Shipping.FreeShipping.Data
{
    public partial class ShippingFreeOrdersOverRecordMap : EntityTypeConfiguration<FreeShippingByOrderTotalsRecord>
    {
        public ShippingFreeOrdersOverRecordMap()
        {
            this.ToTable("CLF_FreeShippingOrdersOver");
            this.HasKey(x => x.Id);

            this.Property(x => x.Zip).HasMaxLength(400);
        }
    }
}