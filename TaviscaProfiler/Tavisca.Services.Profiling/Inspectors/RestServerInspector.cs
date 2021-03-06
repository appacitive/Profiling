﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.ServiceModel.Web;
using StackExchange.Profiling;

namespace Tavisca.Services.Profiling
{
    public class RestServerInspector : IDispatchMessageInspector
    {
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            bool shouldProfile = ShouldProfile(request);
            if (shouldProfile == false)
                return shouldProfile;
            var profiler = Profiler.Instance;
            profiler.Start(ProfilerEnvironment.Restfull);

            var transactionId = Guid.NewGuid().ToString();
            var debugEnabledBool = false;
            var webOperationContext = WebOperationContext.Current;
            if (webOperationContext != null)
            {
                if ((webOperationContext.IncomingRequest.UriTemplateMatch == null) == false)
                {
                    var tempTransactionId = webOperationContext.IncomingRequest.Headers.Get("X-TransactionId");
                    if (string.IsNullOrWhiteSpace(tempTransactionId))
                        tempTransactionId =
                            webOperationContext.IncomingRequest.UriTemplateMatch.QueryParameters.Get("transactionid");

                    if (string.IsNullOrWhiteSpace(tempTransactionId) == false)
                        transactionId = tempTransactionId;

                    var debugEnabled = webOperationContext.IncomingRequest.Headers.Get("X-Debug");
                    if (string.IsNullOrWhiteSpace(debugEnabled))
                        debugEnabled = webOperationContext.IncomingRequest.UriTemplateMatch.QueryParameters.Get("debug");

                    if (string.IsNullOrWhiteSpace(debugEnabled) == false)
                        bool.TryParse(debugEnabled, out debugEnabledBool);
                }
            }
            profiler.TransactionId = transactionId;
            profiler.Enabled = debugEnabledBool;
            return shouldProfile;
        }

        private bool ShouldProfile(Message request)
        {
            HttpRequestMessageProperty httpRequest = request.Properties["httpRequest"] as HttpRequestMessageProperty;
            if (httpRequest == null)
                return true;
            return string.Compare(httpRequest.Method, "OPTIONS", true) != 0;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            bool shouldProfile = correlationState == null ? true : (bool)correlationState;
            if (shouldProfile == false)
                return;
            var profiler = Profiler.Instance;
            
            bool fault = reply == null ? false : reply.IsFault;
            if (fault)
                profiler.AddMetaData("code", "fault");
            else
                profiler.AddMetaData("code", "success");
            profiler.Stop();
        }
    }
}
