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
    public class Range<T>
    {
        public T Min { get; set; }
        public T Max { get; set; }
    }
    public class SensingDataAccess : ILocalSensingDataAccess
    {
        public static string AppPodDataDirectory { get; set; }
        public static DeviceSetting DeviceSetting { get; set; }

        private List<ShowProductInfo> mShowProducts;
        public SensingDataAccess(bool isAutoFindAppPodDataDirecotry = true)
        {
            if (isAutoFindAppPodDataDirecotry)
            {
                AppPodDataDirectory = FindAppPodDataFolder();
                DeviceSetting = FindDeviceSetting();
            }
        }

        public SensingDataAccess(string appPodDataDirectory)
        {
            AppPodDataDirectory = appPodDataDirectory;
        }


        public string GetQrcode(ShowProductInfo showProductInfo, string staffId)
        {
            if (showProductInfo == null) return null;
            string qrcode = string.Empty;
            var storeType = GetStoreType();
            if (showProductInfo.Type == ProductType.Product)
            {
                var pModel = FindByShowProduct(showProductInfo);
                qrcode = pModel.OnlineStoreInfos.FirstOrDefault(s => s.OnlineStoreType == storeType)?.Qrcode;
            }
            else
            {
                var sModel = FindSkuById(showProductInfo.Id);
                qrcode = sModel.OnlineStoreInfos.FirstOrDefault(s => s.OnlineStoreType == storeType)?.Qrcode;
            }
            if (string.IsNullOrEmpty(qrcode)) return null;
            if (!string.IsNullOrEmpty(staffId))
            {
                if (qrcode.EndsWith("&"))
                {
                    qrcode = $"{qrcode}sellerId={staffId}";
                }
                else
                {
                    qrcode = $"{qrcode}&sellerId={staffId}";
                }
            }
            return qrcode;
        }

        #region Bussiness Logical Data
        public string GetOnlineStoreStaffId(int staffId)
        {
            var storeType = GetStoreType();
            var staff = Staffs.Find(s => s.Id == staffId);
            if (staff == null) return null;
            var onlineStaff = staff.OnlineStoreProfiles.AsQueryable().FirstOrDefault(s => s.OnlineStoreType == storeType);
            if (onlineStaff == null) return staff.Code;
            return onlineStaff.Code;
        }

        public string GetOnlineStoreStaffIdByRFID(string rfidCode)
        {
            var staff = Staffs.Find(s => s.RFIDCode == rfidCode);
            if (staff == null) return null;
            return GetOnlineStoreStaffId(staff.Id);
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

        public SkuSdkModel FindSkuById(int skuId)
        {
            return Products?.FirstOrDefault(p => p.Skus.Any(s => s.Id == skuId)).Skus.FirstOrDefault(s => s.Id == skuId);
        }

        public ProductSdkModel FindByScanId(string skc)
        {
            throw new NotImplementedException();
        }


        public static string GetLocalImagePath(string path, string category)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var localPath = Extensions.ExtractSchema(path);
            return $"{SensingDataAccess.AppPodDataDirectory}\\{category}\\res\\{localPath}";
        }

        private string FindTagIcon(int[] tagIds)
        {
            if (Tags == null || Tags.Count == 0) return null;
            var tag = Tags.Find(t => tagIds.Any(id => id == t.Id) && t.IsSpecial == true);
            if (tag != null) return tag.GetLocalIconFile();
            return null;
        }

        public List<ShowProductInfo> QueryShowProducts(bool onlySpu = false)
        {
            if (Products == null || Products.Count == 0) return null;
            if (mShowProducts != null) return mShowProducts;
            string storeType = GetStoreType();
            if (onlySpu)
            {
                var infos = Products.Select(pModel => new ShowProductInfo
                {
                    Id = pModel.Id,
                    ImageUrl = GetLocalImagePath(pModel.PicUrl, "Products"),
                    Name = pModel.Title,
                    Price = pModel.Price,
                    //QrcodeUrl = pModel.OnlineStoreInfos.FirstOrDefault(s => s.OnlineStoreType == storeType)?.Qrcode,
                    Type = ProductType.Product,
                    TagIconUrl = FindTagIcon(pModel.Tags),
                    Product = pModel
                }).ToList();
                mShowProducts = infos;
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
                                Type = ProductType.Product,
                                TagIconUrl = FindTagIcon(prod.Tags),
                                Product = prod
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
                                //QrcodeUrl = prod.OnlineStoreInfos.FirstOrDefault(s => s.OnlineStoreType == storeType)?.Qrcode,
                                Type = ProductType.Product,
                                TagIconUrl = FindTagIcon(prod.Tags),
                                Product = prod
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
                                    //QrcodeUrl = prod.OnlineStoreInfos.FirstOrDefault(s => s.OnlineStoreType == storeType)?.Qrcode,
                                    TagIconUrl = FindTagIcon(firstSku.Tags),
                                    Type = ProductType.Sku,
                                    Product = prod
                                });
                            }
                        }
                    }
                }
                mShowProducts = showProducts;
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

        public List<ShowProductInfo> GetShowProductsByCategroyName(string categroyName)
        {
            var category = FindCategoryByName(categroyName);
            if (category != null)
            {
                return mShowProducts.Where(p => p.Product.CategoryIds != null && p.Product.CategoryIds.Contains(category.Id)).ToList();
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

        public List<ShowProductInfo> GetShowProductByCategoryNames(int[] categroyIds)
        {
            return mShowProducts.Where(p => p.Product.CategoryIds.Intersect(categroyIds).Count() > 0).ToList();
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
            return PCategories?.Where(p => p.IsSpecial == isSpecial).ToList();
        }

        public List<CouponViewModel> GetCoupons()
        {
            return Coupons.ToList();
        }

        public bool CanAddFilter(ProductSdkModel product, List<Range<float>> priceRanges, List<string> colors, List<int> tags, List<int> categories)
        {
            var priceOk = false;
            if (priceRanges != null && priceRanges.Count > 0)
            {
                foreach (var range in priceRanges)
                {
                    if (product.Price >= range.Min && product.Price <= range.Max)
                    {
                        priceOk = true;
                        break;
                    }
                }
            }
            else
            {
                priceOk = true;
            }

            var colorOK = false;
            if (colors != null && colors.Count > 0)
            {
                foreach (var color in colors)
                {
                    if ((!string.IsNullOrEmpty(product.Title) && product.Title.Contains(color))
                        || (!string.IsNullOrEmpty(product.Description) && product.Description.Contains(color)))
                    {
                        colorOK = true;
                        break;
                    }
                }
            }
            else
            {
                colorOK = true;
            }

            var tagOK = false;
            if (tags != null && tags.Count > 0)
            {
                if (product.Tags != null)
                {
                    foreach (var tag in tags)
                    {
                        if (product.Tags.Contains(tag))
                        {
                            tagOK = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                tagOK = true;
            }

            var categoryOK = false;
            if (categories != null && categories.Count > 0)
            {
                if (product.CategoryIds != null)
                {
                    foreach (var category in categories)
                    {
                        if (product.CategoryIds.Contains(category))
                        {
                            categoryOK = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                categoryOK = true;
            }


            return priceOk && colorOK && tagOK && categoryOK;
        }

        public bool CanAddFilter(SkuSdkModel sku, List<Range<float>> priceRanges, List<string> colors, List<int> tags)
        {
            var priceOk = false;
            if (priceRanges != null && priceRanges.Count > 0)
            {
                foreach (var range in priceRanges)
                {
                    if (sku.Price >= range.Min && sku.Price <= range.Max)
                    {
                        priceOk = true;
                        break;
                    }
                }
            }
            else
            {
                priceOk = true;
            }

            var colorOK = false;
            if (colors != null && colors.Count > 0)
            {
                foreach (var color in colors)
                {
                    if ((!string.IsNullOrEmpty(sku.Title) && sku.Title.Contains(color))
                        || (!string.IsNullOrEmpty(sku.Description) && sku.Description.Contains(color))
                        || (!string.IsNullOrEmpty(sku.ColorName) && sku.ColorName.Contains(color))
                        || (!string.IsNullOrEmpty(sku.PropsName) && sku.PropsName.Contains(color))
                        )
                    {
                        colorOK = true;
                        break;
                    }
                }
            }
            else
            {
                colorOK = true;
            }

            var tagOK = false;
            if (tags != null && tags.Count > 0)
            {
                if (sku.Tags != null)
                {
                    foreach (var tag in tags)
                    {
                        if (sku.Tags.Contains(tag))
                        {
                            tagOK = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                tagOK = true;
            }

            return priceOk && colorOK && tagOK;
        }

        private bool ProductIsOK(ProductSdkModel product, List<int> categories)
        {
            if (categories == null) return true;
            if (categories.Count < 1) return true;
            if (categories.Intersect(product.CategoryIds).Count() > 0) return true;
            return false;
        }
        public List<ShowProductInfo> SearchProducts(List<Range<float>> priceRanges, List<string> colors, List<int> categories, List<int> tags, bool onlySpu = false)
        {
            if (Products == null || Products.Count == 0) return null;
            string storeType = GetStoreType();
            if (onlySpu)
            {
                var infos = Products.Where(p => CanAddFilter(p, priceRanges, colors, tags, categories)).Select(pModel => new ShowProductInfo
                {
                    Id = pModel.Id,
                    ImageUrl = GetLocalImagePath(pModel.PicUrl, "Products"),
                    Name = pModel.Title,
                    Price = pModel.Price,
                    //QrcodeUrl = pModel.OnlineStoreInfos.FirstOrDefault(s => s.OnlineStoreType == storeType)?.Qrcode,
                    Type = ProductType.Product,
                    TagIconUrl = FindTagIcon(pModel.Tags),
                    Product = pModel
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
                            if (CanAddFilter(prod, priceRanges, colors, tags, categories))
                            {
                                showProducts.Add(new ShowProductInfo
                                {
                                    Id = prod.Id,
                                    ImageUrl = GetLocalImagePath(prod.PicUrl, "Products"),
                                    Name = prod.Title,
                                    Price = prod.Price,
                                    Quantity = prod.Num,
                                    Type = ProductType.Product,
                                    TagIconUrl = FindTagIcon(prod.Tags),
                                    Product = prod
                                });
                            }
                        }
                        continue;
                    }
                    if (prod.Skus != null && prod.Skus.Count() == 0)
                    {
                        if (!prod.HasRealSkus)
                        {
                            if (CanAddFilter(prod, priceRanges, colors, tags, categories))
                            {
                                showProducts.Add(new ShowProductInfo
                                {
                                    Id = prod.Id,
                                    ImageUrl = GetLocalImagePath(prod.PicUrl, "Products"),
                                    Quantity = prod.Num,
                                    Name = prod.Title,
                                    Price = prod.Price,
                                    //QrcodeUrl = prod.OnlineStoreInfos.FirstOrDefault(s => s.OnlineStoreType == storeType)?.Qrcode,
                                    Type = ProductType.Product,
                                    TagIconUrl = FindTagIcon(prod.Tags),
                                    Product = prod
                                });
                            }
                        }
                        continue;
                    }
                    if (prod.PropImgs != null && prod.PropImgs.Count() > 0)
                    {
                        if (ProductIsOK(prod, categories))
                        {
                            foreach (var pImg in prod.PropImgs)
                            {
                                var keyProps = pImg.PropertyName;
                                var firstSku = prod.Skus.AsQueryable().FirstOrDefault(s => s.PropsName.Contains(keyProps) && CanAddFilter(s, priceRanges, colors, tags));
                                if (firstSku != null)
                                {
                                    showProducts.Add(new ShowProductInfo
                                    {
                                        Id = firstSku.Id,
                                        ImageUrl = GetLocalImagePath(pImg.ImageUrl, "Products"),
                                        Quantity = firstSku.Quantity,
                                        Name = firstSku.Title,
                                        Price = firstSku.Price,
                                        //QrcodeUrl = prod.OnlineStoreInfos.FirstOrDefault(s => s.OnlineStoreType == storeType)?.Qrcode,
                                        TagIconUrl = FindTagIcon(firstSku.Tags),
                                        Type = ProductType.Sku,
                                        Product = prod
                                    });
                                }
                            }
                        }
                    }
                }
                return showProducts;
            }
        }

        public List<ShowProductInfo> SearchShowProductsByName(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<ShowProductInfo>();

            //TODO : 拼音搜索商品的名称
            return mShowProducts.Where(p => p.Type == ProductType.Sku && p.Id.ToString() == searchTerm).ToList();
        }

        public void Like(ProductSdkModel productInfo)
        {
            //throw new NotImplementedException();
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
        public static string FindAppPodRootFolder()
        {
            var exeRoot = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo root = new DirectoryInfo(exeRoot);
            while (root != null)
            {
                if (File.Exists(Path.Combine(root.FullName, "AppPod.exe")))
                {
                    return root.FullName;
                }
                root = root.Parent;
            }
            return null;
        }

        public static string FindAppPodDataFolder()
        {
            var rootFolder = FindAppPodRootFolder();
            if (rootFolder == null)
                return null;
            return Path.Combine(rootFolder, "AppPodData");

        }

        private static DeviceSetting FindDeviceSetting()
        {
            var rootFolder = FindAppPodRootFolder();
            if (rootFolder == null)
                return null;
            var sqlconnection = new SQLite.SQLiteConnection(rootFolder + "/_data/AppPodData.db");
            return sqlconnection.Table<DeviceSetting>().FirstOrDefault();
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
        public List<TagSdkModel> Tags { get; set; }
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

        public List<TagSdkModel> ReadTags()
        {
            var path = $"{AppPodDataDirectory}/Products/Tags.json";
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<TagSdkModel>>(json);
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
            Tags = ReadTags();
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
                if (useSameSpu)
                {
                    var product = FindByShowProduct(productInfo);
                    similarSkus = DistinctShowProducts(product, productInfo.Id);
                    if (similarSkus == null) similarSkus = new List<ShowProductInfo>();
                    if (similarSkus.Count == 0)
                    {
                        similarSkus.Insert(0, productInfo);
                    }
                }
            }
            if (productInfo.Type == ProductType.Sku)
            {
                if (useSameSpu)
                {
                    var spu = Products?.FirstOrDefault(p => p.Skus.Any(s => s.Id == productInfo.Id));
                    similarSkus = DistinctShowProducts(spu, productInfo.Id);
                    if (similarSkus == null) similarSkus = new List<ShowProductInfo>();
                    similarSkus.Insert(0, productInfo);
                }
            }
            return similarSkus;
        }

        public PropertyInfo GetKeyPropertyInfoInSkus(ShowProductInfo info)
        {
            var productSdkModel = FindByShowProduct(info);
            return GetKeyPropertyInfoInSkus(productSdkModel);
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
                            if (propInfo.Name == info.Key)
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
                    if (info == null)
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
                if (key != null)
                {
                    pInfos.Remove(key);
                    pInfos.Insert(0, keyPInfo);
                }
            }
            return pInfos;
        }

        public List<PropertyInfo> GetPropertyInfosInSkus(ShowProductInfo product)
        {
            var productSdkInfo = FindByShowProduct(product);
            return GetPropertyInfosInSkus(productSdkInfo);
        }

        public string GetStoreType()
        {
            if (DeviceSetting == null)
                return "Taobao";
            return DeviceSetting.OnlineTrafficTarget.ToString();
        }

        /// <summary>
        /// nameValue must like that, 颜色:红色
        /// </summary>
        /// <param name="nameValues"></param>
        /// <returns></returns>
        public List<PropertyInfo> FindReminderAvailablePropertiesInSkus(ShowProductInfo showProduct, params string[] nameValues)
        {
            var productSdkModel = FindByShowProduct(showProduct);
            if (productSdkModel == null || productSdkModel.Skus == null || productSdkModel.Skus.Count() == 0 || nameValues.Length == 0) return null;
            var pInfos = new List<PropertyInfo>();
            List<SkuSdkModel> includeSkus = null;

            includeSkus = productSdkModel.Skus.Where(s => ContainsAll(s.PropsName, nameValues)).ToList();

            foreach (var sku in includeSkus)
            {
                var props = GetPropertyNames(sku.PropsName);
                foreach (var prop in props)
                {
                    var info = pInfos.Find(p => p.Name == prop.Key);
                    if (info == null)
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
            foreach (var nameValue in nameValues)
            {
                var keyValueArrary = nameValue.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (keyValueArrary.Length != 2) continue;
                var key = keyValueArrary[0];
                var value = keyValueArrary[1];
                var existedPInfo = pInfos.Find(p => p.Name == key);
                if (existedPInfo != null) pInfos.Remove(existedPInfo);
            }
            return pInfos;
        }


        public static bool ContainsAll(string propNames, string[] keyValues)
        {
            if (string.IsNullOrEmpty(propNames) || keyValues.Length == 0) return false;
            foreach (var value in keyValues)
            {
                if (!propNames.Contains(value)) return false;
            }
            return true;
        }
    }
}
