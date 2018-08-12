﻿using System;

namespace EasyRpc.AspNetCore
{
    public interface IFactoryExposureConfiguration
    {
        IFactoryExposureConfiguration Methods(Action<IFactoryMethodConfiguration> method);
    }

    public interface IFactoryMethodConfiguration
    {
        IFactoryMethodConfiguration Func<TResult>(string methodName, Func<TResult> func);

        IFactoryMethodConfiguration Action(string methodName, Action method);

        IFactoryMethodConfiguration Func<TIn, TResult>(string methodName, Func<TIn, TResult> method);

        IFactoryMethodConfiguration Action<TIn>(string methodName, Action<TIn> method);

        IFactoryMethodConfiguration Func<TIn1, TIn2, TResult>(string methodName, Func<TIn1, TIn2, TResult> method);

        IFactoryMethodConfiguration Action<TIn1, TIn2>(string methodName, Action<TIn1, TIn2> method);

        IFactoryMethodConfiguration Func<TIn1, TIn2, TIn3, TResult>(string methodName, Func<TIn1, TIn2, TIn3, TResult> method);

        IFactoryMethodConfiguration Action<TIn1, TIn2, TIn3>(string methodName, Action<TIn1, TIn2, TIn3> method);

        IFactoryMethodConfiguration Func<TIn1, TIn2, TIn3,TIn4, TResult>(string methodName, Func<TIn1, TIn2, TIn3,TIn4, TResult> method);

        IFactoryMethodConfiguration Action<TIn1, TIn2, TIn3, TIn4>(string methodName, Action<TIn1, TIn2, TIn3, TIn4> method);
    }
}
