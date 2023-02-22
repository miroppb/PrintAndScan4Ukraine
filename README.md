# Print And Scan 4 Ukraine

First time diving into WPF and MVVM.

This application is intended to be used to keep track of packages, along with the sender/recipient/contents/weight/cost, that are shipping across the ocean.

Currently uses my personal MariaDB server, but I wrote this to be as open to changes as possible.

<img src="https://github.com/miroppb/PrintAndScan4Ukraine/blob/master/PrintAndScan4Ukraine/Images/ScanWindow.png?raw=true" alt="Screenshot" width="700"/>

### Setup
---
Secrets.cs
```csharp
public class Secrets
{
    public static MySqlConnection GetConnectionString()
    {
        return new MySqlConnection($"Server={MySqlUrl};Database={MySqlDb};Uid={MySqlUsername};Pwd={MySqlPassword};");
    }

    internal static NetworkCredential GetFTPCredentials()
    {
        return new NetworkCredential("username", "password"); //used for checking updates
    }

    internal static string GetFTPURL()
    {
        return "ftp://urltoftpserver.com/update/location/file.zip";
    }

    public const string MySqlUrl = "###";
    public const string MySqlUsername = "###";
    public const string MySqlPassword = "###";
    public const string MySqlDb = "###";
    public const string MySqlTable = "###";
}
```

Database:
```sql
CREATE DATABASE IF NOT EXISTS `printandscan4ukraine`;
USE `printandscan4ukraine`;

CREATE TABLE IF NOT EXISTS `packages` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `packageid` int(11) NOT NULL,
  `sender_name` text DEFAULT NULL,
  `sender_address` text DEFAULT NULL,
  `sender_phone` varchar(15) DEFAULT '0',
  `recipient_name` text DEFAULT NULL,
  `recipient_address` text DEFAULT NULL,
  `recipient_phone` varchar(15) DEFAULT '0',
  `weight` text DEFAULT NULL,
  `contents` text DEFAULT NULL,
  `cost` int(11) DEFAULT NULL,
  `delivery` int(11) DEFAULT NULL,
  `insurance` int(11) DEFAULT NULL,
  `other` int(11) DEFAULT NULL,
  `date_shipped` datetime DEFAULT NULL,
  `date_added` datetime DEFAULT NULL,
  `removed` tinyint(3) unsigned NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4;
```