using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using System.Drawing.Drawing2D;

namespace Burner
{
    public class MenuGenerator
    {
        #region Enums

        private enum MenuGenarationState
        {
            MainMenuGen = 0,
            Thumbnails = 1,
            SubMenu = 2,
            ConvertToMp2 = 3,
            AddSilence = 4,
            Spumux = 5,
            AllFinished = 6
        }

         #endregion

        # region Class Variables

        private Process MGProcess; // Will run the external processes in another thread
        private string _PathToBackground; // Will hold path to image used for background
        private string _PathToTempFolder; // will hold path to temp folder
        private List<string> _ShowNames; // Names of shows on the disk
        private List<string> _SubMenuStr; // 'Main menu','Play Show','Episodes'
        private bool _InDebugMode; // true if in debug mode
        private MenuGenarationState _MenuGenState; // States for whole menu generation
        private bool _AllFinished; // will be true when all submenus will be finished
        private Image menuBackground; // will store background image
        private Image Button; // will store button image for menus
        private long pngDepth = 32L;

        #endregion

        #region Constructors

        ///<summary>ManuGenerator Class Constructor.</summary>
        ///<return>None</return>
        ///<param name="ShowNames">List of show names to include in menu</param>
        ///<param name="PathToBackground">string path to png with background</param>
        ///<param name="PathToTempFolder">Path to the folder to use for creating temporary files</param>
        ///<param name="SubMenuStr">List of text to replace in submenu 'Main menu','Play Show','Episode:' text</param>

        public MenuGenerator(List<string> ShowNames, string PathToBackground, string PathToTempFolder,
                             List<string> SubMenuStr, bool DebugMode)
        {
            _PathToBackground = PathToBackground;
            _PathToTempFolder = PathToTempFolder;
            _ShowNames = ShowNames;

            if (SubMenuStr.Count == 0)
            {

                SubMenuStr.Add("Main menu");
                SubMenuStr.Add("Play Show");
                SubMenuStr.Add("Episodes: ");

            }
            _SubMenuStr = SubMenuStr;
            _InDebugMode = DebugMode;

            _MenuGenState = MenuGenarationState.MainMenuGen;

            Log.Debug("MenuGenerator Init:", "ShowNames count: " + ShowNames.Count);
            Log.Debug("MenuGenerator Init:", "SubMenuStr count: " + SubMenuStr.Count);
        }

        #endregion

        #region Start

