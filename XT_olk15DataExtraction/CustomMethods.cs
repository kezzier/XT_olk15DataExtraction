using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using FileSignatures;
using XTensions;
using System.Text.RegularExpressions;

namespace XTension_template
{
    internal class CustomMethods
    {
        public static List<string[]> ParseDB(string dbPath)
        {
            var dbResults = new List<string[]>();
            using (var conn = new SQLiteConnection(@"Data Source=" + dbPath))
            {
                conn.Open();           
                var command = conn.CreateCommand();
                command.CommandText = "select m.Record_RecordID, m.PathToDataFile, m.Message_NormalizedSubject, m.Message_HasAttachment, b.PathToDataFile from Mail as m " +
                    "join Mail_OwnedBlocks as mob on m.Record_RecordID = mob.Record_RecordID join Blocks as b on b.BlockID = mob.BlockID";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var m_Record_RecordID = reader.GetInt64(0).ToString();
                            var m_PathToDataFile = Uri.UnescapeDataString(reader.GetString(1));
                            var m_Message_NormalizedSubject = "NoSubject";
                            try
                            {
                                m_Message_NormalizedSubject = reader.GetString(2);
                            } catch {}
                            var m_Message_HasAttachment = reader.GetBoolean(3).ToString();
                            var b_PathToDataFile = "NoAttachment";
                            if (m_Message_HasAttachment == "True") b_PathToDataFile = Uri.UnescapeDataString(reader.GetString(4));
                            dbResults.Add(new string[] { m_Record_RecordID, m_PathToDataFile, m_Message_NormalizedSubject, m_Message_HasAttachment, b_PathToDataFile });
                        }
                    }
                }

            }
            return dbResults;
        }

        public static List<string> ProcessDbResults(string dataPath, List<string[]> dbResults, string attResultPath)
        {
            HelperMethods.ShowProgress("Extracting attachments", ProgressIndicatorOptions.ShowImmediately);
            int progress = 0;

            var csvContent = new List<string> { String.Join(", ", new string[] { "MailRecord", "MailPath", "MailSubject", "AttachmentPath", "AttFileName" }) };
            if (dbResults.Count > 0)
            {
                int total = dbResults.Count;
                foreach (string[] record in dbResults)
                {
                    if (HelperMethods.ShouldStop())
                    {
                        HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}:[XT_olk15DataExtraction] Stopping processing of current database and its attachments.");
                        return csvContent;
                    }

                    if (record[3] == "True")
                    {
                        var attFileName = "AttachmentNotFound";
                        if (record[4].Contains(".olk15MsgAttachment")) attFileName = ExtractAttachment($"{dataPath}\\Message Attachments\\{record[4].Split(new char[] { '/' })[2]}", attResultPath);
                        csvContent.Add(String.Join(", ", new string[] { record[0], record[1], record[2], record[4], attFileName }));
                    }
                    else csvContent.Add(String.Join(", ", new string[] { record[0], record[1], record[2], "no attachment", "no attachment" }));

                    progress++;
                    HelperMethods.SetProgressPercentage((uint)(100 * progress / total));
                }
            }
            else
            {
                var attachments = Directory.GetFiles($"{dataPath}\\Message Attachments\\");
                var total = attachments.Length;
                foreach (var att in attachments)
                {
                    if (HelperMethods.ShouldStop())
                    {
                        HelperMethods.OutputMessage($"{DateTime.Now.TimeOfDay}:[XT_olk15DataExtraction] Stopping processing of attachments.");
                        return csvContent;
                    }

                    var attFileName = ExtractAttachment(att, attResultPath);
                    csvContent.Add(String.Join(", ", new string[] { "unknown", "unknown", "unknown", "outside of database", attFileName }));

                    progress++;
                    HelperMethods.SetProgressPercentage((uint)(100 * progress / total));

                }
            }

            HelperMethods.HideProgress();
            return csvContent;
        }

        private static string ExtractAttachment(string attPath, string attResultPath)
        {
            try
            {
                var fileContent = System.IO.File.ReadAllText(attPath);
                var lines = fileContent.Split(new char[] { '\r' }).ToArray();

                var data = GetBase64(lines);
                var attFileName = GetFileName(lines, data);

                var attPathSplit = attPath.Split(new char[] { '\\' });
                var attID = attPathSplit[attPathSplit.Length - 1].Split(new char[] { '.' })[0];
                try
                {
                    var outputfile = attResultPath + attID + "_" + attFileName;
                    System.IO.File.WriteAllText(outputfile, BitConverter.ToString((Convert.FromBase64String(data))));
                }
                catch
                {
                    attFileName += "_AttachmentExtractionError";
                }
                return attFileName;
            }
            catch
            {
                return "AttachmentExtractionError";
            }
        }

        private static string GetBase64(string[] lines)
        {
            string[] data;
            var encodingLine = Array.FindIndex(lines, row => row.Contains("Content-transfer-encoding: base64"));
            try
            {
                var startIndex = encodingLine;
                foreach (var x in lines.Skip(encodingLine + 1).ToArray())
                {
                    startIndex += 1;
                    if (x != "") break;
                }
                data = lines.Skip(startIndex).ToArray();
            }
            catch
            {
                data = lines.Skip(encodingLine + 2).ToArray();
            }
            return String.Join("\r", data);
        }

        private static string GetFileName(string[] lines, string data)
        {
            try
            {
                var filenameLine = new string(Array.Find(lines, s => s.Contains("filename")).Skip(1).ToArray());
                string splitChar;
                if (filenameLine.Contains("UTF-8")) splitChar = "\'\'";
                else splitChar = "\"";
                return Regex.Replace(Uri.UnescapeDataString(Encoding.UTF8.GetString(Encoding.Default.GetBytes(filenameLine)).Split(splitChar.ToCharArray()).ToArray()[1]), "[^a-zA-Z0-9% ._]", string.Empty);
            } catch
            {
                var inspector = new FileFormatInspector();
                byte[] byteArray = Encoding.UTF8.GetBytes(data);
                MemoryStream stream = new MemoryStream(byteArray);
                var extension = "";
                try
                {
                    extension = inspector.DetermineFileFormat(stream).Extension;
                }
                catch { }
                return "NoFilename" + extension;
            }
        }

        public static void WriteTempFile(IntPtr _hVolume, int nItemID, string path)
        {
            IntPtr hItem = HelperMethods.OpenItem(_hVolume, nItemID);
            byte[] file = HelperMethods.ReadItem(hItem);
            Stream stream = new MemoryStream(file);
            System.IO.File.WriteAllBytes(path, file);
        }

        public static Profile GetProfile(IntPtr _hVolume, IntPtr _hEvidence, string fullPath)
        {
            var userName = "parsed";
            var profileName = "noprofile";
            if (!fullPath.Equals("noprofile"))
            {
                var splitChar = "\\";
                var splitPath = fullPath.Split(splitChar.ToCharArray());
                userName = splitPath[Array.IndexOf(splitPath, "Users") + 1];
                profileName = splitPath[Array.IndexOf(splitPath, "Outlook 15 Profiles") + 1];
            }
            var eoName = HelperMethods.GetEvidenceObjectProperties(_hEvidence).extendedTitle;

            EvidenceObject curr_eo = XT_olk15DataExtraction.curr_case.Find(eo => eo._hVolume.Equals(_hVolume));
            User curr_u = null;
            Profile curr_p = null;
            if (curr_eo != null)
            {
                curr_u = curr_eo.Users.Find(u => u.Name.Equals(userName));
                if (curr_u != null)
                {
                    curr_p = curr_u.Profiles.Find(p => p.Name.Equals(profileName));
                    if (curr_p == null)
                    {
                        curr_p = new Profile(profileName, $"{Path.GetTempPath()}olk15DataExtraction\\{eoName}\\{userName}\\{profileName}");
                        Directory.CreateDirectory($"{Path.GetTempPath()}olk15DataExtraction\\{eoName}\\{userName}\\{profileName}\\Message Attachments");
                        curr_u.Profiles.Add(curr_p);
                    }
                }
                else
                {
                    curr_p = new Profile(profileName, $"{Path.GetTempPath()}olk15DataExtraction\\{eoName}\\{userName}\\{profileName}");
                    Directory.CreateDirectory($"{Path.GetTempPath()}olk15DataExtraction\\{eoName}\\{userName}\\{profileName}\\Message Attachments");
                    curr_u = new User(userName, curr_p);
                    curr_eo.Users.Add(curr_u);
                }
            }
            else
            {
                curr_p = new Profile(profileName, $"{Path.GetTempPath()}olk15DataExtraction\\{eoName}\\{userName}\\{profileName}");
                Directory.CreateDirectory($"{Path.GetTempPath()}olk15DataExtraction\\{eoName}\\{userName}\\{profileName}\\Message Attachments");
                curr_u = new User(userName, curr_p);
                curr_eo = new EvidenceObject(_hVolume, eoName, curr_u);
                XT_olk15DataExtraction.curr_case.Add(curr_eo);
            }

            return curr_p;
        }
    }
}
