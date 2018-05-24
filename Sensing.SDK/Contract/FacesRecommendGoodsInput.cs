﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensing.SDK.Contract
{
    public class FaceImage
    {
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public string Type { get; set; }
    }
    public class FacesRecommendsInput
    {
        public FaceImage[] Faces { get; set; }
        public string Subkey { get; set; }
    }

    public class Recommneds
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Reason { get; set; }
    }
    public class FaceRecommnedsOutput
    {
        public bool Recognized { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public bool IsMember { get; set; }
        public string MemberId { get; set; }
        public string MemberName { get; set; }
        public Recommneds[] Recommends { get; set; }
        public string Reason { get; set; }
    }
}