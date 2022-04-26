using Pastel;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Drawing;
using System.Linq;

namespace GetUserInfo {
    public class CustomUserInfo{
        public string userSicil { get; set; }
        public string userEmplId { get; set; }
        public string userFullName { get; set; }
        public string userUnit { get; set; }
        public string userDepartment { get; set; }
        public string userPhone { get; set; }
        public string description { get; set; }
        public string passLastSet { get; set; }
        public string passRequired { get; set; }
        public string lastLogon { get; set; }
        public string badPwdCount { get; set; }
        public string badPwdTime { get; set; }
        public UserAccountControlAttr userUACProp { get; set; }
        public List<string> userUACPropList { get; set; }
        public List<string> userMemberOfList { get; set; }

        public CustomUserInfo(SearchResult searchResult, bool multi = false ) {
            if (searchResult == null) { return; }
            //msds-psoapplied attributu önemli !!!

            userSicil       = searchResult.GetPropertyAsString( "SamAccountName" );
            userEmplId      = searchResult.GetPropertyAsString( "employeeId");
            userFullName    = searchResult.GetPropertyAsString( "GivenName");
            userUnit        = searchResult.GetPropertyAsString( "DisplayName");
            userDepartment  = searchResult.GetPropertyAsString( "Department");
            userPhone       = searchResult.GetPropertyAsString( "Mobile" );
            description     = searchResult.GetPropertyAsString( "description" );
            passLastSet     = searchResult.GetPropertyAsString( "pwdLastSet");
            lastLogon       = searchResult.GetPropertyAsString( "LastLogon");
            badPwdCount     = searchResult.GetPropertyAsString( "badPwdCount" );
            badPwdTime      = searchResult.GetPropertyAsString( "badPasswordTime" );
            userUACProp     = new UserAccountControlAttr();
            userUACPropList = new List<string>();
            userMemberOfList= searchResult.GetMemberOfList();
            passRequired    = "NULL";

            userFullName = multi ? searchResult.GetPropertyAsString( "DisplayName" ) : userFullName + " " + searchResult.GetPropertyAsString( "sn" );
            badPwdCount = badPwdCount == "0" ? "None" : badPwdCount;
            badPwdTime  = badPwdTime  == "0" ? "None" : badPwdTime;

            try { userUnit = String.Join("", userUnit.SkipWhile(cc => cc != '(')); }
            catch{ userUnit = "CANNOT PARSE"; }

            try { userPhone = userPhone.Contains( "NOTFOUND" ) ? userPhone : 
                    ( (userPhone.StartsWith("0") ? "" : "0") + userPhone).Replace("+90", "").Insert(4, " ").Insert(8, " ").Insert(11, " "); }
            catch{ userPhone = "CANNOT PARSE"; }

            try { userUACProp = new UserAccountControlAttr( int.Parse( searchResult.GetPropertyAsString( "UserAccountControl" ) ) ); }
            catch(Exception err) { Helpers.PrintUnexpectedErr( err, "userUACProp" ); }

            try { userUACPropList = userUACProp.myUserControls.Where( control => control.Key == "YES" ).Select( cc => cc.Value ).ToList(); }
            catch(Exception err) { Helpers.PrintUnexpectedErr( err, "userUACPropList" ); }

            try { passRequired = userUACProp.myUserControls.Find(control => control.Value == "PASSWD_NOTREQD").Key == "YES" ? "NO" : "YES"; }
            catch (Exception err) { Helpers.PrintUnexpectedErr(err, "passRequired"); }

        }
        public CustomUserInfo() { }
        public void PrintUserInfos(int verbose = -1){
            int padwidth = 25;

            string br = " " + Helpers.borderChr + " ";
            string fullinfo = userSicil + " - " + userFullName + " - " + userPhone;
            Console.WriteLine(br + "User Full Info".PadRight(padwidth) + fullinfo.Pastel(Color.MediumVioletRed));
            if (verbose == 1) {
                userUACPropList.ForEach(uac => Console.Write(br + " ".PadRight(padwidth) + uac.Pastel(Color.Yellow) + '\n'));
                Console.Write("\n");
                return; 
            }
            Console.WriteLine(br + "User Department".PadRight(padwidth) + userDepartment.Pastel(Color.LightYellow));
            Console.WriteLine(br + "User Unit".PadRight( padwidth ) + userUnit.Pastel( Color.LightYellow ) );
            Console.WriteLine(br + "Description".PadRight( padwidth ) + description.Pastel( Color.LightYellow ) );

            if(verbose <= 3 && verbose > 1) { return; }
            Console.WriteLine(br);
            Console.WriteLine(br + "Last Valid Logon".PadRight(padwidth) + lastLogon.Pastel(Color.LightYellow));
            Console.WriteLine(br + "Last Invalid Logon".PadRight( padwidth ) + badPwdTime.Pastel( Color.LightYellow ) );
            Console.WriteLine(br + "Last Password Set".PadRight( padwidth ) + passLastSet.Pastel( Color.LightYellow ) );

            if(verbose <= 6 && verbose > 3) { return; }
            Console.WriteLine(br + "Is Password Required".PadRight( padwidth ) + passRequired.Pastel( Color.LightYellow ) );
            Console.WriteLine( br + "Recent BadPwd Count".PadRight( padwidth ) + badPwdCount.Pastel( Color.MediumVioletRed ) );
            Console.WriteLine(br);
            Console.Write(br + "User Account Control".PadRight(padwidth));
            userUACPropList.ForEach(uac => Console.Write(uac.Pastel(Color.MediumVioletRed) + '\n' + br.PadRight(padwidth + 3)));
            Console.Write('\n' + br + "User Member Of".PadRight(padwidth));

            if (userMemberOfList.Count == 0) { Console.Write( "CANNOT FOUND(userMemberOfList)".Pastel(Color.Red)); }
            else {
                int i = 0;
                foreach(string grpName in userMemberOfList) {
                    string toPrint = "•" + grpName.PadRight( 20 ).Pastel( Color.Gold );
                    toPrint = i % 2 == 0 ? toPrint : toPrint + '\n' + br.PadRight( padwidth + 3 );
                    Console.Write( toPrint );
                    //Console.Write( grpName.Pastel( Color.Gold ) + '\n' + br.PadRight( padwidth + 3 ) );
                    i++;
                }               
            }

        } //  end of func PrintUserInfos
    }
}
