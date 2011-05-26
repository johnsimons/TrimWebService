using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using log4net;

namespace TrimWeb
{
	internal class TrimWebService : IDisposable
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof (TrimWebService));
		private static readonly Regex FilesMatcher = new Regex(@"^/files/(?<record>.*)$");
		private const string LastUpdateFormat = "yyyyMMddHHmm";

		private readonly string hostname;
		private readonly string port;
		private readonly string trimServer;
	    private readonly string trimDatasetId;

	    private HttpListener listener;

		public TrimWebService(string hostname, string port, string trimServer, string trimDatasetId)
		{
			this.hostname = hostname;
			this.port = port;
			this.trimServer = trimServer;
		    this.trimDatasetId = trimDatasetId;
		}

		public void Dispose()
		{
			if (listener != null && listener.IsListening)
			{
                Logger.Info("Service is stopping listening for requests.");
				listener.Stop();
                Logger.Info("Service has stopped listening for requests.");
			}
		}

		public void Start()
		{
            if (!HttpListener.IsSupported)
            {
                Logger.Fatal("HttpListener is not supported on this OS!");
                throw new NotSupportedException();
            }

            string listeningUrl = String.Format("http://{0}:{1}/", String.IsNullOrWhiteSpace(hostname) ? "+" : hostname,
                                                String.IsNullOrWhiteSpace(port) ? "8080" : port);
			listener = new HttpListener();
            listener.Prefixes.Add(listeningUrl);
			listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

			listener.Start();
            Logger.InfoFormat("Service started listening for requests on {0}.", listeningUrl);
			listener.BeginGetContext(GetContext, null);
		}

        private void GetContext(IAsyncResult ar)
        {
            HttpListenerContext context;

            try
            {
                context = listener.EndGetContext(ar);
                listener.BeginGetContext(GetContext, null);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }

            if (context.Request.HttpMethod != "GET")
            {
                return;
            }

            try
            {
                HandleActualRequest(context);
                context.Response.Close();
            }
            catch (HttpListenerException ex)
            {
                Logger.Warn("Connection aborted by client.", ex);
                context.Response.Abort();
                return;
            }
            catch (COMException ex)
            {
                ReportErrorAndCloseResponse(context, ex);
            }
            catch (Exception ex)
            {
                ReportErrorAndCloseResponse(context, ex);
            }
        }

	    private static void ReportErrorAndCloseResponse(HttpListenerContext context, Exception ex)
	    {
	        Logger.Error("An error occurred on the server.", ex);
	        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
	        context.Response.StatusDescription = "Internal server error";
            context.Response.Close();
	    }

	    private void HandleActualRequest(HttpListenerContext context)
		{
			string matchUrl = context.Request.Url.AbsolutePath.Trim().ToLower();

			Match match = FilesMatcher.Match(matchUrl);

			if (match.Success)
			{
				string recordNumber = match.Groups["record"].Value;
				string filePath = null;

				try
				{
                    using (var trimService = new TrimAttachmentRetriever(trimServer, trimDatasetId))
                    {
                        filePath = trimService.GetTrimAttachmentPath(long.Parse(recordNumber));
                        if (filePath == null)
                        {
                            context.SetStatusToNotFound();
                            return;
                        }
                        context.WriteEmbeddedFile(filePath, trimService.FileName, trimService.MimeType);
                    }
				}
				finally
				{
					if (filePath != null)
					{
						File.Delete(filePath);
					}
				}
			}
			else
			{
				string lastUpdateString = context.Request.QueryString.Get("from");
				DateTime lastUpdated;
				if (!DateTime.TryParseExact(lastUpdateString, LastUpdateFormat, CultureInfo.CurrentCulture,
									   DateTimeStyles.AssumeLocal,
									   out lastUpdated))
				{
					lastUpdated = DateTime.MinValue;
				}

                using (var trimService = new TrimRecordsRetriever(trimServer, trimDatasetId))
                {
                    var records = trimService.RetrieveRecords(lastUpdated).ToList();
                    context.Write(JsonConvert.SerializeObject(new
                                                                {
                                                                    Records = records,
                                                                    FromDate = trimService.LastRecordUpdatedDate == null ? null : trimService.LastRecordUpdatedDate.Value.ToString(LastUpdateFormat)
                                                                }));
                }
			    context.Response.ContentType = "application/json";
			}

            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";
		}
    }
}