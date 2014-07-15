﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aardvark.Base
{

    public static class Mapping
    {

        /// <summary>
        /// [-inf, +inf] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> Trunc =
            x => x < 0 ? 0 : (x > 1 ? 1 : x);

        /// <summary>
        /// [-inf, +inf] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> Repeat =
            x => x - System.Math.Floor(x);

        /// <summary>
        /// [-inf, +inf] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> RepeatAndMirror = x =>
        {
            x = x - System.Math.Floor(x * 0.5) * 2;
            return x < 1 ? x : 2 - x;
        };

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> Linear = x => x;

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> Cosine =
            x => (1 - (x * Constant.Pi).Cos()) * 0.5;

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> EaseIn = x => x.Pow(1.3);

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> EaseOut = x => 1 - EaseIn(1 - x);

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> EaseInEaseOut = Cosine;

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> Min = x => 0;

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> Max = x => 1;

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> JumpStart = x => x > 0 ? 1 : 0;

        /// <summary>
        /// [0, 1] => [0, 1]
        /// </summary>
        public static readonly Func<double, double> JumpStop = x => x < 1 ? 0 : 1;

    }

}
