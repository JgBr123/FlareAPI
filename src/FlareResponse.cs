using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FlareAPI
{
    public class FlareResponse
    {
        private readonly HttpListenerContext context;
        private readonly FlareServer server;
        private bool hasBody = false;

        private ContentEncoding[] contentEncoding = [];
        private CompressionLevel compressionLevel = CompressionLevel.Optimal;
        private bool forcedEncoding = false;

        //CONSTRUCTOR
        internal FlareResponse(HttpListenerContext context, FlareServer server)
        {
            this.context = context;
            this.server = server;

            context.Response.Headers["Server"] = server.name;

            foreach (string headerKey in server.globalHeaders.Keys) //Add global headers
            {
                context.Response.Headers.Add(headerKey, server.globalHeaders[headerKey]);
            }
            foreach (Cookie cookie in server.globalCookies) //Add global cookies 
            {
                context.Response.Cookies.Add(cookie);
            }
        }

        //STATUS CODE

        /// <summary>
        /// Sets the status code of the response.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <exception cref="UnmodifiableResponseException"></exception>
        public void StatusCode(int statusCode)
        {
            if (hasBody) throw new UnmodifiableResponseException();
            context.Response.StatusCode = statusCode;
        }

        /// <summary>
        /// Sets the status code of the response.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <exception cref="UnmodifiableResponseException"></exception>
        public void StatusCode(HttpStatusCode statusCode)
        {
            if (hasBody) throw new UnmodifiableResponseException();
            context.Response.StatusCode = (int)statusCode;
        }

        //HEADERS

        /// <summary>
        /// Adds a header to the response.
        /// </summary>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        /// <exception cref="UnmodifiableResponseException"></exception>
        public void AddHeader(string key, string? value = null)
        {
            if (hasBody) throw new UnmodifiableResponseException();
            context.Response.Headers.Add(key, value);
        }

        /// <summary>
        /// Adds a header to the response.
        /// </summary>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        /// <exception cref="UnmodifiableResponseException"></exception>
        public void AddHeader(HttpResponseHeader key, string? value = null)
        {
            if (hasBody) throw new UnmodifiableResponseException();
            context.Response.Headers.Add(key, value);
        }

        //COOKIES

        /// <summary>
        /// Adds a cookie to the response.
        /// </summary>
        /// <param name="cookie">The cookie.</param>
        /// <exception cref="UnmodifiableResponseException"></exception>
        public void AddCookie(Cookie cookie)
        {
            if (hasBody) throw new UnmodifiableResponseException();
            context.Response.Cookies.Add(cookie);
        }

        //TEXT

        /// <summary>
        /// Sends a text as the body of the response.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        public bool Text(string text)
        {
            return TextBasedBody(text, "text/plain");
        }

        //JSON

        /// <summary>
        /// Sends JSON as the body of the response.
        /// </summary>
        /// <param name="json">The JSON.</param>
        /// <returns><see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        public bool Json(string json)
        {
            return TextBasedBody(json, "application/json");
        }

        /// <summary>
        /// Sends JSON as the body of the response.
        /// </summary>
        /// <param name="obj">The JSON.</param>
        /// <returns><see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        public bool Json(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return TextBasedBody(json, "application/json");
        }

        //HTML

        /// <summary>
        /// Sends HTML as the body of the response.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns><see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        public bool Html(string html)
        {
            return TextBasedBody(html, "text/html");
        }

        //CSS

        /// <summary>
        /// Sends CSS as the body of the response.
        /// </summary>
        /// <param name="css">The CSS.</param>
        /// <returns><see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        public bool Css(string css)
        {
            return TextBasedBody(css, "text/css");
        }

        //CSV

        /// <summary>
        /// Sends CSV as the body of the response.
        /// </summary>
        /// <param name="csv">The CSV.</param>
        /// <returns><see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        public bool Csv(string csv)
        {
            return TextBasedBody(csv, "text/csv");
        }

        //JAVASCRIPT

        /// <summary>
        /// Sends JavaScript as the body of the response.
        /// </summary>
        /// <param name="javascript">The JavaScript.</param>
        /// <returns><see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        public bool JavaScript(string javascript)
        {
            return TextBasedBody(javascript, "application/javascript");
        }

        //FILE

        /// <summary>
        /// Sends a file as the body of the response.
        /// </summary>
        /// <param name="file">The file that will be sent.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="autoSetHeaders">If the response headers should be automatically setted.</param>
        /// <param name="bufferSize">The size of the buffer used for reading the file, in bytes.</param>
        /// <param name="range">The range of the file, in bytes, following the HTTP standard.</param>
        /// <returns><see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        /// <exception cref="InvalidResponseBodyException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidContentRangeException"></exception>
        public bool File(string file, string contentType = "application/octet-stream", bool autoSetHeaders = true, int bufferSize = 128 * 1024, string? range = null)
        {
            if (hasBody) throw new InvalidResponseBodyException();
            else hasBody = true;

            if (!System.IO.File.Exists(file)) throw new FileNotFoundException(file); //Throws exception if file doesnt exist

            var bodyWriter = CreateResponseBodyWriter(context, this, isText: false);

            using (FileStream fs = System.IO.File.OpenRead(file))
            {
                (long? startingRange, long? endingRange) pr = (null, null);

                if (range != null)
                {
                    if (Utils.IsValidRange(range, fs.Length)) pr = Utils.ParseRange(range);
                    else throw new InvalidContentRangeException();
                }

                long bytesLeftToRead;
                if (range != null) bytesLeftToRead = Utils.CalculateRangeLength(range, fs.Length);
                else bytesLeftToRead = fs.Length;

                if (!bodyWriter.isEncoded) context.Response.ContentLength64 = bytesLeftToRead; //Only uses content length header if content isnt encoded
                context.Response.ContentType = contentType;

                if (pr.startingRange != null) //If has starting range
                {
                    fs.Seek(pr.startingRange.Value, SeekOrigin.Begin);
                    if (autoSetHeaders)
                    {
                        AddHeader("Content-Range", $"bytes {pr.startingRange}-{pr.endingRange ?? fs.Length - 1}/{fs.Length}"); //Sends header with range information
                        StatusCode(206); //Sends status code 206 (Partial Content)
                    }
                }

                try
                {
                    int bytesRead;
                    byte[] buffer = new byte[bufferSize]; //Sets buffer size
                    while ((bytesRead = fs.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesLeftToRead))) > 0) //Sends the file in small parts with the buffer size (and respects the range if its setted)
                    {
                        bodyWriter.OutputStream.Write(buffer, 0, bytesRead);
                        bodyWriter.OutputStream.Flush();
                        bytesLeftToRead -= bytesRead;
                        if (bytesLeftToRead == 0) break;
                    }
                    bodyWriter.OutputStream.Close();
                }
                catch (HttpListenerException) { return false; } //Returns false if upload failed
                return true; //Returns true if successful
            }
        }

        //ENCODING

        /// <summary>
        /// Sets the encoding that will be used in the body of the response. When sending the response, the body will be compressed with the specified algorithm.
        /// </summary>
        /// <param name="contentEncoding">An array of encoding types, in order of priority.</param>
        /// <param name="compressionLevel">The compression level used in the encoding.</param>
        /// <param name="forcedEncoding">Indicates whether the content should be forcefully encoded, ignoring the client's accepted encodings.</param>
        /// <exception cref="UnmodifiableResponseException"></exception>
        public void SetEncoding(ContentEncoding[] contentEncoding, CompressionLevel compressionLevel = CompressionLevel.Optimal, bool forcedEncoding = false)
        {
            if (hasBody) throw new UnmodifiableResponseException();
            this.contentEncoding = contentEncoding;
            this.compressionLevel = compressionLevel;
            this.forcedEncoding = forcedEncoding;
        }

        //STREAM

        /// <summary>
        /// Gets the stream used to write to the body of the response.
        /// </summary>
        /// <returns>The <see cref="Stream"/>.</returns>
        /// <exception cref="InvalidResponseBodyException"></exception>
        public Stream GetOutputStream()
        {
            if (hasBody) throw new InvalidResponseBodyException();
            else hasBody = true;
            return context.Response.OutputStream;
        }

        //ABORT

        /// <summary>
        /// Closes the connection to the client without sending a response.
        /// </summary>
        public void Abort()
        {
            context.Response.Abort();
        }

        //SEND

        /// <summary>
        /// Sends the response to the client.
        /// </summary>
        /// <returns></returns>
        public bool Send()
        {
            try
            {
                context.Response.Close();
                return true;
            }
            catch (HttpListenerException) { return false; }
        }

        //
        // ASYNC FUNCTIONS
        //

        /// <summary>
        /// Sends a file as the body of the response.
        /// </summary>
        /// <param name="file">The file that will be sent.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="autoSetHeaders">If the response headers should be automatically setted.</param>
        /// <param name="bufferSize">The size of the buffer used for reading the file, in bytes.</param>
        /// <param name="range">The range of the file, in bytes, following the HTTP standard.</param>
        /// <returns>The task object representing the asynchronous operation. The <see cref="Task{bool}"/>.Result property on the task object returns <see langword="true"/> if the operation is successful, otherwise <see langword="false"/>.</returns>
        /// <exception cref="InvalidResponseBodyException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidContentRangeException"></exception>
        public async Task<bool> FileAsync(string file, string contentType = "application/octet-stream", bool autoSetHeaders = true, int bufferSize = 128 * 1024, string? range = null)
        {
            return await Task.Run(() => File(file, contentType, autoSetHeaders, bufferSize, range));
        }

        //
        // PRIVATE FUNCTIONS
        //

        private bool TextBasedBody(string content, string contentType)
        {
            if (hasBody) throw new InvalidResponseBodyException();
            else hasBody = true;

            var buffer = Encoding.UTF8.GetBytes(content);
            var bodyWriter = CreateResponseBodyWriter(context, this, isText: true);

            try
            {
                if (!bodyWriter.isEncoded) context.Response.ContentLength64 = buffer.Length; //Only uses content length header if content isnt encoded
                context.Response.ContentType = contentType;
                bodyWriter.OutputStream.Write(buffer);
                bodyWriter.OutputStream.Close();
            }
            catch (HttpListenerException) { return false; } //Returns false if upload failed
            return true; //Returns true if successful
        }

        private (Stream OutputStream, bool isEncoded) CreateResponseBodyWriter(HttpListenerContext context, FlareResponse response, bool isText)
        {
            foreach (var encoding in response.contentEncoding) //Manual setted encoding
            {
                if (context.Request.AcceptsEncoding(encoding) || response.forcedEncoding)
                {
                    //Gzip
                    if (encoding == ContentEncoding.Gzip)
                    {
                        context.Response.AddHeader("Content-Encoding", "gzip");
                        return (new GZipStream(context.Response.OutputStream, response.compressionLevel), true);
                    }
                    //Brotli
                    else if (encoding == ContentEncoding.Brotli)
                    {
                        context.Response.AddHeader("Content-Encoding", "br");
                        return (new BrotliStream(context.Response.OutputStream, response.compressionLevel), true);
                    }
                    //Deflate
                    else if (encoding == ContentEncoding.Deflate)
                    {
                        context.Response.AddHeader("Content-Encoding", "deflate");
                        return (new DeflateStream(context.Response.OutputStream, response.compressionLevel), true);
                    }
                }
            }
            if (isText) //Global text encoding, if is a text and still has no encoding
            {
                foreach (var encoding in server.globalTextEncoding)
                {
                    if (context.Request.AcceptsEncoding(encoding))
                    {
                        //Gzip
                        if (encoding == ContentEncoding.Gzip)
                        {
                            context.Response.AddHeader("Content-Encoding", "gzip");
                            return (new GZipStream(context.Response.OutputStream, server.globalTextCompressionLevel), true);
                        }
                        //Brotli
                        else if (encoding == ContentEncoding.Brotli)
                        {
                            context.Response.AddHeader("Content-Encoding", "br");
                            return (new BrotliStream(context.Response.OutputStream, server.globalTextCompressionLevel), true);
                        }
                        //Deflate
                        else if (encoding == ContentEncoding.Deflate)
                        {
                            context.Response.AddHeader("Content-Encoding", "deflate");
                            return (new DeflateStream(context.Response.OutputStream, server.globalTextCompressionLevel), true);
                        }
                    }
                }
            }
            return (context.Response.OutputStream, false); //No encoding
        }
    }
}