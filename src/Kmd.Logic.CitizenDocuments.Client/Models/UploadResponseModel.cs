using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Kmd.Logic.CitizenDocuments.Client.Models
{
    public class UploadResponseModel
    { /// <summary>
        /// Gets or sets value must be 200 for OK.
      /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        public string Message { get; set; }
    }
}
