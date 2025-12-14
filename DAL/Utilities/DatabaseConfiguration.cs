using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Utilities
{
    public class DatabaseConfiguration
    {
        public static string DB_SCHEMA = "public.";
        public static string Users = DB_SCHEMA + "users";
        public static string EventOrganizer = DB_SCHEMA + "event_organizer";
        public static string OtpVerification = DB_SCHEMA + "otp_verification";
        public static string RoleMaster = DB_SCHEMA + "role_master";
        public static string BannerManagement = DB_SCHEMA + "banner_management";
        public static string testimonial = DB_SCHEMA + "testimonial";
        public static string event_category = DB_SCHEMA + "event_category";
        public static string events = DB_SCHEMA + "events";
        public static string event_media = DB_SCHEMA + "event_media";
        public static string event_gallary = DB_SCHEMA + "event_gallary";
        public static string event_artist = DB_SCHEMA + "event_artist";
        public static string event_seat_type_inventory = DB_SCHEMA + "event_seat_type_inventory";
    }
}
