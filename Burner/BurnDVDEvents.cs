#region Copyright (C) 2005-2010 Team MediaPortal

// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;

namespace Burner
{

  #region EventArgs Classes

  public class FileFinishedEventArgs : EventArgs
  {
    public string SourceFile;
    public string DestinationFile;

    public FileFinishedEventArgs(string inputFileName, string outputFileName)
    {
      SourceFile = inputFileName;
      DestinationFile = outputFileName;
    }
  }

  public class ProcessExitedEventArgs : EventArgs
  {
    public string ProcessName;
    public string DestinationFile;

    public ProcessExitedEventArgs(string ProcessExitedName)
    {
      ProcessName = ProcessExitedName;
    }
  }

  public class BurnDVDErrorEventArgs : EventArgs
  {
    public string Error_Process;
    public string Error_Text;

    public BurnDVDErrorEventArgs(string ErrorProcess, string ErrorText)
    {
      Error_Process = ErrorProcess;
      Error_Text = ErrorText;
    }
  }

  public class BurnDVDStatusUpdateEventArgs : EventArgs
  {
    private string _Status;

    public BurnDVDStatusUpdateEventArgs(string StatusString)
    {
      _Status = StatusString;
    }

    public string Status
    {
      get { return _Status; }
      set { _Status = value; }
    }
  }

  #endregion
}