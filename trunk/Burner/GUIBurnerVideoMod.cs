#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Playlists;
using MediaPortal.Ripper;
using MediaPortal.TagReader;
using MediaPortal.Util;
using MediaPortal.Profile;
using XPBurn;
using MediaInfoLib;

namespace Burner
{
    public class GUIBurnerVideoMod : GUIInternalOverlayWindow
    {
        #region Class Variables

        public const int ID = 762; // Holds the ID of this Window
        private ArrayList _ShowNames = new ArrayList();
        private List<string> _Buttons;
        public Quality _quality;
        private int _totaltime;

        #endregion

        #region Enumerators

        public enum Quality
        {
            SP = 1,
            LP = 2,
            EP = 3
        }
        
        private enum Controls : int
        {
            UP_DOWN_BITRATE = 2,
            MAIN_MENU = 3,
            PLAY_SHOW = 4,
            EPISODES = 5,
            DISK_NAME = 6,
            DONE = 7,
            BACKGROUND_IMAGE = 8,
            SHOW_NAMES = 9
        }


        #endregion

        #region constructor

        public GUIBurnerVideoMod()
        {
            GetID = ID;
        }

        #endregion

        #region Overrides

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + @"\myburner.videomod.xml");
        }

        public override bool OnMessage(GUIMessage message)
        {
            switch (message.Message)
            {

                #region Initiation

                case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
                    base.OnMessage(message);

                    GUIPropertyManager.SetProperty("#currentmodule", GUILocalizeStrings.Get(2100));
                    //GUIPropertyManager.SetProperty("#background_menu", @"I:\temp\DVD\F1_s.jpg");
                    GUIControl.HideControl(GetID, (int)Controls.BACKGROUND_IMAGE); // this part should be done in next version of Burner

                    GUIControl.ClearControl(GetID, (int)Controls.SHOW_NAMES);

                    for (int i = 0; i < _ShowNames.Count; i++)
                    {
                        GUIListItem Item = new GUIListItem((GUIListItem)_ShowNames[i]);
                        GUIControl.AddListItemControl(GetID, (int)Controls.SHOW_NAMES, Item);
                    }
                    if (_Buttons.Count == 0)
                    {
                        _Buttons.Add(GUILocalizeStrings.Get(974)); // Main menu
                        _Buttons.Add(GUILocalizeStrings.Get(4552)); // Play
                        _Buttons.Add(GUILocalizeStrings.Get(1091) + ": "); //Show: 
                        _Buttons.Add(string.Format("MP-DVD-{0}", DateTime.Now.ToShortDateString()));
                    }

                    GUIControl.SetControlLabel(GetID, (int)Controls.MAIN_MENU, _Buttons[0]);
                    GUIControl.SetControlLabel(GetID, (int)Controls.PLAY_SHOW, _Buttons[1]);
                    GUIControl.SetControlLabel(GetID, (int)Controls.EPISODES, _Buttons[2]);

                    GUIControl.SetControlLabel(GetID, (int)Controls.DONE, GUILocalizeStrings.Get(804)); //Done
                    //GUIControl.SetControlLabel(GetID, (int)Controls.DISK_NAME, GUILocalizeStrings.Get(988));

                    UpdateTime(_quality);

                    GUIControl.SetControlLabel(GetID, (int)Controls.UP_DOWN_BITRATE, GUILocalizeStrings.Get(988) + " " + _quality.ToString()); // Quality

                    return true;

                #endregion

                case GUIMessage.MessageType.GUI_MSG_CLICKED:
                    base.OnMessage(message);

                    int iControl = message.SenderControlId;

                    #region Quality

                    if (iControl == (int)Controls.UP_DOWN_BITRATE)
                    {
                        Quality tmpQ = _quality;
                        
                        GUIDialogSelect2 dlgSelectBitrate = (GUIDialogSelect2)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_SELECT2);

                        if (dlgSelectBitrate != null)
                        {
                            dlgSelectBitrate.Reset();
                            dlgSelectBitrate.SetHeading(GUILocalizeStrings.Get(988)); //Quality
                            dlgSelectBitrate.Add("SP: 2h 12min");
                            dlgSelectBitrate.Add("LP: 3h 40min");
                            dlgSelectBitrate.Add("EP: 6h 22min");
                            dlgSelectBitrate.DoModal(GetID);

                            switch (dlgSelectBitrate.SelectedLabelText)
                            {
                                case "SP: 2h 12min":
                                    tmpQ = Quality.SP;
                                    break;
                                case "LP: 3h 40min":
                                    tmpQ = Quality.LP;
                                    break;
                                case "EP: 6h 22min":
                                    tmpQ = Quality.EP;
                                    break;
                                default:
                                    tmpQ = Quality.SP;
                                    break;
                            }

                            if (UpdateTime(tmpQ))
                            {
                                _quality = tmpQ;
                                GUIControl.SetControlLabel(GetID, (int)Controls.UP_DOWN_BITRATE, GUILocalizeStrings.Get(988) + " " + _quality.ToString()); // Quality
                            }
                            
                        }
                    }


                    #endregion

                    #region Main menu

                    if (iControl == (int)Controls.MAIN_MENU)
                    {
                        VirtualKeyboard VrtKey = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
                        if (VrtKey != null)
                        {
                            VrtKey.Reset();
                            VrtKey.Text = _Buttons[(int)Controls.MAIN_MENU - 3];
                            VrtKey.DoModal(GetID);
                            _Buttons[(int)Controls.MAIN_MENU - 3] = VrtKey.Text;
                            GUIControl.SetControlLabel(GetID, (int)Controls.MAIN_MENU, _Buttons[(int)Controls.MAIN_MENU - 3]);
                        }

                    }

                    #endregion

                    #region Play show

                    if (iControl == (int)Controls.PLAY_SHOW)
                    {
                        VirtualKeyboard VrtKey = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
                        if (VrtKey != null)
                        {
                            VrtKey.Reset();
                            VrtKey.Text = _Buttons[(int)Controls.PLAY_SHOW - 3];
                            VrtKey.DoModal(GetID);
                            _Buttons[(int)Controls.PLAY_SHOW - 3] = VrtKey.Text;
                            GUIControl.SetControlLabel(GetID, (int)Controls.PLAY_SHOW, _Buttons[(int)Controls.PLAY_SHOW - 3]);
                        }

                    }

                    #endregion

                    #region Episodes

                    if (iControl == (int)Controls.EPISODES)
                    {
                        VirtualKeyboard VrtKey = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
                        if (VrtKey != null)
                        {
                            VrtKey.Reset();
                            VrtKey.Text = _Buttons[(int)Controls.EPISODES - 3];
                            VrtKey.DoModal(GetID);
                            _Buttons[(int)Controls.EPISODES - 3] = VrtKey.Text;
                            GUIControl.SetControlLabel(GetID, (int)Controls.EPISODES, _Buttons[(int)Controls.EPISODES - 3]);
                        }

                    }

                    #endregion

                    #region Disk Name

                    if (iControl == (int)Controls.DISK_NAME)
                    {
                        VirtualKeyboard VrtKey = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
                        if (VrtKey != null)
                        {
                            VrtKey.Reset();
                            VrtKey.Text = _Buttons[(int)Controls.DISK_NAME - 3];
                            VrtKey.DoModal(GetID);
                            _Buttons[(int)Controls.DISK_NAME - 3] = VrtKey.Text;
                            GUIControl.SetControlLabel(GetID, (int)Controls.DISK_NAME, _Buttons[(int)Controls.DISK_NAME - 3]);
                        }

                    }

                    #endregion

                    #region Background

                    /*if (iControl == (int)Controls.BACKGROUND_IMAGE)
                    {
                        GUIFacadeControl
                        GUIDialogFile dlgFile = (GUIDialogFile)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_FILE);
                        if (dlgFile != null)
                        {
                            VirtualDirectory vdir = new VirtualDirectory();
                            vdir.SetExtensions(MediaPortal.Util.Utils.PictureExtensions);
                            dlgFile.SetDirectoryStructure(vdir);
                            dlgFile.SetDestinationDir(@"I:\temp\");
                            //dlgFile.SetSourceItem(GUIControl.GetListItem(GetID, (int)Controls.SHOW_NAMES, 0));
                            dlgFile.SetSourceDir(@"I:\");
                            dlgFile.DoModal(GetID);
                            //GUIPropertyManager.SetProperty("#background_menu", dlgFile.GetDestinationDir());
                        }

                    }*/
                    #endregion

                    #region Show Names

                    if (iControl == (int)Controls.SHOW_NAMES)
                    {
                        VirtualKeyboard VrtKey = (VirtualKeyboard)GUIWindowManager.GetWindow((int)Window.WINDOW_VIRTUAL_KEYBOARD);
                        if (VrtKey != null)
                        {
                            VrtKey.Reset();
                            GUIListItem li = GUIControl.GetSelectedListItem(GetID, (int)Controls.SHOW_NAMES);
                            VrtKey.Text = li.Label;
                            VrtKey.DoModal(GetID);
                            li.Label = VrtKey.Text;
                            int lstcount = GUIControl.GetItemCount(GetID, (int)Controls.SHOW_NAMES);
                            _ShowNames.Clear();
                            for (int i = 0; i < lstcount; i++)
                            {
                                _ShowNames.Add( GUIControl.GetListItem(GetID, (int)Controls.SHOW_NAMES, i));
                            }
                        }

                    }

                    #endregion

                    #region Done Button

                    if (iControl == (int)Controls.DONE)
                    {
                        GUIBurner brnWind = (GUIBurner)GUIWindowManager.GetWindow(GUIWindowManager.GetPreviousActiveWindow());
                        GUIWindowManager.ShowPreviousWindow();
                        brnWind.LoadParameters(_ShowNames, _totaltime, _quality);
                        return true;
                    }

                    return true;

                    #endregion

                default:
                    return base.OnMessage(message);
            }
        }
        
        #endregion

        #region Start

        /// <summary>
        /// Initiate Variables for VideoMod
        /// </summary>
        /// <param name="ShowNames">List with names of Shows</param>
        /// <param name="Buttons">List with "Main Menu", "Play Show", "Episodes", disk name</param>
        /// <param name="q">Default quality</param>
        public void Start(ArrayList ShowNames, List<string> Buttons, Quality q, int totaltime)
        {
            _ShowNames = ShowNames;
            _Buttons = Buttons;
            _quality = q;
            _totaltime = totaltime;
        }

        #endregion

        #region Metods

        private int getMaxTime(Quality q)
        {
            switch (q)
            {
                case Quality.SP:
                    return 7920;

                case Quality.LP:
                    return 13200;

                case Quality.EP:
                    return 22920;
            }
            
            return 7920;
        
        }

        private bool UpdateTime(Quality q)
        {

            if (_totaltime < getMaxTime(q))
            {
                string time = MediaPortal.Util.Utils.SecondsToHMSString(_totaltime) + " " + GUILocalizeStrings.Get(1973) + " " + MediaPortal.Util.Utils.SecondsToHMSString(getMaxTime(q));
                GUIPropertyManager.SetProperty("#duration", time);
                return true;
            } else {
                GUIDialogNotify dlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_NOTIFY);
                if (null != dlgNotify)
                {
                    dlgNotify.SetHeading(GUILocalizeStrings.Get(2100)); // Burner
                    dlgNotify.SetText(GUILocalizeStrings.Get(2146)); // Not enough room on CD
                    dlgNotify.DoModal(GetID);
                }

                return false;
            }

        }


        #endregion

    }



}