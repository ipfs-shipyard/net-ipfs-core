﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Ipfs.Server
{
    /// <summary>
    ///   Simple controller to present the IPFS WebUI
    /// </summary>
    [Route("webui")]
    public class WebUIController : Controller
    {
        /// <summary>
        ///   Handles HTTP Get "/webui" 
        /// </summary>
        [HttpGet]
        public ActionResult Get()
        {
            return Redirect("/ipfs/QmfQkD8pBSBCBxWEwFSu4XaDVSWK6bjnNuaWZjMyQbyDub/index.html");
        }

    }
}
