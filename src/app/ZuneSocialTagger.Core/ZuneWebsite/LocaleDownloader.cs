﻿using System;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace ZuneSocialTagger.Core.ZuneWebsite
{
    public enum MarketplaceStatus
    {
        Available,
        NotAvailable,
        Error
    }

    public class LocaleDownloader
    {
        public static void IsMarketPlaceEnabledForLocaleAsync(string locale, Action<MarketplaceStatus> callback)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(
                String.Format("http://tuners.zune.net/{0}/ZunePCClient/v4.7/configuration.xml", locale));

            httpWebRequest.BeginGetResponse(ReqCallback, new AsyncResult<MarketplaceStatus>(httpWebRequest, callback));
        }

        private static void ReqCallback(IAsyncResult asyncResult)
        {
            var result = asyncResult.AsyncState as AsyncResult<MarketplaceStatus>;
            try
            {
                HttpWebRequest httpWebRequest = result.HttpWebRequest;

                using (var httpWebResponse = (HttpWebResponse)httpWebRequest.EndGetResponse(asyncResult))
                {
                    XDocument document = XDocument.Load(XmlReader.Create(httpWebResponse.GetResponseStream()));

                    var isMarketPlaceEnabled = document
                        .Descendants().Where(x => x.Name.LocalName == "featureEnablement")
                        .Descendants().Where(x => x.Name.LocalName == "marketplace")
                        .Descendants().Where(x => x.Name.LocalName == "status")
                        .First().Value;

                    if (isMarketPlaceEnabled == "enabled")
                        result.Callback(MarketplaceStatus.Available);

                    if (isMarketPlaceEnabled == "disabled")
                        result.Callback(MarketplaceStatus.NotAvailable);
                }
            }
            catch (Exception)
            {
                result.Callback(MarketplaceStatus.Error);
            }
        }

        internal class AsyncResult<T>
        {
            public AsyncResult(HttpWebRequest httpWebRequest, Action<T> callback)
            {
                HttpWebRequest = httpWebRequest;
                Callback = callback;
            }

            public HttpWebRequest HttpWebRequest { get; set; }
            public Action<T> Callback { get; set; }
        }
    }

}
