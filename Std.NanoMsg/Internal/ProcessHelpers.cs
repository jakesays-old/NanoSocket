using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Std.NanoMsg.Internal
{
	public static class ProcessHelpers
	{
		//these will never change during the course of
		//an exe's lifetime.
		private static string _processDirectory;
		private static string _processName;
	    private static string _processPath;

		public static string HostProcessDirectory
		{
			get
			{
				InitializeProcessInformation();

				return _processDirectory;
			}
		}

		public static string HostProcessName
		{
			get
			{
				InitializeProcessInformation();

				return _processName;
			}
		}


	    public static string HostProcessPath
	    {
	        get
	        {
	            InitializeProcessInformation();

	            return _processPath;
	        }
	    }

        //if you wish to mock this, use a private accessor
        //and set its value.
        private static bool? _isTestHost;

		/// <summary>
		/// Returns true if the caller is running under an mstest host process
		/// </summary>
		public static bool IsTestHostProcess
		{
			get
			{
				if (!_isTestHost.HasValue)
				{
					var processFileName = Process.GetCurrentProcess().MainModule.FileName.ToLower();
					_isTestHost = processFileName.Contains("qtagent") || processFileName.Contains("vstesthost") ||
						processFileName.Contains("jetbrains.resharper.taskrunner");
				}

				//?? false is redundant, but it shuts up resharper
				return _isTestHost ?? false;
			}
		}

		private static void InitializeProcessInformation()
		{
		    if (!string.IsNullOrEmpty(_processDirectory))
		    {
		        return;
		    }

		    if (IsTestHostProcess)
		    {
		        _processDirectory = Directory.GetCurrentDirectory();
		        _processName = Process.GetCurrentProcess().MainModule.FileName;
		    }
		    else
		    {
		        //first try the executable assembly location. it appears this isn't always available.
		        //seems to be the case when running under the vs test host process.

		        var assy = Assembly.GetEntryAssembly();
		        if (assy != null && !string.IsNullOrEmpty(assy.Location))
		        {
		            _processName = Path.GetFileName(assy.Location);

		            if (assy.Location.ToLower().EndsWith(".exe", StringComparison.Ordinal))
		            {
		                _processDirectory = Path.GetDirectoryName(assy.Location);
		            }
		            else
		            {
		                _processDirectory = assy.Location;
		            }
		        }
		        else
		        {
		            //fall back to the process location
		            var fullPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
		            _processName = Path.GetFileName(fullPath);
		            _processDirectory = Path.GetDirectoryName(fullPath);
		        }
		    }

		    var nameLowered = _processName.ToLowerInvariant();
		    var vsHostPos = nameLowered.IndexOf(".vshost", StringComparison.Ordinal);
		    if (vsHostPos != -1)
		    {
		        _processName = _processName.Substring(0, vsHostPos);
		        if (nameLowered.EndsWith(".exe", StringComparison.Ordinal))
		        {
		            _processName += ".exe";
		        }
		    }

		    _processPath = Path.Combine(_processDirectory, _processName);
		}		 
	}
}