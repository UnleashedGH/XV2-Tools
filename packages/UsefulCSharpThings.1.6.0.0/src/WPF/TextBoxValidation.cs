﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace UsefulThings.WPF
{
    /// <summary>
    /// Deals with simple path or number validation.
    /// </summary>
    public class TextBoxValidation : ValidationRule
    {
        public bool RequireExistence { get; set; }  // KFreon: If True, checks if entered path actually exists
        public bool IsNumber { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (String.IsNullOrEmpty(value as string))
                return new ValidationResult(false, "Value can't be empty.");

                string val = (string)value;

            ValidationResult result = ValidationResult.ValidResult;

            if (IsNumber)
            {
                // KFreon: Should be a number so try to parse number, and check bounds
                    int num = -1;
                    if (!Int32.TryParse(val, out num))
                    result = new ValidationResult(false, "Not a valid number");
                    
                if (Min != Max) // KFreon: One is set to something
                {
                    if (num > Max)
                        result = new ValidationResult(false, "Must be smaller than " + Max);
                    else if (num < Min)
                        result = new ValidationResult(false, "Must be larger than " + Min);
                }
            }
            else
            {
                if (val.Length < 3) 
                    return new ValidationResult(false, "Need more characters"); // KFreon: Just needs to be red. No message
                
                if (!Regex.IsMatch(val, @"^([a-zA-Z]:|\e)\e"))
                    return new ValidationResult(false, "Path should be <letter>:\\"); 
                        
                if (RequireExistence)
                {
                        // KFreon: Check if path exists
                        if (val.isFile() && !File.Exists(val))
                        result = new ValidationResult(false, "Specified file doesn't exist!");
                    else if (!Directory.Exists(val))
                        result = new ValidationResult(false, "Specified directory doesn't exist!");
                }
            }
            return result;
        }
    }
}
