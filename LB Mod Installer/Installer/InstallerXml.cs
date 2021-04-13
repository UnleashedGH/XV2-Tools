﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using YAXLib;

namespace LB_Mod_Installer.Installer
{
    public class InstallerXml : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [YAXDontSerialize]
        public string InstallerName { get { return string.Format("{0} Installer", Name); } }
        [YAXDontSerialize]
        public string InstallerNameWithVersion { get { return string.Format("{0} Installer ({1})", Name, VersionFormattedString); } }
        [YAXDontSerialize]
        private int _currentInstallStep = -1;
        [YAXDontSerialize]
        public int CurrentInstallStep
        {
            get
            {
                return _currentInstallStep;
            }
            set
            {
                _currentInstallStep = value;
                NotifyPropertyChanged("CurrentInstallStep");
                NotifyPropertyChanged("CurrentStepString");
            }
        }
        [YAXDontSerialize]
        public string CurrentStepString
        {
            get
            {
                return string.Format("Step {0} of {1}", CalcCurrentStepId(), CalcTotalValidSteps());
            }
        }
        [YAXDontSerialize]
        public Version Version
        {
            get
            {
                if (VersionString.Contains(","))
                {
                    VersionString.Replace(',', '.');
                }
                return new Version(VersionString);
            }
        }
        [YAXDontSerialize]
        public string VersionFormattedString
        {
            get
            {
                try
                {
                    string[] split = VersionString.Split('.');
                    if (split[2] == "0" && split[3] == "0")
                    {
                        return String.Format("{0}.{1}", split[0], split[1]);
                    }
                    else if (split[3] == "0")
                    {
                        return String.Format("{0}.{1}{2}", split[0], split[1], split[2]);
                    }
                    else
                    {
                        return String.Format("{0}.{1}{2}{3}", split[0], split[1], split[2], split[3]);
                    }
                }
                catch
                {
                    return Version.ToString();
                }
            }
        }
        

        [YAXAttributeFor("Name")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }
        [YAXAttributeFor("Author")]
        [YAXSerializeAs("value")]
        public string Author { get; set; }
        [YAXAttributeFor("Version")]
        [YAXSerializeAs("value")]
        public string VersionString { get; set; }

        [YAXDontSerializeIfNull]
        public Overrides UiOverrides { get; set; } = new Overrides();

