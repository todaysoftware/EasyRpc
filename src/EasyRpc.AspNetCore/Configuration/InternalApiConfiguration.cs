﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EasyRpc.Abstractions.Path;
using EasyRpc.Abstractions.Services;
using EasyRpc.AspNetCore.Authorization;
using EasyRpc.AspNetCore.Configuration.DelegateConfiguration;
using EasyRpc.AspNetCore.Data;
using EasyRpc.AspNetCore.EndPoints;
using EasyRpc.AspNetCore.Filters;
using EasyRpc.AspNetCore.ResponseHeader;
using EasyRpc.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.Extensions.DependencyInjection;

namespace EasyRpc.AspNetCore.Configuration
{
    /// <summary>
    /// Internal implementation for api configuration
    /// </summary>
    public partial class InternalApiConfiguration : IInternalApiConfiguration
    {
        private ImmutableLinkedList<Func<IEndPointMethodConfigurationReadOnly, IEnumerable<IEndPointMethodAuthorization>>> _authorizations =
            ImmutableLinkedList<Func<IEndPointMethodConfigurationReadOnly, IEnumerable<IEndPointMethodAuthorization>>>.Empty;
        private ImmutableLinkedList<Func<IEndPointMethodConfigurationReadOnly, Func<RequestExecutionContext, IRequestFilter>>> _filters =
            ImmutableLinkedList<Func<IEndPointMethodConfigurationReadOnly, Func<RequestExecutionContext, IRequestFilter>>>.Empty;
        private ImmutableLinkedList<Func<MethodInfo, bool>> _methodFilters = ImmutableLinkedList<Func<MethodInfo, bool>>.Empty;
        private ImmutableLinkedList<Func<Type, IEnumerable<string>>> _prefixes = ImmutableLinkedList<Func<Type, IEnumerable<string>>>.Empty;
        private ImmutableLinkedList<IResponseHeader> _responseHeaders = ImmutableLinkedList<IResponseHeader>.Empty;

        private readonly IConfigurationManager _configurationMethodRepository;

        private ICurrentApiInformation _currentApiInformation;
        private ExposeDefaultMethod _defaultMethod = ExposeDefaultMethod.PostOnly;

        private readonly IApplicationConfigurationService _applicationConfigurationService;
        private readonly IAuthorizationImplementationProvider _authorizationImplementationProvider;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="applicationServiceProvider"></param>
        /// <param name="authorizationImplementationProvider"></param>
        public InternalApiConfiguration(IServiceProvider applicationServiceProvider, 
            IAuthorizationImplementationProvider authorizationImplementationProvider)
        {
            _applicationConfigurationService = applicationServiceProvider.GetRequiredService<IApplicationConfigurationService>();
            Configure = applicationServiceProvider.GetRequiredService<IEnvironmentConfiguration>();
            AppServices = applicationServiceProvider;
            _authorizationImplementationProvider = authorizationImplementationProvider;
            _configurationMethodRepository =
                applicationServiceProvider.GetRequiredService<IConfigurationManager>();
        }

        /// <inheritdoc />
        public IApiConfiguration Authorize(string role = null, string policy = null)
        {
            IEndPointMethodAuthorization authorization;

            if (!string.IsNullOrEmpty(role))
            {
                authorization = _authorizationImplementationProvider.UserHasRole(role);
            }
            else if (!string.IsNullOrEmpty(policy))
            {
                authorization = _authorizationImplementationProvider.UserHasPolicy(policy);
            }
            else
            {
                authorization = _authorizationImplementationProvider.Authorized();
            }

            return Authorize(endPoint => new[] {authorization});
        }

        /// <inheritdoc />
        public IApiConfiguration Authorize(Func<IEndPointMethodConfigurationReadOnly, IEnumerable<IEndPointMethodAuthorization>> authorizations)
        {
            _authorizations = _authorizations.Add(authorizations);

            return this;
        }

        /// <inheritdoc />
        public IApiConfiguration ClearAuthorize()
        {
            _authorizations = ImmutableLinkedList<Func<IEndPointMethodConfigurationReadOnly, IEnumerable<IEndPointMethodAuthorization>>>.Empty;

            ClearCurrentApi();

            return this;
        }

        /// <inheritdoc />
        public IEnvironmentConfiguration Configure { get; }

