using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PelotonIDE.Presentation
{
    public class PropertyBag
    {
        private Dictionary<string, object> _values;
        private Dictionary<string, object> _duplicates;
        private Dictionary<long, string> _opids;
        private Dictionary<string, long> _optxs; 
        private string _name;
        public PropertyBag()
        {
            _values = [];
            _duplicates = [];
            _opids = [];
            _optxs = [];
            _name = string.Empty;
        }
        public Dictionary<string, object> Contents { get { return _values; } }
        public string Name { get { return _name; } }
        public void LoadBagFromFile(string fileName, bool debug = false)
        {
            if (debug) Debugger.Launch();
            Span<byte> bytes = File.ReadAllBytes(fileName);
            LoadBagFromByteArray(bytes.Slice(12).ToArray(), debug);
            _name = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
            IEnumerable<string> ids = from key in _values.Keys where key.Length == 9 && long.TryParse(key, out long _) select key;
            foreach (string? id in ids)
            {
                long Id = long.Parse(id);
                _opids[Id] = (string)_values[id];
                string? txkey = _values[id].ToString();
                _optxs[txkey!] = Id;
            }
        }
        private void LoadBagFromByteArray(byte[] data, bool debug = false)
        {
            if (debug) Debugger.Launch();

            if (data != null && data.Length > 12
                && data[0] == 147 && data[1] == 178)
            {
                int offset = 4;
                int totalSize = BitConverter.ToInt32(data, offset);

                if (data.Length != totalSize)
                    throw new IndexOutOfRangeException("The PropertyBag total size does not match the actual data size");

                offset += 4;

                while (data.Length > offset + 4)
                {
                    PropertyBagValueType valueType = (PropertyBagValueType)BitConverter.ToUInt16(data, offset);
                    offset += 2;

                    ushort valueNameLen = BitConverter.ToUInt16(data, offset);
                    offset += 2;
                    string valueName;

                    offset += 8; // Some data I don't understand

                    valueName = UnicodeEncoding.Unicode.GetString(data, offset, valueNameLen * 2);
                    offset += valueNameLen * 2; // Unicode take 2 bytes per char
                    int valueLen = 0;

                    switch (valueType)
                    {
                        case PropertyBagValueType.Bool:
                            valueLen = 2;
                            if (data[offset] == 0)
                                if (_values.ContainsKey(valueName))
                                    _duplicates.Add(valueName, false);
                                else
                                    _values.Add(valueName, false);
                            else
                            {
                                Debug.Assert(data[offset] == 255);
                                if (_values.ContainsKey(valueName))
                                    _duplicates.Add(valueName, true);
                                else
                                    _values.Add(valueName, true);
                            }
                            break;
                        case PropertyBagValueType.Byte:
                            valueLen = 2;
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, data[offset]);
                            else
                                _values.Add(valueName, data[offset]);
                            break;
                        case PropertyBagValueType.Int16:
                            valueLen = 2;
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, BitConverter.ToInt16(data, offset));
                            else
                                _values.Add(valueName, BitConverter.ToInt16(data, offset));
                            break;
                        case PropertyBagValueType.Int32:
                            valueLen = 4;
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, BitConverter.ToInt32(data, offset));
                            else
                                _values.Add(valueName, BitConverter.ToInt32(data, offset));
                            break;
                        case PropertyBagValueType.Single:
                            valueLen = 4;
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, BitConverter.ToSingle(data, offset));
                            else
                                _values.Add(valueName, BitConverter.ToSingle(data, offset));
                            break;
                        case PropertyBagValueType.Double:
                            valueLen = 8;
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, BitConverter.ToDouble(data, offset));
                            else
                                _values.Add(valueName, BitConverter.ToDouble(data, offset));
                            break;
                        case PropertyBagValueType.Currency:
                            valueLen = 8;
                            long i64Value = BitConverter.ToInt64(data, offset);
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, i64Value / 10000.0);
                            else
                                _values.Add(valueName, i64Value / 10000.0);
                            break;
                        case PropertyBagValueType.String:
                            valueLen = BitConverter.ToInt32(data, offset) * 2; // Unicode
                            offset += 4;
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, UnicodeEncoding.Unicode.GetString(data, offset, valueLen));
                            else
                                _values.Add(valueName, UnicodeEncoding.Unicode.GetString(data, offset, valueLen));
                            break;
                        case PropertyBagValueType.ByteArray:
                            offset += 2; // The value in the current offset seem to be 1. It might be the size per item in the array. Other array types are not seem to be supported
                            valueLen = BitConverter.ToInt32(data, offset);
                            offset += 4;
                            offset += 4; // I have no idea why extra 4 bytes padding. Maybe it's for supporting arrays with size larger than 4GB, but VB is 32 bit, so it can't support more than 4GB in RAM
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, data.Skip(offset).Take(valueLen).ToArray());
                            else
                                _values.Add(valueName, data.Skip(offset).Take(valueLen).ToArray());
                            break;
                        case PropertyBagValueType.Date:
                            valueLen = 8;
                            /* A Date is stored as an IEEE 64-bit (8-byte) floating point number, just like a Double
                            Digits to the left of the decimal point (when converted to decimal) are interperated as a date between 1 January 100 and 31 December 9999. Values to the right of the decimal point indicate time between 0:00:00 and 23:59:59.
                            The date section of the Date data type (before the decimal point), is a count of the number of days that have passed since 1 Jan 100, offset by 657434. That is, 1 Jan 100 is denoted by a value of -657434; 2 Jan 100 is denoted -657433; 31 Dec 1899 is 1; 1 Jan 2000 is 36526, and so on.
                            The time section of the date (after the decimal point) is the fraction of a day, expressed as a time. For example, 1.5 indicates the date 31 Dec 1899 (as above) and half a day, i.e. 12:00:00. So, an hour is denoted by an additional 4.16666666666667E-02, a minute by 6.94444444444444E-04, and a second by 1.15740740740741E-05.

                            Taken from: https://www.codeguru.com/visual-basic/how-visual-basic-6-stores-data/
                            */
                            double num = BitConverter.ToDouble(data, offset);
                            DateTime dateTime = new DateTime(1899, 12, 30);
                            double days = Math.Floor(num);
                            double seconds = (num - days) * (3600 * 24);

                            dateTime = dateTime.AddDays(days);
                            dateTime = dateTime.AddSeconds(seconds);
                            if (_values.ContainsKey(valueName))
                                _duplicates.Add(valueName, dateTime);
                            else
                                _values.Add(valueName, dateTime);
                            break;
                        default:
                            throw new NotSupportedException("Unsupported value type");
                    }

                    offset += valueLen;

                    if (offset % 4 != 0)
                        offset += 4 - offset % 4;
                }
            }
        }
        public IEnumerable<string> Keys => _values.Keys.ToList();
        public string AsJSON() => JsonConvert.SerializeObject(_values);
        public string DuplicatesAsJSON() => JsonConvert.SerializeObject(_duplicates);
        public bool HasDuplicates => _duplicates.Count > 0;
        public Dictionary<long, string> Identifiers => _opids;
        public string IdentifiersAsJSON => JsonConvert.SerializeObject(Identifiers);
        public Dictionary<string, long> Keywords => _optxs;
        public string KeywordsAsJSON => JsonConvert.SerializeObject(Keywords);
        public object? ReadValue(string key, object? defaultValue = null)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            string keyToUse = key.ToLowerInvariant();
            if (_values.ContainsKey(keyToUse))
                return _values[keyToUse];

            return defaultValue;
        }
        public string? ReadValueAsString(string key, string? defaultValue = "")
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key null or empty");

            string keyToUse = key.ToLowerInvariant();
            if (_values.ContainsKey(keyToUse))
                return _values[keyToUse].ToString();

            return defaultValue;
        }
    }

    internal enum PropertyBagValueType
    {
        Bool = 11,
        Byte = 17, // 0x11
        Int16 = 2,
        Int32 = 3,
        Single = 4,
        Double = 5,
        Currency = 6,
        String = 8,
        Date = 7,
        ByteArray = 8209, //0x2011
    }

}