        [YAXSerializeAs("InstallSteps")]
        public List<InstallStep> InstallOptionSteps { get; set; }
        [YAXSerializeAs("FilesToInstall")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "File")]
        public List<FilePath> InstallFiles { get; set; }

        /// <summary>
        /// Returns a list of all selected files + the static InstallFiles.
        /// </summary>
        /// <returns></returns>
        public List<FilePath> GetInstallFiles()
        {
            List<FilePath> files = new List<FilePath>();

            //Get all entries without DoLast flag
            GetOptionInstallFiles(files, false);

            if (InstallFiles != null)
            {
                files.AddRange(InstallFiles.Where(x => !x.GetDoLast()));
            }

            //Get entries with DoLast flag
            GetOptionInstallFiles(files, true);

            if (InstallFiles != null)
            {
                files.AddRange(InstallFiles.Where(x => x.GetDoLast()));
            }

            //Remove all files with empty SourcePaths
            files.RemoveAll(x => string.IsNullOrWhiteSpace(x.SourcePath));

            //Remove files that have a invalid flag
            files.RemoveAll(x => !FlagsIsSet(x.HasFlag));

            return files;
        }

        private void GetOptionInstallFiles(List<FilePath> files, bool doLast)
        {
            //Create a list of all files that are to be installed, based on what the user selected.
            for (int i = 0; i < InstallOptionSteps.Count; i++)
            {
                //Ignore all steps that require a flag that has not been set by previous steps.
                if (FlagsIsSet(InstallOptionSteps[i].HasFlag))
                {
                    if (InstallOptionSteps[i].StepType == InstallStep.StepTypes.Options)
                    {
                        if (InstallOptionSteps[i].OptionList != null && InstallOptionSteps[i].SelectedOptionBinding != -1)
                        {
                            if (InstallOptionSteps[i].OptionList[InstallOptionSteps[i].SelectedOptionBinding].Paths != null)
                            {
                                foreach(var file in InstallOptionSteps[i].OptionList[InstallOptionSteps[i].SelectedOptionBinding].Paths)
                                {
                                    if(file.GetDoLast() == doLast)
                                    {
                                        files.Add(file);
                                    }
                                }
                            }
                        }
                    }
                    else if (InstallOptionSteps[i].StepType == InstallStep.StepTypes.OptionsMultiSelect)
                    {
                        if (InstallOptionSteps[i].OptionList != null)
                        {
                            foreach (var option in InstallOptionSteps[i].OptionList.Where(x => x.IsSelected_OptionMultiSelect && x.Paths != null))
                            {
                                foreach (var file in option.Paths)
                                {
                                    if (file.GetDoLast() == doLast)
                                    {
                                        files.Add(file);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Init()
        {
            if (InstallOptionSteps == null) InstallOptionSteps = new List<InstallStep>();
            if (UiOverrides == null) UiOverrides = new Overrides();
            if (InstallFiles == null) InstallFiles = new List<FilePath>();

            if(InstallOptionSteps.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(InstallOptionSteps[0].HasFlag))
                {
                    throw new InvalidDataException("The first InstallStep cannot have HasFlag set on it.");
                }
            }

            foreach (var step in InstallOptionSteps)
            {
                if(step.OptionList != null)
                {
                    if (step.SelectedOptions == null)
                        step.SelectedOptions = new List<int>();

                    if (step.SelectedOptions.Count > 0)
                    {
                        switch (step.StepType)
                        {
                            case InstallStep.StepTypes.Options:
                                step.SelectedOptionBinding = step.SelectedOptions[0];
                                break;
                            case InstallStep.StepTypes.OptionsMultiSelect:
                                step.SetSelectedOptions(step.SelectedOptions);
                                break;
                        }
                    }
                }
            }
        }

        //UI
        public bool AtFirstInstallStep()
        {
            if (HasInstallSteps())
            {
                return (_currentInstallStep == 0);
            }
            else
            {
                return true;
            }
        }

        public bool HasInstallSteps()
        {
            if (InstallOptionSteps == null) return false;
            return (InstallOptionSteps.Count > 0) ? true : false;
        }

        public InstallStep GetNextInstallStep()
        {
            if(CurrentInstallStep != InstallOptionSteps.Count - 1)
            {
                do
                {
                    //If at last index, then return null.
                    if (CurrentInstallStep >= InstallOptionSteps.Count - 1)
                    {
                        return null;
                    }

                    CurrentInstallStep++;
                }
                while (!FlagsIsSet(InstallOptionSteps[CurrentInstallStep].HasFlag));

                //CurrentInstallStep++;
                StepCountUpdate();
                return InstallOptionSteps[CurrentInstallStep];
            }
            else
            {
                return null;
            }
        }

        public InstallStep GetPreviousInstallStep()
        {
            if (CurrentInstallStep > 0)
            {
                do
                {
                    if (CurrentInstallStep < 0)
                    {
                        return null;
                    }

                    CurrentInstallStep--;
                }
                while (!FlagsIsSet(InstallOptionSteps[CurrentInstallStep].HasFlag));

                StepCountUpdate();
                return InstallOptionSteps[CurrentInstallStep];

                //CurrentInstallStep--;
                //return InstallOptionSteps[CurrentInstallStep];
            }
            else
            {
                return null;
            }
        }

        public InstallStep GetLastValidInstallStep()
        {
            CurrentInstallStep = InstallOptionSteps.Count;
            return GetPreviousInstallStep();
        }

        private bool FlagsIsSet(string flags)
        {
            if (string.IsNullOrWhiteSpace(flags)) return true;

            foreach(var flag in flags.Split(','))
            {
                if (!FlagIsSet(flag.Trim()))
                    return false;
            }

            //Either all flags are true or there are none, so default to true anyway
            return true;
        }

        private bool FlagIsSet(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return true;

            int value = (CurrentInstallStep + 1 <= InstallOptionSteps.Count) ? CurrentInstallStep + 1 : CurrentInstallStep;

            for (int i = 0; i < value; i++)
            {
                if (InstallOptionSteps[i].CheckForFlag(flag) && FlagsIsSet(InstallOptionSteps[i].HasFlag))
                {
                    return true;
                }
            }
            return false;
        }

        //Step Count
        private int CalcTotalValidSteps()
        {
            int value = 0;

            for(int i = 0; i < InstallOptionSteps.Count; i++)
            {
                if (FlagsIsSet(InstallOptionSteps[i].HasFlag))
                {
                    value++;
                }
            }

            return value;
        }

        private int CalcCurrentStepId()
        {
            int value = 0;

            for (int i = 0; i < InstallOptionSteps.Count; i++)
            {
                if (FlagsIsSet(InstallOptionSteps[i].HasFlag))
                {
                    value++;
                }

                if (i == CurrentInstallStep) break;
            }

            return value;
        }

        public void StepCountUpdate()
        {
            NotifyPropertyChanged("CurrentStepString");
        }
    }
    
    public class Overrides
    {
        //Overrides for UI elements such as brushes and preset InstallSteps go in here.
        //Everything should be optional.

        [YAXAttributeFor("DefaultBackground")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string DefaultBackgroundBrush { get; set; }
        [YAXAttributeFor("DefaultFontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string DefaultFontColor { get; set; }
        [YAXAttributeFor("InstallingBackground")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string InstallingBackgroundBrush { get; set; }
        [YAXAttributeFor("InstallingFontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string InstallingFontColor { get; set; }
        [YAXAttributeFor("UninstallingBackground")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string UninstallingBackgroundBrush { get; set; }
        [YAXAttributeFor("UninstallingFontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string UninstallingFontColor { get; set; }

        [YAXAttributeFor("TitleBarBackground")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string TitleBarBackground { get; set; }
        [YAXAttributeFor("TitleBarFontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string TitleBarFontColor { get; set; }

        //Preset InstallStep Overwrites
        [YAXDontSerializeIfNull]
        public InstallStep InstallConfirm { get; set; }
        [YAXDontSerializeIfNull]
        public InstallStep UninstallConfirm { get; set; }
        [YAXDontSerializeIfNull]
        public InstallStep ReinstallInitialPrompt { get; set; }
        [YAXDontSerializeIfNull]
        public InstallStep UpdateInitialPrompt { get; set; }
        [YAXDontSerializeIfNull]
        public InstallStep DowngradeInitialPrompt { get; set; }
    }

    //These are the steps displayed during the install process. They can have choices between different files to install, or just a message (for introduction - first step)
    public class InstallStep : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public enum StepTypes
        {
            Message,
            Options,
            OptionsMultiSelect
        }

        //Preset InstallSteps
        public static InstallStep InstallConfirm
        {
            get
            {
                if (GeneralInfo.InstallerXmlInfo.UiOverrides.InstallConfirm != null) return GeneralInfo.InstallerXmlInfo.UiOverrides.InstallConfirm;

                if (GeneralInfo.InstallerXmlInfo.HasInstallSteps())
                {
                    return new InstallStep()
                    {
                        Name = "Ready to Install",
                        Message = "The mod will now be installed.\n\nIf you need to change anything, then press the Back button, otherwise press Next.",
                        StepType = StepTypes.Message
                    };
                }
                else
                {
                    return new InstallStep()
                    {
                        Name = "Ready to Install",
                        Message = "The mod will now be installed. \n\nPress Next to continue.",
                        StepType = StepTypes.Message
                    };
                }
            }
        }
        public static InstallStep UninstallConfirm
        {
            get
            {
                if (GeneralInfo.InstallerXmlInfo.UiOverrides.UninstallConfirm != null) return GeneralInfo.InstallerXmlInfo.UiOverrides.UninstallConfirm;
                return new InstallStep()
                {
                    Name = "Uninstall?",
                    Message = "The mod will now be uninstalled from the system. If this is not what you want, then press Back. \n\nPress Next to continue.",
                    StepType = StepTypes.Message
                };
            }
        }
        public static InstallStep InitialPromptUpdate
        {
            get
            {
                if (GeneralInfo.InstallerXmlInfo.UiOverrides.UpdateInitialPrompt != null) return LoadInitialPromptOverwrite(GeneralInfo.InstallerXmlInfo.UiOverrides.UpdateInitialPrompt);
                return new InstallStep()
                {
                    Name = "Update Installation",
                    Message = string.Format("Welcome to the installer for {0}. \n\nA previous version of {0} is currently installed ({2}). You may either update the mod ({1}) or uninstall the current version.", GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion),
                    StepType = StepTypes.Options,
                    OptionList = new List<InstallOption>()
                    {
                        new InstallOption()
                        {
                            IsSelected_Option = true,
                            Name = "Update",
                            Tooltip = string.Format("{0} will be updated to {1}.", GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion)
                        },
                        new InstallOption()
                        {
                            Name = "Uninstall",
                            Tooltip = string.Format("{0} {2} will be completely uninstalled.", GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion)
                        }
                    }
                };
            }
        }
        public static InstallStep InitialPromptReinstall
        {
            get
            {
                if (GeneralInfo.InstallerXmlInfo.UiOverrides.ReinstallInitialPrompt != null) return LoadInitialPromptOverwrite(GeneralInfo.InstallerXmlInfo.UiOverrides.ReinstallInitialPrompt);
                return new InstallStep()
                {
                    Name = "Update Installation",
                    Message = string.Format("Welcome to the installer for {0}. \n\nThe mod is currently installed and is up to date ({2}). You may either reinstall or uninstall the mod.", GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion),
                    StepType = StepTypes.Options,
                    OptionList = new List<InstallOption>()
                    {
                        new InstallOption()
                        {
                            IsSelected_Option = true,
                            Name = "Reinstall",
                            Tooltip = "Reinstall the mod. The currently installed version will be uninstalled first."
                        },
                        new InstallOption()
                        {
                            Name = "Uninstall",
                            Tooltip = string.Format("{0} {2} will be completely uninstalled.", GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion)
                        }
                    }
                };
            }
        }
        public static InstallStep InitialPromptReinstall_PrevVer
        {
            get
            {
                if (GeneralInfo.InstallerXmlInfo.UiOverrides.DowngradeInitialPrompt != null) return LoadInitialPromptOverwrite(GeneralInfo.InstallerXmlInfo.UiOverrides.DowngradeInitialPrompt);
                return new InstallStep()
                {
                    Name = "Update Installation",
                    Message = string.Format("Welcome to the installer for {0}. \n\nA newer version of {0} is currently installed ({2}). You may either downgrade the mod ({1}) or just uninstall the current version.", GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion),
                    StepType = StepTypes.Options,
                    OptionList = new List<InstallOption>()
                    {
                        new InstallOption()
                        {
                            IsSelected_Option = true,
                            Name = "Downgrade",
                            Tooltip = string.Format("{0} will be downgraded to {1}. (Version {2} will be uninstalled.)" , GeneralInfo.CurrentModName,GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion)
                        },
                        new InstallOption()
                        {
                            Name = "Uninstall",
                            Tooltip = string.Format("{0} {2} will be completely uninstalled.", GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion)
                        }
                    }
                };
            }
        }


        //View/UI
        [YAXDontSerialize]
        public Visibility OptionListVisibility { get { return (StepType == StepTypes.Options) ? Visibility.Visible : Visibility.Hidden; } }
        [YAXDontSerialize]
        public Visibility OptionMultiSelectListVisibility { get { return (StepType == StepTypes.OptionsMultiSelect) ? Visibility.Visible : Visibility.Hidden; } }
        
        [YAXDontSerialize]
        public int SelectedOptionBinding
        {
            get
            {
                return GetSelectedOption();
            }
            set
            {
                SetSelectedOption(value);
            }
        }
        [YAXDontSerialize]
        public bool _requireSelection
        {
            get
            {
                return Xv2CoreLib.Utils.StringToBool(RequireSelection);
            }
            set
            {
                RequireSelection = Xv2CoreLib.Utils.BoolToString(value);
            }
        }

        //Attributes
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string HasFlag { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string RequireSelection { get; set; }

        //Elements
        [YAXAttributeFor("Type")]
        [YAXSerializeAs("value")]
        public StepTypes StepType { get; set; }
        [YAXAttributeFor("StepName")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }
        [YAXAttributeFor("Message")]
        [YAXSerializeAs("value")]
        public string Message { get; set; }
        [YAXAttributeFor("FontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string FontColor { get; set; }
        [YAXAttributeFor("Background")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string BackgroundImagePath { get; set; }

        [YAXAttributeFor("SelectedOptions")]
        [YAXSerializeAs("Default")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        [YAXDontSerializeIfNull]
        public List<int> SelectedOptions { get; set; }


        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Option")]
        [YAXDontSerializeIfNull]
        public List<InstallOption> OptionList { get; set; } = new List<InstallOption>();

        public void SetSelectedOptions(List<int> index)
        {
            if (OptionList == null) return;
            if (index == null) return;

            for (int i = 0; i < index.Count; i++)
            {
                if (OptionList.Count - 1 >= index[i])
                    OptionList[index[i]].IsSelected_OptionMultiSelect = true;
            }
        }

        public List<int> GetSelectedOptions()
        {
            List<int> selections = new List<int>();
            if (OptionList == null) return selections;

            for (int i = 0; i < OptionList.Count; i++)
            {
                if (OptionList[i].IsSelected_OptionMultiSelect)
                {
                    selections.Add(i);
                }
            }

            return selections;
        }

        private void SetSelectedOption(int index)
        {
            if (OptionList == null) return;

            for(int i = 0; i < OptionList.Count; i++)
            {
                OptionList[i].IsSelected_Option = (i == index) ? true : false;
                OptionList[i].IsSelected_OptionMultiSelect = (i == index) ? true : false;
            }
        }

        private int GetSelectedOption()
        {
            if (OptionList == null) return -1;

            for (int i = 0; i < OptionList.Count; i++)
            {
                if (OptionList[i].IsSelected_Option) return i;
            }

            return -1;
        }

        private string GetBackgroundBrushForOption(int index)
        {
            if (StepType != StepTypes.Options) return null;
            if (OptionList == null) return null;

            if (OptionList.Count - 1 >= index && index >= 0)
            {
                return OptionList[index].BackgroundImagePath;
            }
            else
            {
                return null;
            }
        }

        private string GetTextBrushForOption(int index)
        {
            if (StepType != StepTypes.Options) return null;
            if (OptionList == null) return null;

            if (OptionList.Count - 1 >= index && index >= 0)
            {
                return OptionList[index].FontColor;
            }
            else
            {
                return null;
            }
        }

        public string GetBackgroundBrush()
        {
            string brush = GetBackgroundBrushForOption(SelectedOptionBinding);

            if(string.IsNullOrWhiteSpace(brush))
            {
                brush = BackgroundImagePath;
            }

            return brush;
        }

        public string GetTextBrush()
        {
            string brush = GetTextBrushForOption(SelectedOptionBinding);

            if (string.IsNullOrWhiteSpace(brush))
            {
                brush = FontColor;
            }

            return brush;
        }

        private Visibility GetVisibilityForOption(int index)
        {
            if (OptionList == null) return Visibility.Hidden;

            if (OptionList.Count - 1 >= index)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Hidden;
            }
        }

        private InstallOption GetOption(int index)
        {
            if (OptionList == null) return null;

            if (OptionList.Count - 1 >= index)
            {
                return OptionList[index];
            }
            else
            {
                return null;
            }
        }
        
        private static InstallStep LoadInitialPromptOverwrite(InstallStep step)
        {
            step.Name = string.Format(step.Name, GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion);
            step.Message = string.Format(step.Message, GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion);

            if (step.OptionList == null) throw new InvalidDataException("LoadInitialPromptOverwrite: InstallStep must have options.");
            if (step.StepType != StepTypes.Options) throw new InvalidDataException("LoadInitialPromptOverwrite: InstallStep must be Options StepType");
            if (step.OptionList.Count != 2) throw new InvalidDataException("LoadInitialPromptOverwrite: InstallStep must have exactly 2 options (1st for Reinstall/Update/Downgrade and 2nd for Uninstall).");

            for (int i = 0; i < step.OptionList.Count; i++)
            {
                step.OptionList[i].Name = string.Format(step.OptionList[i].Name, GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion);
                step.OptionList[i].Tooltip = string.Format(step.OptionList[i].Tooltip, GeneralInfo.CurrentModName, GeneralInfo.CurrentModVersion, GeneralInfo.InstalledModVersion);
                
                if(i == 0)
                {
                    step.OptionList[i].IsSelected_Option = true;
                }
            }

            return step;
        }

        //Flag
        /// <summary>
        /// Checks if a single flag has been set by this InstallStep.
        /// </summary>
        public bool CheckForFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return true;

            if(OptionList != null)
            {
                foreach(var option in OptionList)
                {
                    if ((option.IsSelected_OptionMultiSelect && StepType == StepTypes.OptionsMultiSelect || option.IsSelected_Option && StepType == StepTypes.Options) && !string.IsNullOrWhiteSpace(option.SetFlag))
                    {
                        foreach(var setFlag in option.SetFlag.Split(','))
                        {
                            if (string.Equals(setFlag.Trim(), flag)) return true;
                        }
                    }
                }
            }

            return false;
        }

    }

    [YAXSerializeAs("Option")]
    public class InstallOption : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        [YAXDontSerialize]
        private bool _isSelected = false;
        [YAXDontSerialize]
        public bool IsSelected_Option
        {
            get
            {
                return this._isSelected;
            }
            set
            {
                if (value != this._isSelected)
                {
                    this._isSelected = value;
                    NotifyPropertyChanged("IsSelected_Option");
                }
            }
        }
        [YAXDontSerialize]
        private bool _isSelected_OptionMultiSelect = false;
        [YAXDontSerialize]
        public bool IsSelected_OptionMultiSelect
        {
            get
            {
                return this._isSelected_OptionMultiSelect;
            }
            set
            {
                if (value != this._isSelected_OptionMultiSelect)
                {
                    this._isSelected_OptionMultiSelect = value;
                    NotifyPropertyChanged("IsSelected_OptionMultiSelect");
                }
            }
        }


        //Class properties
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string SetFlag { get; set; }
        [YAXAttributeFor("Message")]
        [YAXSerializeAs("value")]
        public string Name { get; set; }
        [YAXAttributeFor("FontColor")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string FontColor { get; set; }
        [YAXAttributeFor("Background")]
        [YAXSerializeAs("Brush")]
        [YAXDontSerializeIfNull]
        public string BackgroundImagePath { get; set; }
        [YAXAttributeFor("ToolTip")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string Tooltip { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "File")]
        public List<FilePath> Paths { get; set; }

        [YAXDontSerialize]
        public int PathCount
        {
            get
            {
                if (Paths != null) return Paths.Count;
                return 0;
            }
        }

    }
    
    [YAXSerializeAs("File")]
    public class FilePath
    {

        [YAXAttributeFor("SourcePath")]
        [YAXSerializeAs("value")]
        public string SourcePath { get; set; }
        [YAXAttributeFor("InstallPath")]
        [YAXSerializeAs("value")]
        [YAXDontSerializeIfNull]
        public string InstallPath { get; set; }

        //optional:
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string HasFlag { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string Overwrite { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string Type { get; set; }
        [YAXAttributeForClass]
        [YAXDontSerializeIfNull]
        public string DoLast { get; set; }

        public bool AllowOverwrite()
        {
            if(String.IsNullOrWhiteSpace(Overwrite))
            {
                return false;
            }
            else if(Overwrite.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    
        public FileType GetFileType()
        {
            FileType type = ParseFileTypeValue();

            //If SourcePath is for a special file type, then return that type.
            if(Path.GetExtension(SourcePath) == Xv2CoreLib.EffectContainer.EffectContainerFile.ZipExtension)
            {
                return FileType.VfxPackage;
            }
            else if (Path.GetExtension(SourcePath) == Xv2CoreLib.ACB_NEW.ACB_File.MUSIC_PACKAGE_EXTENSION)
            {
                return FileType.MusicPackage;
            }

            if (type == FileType.Default)
            {
                //Simulates the original behavior before FileType was added. Will infer type from SourcePath.

                if (SourcePath.EndsWith("/") || SourcePath.EndsWith(@"\")) return FileType.CopyDir;
                return (Path.GetExtension(SourcePath) != ".xml") ? FileType.CopyFile : FileType.XML;
            }
            else
            {
                return type;
            }
        }

        private FileType ParseFileTypeValue()
        {
            if (string.IsNullOrWhiteSpace(Type)) return FileType.Default;

            FileType type;
            
            if(!Enum.TryParse(Type, out type))
            {
                return FileType.Default;
            }

            return type;
        }
        
        public bool GetDoLast()
        {
            if (String.IsNullOrWhiteSpace(DoLast)) return false;
            return (DoLast.ToLower() == "true");
        }
    }

    public enum FileType
    {
        Default = 0,
        XML,
        Binary,
        CopyFile,
        CopyDir,
        VfxPackage,
        MusicPackage
    }
}
