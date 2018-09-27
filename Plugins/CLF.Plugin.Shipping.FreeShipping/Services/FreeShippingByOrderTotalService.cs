using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using CLF.Plugin.Shipping.FreeShipping.Domain;

namespace CLF.Plugin.Shipping.FreeShipping.Services
{
    public partial class FreeShippingByOrderTotalService : IFreeShippingByOrderTotalService
    {
        #region Constants
        private const string FREESHIPPING_ALL_KEY = "CLF.freeshippingbyorder.all-{0}-{1}";
        private const string FREESHIPPING_PATTERN_KEY = "CLF.freeshippingbyorder.";
        #endregion

        #region Fields

        private readonly IRepository<FreeShippingByOrderTotalsRecord> _sbwRepository;
        private readonly ICacheManager _cacheManager;

        #endregion

        #region Ctor

        public FreeShippingByOrderTotalService(ICacheManager cacheManager,
            IRepository<FreeShippingByOrderTotalsRecord> sbwRepository)
        {
            this._cacheManager = cacheManager;
            this._sbwRepository = sbwRepository;
        }

        #endregion

        #region Methods


        public virtual void DeleteFreeShippingRecord(FreeShippingByOrderTotalsRecord freeShippingByOrderTotalsRecord)
        {
            if (freeShippingByOrderTotalsRecord == null)
                throw new ArgumentNullException("FreeShippingByOrderTotalsRecord");

            _sbwRepository.Delete(freeShippingByOrderTotalsRecord);

            _cacheManager.RemoveByPattern(FREESHIPPING_PATTERN_KEY);
        }

        public virtual IPagedList<FreeShippingByOrderTotalsRecord> GetAll(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            string key = string.Format(FREESHIPPING_ALL_KEY, pageIndex, pageSize);
            return _cacheManager.Get(key, () =>
            {
                var query = from sbw in _sbwRepository.Table
                            orderby sbw.CountryId, sbw.StateProvinceId, sbw.Zip
                            select sbw;

                var records = new PagedList<FreeShippingByOrderTotalsRecord>(query, pageIndex, pageSize);
                return records;
            });
        }

        public virtual FreeShippingByOrderTotalsRecord FindRecord(int shippingMethodId, int countryId, int stateProvinceId, string zip)
        {
            if (zip == null)
                zip = string.Empty;
            zip = zip.Trim();

            //filter by country
            var existingRates = GetAll()
                .Where(sbw => sbw.ShippingMethodId == shippingMethodId)
                .ToList();
            
            //filter by country
            var matchedByCountry = new List<FreeShippingByOrderTotalsRecord>();
            foreach (var sbw in existingRates)
                if (countryId == sbw.CountryId)
                    matchedByCountry.Add(sbw);
            if (matchedByCountry.Count == 0)
                foreach (var sbw in existingRates)
                    if (sbw.CountryId == 0)
                        matchedByCountry.Add(sbw);

            //filter by state/province
            var matchedByStateProvince = new List<FreeShippingByOrderTotalsRecord>();
            foreach (var sbw in matchedByCountry)
                if (stateProvinceId == sbw.StateProvinceId)
                    matchedByStateProvince.Add(sbw);
            if (matchedByStateProvince.Count == 0)
                foreach (var sbw in matchedByCountry)
                    if (sbw.StateProvinceId == 0)
                        matchedByStateProvince.Add(sbw);


            //filter by zip
            var matchedByZip = new List<FreeShippingByOrderTotalsRecord>();
            foreach (var sbw in matchedByStateProvince)
                if ((String.IsNullOrEmpty(zip) && String.IsNullOrEmpty(sbw.Zip)) ||
                    (zip.Equals(sbw.Zip, StringComparison.InvariantCultureIgnoreCase)))
                    matchedByZip.Add(sbw);

            if (matchedByZip.Count == 0)
                foreach (var taxRate in matchedByStateProvince)
                    if (String.IsNullOrWhiteSpace(taxRate.Zip))
                        matchedByZip.Add(taxRate);

            return matchedByZip.FirstOrDefault();
        }

        public virtual FreeShippingByOrderTotalsRecord GetById(int freeShippingRecordId)
        {
            if (freeShippingRecordId == 0)
                return null;

            var record = _sbwRepository.GetById(freeShippingRecordId);
            return record;
        }

        public virtual void InsertFreeShippingRecord(FreeShippingByOrderTotalsRecord freeShippingByOrderTotalsRecord)
        {
            if (freeShippingByOrderTotalsRecord == null)
                throw new ArgumentNullException("FreeShippingByOrderTotalsRecord");

            _sbwRepository.Insert(freeShippingByOrderTotalsRecord);

            _cacheManager.RemoveByPattern(FREESHIPPING_PATTERN_KEY);
        }

        public virtual void UpdateFreeShippingRecord(FreeShippingByOrderTotalsRecord freeShippingByOrderTotalsRecord)
        {
            if (freeShippingByOrderTotalsRecord == null)
                throw new ArgumentNullException("FreeShippingByOrderTotalsRecord");

            _sbwRepository.Update(freeShippingByOrderTotalsRecord);

            _cacheManager.RemoveByPattern(FREESHIPPING_PATTERN_KEY);
        }

        #endregion
    }
}
