﻿//
//  Copyright © 2013 Parrish Husband (parrish.husband@gmail.com)
//  The MIT License (MIT) - See LICENSE.txt for further details.
//

using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.Win32;

namespace SharpPlant.SmartPlantReview
{
    public static class SprUtilities
    {
        public static Dictionary<string, object> TagTemplate = new Dictionary<string, object>
            {
                {"tag_unique_id", 0},
                {"tag_size", 0},
                {"tag_text", string.Empty},
                {"number_color", 0},
                {"backgnd_color", 0},
                {"leader_color", 0},
                {"discipline", string.Empty},
                {"creator", string.Empty},
                {"computer_name", string.Empty},
                {"status", string.Empty}
            };

        /// <summary>
        ///     Returns a 24-bit color integer.
        /// </summary>
        /// <param name="rgbColor">The System.Drawing.Color to be converted.</param>
        /// <returns></returns>
        public static int Get0Bgr(Color rgbColor)
        {
            // Return a zero-alpha 24-bit BGR color integer
            return 0 + (rgbColor.B << 0x10) + (rgbColor.G << 0x8) + rgbColor.R;
        }

        /// <summary>
        ///     Returns a fully opaque (Alpha 255) color from a 0BGR format.
        /// </summary>
        /// <param name="bgrColor">The 0BGR integer to be converted.</param>
        /// <returns></returns>
        public static Color From0Bgr(int bgrColor)
        {
            // Get the color bytes
            var bytes = BitConverter.GetBytes(bgrColor);

            // Return the color from the byte array
            return Color.FromArgb(bytes[0], bytes[1], bytes[2]);
        }

        /// <summary>
        ///     Writes the tag properties to the default tag registry settings used by SmartPlant Review.
        /// </summary>
        /// <param name="tag">The tag used to determine settings values.</param>
        public static void SetTagRegistry(SprTag tag)
        {
            // Create a three-variable list containing the subkey name, value and type.
            var tagAtts = new List<Tuple<string, object, RegistryValueKind>>
            {
                new Tuple<string, object, RegistryValueKind>("BackgndColor", Get0Bgr(tag.BackgroundColor), RegistryValueKind.DWord),
                new Tuple<string, object, RegistryValueKind>("ComputerName", tag.ComputerName, RegistryValueKind.String),
                new Tuple<string, object, RegistryValueKind>("Creator", tag.Creator, RegistryValueKind.String),
                new Tuple<string, object, RegistryValueKind>("Discipline", tag.Discipline, RegistryValueKind.String),
                new Tuple<string, object, RegistryValueKind>("LeaderColor", Get0Bgr(tag.LeaderColor), RegistryValueKind.DWord),
                new Tuple<string, object, RegistryValueKind>("Status", tag.Status, RegistryValueKind.String),
                new Tuple<string, object, RegistryValueKind>("TextColor", Get0Bgr(tag.TextColor), RegistryValueKind.DWord)
            };

            // Open the SmartPlant Review tag registry
            var regPath = @"Software\Intergraph\SmartPlant Review\Settings\Tags\";
            using (var regKey = Registry.CurrentUser.OpenSubKey(regPath, true))
            {
                // Iterate through the attributes
                for (int i = 0; i < tagAtts.Count; i++)
                {
                    // Get the subkey name (prefixed with "Default" per Spr formatting)
                    var valString = string.Format("Default{0}", tagAtts[i].Item1);

                    // Set the subkey values from the current tuple
                    regKey.SetValue(valString, tagAtts[i].Item2, tagAtts[i].Item3);
                }
            }
        }

        /// <summary>
        ///     Clears the default tag settings from the registry.
        /// </summary>
        public static void ClearTagRegistry()
        {
            // Open the SmartPlant Review tag registry
            var regPath = @"Software\Intergraph\SmartPlant Review\Settings\Tags";
            using (var regKey = Registry.CurrentUser.OpenSubKey(regPath, true))
            {
                // Get the list of values
                var values = regKey.GetValueNames();

                // Iterate through the values
                for (int i = 0; i < values.Length; i++)
                {
                    // Delete the current value
                    regKey.DeleteValue(values[i]);
                }
            }
        }
    }
}