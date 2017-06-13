using ServiceFabric.Utils.Logging;
using ServiceFabric.Utils.Shared;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;

namespace ServiceFabric.Utils.DelegatingHandlers
{
    /// <summary>
    /// Used to catch HttpErrors and log using <see cref="IErrorHandler"/>
    /// </summary>
    public class ApiHttpDelegatingHandler : DelegatingHandler
    {
        private readonly IErrorHandler _errorHandler;

        /// <summary>
        /// Creates a new instance of <see cref="ApiHttpDelegatingHandler"/>
        /// </summary>
        /// <param name="errorHandler"></param>
        public ApiHttpDelegatingHandler(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }

        /// <summary>
        /// Sends the request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
           CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            var context = request.GetOwinContext();

            return ResponseMessageHandler != null
                ? ResponseMessageHandler.Invoke(context, response)
                : await DefaultResponseMessageHandler(context, response);
        }

        /// <summary>
        /// if <see cref="ResponseMessageHandler"/> is not set explicitly this will be called instead.
        /// </summary>
        /// <param name="context"><see cref="IOwinContext"/> used to for logging</param>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to return once the message has been handled</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> DefaultResponseMessageHandler(IOwinContext context, HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.InternalServerError)
                return response;

            HttpError httpError;

            if (!response.TryGetContentValue(out httpError))
                return response;

            Guid errorId;

            switch (response.StatusCode)
            {
                default:
                    errorId = await _errorHandler.LogErrorAsync(context, response.StatusCode, httpError);
                    return new ApiHttpResponseMessage(
                        response.StatusCode,
                        httpError.Message,
                        errorId == Guid.Empty ? null : errorId.ToString());
            }
        }

        public event Func<IOwinContext, HttpResponseMessage, HttpResponseMessage> ResponseMessageHandler;
    }
    
    
}