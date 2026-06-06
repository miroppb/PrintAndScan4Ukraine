using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using System.Collections.Generic;

namespace PrintAndScan4Ukraine.Extensions
{
    public static class TrimExtension
    {
        /// <summary>
        /// Trims all string properties of the Package object, including the contents of the Recipient_Contents list.
        /// </summary>
        /// <param name="pkg">Package object to trim</param>
        /// <returns>Trimmed Package object</returns>
        public static Package Trim(this Package pkg)
        {
            //go through each string property and trim it
            pkg.Sender_Name = pkg.Sender_Name?.Trim()!;
            pkg.Sender_Address = pkg.Sender_Address?.Trim()!;
            pkg.Sender_Phone = pkg.Sender_Phone?.Trim()!;
            pkg.Recipient_Name = pkg.Recipient_Name?.Trim()!;
            pkg.Recipient_Address = pkg.Recipient_Address?.Trim()!;
            pkg.Recipient_Phone = pkg.Recipient_Phone?.Trim()!;

            pkg.Recipient_Contents.ForEach(x => x.Name = x.Name?.Trim()!);
            if (pkg.Recipient_Contents.Count > 0)
                pkg.Contents = JsonConvert.SerializeObject(pkg.Recipient_Contents);

            return pkg;
        }
    }
}
