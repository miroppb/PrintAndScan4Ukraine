v. 1.5.6.12

- Fixed bug that was causing User info to get removed from the database

v. 1.5.6.11

- When Scanning as ship/arrive/deliver, the Done button is disabled while sending and exporting
- Added a heartbeat, to check into the database
- Added some more logging throughout the application
- Trying to resolve the log file not uploading when it's too large

v. 1.5.6.10

- Fixed a bug where the new package ID wasn't being saved in some cases

v. 1.5.6.9

- Prevent saving if package ID is empty.

v. 1.5.6.8

- Fixed a bug where while adding a new package, application didn't save the new package
- When scanning an existing package, list now scrolls to the selected item

v. 1.5.6.7

- When editing package IDs and scanning new, prompt when adding a new and not while searching
- When editing package IDs, checks format, and prompts if not in correct format

v. 1.5.6.6

- Added some more logging in various spots
- Check if system time is different from the server time, and prompt user to resync

v. 1.5.6.5

- Wasn't logging the package ID change in the right spot

v. 1.5.6.4

- Fixed Regex that was matching new package IDs

v. 1.5.6.3

- Fixed some bugs with the new ability to change package IDs.
- Re-enabled time resyncing when Window opens

v. 1.5.6.2

- Fixed bug that caused application to close if search-selected package isn't on the list

v. 1.5.6.1

- Added ability to double-click package in search to open it in Main-Window
- Ability to Search Archive or not

v. 1.5.6.0

- Added a Search Window, where the user can search by Package ID or by Sender's Name
- Fixed a bug when changing Package IDs
- Fixed a bug on the History Window, where the main package would get updated when the user clicked an old package but then X-closed the window
- Increased the digit count from 7 to 9 on labels

v. 1.5.5.3

- Added ability to change Package ID (with permission)

v. 1.5.5.2

- No change

v. 1.5.5.1

- Attempt to detect if computer is going to sleep, and to stop/restart timers

v. 1.5.5.0

- Added ability to scan mulitple packages into the database.
- When typing a new barcode to enter, the barcode is now visibile (for entering manually)

v. 1.5.4.1

- When Shipping packages, for those that are missing from the list, a document gets genarated with last status updates
- When printing labels, added validation for the starting and ending numbers to be 1000000 < x < 9999999
- When printing labels, added a button to cancel printing (needs further testing).

v. 1.5.4.0

- Added more options to allow exporting to excel using specific dates and statuses.
- Package updates

v. 1.5.3.7

-  Added some messages before exporting, explaining what's about to happen, and asking if user wants to continue

v. 1.5.3.6

- Regex that was checking the package format, didn't check for ending. Fixed

v. 1.5.3.5

- Added more info to the message when scanned package number isn't in correct format

v. 1.5.3.4

- Fixed list of barcodes scanned not being reset
- Added Delivery to Excel, showing checkmark when Delivery is checked

v. 1.5.3.3

- Added 2 small logs, for debuging issues

v. 1.5.3.2

- Added Delivery Checkbox
- Detects Russian language on PC, and prompts to switch if current language is English

v. 1.5.3.1

- Removed redundant log when showing history
- When saving Removed records, don't log everything, just the IDs
- libmiroppb.UploadLog fixed not trimming space at the beginning

v. 1.5.3.0

- Moved Excel table down 5 rows, to add extra info on top
- Now checks to make sure new package IDs start correctly (previously didn't check beginning)

v. 1.5.2.9

- Error when unable to insert access into db
- Separated Save and ExecuteSave (button) to show a message when saving manually

v. 1.5.2.8

- Moving towards a better MVVM model

v. 1.5.2.7

- Print Button turned out to not be implemented :D

v. 1.5.2.5

- Fixed error during exporting to excel

v. 1.5.2.4

- One single ? can make the difference :D
- DapperPlus is now gone

v. 1.5.2.3

- Dapper Contrib doesn't like Package.cs

v. 1.5.2.2

- Stopped using DapperPlus again...

v. 1.5.2.1

- Reverted back to DapperPlus for now

v. 1.5.2.0

- Removed DapperPlus and started using Dapper.Contrib
- Added some more translations (to Mark As Shipped Window)
- Mark As Shipped checks which packages were scanned but aren't on list

v. 1.5.1.2

- Updated packages

v. 1.5.1.1

- Reverted Add New code
- Added more specification to Verify If Exists

v. 1.5.1.0

- Double-check package number before inserting New
- Checks format CV#######US before inserting

v. 1.5.0.12

- Logging doesn't delete files for some reason

v. 1.5.0.11

- Testing for potential cause for a freeze
	- Stopped logging list of packages in beginning

v. 1.5.0.10

- Still trying to minimize saving to db
- Trying to make logs more clear :)

