using static FlareAPI.FlareServer;

namespace FlareAPI
{
    public class Route
    {
        internal string route;
        internal HttpMethod httpMethod;
        internal RouteCallback callback;
        internal Dictionary<int, string>? parametersCast;

        internal Route(string route, HttpMethod httpMethod, RouteCallback callback)
        {
            this.route = route;
            this.httpMethod = httpMethod;
            this.callback = callback;
        }

        internal bool MatchesRoute(string routeToCheck)
        {
            //Splits both the route and the routeTarget into segments
            var routeSegments = routeToCheck.Trim('/').Split('/');
            var targetSegments = route.Trim('/').Split('/');

            //Returns false if the number of segments dont match
            if (routeSegments.Length != targetSegments.Length) return false;

            for (int i = 0; i < targetSegments.Length; i++)
            {
                if (targetSegments[i] == "*" && !String.IsNullOrEmpty(routeSegments[i])) //If the target segment is a *, then any segment is allowed (except empty strings)
                    continue;

                if (targetSegments[i] != routeSegments[i]) //If not, then the segments must match
                    return false;
            }

            return true; //If all checks passed
        }
    }
}
