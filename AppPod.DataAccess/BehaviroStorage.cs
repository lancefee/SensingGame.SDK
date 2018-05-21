﻿using AppPod.DataAccess.Models;
using Sensing.SDK;
using Sensing.SDK.Contract;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppPod.DataAccess
{
    public interface IBehaviorDataUploader
    {
        void AddBehavoirData(string thingId, string category, string action);
        void AddClick(AdsSdkModel ads);
        void AddLike(ShowProductInfo productInfo);
        void AddClick(ShowProductInfo productInfo);
    }
    public class BehaviroStorage : IBehaviorDataUploader
    {
        private SensingWebClient sesingWebClient;

        private string DBPath = "Behavior.db";
        private SQLite.SQLiteConnection m_db;
        private object lockobject = new object();
        private string mMac;

        public BehaviroStorage(SensingWebClient webClient, string mac)
        {
            sesingWebClient = webClient;
            mMac = mac;
            m_db = new SQLite.SQLiteConnection(DBPath);
            m_db.CreateTable<SqlLiteBehaviorRecord>();
        }

        public void AddClick(AdsSdkModel ads)
        {
            if (ads == null) return;
            Task.Factory.StartNew(() =>
            {
                AddBehavoirData(ads.Id.ToString(), "ads", "click");
            });
        }

        public void AddLike(ShowProductInfo productInfo)
        {
            if (productInfo == null) return;
            Task.Factory.StartNew(() => 
            {
                AddBehavoirData(productInfo.Id.ToString(), productInfo.Type.ToString(), "like");
            });
        }

        public void AddClick(ShowProductInfo productInfo)
        {
            if (productInfo == null) return;
            Task.Factory.StartNew(() =>
            {
                AddBehavoirData(productInfo.Id.ToString(), productInfo.Type.ToString(), "click");
            });
        }

        public void AddBehavoirData(string thingId, string category,string action)
        {
            //todo:william.
            //if (string.IsNullOrEmpty(thingId)) return;
            //SqlLiteBehaviorRecord record = new SqlLiteBehaviorRecord();
            //record.Action = action;
            //record.CollectTime = DateTime.Now;
            //record.Increment = 1;
            //record.ThingId = thingId;
            //record.Category = category;
            //record.IsSynced = false;
            //m_db.Insert(record);

            //var records = m_db.Table<SqlLiteBehaviorRecord>().Where(r => r.IsSynced == false).Take(10).ToList();
            //if (records.Count() > 0)
            //{
            //    bool success = sesingWebClient.PostBehaviorRecordsAsync(records).GetAwaiter().GetResult();
            //    if (success)
            //    {
            //        foreach (var r in records)
            //        {
            //            r.IsSynced = true;
            //        }
            //        m_db.UpdateAll(records);
            //    }
            //}
        }
    }

    public  class SqlLiteBehaviorRecord : BehaviorRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public bool IsSynced { get; set; }
    }
}
