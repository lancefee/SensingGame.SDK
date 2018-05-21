﻿using Sensing.SDK.Contract;
using SensingStoreCloud.Devices.Dto.SensingDevice;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Sensing.SDK
{
    partial class SensingWebClient
    {
        /// <summary>
        /// Get all the things.
        /// </summary>
        private const string AdsBaseUrl = "SensingDevice";
        private const string GetAdsQuery = AdsBaseUrl + "/GetAds";

        public async Task<PagedResultDto<AdsSdkModel>> GetAds(int skipCount = 0,int maxCount=300)
        {
            var absolutePath = $"{ServiceHost}/{GetAdsQuery}?{GetBasicNameValuesQueryString()}&{MaxResultCount}={maxCount}&{SkipCount}={skipCount}";
            try
            {
                var webResult = await SendRequestAsync<string, AjaxResponse<PagedResultDto<AdsSdkModel>>>(HttpMethod.Get, absolutePath,null);
                if(webResult.Success)
                {
                    return webResult.Result;
                }
                Console.WriteLine("GetAds:" + webResult.Error.Message);
                return null;
            }
            catch (Exception ex)
            {
                //logger.Error("PostBehaviorRecordsAsync", ex);
                Console.WriteLine("GetAds:" + ex.InnerException);
            }
            return null;
        }
    }
}
