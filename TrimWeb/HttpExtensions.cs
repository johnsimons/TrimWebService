using System.IO;
using System.Net;
using System.Text;

namespace TrimWeb
{
	internal static class HttpExtensions
	{
		public static void Write(this HttpListenerContext context, string str)
		{
		    byte[] buffer = Encoding.ASCII.GetBytes(str);
		    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
		}

		public static void SetStatusToNotFound(this HttpListenerContext context)
		{
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
			context.Response.StatusDescription = "Not Found";
		}

        public static void WriteEmbeddedFile(this HttpListenerContext context, string filePath, string fileName, string mimeType)
		{
            context.Response.ContentType = mimeType;
			context.Response.AddHeader("content-disposition", "attachment; filename=" + fileName);

            byte[] buffer = File.ReadAllBytes(filePath);
			
			context.Response.OutputStream.Write(buffer, 0, buffer.Length);
		}
	}
}