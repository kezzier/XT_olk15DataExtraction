# XT_olk15DataExtraction

X-Ways X-Tension to extract mail attachments from information files found in the Data folder of the Outlook container in macOS.

## Requirements and Installation

This extension is compatible with **X-Ways (x64) v20.3 or higher**.

* **DOWNLOAD THE APP IN [RELEASES](https://git/laura.rondelez/xt_olk15dataextraction/-/releases)**

Add the X-Tension in X-Ways:
  1. In X-Ways, go to X-Tensions (yellow cogwheel)
  2. Click the "+" button
  3. Reference the XT_olk15DataExtraction dll
  4. When prompted for a 'force unload', choose "No"

Add the other dll files to your X-Ways Forensics folder (same folder as the xwforensics exe).

## Usage

There are two ways to use the X-Tension: by right clicking in the directory browser (check only selected files) OR by performing a Refine Volume Snapshot (RVS).

Right click:
1. Select the files you want to check
2. Right click
3. 'Run X-Tensions...' (yellow cogwheel)
4. Select the XT_olk15DataExtraction dll and click OK or press Enter

Refine Volume Snapshot:
1. Open the RVS selection window
2. Check 'Run X-Tensions...'
3. Select the XT_olk15DataExtraction dll
4. Make sure to include the volume with the Outlook directory in it
5. Run the RVS

You will be prompted for a directory path for the output (default is the current user's Desktop).

## Output

A directory will be added to the X-Ways case containing the attachments for each database that was processed as well as a csv file with its info on: MailRecord, MailPath, MailSubject, AttachmentPath, AttFileName. The directory cannot be added to the case again if it already exists in the case.

You can also find the results in the folder _\<case name>\_olk15Attachments_ in the chosen output path (default is the current user's Desktop), no matter if the import was successful or not.

## Database

Database used: _\Macintosh HD - Data\Users\\\<user>\Library\Group Containers\UBF8T346G9.Office\Outlook\Outlook 15 Profiles\\\<profile>\Data\Outlook.sqlite_

Tables and columns used:
* Mail: Record_RecordID (PK), PathToDataFile (.olk15Message), Message_HasAttachment (boolean), Message_NormalizedSubject (mail subject line)
* Mail_OwnedBlocks: Record_RecordID (FK), BlockID (binary)
* Blocks: BlockID (FK), PathToDataFile (.olk15MsgAttachment)