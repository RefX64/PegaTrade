using Mapster;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PegaTrade.Core.StaticLogic.Helper
{
    public static class Utilities
    {
        public static PegaUser GetDevUser(string username)
        {
            if (AppSettingsProvider.IsDevelopment)
            {
                using (EntityFramework.PegasunDBContext db = new EntityFramework.PegasunDBContext())
                {
                    PegaUser user = db.Users.First(x => x.Username == username).Adapt<PegaUser>();
                    user.Portfolios = db.Portfolios.Where(x => x.UserId == user.UserId).Adapt<List<Layer.Models.Coins.Portfolio>>();
                    user.PTUserInfo = db.PTUserInfo.FirstOrDefault(x => x.UserId == user.UserId).Adapt<PTUserInfo>();
                    return user;
                }
            }
            return null;
        }

        public static string Serialize(object obj)
        {
            return NetJSON.NetJSON.Serialize(obj);
        }

        public static T Deserialize<T>(string serializedObject)
        {
            return NetJSON.NetJSON.Deserialize<T>(serializedObject);
        }

        public static int GetCurrentEpochTime()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (int)t.TotalSeconds;
        }

        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        public static IEnumerable<T> EnumToList<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
        
        public static bool ValidateReCaptcha(string key, string responsee)
        {
            using (HttpClient client = new HttpClient())
            {
                var getTask = client.GetAsync($"https://www.google.com/recaptcha/api/siteverify?secret={key}&response={responsee}");
                Task.WaitAll(getTask);

                HttpResponseMessage response = getTask.Result;
                var readTask = response.Content.ReadAsStringAsync();
                Task.WaitAll(readTask);

                string resultJson = readTask.Result;
                ReCaptchaResult result = NetJSON.NetJSON.Deserialize<ReCaptchaResult>(resultJson);
                return result.success;
            }
        }

        #region Coins Related

        public static decimal UsdToEuro(this decimal usd)
        {
            return usd * (decimal).85;
        }

        public static string Symbol(this Types.CoinCurrency currency) { return GetCurrencySymbol(currency); }
        private static string GetCurrencySymbol(Types.CoinCurrency currency)
        {
            switch (currency)
            {
                case Types.CoinCurrency.USD: return "$";
                case Types.CoinCurrency.BTC: return "฿";
                case Types.CoinCurrency.ETH: return "ξ";
                case Types.CoinCurrency.EUR: return "£";
                default: return "$";
            }
        }

        public static string CSS_PriceRoseDecline(decimal priceChange)
        {
            return priceChange > 0 ? "price-rose-text" : "price-fell-text";
        }

        #endregion

        #region Encryptions

        public static string GenerateSHA512Hash(string key)
        {
            using (SHA512 sha = SHA512.Create())
            {
                var hashedBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                return BitConverter.ToString(hashedBytes).Replace("-", string.Empty).ToLower();
            }
        }

        // If need to compare results with PHP_hmac, use: http://www.writephponline.com/
        public static string GenerateHmacSHA256Hash(string message, string key, bool rawFormat = false)
        {
            using (HMACSHA256 hmacSHA256 = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var hashedBytes = hmacSHA256.ComputeHash(Encoding.UTF8.GetBytes(message));
                return rawFormat ? Convert.ToBase64String(hashedBytes) : BitConverter.ToString(hashedBytes).Replace("-", string.Empty).ToLower();
            }
        }

        public static string GenerateHmacSHA512Hash(string message, string key)
        {
            using (HMACSHA512 hmacSHA512 = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                var hashedBytes = hmacSHA512.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hashedBytes).Replace("-", string.Empty).ToLower();
            }
        }
        
        #endregion

        public static bool IsValidEmailAddress(string email)
        {
            Regex rx = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            return rx.IsMatch(email);
        }

        public static bool SendEmailUsingSyneiGmail(string fromEmail, string fromName, string subject, string body, string sendToEmail = null)
        {
            try
            {
                SmtpClient client = new SmtpClient
                {
                    Host = "Smtp.Gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Timeout = 10000,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential("xxx@gmail.com", "xxx")
                };

                MailMessage mail = new MailMessage(fromEmail, sendToEmail ?? "support@pegatrade.com", subject, body);
                mail.ReplyToList.Add(new MailAddress(fromEmail, fromName));

                client.Send(mail);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void LogException(string[] data, Exception ex)
        {
            var _ = LogExceptionAsync(data, ex).ConfigureAwait(false);
        }

        public static async Task LogExceptionAsync(string[] data, Exception ex)
        {
            string paramString = string.Join("|", data);
            using (EntityFramework.PegasunDBContext db = new EntityFramework.PegasunDBContext())
            {
                EntityFramework.Exceptions exception = new EntityFramework.Exceptions
                {
                    SystemCode = (int)Types.SystemCode.PegaTrade,
                    Date = DateTime.Now,
                    ExtraData = paramString,
                    InnerMessage = ex.InnerException?.Message ?? string.Empty,
                    Message = ex.Message,
                    Source = $"{ex.Source} | {ex.StackTrace}"
                };

                db.Add(exception);
                await db.SaveChangesAsync();
            }
        }

        public static string FormatPortfolioName(string portfolioName)
        {
            return portfolioName.Replace(" ", string.Empty).Replace("%20", string.Empty).Replace("&", "And");
        }

        private class ReCaptchaResult { public bool success { get; set; } }
    }

    public static class Extentions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return list == null || !list.Any();
        }

        public static bool IsMin(this DateTime date)
        {
            return date == DateTime.MinValue;
        }

        public static string RemoveTrailingZero(this decimal target)
        {
            if (target == 0) return "0";

            string strValue = target.ToString(CultureInfo.InvariantCulture);
            if (strValue.Contains("."))
            {
                strValue = strValue.TrimEnd('0');
                if (strValue.EndsWith(".")) { strValue = strValue.TrimEnd('.'); }
            }
            return strValue;
        }
        
        public static string ToNumeric(this string str, bool allowDecimals = true)
        {
            return allowDecimals ? Regex.Replace(str, @"[^0-9.]", "") : Regex.Replace(str, "[^0-9]", "");
        }

        public static bool EqualsTo(this string first, string second)
        {
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsTheWord(this string first, string second)
        {
            return first.IndexOf(second, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static int ToEpochDayAt12am(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt32((date.Date - epoch).TotalSeconds);
        }

        public static string Clean(this string str, Types.CleanInputType[] cleanTypes)
        {
            if (string.IsNullOrEmpty(str)) { return str; }

            // RegEx help: ^:Not, ([CharactersToMatch])
            // http://regexr.com/
            string regexString = string.Empty;

            // If AZ09CommonCharsSM, ignore everything else.
            foreach (var cleanType in cleanTypes)
            {
                if (cleanType == Types.CleanInputType.AZ09CommonCharsSM) { regexString = "a-zA-Z0-9\t\n ./?,;:\"'`!@#$%^&*()\\[\\]{}_+=|\\-"; break; }

                if (cleanType == Types.CleanInputType.Letters) { regexString += "a-zA-Z"; }
                else if (cleanType == Types.CleanInputType.Digits) { regexString += "0-9"; }
                else if (cleanType == Types.CleanInputType.Dash) { regexString += "-"; }
                else if (cleanType == Types.CleanInputType.Space) { regexString += " "; }
            }

            return Regex.Replace(str, $"([^{regexString}])", string.Empty);
        }

        public static bool IsDemoUser(this PegaUser user) => user != null && user.Username == "DemoUser";
        public static bool IsValidUser(this PegaUser user) => user != null && user.Username != "DemoUser";

        #region Calculations

        public static decimal ToRoundedWholeNumber(this decimal dec)
        {
            return Math.Round(dec, 0);
        }

        public static string ToTwoDigit(this decimal dec, bool showPlusSign = false)
        {
            bool showPlus = (showPlusSign && dec > 0);
            string result = showPlus ? "+" + string.Format("{0:0.00}", dec) : string.Format("{0:0.00}", dec);
            return result;
        }

        public static string ToWholeNumberByCurrency(this decimal dec, Types.CoinCurrency displayType)
        {
            if (displayType == Types.CoinCurrency.BTC || displayType == Types.CoinCurrency.ETH) { return dec.ToString(new CultureInfo("en-US")); }
            return dec.ToRoundedWholeNumber().ToString(new CultureInfo("en-US"));
        }

        public static decimal ToDecimalPrecision(this decimal dec, int maxPrecision)
        {
            return decimal.Round(dec, maxPrecision, MidpointRounding.AwayFromZero);
        }

        public static decimal ToDecimal(this string str)
        {
            try
            {
                if (string.IsNullOrEmpty(str)) { return 0; }
                str = str.Replace("$", string.Empty).Replace(",", string.Empty);

                return Math.Round(decimal.Parse(str), 2);
            }
            catch { return 0; }
        }

        public static string ToFormatNumber(this decimal dec, bool showDecimal = false)
        {
            string format = showDecimal ? "N" : "#,##0";
            return dec.ToString(format, new CultureInfo("en-US"));
        }

        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            string description = e.ToString(); // If no description, return default string value.
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if (descriptionAttributes.Length > 0) { description = ((DescriptionAttribute)descriptionAttributes[0]).Description; }

                        break;
                    }
                }
            }
            return description;
        }

        #endregion

        public static string GenerateLabelHTML(this Types.ConvTagCode tagCode)
        {
            if (tagCode == Types.ConvTagCode.Bullish) { return "<label class='label label-success'>Long</label>"; }
            if (tagCode == Types.ConvTagCode.Bear) { return "<label class='label label-danger'>Short</label>"; }

            return string.Empty;
        }
    }

    public static class Cryptography
    {
        #region Settings

        private static int _iterations = 2;
        private static int _keySize = 256;

        private static string _hash = "SHA1";
        private static string _salt = "xxx"; // Random
        private static string _vector = "xxx"; // Random

        private static void SetProperSaltAndVector(Types.EncryptionType encryptionType)
        {
            switch (encryptionType)
            {
                case Types.EncryptionType.ApiKey_Public:
                case Types.EncryptionType.ApiKey_Private:
                    _salt = "xxx+";
                    _vector = "xxx";
                    return;
            }
        }

        private static string GeneratePassword(Types.EncryptionType encryptionType, PegaUser user)
        {
            switch (encryptionType)
            {
                case Types.EncryptionType.ApiKey_Public: return $"QZ{string.Join("d", user.Username.Reverse())}_V638uhWV";
                case Types.EncryptionType.ApiKey_Private: return $"TZ{user.Username}V638{string.Join("b", user.CreatedDate.ToShortDateString().Replace("/", "").Reverse())}_$WV";
            }

            return string.Empty;
        }

        #endregion

        // Use other encryptions
        // string encrypted = Cryptography.Encrypt<RijndaelManaged>(dataToEncrypt, password);
        // string decrypted = Cryptography.Decrypt<RijndaelManaged>(encrypted, password);

        #region Encrypt
        public static string Encrypt(string value, Types.EncryptionType encryptionType, PegaUser user)
        {
            SetProperSaltAndVector(encryptionType);

            switch (encryptionType)
            {
                case Types.EncryptionType.ApiKey_Public: return Encrypt(value, GeneratePassword(encryptionType, user));
                case Types.EncryptionType.ApiKey_Private: return Encrypt(value, GeneratePassword(encryptionType, user));
            }

            return string.Empty;
        }

        public static string Encrypt(string value, string password)
        {
            return Encrypt<AesManaged>(value, password);
        }
        public static string Encrypt<T>(string value, string password) where T : SymmetricAlgorithm, new()
        {
            byte[] vectorBytes = Encoding.ASCII.GetBytes(_vector);
            byte[] saltBytes = Encoding.ASCII.GetBytes(_salt);
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);

            byte[] encrypted;
            using (T cipher = new T())
            {
                PasswordDeriveBytes _passwordBytes = new PasswordDeriveBytes(password, saltBytes, _hash, _iterations);
                byte[] keyBytes = _passwordBytes.GetBytes(_keySize / 8);

                cipher.Mode = CipherMode.CBC;

                using (ICryptoTransform encryptor = cipher.CreateEncryptor(keyBytes, vectorBytes))
                {
                    using (MemoryStream to = new MemoryStream())
                    {
                        using (CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write))
                        {
                            writer.Write(valueBytes, 0, valueBytes.Length);
                            writer.FlushFinalBlock();
                            encrypted = to.ToArray();
                        }
                    }
                }
                cipher.Clear();
            }
            return Convert.ToBase64String(encrypted);
        }
        #endregion

        #region Drcrypt
        public static string Decrypt(string value, Types.EncryptionType encryptionType, PegaUser user)
        {
            SetProperSaltAndVector(encryptionType);

            switch (encryptionType)
            {
                case Types.EncryptionType.ApiKey_Public: return Decrypt(value, GeneratePassword(encryptionType, user));
                case Types.EncryptionType.ApiKey_Private: return Decrypt(value, GeneratePassword(encryptionType, user));
            }

            return string.Empty;
        }

        public static string Decrypt(string value, string password)
        {
            return Decrypt<AesManaged>(value, password);
        }
        public static string Decrypt<T>(string value, string password) where T : SymmetricAlgorithm, new()
        {
            byte[] vectorBytes = Encoding.ASCII.GetBytes(_vector);
            byte[] saltBytes = Encoding.ASCII.GetBytes(_salt);
            byte[] valueBytes = Convert.FromBase64String(value);

            byte[] decrypted;
            int decryptedByteCount;

            using (T cipher = new T())
            {
                PasswordDeriveBytes _passwordBytes = new PasswordDeriveBytes(password, saltBytes, _hash, _iterations);
                byte[] keyBytes = _passwordBytes.GetBytes(_keySize / 8);

                cipher.Mode = CipherMode.CBC;

                try
                {
                    using (ICryptoTransform decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes))
                    {
                        using (MemoryStream from = new MemoryStream(valueBytes))
                        {
                            using (CryptoStream reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
                            {
                                decrypted = new byte[valueBytes.Length];
                                decryptedByteCount = reader.Read(decrypted, 0, decrypted.Length);
                            }
                        }
                    }
                }
                catch
                {
                    return String.Empty;
                }

                cipher.Clear();
            }
            return Encoding.UTF8.GetString(decrypted, 0, decryptedByteCount);
        }
        #endregion
    }
}


