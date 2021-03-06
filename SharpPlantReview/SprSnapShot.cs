﻿//
//  Copyright © 2014 Parrish Husband (parrish.husband@gmail.com)
//  The MIT License (MIT) - See LICENSE.txt for further details.
//

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace SharpPlant.SharpPlantReview
{
    /// <summary>
    ///     Provides the properties for creating a snapshot in SmartPlant Review.
    /// </summary>
    public class SprSnapShot
    {
        #region Properties

        /// <summary>
        ///     The path to the temporary snapshot directory.
        /// </summary>
        public static string TempDirectory { get; set; }

        /// <summary>
        ///     The path to the default snapshot output directory.
        /// </summary>
        public static string DefaultDirectory { get; set; }

        /// <summary>
        ///     The active COM reference to the DrSnapShot class
        /// </summary>
        internal dynamic DrSnapShot;

        /// <summary>
        ///     Holds the snapshot bitmask values used in the DrApi.SnapShot method.
        /// </summary>
        internal int Flags;

        /// <summary>
        ///     The parent Application reference.
        /// </summary>
        public SprApplication Application { get; private set; }

        /// <summary>
        ///     Anti-aliasing factor (1 to 4); 1 = no anti-aliasing.
        /// </summary>
        public int AntiAlias
        {
            get
            {
                if (IsActive) return DrSnapShot.AntiAlias;
                return -1;
            }
            set
            {
                if (value < 1) DrSnapShot.AntiAlias = 1;
                else if (AntiAlias > 4) DrSnapShot.AntiAlias = 4;
                else DrSnapShot.AntiAlias = value;
            }
        }

        /// <summary>
        ///     Pixel height of the output image (minimum 10).  Overriden by Scale/AspectOn.
        /// </summary>
        public int Height
        {
            get
            {
                if (IsActive) return DrSnapShot.Height;
                return -1;
            }
            set
            {
                if (!IsActive) return;
                DrSnapShot.Height = value > 10 ? value : 10;
            }
        }

        /// <summary>
        ///     Pixel width of the output image (minimum 10).  Overriden by Scale/AspectOn.
        /// </summary>
        public int Width
        {
            get
            {
                if (IsActive) return DrSnapShot.Width;
                return -1;
            }
            set
            {
                if (!IsActive) return;
                DrSnapShot.Width = value > 10 ? value : 10;
            }
        }

        /// <summary>
        ///     Output image size / main application view size.  Only applied if AspectOn is true.
        /// </summary>
        public double Scale
        {
            get
            {
                if (IsActive) return DrSnapShot.Scale;
                return -1;
            }
            set { if (IsActive) DrSnapShot.Scale = value; }
        }

        /// <summary>
        ///     Output format of the snapshot.
        /// </summary>
        public SprSnapshotFormat OutputFormat { get; set; }

        /// <summary>
        ///     Determines whether the snapshot will overwrite an existing snapshot of the same name.
        /// </summary>
        public bool Overwrite
        {
            get
            {
                // Return the bitwise zero check
                return (Flags & SprConstants.SprSnapOverwrite) != 0;
            }
            set
            {
                // Set flag true/false
                if (value) Flags |= SprConstants.SprSnapOverwrite;
                else Flags &= ~SprConstants.SprSnapOverwrite;
            }
        }

        /// <summary>
        ///     Determines if the snapshot is fullscreen
        /// </summary>
        public bool Fullscreen
        {
            get
            {
                // Return the bitwise zero check
                return (Flags & SprConstants.SprSnapFullscreen) != 0;
            }
            set
            {
                // Set flag true/false
                if (value) Flags |= SprConstants.SprSnapFullscreen;
                else Flags &= ~SprConstants.SprSnapFullscreen;
            }
        }

        /// <summary>
        ///     Determines if the snapshot is rotated 90 degrees clockwise.
        /// </summary>
        public bool Rotated90
        {
            get
            {
                // Return the bitwise zero check
                return (Flags & SprConstants.SprSnapRotate90) != 0;
            }
            set
            {
                // Set flag true/false
                if (value) Flags |= SprConstants.SprSnapRotate90;
                else Flags &= ~SprConstants.SprSnapRotate90;
            }
        }

        /// <summary>
        ///     Determines whether the scale property is used to determine output image size.
        /// </summary>
        public bool AspectOn
        {
            get
            {
                // Return the bitwise zero check
                return (Flags & SprConstants.SprSnapAspectOn) != 0;
            }
            set
            {
                // Set flag true/false
                if (value) Flags |= SprConstants.SprSnapAspectOn;
                else Flags &= ~SprConstants.SprSnapAspectOn;
            }
        }

        // Determines whether a reference to the COM object is established
        private bool IsActive
        {
            get { return (DrSnapShot != null); }
        }

        #endregion

        #region Constructors

        public SprSnapShot()
        {
            // Link the parent application
            Application = SprApplication.ActiveApplication;

            // Get a new DrSnapShot object
            DrSnapShot = Activator.CreateInstance(SprImportedTypes.DrSnapShot);

            // Set the default snapshot values
            if (Application.DefaultSnapshot != null)
            {
                Flags = Application.DefaultSnapshot.Flags;
                AntiAlias = Application.DefaultSnapshot.AntiAlias;
            }

            if (Application.IsConnected)
            {
                // Set the default size based on the main window 
                Height = Application.Windows.MainWindow.Height;
                Width = Application.Windows.MainWindow.Width;
            }
        }

        #endregion

        #region Methods

        internal static Image FormatSnapshot(string imagePath, SprSnapshotFormat format)
        {
            var imgFile = new Bitmap(imagePath);
            using (var g = Graphics.FromImage(imgFile))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(imgFile, 0, 0, imgFile.Width, imgFile.Height);
            }


            var savePath = imagePath;
            var imgStream = new MemoryStream(); // Must stay open!
            if (format == SprSnapshotFormat.Png)
            {
                savePath = Path.ChangeExtension(imagePath, "png");
                imgFile.Save(imgStream, ImageFormat.Png);
            }
            else if(format == SprSnapshotFormat.Jpg)
            {
                var jpgEncoder = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                if (jpgEncoder != null)
                {
                    // Set the image quality
                    var encodeQuality = Encoder.Quality;
                    var encodeParams = new EncoderParameters(1);
                    var qualityParam = new EncoderParameter(encodeQuality, 95L);
                    encodeParams.Param[0] = qualityParam;

                    savePath = Path.ChangeExtension(imagePath, "jpg");
                    imgFile.Save(imgStream, jpgEncoder, encodeParams);
                }
            }

            imgFile.Dispose();
            return Image.FromStream(imgStream);
        }

        #endregion
    }
}