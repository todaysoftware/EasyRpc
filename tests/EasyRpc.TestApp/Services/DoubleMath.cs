﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyRpc.TestApp.Services
{
    /// <summary>
    /// Services associated with doing double math
    /// </summary>
    public class DoubleMath
    {
        public double Add(double x, double y)
        {
            return x + y;
        }

        public double Subtract(double x, double y)
        {
            return x - y;
        }

    }
}
