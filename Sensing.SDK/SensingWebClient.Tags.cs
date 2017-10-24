﻿using Sensing.SDK.Contract;
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
        private const string TagBaseUrl = "SensingDevice";
        private const string GetTagsQuery = TagBaseUrl + "/Tags";

        public async Task<PagedList<TagSdkModel>> GetTags(int page = 1,int maxCount=300)
        {
            var absolutePath = $"{ServiceHost}/{GetTagsQuery}?{GetBasicNameValuesQueryString()}&pageSize={maxCount}&page={page}";
            try
            {
                var webResult = await SendRequestAsync<string,WebApiResult<PagedList<TagSdkModel>>>(HttpMethod.Get, absolutePath,null);
                if(webResult.status == ApiStatus.OK)
                {
                    return webResult.data;
                }
                Console.WriteLine("GetTags:" + webResult.message);
                return null;
            }
            catch (Exception ex)
            {
                //logger.Error("PostBehaviorRecordsAsync", ex);
                Console.WriteLine("GetTags:" + ex.InnerException);
            }
            return null;
        }
    }
}
