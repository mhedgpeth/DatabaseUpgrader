# DatabaseUpgrader
A simple project to manage schema versions in SQL Server. Compatible with chef.

Doesn't currently use transactions, so you should be sure that your schema is good.

# Overview
This process can be used standalone or with chef. Create a scripts folder in your source and create sql files in Sql Management Studio that fit the pattern [DatabaseVersion].sql. So you'll end up with 1.sql, 2.sql, etc.

When the program runs, it checks the Version table for the latest version that is in the table. It then checks if any files are greater than that version, and runs those sql files if out of date.

# Usage
## Create the Version Table for Schema Version Tracking
If you have a fresh database, use this to add a version table which will get you started

```
DatabaseUpgrader.exe --initialize "-cServer=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword"
```

## Check if a Database Upgrade is Needed
This will be useful for a not-if condition in chef:

```
DatabaseUpgrader.exe --checkIfUpgradeRequired "-cServer=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword" -dscripts
```

Note the quotes so you can have a space in the connection string. Also, the scripts will be found in the scripts directory, relative to the executable.

## Upgrade Database to Latest Version
This will perform the upgrade, or do nothing if the schema is up to date.

```
DatabaseUpgrader.exe "-cServer=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword" -dscripts -v1.2.3.4
```

The upgrader will insert the version of the software into the version table, so you have a good history of what has been done on the database.
