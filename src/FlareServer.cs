using System.IO.Compression;
using System.Net;

namespace FlareAPI
{
    public class FlareServer
    {
        private HttpListener httpListener;
        private List<Route> routes = [];
        private Route? routeFallback;

        /// <summary>
        /// The version of FlareAPI.
        /// </summary>
        public const string version = "v1";

        /// <summary>
        /// The name of the API.
        /// </summary>
        public string name = "FlareAPI";

        /// <summary>
        /// The headers that will be sent with every response.
        /// </summary>
        public WebHeaderCollection globalHeaders = [];

        /// <summary>
        /// The cookies that will be sent with every response.
        /// </summary>
        public CookieCollection globalCookies = [];

        /// <summary>
        /// The encoding used in the body of every text response by default, in order of priority.
        /// </summary>
        public ContentEncoding[] globalTextEncoding = [];

        /// <summary>
        /// The compression level used in the encoding of every text response by default.
        /// </summary>
        public CompressionLevel globalTextCompressionLevel = CompressionLevel.Optimal;

        public delegate void RouteCallback(HttpListenerRequest request, FlareResponse response);

        //
        // PUBLIC FUNCTIONS
        //

        /// <summary>
        /// Creates a new FlareServer instance.
        /// </summary>
        /// <param name="uri">The URI that the server will listen to for requests.</param>
        public FlareServer(string uri)
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(uri);
        }

        /// <summary>
        /// Creates a new FlareServer instance.
        /// </summary>
        /// <param name="uris">An array of URIs that the server will listen to for requests.</param>
        public FlareServer(string[] uris)
        {
            httpListener = new HttpListener();
            foreach (var uri in uris) httpListener.Prefixes.Add(uri);
        }

        /// <summary>
        /// Starts listening for requests.
        /// </summary>
        public async void Start()
        {
            httpListener.Start();

            while (httpListener.IsListening)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
        }

        /// <summary>
        /// Stops listening for requests.
        /// </summary>
        public void Stop()
        {
            httpListener.Stop();
        }

        /// <summary>
        /// Shuts down the FlareServer and discards queued requests.
        /// </summary>
        public void Abort()
        {
            httpListener.Abort();
        }

        /// <summary>
        /// Closes and disposes the FlareServer instance.
        /// </summary>
        public void Close()
        {
            httpListener.Close();
        }

        /// <summary>
        /// Adds a route to the FlareServer where requests will be processed.
        /// </summary>
        /// <param name="httpMethod">The target HTTP request method of this route.</param>
        /// <param name="route">The URL path that this route will listen to for requests.</param>
        /// <param name="routeCallback">The callback function that will handle requests of this route.</param>
        /// <returns>The <see cref="Route"/> object that was added.</returns>
        public Route AddRoute(HttpMethod httpMethod, string route, RouteCallback routeCallback)
        {
            route = route.Replace("*", ""); //Remove asterisks from the route as they are used internally
            var routeObject = new Route(route, httpMethod, routeCallback);
            if (route.Contains(':')) //If the route has parameters to set
            {
                int index = 0;
                routeObject.route = "";
                routeObject.parametersCast = [];
                foreach (string segment in route.Trim('/').Split('/'))
                {
                    if (segment.StartsWith(":"))
                    {
                        var parameter = segment.Substring(1);
                        routeObject.parametersCast.Add(index, parameter);
                        routeObject.route += "/*";
                    }
                    else routeObject.route += $"/{segment}";
                    index++;
                }
            }
            routes.Add(routeObject);
            return routeObject;
        }

        /// <summary>
        /// Removes a route from the FlareServer.
        /// </summary>
        /// <param name="routeObject">The <see cref="Route"/> object that will be removed.</param>
        /// <returns><see langword="true"/> if the route is successfuly removed, otherwise <see langword="false"/>.</returns>
        public bool RemoveRoute(Route routeObject)
        {
            return routes.Remove(routeObject);
        }

        /// <summary>
        /// Adds a route to the FlareServer where requests that don't match any route will be processed.
        /// </summary>
        /// <param name="routeCallback">The callback function that will handle requests of this route.</param>
        public void SetRouteFallback(RouteCallback routeCallback)
        {
            var routeObject = new Route("*", HttpMethod.Get, routeCallback); //The * and the HttpMethod.Get are just placeholders, it will work for any route or httpmethod
            routeFallback = routeObject;
        }

        /// <summary>
        /// Goes back to the default behavior of returning an <see cref="HttpStatusCode.NotFound"/> to requests that don't match any route.
        /// </summary>
        public void RemoveRouteFallback()
        {
            routeFallback = null;
        }

        //
        // PRIVATE FUNCTIONS
        //

        private void HandleRequest(HttpListenerContext context)
        {
            //Tries to get the http method of the request
            HttpMethod httpMethod;
            try { httpMethod = HttpMethod.Parse(context.Request.HttpMethod); }
            catch
            {
                AnswerRequest(context, 400); //Malformed request
                return;
            }

            //Continues processing the request
            if (context.Request.Url != null)
            {
                var path = context.Request.Url.AbsolutePath;
                
                foreach (var routeObject in routes)
                {
                    if (routeObject.httpMethod == httpMethod && routeObject.MatchesRoute(path)) //Calls the route if it matches
                    {
                        if (routeObject.parametersCast != null) //This parses the parameters in a dictionary and adds it to the request
                        {
                            var segments = path.Trim('/').Split('/');
                            Dictionary<string, string> parameters = [];
                            foreach (var parameter in routeObject.parametersCast)
                            {
                                parameters.Add(parameter.Value, WebUtility.UrlDecode(segments[parameter.Key]));
                            }
                            context.Request.SetParameters(parameters);
                        }

                        var response = new FlareResponse(context, this);
                        routeObject.callback.Invoke(context.Request, response);
                        response.Send();
                        return;
                    }
                }

                //No routes found
                if (routeFallback != null) //Calls the fallback if its setted, or else just send a 404
                {
                    var response = new FlareResponse(context, this);
                    routeFallback.callback.Invoke(context.Request, response);
                    response.Send();
                }
                else AnswerRequest(context, 404); //Not found
            }
            else AnswerRequest(context, 400); //Malformed request
        }
        private void AnswerRequest(HttpListenerContext context, int statusCode)
        {
            context.Response.StatusCode = statusCode;
            context.Response.Close();
        }
    }
    public enum ContentEncoding
    {
        Gzip,
        Brotli,
        Deflate
    }
}