        /// <inheritdoc />
        public IApiConfiguration Prefix(string prefix)
        {
            var prefixArray = new [] {prefix};

            return Prefix(type => prefixArray);
        }

        /// <inheritdoc />
        public IApiConfiguration Prefix(Func<Type, IEnumerable<string>> prefixFunc)
        {
            _prefixes = _prefixes.Add(prefixFunc);

            return this;
        }

        /// <inheritdoc />
        public IApiConfiguration ClearPrefixes()
        {
            _prefixes = ImmutableLinkedList<Func<Type, IEnumerable<string>>>.Empty;

            ClearCurrentApi();

            return this;
        }

        /// <inheritdoc />
        public IExposureConfiguration Expose(Type type)
        {
            var config = new TypeExposureConfiguration(GetCurrentApiInformation(), type);

            _applicationConfigurationService.AddConfigurationObject(config);

            return config;
        }

        /// <inheritdoc />
        public IExposureConfiguration<T> Expose<T>()
        {
            var config = new TypeExposureConfiguration<T>(GetCurrentApiInformation());

            _applicationConfigurationService.AddConfigurationObject(config);

            return config;
        }

        /// <inheritdoc />
        public ITypeSetExposureConfiguration Expose(IEnumerable<Type> types)
        {
            var config = new TypeSetExposureConfiguration(GetCurrentApiInformation(), types);

            _applicationConfigurationService.AddConfigurationObject(config);

            return config;
        }

        /// <inheritdoc />
        public IApiConfiguration Header(string header, string value)
        {
            _responseHeaders = _responseHeaders.Add(new ResponseHeader.ResponseHeader(header, value));

            return this;
        }

        /// <inheritdoc />
        public IApiConfiguration ClearHeaders()
        {
            _responseHeaders = ImmutableLinkedList<IResponseHeader>.Empty;
            
            return this;
        }

        /// <inheritdoc />
        public IApiConfiguration ApplyFilter<T>(Func<MethodInfo, bool> where = null, bool shared = false) where T : IRequestFilter
        {
            if (where == null)
            {
                where = methodInfo => true;
            }

            return ApplyFilter(methodInfo =>
                {
                    if (where(methodInfo.InvokeInformation.Signature))
                    {
                        return context => ActivatorUtilities.CreateInstance<T>(context.HttpContext.RequestServices);
                    }

                    return null;
                });
        }

        /// <inheritdoc />
        public IApiConfiguration ApplyFilter(Func<IEndPointMethodConfigurationReadOnly, Func<RequestExecutionContext, IRequestFilter>> filterFunc)
        {
            _filters = _filters.Add(filterFunc);

            return this;
        }

        /// <inheritdoc />
        public IApiConfiguration MethodFilter(Func<MethodInfo, bool> methodFilter)
        {
            _methodFilters = _methodFilters.Add(methodFilter);

            return this;
        }

        /// <inheritdoc />
        public IApiConfiguration ClearMethodFilters()
        {
            _methodFilters = ImmutableLinkedList<Func<MethodInfo, bool>>.Empty;

            return this;
        }

        /// <inheritdoc />
        public IServiceProvider AppServices { get; }

        /// <inheritdoc />
        public IApiConfiguration DefaultHttpMethod(ExposeDefaultMethod defaultMethod)
        {
            _defaultMethod = defaultMethod;

            return this;
        }

        /// <inheritdoc />
        public ICurrentApiInformation GetCurrentApiInformation()
        {
            if (_currentApiInformation != null)
            {
                return _currentApiInformation;
            }

            _currentApiInformation = new CurrentApiInformation(
                _authorizations, 
                _filters, 
                _prefixes, 
                _methodFilters, 
                false, 
                _defaultMethod, 
                ServiceActivationMethod.ActivationUtility, 
                AppServices, 
                _configurationMethodRepository,
                _responseHeaders);

            return _currentApiInformation;
        }

        /// <inheritdoc />
        public IReadOnlyList<IEndPointMethodHandler> GetEndPointHandlers()
        {
            return _applicationConfigurationService.ProvideEndPointHandlers();
        }

        /// <summary>
        /// Gets a static copy of the current api information
        /// </summary>
        protected void ClearCurrentApi()
        {
            _currentApiInformation = null;
        }
    }
}
