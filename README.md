# Print And Scan 4 Ukraine

First time diving into WPF and MVVM.

This application is intended to be used to keep track of packages, along with the sender/recipient/contents/weight/cost, that are shipping across the ocean.

Currently uses my personal MariaDB server, but I wrote this to be as open to changes as possible.

<img src="https://github.com/miroppb/PrintAndScan4Ukraine/blob/master/PrintAndScan4Ukraine/Images/ScanWindow.png?raw=true" alt="Screenshot" width="700"/>

## Requirements
Written for .NET 7, but should be backwards compatible in most cases.

## Setup
Secrets.cs
```csharp
public class Secrets
{
    public static MySqlConnection GetConnectionString() => new MySqlConnection($"Server={MySqlUrl};Database={MySqlDb};Uid={MySqlUsername};Pwd={MySqlPassword};");

    internal static string GetMySQLTable() => MySqlTable;

    private const string MySqlUrl = "###";
    private const string MySqlUsername = "###";
    private const string MySqlPassword = "###";
    private const string MySqlDb = "###";
    private const string MySqlTable = "###";
}
```

Database:
```sql
CREATE DATABASE IF NOT EXISTS `printandscan4ukraine`;
USE `printandscan4ukraine`;

CREATE TABLE IF NOT EXISTS `access_type` (
  `type` tinyint(3) unsigned DEFAULT NULL,
  `access` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='For reference';

CREATE TABLE IF NOT EXISTS `packages` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `packageid` text NOT NULL,
  `sender_name` text DEFAULT NULL,
  `sender_address` text DEFAULT NULL,
  `sender_phone` varchar(15) DEFAULT NULL,
  `recipient_name` text DEFAULT NULL,
  `recipient_address` text DEFAULT NULL,
  `recipient_phone` varchar(15) DEFAULT NULL,
  `weight` text DEFAULT NULL,
  `contents` text DEFAULT NULL,
  `value` int(11) DEFAULT NULL,
  `delivery` int(11) DEFAULT NULL,
  `insurance` int(11) DEFAULT NULL,
  `other` int(11) DEFAULT NULL,
  `date_added` datetime DEFAULT NULL,
  `date_shipped` datetime DEFAULT NULL,
  `removed` tinyint(3) unsigned NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `package_status` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `packageid` text NOT NULL,
  `createddate` datetime NOT NULL,
  `status` tinyint(3) unsigned NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `users` (
  `id` smallint(5) unsigned NOT NULL AUTO_INCREMENT,
  `computername` text NOT NULL,
  `access` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `comment` text DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `access_type` (
  `type` tinyint(3) unsigned DEFAULT NULL,
  `access` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='For reference';

INSERT INTO `access_type` (`type`, `access`) VALUES
	(0, 'None'),
	(1, 'Edit Sender'),
	(2, 'Edit Recipient'),
  (4, 'See Sender'),
	(8, 'Add New'),
	(16, 'Mark As Shipped'),
	(32, 'Mark As Arrived'),
	(64, 'Mark As Delivered'),
	(128, 'Print Labels');

CREATE TABLE IF NOT EXISTS `status_type` (
  `id` int(1) unsigned NOT NULL DEFAULT 0,
  `name` text DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='For reference';

INSERT INTO `status_type` (`id`, `name`) VALUES
	(1, 'Scanned New'),
	(2, 'Scanned Shipped'),
	(3, 'Scanned Arrived'),
	(4, 'Scanned Delivered');
```