using System;
using System.Collections.Generic;
using System.Text;
    
namespace ImportData
{
    /// <summary>
    /// Pick server(s) for data placement.
    /// TODO:  Hard coded; should read from a configuration file.
    /// </summary>
    class ServerSelection
    {
        string[] servers = { "dlmsdb001", "dlmsdb002", "dlmsdb003", "dlmsdb004" };
        int blocks = 4096;

        public ServerSelection()
        {

        }
    }
}
