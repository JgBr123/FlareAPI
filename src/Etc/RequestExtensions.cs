using System.Net;
using System.Runtime.CompilerServices;

namespace FlareAPI
{
    public static class RequestExtensions
    {
        //Weak tables
        private static readonly ConditionalWeakTable<HttpListenerRequest, Dictionary<string, string>> parametersTable = [];

        //Normal functions

        /// <summary>
        /// Writes the entire body of this request to a file.
        /// </summary>
        /// <param name="file">The file that will be created.</param>
        /// <param name="bufferSize">The size of the buffer used for reading the request body, in bytes.</param>
        public static void WriteBodyAsFile(this HttpListenerRequest request, string file, int bufferSize = 128 * 1024)
        {
            using (FileStream fs = File.OpenWrite(file))
            {
                int bytesRead;
                byte[] buffer = new byte[bufferSize]; //Sets buffer size
                while ((bytesRead = request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, bytesRead);
                }
            }
        }

        /// <summary>
        /// Verifies if the client accepts a specific encoding.
        /// </summary>
        /// <param name="contentEncoding">The encoding that will be verified.</param>
        /// <returns><see langword="true"/> if the client supports the encoding, otherwise <see langword="false"/>.</returns>
        public static bool AcceptsEncoding(this HttpListenerRequest request, ContentEncoding contentEncoding)
        {
            var encodingHeader = request.Headers["Accept-Encoding"];
            if (encodingHeader != null)
            {
                var acceptedEncodings = encodingHeader.Replace(" ", "").Split(",");
                if (contentEncoding == ContentEncoding.Gzip && acceptedEncodings.Contains("gzip")) return true;
                else if (contentEncoding == ContentEncoding.Brotli && acceptedEncodings.Contains("br")) return true;
                else if (contentEncoding == ContentEncoding.Deflate && acceptedEncodings.Contains("deflate")) return true;
                else return false;
            }
            else return false;
        }

        //Set functions
        internal static void SetParameters(this HttpListenerRequest request, Dictionary<string, string> parameters)
        {
            parametersTable.AddOrUpdate(request, parameters);
        }

        //Get functions

        /// <summary>
        /// Gets the parameters used in the request.
        /// </summary>
        /// <returns>A <see cref="Dictionary{string, string}"/> containing the parameters names and values.</returns>
        public static Dictionary<string,string> GetParameters(this HttpListenerRequest request)
        {
            Dictionary<string,string>? parameters;
            if (parametersTable.TryGetValue(request, out parameters)) return parameters;
            else return [];
        }
    }
}
