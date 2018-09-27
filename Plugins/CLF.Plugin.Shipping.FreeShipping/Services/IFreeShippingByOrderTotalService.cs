using System.Collections.Generic;
using Nop.Core;
using CLF.Plugin.Shipping.FreeShipping.Domain;

namespace CLF.Plugin.Shipping.FreeShipping.Services
{
    public partial interface IFreeShippingByOrderTotalService
    {
        void DeleteFreeShippingRecord(FreeShippingByOrderTotalsRecord freeShippingByOrderTotalsRecord);

        IPagedList<FreeShippingByOrderTotalsRecord> GetAll(int pageIndex = 0, int pageSize = int.MaxValue);

        FreeShippingByOrderTotalsRecord FindRecord(int shippingMethodId, int countryId, int stateProvinceId, string zip);

        FreeShippingByOrderTotalsRecord GetById(int freeShippingRecordId);

        void InsertFreeShippingRecord(FreeShippingByOrderTotalsRecord freeShippingByOrderTotalsRecord);

        void UpdateFreeShippingRecord(FreeShippingByOrderTotalsRecord freeShippingByOrderTotalsRecord);
    }
}
