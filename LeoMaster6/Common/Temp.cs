using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeoMaster6.Common
{
    public class Temp
    {
        
    }


    /*
     
     ## todo

     - extract to a file based storage system
       - config file use json format
       - data file in CSV or json ?
       - use task queue to write to file to avoid frequently open/close file
       - full features
         - default config file
         - username & password supported (by config file)
         - file size limit supported
         - relative path, root data folder config supported
         - logging supported
           - config log file path
           - user log - audit user actions
           - system log
         - CRUD
         - email notification when error or warning (configurable)
       - core version (small and agile)
         - CRUD
         - password checking ??
         - no logging ??
           - only log to console?
     
     
     
     
     
     */
}