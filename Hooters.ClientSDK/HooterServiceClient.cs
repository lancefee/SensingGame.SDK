﻿using Hooters.ClientSDK.Contract;
using LogService;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Hooters.ClientSDK
{
    public class HooterServiceClient
    {
        public const string ApiOK = "OK";
        /// <summary>
        /// The service host.
        /// </summary>
        //private const string ServiceHost = "http://game.troncell.com/api/v0/CounterApi";

        private const string ServiceHost = "http://localhost:4469/api/v0/CounterApi";

        private const string CreateCountersByDeviceQuery = "/CreateCountersByDevice";
        private const string PostHeatmapByDeviceQuery = "/PostHeatmapByDevice";
        private const string PostCountersByDeviceQuery = "/PostCountersByDevice";
        private const string GetCountersByDeviceQuery = "/GetCountersByDevice";

        /// <summary>
        /// The json header
        /// </summary>
        private const string JsonHeader = "application/json";
        private string subscriptionKey = null;

        private static readonly IBizLogger logger = ServerLogFactory.GetLogger(typeof(HooterServiceClient));

        /// <summary>
        /// The default resolver.
        /// </summary>
        private static CamelCasePropertyNamesContractResolver s_defaultResolver = new CamelCasePropertyNamesContractResolver();

        private static JsonSerializerSettings s_settings = new JsonSerializerSettings()
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = s_defaultResolver
        };

        private static HttpClient s_httpClient = new HttpClient();


        public HooterServiceClient(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;
        }

        public async Task<DeviceResult> GetCountersByDevice(string mac)
        {
            var absolutePath = $"{ServiceHost}/{GetCountersByDeviceQuery}?SubscriptionKey={subscriptionKey}";
            var formNameValues = $"mac={mac}";
            try
            {
                var deviceResult = await SendRequestAsync<string, DeviceResult>(HttpMethod.Post, absolutePath, formNameValues, "form");
                return deviceResult;
            }
            catch (Exception ex)
            {
                logger.Error("CreateCountersByDevice", ex);
            }
            return default(DeviceResult);

        }

        public async Task<DeviceResult> CreateCountersByDevice(DeviceInfo deviceInfo)
        {
            var absolutePath = $"{ServiceHost}/{CreateCountersByDeviceQuery}?SubscriptionKey={subscriptionKey}";
            try
            {
                var deviceResult = await SendRequestAsync<DeviceInfo, DeviceResult>(HttpMethod.Post, absolutePath, deviceInfo);
                return deviceResult;
            }
            catch (Exception ex)
            {
                logger.Error("CreateCountersByDevice", ex);
            }
            return default(DeviceResult);

        }

        public async Task<DeviceResult> PostCountersByDevice(DeviceInfo deviceInfo)
        {
            var absolutePath = $"{ServiceHost}/{PostCountersByDeviceQuery}?SubscriptionKey={subscriptionKey}";
            try
            {
                var deviceResult = await SendRequestAsync<DeviceInfo, DeviceResult>(HttpMethod.Post, absolutePath, deviceInfo);
                return deviceResult;
            }
            catch (Exception ex)
            {
                logger.Error("PostCountersByDevice", ex);
            }
            return default(DeviceResult);
        }


        public async Task<DeviceResult> PostHeatmapByDevice(string mac, string heatmapImagePath, string cameraImagePath)
        {
            var absolutePath = $"{ServiceHost}/{PostHeatmapByDeviceQuery}?SubscriptionKey={subscriptionKey}";


            var nameValues = new NameValueCollection();
            nameValues.Add("mac", mac);
            nameValues.Add("deviceId", "0");
            try
            {
                var files = new List<string>();
                var names = new List<string>();
                if (!string.IsNullOrEmpty(heatmapImagePath) && File.Exists(heatmapImagePath))
                {
                    files.Add(heatmapImagePath);
                    names.Add("heatmapimage");
                }
                if (!string.IsNullOrEmpty(cameraImagePath) && File.Exists(cameraImagePath))
                {
                    files.Add(cameraImagePath);
                    names.Add("cameraImage");
                }
                return await SendMultipartFormRequestAsync<DeviceResult>(absolutePath, files.ToArray(), names.ToArray(), nameValues);
            }
            catch (Exception ex)
            {
                logger.Error("PostCountersByDevice", ex);
            }
            return default(DeviceResult);
        }


        #region the json client
        private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(HttpMethod httpMethod, string requestUrl, TRequest requestBody,string type="json")
        {
            var request = new HttpRequestMessage(httpMethod, ServiceHost);
            request.RequestUri = new Uri(requestUrl);
            if (requestBody != null)
            {
                if (requestBody is Stream)
                {
                    request.Content = new StreamContent(requestBody as Stream);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                }
                else if (requestBody is string)
                {
                    request.Content = new StringContent(requestBody as string);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                }
                else
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(requestBody, s_settings), Encoding.UTF8, JsonHeader);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }
            }

            HttpResponseMessage response = await s_httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = null;
                if (response.Content != null)
                {
                    responseContent = await response.Content.ReadAsStringAsync();
                }

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    return JsonConvert.DeserializeObject<TResponse>(responseContent, s_settings);
                }

                return default(TResponse);
            }
            else
            {
                if (response.Content != null && response.Content.Headers.ContentType.MediaType.Contains(JsonHeader))
                {
                    var errorObjectString = await response.Content.ReadAsStringAsync();
                    ClientError errorCollection = JsonConvert.DeserializeObject<ClientError>(errorObjectString);
                    if (errorCollection != null)
                    {
                        throw new ClientException(errorCollection, response.StatusCode);
                    }
                }

                response.EnsureSuccessStatusCode();
            }

            return default(TResponse);
        }

        private async Task<TResponse> SendMultipartFormRequestAsync<TResponse>(string requestUrl, string[] files, string[] names, NameValueCollection data)
        {

            using (MultipartFormDataContent form = new MultipartFormDataContent(("Upload----" + DateTime.Now.Ticks.ToString())))
            {
                //1.1 key/value
                foreach (string key in data.Keys)
                {
                    //Content-Disposition : form-data; name="json".
                    var stringContent = new StringContent(data[key]);
                    stringContent.Headers.Add("Content-Disposition", $"form-data; name={key}");
                    form.Add(stringContent, key);
                }

                //1.2 file
                for (int index = 0; index < files.Length; index++)
                {
                    var filePath = files[index];
                    FileStream stream = File.OpenRead(filePath);
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.Add("Content-Type", "application/octet-stream");
                    streamContent.Headers.Add("Content-Disposition", $"form-data; name={names[index]}; filename={Path.GetFileName(filePath)}");
                    form.Add(streamContent, names[index], Path.GetFileName(filePath));
                }

                HttpResponseMessage response = await s_httpClient.PostAsync(requestUrl, form);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = null;
                    if (response.Content != null)
                    {
                        responseContent = await response.Content.ReadAsStringAsync();
                    }

                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        return JsonConvert.DeserializeObject<TResponse>(responseContent, s_settings);
                    }
                    return default(TResponse);
                }
                else
                {
                    if (response.Content != null && response.Content.Headers.ContentType.MediaType.Contains(JsonHeader))
                    {
                        var errorObjectString = await response.Content.ReadAsStringAsync();
                        ClientError errorCollection = JsonConvert.DeserializeObject<ClientError>(errorObjectString);
                        if (errorCollection != null)
                        {
                            throw new ClientException(errorCollection, response.StatusCode);
                        }
                    }

                    response.EnsureSuccessStatusCode();
                }
                return default(TResponse);
            }
        }

        private string GetBasicFormNameValues()
        {
            return string.Empty;
            //return $"weiXinAppId={weiXinAppId}&gameId={gameId}&clientUniueId={clientUniueId}&activityId={activityId}&subscriptionKey={_subscriptionKey}";
        }

        //private void AddBasicNameValues(NameValueCollection collections)
        //{
        //    collections.Add("weiXinAppId", weiXinAppId);
        //    collections.Add("gameId", gameId);
        //    collections.Add("clientUniueId", clientUniueId);
        //    collections.Add("activityId", activityId);
        //    collections.Add("subscriptionKey", _subscriptionKey);
        //}
        #endregion
    }
}
