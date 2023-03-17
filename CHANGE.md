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