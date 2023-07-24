using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using TelegramAPIPlugin.Utils;

namespace TelegramAPIPlugin.TdlibSetup {
    public class AuthSetting  {
        /// <summary>
        /// This is the APIID which will be generated after PhoneNr Registration on https://my.telegram.org/auth
        /// </summary>
        public int ApiId { get; }

        /// <summary>
        /// Is Also Provided by TelegramWebsite
        /// </summary>
        public string ApiHash { get; }

        // PhoneNumber must contain international phone with (+) prefix.
        // For example +16171234567
        public string PhoneNumber { get; }
        public string Name { get; }

        public string DBlocation { get; set; }

        public bool Equals(AuthSetting obj) {
            return ApiId.Equals(obj.ApiId) && ApiHash.Equals(obj.ApiHash) && PhoneNumber.Equals(obj.PhoneNumber) &&
                   Name.Equals(obj.Name) && DBlocation.Equals(obj.DBlocation);
        }


        public override bool Equals(object obj) {
            if (obj is AuthSetting setting) {
                return Equals(setting);
            }
            else {
                return base.Equals(obj);
            }
        }

        //This was auto generated
        public override int GetHashCode() {
            unchecked {
                var hashCode = ApiId;
                hashCode = (hashCode * 397) ^ (ApiHash != null ? ApiHash.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PhoneNumber != null ? PhoneNumber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DBlocation != null ? DBlocation.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() {
            return $"{Name} - [{PhoneNumber}]";
        }

        public AuthSetting(int apiId, string apiHash, string phoneNumber, string name) {
            DBlocation = Path.Combine("./", "tg_profiles", "db_" + name);
            ApiId = apiId;
            ApiHash = apiHash;
            PhoneNumber = phoneNumber;
            Name = name;
        }

        [JsonConstructor]
        public AuthSetting(int apiId, string apiHash, string phoneNumber, string name, string dBlocation) {
            DBlocation = dBlocation;
            ApiId = apiId;
            ApiHash = apiHash;
            PhoneNumber = phoneNumber;
            Name = name;
        }


        public AuthSetting getCopy() {
            return new AuthSetting(ApiId, ApiHash, PhoneNumber, Name, DBlocation);
        }
    }
}