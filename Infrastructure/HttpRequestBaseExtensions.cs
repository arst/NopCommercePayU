using System.IO;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.PayU.Infrastructure
{
    public static class HttpRequestBaseExtensions
    {
        public static async Task<string> GetBody(this System.Web.HttpRequestBase request)
        {
            string documentContents;
            using (Stream receiveStream = request.InputStream)
            {
                receiveStream.Position = 0;
                using (StreamReader readStream = new StreamReader(receiveStream, request.ContentEncoding))
                {
                    documentContents = await readStream.ReadToEndAsync();
                }
            }

            return documentContents;
        }
    }
}
