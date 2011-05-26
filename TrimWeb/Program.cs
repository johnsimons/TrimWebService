using System.Configuration;
using log4net.Config;
using Topshelf;

namespace TrimWeb
{
	class Program
	{
		private static void Main()
		{
			string hostname = ConfigurationManager.AppSettings["Hostname"];
			string port = ConfigurationManager.AppSettings["Port"];
			string trimServer = ConfigurationManager.AppSettings["TrimServer"];
            string trimDatasetId = ConfigurationManager.AppSettings["TrimDatasetId"];

			XmlConfigurator.Configure();

			HostFactory.Run(x =>
			                	{
			                		x.Service<TrimWebService>(s =>
			                		                          	{
																	s.ConstructUsing(name => new TrimWebService(hostname, port, trimServer, trimDatasetId));
			                		                          		s.WhenStarted(tc => tc.Start());
			                		                          		s.WhenStopped(tc => tc.Dispose());
			                		                          	});
			                		x.StartAutomatically();
									x.SetServiceName("TrimWebService");
			                		x.SetDescription("Trim Web Service");
									x.SetDisplayName("Trim Web Service");
			                	});
		}
	}
}