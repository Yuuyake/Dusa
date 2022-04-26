using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace GetUserInfo
{
    public static class SearchResultExt {

        public static string GetPropertyAsString(this SearchResult srcResult, string propertyName){
            srcResult.GetDirectoryEntry().RefreshCache(new string[] { propertyName });
            object value = new object();
            string notfound = "NOTFOUND(" + propertyName + ")";
            try { value = srcResult.Properties[propertyName][0]; }
            catch { value = notfound; }
            string valueStr = value.ToString();
            if ( ( propertyName.Contains("Last") || propertyName.Contains( "Time" ) ) && value != notfound) { valueStr = DateTime.FromFileTime((long)value).ToString(); }
            return valueStr;
        }
        public static List<string> GetMemberOfList(this SearchResult srcresult){
            List<string> nestedGroups = new List<string>();
            DirectoryEntry theUser = srcresult.GetDirectoryEntry();
            theUser.RefreshCache( new string[] { "tokenGroups" } );

            foreach(byte[] sid in theUser.Properties["tokenGroups"]){
                try { nestedGroups.Add( GetGroupNamebySID(sid)); } catch { }
            }
            nestedGroups.Sort();
            return nestedGroups;
        }
        public static string GetGroupNamebySID(byte[] SID) {
            string groupSID = new SecurityIdentifier( SID, 0 ).Value;
            DirectoryEntry grpuEnrty = new DirectoryEntry( "LDAP://<SID=" + groupSID + ">" );
            try { return grpuEnrty.Properties["samAccountName"][0].ToString(); } catch { return null; }
        }
        public static CustomUserInfo GetUser(this SearchResult srcResult){ return new CustomUserInfo( srcResult ); }
    }

}
