<%@ Application Language="C#" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e)
    {
        // Code that runs on application startup
        
        String _path = System.Environment.GetEnvironmentVariable("PATH");
        
//#if __WIN32
//        _path = String.Concat( "~/Bin32/", ";", System.Environment.GetEnvironmentVariable("PATH"), ";", System.AppDomain.CurrentDomain.RelativeSearchPath);
//#elif __X64
//        _path = String.Concat( "~/Bin64/", ";", System.Environment.GetEnvironmentVariable("PATH"), ";", System.AppDomain.CurrentDomain.RelativeSearchPath);
//#endif

        String proc_arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        if (proc_arch.CompareTo("x86") == 0)
        {
            _path = String.Concat(System.Environment.GetEnvironmentVariable("PATH"), ";", System.AppDomain.CurrentDomain.RelativeSearchPath, ";", 
                System.AppDomain.CurrentDomain.RelativeSearchPath + "\\Bin32");
        }
        else
        {
            _path = String.Concat(System.Environment.GetEnvironmentVariable("PATH"), ";", System.AppDomain.CurrentDomain.RelativeSearchPath, ";", 
                System.AppDomain.CurrentDomain.RelativeSearchPath + "\\Bin64");
        }
        
        System.Environment.SetEnvironmentVariable("PATH", _path, EnvironmentVariableTarget.Process);

        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Documents and Settings\kalin\My Documents\Path.txt");
        //file.WriteLine(System.Environment.GetEnvironmentVariable("PATH"));
        //file.Close();
    }
    
    void Application_End(object sender, EventArgs e) 
    {
        //  Code that runs on application shutdown

    }
        
    void Application_Error(object sender, EventArgs e) 
    { 
        // Code that runs when an unhandled error occurs
    }

    void Session_Start(object sender, EventArgs e) 
    {
        // Code that runs when a new session is started

    }

    void Session_End(object sender, EventArgs e) 
    {
        // Code that runs when a session ends. 
        // Note: The Session_End event is raised only when the sessionstate mode
        // is set to InProc in the Web.config file. If session mode is set to StateServer 
        // or SQLServer, the event is not raised.

    }
       
</script>