v. 1.5.0.9

- Trying to minimize redunadant saves
- Main window shows version number
- Upon connection, saves version number to db

v. 1.5.0.8

- Previous change wasn't actually implemented :)

v. 1.5.0.7

- Not save list of packages during refresh to log

v. 1.5.0.6

- Bug fixes, more-or-less
- Swapped spots of Value and Weight

v. 1.5.0.5

- Fixed no access being set

v. 1.5.0.4

- Translation fix for sender

v. 1.5.0.3

- Users can always open Scan now, but then fields are hidden depending on their access
- Fixed bug with Address fields
- Removed the package refresh for current package for now

v. 1.5.0.2

- If the user has no access, or is offline, the application won't allow them to continue.
- Application will save the computer name if it doesn't exist in the user's table

v. 1.5.0.1

- Removed exporting from Delivered screen

v. 1.5.0.0

- Massive update
- Moved status updates to separate table
- Added ability to mark packages that have arrived and were delivered
- Added access to application by computer name
	- If you don't have any access, the main Print/Scan buttons are disabled
	- Types: None, SeeSender, EditSender, EditRecipient, AddNew, Ship, Arrive, Deliver, Print(Labels)
	- Could be used to separate different 
- Moved some Menu items around to be more intuative and not hard-to-find
- Ability to Export packages that were shipped but haven't arrived
- Fixes to hiding some fields that were showing wrong text when a package wasn't selected
	-TODO: Still need to fix the status not updating when changing language

v. 1.4.5.1

- Prevent duplicates from being scanned as shipped

v. 1.4.5.0

- Moved statuses to separate table, to make any updates be tracked

v. 1.4.0.0

- Added Localization (Russian)
- Prevent duplicate records from being added
- Save in strategic places, to lower the amount of data being sent
- Refreshes content in background if it was updated in the db/other instances

v. 1.3.0.5

- Changed url for db

v. 1.3.0.4

- If initial loading of packages fails, logs attempt

v. 1.3.0.3

- Added logging to Print window
- Checks for updates at start of the Scan window

v. 1.3.0.2

- Changed MySql.Data to MySqlConnector

v. 1.3.0.1

- Updates to Updater and EPPlus
- Added some more logging
- Allowing only one instance to be opened

v. 1.3.0.0

- Implemented a history window, to search for previous shipments from a sender

v. 1.2.5.3

- Upload logs when closing the main window

v. 1.2.5.1

- Resized Sender Address box
- While typing an address, prevent the Scan New window from opening
- If Scan New window was opened, but nothing was entered, application doesn't insert/reload the list

v. 1.2.5.0

- Fixed Contents being empty after new update
- Made sure that Package ID isn't an empty string

v. 1.2.0.0

- Made Phone/Weight/Value default to blank fields
- Reworked the Print Window

v. 1.1.1.1

- Fixed Cost/Value

v. 1.1.1.0

- Fixed not inserting new

v. 1.1.0.0

- Fixed not updating after shipping

v. 1.0.9.0

- Trying to fix Marking as Shipped

v. 1.0.8.1

- Fixed Date formatting in Excel

v. 1.0.8.0

- Changed exporting to Russian

v. 1.0.6.0 - 1.0.7.0

- Fixed Export	
- Changed Cost to Value

v. 1.0.5.1

- Fixed list Selection

v. 1.0.5.0

- Fixed pressing Enter on list

v. 1.0.4.1

- Fixed saving before New

v. 1.0.4.0

- Fixed online check