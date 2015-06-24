using SimpleHttpServer.Common;

namespace SimpleHttpServer.HttpServerEngine
{
    public enum RequestStatus
    {
        [GEI("200 Ok")]
        Ok = 200,
        [GEI("201 Created")]
        Created = 201,
        [GEI("202 Accepted")]
        Accepted = 202,
        [GEI("204 No Content")]
        No_Content = 204,

        [GEI("301 Moved Permanently")]
        Moved_Permanently = 301,
        [GEI("302 Redirection")]
        Redirection = 302,
        [GEI("304 Not Modified")]
        Not_Modified = 304,

        [GEI("400 Bad Request")]
        Bad_Request = 400,
        [GEI("401 Unauthorized")]
        Unauthorized = 401,
        [GEI("403 Forbidden")]
        Forbidden = 403,
        [GEI("404 Not Found")]
        Not_Found = 404,

        [GEI("500 Internal Server Error")]
        Internal_Server_Error = 500,
        [GEI("501 Not Implemented")]
        Not_Implemented = 501,
        [GEI("502 Bad Gateway")]
        Bad_Gateway = 502,
        [GEI("503 Service Unavailable")]
        Service_Unavailable = 503,
    }
}
