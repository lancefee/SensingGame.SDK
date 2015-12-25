﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensngGame.ClientSDK.Contract;
using System.Net.Http;
using SensingSite.ClientSDK.Common;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http.Headers;
using Newtonsoft.Json.Serialization;
using System.Collections.Specialized;
using LogService;

namespace SensngGame.ClientSDK
{
    public class GameServiceClient : IGameServiceClient
    {
        /// <summary>
        /// The service host.
        /// </summary>
        //private const string ServiceHost = "http://game.troncell.com/api/v0/WeixinApi";

        private const string ServiceHost = "http://localhost:4468/api/v0/WeixinApi";

        /// <summary>
        /// The json header
        /// </summary>
        private const string JsonHeader = "application/json";

        /// <summary>
        /// The qrcode4login.
        /// </summary>
        private const string QrCode4LoginQuery = "QrCode4Login";

        /// <summary>
        /// The postdata4scan.
        /// </summary>
        private const string PostData4ScanQuery = "PostData";

        /// <summary>
        /// The find scan qrcode user query.
        /// </summary>
        private const string FindScanQCodeUserQuery = "FindScanQrCodeUser";

        /// <summary>
        /// The post data by user query.
        /// </summary>
        private const string PostDataByUserQuery = "PostDataByUser";


        private const string GetUsersByActivityQuery = "GetUsersByActivitiy";

        private const string GetRankUsersByActivityQuery = "GetRankUsersByActivity";

        private const string GetActivityInfoQuery = "GetActivityInfo";

        private const string GetAwardsByActivityQuery = "GetAwardsByActivity";

        private const string WinAwardByActionIdQuery = "WinAwardByActionId";

        private const string GetWinAwardUsersByActivityIdQuery = "GetWinAwardUsersByActivityId";

        private const string WinAwardByRandomQuery = "WinAwardByRandom";





        /// <summary>
        /// The subscription key name.
        /// </summary>
        private const string SubscriptionKeyName = "SubscriptionKey";

        #region Inner Keys.
        /// <summary>
        /// The subscription key.
        /// </summary>
        private string _subscriptionKey;

        private string weiXinAppId;
        private string gameId;
        private string activityId;
        private string clientUniueId;
        #endregion

        private static readonly IBizLogger logger = ServerLogFactory.GetLogger(typeof(GameServiceClient));

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

