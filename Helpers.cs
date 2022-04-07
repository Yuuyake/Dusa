using GetUserInfo.Properties;
using Pastel;
using Salaros.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace GetUserInfo {
    internal class Helpers {
        public static string configsFilePath = "./GetUserInfo.cfg"; // constant
        public static string errorsFilePath  = "./GetUserInfo.err"; // default
        public static char borderChr;

        public static ConfigParser PrepareConfigs( ) {
            ConfigParser tempConfig = new ConfigParser();
            ConfigParserSettings cfgsettings = new ConfigParserSettings();
            cfgsettings.MultiLineValues = MultiLineValues.Simple;

            try {
                tempConfig = new ConfigParser( configsFilePath, cfgsettings );
                if(tempConfig.Lines.Count == 0) {
                    File.WriteAllBytes( configsFilePath, Resources.GetUserInfo );
                    tempConfig = new ConfigParser( configsFilePath, cfgsettings );
                    //tempConfig.GetValue("Strings", "canBeIndented");          // = value
                }
                errorsFilePath = tempConfig.GetValue( "Strings", "errorFilePath", errorsFilePath );

                string borderCodePage = tempConfig.GetValue( "Strings", "borderCodePage", "IBM437" );
                string borderAsciiNoStr = tempConfig.GetArrayValue( "Advanced", "borderAsciiNo" )[0];
                int borderAsciiNo = int.TryParse( borderAsciiNoStr, out _ ) ? int.Parse( borderAsciiNoStr ) : 179;
                // getting the visual character from CP 437 extended ascii table
                // https://theasciicode.com.ar/extended-ascii-code/box-drawings-single-vertical-line-character-ascii-code-179.html
                borderChr = Encoding.GetEncoding( borderCodePage ).GetString( new byte[1] { (byte)borderAsciiNo } )[0];
            }
            catch(Exception err) { PrintUnexpectedErr( err, "CONFIG FILE IS MALFORMED, delete it and RERUN -> " + configsFilePath ); }
            return tempConfig;
        }
        public static string PrepareQueryWithInfo( string infoToSearch ) {
            // Example PowerShell
            // ([adsisearcher]"(&(objectCategory=user)(samaccountname=SİCİL))").FindAll() | ForEach - Object{[pscustomobject]@{ displayname = $_.properties['displayname'][0]; department = $_.properties['department'][0]} }
            // ([adsisearcher]"(&(objectCategory=user)(displayName=*emre ekinci*))").FindAll().Properties.displayname

            string _infoToSearch = infoToSearch.Trim().ToLower().Replace( "  ", "" ).Replace( "(", "" ).Replace( ")", "" );

            Regex sicilRegex    = new Regex( Program.configs.GetValue( "Strings", "sicilRegex" ), RegexOptions.IgnoreCase );
            Regex serviceRegex  = new Regex( Program.configs.GetValue( "Strings", "serviceRegex" ), RegexOptions.IgnoreCase );
            Regex fullnameRegex = new Regex( Program.configs.GetValue( "Strings", "fullnameRegex" ), RegexOptions.IgnoreCase );
            Regex phoneRegex    = new Regex( Program.configs.GetValue( "Strings", "phoneRegex" ), RegexOptions.IgnoreCase );

            if(sicilRegex.IsMatch( infoToSearch )) {
                string critAccFlag  = Program.configs.GetValue( "Strings", "critAccFlag", "-1" );
                string adminAccFlag = Program.configs.GetValue( "Strings", "adminAccFlag", "-1" );

                _infoToSearch = Program.configs.GetValue( "Boolean", "deleteCritAccFlag" ) == "True" ? _infoToSearch.Replace( critAccFlag , "" ) : _infoToSearch;
                _infoToSearch = Program.configs.GetValue( "Boolean", "deleteAdminFlag"   ) == "True" ? _infoToSearch.Replace( adminAccFlag, "" ) : _infoToSearch;
            }

            var sicilMatch      = sicilRegex.Match( _infoToSearch );
            var serviceMatch    = serviceRegex.Match( _infoToSearch );
            var phoneMatch      = phoneRegex.Match( _infoToSearch.Replace( " ", "" ) );
            var fullnameMatch   = fullnameRegex.Match( _infoToSearch );

            if(!sicilMatch.Success && !serviceMatch.Success && !phoneMatch.Success && !fullnameMatch.Success) {
                PrintUnexpectedErr( new Exception( "THE SPECIFIED STRING IS NOT A PROPER INFO TO SEARCH" ), "No regex (sicil,phone,fullname) can match with string : " + _infoToSearch );
            }

            string queryString = "(&" + "(objectClass=user)";
            queryString = ( sicilMatch.Success || serviceMatch.Success ) ? queryString + "(samAccountName=" + serviceMatch.Value + ")" : queryString;
            queryString = fullnameMatch.Success == true ? queryString + "(displayName=*" + fullnameMatch.Value.Replace( " ", "*" ) + "*)" : queryString;
            queryString = phoneMatch.Success == true ? queryString + "(mobile=*" + phoneMatch.Value.Replace( "(", "" ).Replace( ")", "" ).Replace( " ", "" ) + "*)" : queryString;
            queryString += ")";

            return queryString;
        }
        public static void PrintUnexpectedErr( Exception err, string cause ) {
            // https://stackoverflow.com/questions/18316683/how-to-get-the-current-project-name-in-c-sharp-code/39718377

            string printDivider = "\n\n \t_".PadRight( 80, '_' );
            string errorPosition = "\n\n\t" + DateTime.Now + " !!! ERROR in function '" + ( ( new System.Diagnostics.StackTrace() ).GetFrame( 1 ).GetMethod().Name ?? " " );
            string errorCause = "'\n\t\t Possible Cause : " + cause;
            string errorMessage = "\n\t\t Error Message  : " + err.Message;

            File.AppendAllLines( errorsFilePath, new List<string> { ( printDivider + errorPosition + errorCause + errorMessage + printDivider ) } );
            Console.WriteLine( printDivider + errorPosition + errorCause.Pastel( Color.Red ) + errorMessage + printDivider );

            Console.ReadLine();
            Environment.Exit( 0 );
        }
        public static void PrintHeaders( string req ) {

            Console.WindowHeight = Console.LargestWindowHeight - 2;
            Console.OutputEncoding = Encoding.UTF8;

            char borderChr = Helpers.borderChr;
            string headerFlagFile = Program.configs.GetValue( "Strings", "headerFlagFile", "headerFlag1" );
            List<string> headerFlag =
                headerFlagFile == "headerFlag1" ?
                Resources.HeaderFlag1.Replace( "\r", "" ).Split( '\n' ).ToList() :
                Resources.HeaderFlag2.Replace( "\r", "" ).Split( '\n' ).ToList();

            string headerInfoFile = Program.configs.GetValue( "Strings", "headerInfoFile", "headerInfo1" );
            List<string> headerInfo =
                headerInfoFile == "headerInfo1" ?
                Resources.HeaderInfo1.Replace( "\r", "" ).Split( '\n' ).ToList() :
                Resources.HeaderInfo2.Replace( "\r", "" ).Split( '\n' ).ToList();

            foreach(var line in headerFlag.Zip( headerInfo, Tuple.Create )) {
                Console.WriteLine( line.Item1.Pastel( Color.Yellow ) + "\t" + line.Item2.Pastel( Color.White ) );
            }
            Console.WriteLine( " The request " + req.Pastel( Color.Red ) + " will be processed at a domain controller" );
            Console.WriteLine( ( " " + ( borderChr == '│' ? '┌' : borderChr ) ).PadRight( 110, borderChr == '│' ? '─' : borderChr ) + "\n " + borderChr );
        }
        public static string SetClipboard( CustomUserInfo userFound ) {
            string propertyToClipboard = Program.configs.GetArrayValue( "Advanced", "copytoclipboard", new string[] { "userFullName" } )[0];
            string strToClipboard = userFound.GetType().GetProperty( propertyToClipboard ).GetValue( userFound ) as string;

            Clipboard.SetText( strToClipboard );
            Console.Write( ( "\n\n\n\t Text copied to clipboard : " + strToClipboard ).Pastel( Color.Yellow ) );

            return strToClipboard;
        }

        public static void PrintMatrixed( List<string> matrix, bool alltime = false ) {
            List<int> lineRange = Enumerable.Range( 0, 100 ).ToList();
            bool forward = true;
            string trace = " ░▒▓";

            while(true) {
                foreach(int pos in lineRange) {
                    int coorY = 0;
                    foreach(string currLine in matrix) {
                        Console.SetCursorPosition( pos, coorY );
                        string toScreen = forward == true ? currLine[pos] + trace : trace + currLine[pos];
                        Console.Write( toScreen );
                        Console.Write( '\n' );
                        coorY++;
                    }
                    Thread.Sleep( 20 );
                } // END for-each
                if(alltime) { break; }
                lineRange.Reverse();
                trace = String.Join( "", trace.Reverse());
                forward = !forward;
            } // END while
        } // END PrintMatrixed
    } // END class
}