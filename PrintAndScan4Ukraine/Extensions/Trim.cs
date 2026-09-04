using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;

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
            // Trim each string property but only assign back when the trimmed value differs
            var s = pkg.Sender_Name?.Trim();
            if (s != pkg.Sender_Name) pkg.Sender_Name = s!;

            var sa = pkg.Sender_Address?.Trim();
            if (sa != pkg.Sender_Address) pkg.Sender_Address = sa!;

            var sp = pkg.Sender_Phone?.Trim();
            if (sp != pkg.Sender_Phone) pkg.Sender_Phone = sp!;

            var rn = pkg.Recipient_Name?.Trim();
            if (rn != pkg.Recipient_Name) pkg.Recipient_Name = rn!;

            var ra = pkg.Recipient_Address?.Trim();
            if (ra != pkg.Recipient_Address) pkg.Recipient_Address = ra!;

            var rp = pkg.Recipient_Phone?.Trim();
            if (rp != pkg.Recipient_Phone) pkg.Recipient_Phone = rp!;

            // Trim content item names only when changed to avoid unnecessary notifications
            for (int i = 0; i < pkg.Recipient_Contents.Count; i++)
            {
                var item = pkg.Recipient_Contents[i];
                var tn = item.Name?.Trim();
                if (tn != item.Name) item.Name = tn!;
            }

            pkg.Recipient_Contents.RemoveAll(x => string.IsNullOrWhiteSpace(x.Name) && x.Amount == 0);

            if (pkg.Recipient_Contents.Count > 0)
            {
                var serialized = JsonConvert.SerializeObject(pkg.Recipient_Contents);
                if (serialized != pkg.Contents) pkg.Contents = serialized;
            }

            return pkg;
        }
    }
}
