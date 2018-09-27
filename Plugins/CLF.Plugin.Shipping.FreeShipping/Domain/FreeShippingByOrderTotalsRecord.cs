using Nop.Core;

namespace CLF.Plugin.Shipping.FreeShipping.Domain
{
    /// <summary>
    /// Represents a shipping by weight record
    /// </summary>
    public partial class FreeShippingByOrderTotalsRecord : BaseEntity
    {
        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public virtual int CountryId { get; set; }

        /// <summary>
        /// Gets or sets the state/province identifier
        /// </summary>
        public virtual int StateProvinceId { get; set; }

        /// <summary>
        /// Gets or sets the zip
        /// </summary>
        public virtual string Zip { get; set; }

        /// <summary>
        /// Gets or sets the zip
        /// </summary>
        public virtual decimal OrderOver { get; set; }
        
        /// <summary>
        /// Gets or sets the shipping method identifier
        /// </summary>
        public virtual int ShippingMethodId { get; set; }

      
    }
}