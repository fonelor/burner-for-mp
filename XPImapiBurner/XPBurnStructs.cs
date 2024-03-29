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

namespace XPBurn
{
  /// <summary>
  /// This type is returned by the <see cref="XPBurnCD.MediaInfo" /> property and contains information about the 
  /// media (CD) which is currently inserted into the recorder.  
  /// </summary>
  public struct Media
  {
    /// <summary>
    /// Indicates the CD in the active recorder is blank.
    /// </summary>
    public bool isBlank;

    /// <summary>
    /// Indicates that the CD in the active recorder is both readable and writable.
    /// </summary>
    public bool isReadWrite;

    /// <summary>
    /// Indicates that the CD in the active recorder is writable.
    /// </summary>
    public bool isWritable;

    /// <summary>
    /// Indicates that the CD in the active recorder is usable.
    /// </summary>
    public bool isUsable;
  }
}