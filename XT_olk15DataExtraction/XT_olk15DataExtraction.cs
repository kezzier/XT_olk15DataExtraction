using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.IO;
using XTension_template;
using System.Text.RegularExpressions;

namespace XTensions
{
    public class XT_olk15DataExtraction
    {
        public static bool running;
        public static string resultPath;
        public static List<EvidenceObject> curr_case;
        private static IntPtr _hVolume;
        private static IntPtr _hEvidence;

        ///////////////////////////////////////////////////////////////
        /// X-Ways X-Tension calls
        /// 

        [DllExport]
        public static int XT_Init(CallerInformation nVersion, CallingProgramCapabilities nFlags, IntPtr hMainWnd, IntPtr lpReserved)
        {
            // If importing functions fails, we return -1 to prevent further use of the X-Tension.
            if (!ImportedMethods.Import())
            {
                HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}: [XT_olk15DataExtraction] Failed to import API methods.");
                return -1;
            }

            running = false;
            curr_case = new List<EvidenceObject>();
            resultPath = null;

            HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}: [XT_olk15DataExtraction] Starting X-Tension");

            return 1;
        }

        [DllExport]
        public static int XT_About(IntPtr hParentWnd, IntPtr lpReserved)
        {
            HelperMethods.OutputMessage("[XT_olk15DataExtraction] X-Ways X-Tension to extract mail attachments from information files found in the Data folder of the Outlook container in macOS.");
            return 0;
        }

        /// <summary>
        /// X-Tension must be initiated through ...
        /// ...
        /// </summary>
        /// <param name="nOpType">How X-Tension is initiated.</param> 
        [DllExport]
        public static int XT_Prepare(IntPtr hVolume, IntPtr hEvidence, XTensionActionSource nOpType, IntPtr lpReserved)
        {
            if (nOpType == XTensionActionSource.VolumeSnapshotRefinement || nOpType == XTensionActionSource.DirectoryBrowserContextMenu)
            {
                running = true;
                _hVolume = hVolume;
                _hEvidence = hEvidence;
                if (resultPath == null)
                {
                    resultPath = HelperMethods.GetUserInput("Enter output path or Cancel for default (Desktop)", "");
                    if (resultPath == "Clicked cancel") resultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{HelperMethods.GetCaseProperties().CaseTitle}_olk15Attachments");
                }
                return 1;
            }
            else
            {
                HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}:[XT_olk15DataExtraction] This X-Tension can only be run from refine volume snapshot or by right clicking in the directory browser.");
                return -3;
            }
        }

        /// <summary>
        /// ...
        /// </summary>
        [DllExport]
        public static long XT_ProcessItem(int nItemID, IntPtr lpReserved)
        {
            if (HelperMethods.ShouldStop())
            {
                HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}:[XT_olk15DataExtraction] Stopping X-Tension");
                running = false;
                return -1;
            }

            var fullPath = HelperMethods.GetFullPath(nItemID);
            if (Regex.IsMatch(fullPath, @"^.+\\Library\\Group Containers\\UBF8T346G9\.Office\\Outlook\\Outlook 15 Profiles\\[\w\s]+\\Data\\Message Attachments\\.+$"))
            {
                Profile profile = CustomMethods.GetProfile(_hVolume, _hEvidence, fullPath);
                CustomMethods.WriteTempFile(_hVolume, nItemID, profile.DataPath + "\\Message Attachments\\" + HelperMethods.GetItemName(nItemID));
            }
            else if (Regex.IsMatch(fullPath, @"^.+\\Library\\Group Containers\\UBF8T346G9\.Office\\Outlook\\Outlook 15 Profiles\\[\w\s]+\\Data\\Outlook\.sqlite$"))
            {
                Profile profile = CustomMethods.GetProfile(_hVolume, _hEvidence, fullPath);
                CustomMethods.WriteTempFile(_hVolume, nItemID, profile.DataPath + "\\Outlook.sqlite");
            }
            else if(fullPath.EndsWith(".olk15MsgAttachment"))
            {
                Profile profile = CustomMethods.GetProfile(_hVolume, _hEvidence, "noprofile");
                CustomMethods.WriteTempFile(_hVolume, nItemID, profile.DataPath + "\\Message Attachments\\" + HelperMethods.GetItemName(nItemID));
            }

            return 0;
        }

        /// <summary>
        /// After all items are processed.
        /// </summary>
        [DllExport]
        public static int XT_Finalize(IntPtr hVolume, IntPtr hEvidence, XTensionActionSource nOpType, IntPtr lpReserved)
        {
            return 0;
        }

        [DllExport]
        public static int XT_Done(IntPtr lpReserved)
        {
            if (running)
            {
                foreach (EvidenceObject eo in curr_case)
                {
                    foreach (User u in eo.Users)
                    {
                        foreach (Profile p in u.Profiles)
                        {
                            var dbResults = new List<string[]>();
                            if (File.Exists($"{p.DataPath}\\Outlook.sqlite"))
                            {
                                HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}:[XT_olk15DataExtraction] Parsing Outlook.sqlite for {eo.Name}/{u.Name}/{p.Name}...");
                                dbResults = CustomMethods.ParseDB($"{p.DataPath}\\Outlook.sqlite");
                            }
                            Directory.CreateDirectory(resultPath);
                            string attResultPath = $"{resultPath}\\{eo.Name}\\{u.Name}_{p.Name}_attachments\\";
                            Directory.CreateDirectory(attResultPath);
                            HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}:[XT_olk15DataExtraction] Extracting attachments for {eo.Name}/{u.Name}/{p.Name}...");
                            var csvContent = CustomMethods.ProcessDbResults(p.DataPath, dbResults, attResultPath);
                            System.IO.File.WriteAllLines($"{resultPath}\\{u.Name}_{p.Name}_olk15DataExtraction.csv", csvContent);
                        }
                    }                    
                }
                Directory.Delete($"{Path.GetTempPath()}olk15DataExtraction", true);

                if (Directory.Exists(resultPath))
                {
                    if (HelperMethods.CreateEvidenceObject(EvidenceObjectType.Directory, resultPath) != IntPtr.Zero) HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}: [XT_olk15DataExtraction] Resulting attachment files have been added to case and {resultPath}.");
                    else HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}: [XT_olk15DataExtraction] Unable to add resulting attachment files to case. You can find the files in {resultPath}.");
                }
                else HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}: [XT_olk15DataExtraction] No database or attachments found.");

                    resultPath = null;

                HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}: [XT_olk15DataExtraction] X-Tension finished");
            }
            else HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}:[XT_olk15DataExtraction] Dll loaded!");
            return 0;
        }

    }
}
