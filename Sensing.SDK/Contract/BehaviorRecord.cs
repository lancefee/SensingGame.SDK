﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sensing.SDK.Contract
{
    public class BehaviorRecord
    {
        /// <summary>
        /// 实务的唯一编码
        /// </summary>
        public string ThingId { get; set; }

        /// <summary>
        /// 当前行为的类别，如Ads,Products,Apps等.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 一段时间内的增量
        /// </summary>
        public int Increment { get; set; }

        /// <summary>
        /// 关注特定SKU的行为,如Click,Like,Take,See,Buy,ScanQrCode...
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 这个时间是设备传过来，最终的效果是 2016/09/19 10：00：05 这个时间点的增量，比如5分钟单位集合
        /// </summary>
        public DateTime CollectTime { get; set; }

        /// <summary>
        /// 来自于那个软件
        /// </summary>
        public string Software { get; set; }
    }
}