        /// <summary>
        /// Called to start menu generation
        /// </summary>
        public void Start()
        {
            // Background generation
            Image Background = Bitmap.FromFile(_PathToBackground);
            Size newSize = new Size(720, 576);
            menuBackground = resizeImage(Background, newSize);

            if (!savePng(Path.Combine(_PathToTempFolder, "menuBackground.png"),
                            new Bitmap(menuBackground), 100L, pngDepth))
                Log.Debug("Menu generator: Main menu: Background: ", "cant save file ",
                                Path.Combine(_PathToTempFolder, "menuBackground.png"));
            Background.Dispose(); //release background image

            // Load button image
            //Button = Bitmap.FromFile(Config.GetFile(Config.Dir.Skin, @"Default\Media\", "arrow_round_right_focus.png"));
            Button = Bitmap.FromFile(Config.GetFile(Config.Dir.Base, @"Burner\", "navButton.png"));
            Button = resizeImage(Button, new Size(40, 40));

            while (!_AllFinished)
            {
              if(MGProcess == null || MGProcess.HasExited)
                switch (_MenuGenState)
                {
                    case MenuGenarationState.MainMenuGen:
                        // Step 1. Main menu generation
                        MainMenuGeneration();
                        break;

                    case MenuGenarationState.Thumbnails:
                        // Step 2. Movie thumbnails generation
                        GenThumbnails();
                        //MainMenuGeneration();
                        break;

                    case MenuGenarationState.SubMenu:
                        // Step 3. Submenu generation
                        SubMenuGeneration();
                        break;

                    case MenuGenarationState.ConvertToMp2:
                        Mp2Convert();
                        break;

                    case MenuGenarationState.AddSilence:
                        AddSilence();
                        break;

                    case MenuGenarationState.Spumux:
                        RunSpumux();
                        break;
                }
            }
        }

        #endregion

        #region Events

        ///<summary>Called when each Menu Generation Step has completed.</summary>
        private void MGProcess_Exited(object sender, EventArgs e)
        {
            Log.Debug("Menu creation Step Exited: Step: ", _MenuGenState.ToString());
            //ProvideStatusUpdate("DVD Burn Process Exited: " + _CurrentProcess);

            //one process has finished, start next process
            
            _MenuGenState += 1;

            if (_MenuGenState == MenuGenarationState.AllFinished)
                _AllFinished = true;
            
            //Start();
        }

        #endregion

        #region MenuGenerationSteps

        /// <summary>
        /// Generates main menu background, stamp file for spumux and xml file for spumux
        /// </summary>
        private void MainMenuGeneration()
        {
            Log.Info("Started main menu generation");

            // Step 1. Generate background
            // Done in Start()
            
            // Step 2. Put text on background and generate stamp for spumux
            Image menuWithText = (Image) menuBackground.Clone();
            // Add image with video tape
            Image myVideosElement = Bitmap.FromFile(Config.GetFile(Config.Dir.Skin, @"Default\Media\", "hover_my videos.png"));
            myVideosElement = resizeImage(myVideosElement, new Size(myVideosElement.Width / 2, myVideosElement.Height / 2));
            menuWithText = CombineImages(menuWithText, myVideosElement, 510, 250);

            Image mainMenuStamp = new Bitmap(menuWithText.Width, menuWithText.Height);
            Graphics menuStamp = Graphics.FromImage(mainMenuStamp);
            menuStamp.Clear(Color.Transparent);

            for (int i = 1; i != _ShowNames.Count + 1; i++)
            {
                int y = i * (Button.Height + 10);
                string text = _ShowNames[i - 1];
                mainMenuStamp = CombineImages(mainMenuStamp, Button, 40, y);
                menuWithText = PutTextOnImage(menuWithText, text, 60, y, 26);
            }

            // save stamp and main menu
            if (!savePng(Path.Combine(_PathToTempFolder, "menuMainStamp.png"),
                new Bitmap(mainMenuStamp), 100L, 16L))
                Log.Debug("Menu generator: Main menu: Background: ", "cant save file ",
                                Path.Combine(_PathToTempFolder, "menuMainStamp.png"));

            if (!savePng(Path.Combine(_PathToTempFolder, "menuMainBackground.png"),
                            new Bitmap(menuWithText), 100L, pngDepth))
                Log.Debug("Menu generator: Main menu: Background: ", "cant save file ",
                                Path.Combine(_PathToTempFolder, "menuMainBackground.png"));
            
            // release all used images
            mainMenuStamp.Dispose(); 
            menuStamp.Dispose();
            menuWithText.Dispose();
            myVideosElement.Dispose();

            // Step 3. Generate spumux xml
            using (StreamWriter SW_SpumuxMain = File.CreateText(Path.Combine(_PathToTempFolder, "menuBackground.menu.config.xml")))
            {
                SW_SpumuxMain.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                SW_SpumuxMain.WriteLine("  <subpictures>");
                SW_SpumuxMain.WriteLine("    <stream>");
                SW_SpumuxMain.WriteLine("      <spu start=\"00:00:00.0\" end=\"00:00:00.0\" highlight=\"" + Path.Combine(_PathToTempFolder, "menuMainStamp.png") +
                                        "\" select=\"" + Path.Combine(_PathToTempFolder, "menuMainStamp.png") + "\" transparent=\"010101\" force=\"yes\" autoorder=\"rows\">");
                
                for (int i = 1; i != _ShowNames.Count + 1; i++)
                {
                    int y = i * (Button.Height + 10);
                    SW_SpumuxMain.WriteLine("      <button x0=\"0\" y0=\"" + y.ToString() + "\" x1=\"720\" y1=\"" + (y + 47).ToString() + "\" />");
                }

                SW_SpumuxMain.WriteLine("    </spu>");
                SW_SpumuxMain.WriteLine("  </stream>");
                SW_SpumuxMain.WriteLine("</subpictures>");

                SW_SpumuxMain.Close();
            }

            // No Actual external app running to Exit so 
            // we just call MGProcess_exited
            EventArgs e = new EventArgs();
            MGProcess_Exited(this, e);
            
            Log.Info("Ended main menu generation");
        }

        /// <summary>
        /// Generates thumbnails for all movies
        /// </summary>
        private void GenThumbnails()
        {
            Log.Info("Started thumbnails generation");

            // Step 1. Make a batch file
            using (StreamWriter SW_Thumbnail = File.CreateText(Path.Combine(_PathToTempFolder, "thumbnails.bat")))
            {
                for (int i = 1; i != _ShowNames.Count + 1; i++)
                {
                    SW_Thumbnail.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"MovieThumbnailer\", "mtn.exe") + "\" -i -t -P -c 1 -r 1 \"" + 
                            Path.Combine(_PathToTempFolder, "F" + i.ToString() + ".mpg"));
                }
                SW_Thumbnail.WriteLine("exit(0)");

                SW_Thumbnail.Close();
            }

            // Step 2. Run batch file
            MGProcess = new Process();
            MGProcess.EnableRaisingEvents = true;
            MGProcess.StartInfo.WorkingDirectory = _PathToTempFolder;
            MGProcess.StartInfo.UseShellExecute = false;
            MGProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            if (!_InDebugMode) // Show output if in Debug mode
            {
                MGProcess.StartInfo.RedirectStandardOutput = true;
                MGProcess.StartInfo.CreateNoWindow = true;
            }

            MGProcess.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");//Path.Combine(_PathToTempFolder, "thumbnails.bat");
            MGProcess.StartInfo.Arguments = "/C thumbnails.bat";
            MGProcess.Exited += new EventHandler(MGProcess_Exited);

            Log.Info("Starting Thumbnail generation Process");
            MGProcess.Start();

            if (!MGProcess.HasExited)
            {
                MGProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            Log.Info("Ended thumbnails generation");
        }

        /// <summary>
        /// Generates Sub Menu background, stamp file for spumux and xml file for spumux
        /// </summary>
        private void SubMenuGeneration()
        {
            Log.Info("Started submenus generation");

            // Load some needed images
            Image myVideosElement = Bitmap.FromFile(Config.GetFile(Config.Dir.Skin, @"Default\Media\", "hover_my videos.png"));
            myVideosElement = resizeImage(myVideosElement, new Size(myVideosElement.Width / 2, myVideosElement.Height / 2));

            Image osd = Bitmap.FromFile(Config.GetFile(Config.Dir.Skin, @"Default\Media\", "osd_dialog_big.png"));


            
            for (int curMenu = 0; curMenu != _ShowNames.Count; curMenu++)
            {
                // Step 1. Add thumbnail on image and other images
                Image menuWithText = (Image)menuBackground.Clone();
                
                // Add image with video tape
                menuWithText = CombineImages(menuWithText, myVideosElement, 510, 250);

                // Add osd image
                menuWithText = CombineImages(menuWithText, osd, 75, 50);
              
                // Add thumbnail
                Image thumb = Bitmap.FromFile(Path.Combine(_PathToTempFolder, "F" + (curMenu + 1).ToString() + "_s.jpg"));
                thumb = resizeImage(thumb, new Size(380, 285));
                menuWithText = CombineImages(menuWithText, thumb, 200, 86);

                // Make stamp image for spumux
                Image subMenuStamp = new Bitmap(menuWithText.Width, menuWithText.Height);
                Graphics menuStamp = Graphics.FromImage(subMenuStamp);
                menuStamp.Clear(Color.Transparent);


                // Step 2. Add text and make stamp image
                // Add Episodes and Name of the show
                //subMenuStamp = CombineImages(subMenuStamp, Button, 10, 20);
                menuWithText = PutTextOnImage(menuWithText, _SubMenuStr[2] + " " + _ShowNames[curMenu], 60, 20, 26);

                // Add Play Show
                subMenuStamp = CombineImages(subMenuStamp, Button, 40, 400);
                menuWithText = PutTextOnImage(menuWithText, _SubMenuStr[1], 60, 400, 26);

                // Add Main menu
                subMenuStamp = CombineImages(subMenuStamp, Button, 40, 450);
                menuWithText = PutTextOnImage(menuWithText, _SubMenuStr[0], 60, 450, 26);

                // Save stamp image and background
                if (!savePng(Path.Combine(_PathToTempFolder, "menuSubStamp." + curMenu.ToString() + ".png"),
                new Bitmap(subMenuStamp), 100L, 16L))
                    Log.Debug("Menu generator: Sub menu: Stamp: ", "cant save file ",
                                    Path.Combine(_PathToTempFolder, "menuSubStamp." + curMenu.ToString() + ".png"));

                if (!savePng(Path.Combine(_PathToTempFolder, "menuSubBackground." + curMenu.ToString() +".png"),
                new Bitmap(menuWithText), 100L, pngDepth))
                    Log.Debug("Menu generator: Sub menu: Background: ", "cant save file ",
                                    Path.Combine(_PathToTempFolder, "menuSubBackground." + curMenu.ToString() + ".png"));

                // Release all images
                menuWithText.Dispose();
                thumb.Dispose();
                subMenuStamp.Dispose();
                menuStamp.Dispose();

                // Step 3. Generate spumux xml
                using (StreamWriter SW_SpumuxMain = File.CreateText(Path.Combine(_PathToTempFolder, "subMenuBackground." + curMenu.ToString() + ".menu.config.xml")))
                {
                    SW_SpumuxMain.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    SW_SpumuxMain.WriteLine("  <subpictures>");
                    SW_SpumuxMain.WriteLine("    <stream>");
                    SW_SpumuxMain.WriteLine("      <spu start=\"00:00:00.0\" end=\"00:00:00.0\" highlight=\"" + 
                                            Path.Combine(_PathToTempFolder, "menuSubStamp." + curMenu.ToString() + ".png") +
                                            "\" select=\"" +
                                            Path.Combine(_PathToTempFolder, "menuSubStamp." + curMenu.ToString() + ".png")
                                            + "\" transparent=\"010101\" force=\"yes\" autoorder=\"rows\">");

                    SW_SpumuxMain.WriteLine("      <button x0=\"0\" y0=\"400\" x1=\"720\" y1=\"447\" />");
                    SW_SpumuxMain.WriteLine("      <button x0=\"0\" y0=\"450\" x1=\"720\" y1=\"497\" />");
                    SW_SpumuxMain.WriteLine("    </spu>");
                    SW_SpumuxMain.WriteLine("  </stream>");
                    SW_SpumuxMain.WriteLine("</subpictures>");

                    SW_SpumuxMain.Close();
                }


            }

            // Release images
            myVideosElement.Dispose();
            osd.Dispose();

            // No Actual external app running to Exit so 
            // we just call MGProcess_exited
            EventArgs e = new EventArgs();
            MGProcess_Exited(this, e);

            Log.Info("Ended submenus generation");

        }

        /// <summary>
        /// Converts png files to m2v
        /// </summary>
        private void Mp2Convert()
        {
            Log.Info("Started Mp2 Convertion");

            // Step 1. Make a batch file
            using (StreamWriter SW_mp2 = File.CreateText(Path.Combine(_PathToTempFolder, "mp2convert.bat")))
            {
                // main menu file
                SW_mp2.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"Burner\", "png2yuv.exe") + "\" -n 30 -I p -f 25 -j \"" +
                Path.Combine(_PathToTempFolder, "menuMainBackground.png") +
                "\" | \"" + Config.GetFile(Config.Dir.Base, @"Burner\", "mpeg2enc.exe") +
                "\"  -n p -f 8 -o \"" +
                Path.Combine(_PathToTempFolder, "menuBackground.menu.m2v") + "\"");
                
                for (int i = 0; i != _ShowNames.Count; i++)
                {
                    SW_mp2.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"Burner\", "png2yuv.exe") + "\" -n 30 -I p -f 25 -j \"" +
                            Path.Combine(_PathToTempFolder, "menuSubBackground." + i.ToString() + ".png") + 
                            "\" | \"" + Config.GetFile(Config.Dir.Base, @"Burner\", "mpeg2enc.exe") + 
                            "\"  -n p -f 8 -o \"" + 
                            Path.Combine(_PathToTempFolder, "subMenuBackground." + i.ToString() + ".m2v") + "\"");
                }
                //SW_mp2.WriteLine("pause");

                SW_mp2.Close();
            }

            // Step 2. Run batch file
            MGProcess = new Process();
            MGProcess.EnableRaisingEvents = true;
            MGProcess.StartInfo.WorkingDirectory = _PathToTempFolder;
            MGProcess.StartInfo.UseShellExecute = false;
            if (!_InDebugMode) // Show output if in Debug mode
            {
                //MGProcess.StartInfo.RedirectStandardOutput = true;
                MGProcess.StartInfo.CreateNoWindow = true;
            }

            MGProcess.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            MGProcess.StartInfo.Arguments = "/C mp2convert.bat";
            MGProcess.Exited += new EventHandler(MGProcess_Exited);

            Log.Info("Starting mp2 convertion Process");
            MGProcess.Start();

            if (!MGProcess.HasExited)
            {
                MGProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            Log.Info("Ended Mp2 Convertion");

        }

        /// <summary>
        /// Adds silence to all m2v files
        /// </summary>
        private void AddSilence()
        {
            Log.Info("Started Adding Silence to m2v files");

            // Step 1. Make a batch file
            using (StreamWriter SW_silence = File.CreateText(Path.Combine(_PathToTempFolder, "addsilence.bat")))
            {
                // main menu file
                SW_silence.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"Burner\", "mplex.exe") + "\" -f 8 -o \"" +
                Path.Combine(_PathToTempFolder, "menuBackground.menu_temp.mpg") +
                "\" \"" + Path.Combine(_PathToTempFolder, "menuBackground.menu.m2v") + "\" \"" + 
                Config.GetFile(Config.Dir.Base, @"Burner\", "Silence.ac3") + "\"");

                for (int i = 0; i != _ShowNames.Count; i++)
                {
                    SW_silence.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"Burner\", "mplex.exe") + "\" -f 8 -o \"" +
                    Path.Combine(_PathToTempFolder, "menuSubBackground." + i.ToString() + ".menu_temp.mpg") +
                    "\" \"" + Path.Combine(_PathToTempFolder, "subMenuBackground." + i.ToString() + ".m2v") + "\" \""
                    + Config.GetFile(Config.Dir.Base, @"Burner\", "Silence.ac3") + "\"");
                }
                SW_silence.WriteLine("exit(0)");

                SW_silence.Close();
            }

            // Step 2. Run batch file
            MGProcess = new Process();
            MGProcess.EnableRaisingEvents = true;
            MGProcess.StartInfo.WorkingDirectory = _PathToTempFolder;
            MGProcess.StartInfo.UseShellExecute = false;
            if (!_InDebugMode) // Show output if in Debug mode
            {
                MGProcess.StartInfo.RedirectStandardOutput = true;
                MGProcess.StartInfo.CreateNoWindow = true;
            }

            MGProcess.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

            MGProcess.StartInfo.Arguments = "/C addsilence.bat";
            MGProcess.Exited += new EventHandler(MGProcess_Exited);

            Log.Info("Starting adding silencs Process");
            MGProcess.Start();

            if (!MGProcess.HasExited)
            {
                MGProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            Log.Info("Ended Adding Silence to m2v files");

        }

        /// <summary>
        /// Runs spumux on all menu files
        /// </summary>
        private void RunSpumux()
        {
            Log.Info("Started Spumux");

            // Step 1. Make a batch file
            using (StreamWriter SW_spumux = File.CreateText(Path.Combine(_PathToTempFolder, "spumux.bat")))
            {
                // main menu file
                SW_spumux.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"Burner\", "imagequ.exe") + "\" /c4 \"" + 
                    Path.Combine(_PathToTempFolder, "menuMainStamp.png") + "\"");
                SW_spumux.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"Burner\", "spumux.exe") + "\" -v 1 \"" +
                    Path.Combine(_PathToTempFolder, "menuBackground.menu.config.xml") + "\" < \"" +
                    Path.Combine(_PathToTempFolder, "menuBackground.menu_temp.mpg") + "\" > \"" +
                    Path.Combine(_PathToTempFolder, "menuBackground.menu.mpg") + "\"");

                for (int i = 0; i != _ShowNames.Count; i++)
                {
                    SW_spumux.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"Burner\", "imagequ.exe") + "\" /c4 \"" +
                        Path.Combine(_PathToTempFolder, "menuSubStamp." + i.ToString() + ".png") + "\"");
                    SW_spumux.WriteLine("\"" + Config.GetFile(Config.Dir.Base, @"Burner\", "spumux.exe") + "\" -v 1 \"" +
                        Path.Combine(_PathToTempFolder, "subMenuBackground." + i.ToString() + ".menu.config.xml") + "\" < \"" +
                        Path.Combine(_PathToTempFolder, "menuSubBackground." + i.ToString() + ".menu_temp.mpg") + "\" > \"" +
                        Path.Combine(_PathToTempFolder, "menuSubBackground." + i.ToString() + ".menu.mpg") + "\"");
                }
                SW_spumux.WriteLine("exit(0)");

                SW_spumux.Close();
            }

            // Step 2. Run batch file
            MGProcess = new Process();
            MGProcess.EnableRaisingEvents = true;
            MGProcess.StartInfo.WorkingDirectory = _PathToTempFolder;
            MGProcess.StartInfo.UseShellExecute = false;
            if (!_InDebugMode) // Show output if in Debug mode
            {
                MGProcess.StartInfo.RedirectStandardOutput = true;
                MGProcess.StartInfo.CreateNoWindow = true;
            }

            MGProcess.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            MGProcess.StartInfo.Arguments = "/C spumux.bat";
            MGProcess.Exited += new EventHandler(MGProcess_Exited);

            Log.Info("Starting spumux Process");
            MGProcess.Start();

            if (!MGProcess.HasExited)
            {
                MGProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            Log.Info("Ended Spumux");

        }

        #endregion

        #region Methods for images

        /// <summary>
        /// Puts text on image
        /// </summary>
        /// <param name="imgBack">background image</param>
        /// <param name="text">text to draw</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>image with text</returns>
        private Image PutTextOnImage(Image imgBack, string text, int x, int y, int size)
        {
            Graphics graphBack = Graphics.FromImage(imgBack);
            if (x < imgBack.Width || y < imgBack.Height)
            {
                graphBack.DrawString(text, new Font("Tahoma", size), Brushes.White, new Point(x, y));
            }
            else
            {
                Log.Debug("PutTextOnImage: coordinates out of image range x = %d, y = %d", x, y);
            }

            return imgBack;
        }

        /// <summary>
        /// Combines two images
        /// </summary>
        /// <param name="imgBack">image that will be in background</param>
        /// <param name="imgOn">image that will be in the front</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>combined image</returns>
        private Image CombineImages(Image imgBack, Image imgOn, int x, int y)
        {
            Graphics graphBack = Graphics.FromImage(imgBack);
            if (x < imgBack.Width || y < imgBack.Height)
            {
                graphBack.DrawImage(imgOn, new Point(x, y));
            }
            else
            {
                Log.Debug("CombineImages: coordinates out of image range x = %d, y = %d", x, y);
            }

            return imgBack;
        }
        
        /// <summary>
        /// Saves Bitmap to png file
        /// </summary>
        /// <param name="path">path to file</param>
        /// <param name="img">bitmap that should be saved</param>
        /// <param name="quality">quality for image</param>
        /// <returns>true if image saved, false othewise</returns>
        private bool savePng(string path, Bitmap img, long quality, long colorDepth)
        {
            System.Drawing.Imaging.Encoder pngEncoder = System.Drawing.Imaging.Encoder.ColorDepth;
            
           /* // Encoder parameter for image quality
            EncoderParameter qualityParam = 
                    new EncoderParameter(pngEncoder, quality);*/

            // Encoder parameter for color depth
            EncoderParameter colorParam =
                    new EncoderParameter(pngEncoder, 2L);//colorDepth);

            // PNG image codec
            ImageCodecInfo pngCodec = getEncoderInfo("image/png");

            if (pngCodec == null)
                return false;

            EncoderParameters encoderParams = new EncoderParameters(1);
            //encoderParams.Param[0] = qualityParam;
            encoderParams.Param[0] = colorParam;

            img.Save(path, pngCodec, encoderParams);

            return true;
        }

        /// <summary>
        /// Resizes image
        /// </summary>
        /// <param name="imgToResize">Image to resize</param>
        /// <param name="size">new image size</param>
        /// <returns>resized image</returns>
        private static Image resizeImage(Image imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }

        /// <summary>
        /// Searches for codec
        /// </summary>
        /// <param name="mimeType">mime type for codec</param>
        /// <returns>ImageCodecInfo if coedec found, otherwise null</returns>
        private ImageCodecInfo getEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        #endregion

    }
}   

