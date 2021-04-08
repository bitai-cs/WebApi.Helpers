using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Bitai.WebApi.Client
{
    public static class WebApiClientParameters
    {
        static public int ClientRequestTimeOut { get; set; }

        static public long MaxResponseContentBufferSize { get; set; }

        static public JsonSerializerOptions SerializerOptions { get; set; }


        /// <summary>
        /// Constructor Static que inicializa los parametros con valores por defecto
        /// </summary>
        static WebApiClientParameters()
        {
            ClientRequestTimeOut = 3 * 60;
            MaxResponseContentBufferSize = 1 * 1024 * 1024;

            SerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }
    }
}