        public GameServiceClient(string subscriptionKey, string weiXinAppId, string gameId, string activityId)
        {
            this._subscriptionKey = subscriptionKey;
            this.weiXinAppId = weiXinAppId;
            this.gameId = gameId;
            this.activityId = activityId;
            this.clientUniueId = MacIPHelper.GetClientMac();

            s_httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        #region QrcodeClient
        public async Task<UserActionResult> FindScanQrCodeUserAsync(string qrCodeId)
        {
            var absolutePath = $"{ServiceHost}/{FindScanQCodeUserQuery}";
            var formNameValues = $"qrCodeId={qrCodeId}&{GetBasicFormNameValues()}";
            try
            {
                var userAction = await SendRequestAsync<string, UserActionResult>(HttpMethod.Post, absolutePath, formNameValues);
                return userAction;
            }
            catch (Exception ex)
            {
                logger.Error("FindScanQrCodeUserAysnc", ex);
            }
            return default(UserActionResult);
        }

        public async Task<QrCodeResult> GetQrCode4LoginAsync(bool isSendWeChatMsg = false)
        {
            var absolutePath = $"{ServiceHost}/{QrCode4LoginQuery}";
            var formNameValues = $"isSendWeChatMsg={isSendWeChatMsg}&{GetBasicFormNameValues()}";
            try
            {
                return await SendRequestAsync<object, QrCodeResult>(HttpMethod.Post, absolutePath, formNameValues);
            }
            catch (Exception ex)
            {
                logger.Error("GetQrCode4LoginAsync", ex);
            }
            return default(QrCodeResult);
        }

        public async Task<QrCodeResult> PostData4ScanAsync(string playerImage, string gameImage, int score)
        {
            var absolutePath = $"{ServiceHost}/{PostData4ScanQuery}";
            var nameValues = new NameValueCollection();
            AddBasicNameValues(nameValues);
            nameValues.Add("score", score.ToString());
            try
            {
                var files = new List<string>();
                var names = new List<string>();
                if (!string.IsNullOrEmpty(playerImage) && File.Exists(playerImage))
                {
                    files.Add(playerImage);
                    names.Add("playerimage");
                }
                if (!string.IsNullOrEmpty(gameImage) && File.Exists(gameImage))
                {
                    files.Add(gameImage);
                    names.Add("gameImage");
                }
                return await SendMultipartFormRequestAsync<QrCodeResult>(absolutePath, files.ToArray(), names.ToArray(), nameValues);
            }
            catch (Exception ex)
            {
                logger.Error("PostData4ScanAsync", ex);
            }
            return default(QrCodeResult);
        }

        public async Task<UserActionResult> PostDataByUserAsync(string actionId, string playerImage, string gameImage, int score, bool isSendWeChatMsg = true)
        {
            var absolutePath = $"{ServiceHost}/{PostDataByUserQuery}";
            var nameValues = new NameValueCollection();
            AddBasicNameValues(nameValues);
            nameValues.Add("actionId", actionId);
            nameValues.Add("score", score.ToString());
            nameValues.Add("isSendWeChatMsg", isSendWeChatMsg.ToString());
            try
            {
                var files = new List<string>();
                var names = new List<string>();
                if (!string.IsNullOrEmpty(playerImage) && File.Exists(playerImage))
                {
                    files.Add(playerImage);
                    names.Add("playerimage");
                }
                if (!string.IsNullOrEmpty(gameImage) && File.Exists(gameImage))
                {
                    files.Add(gameImage);
                    names.Add("gameImage");
                }
                return await SendMultipartFormRequestAsync<UserActionResult>(absolutePath, files.ToArray(), names.ToArray(), nameValues);
            }
            catch (Exception ex)
            {
                logger.Error("PostData4ScanAsync", ex);
            }
            return default(UserActionResult);
        }
        #endregion

        #region Activity Apis

        public async Task<UserActionsResult> GetUsersByActivitiy(int maxUserCount)
        {
            var absolutePath = $"{ServiceHost}/{GetUsersByActivityQuery}";
            var formNameValues = $"maxUsersCnt={maxUserCount}&{GetBasicFormNameValues()}";
            try
            {
                var usersAction = await SendRequestAsync<string, UserActionsResult>(HttpMethod.Post, absolutePath, formNameValues);
                return usersAction;
            }
            catch (Exception ex)
            {
                logger.Error("GetUsersByActivitiy", ex);
            }
            return default(UserActionsResult);
        }


        public async Task<UserActionsResult> GetRankUsersByActivity(string rankColumn, int maxRankUsersCnt)
        {
            var absolutePath = $"{ServiceHost}/{GetRankUsersByActivityQuery}";
            var formNameValues = $"maxRankUsersCnt={maxRankUsersCnt}&rankColumn={rankColumn}&{GetBasicFormNameValues()}";
            try
            {
                var usersActions = await SendRequestAsync<string, UserActionsResult>(HttpMethod.Post, absolutePath, formNameValues);
                return usersActions;
            }
            catch (Exception ex)
            {
                logger.Error("GetUsersByActivitiy", ex);
            }
            return default(UserActionsResult);
        }

        public async Task<ActivityResult> GetActivityInfo()
        {
            var absolutePath = $"{ServiceHost}/{GetActivityInfoQuery}";
            var formNameValues = $"{GetBasicFormNameValues()}";
            try
            {
                var activity = await SendRequestAsync<string, ActivityResult>(HttpMethod.Post, absolutePath, formNameValues);
                return activity;
            }
            catch (Exception ex)
            {
                logger.Error("GetUsersByActivitiy", ex);
            }
            return default(ActivityResult);
        }

        #endregion




        #region Awards Apis
        public async Task<AwardsResult> GetAwardsByActivity()
        {
            var absolutePath = $"{ServiceHost}/{GetAwardsByActivityQuery}";
            var formNameValues = $"{GetBasicFormNameValues()}";
            try
            {
                var awards = await SendRequestAsync<string, AwardsResult>(HttpMethod.Post, absolutePath, formNameValues);
                return awards;
            }
            catch (Exception ex)
            {
                logger.Error("GetAwardsByActivity", ex);
            }
            return default(AwardsResult);
        }


        public async Task<UserActionResult> WinAwardByActionId(string actionId)
        {
            var absolutePath = $"{ServiceHost}/{GetAwardsByActivityQuery}";
            var formNameValues = $"actionId={actionId}&{GetBasicFormNameValues()}";
            try
            {
                var userAction = await SendRequestAsync<string, UserActionResult>(HttpMethod.Post, absolutePath, formNameValues);
                return userAction;
            }
            catch (Exception ex)
            {
                logger.Error("WinAwardByActionId", ex);
            }
            return default(UserActionResult);
        }

        public async Task<UserActionResult> GetWinAwardUsersByActivityId(string actionId, string awardId)
        {
            var absolutePath = $"{ServiceHost}/{GetWinAwardUsersByActivityIdQuery}";
            var formNameValues = $"actionId={actionId}&awardId={awardId}&{GetBasicFormNameValues()}";
            try
            {
                var userAction = await SendRequestAsync<string, UserActionResult>(HttpMethod.Post, absolutePath, formNameValues);
                return userAction;
            }
            catch (Exception ex)
            {
                logger.Error("WinAwardByActionId", ex);
            }
            return default(UserActionResult);
        }


        public async Task<UserActionResult> WinAwardByRandom(string awardId)
        {
            var absolutePath = $"{ServiceHost}/{WinAwardByRandomQuery}";
            var formNameValues = $"awardId={awardId}&{GetBasicFormNameValues()}";
            try
            {
                var userAction = await SendRequestAsync<string, UserActionResult>(HttpMethod.Post, absolutePath, formNameValues);
                return userAction;
            }
            catch (Exception ex)
            {
                logger.Error("WinAwardByRandom", ex);
            }
            return default(UserActionResult);
        }

        #endregion
        #region the json client
        private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(HttpMethod httpMethod, string requestUrl, TRequest requestBody)
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
            return $"weiXinAppId={weiXinAppId}&gameId={gameId}&clientUniueId={clientUniueId}&activityId={activityId}&subscriptionKey={_subscriptionKey}";
        }

        private void AddBasicNameValues(NameValueCollection collections)
        {
            collections.Add("weiXinAppId", weiXinAppId);
            collections.Add("gameId", gameId);
            collections.Add("clientUniueId", clientUniueId);
            collections.Add("activityId", activityId);
            collections.Add("subscriptionKey", _subscriptionKey);
        }
        #endregion

    }
}
