﻿using AppPod.DataAccess.Models;
using Newtonsoft.Json;
using Sensing.SDK.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppPod.DataAccess
{
    public class SensingDataAccess : ILocalSensingDataAccess
    {
        public static string AppPodDataDirectory { get; set; }
        public SensingDataAccess(bool isAutoFindAppPodDataDirecotry = true)
        {
            if (isAutoFindAppPodDataDirecotry)
            {
                AppPodDataDirectory = FindAppPodDataFolder();
            }
        }

        public SensingDataAccess(string appPodDataDirectory)
        {
            AppPodDataDirectory = appPodDataDirectory;
        }

        #region Bussiness Logical Data
        public async Task<string> GetOnlineStoreStaffId(int staffId, string storeType)
        {
            var staff = Staffs.Find(s => s.Id == staffId);
            if (staff == null) return null;
            var onlineStaff = staff.OnlineStoreProfiles.AsQueryable().FirstOrDefault(s => s.OnlineStoreType == storeType);
            if (onlineStaff == null) return staff.Code;
            return onlineStaff.Code;
        }

        public ProductSdkModel FindByProductId(int id)
        {
            return Products?.Find(p => p.Id == id);
        }

        public ProductSdkModel FindBySkuId(int skuId)
        {
            return Products?.FirstOrDefault(p => p.Skus.Any(s => s.Id == skuId));
        }

        public ProductSdkModel FindByShowProduct(ShowProductInfo showProductInfo)
        {
            if (showProductInfo == null) return null;
            if (showProductInfo.Type == ProductType.Product) return FindByProductId(showProductInfo.Id);
            return FindBySkuId(showProductInfo.Id);
        }

        public ProductSdkModel FindByScanId(string skc)
        {
            throw new Exception();
        }


        public static string GetLocalImagePath(string path, string category)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var localPath = Extensions.ExtractSchema(path);
            return $"{SensingDataAccess.AppPodDataDirectory}\\{category}\\res\\{localPath}";
        }


        public List<ShowProductInfo> QueryShowProducts(bool onlySpu = false)
        {
            if (Products == null || Products.Count == 0) return null;
            if (onlySpu)
            {
                var infos = Products.Select(pModel => new ShowProductInfo
                {
                    Id = pModel.Id,
                    ImageUrl = GetLocalImagePath(pModel.PicUrl, "Product"),
                    Name = pModel.Title,
                    Price = pModel.Price,
                    Type = ProductType.Product
                }).ToList();
                return infos;
            }
            else
            {
                var showProducts = new List<ShowProductInfo>();
                foreach (var prod in Products)
                {
                    if (prod.Skus == null)
                    {
                        if (prod.HasRealSkus == false)
                        {
                            showProducts.Add(new ShowProductInfo
                            {
                                Id = prod.Id,
                                ImageUrl = GetLocalImagePath(prod.PicUrl, "Products"),
                                Name = prod.Title,
                                Price = prod.Price,
                                Quantity = prod.Num,
                                Type = ProductType.Product
                            });
                        }
                        continue;
                    }
                    if (prod.Skus != null && prod.Skus.Count() == 0)
                    {
                        if (!prod.HasRealSkus)
                        {
                            showProducts.Add(new ShowProductInfo
                            {
                                Id = prod.Id,
                                ImageUrl = GetLocalImagePath(prod.PicUrl, "Products"),
                                Quantity = prod.Num,
                                Name = prod.Title,
                                Price = prod.Price,
                                Type = ProductType.Product
                            });
                        }
                        continue;
                    }
                    if (prod.PropImgs != null && prod.PropImgs.Count() > 0)
                    {
                        foreach (var pImg in prod.PropImgs)
                        {
                            var keyProps = pImg.PropertyName;
                            var firstSku = prod.Skus.AsQueryable().FirstOrDefault(s => s.PropsName.Contains(keyProps));
                            if (firstSku != null)
                            {
                                showProducts.Add(new ShowProductInfo
                                {
                                    Id = firstSku.Id,
                                    ImageUrl = GetLocalImagePath(pImg.ImageUrl, "Products"),
                                    Quantity = firstSku.Quantity,
                                    Name = firstSku.Title,
                                    Price = firstSku.Price,
                                    Type = ProductType.Sku
                                });
                            }
                        }
                    }
                }
                return showProducts;
            }
        }


        public List<ProductSdkModel> GetProductsByCategroyName(string categroyName)
        {
            var category = FindCategoryByName(categroyName);
            if (category != null)
            {
                return Products.Where(p => p.CategoryIds != null && p.CategoryIds.Contains(category.Id)).ToList();
            }
            return null;
        }

        public List<ProductSdkModel> GetProductsByCategroyNames(string[] categroyNames)
        {
            var ids = FindCategoryIdsByNames(categroyNames);
            if (Products == null || ids == null) return null;
            var prods = new List<ProductSdkModel>();
            foreach (var p in Products)
            {
                foreach (var id in ids)
                {
                    if (p.CategoryIds.Contains(id))
                    {
                        prods.Add(p);
                        break;
                    }
                }
            }
            return prods;
        }

        public ProductCategorySDKModel FindCategoryByName(string categoryName)
        {
            return PCategories.Find(p => p.Name.Contains(categoryName));
        }

        public List<int> FindCategoryIdsByNames(string[] categoryNames)
        {
            return PCategories.Where(p => categoryNames.Any(c => c == p.Name)).Select(s => s.Id).ToList();
        }
        public List<ProductCategorySDKModel> GetCategroyInfos(bool isSpecial = true)
        {
            return PCategories?.Where(p => p.IsSpecial).ToList();
        }

        public List<CouponViewModel> GetCoupons()
        {
            return Coupons.ToList();
        }

        public List<ProductSdkModel> SearchProducts(float minPrice, float maxPrice, List<string> colors, List<int> categoryIds, List<string> tags)
        {
            throw new NotImplementedException();
        }

        public List<ProductSdkModel> SearchProductsByName(string searchTerm)
        {
            throw new NotImplementedException();
        }

        public void Like(ProductSdkModel productInfo)
        {
            throw new NotImplementedException();
        }
        public List<AdsSdkModel> GetAdsInfos()
        {
            return Ads;
        }
        public bool IsCompleted()
        {
            return true;
        }
        #endregion

        #region AppPod Base
        public static string FindAppPodDataFolder()
        {
            var exeRoot = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo root = new DirectoryInfo(exeRoot);
            while (root != null)
            {
                if (File.Exists(Path.Combine(root.FullName, "AppPod.exe")))
                {
                    return root.FullName + @"\AppPodData\";
                }
                root = root.Parent;
            }
            return null;
        }

        private static string FindResourceFolder()
        {
            var exeRoot = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo root = new DirectoryInfo(exeRoot);
            while (root != null)
            {
                if (File.Exists(Path.Combine(root.FullName, "AppPod.exe")))
                {
                    return root.FullName + "/AppPodData/";
                }
                root = root.Parent;
            }
            return null;
        }
        #endregion

        public List<AdsSdkModel> Ads { get; set; }
        public List<StaffSdkModel> Staffs { get; set; }
        public List<ProductSdkModel> Products { get; set; }
        public List<CouponViewModel> Coupons { get; set; }
        public List<ProductCategorySDKModel> PCategories { get; set; }

        public List<MatchInfoViewModel> Matches { get; set; }
        public List<LikeInfoViewModel> Likes { get; set; }


        #region Read Data from Local Json.
        public List<AdsSdkModel> ReadAds()
        {
            var path = $"{AppPodDataDirectory}/Ads/Ads.json";
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<AdsSdkModel>>(json);
        }

        public List<MatchInfoViewModel> ReadProductMatches()
        {
            var path = $"{AppPodDataDirectory}/Products/Matches.json";
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<MatchInfoViewModel>>(json);
        }

        public List<LikeInfoViewModel> ReadProductLikes()
        {
            var path = $"{AppPodDataDirectory}/Products/Likes.json";
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<LikeInfoViewModel>>(json);
        }

        public List<CouponViewModel> ReadCoupons()
        {
            var path = $"{AppPodDataDirectory}/Products/Coupons.json";
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<CouponViewModel>>(json);
        }

        public List<ProductSdkModel> ReadProducts()
        {
            var path = $"{AppPodDataDirectory}/Products/Products.json";
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<ProductSdkModel>>(json);
        }

        public List<ProductCategorySDKModel> ReadCategorys()
        {
            var path = $"{AppPodDataDirectory}/Products/Categories.json";
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<ProductCategorySDKModel>>(json);

        }

        public List<StaffSdkModel> ReadStaffs()
        {
            var path = $"{AppPodDataDirectory}/Staffs/Staffs.json";
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<StaffSdkModel>>(json);
        }

        public static async Task<string> ReadText(string filePath)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                StringBuilder sb = new StringBuilder();

                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.Unicode.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }

                return sb.ToString();
            }
        }
        #endregion

        public bool Intialize()
        {
            Ads = ReadAds();
            Staffs = ReadStaffs();
            Products = ReadProducts();
            PCategories = ReadCategorys();
            Coupons = ReadCoupons();
            Matches = ReadProductMatches();
            Likes = ReadProductLikes();
            return true;
        }

        public List<ShowProductInfo> DistinctShowProducts(ProductSdkModel prod, int exceptSkuId = -1)
        {
            if (prod == null) return null;
            if (prod.PropImgs != null && prod.PropImgs.Count() > 0)
            {
                var showProducts = new List<ShowProductInfo>();
                foreach (var pImg in prod.PropImgs)
                {
                    var keyProps = pImg.PropertyName;
                    var firstSku = prod.Skus.AsQueryable().FirstOrDefault(s => s.PropsName.Contains(keyProps));
                    if (firstSku != null)
                    {
                        if (firstSku.Id != exceptSkuId)
                        {
                            showProducts.Add(new ShowProductInfo
                            {
                                Id = firstSku.Id,
                                ImageUrl = GetLocalImagePath(pImg.ImageUrl, "Products"),
                                Quantity = firstSku.Quantity,
                                Name = firstSku.Title,
                                Price = firstSku.Price,
                                Type = ProductType.Sku
                            });
                        }
                    }
                }
                return showProducts;
            }
            return null;
        }

        public List<ShowProductInfo> FindSimilar(ShowProductInfo productInfo, bool useSameSpu = true, bool useSameCategories = false)
        {
            //todo:Qu.
            if (productInfo == null) return null;
            List<ShowProductInfo> similarSkus = new List<ShowProductInfo>();
            if (useSameSpu == false && useSameCategories == false)
            {
                similarSkus.Add(productInfo);
                return similarSkus;
            }
            if (productInfo.Type == ProductType.Product)
            {

            }
            if (productInfo.Type == ProductType.Sku)
            {
                if (useSameSpu)
                {
                    var spu = Products?.FirstOrDefault(p => p.Skus.Any(s => s.Id == productInfo.Id));
                    similarSkus = DistinctShowProducts(spu, productInfo.Id);
                    similarSkus.Insert(0, productInfo);
                }
                return similarSkus;
            }
            return null;
        }

        public PropertyInfo GetKeyPropertyInfoInSkus(ProductSdkModel product)
        {
            if (product == null || product.PropImgs == null || product.PropImgs.Count() == 0) return null;
            PropertyInfo propInfo = null;
            foreach (var propImg in product.PropImgs)
            {
                if (product.Skus != null)
                {
                    var first = product.Skus.FirstOrDefault(s => s.PropsName.Contains(propImg.PropertyName));
                    if (first != null)
                    {
                        var propName = propImg.PropertyName;
                        var info = GetPropertyName(propName);
                        if (info.Key == null) continue;
                        if (propInfo == null)
                        {
                            propInfo = new PropertyInfo { IsKey = true, Name = info.Key };
                            propInfo.Values.Add(new PropertyValueInfo { Name = info.Value, ImageUrl = propImg.ImageUrl });
                        }
                        else
                        {
                            if (propInfo.Name != info.Key)
                            {
                                var existedValue = propInfo.Values.Find(v => v.Name == info.Value);
                                if (existedValue != null) continue;
                                propInfo.Values.Add(new PropertyValueInfo { Name = info.Value, ImageUrl = propImg.ImageUrl });
                            }
                        }
                    }
                }
            }
            return propInfo;
        }

        public (string Key, string Value) GetPropertyName(string properties)
        {
            if (string.IsNullOrEmpty(properties)) return (null, null);
            var values = properties.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (values == null || values.Length != 4) return (null, null);
            if (values != null && values.Length == 4)
            {
                return (values[2], values[3]);
            }
            return (null, null);
        }


        public Dictionary<string, string> GetPropertyNames(string properties)
        {
            if (string.IsNullOrEmpty(properties)) return null;
            var props = properties.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            if (props == null) return null;
            var keyValues = new Dictionary<string, string>();
            foreach (var prop in props)
            {
                var values = prop.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (values == null || values.Length != 4) continue;
                keyValues.Add(values[2], values[3]);
            }
            return keyValues;
        }

        public List<PropertyInfo> GetPropertyInfosInSkus(ProductSdkModel product)
        {
            
            if (product == null || product.Skus == null || product.Skus.Count() == 0) return null;
            var pInfos = new List<PropertyInfo>();
            var keyPInfo = GetKeyPropertyInfoInSkus(product);
            foreach (var sku in product.Skus)
            {
                var props = GetPropertyNames(sku.PropsName);
                foreach (var prop in props)
                {
                    var info = pInfos.Find(p => p.Name == prop.Key);
                    if(info == null)
                    {
                        info = new PropertyInfo { Name = prop.Key };
                        pInfos.Add(info);
                    }
                    if (!info.Values.Any(p => p.Name == prop.Value))
                    {
                        info.Values.Add(new PropertyValueInfo { Name = prop.Value });
                    }
                }
            }
            if (keyPInfo != null)
            {
                var key = pInfos.Find(p => p.Name == keyPInfo.Name);
                if (key != null) key.IsKey = true;
            }
            return pInfos;
        }

        public List<PropertyInfo> GetPropertyInfosInSkus(ShowProductInfo product)
        {
            var productSdkInfo = FindByShowProduct(product);
            return GetPropertyInfosInSkus(productSdkInfo);
        }
    }
}
