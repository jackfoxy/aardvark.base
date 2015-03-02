﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aardvark.Base
{

    // Details taken from:
    // http://msdn.microsoft.com/en-us/library/windows/desktop/bb172415(v=vs.85).aspx

    public enum WrapMode
    { 
        Wrap        = 1,
        Mirror      = 2,
        Clamp       = 3,
        Border      = 4,
        MirrorOnce  = 5
    }

    public enum TextureFilterMode
    { 
        None            = 0x0000,
        Point           = 0x0001,
        Linear          = 0x0002
    }

    public enum SamplerComparisonFunction
    { 
        Never           = 1,
        Less            = 2,
        Equal           = 3,
        LessOrEqual     = 4,
        Greater         = 5,
        NotEqual        = 6,
        GreaterOrEqual  = 7,
        Always          = 8
    }

    public struct TextureFilter
    {
        public readonly TextureFilterMode Min;
        public readonly TextureFilterMode Mag;
        public readonly TextureFilterMode Mip;
        public readonly bool IsAnisotropic;

        public TextureFilter(TextureFilterMode min, TextureFilterMode mag, TextureFilterMode mip, bool anisotropic = false)
        {
            Min = min; Mag = mag; Mip = mip; IsAnisotropic = anisotropic;
        }


        public static readonly TextureFilter MinMagPoint =
            new TextureFilter(TextureFilterMode.Point, TextureFilterMode.Point, TextureFilterMode.None);

        public static readonly TextureFilter MinPointMagLinear =
            new TextureFilter(TextureFilterMode.Point, TextureFilterMode.Linear, TextureFilterMode.None);

        public static readonly TextureFilter MinLinearMagPoint =
            new TextureFilter(TextureFilterMode.Linear, TextureFilterMode.Point, TextureFilterMode.None);

        public static readonly TextureFilter MinMagLinear =
            new TextureFilter(TextureFilterMode.Linear, TextureFilterMode.Linear, TextureFilterMode.None);




        public static readonly TextureFilter MinMagMipPoint = 
            new TextureFilter(TextureFilterMode.Point, TextureFilterMode.Point, TextureFilterMode.Point);

        public static readonly TextureFilter MinMagPointMipLinear = 
            new TextureFilter(TextureFilterMode.Point, TextureFilterMode.Point, TextureFilterMode.Linear);

        public static readonly TextureFilter MinPointMagLinearMipPoint =
            new TextureFilter(TextureFilterMode.Point, TextureFilterMode.Linear, TextureFilterMode.Point);

        public static readonly TextureFilter MinPointMagMipLinear =
            new TextureFilter(TextureFilterMode.Point, TextureFilterMode.Linear, TextureFilterMode.Linear);

        public static readonly TextureFilter MinLinearMagMipPoint =
            new TextureFilter(TextureFilterMode.Linear, TextureFilterMode.Point, TextureFilterMode.Point);

        public static readonly TextureFilter MinLinearMagPointMipLinear =
            new TextureFilter(TextureFilterMode.Linear, TextureFilterMode.Point, TextureFilterMode.Linear);

        public static readonly TextureFilter MinMagLinearMipPoint =
            new TextureFilter(TextureFilterMode.Linear, TextureFilterMode.Linear, TextureFilterMode.Point);

        public static readonly TextureFilter MinMagMipLinear =
            new TextureFilter(TextureFilterMode.Linear, TextureFilterMode.Linear, TextureFilterMode.Linear);

        public static TextureFilter Anisotropic =
            new TextureFilter(TextureFilterMode.Linear, TextureFilterMode.Linear, TextureFilterMode.Linear, true);
    }

    public class SamplerStateDescription
    {
        public TextureFilter Filter;
        public WrapMode AddressU;
        public WrapMode AddressV;
        public WrapMode AddressW;

        public int MaxAnisotropy;
        public float MipLodBias;
        public float MinLod;
        public float MaxLod;

        public SamplerComparisonFunction ComparisonFunction;
        public C4f BorderColor;


        public SamplerStateDescription()
        {
            Filter = TextureFilter.MinMagMipPoint;
            AddressU = WrapMode.Clamp;
            AddressV = WrapMode.Clamp;
            AddressV = WrapMode.Clamp;

            MaxAnisotropy = 16;
            MinLod = 0.0f;
            MaxLod = float.MaxValue;
            MipLodBias = 0.0f;

            ComparisonFunction = SamplerComparisonFunction.Never;
            BorderColor = C4f.Black;
        }

    }
}
