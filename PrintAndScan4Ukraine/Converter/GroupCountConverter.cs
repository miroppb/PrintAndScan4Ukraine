using PrintAndScan4Ukraine.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PrintAndScan4Ukraine.Converter
{
    public class GroupCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var group = value as CollectionViewGroup;
            if (group == null) return "";

            string sender = group.Name?.ToString() ?? "";

            // If this is the second-level group (Recipient Phone)
            if (group.Items.Count > 0 && group.Items[0] is Package p)
            {
                string senderPhone = p.Sender_Phone;
                string phone = p.Recipient_Phone ?? "";
                int count = group.ItemCount;

                return $"{senderPhone} → {phone}   ({count} packages)";
            }

            return sender;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}